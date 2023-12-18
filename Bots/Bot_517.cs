namespace auto_Bot_517;
using ChessChallenge.API;
using System;

public class Bot_517 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move[] allmovesdone = board.GameMoveHistory;
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        Move dangered = moveToPlay;
        Move temp = moveToPlay;
        Move better = moveToPlay;


        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move; // Return immediately if a checkmate move is found.
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            Piece movePiece = board.GetPiece(move.StartSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            int capturingValue = pieceValues[(int)movePiece.PieceType];
            Square esquare = move.TargetSquare;
            Square ssquare = move.StartSquare;
            Square psquare = move.StartSquare;

            if (capturedPieceValue > highestValueCapture)
            {
                if (!board.SquareIsAttackedByOpponent(esquare))
                {
                    if (capturingValue < capturedPieceValue)
                    {
                        highestValueCapture = capturedPieceValue;
                        moveToPlay = move;
                    }
                    else
                    {
                        highestValueCapture = capturedPieceValue;
                        moveToPlay = move;
                    }

                }
            }
            if (allmovesdone.Length > 25)
            {
                if (capturingValue == 100)
                {
                    temp = move;
                }
            }
            int vpeice = 0;
            if (board.SquareIsAttackedByOpponent(ssquare))
            {
                if (capturingValue > vpeice)
                {
                    if (!board.SquareIsAttackedByOpponent(esquare))
                    {
                        vpeice = capturingValue;
                        dangered = move;
                    }
                }
            }


            bool promot = move.IsPromotion;
            if (highestValueCapture == 0)
            {
                moveToPlay = dangered;
            }
            /*                if (promot==true)
                            {
                            int promotype=pieceValues[(int)(move.PromotionPieceType)];// Assuming PromotionPieceType is a property or field
                                    if (promotype==900)
                                    {
                                    moveToPlay = move;
                                    }
                            }*/
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
    bool SquareIsProtected(Board board, Move move, Square ssquare)
    {
        board.MakeMove(move);
        bool safe = board.SquareIsAttackedByOpponent(ssquare);
        board.UndoMove(move);
        return safe;
    }
}
