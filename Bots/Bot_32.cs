namespace auto_Bot_32;
using ChessChallenge.API;
using System;

public class Bot_32 : IChessBot
{
    private const int MaxDepth = 3;
    private Random random = new Random();

    public Move Think(Board board, Timer timer)
    {
        Move? bestMove = null;
        int bestScore = int.MinValue;

        foreach (Move move in board.GetLegalMoves())
        {
            int score = EvaluateMove(board, move, MaxDepth - 1, int.MinValue, int.MaxValue, false);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove ?? throw new Exception("No legal moves found!");
    }

    private int EvaluateMove(Board board, Move move, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        board.MakeMove(move);

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            int score = EvaluateBoard(board);
            board.UndoMove(move);
            return score;
        }

        int bestScore = maximizingPlayer ? int.MinValue : int.MaxValue;
        foreach (Move nextMove in board.GetLegalMoves())
        {
            int score = EvaluateMove(board, nextMove, depth - 1, alpha, beta, !maximizingPlayer);

            if (maximizingPlayer)
            {
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, score);
            }
            else
            {
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, score);
            }

            if (beta <= alpha)
                break;
        }

        board.UndoMove(move);
        return bestScore;
    }

    private int EvaluateBoard(Board board)
    {
        return random.Next(-1000, 1000);
    }
}
//The best that i could come up with (: