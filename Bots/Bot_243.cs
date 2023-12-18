namespace auto_Bot_243;
using ChessChallenge.API;
using System;

public class Bot_243 : IChessBot
{
    public double evaluate(Board board, int factor)
    {
        if (board.IsInCheckmate()) return -factor * Double.MaxValue;
        else if (board.IsDraw()) return 0;

        PieceList[] piecelists = board.GetAllPieceLists();

        return (piecelists[0].Count - piecelists[6].Count)
        + 3 * (piecelists[1].Count - piecelists[7].Count)
        + 3.5 * (piecelists[2].Count - piecelists[8].Count)
        + 5 * (piecelists[3].Count - piecelists[9].Count)
        + 9 * (piecelists[4].Count - piecelists[10].Count);
    }

    public (double, Move) minimax(Board board, int depth, double alpha, double beta, int factor)
    {
        Move[] moves = board.GetLegalMoves();
        if (depth == 0 || moves.Length == 0) return (evaluate(board, factor), Move.NullMove);

        int n = moves.Length;
        Random rng = new();
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Move value = moves[k];
            moves[k] = moves[n];
            moves[n] = value;
        }

        double bestEval = -factor * Double.MaxValue;
        Move bestMove = moves[0];

        foreach (bool capture in new[] { true, false })
        {
            foreach (PieceType piece in new[] { 2, 3, 5, 1, 4, 6 })
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    if (moves[i].IsCapture == capture)
                    {
                        if (moves[i].MovePieceType == piece)
                        {
                            board.MakeMove(moves[i]);
                            (double eval, _) = minimax(board, depth - 1 + Convert.ToInt32(board.IsInCheck()), alpha, beta, -factor);
                            board.UndoMove(moves[i]);

                            if (factor * bestEval < factor * eval)
                            {
                                bestEval = eval;
                                bestMove = moves[i];
                            }

                            if (factor == 1) alpha = Math.Max(alpha, eval);
                            if (factor == -1) beta = Math.Min(beta, eval);
                            if (beta <= alpha) break;
                        }
                    }
                }
            }
        }
        return (bestEval, bestMove);
    }

    public Move Think(Board board, Timer timer)
    {
        return minimax(board, 5 - 3 * Convert.ToInt32(timer.MillisecondsRemaining < 5000), -Double.MaxValue, +Double.MaxValue, 2 * Convert.ToInt32(board.IsWhiteToMove) - 1).Item2;
    }
}