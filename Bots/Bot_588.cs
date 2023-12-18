namespace auto_Bot_588;
using ChessChallenge.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

public class Bot_588 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        double negInf = -900;
        double posInf = 900;
        Random rng = new();


        int[,] coolmatrix = new int[64, 64];

        //Dictionary<ulong, double> hashtable = new Dictionary<ulong, double>();
        OrderedDictionary hashtable = new OrderedDictionary();

        ICollection keyCollection = hashtable.Keys;
        ICollection valueCollection = hashtable.Values;
        return StartSearch();



        Move StartSearch()
        {
            Move[] moves = board.GetLegalMoves();
            Dictionary<Move, double> moveScore = new Dictionary<Move, double>();
            Move best = moves[0];
            for (int i = 0; i <= 32; i++)
            {
                double besteval = negInf;
                foreach (Move move in moves)
                {
                    board.MakeMove(move);
                    double r = -search(i, beta: -besteval);
                    board.UndoMove(move);

                    if (r > besteval)
                    {
                        besteval = r;
                        best = move;
                    }
                    moveScore[move] = r;
                }
                foreach (KeyValuePair<Move, double> kvp in moveScore)
                {
                }
                Array.Sort(moves, (x, y) => moveScore[y].CompareTo(moveScore[x]));
            }
            return best;
        }

        double quiesce(double alpha = -900, double beta = 900)
        { //This is making everything too slow. I need to come up with optimizations.
            ulong key = board.ZobristKey;

            if (hashtable.Contains(key)) return (double)hashtable[key];

            double base_eval = -evaluate();
            if (base_eval >= beta) return beta;
            if (base_eval > alpha) alpha = base_eval;

            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return negInf; //I'm in checkmate so that must be bad

            Move[] moves = board.GetLegalMoves(!board.IsInCheck());
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                double r = -quiesce(-beta, -alpha);
                board.UndoMove(move);
                if (r >= beta) return beta;
                if (r > alpha) alpha = r;
            }
            return alpha;
        }

        double search(int depth, double alpha = -900, double beta = 900)
        {
            ulong key = board.ZobristKey;

            if (hashtable.Contains(key)) return (double)hashtable[key];

            if (depth == 0)
            {
                double val = quiesce();
                addtohashtable(key, val); ; //The evaluation function is pretty much another depth 1 search, so this is actualy good.
                return val;
            }

            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return negInf; //I'm in checkmate so that must be bad

            Move[] moves = board.GetLegalMoves();
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                double r = -search(depth - 1, -beta, -alpha);
                board.UndoMove(move);
                if (r >= beta)
                {
                    addtohashtable(key, beta);
                    return beta;
                }
                if (r > alpha) alpha = r;
            }
            addtohashtable(key, alpha);
            return alpha;
        }

        double evaluate()
        {
            if (board.IsInCheckmate())
            {
                //It's a win, pl_eval should go through the roof.
                return posInf;
            }
            if (board.IsDraw())
            {
                //It's a draw, pl_eval and op_eval should be equal.
                return 0;
            }

            int op = board.GetLegalMoves().Length;
            board.ForceSkipTurn();
            int pl = board.GetLegalMoves().Length;
            board.UndoSkipTurn();

            double op_eval = (op + .5) / (1 + pl);
            double pl_eval = pl / (1 + op);

            return (pl_eval - op_eval);
        }
        //DivertedConsole.Write(Convert.ToString((int)BitboardHelper.GetKnightAttacks(new Square("e2")), 2).PadLeft(64, '0'));

        void addtohashtable(ulong key, double value)
        {
            // 64 bit ulong key
            // 64 bit double value
            // 200 mb = 1000x1000x200 = 200.000.000 bits
            // 200.000.000 / 128 = 1562500
            if (hashtable.Count >= 1562500) hashtable.RemoveAt(rng.Next(1562500)); //I don't need accurate cache, I just need something good enough.
            hashtable.Add(key, value);
        }
    }
}