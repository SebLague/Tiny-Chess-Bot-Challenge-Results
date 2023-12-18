namespace auto_Bot_233;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_233 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        if (board.GameMoveHistory.Length > 0)
        {
            Move lastMove = board.GameMoveHistory[board.GameMoveHistory.Length - 1];
            string oppositeStart = GetOppositeMove(lastMove.StartSquare);
            string oppositeTarget = GetOppositeMove(lastMove.TargetSquare);
            DivertedConsole.Write(oppositeStart + oppositeTarget);
            Move geniusMove = new Move(oppositeStart + oppositeTarget, board);
            if (allMoves.Contains(geniusMove))
                moveToPlay = geniusMove;
        }

        return moveToPlay;
    }

    private string GetOppositeMove(Square move)
    {
        string startMove = $"{move.Name[0]}{9 - char.GetNumericValue(move.Name[1])}";
        return startMove;
    }
}