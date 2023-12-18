namespace auto_Bot_500;
using ChessChallenge.API;
using System;
using static System.Math;

/// <summary>
/// This chess bot was created as participation in the "Tiny chess bot challenge" by Sebastian Lague. It uses the API of the given Project that already implements all features of chess.
/// This bot implements the following features (due to the restrictions of tokens in this challenge not all features can be implemented):
/// For search:
/// - Alpha-Beta Pruning
/// - Move Ordering - after capture values, promotion
/// - Transposition Tables
/// - Iterative deepening
/// For evaluation:
/// - Material difference
/// - Checkmate, Check or Draw
/// - King and pawns for endgame
/// </summary>
public class Bot_500 : IChessBot
{
    //settings
    private const int Infinity = 99999999;
    private int _timeLimit;

    //mandatory variables
    private Move _bestMoveThisIteration;
    private Move _bestMoveSoFar;
    private readonly int[] _pieceValues = { 0, 100, 320, 330, 500, 900, 20_000 };
    private int[] _moveValues;


    private TranspositionTableEntry?[] _transpositionTable;


    public Move Think(Board board, Timer timer)
    {
        _moveValues = new int[218];
        _transpositionTable = new TranspositionTableEntry[10_000];

        //set the time limit for the search
        var timeRemaining = timer.MillisecondsRemaining;
        var increment = timer.IncrementMilliseconds;

        var thinkTime = timeRemaining / 40.0;
        if (timeRemaining > increment * 2) thinkTime += increment * 0.8;
        var minThinkTime = Min(50, timeRemaining * 0.25);

        _timeLimit = (int)Ceiling(Max(minThinkTime, thinkTime));


        IterativeDeepening(board, timer);

        return _bestMoveSoFar;
    }

    private void IterativeDeepening(Board board, Timer timer)
    {
        for (int i = 1; i < 21; i++)
        {
            Search(board, i, -Infinity, Infinity, 0, timer);

            _bestMoveSoFar = _bestMoveThisIteration;
            _bestMoveThisIteration = Move.NullMove;

            if (timer.MillisecondsElapsedThisTurn >= _timeLimit)
                break;

        }

    }

    private int Search(Board board, int depth, int alpha, int beta, int plyFromRoot, Timer timer)
    {
        if (timer.MillisecondsElapsedThisTurn >= _timeLimit) return 0;
        if (depth == 0) return Evaluate(board);//QuiescenceSearch(alpha, beta, board);

        var zobrisKey = board.ZobristKey;
        var originalAlpha = alpha;
        if (_transpositionTable[zobrisKey % 10_000]?.Key == zobrisKey && _transpositionTable[zobrisKey % 10_000]?.Depth >= depth)
        {
            var entry = _transpositionTable[zobrisKey % 10_000];
            if (entry.Flag == 0) return entry.Value;
            if (entry.Flag == 2) alpha = Max(alpha, entry.Value);
            else if (entry.Flag == 1) beta = Min(beta, entry.Value);
        }

        if (alpha >= beta) return _transpositionTable[zobrisKey % 10_000]?.Value ?? 0;

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        moves = OrderMoves(moves, board);

        if (moves.Length == 0 || board.IsDraw()) return board.IsInCheck() ? -9999999 : 0;

        foreach (var move in moves)
        {
            board.MakeMove(move);
            var evaluation = -Search(board, depth - 1, -beta, -alpha, plyFromRoot + 1, timer);
            board.UndoMove(move);

            if (evaluation >= beta)
            {
                _transpositionTable[zobrisKey % 10_000] = new TranspositionTableEntry { Key = zobrisKey, Depth = (byte)depth, Flag = 2, Value = alpha };
                return beta;
            }

            if (evaluation > alpha)
            {
                alpha = evaluation;
                if (plyFromRoot == 0) _bestMoveThisIteration = move;
            }
        }

        var storeFlag = alpha <= originalAlpha ? 1 : alpha >= beta ? 2 : 0;
        _transpositionTable[zobrisKey % 10_000] = new TranspositionTableEntry { Key = zobrisKey, Depth = (byte)depth, Flag = (byte)storeFlag, Value = alpha };
        return alpha;
    }


