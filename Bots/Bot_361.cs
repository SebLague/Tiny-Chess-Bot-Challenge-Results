namespace auto_Bot_361;
using ChessChallenge.API;
using System;

public class Bot_361 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private int[] pieceCaptureValues = { 0, 100, 300, 300, 500, 900, 5000 };
    private int[] pieceLosingValuesIfAttack = { 0, 100, 300, 300, 500, 900, 2000 };
    private int[] pieceInDangerValues = { 0, 100, 300, 300, 500, 900, 2000 };

    private Board mainBoard;

    public Move Think(Board board, Timer timer)
    {
        mainBoard = board;

        (Move, int) lPointsPerMove = GetBestMovePerPoints(board.GetLegalMoves(), 3, out int lAveragePoints);

        // or select a random move
        return lPointsPerMove.Item1;
    }

    private (Move, int) GetBestMovePerPoints(Move[] lMoveToGetPointsFrom, int pDepth, out int lAveragePoints)
    {
        lAveragePoints = 0;

        (Move, int) lBestMoveWithPoints = (Move.NullMove, -999999999);
        foreach (Move lMove in lMoveToGetPointsFrom)
        {
            int lMoveValue = 0;

            // remove points if king so it does not do useless moves
            int lPieceType = (int)lMove.MovePieceType;
            if (lPieceType == 6) lMoveValue -= 1500;

            // add points if the piece will eat an opponent piece
            lMoveValue += pieceCaptureValues[(int)mainBoard.GetPiece(lMove.TargetSquare).PieceType];

            // if this move is checkmate then return it
            if (MoveIsCheckmate(lMove)) lMoveValue += 2500;

            // add points if the piece will be eaten if don't move
            if (mainBoard.SquareIsAttackedByOpponent(lMove.StartSquare))
                lMoveValue += (int)MathF.Round(pieceInDangerValues[lPieceType] * .5f);

            // remove points if the move will kill this piece
            if (mainBoard.SquareIsAttackedByOpponent(lMove.TargetSquare))
                lMoveValue -= (int)MathF.Round(pieceLosingValuesIfAttack[lPieceType] * .75f);
            // get average points of after moves if move
            else if (pDepth > 0)
            {
                mainBoard.MakeMove(lMove);

                // get and do the best opponent move
                (Move, int) lBestOpponentMove = GetBestMovePerPoints(mainBoard.GetLegalMoves(), 0, out int opponaentNextMoveAveragePoints);
                if (!lBestOpponentMove.Item1.IsNull)
                {
                    mainBoard.MakeMove(lBestOpponentMove.Item1);

                    // get the points per moves
                    (Move, int) lBestNextMoveWithPoints = GetBestMovePerPoints(mainBoard.GetLegalMoves(), pDepth - 1, out int nextMoveAveragePoints);

                    // add the average points into the move value
                    lMoveValue += (int)MathF.Round(lBestNextMoveWithPoints.Item2 * .1f);

                    mainBoard.UndoMove(lBestOpponentMove.Item1);
                }

                mainBoard.UndoMove(lMove);
            }

            lAveragePoints += lMoveValue;

            if (lMoveValue > lBestMoveWithPoints.Item2) lBestMoveWithPoints = (lMove, lMoveValue);
        }
        lAveragePoints = (int)Math.Round((float)lAveragePoints / lMoveToGetPointsFrom.Length);

        return lBestMoveWithPoints;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Move move)
    {
        mainBoard.MakeMove(move);
        bool isMate = mainBoard.IsInCheckmate();
        mainBoard.UndoMove(move);
        return isMate;
    }
}