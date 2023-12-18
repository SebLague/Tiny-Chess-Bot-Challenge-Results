namespace auto_Bot_38;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_38 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };
    //Initializes the array for piece values used in the evaluation function
    Random rng = new();
    byte[] openbest = new byte[72];
    //Subleq code for the opening
    byte[] midbest = new byte[72];
    //Subleq code for the midgame
    byte[] endbest = new byte[72];
    //Subleq code for the endgame
    public struct DuelResult
    {
        public int eval;
        public int a1;
        public int a2;
        public DuelResult(int eval, int a1, int a2)
        {
            this.eval = eval;
            this.a1 = a1;
            this.a2 = a2;
        }
    }
    //Struct to store the result of a match between two programs
    public byte[] ProgramPlusBoard(Board board, byte[] program)
    {
        byte[] newprogram = program;
        for (int i = 0; i < 64; i++)
        {
            newprogram[i] += (byte)board.GetPiece(new Square(i)).PieceType;
        }
        return newprogram;
    }
    //Gives the board as 'input' to the Subleq program
    public Move toMove(int a, Board board, Move[] moves)
    {
        List<Move> bestmoves = new List<Move>();
        int bestvalue = 0;
        foreach (Move m in moves)
        {
            int capturevalue = a * (pieceValues[(int)m.CapturePieceType] & a);
            //Weights the capture value of the pieces based on the output of the Subleq
            if (capturevalue > bestvalue)
            {
                capturevalue -= a * (pieceValues[(int)m.MovePieceType] & a);
                if (capturevalue > bestvalue)
                {
                    bestvalue = capturevalue;
                    bestmoves.Clear();
                    bestmoves.Add(m);
                }
                if (capturevalue == bestvalue)
                {
                    bestmoves.Add(m);
                }
            }
        }
        if (bestmoves.Count == 0)
            bestmoves.Add(moves[a % moves.Length]);
        //Checks if there is a good capture or a checkmate
        int closestindex = 0;
        foreach (Move m in bestmoves)
            if (Math.Abs((a % moves.Length) - Array.IndexOf(moves, m)) < Math.Abs((a % moves.Length) - Array.IndexOf(moves, bestmoves[closestindex])))
                closestindex = bestmoves.IndexOf(m);
        //Finds the best move closest to the output of the program
        return bestmoves[closestindex];
    }
    public DuelResult Simulate(Board board, byte[] program1, byte[] program2)
    {
        int a, b, c, pointer = 0;
        int a1, a2 = 0;
        a1 = a = b = c = 0;
        byte[] memory;
        Board tempboard = board;
        for (int counter = 0; counter < 10; counter++)
        {
            //Only 'searches' 10 moves ahead
            a = b = c = 0;
            if (counter % 2 == 0)
                memory = program1;
            else
                memory = program2;
            //Alternates which program makes a move
            memory = ProgramPlusBoard(tempboard, memory);
            if (tempboard.GetLegalMoves().Length == 0)
            {
                break;
            }
            //Halts if there is a stalemate or checkmate
            for (int stophalt = 0; stophalt < 72; stophalt++)
            {
                //Since you can't check if a Subleq program will halt just ends after long enough
                pointer %= 24;
                a = memory[pointer] % 24;
                b = memory[pointer + 1] % 24;
                c = memory[pointer + 2] % 24;
                //Stops pointer, a, b, c from accessing outside of memory
                if (a < 0 || b < 0)
                {
                    break;
                }
                memory[b] -= memory[a];
                if (memory[b] > 0)
                {
                    pointer += 3;
                }
                else
                {
                    pointer = c;
                }
                //Subleq interpreter
            }
            tempboard.MakeMove(toMove(a, tempboard, tempboard.GetLegalMoves()));
            if (counter == 0)
                a1 = a;
            if (counter == 1)
                a2 = a;
            //Stores the first output of each program
        }
        return new DuelResult(Evaluate(tempboard), a1, a2);
        //Finds out how good the end position is
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        byte[] program = new byte[72];
        byte[] currentbest = new byte[72];
        int a = 0;
        if (board.PlyCount <= 20)
            currentbest = openbest;
        if (board.PlyCount > 20 && board.PlyCount < 100)
            currentbest = midbest;
        if (board.PlyCount > 100)
            currentbest = endbest;
        //Switches out which program should be used
        for (uint counter = 0; counter < 512; counter++)
        {
            //Creates 512 slight variations of the best program
            for (int i = 0; i < 72; i++)
            {
                program[i] = (byte)(currentbest[i] + rng.Next(0, 4));
            }
            DuelResult duel = Simulate(board, program, currentbest);
            //Pits the best and current program against eachother
            if ((duel.eval > 0 && board.IsWhiteToMove) || (duel.eval < 0 && !board.IsWhiteToMove))
            {
                currentbest = program;
                if (board.PlyCount <= 20)
                    openbest = currentbest;
                if (board.PlyCount > 20 && board.PlyCount < 100)
                    midbest = currentbest;
                if (board.PlyCount > 100)
                    endbest = currentbest;
                a = duel.a1;
            }
            else
                a = duel.a2;
        }
        return toMove(a, board, moves);
    }
    private int Evaluate(Board board)
    {
        int eval, whitesum, blacksum = 0;
        eval = whitesum = blacksum;
        for (int i = 0; i < 12; i++)
        {
            if (i < 6)
            {
                whitesum += board.GetAllPieceLists()[i].Count * pieceValues[i + 1];
            }
            else
                blacksum += board.GetAllPieceLists()[i].Count * pieceValues[i - 5];
        }
        if (board.IsInCheckmate())
            eval += int.MaxValue * (board.IsWhiteToMove ? -1 : 1);
        eval = eval + whitesum - blacksum;
        return eval;
        //Just sums up the value of each piece on the board
    }
}