namespace auto_Bot_394;
using ChessChallenge.API;
using System;

public class Bot_394 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 100000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move[] captures = board.GetLegalMoves(true);
        Move moveToPlay = allMoves[new Random().Next(allMoves.Length)];

        int highestPointGain = 0;
        int highestTrade = 0;
        bool hasCaptured = false;

        foreach (Move move in captures)
        {
            if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                highestPointGain = pieceValues[(int)move.CapturePieceType];
                moveToPlay = move;
                hasCaptured = true;
            }
            else if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                int gain = CalcPointGain(move.MovePieceType, move.CapturePieceType);
                if (MoveIsCheck(move)) gain += 200;
                if (move.IsPromotion && (int)move.PromotionPieceType == 5) gain += 800;
                if (gain > highestPointGain)
                {
                    moveToPlay = move;
                    highestPointGain = gain;
                    hasCaptured = true;
                }
                else if (gain == 0 && pieceValues[(int)move.CapturePieceType] > highestTrade)
                {
                    if (SquasreIsProtectedAfterDifferentMove(move)) continue;
                    highestTrade = pieceValues[(int)move.CapturePieceType];
                    moveToPlay = move;
                    hasCaptured = true;
                }
            }
        }

        foreach (Move move in allMoves)
        {
            if ((int)move.MovePieceType == 6 && board.PlyCount > 10 && board.PlyCount < 50) continue;
            else if ((int)move.MovePieceType == 1 && board.PlyCount < 20 && (move.StartSquare == new Square("a2") ||
                                                                              move.StartSquare == new Square("b2") ||
                                                                              move.StartSquare == new Square("c2") ||
                                                                              move.StartSquare == new Square("f2") ||
                                                                              move.StartSquare == new Square("g2") ||
                                                                              move.StartSquare == new Square("h2") ||
                                                                              move.StartSquare == new Square("a7") ||
                                                                              move.StartSquare == new Square("b7") ||
                                                                              move.StartSquare == new Square("c7") ||
                                                                              move.StartSquare == new Square("f7") ||
                                                                              move.StartSquare == new Square("g7") ||
                                                                              move.StartSquare == new Square("h7"))) continue;
            else if ((int)move.MovePieceType == 2 && board.PlyCount < 20 && (move.TargetSquare == new Square("a3") ||
                                                                              move.TargetSquare == new Square("h3") ||
                                                                              move.TargetSquare == new Square("a6") ||
                                                                              move.TargetSquare == new Square("h6"))) continue;
            if (MoveIsCheckmate(move))
            {
                moveToPlay = move;
                break;
            }
            else if (MoveIsCheck(move) && !hasCaptured)
            {
                moveToPlay = move;
            }
            else if (move.IsPromotion && (int)move.PromotionPieceType == 5)
            {
                moveToPlay = move;
            }
            else if (!board.SquareIsAttackedByOpponent(move.TargetSquare) && !hasCaptured)
            {
                moveToPlay = move;
            }
        }

        return moveToPlay;

        // Calculates gain from capture
        int CalcPointGain(PieceType movePiece, PieceType capturedPiece)
        {
            if ((int)movePiece < 1000) return pieceValues[(int)capturedPiece] - pieceValues[(int)movePiece];
            else return pieceValues[(int)movePiece];
        }

        // Tests if this move is checkmate
        bool MoveIsCheckmate(Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // Tests if this move is check
        bool MoveIsCheck(Move move)
        {
            board.MakeMove(move);
            bool isCheck = board.IsInCheck();
            board.UndoMove(move);
            return isCheck;
        }

        // Checks if moves start squere is protected by another piece
        bool SquasreIsProtectedAfterDifferentMove(Move move)
        {
            board.MakeMove(move);
            bool isProtected = board.SquareIsAttackedByOpponent(move.StartSquare);
            board.UndoMove(move);
            return isProtected;
        }
    }
}