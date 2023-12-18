namespace auto_Bot_488;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_488 : IChessBot
{
    Dictionary<(ulong, int), (float, Move)> hashtable = new();

    public Move Think(Board board, Timer timer)
    {
        int depth = 1, affTime = timer.MillisecondsRemaining / 120;
        Move BestMove = Move.NullMove;
        while (timer.MillisecondsElapsedThisTurn < affTime)
        {
            hashtable.Clear();
            Search(board, depth, depth, -9999, 9999, affTime, timer, BestMove, ref hashtable);

            if (hashtable[BoardKey(board)].Item1 == 9999) break;
            BestMove = hashtable[BoardKey(board)].Item2;
            depth++;
        }
        return hashtable[BoardKey(board)].Item2;
    }

    public static (ulong, int) BoardKey(Board board) { return (board.ZobristKey, board.PlyCount); }

    public static float Eval(Board board)
    {
        if (board.IsInCheckmate()) return -9999;
        if (board.IsDraw()) return 0;

        ulong[] edge = { 103481868288, 66125924401152, 35538699412471296, 18411139144890810879 },
                quart = { 1085102592318504960, 17361641477096079360, 252645135, 4042322160 },
                corner = { 1736165144126822424, 2604460471728153636, 4810688826961871682, 9295429630892703873 };

        PieceList[] Allpieces = board.GetAllPieceLists();
        bool white = board.IsWhiteToMove;
        float open = 0;
        if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) > 26)
        {
            ulong play = white ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
            open = 0.5f * BitboardHelper.GetNumberOfSetBits(edge[0] & play)
                + 0.3f * BitboardHelper.GetNumberOfSetBits(edge[1] & play)
                - 0.03f * BitboardHelper.GetNumberOfSetBits(edge[2] & play)
                - 0.002f * BitboardHelper.GetNumberOfSetBits(edge[3] & play);
        }

        float value = (white ? 1 : -1) *
               (Allpieces[0].Count - Allpieces[6].Count
               + 3 * (Allpieces[1].Count + Allpieces[2].Count - Allpieces[7].Count - Allpieces[8].Count)
               + 5 * (Allpieces[3].Count - Allpieces[9].Count)
               + 9 * (Allpieces[4].Count - Allpieces[10].Count)
               );

        return value + open;
    }

    public static float Search(Board board, int max, int depth, float alpha, float beta, int affTime, Timer timer,
        Move BestMove,
        ref Dictionary<(ulong, int), (float, Move)> hashtable)
    {
        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves);

        if (Math.Min(moves.Length, depth) == 0) return Eval(board);
        Move move, best = moves[0];

        var Moves = moves.ToArray().ToList();
        int q = moves.Length - 1, ca = 0, ch = 0, sw;
        while (q + 1 > ch + ca)
        {
            move = Moves[q];
            sw = -1;
            board.MakeMove(move);
            if (board.IsDraw())
            {
                Moves.Remove(move);
                Moves.Add(move);
            }
            else
            {
                if (board.IsInCheck()) sw++;
                else if (move.IsCapture) sw = ch;
            }
            board.UndoMove(move);
            if (sw > -1)
            {
                Moves.Remove(move);
                Moves.Insert(sw, move);
                if (sw == 0) ch++;
                else ca++;
            }
            else q--;
        }

        moves = Moves.ToArray();

        if (hashtable.ContainsKey(BoardKey(board))) return hashtable[BoardKey(board)].Item1;
        for (int i = 0; i < moves.Length; i++)
        {
            if (affTime < timer.MillisecondsElapsedThisTurn)
            {
                if (depth == max) hashtable.Add(BoardKey(board), (0, BestMove));
                return 0;
            }
            if (!hashtable.ContainsKey(BoardKey(board)))
            {
                move = moves[i];
                board.MakeMove(move);
                float evaluation = -Search(board, max, depth - 1, -beta, -alpha, affTime, timer, BestMove, ref hashtable);
                board.UndoMove(move);

                if (evaluation >= beta)
                {
                    hashtable.Add(BoardKey(board), (beta, move));
                    return beta;
                }
                if (alpha < evaluation)
                {
                    best = move;
                    alpha = evaluation;
                }
            }
            else return hashtable[BoardKey(board)].Item1;
        }

        hashtable.Add(BoardKey(board), (alpha, best));
        return alpha;
    }
}