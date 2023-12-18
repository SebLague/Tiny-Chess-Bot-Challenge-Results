namespace auto_Bot_86;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_86 : IChessBot
{
    private int[] pieceValues = { 0, 1, 3, 3, 5, 10, 100 };
    private bool mycolor = false;
    private PieceType[] pieces = (PieceType[])Enum.GetValues(typeof(PieceType));

    private Dictionary<ulong, double> memory = new Dictionary<ulong, double>();
    private Random rng = new Random();

    // https://en.wikipedia.org/wiki/Hamming_weight
    private int BitCounting(ulong input)
    {
        input = input - ((input >> 1) & 0x5555555555555555UL);
        input = (input & 0x3333333333333333UL) + ((input >> 2) & 0x3333333333333333UL);
        return (int)(unchecked(((input + (input >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
    }

    // Update old values with newer depth
    private void updateMemory(ulong b, double score)
    {
        if (!memory.TryAdd(b, score))
        {
            memory.Remove(b);
            memory.TryAdd(b, score);
        }
    }

    private double search(ref Board b, ref Timer timer, int depth, double alpha, double beta)
    {
        // Use Transposition Table (not always good idea, but we work around it)
        if (memory.TryGetValue(b.ZobristKey, out double value2))
        {
            return value2;
        }
        // Value when Checkmate
        if (b.IsInCheckmate())
        {
            updateMemory(b.ZobristKey, mycolor ? 9000 : -9000);
            return b.IsWhiteToMove != mycolor ? 9000 : -9000;
        }
        // Value when Draw
        if (b.IsDraw())
        {
            updateMemory(b.ZobristKey, 0);
            return 0;
        }
        System.Span<Move> searchMoves = stackalloc Move[256];
        b.GetLegalMovesNonAlloc(ref searchMoves);

        // bounce if depth or time is over
        if ((depth < 0) && ((timer.MillisecondsElapsedThisTurn + 100 >= timer.MillisecondsRemaining || timer.MillisecondsElapsedThisTurn > 100) || depth < -500))
        {
            double value = 0;
            double enemyValue = 0;
            double tm = b.IsWhiteToMove == mycolor ? 1 : -1;
            for (int i = 0; i < pieces.Length; i++)
            {
                value += BitCounting(b.GetPieceBitboard(pieces[i], mycolor)) * pieceValues[(int)pieces[i]];
                enemyValue += BitCounting(b.GetPieceBitboard(pieces[i], !mycolor)) * pieceValues[(int)pieces[i]];
                value += b.IsInCheck() ? 0.1 * -tm : 0; // don't be in check on our turns
            }
            value = (value / enemyValue);// value of an advantage goes up with the fewer peices left
            return value;
        }

        bool maximize = (b.IsWhiteToMove == mycolor);
        double bestVal = maximize ? -99999 : 99999;
        for (int i = 0; i < searchMoves.Length; i++)
        {
            b.MakeMove(searchMoves[i]);
            double tvalue = search(ref b, ref timer, depth - 1, alpha, beta);
            b.UndoMove(searchMoves[i]);
            if (maximize)
            {
                bestVal = Math.Max(bestVal, tvalue);
                alpha = Math.Max(alpha, bestVal);
            }
            else
            {
                bestVal = Math.Min(bestVal, tvalue);
                beta = Math.Min(bestVal, beta);
            }
            if (beta <= alpha) { break; }
        }
        updateMemory(b.ZobristKey, bestVal);
        return bestVal;
    }

    public Move Think(Board board, Timer timer)
    {
        int numpieces = (BitCounting(board.AllPiecesBitboard));
        int minDepth = numpieces > 6 ? 2 : 6;
        memory.Clear();
        mycolor = board.IsWhiteToMove;
        int turn = mycolor ? 1 : -1;
        double best = -9999;

        Move[] moves = board.GetLegalMoves();
        if (board.PlyCount < 2) { return moves[15]; } // play queen's pawn exlusively
        Move bm = moves[0];

        for (int i = 0; i < moves.Length; i++)
        {
            int r = rng.Next(i, moves.Length);
            Move tmp = moves[r];
            moves[r] = moves[i];
            board.MakeMove(tmp);
            double temp = search(ref board, ref timer, minDepth, -99999, 99999);
            if (temp > best)
            {
                best = temp;
                bm = tmp;
            }
            board.UndoMove(tmp);
        }
        return bm;
    }
}