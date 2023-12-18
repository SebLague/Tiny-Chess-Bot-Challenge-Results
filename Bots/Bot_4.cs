namespace auto_Bot_4;
using ChessChallenge.API;
using System;

public class Bot_4 : IChessBot
{
    // Sore Loser Bot.
    // This bot plays chess like normally. But if its losing then it will break the program.

    public Move Think(Board board, Timer timer)
    {
        // We're losing! Quit the game so we don't lose!!
        if (board.IsInCheck())
        {
            // The opponent probably cheated because how else could we be in check?
            DivertedConsole.Write("No fair! you cheater! >:(");
            Environment.Exit(0);
        }

        // Otherwise play the perfect, unbeatable strategy of randomly picking a move and goind with it.
        var moves = board.GetLegalMoves();
        var r = new Random();
        var index = r.Next(moves.Length);
        return moves[index];
    }
}
