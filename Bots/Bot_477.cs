namespace auto_Bot_477;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_477 : IChessBot
{
    Dictionary<string, (int, double)> lookup1 = new Dictionary<string, (int, double)>();
    public Move Think(Board board, Timer timer)
    {
        int minTime = timer.GameStartTimeMilliseconds / 600;
        double Evaluate(Move move)
        {
            double eval = 0;
            int turn = board.IsWhiteToMove ? 1 : -1;
            int bef = board.GetLegalMoves().Count();
            if (move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion)
            {
                eval += 4 * turn;
            }
            board.MakeMove(move);
            int aft = board.GetLegalMoves().Count();
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return turn * 10000;
            }
            if (board.IsDraw())
            {
                board.UndoMove(move);
                return 0;
            }
            if (lookup1.ContainsKey(board.GetFenString()))
            {
                string temp = board.GetFenString();
                board.UndoMove(move);
                return lookup1[temp].Item2;
            }
            PieceList[] allPieces = board.GetAllPieceLists();
            eval += (allPieces[0].Count - allPieces[6].Count) * 100 + (allPieces[1].Count + allPieces[2].Count - allPieces[7].Count - allPieces[8].Count) * 300 + (allPieces[3].Count - allPieces[9].Count) * 500 + (allPieces[4].Count - allPieces[10].Count) * 900;
            ulong wCov = 0; //Calculate how many squares white covers(from Fog of War chess variant)
            ulong bCov = 0;
            ulong wProt = 0; // Calculate how may pieces white protects
            ulong bProt = 0;
            foreach (PieceList piece in allPieces)
            {
                for (int i = 0; i < piece.Count; i++)
                {
                    Piece curr = piece.GetPiece(i);
                    ulong boardAtt = BitboardHelper.GetPieceAttacks(curr.PieceType, curr.Square, board, curr.IsWhite);
                    ulong noBlock = BitboardHelper.GetPieceAttacks(curr.PieceType, curr.Square, 0, curr.IsWhite);
                    if (curr.IsWhite)
                    {
                        wCov |= boardAtt;
                        wProt |= noBlock & board.WhitePiecesBitboard;
                    }
                    else
                    {
                        bCov |= boardAtt;
                        bProt |= noBlock & board.BlackPiecesBitboard;
                    }
                }
            }
            eval += (bef - aft) * turn + (BitboardHelper.GetNumberOfSetBits(wProt) + BitboardHelper.GetNumberOfSetBits(wCov) - (BitboardHelper.GetNumberOfSetBits(bProt) + BitboardHelper.GetNumberOfSetBits(bCov))) / 2;
            board.UndoMove(move);
            return eval;
        }
        double search(Move move, int depth, int maxDepth, double alpha, double beta)
        {
            if (depth == 0 || maxDepth == 0 || timer.MillisecondsElapsedThisTurn > minTime)
            {
                return Evaluate(move);
            }
            board.MakeMove(move);
            if (lookup1.ContainsKey(board.GetFenString()))
            {
                (int lDepth, double lEval) = lookup1[board.GetFenString()];
                if (lDepth >= depth)
                {
                    board.UndoMove(move);
                    return lEval;
                }
            }
            double eval = board.IsWhiteToMove ? double.MinValue : double.MaxValue;
            Move[] moves = board.GetLegalMoves();
            foreach (Move action in moves)
            {
                if (action.IsCapture || action.IsCastles || action.IsEnPassant || action.IsPromotion)
                {
                    depth += 1;
                }
                if (board.IsWhiteToMove)
                {
                    eval = Math.Max(eval, search(action, depth - 1, maxDepth - 1, alpha, beta));
                    if (eval > beta)
                    {
                        break;
                    }
                    alpha = Math.Max(alpha, eval);
                }
                else
                {
                    eval = Math.Min(eval, search(action, depth - 1, maxDepth - 1, alpha, beta));
                    if (eval < alpha)
                    {
                        break;
                    }
                    beta = Math.Min(beta, eval);
                }
            }
            if (lookup1.Count >= Math.Pow(2, 20))
            {
                lookup1.Clear();
            }
            lookup1[board.GetFenString()] = (depth, eval);
            board.UndoMove(move);
            return eval;
        }
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int turn = board.IsWhiteToMove ? 1 : -1;
        double searchResult = 0;
        for (int i = 1; i < int.MaxValue; i += 2)
        {
            double alpha = double.MinValue;
            double beta = double.MaxValue;
            double bestEval = double.MinValue;
            if (board.IsWhiteToMove)
            {
                moves = moves.OrderByDescending(m => Evaluate(m)).ToArray();
            }
            else
            {
                moves = moves.OrderBy(m => Evaluate(m)).ToArray();
            }
            foreach (Move move in moves)
            {
                searchResult = turn * search(move, i, i + 2, alpha, beta);
                if (bestEval < searchResult)
                {
                    bestEval = searchResult;
                    bestMove = move;
                }
                if (board.IsWhiteToMove)
                {
                    alpha = Math.Max(alpha, searchResult);
                }
                else
                {
                    beta = Math.Min(beta, searchResult);
                }
            }
            if (timer.MillisecondsElapsedThisTurn >= minTime)
            {
                break;
            }
        }
        return bestMove;
    }
}