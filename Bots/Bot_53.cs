namespace auto_Bot_53;
using ChessChallenge.API;
using System;

public class Bot_53 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();
        float time = timer.MillisecondsElapsedThisTurn;

        //get all legal moves
        Move[] moves = board.GetLegalMoves();


        //get all legal captures
        Move[] movesCap = board.GetLegalMoves(true);


        //if there are captures, return the best capture
        int bestMoveIndex = -1;
        int bestMoveValue = 0;
        for (int i = 0; i < movesCap.Length; i++)
        {
            Move move = movesCap[i];
            int value = (int)move.CapturePieceType;
            if (value > bestMoveValue)
            {
                bestMoveIndex = i;
                bestMoveValue = value;
            }
        }
        if (bestMoveIndex != -1)
            return movesCap[bestMoveIndex];

        //check if there are some obviously good moves
        foreach (Move move in moves)
        {
            //if there is a move that results in a castle, return it
            if (move.IsCastles)
                return move;

            //if there is a move that results in a promotion, return it
            if (move.IsPromotion)
                return move;
        }

        int moveIndex = 0;
        //try for 1 second to make a move that will result in a capture
        while (time < 1000)
        {
            time = timer.MillisecondsElapsedThisTurn;


            //if there are no more moves, break
            if (moveIndex >= moves.Length)
                break;

            //make a move
            board.MakeMove(moves[moveIndex]);

            //check if there are captures
            movesCap = board.GetLegalMoves(true);
            if (movesCap.Length > 0)
                //if there are captures, return the move that resulted in a capture
                return moves[moveIndex];

            //if there are no captures, undo the move and try again
            board.UndoMove(moves[moveIndex]);
            moveIndex++;
        }

        //if there are no captures, return a random move
        return moves[rnd.Next(moves.Length)];
    }
}