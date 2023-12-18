namespace auto_Bot_177;
using ChessChallenge.API;
using System;

public class Bot_177 : IChessBot
{
    public Move Think(Board board, Timer timer)
    //new ChessChallenge.Chess.Move();
    {
        var allMoves = board.GetLegalMoves();
        Random rng = new();
        var moveToPlay = allMoves[rng.Next(allMoves.Length)];

        foreach (var variabMove in board.GetLegalMoves())
            if (variabMove.IsEnPassant)
            {
                DivertedConsole.Write("holy hell");
                return variabMove;
            }

        return moveToPlay;
    }
}