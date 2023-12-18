namespace auto_Bot_5;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_5 : IChessBot
{
    Random rng;
    Piece angryPiece;
    Square angryPieceSquare;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    bool reevaulate = false;

    public Move Think(Board board, Timer timer)
    {
        rng = new();
        if (angryPiece.IsNull || reevaulate)
        {
            ChooseAngryPiece(board);
            reevaulate = false;
        }
        Move[] moves = board.GetLegalMoves();
        List<Move> angryPieceMoves = new List<Move>();
        int highestValueCapture = 0;

        while (angryPieceMoves.Count <= 0)
        {
            foreach (Move move in moves)
            {
                if (move.StartSquare.Index == angryPieceSquare.Index)
                {
                    angryPieceMoves.Add(move);
                }
            }
            if (angryPieceMoves.Count <= 0)
            {
                ChooseAngryPiece(board);
            }
        }

        Move moveToPlay = angryPieceMoves[rng.Next(angryPieceMoves.Count)];

        foreach (Move move in angryPieceMoves)
        {
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }
        if (angryPiece.IsKing) { reevaulate = true; }

        angryPieceSquare = moveToPlay.TargetSquare;
        return moveToPlay;
    }

    public void ChooseAngryPiece(Board board)
    {
        angryPiece = board.GetPiece(new Square(rng.Next(64)));
        while (angryPiece.IsNull && !angryPiece.IsWhite)
        {
            angryPieceSquare = new Square(rng.Next(64));
            angryPiece = board.GetPiece(angryPieceSquare);
        }
    }
}