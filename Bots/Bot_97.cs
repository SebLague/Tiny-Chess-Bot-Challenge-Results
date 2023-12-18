namespace auto_Bot_97;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_97 : IChessBot
{
    private readonly double PawnValue = 1;
    private readonly double KnightValue = 3;
    private readonly double BishopValue = 3.1;
    private readonly double RookValue = 5;
    private readonly double QueenValue = 9;

    private double getPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.Pawn:
                return PawnValue;
            case PieceType.Knight:
                return KnightValue;
            case PieceType.Bishop:
                return BishopValue;
            case PieceType.Rook:
                return RookValue;
            case PieceType.Queen:
                return QueenValue;
            default:
                return 0;
        }
    }

    private bool isWorthCapturing(Move move)
    {
        PieceType targetPieceType = move.CapturePieceType;
        PieceType capturePieceType = move.MovePieceType;
        if (getPieceValue(targetPieceType) - getPieceValue(capturePieceType) >= 0)
            return true;
        return false;
    }

    private bool isMoveSafe(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] allCaptureMoves = board.GetLegalMoves(true);
        foreach (Move captureMove in allCaptureMoves)
        {
            if (captureMove.TargetSquare == move.TargetSquare)
            {
                board.UndoMove(move);
                return false;
            }
        }
        board.UndoMove(move);
        return true;
    }

    private bool isDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMoveDraw = board.IsDraw();
        board.UndoMove(move);
        return isMoveDraw;
    }

    private Move SelectBestMove(Board board, Move[] moves)
    {
        List<Move> nonDrawMoves = moves.Where(move => !isDraw(board, move)).ToList();
        if (nonDrawMoves.Count > 0)
        {
            // Filter out draw moves and select the best non-draw move
            Move bestMove = nonDrawMoves.OrderByDescending(move => getPieceValue(move.CapturePieceType)).First();
            return bestMove;
        }

        // If all available moves are draws, choose a random move
        System.Random rng = new System.Random();
        return moves[rng.Next(moves.Length)];
    }

    public Move Think(Board board, Timer timer)
    {
        string boardFen = board.GetFenString();
        Move[] moves = board.GetLegalMoves();
        Move[] allCaptureMoves = board.GetLegalMoves(true);

        if (allCaptureMoves.Length > 0)
        {
            foreach (Move move in allCaptureMoves)
            {
                if (move.IsEnPassant)
                    return move;
            }

            // Select the best capture move
            Move bestCaptureMove = allCaptureMoves.OrderByDescending(move => getPieceValue(move.CapturePieceType)).First();
            if (isMoveSafe(board, bestCaptureMove) || isWorthCapturing(bestCaptureMove))
            {
                return bestCaptureMove;
            }
        }
        else
        {
            // Select the best non-capture move
            return SelectBestMove(board, moves);
        }

        // If no move is found, return a random move as a fallback
        System.Random rng = new System.Random();
        return moves[rng.Next(moves.Length)];
    }
}
