namespace auto_Bot_327;
using ChessChallenge.API;
using System;

public class Bot_327 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 270, 300, 500, 900, 10000 };
    int[] moveValues = { 0, 100, 95, 82, 81, 80, 55 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueMove = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            // Only promote to queens
            if ((move.IsPromotion) & !((int)move.PromotionPieceType == 5))
            { continue; }
            // Evaluate the game state
            int thisMoveValue = moveValue(board, move) + rng.Next(50);
            // Look ahead one move, and incorporate the opponent's best move in the score
            board.MakeMove(move);
            Move[] opponentMoves = board.GetLegalMoves();
            bool isMate = false;
            int bestResponseValue = 0;
            foreach (Move response in opponentMoves)
            {
                int responseValue = moveValue(board, response);
                if (bestResponseValue < responseValue)
                { bestResponseValue = responseValue; }
                board.MakeMove(response);
                if (board.IsInCheckmate())
                {
                    isMate = true;
                    board.UndoMove(response);
                    break;
                }
                board.UndoMove(response);
            }
            board.UndoMove(move);
            if (isMate) { continue; }
            thisMoveValue -= bestResponseValue;
            // decide on the move
            if (highestValueMove < thisMoveValue)
            {
                moveToPlay = move;
                highestValueMove = thisMoveValue;
            }

        }

        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Calculate the approximate value of this board state for the current player
    int moveValue(Board board, Move move)
    {
        int thisMoveValue = 0;
        Square targetSquare = move.TargetSquare;
        if (board.SquareIsAttackedByOpponent(targetSquare))
        {
            thisMoveValue = -25 - pieceValues[(int)move.MovePieceType];
        }
        else
        {
            thisMoveValue = moveValues[(int)move.MovePieceType];
        }
        if (move.IsCapture)
        {
            thisMoveValue += pieceValues[(int)move.CapturePieceType];
        }
        if (move.IsPromotion) { thisMoveValue += 800; }
        return thisMoveValue;
    }
}