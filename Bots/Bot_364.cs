namespace auto_Bot_364;
using ChessChallenge.API;

public class Bot_364 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        Move[] allMoves = board.GetLegalMoves();

        Move moveToPlay = allMoves[0];

        foreach (Move move in allMoves)
        {
            // Bongcloud opening
            if (move.MovePieceType == PieceType.King && (move.TargetSquare.Name == "e7" || move.TargetSquare.Name == "e8" || move.TargetSquare.Name == "e1" || move.TargetSquare.Name == "e2"))
            {
                moveToPlay = move;
                break;
            }

            // Move the pawn in front of the king
            else if (move.MovePieceType == PieceType.Pawn && (move.TargetSquare.Name == "e4" || move.TargetSquare.Name == "e5"))
            {
                moveToPlay = move;
                break;
            }
        }
        return moveToPlay;
    }
}