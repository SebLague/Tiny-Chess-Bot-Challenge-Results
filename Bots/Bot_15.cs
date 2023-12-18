namespace auto_Bot_15;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_15 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();

        //Avoid moves that mate the opponent
        Move[] moves = board.GetLegalMoves();
        List<int> elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsInCheckmate())
            {
                elimination_indeces.Add(i);
            }
            board.UndoMove(moves[i]);
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that check 
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsInCheck())
            {
                elimination_indeces.Add(i);
            }
            board.UndoMove(moves[i]);
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that capture queen
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType == PieceType.Queen)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that capture rook
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType == PieceType.Rook)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that capture bishop
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType == PieceType.Bishop)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that capture knight
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType == PieceType.Knight)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoid moves that capture pawn
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType == PieceType.Pawn)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        //Avoids moves that don't use the king
        //I know this is a dumb way to write this but I just want to copy paste other code
        //and I have brain capacity to spare sorry not sorry
        elimination_indeces = new List<int>();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].MovePieceType != PieceType.King)
            {
                elimination_indeces.Add(i);
            }
        }
        if (elimination_indeces.Count == moves.Length)
        { // no moves meet our condition
            return (moves[rnd.Next(moves.Length)]);
        }
        moves = remove_moves(moves, elimination_indeces);

        return (moves[rnd.Next(moves.Length)]);
    }

    public Move[] remove_moves(Move[] moves, List<int> elimination_indeces)
    {
        elimination_indeces.Reverse(); //working in decending order to prevent index weirdness
        var remaining_moves = new List<Move>(moves);
        foreach (int index in elimination_indeces)
        {
            remaining_moves.RemoveAt(index);
        }
        return remaining_moves.ToArray();
    }
}

