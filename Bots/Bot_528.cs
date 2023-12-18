namespace auto_Bot_528;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_528 : IChessBot
{
    Dictionary<ulong, int> transpositionTable;
    Dictionary<ulong, int>? transpositionTable_prev;

    public Move Think(Board board, Timer timer)
    {
        // Compute time budget for this move (increases linearly until ply 20).

        int time_budget = timer.MillisecondsRemaining * 1 / 25;
        time_budget = time_budget * Math.Min(board.PlyCount, 20) / 20;

        Move? m3 = null;


        // Iterative deepending.

        for (int depth_incr = 4; depth_incr <= 20; depth_incr++)
        {
            transpositionTable = new Dictionary<ulong, int>();

            (m3, _) = DeepThink3(board, timer, depth_incr, -999999, 999999);

            transpositionTable_prev = transpositionTable;

            // Stop iterative deepening when computing the next level likely exceeds the time budget.

            if (8 * timer.MillisecondsElapsedThisTurn > time_budget)
            {
                // DivertedConsole.Write("move[{1}]: {0}   budget:{2}, elapsed:{3}", m3.ToString(), depth_incr, time_budget, timer.MillisecondsElapsedThisTurn);
                break;
            }
        }

        return m3 ?? new Move();
    }




    public (Move?, int) DeepThink3(Board board, Timer timer, int depth, int alpha, int beta)
    {
        ulong hash = board.ZobristKey + (ulong)board.PlyCount;
        int e;

        if (transpositionTable.TryGetValue(hash, out e))
        {
            // This board position has already been evaluated.
            return (null, e);
        }
        else
        {
            if (board.IsRepeatedPosition() || board.IsFiftyMoveDraw())
            {
                // Draw.
                return (null, 0);
            }
            else if (depth == 0 || board.IsInCheckmate())
            {
                // Terminal node -> compute evaluation function.
                e = eval(board);
                return (null, e);
            }
        }


        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0)
        {
            // Draw
            return (null, 0);
        }

        int crit = (board.PlyCount % 2) * -2 + 1; // == 0 ? 1 : -1;

        int[] moveeval = new int[moves.Length];

        Move[] sortedmoves = new Move[moves.Length];


        // --- Sort moves

        if (transpositionTable_prev != null)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                // Get evaluation from transposition table of previous deepening step.
                // Do captures first.

                board.MakeMove(moves[i]);
                int ev = 0;
                transpositionTable_prev.TryGetValue(board.ZobristKey + (ulong)board.PlyCount, out ev);

                moveeval[i] = ev + (moves[i].IsCapture ? 1000 * crit : 0);
                board.UndoMove(moves[i]);
            }


            // Selection sort.

            for (int i = 0; i < moves.Length; i++)
            {
                int bestM = -99999;
                int bestI = 0;
                for (int k = 0; k < moves.Length - i; k++)
                {
                    if (moveeval[k] * crit > bestM)
                    {
                        bestM = moveeval[k] * crit;
                        bestI = k;
                    }
                }

                sortedmoves[i] = moves[bestI];
                moves[bestI] = moves[moves.Length - 1 - i];
                moveeval[bestI] = moveeval[moves.Length - 1 - i];
            }

        }
        else
        {
            // If there is no previous evaluation available, simply do captures first.

            int idx = 0;
            foreach (Move m in moves)
            {
                if (m.IsCapture)
                {
                    sortedmoves[idx++] = m;
                }
            }
            foreach (Move m in moves)
            {
                if (!m.IsCapture)
                {
                    sortedmoves[idx++] = m;
                }
            }
        }


        // --- Alpha-Beta search

        Move best = new();

        if (crit > 0)
        {
            // MAXIMIZING

            int bestEval = -999999;

            foreach (Move m in sortedmoves)
            {
                board.MakeMove(m);
                (_, e) = DeepThink3(board, timer, depth - 1, alpha, beta);
                hash = board.ZobristKey + (ulong)board.PlyCount;
                transpositionTable[hash] = e;
                board.UndoMove(m);

                if (e > bestEval)
                {
                    bestEval = e;
                    best = m;

                    alpha = Math.Max(alpha, bestEval);

                    if (alpha >= beta)
                    {
                        break;
                    }
                }
            }

            return (best, bestEval);
        }
        else
        {
            // MINIMIZING

            int bestEval = 999999;

            foreach (Move m in sortedmoves)
            {
                board.MakeMove(m);
                (_, e) = DeepThink3(board, timer, depth - 1, alpha, beta);
                hash = board.ZobristKey + (ulong)board.PlyCount;
                transpositionTable[hash] = e;
                board.UndoMove(m);

                if (e < bestEval)
                {
                    bestEval = e;
                    best = m;

                    beta = Math.Min(beta, bestEval);

                    if (alpha >= beta)
                    {
                        break;
                    }
                }
            }

            return (best, bestEval);
        }
    }


    private int[] poseval_pawn2 = {
        0,0,0,0,0,0,2,0,
        5,5,5,5,5,5,7,5,
        10,10,10,10,10,10,12,10,
        15,15,15,25,35,15,-5,15
     };

    private int[] poseval_knight2 = {
        -50,3,6,9,9,6,3,-15,
        3,12,15,18,18,15,12,-12,
        6,15,21,27,24,21,15,-9,
        9,18,27,32,27,24,18,-6
    };

    private int[] poseval_bishop2 = {
        19,-14,17,18,18,17,16,9,
        16,23,20,21,21,20,23,6,
        17,20,26,23,23,26,20,7,
        18,21,23,28,28,23,21,8
    };

    public int pos_eval(PieceList pieces, int[] poseval2, bool mirror)
    {
        int eval = 0;
        for (int i = 0; i < pieces.Count; i++)
        {
            Square s = pieces.GetPiece(i).Square;
            eval += poseval2[(s.File >= 4 ? 7 - s.File : s.File) * 8 + (mirror ? s.Rank : 7 - s.Rank)];
        }

        return eval;
    }

    public int eval_side(PieceList[] pieces, bool white)
    {
        int offset = (white ? 0 : 6);

        int material = pieces[offset + 0].Count + 3 * pieces[offset + 1].Count + 3 * pieces[offset + 2].Count + 5 * pieces[offset + 3].Count + 9 * pieces[offset + 4].Count;

        int position = pos_eval(pieces[offset + 0], poseval_pawn2, !white) +
                       pos_eval(pieces[offset + 1], poseval_knight2, !white) +
                       pos_eval(pieces[offset + 2], poseval_bishop2, !white);

        return 500 * material + position;
    }

    public int eval(Board board)
    {
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? -99999 + board.PlyCount : 99999 - board.PlyCount;

        PieceList[] pieces = board.GetAllPieceLists();

        return eval_side(pieces, true) - eval_side(pieces, false);
    }
}
