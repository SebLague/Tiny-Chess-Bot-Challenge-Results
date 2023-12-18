namespace auto_Bot_638;
using ChessChallenge.API;
using System.Linq;

public class Bot_638 : IChessBot
{
    public Move Think(Board board, Timer timer) =>
    board.GetLegalMoves().MaxBy(x =>
    {
        return
            board.SquareIsAttackedByOpponent(x.TargetSquare) ? -999999 :
            5000 * (int)x.CapturePieceType
            - x.RawValue
            ;
    }
    );
}