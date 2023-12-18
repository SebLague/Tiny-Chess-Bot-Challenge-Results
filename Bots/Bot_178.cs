namespace auto_Bot_178;
using ChessChallenge.API;
using System;

public class Bot_178 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new Random();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            // Avoid moving the king unless necessary
            if (board.GetPiece(move.StartSquare).PieceType == PieceType.King)
            {
                if (ShouldAvoidMovingKing(board, move))
                {
                    continue;
                }
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                // Check if opponent's best move results in a higher value capture
                if (!OpponentWouldCaptureHighValuePiece(board, move))
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
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

    // Check if opponent's best move would result in a higher value capture
    bool OpponentWouldCaptureHighValuePiece(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] opponentMoves = board.GetLegalMoves();
        int highestValueCapture = 0;

        foreach (Move opponentMove in opponentMoves)
        {
            Piece capturedPiece = board.GetPiece(opponentMove.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            highestValueCapture = Math.Max(highestValueCapture, capturedPieceValue);
        }

        board.UndoMove(move);

        return highestValueCapture > pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
    }

    // Check if the king should avoid moving unless necessary
    bool ShouldAvoidMovingKing(Board board, Move move)
    {
        // Check if the move is castling (always castle if possible)
        if (move.IsCastles)
        {
            return false;
        }

        // Check if moving the king would result in a higher value capture
        board.MakeMove(move);
        Move[] opponentMoves = board.GetLegalMoves();
        int highestValueCapture = 0;

        foreach (Move opponentMove in opponentMoves)
        {
            Piece capturedPiece = board.GetPiece(opponentMove.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            highestValueCapture = Math.Max(highestValueCapture, capturedPieceValue);
        }

        board.UndoMove(move);

        return highestValueCapture > pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
    }
}