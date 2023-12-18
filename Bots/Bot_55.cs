namespace auto_Bot_55;
using ChessChallenge.API;
using System;

// A simple bot that always offer the most valuable piece to the opponent.
// Plays randomly otherwise.
public class Bot_55 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing worst is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        // Find highest value blunder
        foreach (Move move in allMoves)
        {
            if (board.SquareIsAttackedByOpponent(move.TargetSquare) && !board.SquareIsAttackedByOpponent(move.StartSquare) && !move.IsCapture && !MoveIsCheck(board, move))
            {
                int capturedPieceValue = pieceValues[(int)move.MovePieceType];

                if (capturedPieceValue > highestValueCapture)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }
        }

        return moveToPlay;
    }

    // Test if this move gives check
    public bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }
}