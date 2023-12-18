namespace auto_Bot_138;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_138 : IChessBot
{
    static short[] mg_pieceValue = ConvertLongToShorts(0b11111111000101000000101000000111000001100000001000000000);

    short[] pieceSquareTable = CreatePieceSquareTable();

    static Dictionary<ulong, int> transtable;
    public Move Think(Board board, Timer timer)
    {
        int besteval = -100000;
        int alpha = -100000;
        int beta = 100000;
        transtable = new Dictionary<ulong, int>();
        Move bestmove = new();
        Move[] moves = board.GetLegalMoves();

        Array.Sort(moves, (x1, x2) => { return MoveOrder(board, x2).CompareTo(MoveOrder(board, x1)); });
        for (int i = 2; i < 7; i++)
        {
            foreach (Move m in moves)
            {
                board.MakeMove(m);
                int _e = -TreeSearch(board, timer, -beta, -alpha, i);
                board.UndoMove(m);
                DivertedConsole.Write(timer.MillisecondsElapsedThisTurn + "/" + timetoUse(board, timer));
                if (timer.MillisecondsElapsedThisTurn > timetoUse(board, timer))
                    return bestmove;
                if (_e >
                    besteval)
                {
                    besteval = _e;
                    bestmove = m;
                }
                if (_e > alpha)
                    alpha = _e;
            }
        }
        return bestmove;

    }
    public int TreeSearch(Board board, Timer timer, int alpha, int beta, int depth)
    {
        if (depth == 0)
            return evalBoard(board);

        Move[] moves = board.GetLegalMoves();
        if (board.IsRepeatedPosition())
            return 0;
        if (moves.Length == 0)
        {
            if (board.IsInCheck())
                return -50000 + board.PlyCount;
            return 0;
        }

        Array.Sort(moves, (x1, x2) => { return MoveOrder(board, x2).CompareTo(MoveOrder(board, x1)); });

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int _ext = board.IsInCheck() ? 0 : -1;
            int _e = -TreeSearch(board, timer, -beta, -alpha, depth + _ext);
            board.UndoMove(m);
            if (_e >= beta)
                return beta;
            if (_e > alpha)
                alpha = _e;
            if (timer.MillisecondsElapsedThisTurn > timetoUse(board, timer))
                return alpha;
        }
        return alpha;
    }


    public static int MoveOrder(Board b, Move m)
    {
        /*b.MakeMove(m);
        transtable.TryGetValue(b.ZobristKey, out int _order);
        b.UndoMove(m);*/
        int _order = 0;
        if (m.IsCapture)
            _order += mg_pieceValue[(int)m.CapturePieceType] * 50 - mg_pieceValue[(int)m.MovePieceType] * 45;
        if (m.IsPromotion)
            _order += mg_pieceValue[(int)m.PromotionPieceType] * 100;
        return _order;
    }

    static short[] ConvertLongToShorts(ulong value) // convert 1 64bit long to 8 8bit short 
    {
        short[] shorts = new short[8];
        for (int i = 0; i < 8; i++)
            shorts[i] = (short)((value >> (i * 8)) & 0xFF);
        return shorts;
    }
    static short[] ConvertPieceSquareTableLongToShorts(ulong[] value)
    {
        short[] shorts = new short[64];
        for (int i = 0; i < 8; i++)
            ConvertLongToShorts(value[i]).CopyTo(shorts, i * 8);
        for (int i = 0; i < 64; i++)
        {
            shorts[i] -= 128;
            shorts[i] *= 2;
        }
        return shorts;
    }
    static short[] CreatePieceSquareTable()
    {
        short[] table = new short[448];
        short[] fulltable = new short[896];
        // pawn-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b1000000010000000100000001000000010000000100000001000000010000000, 0b0111010110010011100011000111100101110100011101101000000001101110, 0b0111101010010000100000011000000101111011011111100111111001110011, 0b0111010010000101100000111000011110000110011111100111111101110011, 0b0111001110000111100001101000101110001011100000111000011001111001, 0b0111011010001101100110111010000010001111100011011000001101111101, 0b0111101110010000101111101010000010101101100111101100000110101101, 0b1000000010000000100000001000000010000000100000001000000010000000 }).CopyTo(table, 64);
        // knight-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b0110000001100111011100100111100001110000011000110111011001001110, 0b0111011101111001100010011000000001111110011110100110011101110001, 0b0111100010001100100010001000101010000101100001100111110001110011, 0b0111110010001010100010101000111010000111100010001000001001111001, 0b1000101110001001101000111001000110011010100010101000100001111100, 0b1001011010100101110000011010101010100000100100011001111001101001, 0b0111100010000011100111111000110010010000101000110110110001011100, 0b0100110001111001010011111001111001100111011011110101010000101011 }).CopyTo(table, 128);
        // bishop-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b0111010101101100011110100111100101110101011110010111111001101111, 0b1000000010001011100001101000001110000000100010001000011110000010, 0b1000010110001001100011011000011110000111100001111000011110000000, 0b1000001010000101100001101001000110001101100001101000011001111101, 0b0111111110000011100100101001001010011001100010011000001001111110, 0b0111111110010010100110011001000110010100100101011001001001111000, 0b0110100010001001100111011000111101111001011101111000100001110011, 0b0111110010000011011010110111001101101101010101111000001001110001 }).CopyTo(table, 192);
        // rook-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b0111001101101101100000111000100010001000100000000111100101110110, 0b0101110001111101100001010111111101111011011101100111100001101010, 0b0110111101111101100000001000000101110111011110000111001101101001, 0b0111010010000011011111001000010001111111011110100111001101101110, 0b0111011001111100100100011000110010001101100000110111101001110100, 0b1000100010011110100101101000100010010010100011011000100101111101, 0b1001011010001101101000011010100010011111100111011001000010001101, 0b1001010110001111100001001001111110011001100100001001010110010000 }).CopyTo(table, 256);
        // queen-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b0110011101110000011100110111100010000101011110110111011101111111, 0b1000000001111110100001111000010010000001100001010111110001101110, 0b1000001010000111100000010111110101111111011110101000000101111001, 0b0111111010000001011111100111111101111011011110110110000001111011, 0b1000000001111111100010000111111101111000011110000111001001110010, 0b1001110010010111100111001000111010000100100000110111011101111001, 0b1001101110001110100111000111100010000000011111010110110001110100, 0b1001011010010101100101101001110110000110100011101000000001110010 }).CopyTo(table, 320);
        // king-mg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b1000011110001100011100101000010001100101100001101001001001111000, 0b1000010010000100011110000110101001100000011111001000001110000000, 0b0111001001111000011100010110101001101001011101010111100101111001, 0b0110011001101111011010100110100101101100011100100111111101100111, 0b0110111001111001011100110111000101110010011110100111011001110111, 0b0111010110001011100000110111011001111000100000011000110001111011, 0b0111000101101101011111100111110001111100011101100111111110001110, 0b1000011010000001011011110110010001111000100010001000101101011111 }).CopyTo(table, 384);
        table.CopyTo(fulltable, 0);
        table.CopyTo(fulltable, 448); // Clone endgame tables
                                      // pawn-eg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b1000000010000000100000001000000010000000100000001000000010000000, 0b0111110010000001100000001000011010000101100001001000010010000110, 0b0111110001111111011111011000000010000000011111011000001110000010, 0b0111111110000001011111000111110001111100011111101000010010000110, 0b1000100010001000100000100111111110000010100001101000110010010000, 0b1010101010101001100110101001110010100001101010101011001010101111, 0b1101110111010010110000101100100111000011110011111101011011011001, 0b1000000010000000100000001000000010000000100000001000000010000000 }).CopyTo(fulltable, 576);
        // king-eg
        ConvertPieceSquareTableLongToShorts(new ulong[] { 0b0110101001110100011110010111001001111010011101010110111101100101, 0b0111011101111101100000101000011110000110100000100111101001110010, 0b0111101110000011100010001000101110001010011110100111111001110110, 0b0111101010000100100010111000110110001100100010100111111001110111, 0b1000000110001101100100001000110110001101100011001000101101111100, 0b1000011010010110100101101000101010000111100010111000100010000101, 0b1000010110001011100100111000100010001000100001111000100001111010, 0b0111011110000010100001110111101001110111011101110110111001011011 }).CopyTo(fulltable, 832);

        return fulltable;
    }
    int evalBoard(Board board) // evaluate board, positive = player winning
    {
        if (board.IsRepeatedPosition())
            return 0;

        transtable.TryGetValue(board.ZobristKey, out int eval);
        if (eval != 0)
            return eval;

        for (int i = 0; i < 64; i++)
        {
            Piece p = board.GetPiece(new Square(i));
            if (p.IsNull)
                continue;
            eval += mg_pieceValue[(int)p.PieceType] * (p.IsWhite ? 50 : -50); // piece eval
            if (p.IsWhite)
                eval +=
                    (int)(pieceSquareTable[i + ((int)p.PieceType * 64)] * (1 - endGame(board)) +
                    pieceSquareTable[i + ((int)p.PieceType * 64) + 448] * endGame(board)); // white posboard
            else
                eval -=
                    (int)(pieceSquareTable[63 - i + ((int)p.PieceType * 64)] * (1 - endGame(board)) +
                    pieceSquareTable[63 - i + ((int)p.PieceType * 64) + 448] * endGame(board)); // black posboard*/
        }

        eval *= (board.IsWhiteToMove ? 1 : -1);
        transtable.Remove(board.ZobristKey);
        transtable.Add(board.ZobristKey, eval);
        return eval;
    }
    static float endGame(Board board)
    {
        return Math.Min(board.PlyCount * 0.02f, 1f);
    }
    public float timetoUse(Board board, Timer timer)
    {
        return Math.Max(board.PlyCount > 10 ? timer.MillisecondsRemaining / 25 : 400, 100);
    }
}