namespace auto_Bot_223;
using ChessChallenge.API;
using System;

public class Bot_223 : IChessBot
{
    /*

    . . # # # # . .
    . # . . . . # .
    # . # . . # . #
    # . . . . . . #
    # . # . . # . #
    # . . # # . . #
    . # . . . . # .
    . . # # # # . . 

    */

    public Move Think(Board board, Timer timer)
    {
        ulong smileyPattern = 0b0011110001000010101001011000000110100101100110010100001000111100;

        Move[] legalMoves = board.GetLegalMoves();
        (int score, Move move) bestMove = (0, legalMoves[(new Random()).Next(legalMoves.Length)]);

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);

            ulong matchingBits = board.AllPiecesBitboard & smileyPattern;
            int matchingPiecesCount = 0;
            while (matchingBits != 0)
            {
                matchingBits &= (matchingBits - 1);
                matchingPiecesCount++;
            }
            int currentScore = board.IsDraw() || board.IsInCheck() ? -1 : matchingPiecesCount;
            if (currentScore > bestMove.score) bestMove = (currentScore, move);

            board.UndoMove(move);
        }

        return bestMove.move;
    }
}