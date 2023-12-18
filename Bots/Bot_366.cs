namespace auto_Bot_366;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_366 : IChessBot
{
    public LinkedList<Move> BestMoves = new LinkedList<Move>();

    public int NumSearchMoves = 5000000;
    public short MinDepth = 4;
    public Move Think(Board board, Timer timer)
    {
        //Debug.WriteLine(MinMax(board, 1, true, board.IsWhiteToMove ? -1000000 : 1000000, 0));
        return BestMoves.ElementAt(new Random().Next(BestMoves.Count));
    }
    public float MinMax(Board board, int NumEstimatedMoves, bool flag, float alpha, short depth)
    {
        depth++;
        if (flag)
        {
            BestMoves.Clear();
            BestMoves.AddLast(board.GetLegalMoves()[0]);
        }
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -1000000 : 1000000;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        NumEstimatedMoves *= board.GetLegalMoves().Length;
        if (NumEstimatedMoves > NumSearchMoves && depth > MinDepth)
        {
            return Heuristic(board);
        }
        else
        {
            Move[] moves = board.GetLegalMoves();
            float bestHeuristic = 1000000 * (board.IsWhiteToMove ? -1 : 1);
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                float heuristic = MinMax(board, NumEstimatedMoves, false, bestHeuristic, depth);
                board.UndoMove(move);
                int negate = board.IsWhiteToMove ? 1 : -1;
                if (negate * heuristic > negate * bestHeuristic)
                {
                    if (flag)
                    {
                        BestMoves.Clear();
                        BestMoves.AddLast(move);
                    }
                    if (heuristic == negate * 1000000)
                    {
                        return negate * 1000000;
                    }
                    bestHeuristic = heuristic;
                    if (negate * bestHeuristic > negate * alpha && !flag)
                    {
                        return bestHeuristic;
                    }
                }
                else if (flag)
                {
                    if (heuristic == bestHeuristic)
                    {
                        BestMoves.AddLast(move);
                    }
                }
            }
            return bestHeuristic;
        }
    }

    public float Heuristic(Board board)
    {
        float heuristic = 0;
        PieceList[] pieces = board.GetAllPieceLists();
        for (int i = 0; i < pieces.Length; i++)
        {
            float x = pieces[i].IsWhitePieceList ? 1 : -1;
            switch ((int)pieces[i].TypeOfPieceInList)
            {
                case 1:
                    heuristic += x * 100 * pieces[i].Count;
                    break;
                case 2:
                    heuristic += x * 300 * pieces[i].Count;
                    break;
                case 3:
                    heuristic += x * 300 * pieces[i].Count;
                    break;
                case 4:
                    heuristic += x * 500 * pieces[i].Count;
                    break;
                case 5:
                    heuristic += x * 900 * pieces[i].Count;
                    break;
            }
        }
        int negate = board.IsWhiteToMove ? 1 : -1;
        heuristic += (negate * board.GetLegalMoves().Length) / 4;
        board.ForceSkipTurn();
        heuristic -= (negate * board.GetLegalMoves().Length) / 4;
        board.UndoSkipTurn();
        return heuristic;
    }
}