namespace auto_Bot_11;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_11 : IChessBot
{
    Random r = new Random();

    // Deviously lick this code from EvilBot
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Modify the licked code for checks
    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    void AddBestMove(SortedDictionary<double, Move> bestMoves, double eval, Move move)
    {
        while (!bestMoves.TryAdd(eval, move))
        {
            eval += 0.00001;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        SortedDictionary<double, Move> bestMoves = new SortedDictionary<double, Move>();

        foreach (Move move in moves)
        {
            double eval = 0;
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            if (MoveIsCheck(board, move))
            {
                eval += ((double)move.MovePieceType) + 10 * 10;
            }
            if (move.IsCapture)
            {
                eval += Math.Max(move.CapturePieceType + 1 - move.MovePieceType, 0);
            }
            eval += ((-Math.Sqrt(Math.Pow(move.TargetSquare.File - 3.5, 2) + Math.Pow(move.TargetSquare.Rank - 3.5, 2))) + 2.64575131106) * ((double)move.MovePieceType);

            AddBestMove(bestMoves, eval, move);
        }

        if (bestMoves.Count >= 3)
        {
            IEnumerable<KeyValuePair<double, Move>> top3 = bestMoves.Reverse().Take(3);

            double rng = r.NextDouble() * top3.Sum(x => x.Key);

            return top3.SkipWhile(x => { if (rng > x.Key) { rng -= x.Key; return true; } return false; }).First().Value;
        }
        return bestMoves.Reverse().First().Value;
    }
}