namespace auto_Bot_26;
using ChessChallenge.API;
using System;

public class Bot_26 : IChessBot
{
    // Store square of current piece. (So we can continue moving only it)
    Square? chosen = null;
    public Move Think(Board board, Timer timer)
    {
        // Retrieve all legal moves
        Move[] moves = board.GetLegalMoves();
        // If chosen is null, get one randomly and store the start square
        if (chosen == null)
        {
            chosen = chooseRandomMove(moves).StartSquare;
        }
        // Filter all the moves with the square, so we retrieve the available moves for the piece
        Move[] filtered = Array.FindAll(moves, m => m.StartSquare == chosen);
        // If the piece is unavailable, eaten or blocked, we switch to a new one !
        while (filtered.Length == 0)
        {
            chosen = chooseRandomMove(moves).StartSquare;
            filtered = Array.FindAll(moves, m => m.StartSquare == chosen);
        }
        // Choose randomly a move for the piece
        Move chosenMove = chooseRandomMove(filtered);
        // Store the target square so we can retrieve the piece next turn !
        chosen = chosenMove.TargetSquare;
        return chosenMove;
    }

    // Function to randomly chose a move from an array of moves
    private Move chooseRandomMove(Move[] moves)
    {
        System.Random rng = new();
        return moves[rng.Next(moves.Length)];
    }
}