namespace auto_Bot_252;
using ChessChallenge.API;
using System;

public class Bot_252 : IChessBot
{
    int[] pieceValues = { 0, 100, 250, 350, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one and en passant
            if (MoveIsCheckmate(board, move) || move.IsEnPassant)
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];



            board.MakeMove(move);
            Move[] allMoves2 = board.GetLegalMoves();
            int lowestValueCapture = 0;
            int newcapturedPieceValue = 0;

            foreach (Move move2 in allMoves2)
            {

                // Find lowest value capture
                Piece capturedPiece2 = board.GetPiece(move.TargetSquare);
                int capturedPieceValue2 = pieceValues[(int)capturedPiece2.PieceType];

                if (capturedPieceValue2 < lowestValueCapture)
                {
                    newcapturedPieceValue = capturedPieceValue - capturedPieceValue2;
                }
            }

            board.UndoMove(move);

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsDraw();
        board.UndoMove(move);
        return isMate;
    }
}