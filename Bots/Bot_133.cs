namespace auto_Bot_133;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_133 : IChessBot
{
    public int[] pieceValues = new int[] { 100, 300, 300, 500, 900 };
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Random rand = new();
        Move bestMove = moves[rand.Next(0, moves.Count())];
        int bestEval = -99999;
        foreach (Move move in moves)
        {
            if (isCheckmate(board, move))
            {
                bestMove = move;
                break;
            }
            board.MakeMove(move);
            int eval = -Search(board, 2, -999999, 999999);
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return bestMove;
    }
    public bool isCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return true;
        }
        else
        {
            board.UndoMove(move);
            return false;
        }
    }
    public int Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            return Evaluate(board);
        }
        Move[] moves = moveOrder(board, board.GetLegalMoves());
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);
            if (eval >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, eval);
        }
        return alpha;
    }
    public int Evaluate(Board board)
    {
        int whiteEval = CountMaterial(board, true);
        int blackEval = CountMaterial(board, false);
        int perspective = (board.IsWhiteToMove) ? 1 : -1;
        int eval = whiteEval - blackEval;
        return eval * perspective;
    }
    public int CountMaterial(Board board, bool countWhite)
    {
        int countMaterial = 0;
        countMaterial += board.GetPieceList(PieceType.Pawn, countWhite).Count * pieceValues[0];
        countMaterial += board.GetPieceList(PieceType.Knight, countWhite).Count * pieceValues[1];
        countMaterial += board.GetPieceList(PieceType.Bishop, countWhite).Count * pieceValues[2];
        countMaterial += board.GetPieceList(PieceType.Rook, countWhite).Count * pieceValues[3];
        countMaterial += board.GetPieceList(PieceType.Queen, countWhite).Count * pieceValues[4];
        return countMaterial;
    }
    public Move[] moveOrder(Board board, Move[] moves)
    {
        int[] moveScores = new int[moves.Count()];
        foreach (Move move in moves)
        {
            int moveScore = 0;
            PieceType movePieceType = move.MovePieceType;
            Square movePieceTarget = move.TargetSquare;
            if (board.SquareIsAttackedByOpponent(movePieceTarget))
            {
                moveScore -= pieceValues[convertTypeToIndex(movePieceType)];
            }
            if (move.IsCapture)
            {
                moveScore += pieceValues[convertTypeToIndex(move.CapturePieceType)];
            }
            moveScores.Append(moveScore);
        }
        moves = Sort(moves, moveScores);
        return moves;
    }
    public int convertTypeToIndex(PieceType type)
    {
        switch (type)
        {
            case PieceType.Pawn:
                return 0;
            case PieceType.Knight:
                return 1;
            case PieceType.Bishop:
                return 2;
            case PieceType.Rook:
                return 3;
            case PieceType.Queen:
                return 4;
            default:
                return -1;
        }
    }
    Move[] Sort(Move[] moves, int[] moveScores)
    {
        // Sort the moves list based on scores
        for (int i = 0; i < moves.Count() - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
        return moves;
    }
}