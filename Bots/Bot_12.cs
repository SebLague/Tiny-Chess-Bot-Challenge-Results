namespace auto_Bot_12;
using ChessChallenge.API;
using System;

public class Bot_12 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        var rand = new Random();
        Move[] takes = board.GetLegalMoves(true);
        foreach (Move i in takes)
        {
            board.MakeMove(i);
            if (board.IsInCheckmate())
            {
                return i;
            }
            board.UndoMove(i);
        }
        foreach (Move i in takes)
        {
            board.MakeMove(i);
            if (board.IsInCheck())
            {
                return i;
            }
            board.UndoMove(i);
        }
        Move[] moves = board.GetLegalMoves();
        foreach (Move i in moves)
        {
            board.MakeMove(i);
            if (board.IsInCheckmate())
            {
                return i;
            }
            board.UndoMove(i);
        }
        foreach (Move i in moves)
        {
            board.MakeMove(i);
            if (board.IsInCheck())
            {
                return i;
            }
            board.UndoMove(i);
        }
        if (takes.Length > 0)
        {
            return takes[rand.Next(takes.Length)];
        }
        return moves[rand.Next(moves.Length)];
    }
}