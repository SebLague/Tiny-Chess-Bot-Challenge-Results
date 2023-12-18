namespace auto_Bot_474;
using ChessChallenge.API;
using System.Collections.Generic;
public class Bot_474 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        System.Random rng = new();
        if (rng.Next(2) == 0)
        {
            Move[] moves = board.GetLegalMoves(true);
            List<Move> checkmoveslist = new List<Move>();
            System.Random rngMoves = new();
            Move moveToPlay;
            for (int i = 0; i < moves.Length; i++)
            {
                moveToPlay = moves[i];
                board.MakeMove(moveToPlay);
                if (board.IsInCheck())
                {
                    if (!board.IsInCheckmate())
                    {
                        return moveToPlay;
                    }
                    checkmoveslist.Add(moveToPlay);
                }
                board.UndoMove(moveToPlay);
            }
            Move[] checkmoves = checkmoveslist.ToArray();
            if (checkmoves.Length > 0)
            {
                return checkmoves[rngMoves.Next(checkmoves.Length)];
            }
            if (moves.Length > 0)
            {
                return moves[rngMoves.Next(moves.Length)];
            }
            moves = board.GetLegalMoves();
            return moves[rngMoves.Next(moves.Length)];
        }
        else
        {
            Move[] moves = board.GetLegalMoves(true);
            List<Move> checkmoveslist = new List<Move>();
            System.Random rngMoves = new();
            Move moveToPlay;
            for (int i = 0; i < moves.Length; i++)
            {
                moveToPlay = moves[i];
                board.MakeMove(moveToPlay);
                if (board.IsInCheckmate())
                {
                    return moveToPlay;
                }
                if (board.IsInCheck())
                {
                    checkmoveslist.Add(moveToPlay);
                }
                board.UndoMove(moveToPlay);
            }
            Move[] checkmoves = checkmoveslist.ToArray();
            if (checkmoves.Length > 0)
            {
                return checkmoves[rngMoves.Next(checkmoves.Length)];
            }
            if (moves.Length > 0)
            {
                return moves[rngMoves.Next(moves.Length)];
            }
            moves = board.GetLegalMoves();
            return moves[rngMoves.Next(moves.Length)];
        }
    }
}