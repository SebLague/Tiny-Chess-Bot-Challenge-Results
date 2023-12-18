namespace auto_Bot_14;
using ChessChallenge.API;
using System;

public class Bot_14 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        int highestValueCapture = 0;
        Move highestValueCaptureMove = moveToPlay;

        int highestValueSafety = 0;
        Move highestValueSafetyMove = moveToPlay;

        bool hasSafeCheckMove = false;
        Move safeCheckMove = moveToPlay;

        foreach (Move move in allMoves)
        {
            // Find highest value capture using piece values delta
            Piece movingPiece = board.GetPiece(move.StartSquare);
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int movingPieceValue = pieceValues[(int)movingPiece.PieceType];
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            bool isDestinationSafe = !board.SquareIsAttackedByOpponent(move.TargetSquare);
            bool isInDanger = board.SquareIsAttackedByOpponent(move.StartSquare);

            // pls don't int your queen (maybe not good to early exit this)
            if (movingPiece.PieceType == PieceType.Queen && !isDestinationSafe)
            {
                continue;
            }
            // Always deny promotion to anything but queen
            if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen)
            {
                continue;
            }

            // Always play checkmate in one
            if (IsMoveCheckMate(board, move))
            {
                return move;
            }

            if (capturedPieceValue - (isDestinationSafe ? 0 : movingPieceValue) > highestValueCapture)
            {
                DivertedConsole.Write("We could take " + capturedPiece.PieceType + (isDestinationSafe ? " for free." : ", but we would have to sacrifice our " + movingPiece.PieceType));
                highestValueCaptureMove = move;
                highestValueCapture = capturedPieceValue - (isDestinationSafe ? 0 : movingPieceValue);
            }

            // Moves that put the piece to safety 
            // We can add the value of safely taking an opposing piece + the safety of our piece.
            if (isInDanger && isDestinationSafe && movingPieceValue + capturedPieceValue > highestValueSafety)
            {
                DivertedConsole.Write(movingPiece.PieceType + " can move to safety, " + (capturedPiece.PieceType != PieceType.None ? "while capturing " + capturedPiece.PieceType : ""));
                highestValueSafetyMove = move;
                highestValueSafety = movingPieceValue + capturedPieceValue;
            }

            if (isDestinationSafe && IsMoveCheck(board, move) && !IsMoveDraw(board, move))
            {
                hasSafeCheckMove = true;
                safeCheckMove = move;
            }

            // Just make sure we don't hang anything if we can, as a last check
            if (isDestinationSafe)
            {
                moveToPlay = move;
            }
        }

        // Summary to write out
        DivertedConsole.Write("HVC = " + highestValueCapture + " | HVS = " + highestValueSafety);

        if (highestValueCapture > 0 || highestValueSafety > 0)
        {
            return highestValueSafety >= highestValueCapture ? highestValueSafetyMove : highestValueCaptureMove;
        }
        if (hasSafeCheckMove)
        {
            return safeCheckMove;
        }

        return moveToPlay;

        bool IsMoveCheckMate(Board b, Move m)
        {
            board.MakeMove(m);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(m);
            return isMate;
        }

        bool IsMoveCheck(Board b, Move m)
        {
            board.MakeMove(m);
            bool isCheck = board.IsInCheck();
            board.UndoMove(m);
            return isCheck;
        }

        bool IsMoveDraw(Board b, Move m)
        {
            board.MakeMove(m);
            bool isDraw = board.IsDraw();
            board.UndoMove(m);
            return isDraw;
        }
    }
}