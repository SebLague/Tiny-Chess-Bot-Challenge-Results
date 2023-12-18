namespace auto_Bot_261;
using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

/// <summary>
/// 1.0.0: Minimax algorithm
/// 1.3.0: Evaluation boards for each piece types
/// 1.4.0: Evaluation boards optimized
/// 1.5.0: Evaluation boards encoded in ASCII for less token use
/// 1.6.0: End Evaluation boards for pawn and king + transition from start to end board based on # oppononent pieces left and log function
/// 1.7.0: Dynamic depth based on threshold from previous best score
/// 1.8.0: Dynamic depth based on time remaining and/or time ellapsed this turn
/// </summary>
public class Bot_261 : IChessBot
{
    // Valuate center of board more than edges
    private const string _center = "KKKKKMMMKMOOKMOQKMOQKMOOKMMMKKKK";

    // Evaluation board for each piece types based on their placement on the board
    // ASCII value is used - half board encoded since other half is symetricm
    private string _pawn = "KKKKLMMGLJIKKKKOLLMPMMOQUUUUKKKK";
    private string _pawnEnd = "KKKKMMMMMMMMOOOOQQQQUUUUZZZZKKKK";
    private string _knight = "ACEECGKLELMNEKNOELNOEKMNCGKKACEE";
    private string _bishop = "GIIIILKKIMMMIKMMILLMIKLMIKKKGIII";
    private string _rook = "KKKLJKKKJKKKJKKKJKKKJKKKLMMMKKKK";
    private string _queen = "GIIJIKLKILLLKKLLJKLLIKLLIKKKGIIJ";
    private string _king = "OQMKOOKKIGGGGEECECCAECCAECCAECCA";
    private string _kingEnd = "AEEEEFKKFGOPGHQSHIRTIJOQJKLLGIII";

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private int[] _pieceValues = { 0, 10, 30, 30, 50, 90, 900 };

    // Indicate whether MyBot is playing white pieces
    private bool _isPlayingWhite = false;

    // Indicate the coefficient to converge from start evaluation board (coeff: 0) to end evaluation board (coeff: 1)
    private double _coeff = 0.0;

    // Threshold based on previous best Score to determine whether it is worth going deeper
    private double _threshold = 100.0;

    public Move Think(Board board, Timer timer)
    {
        _isPlayingWhite = board.IsWhiteToMove;
        _coeff = (-Math.Log(BitOperations.PopCount(_isPlayingWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard)) / Math.Log(16)) + 1;

        // Order moves so most interesting moves are analyzed first with deeper depth
        var allMoves = board.GetLegalMoves()
            .OrderByDescending(m => GetMoveValue(m));

        var bestMoveScore = -9999.0;
        var bestMove = allMoves.First();

        foreach (var move in allMoves)
        {
            var depth = timer.MillisecondsRemaining < 2000 ? 1
                : timer.MillisecondsRemaining < 10000 || timer.MillisecondsElapsedThisTurn > 1000 ? 3 : 5;

            board.MakeMove(move);
            var moveScore = Minimax(board, depth - 1, -10000, 10000, false, 0);
            board.UndoMove(move);

            if (moveScore > bestMoveScore && !board.GameMoveHistory.TakeLast(2).Any(m => m.Equals(move)))
            {
                bestMoveScore = moveScore;
                bestMove = move;
            }
        }

        _threshold = bestMoveScore;

        return bestMove;
    }

    private double Minimax(Board board, int depth, double alpha, double beta, bool isMaximizingPlayer, int depthReserve)
    {
        if (depth <= 0)
        {
            var score = GetBoardValue(board);
            if (score > _threshold && depthReserve > 0)
            {
                depth += 2;
                depthReserve -= 2;
                _threshold = score;
            }
            else
            {
                return score;
            }
        }

        var allMoves = board.GetLegalMoves();

        if (isMaximizingPlayer)
        {
            var bestMoveScore = -9999.0;
            foreach (var move in allMoves)
            {
                board.MakeMove(move);
                bestMoveScore = Math.Max(bestMoveScore, Minimax(board, depth - 1, alpha, beta, !isMaximizingPlayer, depthReserve));
                board.UndoMove(move);
                alpha = Math.Max(alpha, bestMoveScore);
                if (beta <= alpha)
                {
                    return bestMoveScore;
                }
            }
            return bestMoveScore;
        }
        else
        {
            var bestMoveScore = 9999.0;
            foreach (var move in allMoves)
            {
                board.MakeMove(move);
                bestMoveScore = Math.Min(bestMoveScore, Minimax(board, depth - 1, alpha, beta, !isMaximizingPlayer, depthReserve));
                board.UndoMove(move);
                beta = Math.Min(beta, bestMoveScore);
                if (beta <= alpha)
                {
                    return bestMoveScore;
                }
            }
            return bestMoveScore;
        }
    }

    private double GetMoveValue(Move move)
    {
        var startIndex = move.StartSquare.Rank * 4 + GetFileIndex(move.StartSquare.File);
        var targetIndex = move.TargetSquare.Rank * 4 + GetFileIndex(move.TargetSquare.File);
        return _center[startIndex] + _center[targetIndex];
    }

    private double GetPieceValue(Piece piece)
    {
        var value = _pieceValues[(int)piece.PieceType] + GetPiecePlacementValue(piece);
        return piece.IsWhite == _isPlayingWhite ? value : -value;
    }

    private double GetPiecePlacementValue(Piece piece)
    {
        var code = piece.PieceType switch
        {
            PieceType.Pawn => _pawn,
            PieceType.Knight => _knight,
            PieceType.Bishop => _bishop,
            PieceType.Rook => _rook,
            PieceType.Queen => _queen,
            PieceType.King => _king,
            _ => null
        };

        var codeEnd = piece.PieceType switch
        {
            PieceType.Pawn => _pawnEnd,
            PieceType.King => _kingEnd,
            _ => null
        };

        var file = GetFileIndex(piece.Square.File);
        var rank = piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank;
        var index = rank * 4 + file;
        var valStart = code == null ? 75.0 : code[index];
        var valEnd = codeEnd == null ? valStart : codeEnd[index];

        return ((valStart * (1 - _coeff)) + valEnd * _coeff - 75) / 2.0;
    }

    private int GetFileIndex(int file)
    {
        return file < 4 ? file : 3 - (file % 4);
    }

    private double GetBoardValue(Board board)
    {
        var value = 0.0;

        for (int i = 0; i < 64; i++)
        {
            value += GetPieceValue(board.GetPiece(new Square(i)));
        }

        return value;
    }
}