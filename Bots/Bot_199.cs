namespace auto_Bot_199;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_199 : IChessBot
{
    private int depth = 4;
    private readonly int[] worth = new int[] { 0, 100, 300, 320, 500, 900, 10000 };
    private readonly float[,] table = new float[,]
    {
        { 0, -5, -2, 0, -2, 2 },
        { 0, -4, -1, 0, -1, 3 },
        { 0, -3, -1, 0, -1, 1 },
        { 0, -3, -1, 0.5f, -0.5f, 0 },
        { 0, -3, -1, 0.5f, -0.5f, 0 },
        { 0, -3, -1, 0, -1, 1 },
        { 0, -4, -1, 0, -1, 3 },
        { 0, -5, -2, 0, -2, 2 },
        { 0.5f, -4, -1, -0.5f, -1, 2 },
        { 1, -2, 0.5f, 0, 0, 2 },
        { 1, 0, 0, 0, 0.5f, 0 },
        { -2, 0.5f, 0, 0, 0, 0 },
        { -2, 0.5f, 0, 0, 0, 0 },
        { 1, 0, 0, 0, 0, 0 },
        { 1, -2, 0.5f, 0, 0, 2 },
        { 0.5f, -4, -1, -0.5f, -1, 2 },
        { 0.5f, -3, -1, -0.5f, -1, -1 },
        { -0.5f, 0.5f, 1, 0, 0.5f, -2 },
        { -1, 1, 1, 0, 0.5f, -2 },
        { 0, 1.5f, 1, 0, 0.5f, -2 },
        { 0, 1.5f, 1, 0, 0.5f, -2 },
        { -1, 1, 1, 0, 0.5f, -2 },
        { -0.5f, 0.5f, 1, 0, 0, -2 },
        { 0.5f, -3, -1, -0.5f, -1, -1 },
        { 0, -3, -1, -0.5f, 0, -2 },
        { 0, 0, 0, 0, 0, -3 },
        { 0, 1.5f, 1, 0, 0.5f, -3 },
        { 2, 2, 1, 0, 0.5f, -4 },
        { 2, 2, 1, 0, 0.5f, -4 },
        { 0, 1.5f, 1, 0, 0.5f, -3 },
        { 0, 0, 0, 0, 0, -3 },
        { 0, -3, -1, -0.5f, -0.5f, -2 },
        { 0.5f, -3, -1, -0.5f, -0.5f, -3 },
        { 0.5f, 0.5f, 0.5f, 0, 0, -4 },
        { 1, 1.5f, 0.5f, 0, 0.5f, -4 },
        { 2.5f, 2, 1, 0, 0.5f, -5 },
        { 2.5f, 2, 1, 0, 0.5f, -5 },
        { 1, 1.5f, 0.5f, 0, 0.5f, -4 },
        { 0.5f, 0.5f, 0.5f, 0, 0, -4 },
        { 0.5f, -3, -1, -0.5f, -0.5f, -3 },
        { 1, -3, -1, -0.5f, -1, -3 },
        { 1, 0, 0, 0, 0, -4 },
        { 2, 1, 0.5f, 0, 0.5f, -4 },
        { 3, 1.5f, 1, 0, 0.5f, -5 },
        { 3, 1.5f, 1, 0, 0.5f, -5 },
        { 2, 1, 0.5f, 0, 0.5f, -4 },
        { 1, 0, 0, 0, 0, -4 },
        { 1, -3, -1, -0.5f, -1, -3 },
        { 5, -4, -1, 0.5f, -1, -3 },
        { 5, -2, 0, 1, 0, -4 },
        { 5, 0, 0, 1, 0, -4 },
        { 5, 0, 0, 1, 0, -5 },
        { 5, 0, 0, 1, 0, -5 },
        { 5, 0, 0, 1, 0, -4 },
        { 5, -2, 0, 1, 0, -4 },
        { 5, -4, -1, 0.5f, -1, -3 },
        { 0, -5, -2, 0, -2, -3 },
        { 0, -4, -1, 0, -1, -4 },
        { 0, -3, -1, 0, -1, -4 },
        { 0, -3, -1, 0, -0.5f, -5 },
        { 0, -3, -1, 0, -0.5f, -5 },
        { 0, -3, -1, 0, -1, -4 },
        { 0, -4, -1, 0, -1, -4 },
        { 0, -5, -2, 0, -2, -3 }
    };

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 5000) depth = 2;

        List<Move> possibleMoves = new();
        Minimax(board, depth, int.MinValue, int.MaxValue, board.IsWhiteToMove, possibleMoves);
        return possibleMoves[new Random().Next(possibleMoves.Count)];
    }

    private float Minimax(Board board, int depth, float alpha, float beta, bool maximizer, List<Move>? possibleMoves)
    {
        if (depth == 0) return Evaluation(board);

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0) return board.IsInCheckmate() ? (maximizer ? int.MinValue : int.MaxValue) : 0;

        float bestEval = maximizer ? int.MinValue : int.MaxValue;

        for (int move = 0; move < moves.Length; move++)
        {
            board.MakeMove(moves[move]);
            float eval = Minimax(board, depth - 1, alpha, beta, !maximizer, null);
            board.UndoMove(moves[move]);

            if (depth == this.depth)
            {
                if (maximizer && eval >= bestEval || !maximizer && eval <= bestEval)
                {
                    if (maximizer && eval > bestEval || !maximizer && eval < bestEval) possibleMoves.Clear();
                    possibleMoves.Add(moves[move]);
                }
            }

            bestEval = maximizer ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);

            if (maximizer)
            {
                if (bestEval > beta) break;
                alpha = Math.Max(alpha, bestEval);
            }
            else
            {
                if (bestEval < alpha) break;
                beta = Math.Min(beta, bestEval);
            }
        }

        return bestEval;
    }

    private float Evaluation(Board board)
    {
        float eval = 0;

        for (int square = 0; square < 64; square++)
        {
            PieceType piece = board.GetPiece(new Square(square)).PieceType;

            if (((board.GetPieceBitboard(piece, true) >> square) & 1) == 1)
            {
                eval += worth[(int)piece] + table[square, (int)piece - 1];
            }
            else if (((board.GetPieceBitboard(piece, false) >> square) & 1) == 1)
            {
                eval -= worth[(int)piece] + table[square, (int)piece - 1];
            }
        }

        return eval;
    }
}