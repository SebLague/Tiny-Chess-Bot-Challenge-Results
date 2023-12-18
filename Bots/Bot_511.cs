namespace auto_Bot_511;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_511 : IChessBot
{
    int[] pieceValues = { 0, 100, 310, 330, 500, 1000, 100000 };
    Random rng = new();
    // ulong[] Transposition = new ulong[3000];
    // int MemS = 0;
    // List<int> Materialvalues = new();
    // int Idepth = 3;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move moveToPlay = moves[rng.Next(moves.Length)];
        int highestValue = int.MinValue;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                return move;
            }
            int Value = -Search(board, 3, -100000, 100000);

            if (Value >= highestValue)
            {
                highestValue = Value;
                moveToPlay = move;
            }
            board.UndoMove(move);
        }
        // DivertedConsole.Write(highestValue);
        return moveToPlay;
    }
    int Search(Board board, int iteration, int alpha, int beta)
    {
        Move[] moves = MoveOrder(board);

        if (board.IsInCheckmate())
        {
            return -100000;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        if (iteration == 0)
        {
            return CountMaterial(board);
        }
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int Value = -Search(board, iteration - 1, -beta, -alpha);

            board.UndoMove(move);
            if (beta <= Value)
            {
                return beta;
            }
            alpha = Math.Max(Value, alpha);
        }
        return alpha;
    }
    Move[] MoveOrder(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        var check = new List<Move>() { };
        var capture = new List<Move>() { };
        var else1 = new List<Move>() { };
        var a = new List<Move>() { };
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                a.Add(move);
                board.UndoMove(move);
                return a.ToArray();
            }
            if (move.IsCapture) { capture.Add(move); }
            else if (board.IsInCheck()) { check.Add(move); }
            else { else1.Add(move); }
            board.UndoMove(move);
        }


        return capture.Concat(check).Concat(else1).ToArray();
    }
    // Move[] MoveOrder(Board board)
    // {
    //     Move[] moves = board.GetLegalMoves();
    //     if (moves.Length == 0)
    //     {
    //         return moves;
    //     }
    //     var val = new int[moves.Length];
    //     int i = 0;
    //     foreach (Move move in moves)
    //     {
    //         board.MakeMove(move);
    //         if (board.IsInCheckmate())
    //         {
    //             board.UndoMove(move);
    //             return new Move[1] { move };
    //         }
    //         if (board.IsInCheck())
    //         {
    //             val[i] -= 100;
    //         }
    //         if (move.IsCapture)
    //         {
    //             val[i] -= pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType]
    //              - pieceValues[(int)board.GetPiece(move.StartSquare).PieceType];
    //         }
    //         i++;
    //         board.UndoMove(move);
    //     }
    //     Array.Sort(val, moves);
    //     return moves;
    // }
    int CountMaterial(Board board)
    {
        bool side = board.IsWhiteToMove;
        if (board.IsInCheckmate())
        {
            return (side ? 1 : -1) * 100000;
        }

        int Material = 0;
        PieceList[] all = board.GetAllPieceLists();
        foreach (PieceList piece in all)
        {
            foreach (Piece p in piece)
            {
                Material += pieceValues[(int)p.PieceType] * (p.IsWhite ? 1 : -1) * (side ? 1 : -1);

                Material += Squares[32 * ((int)p.PieceType - 1)
                + (p.Square.Index < 32 ? p.Square.Index : 32 - p.Square.Index + 32)]
                * (p.IsWhite ? 1 : -1) * (side ? 1 : -1);
            }
        }

        Material += (side ? 1 : -1) * (board.IsInCheck() ? 100 : 0);
        return Material;
    }
    int[] Squares = new int[193]{
        0,  0,  0,  0,  0,  0,  0,  0,
        5, 10, 10,-20,-20, 10, 10,  5,
        5, -5,-10,  0,  0,-10, -5,  5,
        0,  0,  0, 20, 20,  0,  0,  0,

        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,

        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,

         0,  0,  0,  5,  5,  0,  0,  0,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,

        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
        -5,  0,  5,  5,  5,  5,  0, -5,

        20, 30, 10,  0,  0, 10, 30, 20,
        20, 20,  0,  0,  0,  0, 20, 20,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,0
    };

}
