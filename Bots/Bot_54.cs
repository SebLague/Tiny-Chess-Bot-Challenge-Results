namespace auto_Bot_54;
using ChessChallenge.API;

public class Bot_54 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        long turtle = 0b_01010100_01110101_01110010_01110100_01101100_01100101;
        long highest = 0;
        int index = 0;
        for (int i = 0; i < moves.Length; i++)
        {
            if ((moves[i].RawValue & turtle) > highest)
            {
                highest = moves[i].RawValue & turtle;
                index = i;
            }
        }

        return moves[index];
    }
}