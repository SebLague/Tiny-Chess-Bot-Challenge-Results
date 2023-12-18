namespace auto_Bot_56;
using ChessChallenge.API;
using System;

public class Bot_56 : IChessBot
{
    // The decimal expansion of the Inverse of the Golden Ration
    // Typed out manually because the precision of the standard sqrt was causing problems.
    private decimal GoldenRatioInverse = 0.61803398874989484820458683436563M;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        // Hash the board state and get a value within the appropriate range for the number of moves.
        int hash = getHash(moves.Length, board.ZobristKey);
        return moves[hash];
    }

    public int getHash(decimal numMoves, ulong zobristKey)
    {
        decimal frac = (zobristKey * GoldenRatioInverse - Math.Floor(zobristKey * GoldenRatioInverse));
        int hash = (int)(numMoves * frac);
        return hash;
    }

}
