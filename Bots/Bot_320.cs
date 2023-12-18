namespace auto_Bot_320;
using ChessChallenge.API;
using System;

public class Bot_320 : IChessBot
{

    bool isfirstMove = true;
    bool hasCastled = false;
    Move lastLastMove = Move.NullMove;
    bool simplify = false;
    int gameMoves = 0;
    int llc = 0;
    ChessChallenge.API.PieceType lastMovedPiece = PieceType.Knight;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Move bestMove = moves[0];
        double cBestMoveRating = 0;
        bool willBe50Move = false;
        bool isCaptureInCycle = false;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            Piece movedPiece = board.GetPiece(move.TargetSquare);

            DivertedConsole.Write("Piece: " + movedPiece);

            double moveRating = 0;

            if (move.IsPromotion)
            {
                moveRating = moveRating + 9;
            }

            if (move.IsCastles)
            {
                moveRating = moveRating + 1;
            }

            if (board.IsInCheckmate())
            {
                moveRating = moveRating + 999999;
            }

            if (lastLastMove == move)
            {
                moveRating = moveRating - 0.1;
            }

            if (board.IsInStalemate())
            {
                moveRating = -99;
            }

            if (board.HasKingsideCastleRight(board.IsWhiteToMove))
            {
                moveRating = moveRating + 0.8;
            }

            if (board.HasQueensideCastleRight(board.IsWhiteToMove))
            {
                moveRating = moveRating + 0.8;
            }

            if (!board.HasKingsideCastleRight(!board.IsWhiteToMove))
            {
                moveRating = moveRating + 0.3;
            }

            if (!board.HasQueensideCastleRight(!board.IsWhiteToMove))
            {
                moveRating = moveRating + 0.3;
            }

            if (board.IsFiftyMoveDraw())
            {
                willBe50Move = true;
            }

            if (willBe50Move)
            {
                if (move.IsCapture)
                {
                    moveRating = moveRating + 3;
                }
                if (move.MovePieceType == PieceType.Pawn)
                {
                    moveRating = moveRating + 2;
                }
            }

            if (board.IsRepeatedPosition())
            {
                moveRating = moveRating - 0.1;
            }

            int mcount = 0;
            int tmcount = 0;
            int pcount = 0;

            if (move.MovePieceType != PieceType.King)
            {
                foreach (Move amove in board.GetLegalMoves())
                {
                    tmcount = tmcount + 1;
                    if (amove.MovePieceType == move.MovePieceType && !board.SquareIsAttackedByOpponent(move.TargetSquare))
                    {
                        mcount = mcount + 1;
                        pcount = pcount + 1;
                    }
                }

                if (pcount > 0)
                {

                    if (move.MovePieceType == PieceType.Knight)
                    {
                        moveRating = moveRating + mcount;
                    }

                    moveRating = moveRating + mcount / pcount;
                }
            }

            int checkValuePT(PieceType piece)
            {
                if (piece == PieceType.Queen)
                {
                    return 9;
                }
                if (piece == PieceType.Bishop)
                {
                    return 4;
                }
                if (piece == PieceType.Knight)
                {
                    if (isfirstMove)
                    {
                        return 1;
                    }
                    return 3;
                }
                if (piece == PieceType.Rook)
                {
                    return 5;
                }
                if (piece == PieceType.Pawn)
                {
                    return 1;
                }
                if (piece == PieceType.King)
                {
                    return 0;
                }
                return 0;
            }

            if (board.IsInsufficientMaterial())
            {
                moveRating = moveRating - 100;
            }

            if (move.TargetSquare.File == 3 || move.TargetSquare.File == 4 || move.TargetSquare.File == 5)
            {
                moveRating = moveRating + 0.02;
            }

            if (move.MovePieceType == PieceType.Pawn)
            {
                if (board.IsWhiteToMove)
                {
                    if (move.TargetSquare.Rank >= 6)
                    {
                        if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                        {
                            moveRating = moveRating + move.TargetSquare.Rank;
                        }
                    }
                    else
                    {
                        if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                        {
                            moveRating = moveRating + 1.0 + (move.TargetSquare.Rank / 100);
                        }
                    }
                }
            }

            if (lastMovedPiece == move.MovePieceType)
            {
                moveRating = moveRating - 1;
            }

            if (move.MovePieceType == PieceType.Queen && move.TargetSquare.File == 2 || move.TargetSquare.File == 4)
            {
                moveRating = moveRating + 0.008;
            }

            if (hasCastled == false && move.MovePieceType == PieceType.King)
            {
                moveRating = moveRating - 3;
            }

            board.UndoMove(move);

            if (board.IsInCheck())
            {
                int lastLowestVal = 0;
                int movedPieceVal = checkValuePT(move.MovePieceType);

                if (move.IsCapture)
                {
                    int capturedPieceVal = checkValuePT(move.CapturePieceType);

                    if (capturedPieceVal >= movedPieceVal)
                    {
                        moveRating = 100;
                    }
                }

                if (movedPieceVal == 0)
                {
                    movedPieceVal = 9;
                }

                if (movedPieceVal < lastLowestVal)
                {
                    lastLowestVal = 0;
                    //moveRating = moveRating +(64 - lastLowestVal);
                }
            }

            if (move.IsPromotion)
            {
                if (move.PromotionPieceType == PieceType.Queen)
                {
                    moveRating = moveRating + 9;
                }
            }

            if (move.IsCapture)
            {
                int capturedPieceVal = checkValuePT(move.CapturePieceType);
                int movedPieceVal = checkValuePT(move.MovePieceType);

                if (capturedPieceVal > movedPieceVal)
                {
                    moveRating = moveRating + ((capturedPieceVal - movedPieceVal) * 1.25);
                }

                if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    moveRating = moveRating + capturedPieceVal;
                }
            }

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveRating = moveRating - checkValuePT(move.MovePieceType);
            }

            if (board.SquareIsAttackedByOpponent(move.StartSquare) && !board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveRating = moveRating + checkValuePT(move.MovePieceType);
            }

            //set best move
            if (moveRating > cBestMoveRating)
            {
                cBestMoveRating = moveRating;
                bestMove = move;
            }
        }

        if (board.IsInCheckmate() || board.IsDraw())
        {
            isfirstMove = true;
            hasCastled = false;
            gameMoves = 0;
        }

        DivertedConsole.Write("Best Eval'd: " + cBestMoveRating);
        DivertedConsole.Write("Moved Piece: " + bestMove.MovePieceType);
        DivertedConsole.Write(bestMove);

        willBe50Move = false;
        isfirstMove = false;

        llc += 1;
        if (llc == 2)
        {
            lastLastMove = bestMove;
            llc = 0;
        }
        lastMovedPiece = bestMove.MovePieceType;

        gameMoves = gameMoves + 1;

        if (bestMove.IsCastles)
        {
            hasCastled = true;
        }

        //play move
        return bestMove;
    }
}