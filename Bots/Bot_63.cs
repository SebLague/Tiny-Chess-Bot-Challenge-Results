namespace auto_Bot_63;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_63 : IChessBot
{
    public Move Think(Board board, Timer timer) =>
    board.GetLegalMoves().MaxBy(x =>
    {
        board.MakeMove(x);
        bool cm = board.IsInCheckmate();
        board.UndoMove(x);
        return
            board.SquareIsAttackedByOpponent(x.TargetSquare) ? -99999 :
            cm ? 999999 :
            new Random().Next(15) +
            50 * (int)x.CapturePieceType
            - Math.Abs(x.StartSquare.Index - 32)
            ;
    }
    );
}