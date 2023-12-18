namespace auto_Bot_43;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_43 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        /* Strategy: Prioritize not getting captured, look 6 moves ahead.
         * If all moves lead to a capture, choose the move that leads to the fewest captures in the next move.
         */

        Move[] moves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;
        int minCapture = int.MaxValue;
        int lookAhead = 6;

        // Randomize move order to minimize repetition
        Random rng = new();
        moves = moves.OrderBy(a => rng.Next()).ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int depth = (timer.MillisecondsRemaining < 10.0) ? 1 : lookAhead;
            if (!LeadsToCapture(board, depth))
            {
                bestMove = move;
                board.UndoMove(move);
                break;
            }
            board.UndoMove(move);
        }

        if (bestMove.IsNull)
        {
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int numCapture = 0;
                foreach (Move oppMove in board.GetLegalMoves())
                {
                    if (oppMove.IsCapture)
                        numCapture++;
                }
                if (numCapture < minCapture)
                {
                    minCapture = numCapture;
                    bestMove = move;
                }
                board.UndoMove(move);
            }
        }

        return bestMove;
    }

    bool LeadsToCapture(Board board, int depth)
    {
        if (depth == 0)
            return false;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            bool leadsToCapture = move.IsCapture || LeadsToCapture(board, depth - 1);
            board.UndoMove(move);
            if (leadsToCapture)
                return true;
        }

        return false;
    }
}