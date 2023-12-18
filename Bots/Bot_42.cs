namespace auto_Bot_42;
using ChessChallenge.API;
using System;

public class Bot_42 : IChessBot
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int highestValueCapture = 0;
        int highestLossValue = 0;
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        Move attackMove = allMoves[rng.Next(allMoves.Length)];
        Move defenseMove = allMoves[rng.Next(allMoves.Length)];
        Move[] moves = board.GetLegalMoves();



        foreach (Move move in allMoves)
        {


            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            Piece currentPiece = board.GetPiece(move.StartSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            int lostPieceValue = 0;

            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            if (board.SquareIsAttackedByOpponent(currentPiece.Square))
            {
                lostPieceValue = pieceValues[(int)currentPiece.PieceType];
            }

            if (capturedPieceValue > highestValueCapture)
            {
                attackMove = move;
                highestValueCapture = capturedPieceValue;
            }
            if (lostPieceValue > highestLossValue)
            {
                defenseMove = move;
                highestLossValue = capturedPieceValue;
            }
            if (highestLossValue > highestValueCapture + 2)
            {
                if (!board.SquareIsAttackedByOpponent(capturedPiece.Square))
                {
                    moveToPlay = defenseMove;
                }
            }
            else
            {
                moveToPlay = attackMove;
            }
            // if (highestLossValue == 0 && highestValueCapture == 0)
            // {
            //     if (MoveIsCheck(board, move))
            // {
            //     if (!board.SquareIsAttackedByOpponent(capturedPiece.Square))
            //         {   
            //         moveToPlay = move;
            //         break;
            //         }
            // }
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
            {
                return move;
            }
            // }
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

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }
}
