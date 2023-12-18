namespace auto_Bot_120;
using ChessChallenge.API;
using System;

public class Bot_120 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int bestMoveValue = int.MinValue;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Evaluate the move
            int moveValue = EvaluateMove(board, move);

            if (moveValue > bestMoveValue)
            {
                moveToPlay = move;
                bestMoveValue = moveValue;
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

    // Evaluate the value of a move
    int EvaluateMove(Board board, Move move)
    {
        // Find the value of the captured piece
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

        // Find the value of the moved piece
        Piece movedPiece = board.GetPiece(move.StartSquare);
        int movedPieceValue = pieceValues[(int)movedPiece.PieceType];

        // Calculate the net gain or loss of material
        int materialGain = capturedPieceValue - movedPieceValue;

        // Add a bonus for putting the opponent in check
        int checkBonus = 0;
        if (MoveGivesCheck(board, move))
        {
            checkBonus = 50;
        }

        // Add a bonus for controlling the center of the board
        int centerControlBonus = 0;
        if (move.TargetSquare.File >= 'C' && move.TargetSquare.File <= 'F' && move.TargetSquare.Rank >= 3 && move.TargetSquare.Rank <= 6)
        {
            centerControlBonus = 10;
        }

        return materialGain + checkBonus + centerControlBonus;
    }

    // Test if this move puts the opponent in check
    bool MoveGivesCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isInCheck = board.IsInCheck();
        board.UndoMove(move);
        return isInCheck;
    }
}