    /// Sorts the moves depending on how good they are, which makes alpha beta pruning a lot more efficient.
    /// This also has effects on how the bot plays. Eg. checkmate although it does not get evaluated that this is good.
    /// Downside: Consumes a lot of brain capacity.
    private Span<Move> OrderMoves(Span<Move> moves, Board board)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            var movePieceType = board.GetPiece(move.TargetSquare).PieceType;
            var capturePieceType = board.GetPiece(move.StartSquare).PieceType;
            int score = 0;

            if (move.Equals(_bestMoveSoFar)) score += Infinity;

            if (move.IsCastles) score += 50;
            if (movePieceType == PieceType.King && board.PlyCount < 15) score -= 50;

            if (move.IsCapture && capturePieceType != PieceType.King)
            {
                var delta = _pieceValues[(int)movePieceType] - _pieceValues[(int)capturePieceType];
                score += board.SquareIsAttackedByOpponent(move.TargetSquare) ? (delta >= 0 ? 10000 : 2000) + delta : 10000 + delta;
            }

            if (movePieceType == PieceType.Pawn && move.IsPromotion && !move.IsCapture) score += 6000;

            _moveValues[i] = score;
        }

        for (int i = 0; i < moves.Length - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (_moveValues[swapIndex] < _moveValues[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (_moveValues[j], _moveValues[swapIndex]) = (_moveValues[swapIndex], _moveValues[j]);
                }
            }
        }

        return moves;
    }


    private int Evaluate(Board board)
    {

        var perspective = board.IsWhiteToMove ? 1 : -1;
        int whiteValue = 0, blackValue = 0, value = 0;

        var ply = board.PlyCount;
        var isEndgame = ply > 20;

        if (board.IsInCheckmate())
            return Infinity;

        if (board.IsDraw())
            return 0;

        PieceList[] pieceLists = board.GetAllPieceLists();
        Piece blackKing = pieceLists[11][0], whiteKing = pieceLists[5][0];

        for (int i = 0; i < pieceLists.Length; i++)
        {
            foreach (var piece in pieceLists[i])
            {
                var rank = piece.Square.Rank;
                switch (i)
                {
                    case 0:
                        //white pawn
                        whiteValue += 100;
                        //pawns should promote in endgame
                        whiteValue += isEndgame ? rank : 0;
                        break;
                    case 1:
                        //white knight
                        whiteValue += 320;
                        whiteValue += rank == 0 ? -10 : 0;
                        break;
                    case 2:
                        //white bishop
                        whiteValue += 330;
                        whiteValue += rank == 0 ? -10 : 0;
                        break;
                    case 3:
                        //white rook
                        whiteValue += 500;
                        break;
                    case 4:
                        //white queen
                        whiteValue += 1100;
                        break;
                    case 6:
                        //black pawn
                        blackValue += 100;
                        blackValue += isEndgame ? 7 - rank : 0;
                        break;
                    case 7:
                        //black knight
                        blackValue += 320;
                        blackValue += rank == 7 ? -10 : 0;
                        break;
                    case 8:
                        //black bishop
                        blackValue += 330;
                        blackValue += rank == 7 ? -10 : 0;
                        break;
                    case 9:
                        //black rook
                        blackValue += 500;
                        break;
                    case 10:
                        //black queen
                        blackValue += 1100;
                        break;
                }
            }
        }

        //in the engame the kings should move towards each other to 
        var distance = Abs(whiteKing.Square.File - blackKing.Square.File) +
                       Abs(whiteKing.Square.Rank - blackKing.Square.Rank);

        value += isEndgame ? (14 - distance) * 5 : 0;
        return perspective * (whiteValue - blackValue) + value;
    }
}

public class TranspositionTableEntry
{
    public ulong Key;
    public int Value;
    public byte Depth;
    public byte Flag;
}