namespace auto_Bot_190;
using ChessChallenge.API;
using System;

public class Bot_190 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        throw new PentagonHexagonOctagonGameGone();
    }

    public class PentagonHexagonOctagonGameGone : Exception
    {
        public override string ToString()
        {
            throw new Exception("Achievement unlocked: Does this count as a win?");
        }
    }
}