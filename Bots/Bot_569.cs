namespace auto_Bot_569;
using ChessChallenge.API;
using System;

public class Bot_569 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return board.GetLegalMoves()[new Random().Next(board.GetLegalMoves().Length)];
    }
}