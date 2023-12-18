namespace auto_Bot_526;
using ChessChallenge.API;
using System;
using System.Linq;

// the evil bot, but rat variation

public class Bot_526 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int turn = board.PlyCount;

        Move[] ratMoves = {
                new Move("d7d6", board),
                new Move("e7e6", board),
                new Move("b7b6", board),
                new Move("c8b7", board),
                new Move("g7g6", board),
                new Move("f8g7", board),
                new Move("b8d7", board),
                new Move("g8e7", board),
                new Move("e8g8", board),

                new Move("d2d3", board),
                new Move("e2e3", board),
                new Move("b2b3", board),
                new Move("c1b2", board),
                new Move("g2g3", board),
                new Move("f1g2", board),
                new Move("b1d2", board),
                new Move("g1e2", board),
                new Move("e1g1", board)
            };

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        if (turn < 20 && highestValueCapture < 300)
        {
            foreach (Move ratMove in ratMoves)
            {
                if (allMoves.Any(move => move.Equals(ratMove)))
                {
                    return ratMove;
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
}