namespace auto_Bot_114;
using ChessChallenge.API;
using System;

public class Bot_114 : IChessBot
{
    public Move Think(Board b, Timer t)
    {
        DivertedConsole.Write(b.FiftyMoveCounter);
        Move[] m = b.GetLegalMoves();
        Move[] m2 = b.GetLegalMoves(true);


        bool IsWhiteToMove = b.IsWhiteToMove;

        var s = b.GetKingSquare(IsWhiteToMove);

        Random rnd = new Random();
        Move move = m[rnd.Next(m.Length)];
        if (m2.Length > 0)
        {

            int i = 0;
            foreach (var Move in m2)
            {
                if (Move.IsEnPassant)
                {
                    move = Move;
                    break;
                }
                if (Move.MovePieceType == PieceType.King)
                {
                    move = Move;
                    break;
                }
                if (Move.IsPromotion)
                {
                    move = Move;
                    break;
                }



            }


        }


        return move;
    }
}