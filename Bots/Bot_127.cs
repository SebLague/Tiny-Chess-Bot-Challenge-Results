namespace auto_Bot_127;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
public class Bot_127 : IChessBot
{
    const double C = 0.5;
    int[] utcht = new int[1024 * 1024 * 128 / 8];
    byte BitCount(ulong value)
    {
        ulong result = value - ((value >> 1) & 0x5555555555555555UL);
        result = (result & 0x3333333333333333UL) + ((result >> 2) & 0x3333333333333333UL);
        return (byte)(unchecked(((result + (result >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
    }
    uint rng(uint input)
    {
        var f = ((uint)input ^ (uint)(2U * 432U)) * 5346547645U + 456425632664U;
        f = (f * 32 * (f * 54));
        return (uint)(f ^ (f * f - f));
    }
    int ms(Move mv, Board board, uint depth, uint seed, int md)
    {
        if (depth > md)
        {
            return 0;
        }



        Move mv2 = mv;
        board.MakeMove(mv);
        Move[] mvs = board.GetLegalMoves();
        if (mvs.Length == 0) { board.UndoMove(mv2); return 1; }
        //DivertedConsole.Write("ggg");
        var mvo = mvs[rng(depth + seed + (uint)mvs.Length) % mvs.Length];

        int msr = -ms(mvo, board, depth + 1, seed, md);
        //utcht[board.GetHashCode() % utcht.Length]++;
        //var nj = utcht[board.GetHashCode() % utcht.Length];
        board.UndoMove(mv2);
        //var n = utcht[board.GetHashCode() % utcht.Length];
        //return msr + (int)C*(int)Math.Sqrt((double)((double)Math.Log(n))/(double)nj);
        return msr;
    }
    double utc(double x, double n, double nj)
    {
        double C = 1.4142;
        return x + C * Math.Sqrt((Math.Log(n)) / nj);
    }
    public Move Think(Board board, Timer timer)
    {


        Move[] moves = board.GetLegalMoves();
        var goodmoves = new List<Move>();

        int[] gut = new int[250];
        int[] fin = new int[250];
        for (int i = 0; i < 250; i++)
        {
            fin[i] = 0;
            gut[i] = 0;
        }
        uint j;
        for (j = 0; 1000 > timer.MillisecondsElapsedThisTurn; j++)
        {
            for (uint i = 0; i < moves.GetLength(0); i++)
            {
                //DivertedConsole.Write( (timer.MillisecondsRemaining )/1200+ (((timer.MillisecondsRemaining )/1200)%2)+1);
                int output = ms(moves[i], board, 0, j, (timer.MillisecondsRemaining) / 1200 + (((timer.MillisecondsRemaining) / 1200) % 2) + 1);
                gut[i] += output * 2 - (Math.Abs(output));
                //gut[i] =  Math.Max(0,output);
                if (output != 0) fin[i]++;
            }
        }

        uint mx = 0;
        int mxn = -1232323;
        int nj = 0;
        for (int i = 0; i < 250; i++)
        {
            nj += fin[i];
        }
        for (uint i = 0; i < moves.Length; i++)
        {
            if (((double)gut[i] > (double)mxn) && fin[i] != 0) { mx = i; mxn = gut[i]; }
            // try uct maybe something like, (no utc is better)
            //if (utc(((double)gut[i] / (double)Math.Max(fin[i], 1)), nj, fin[i]) > utc(((double)mxn / (double)Math.Max(fin[mx], 1)),nj, fin[i]) && fin[i] != 0) { 
            //    mx = i; mxn = gut[i];
            //    }
        }
        //DivertedConsole.Write( fin[mx]);
        return moves[mx % moves.Length];
    }
}