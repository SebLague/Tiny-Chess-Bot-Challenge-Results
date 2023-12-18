namespace auto_Bot_298;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_298 : IChessBot
{

    public Dictionary<string, int> eve = new();
    public Dictionary<string, int> cck = new();
    public int width = 1;
    public int timerestrict = 1300;

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 12000)
        {
            width = 4;
            return SmallCheck(board, 0, 3).Key;
        }

        Move x = SmallCheck(board, 0, 4).Key;
        DivertedConsole.Write((width, timer.MillisecondsElapsedThisTurn, "evaluate:", Evaluate(board)));

        if (timer.MillisecondsElapsedThisTurn > timerestrict + 200)
        {
            width -= 1;
        }
        else if (timer.MillisecondsElapsedThisTurn < timerestrict - 200)
        {
            width += 1;
        }
        return x;
    }

    public KeyValuePair<Move, int> SmallCheck(Board board, int frames, int limit)
    {
        Move[] moves = board.GetLegalMoves();
        Dictionary<Move, int> hihi = new();

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            string f = board.GetFenString();
            if (frames < limit && board.GetLegalMoves().Length > 0)
            {
                if (!cck.ContainsKey(f))
                {
                    cck[f] = -SmallCheck(board, 0, 0).Value;
                }
                hihi[moves[i]] = cck[f];
            }
            else
            {
                if (!eve.ContainsKey(f))
                {
                    eve[f] = Evaluate(board);
                }
                hihi[moves[i]] = eve[f];
            }
            board.UndoMove(moves[i]);
        }

        if (frames < limit)
        {
            List<KeyValuePair<Move, int>> sorted = hihi.OrderBy(kv => kv.Value).ToList();
            for (int i = 0; i < Math.Min(width, hihi.Count); i++)
            {
                board.MakeMove(sorted[i].Key);
                if (board.GetLegalMoves().Length > 0)
                {
                    hihi[sorted[i].Key] = -SmallCheck(board, frames + 1, limit).Value;
                }
                board.UndoMove(sorted[i].Key);
            }
        }
        KeyValuePair<Move, int> hehe = hihi.Aggregate((l, r) => l.Value < r.Value ? l : r);
        return hehe; //returns how good the position is for the next player
    }

    readonly PieceType[] piece_types =
                { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
    readonly int[] piece_values = { 94, 281, 297, 512, 936 };

    public int Evaluate(Board board)
    {
        if (board.IsInCheckmate())
        {
            return -9000000;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsInCheck())
        {
            return -SmallCheck(board, 0, 0).Value;
        }

        bool white = board.IsWhiteToMove;
        int value = 0;

        for (int i = 0; i < piece_types.Length; i++)
        {
            int count = board.GetPieceList(piece_types[i], white).Count
                - board.GetPieceList(piece_types[i], !white).Count;
            value += piece_values[i] * count;
        }


        //value += board.GetLegalMoves().Length;
        board.TrySkipTurn();
        value -= board.GetLegalMoves().Length;
        board.UndoSkipTurn();

        return value;
    }
}