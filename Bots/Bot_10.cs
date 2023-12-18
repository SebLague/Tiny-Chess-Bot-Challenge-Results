namespace auto_Bot_10;
using ChessChallenge.API;
using System;

public class Bot_10 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int moveIndex = 0; //Holds the index of the move to return
        var captureMoves = board.GetLegalMoves();
        var promotionMoves = board.GetLegalMoves();

        Array.Sort(promotionMoves, (m1, m2) =>
        {
            return m1.TargetSquare.Index.CompareTo(m2.TargetSquare.Index);
        });

        Array.Sort(captureMoves, (m1, m2) =>
        {
            return m1.TargetSquare.Index.CompareTo(m2.TargetSquare.Index);
        });

        Array.Sort(captureMoves, (o1, o2) =>
        {
            return o2.CapturePieceType.CompareTo(o1.CapturePieceType);
        });

        Array.Sort(promotionMoves, (o1, o2) =>
        {
            return o2.PromotionPieceType.CompareTo(o1.PromotionPieceType);
        });

        if (captureMoves[0].CapturePieceType >= promotionMoves[0].PromotionPieceType)
        {
            return captureMoves[0];
        }
        return promotionMoves[0];
    }
}