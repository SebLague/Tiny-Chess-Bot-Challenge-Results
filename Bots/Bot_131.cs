namespace auto_Bot_131;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_131 : IChessBot
{
    private static readonly int[] pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        int colorCorrection = board.IsWhiteToMove ? 1 : -1;

        Move move = board
            .GetLegalMoves()
            .GroupBy(move => GetCandidateMOves(board, move))
            .OrderByDescending(group => group.Key)
            .First()
            .GroupBy(move => Evaluate(board, move) * colorCorrection)
            .OrderByDescending(group => group.Key)
            .First()
            .OrderBy(_ => Guid.NewGuid())
            .First();

        return move;
    }

    private int Evaluate(Board board, Move move)
    {
        int currentMaterial = MaterialCount(board);
        board.MakeMove(move);
        int material = MaterialCount(board);

        board.UndoMove(move);

        return material - currentMaterial;
    }

    private static int MaterialCount(Board board)
    {
        return board
            .GetAllPieceLists()
            .SelectMany(
                list =>
                    list.Select(
                        piece => pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 1 : -1)
                    )
            )
            .Sum();
    }

    private int GetCandidateMOves(Board board, Move move)
    {
        bool isWhiteToMove = board.IsWhiteToMove;

        board.MakeMove(move);

        bool isInCheck = board.IsInCheck();
        bool isTargetSquareDefendedByMe = board.SquareIsAttackedByOpponent(move.TargetSquare);
        bool isStartSquareDefendedByMe = board.SquareIsAttackedByOpponent(move.StartSquare);

        board.ForceSkipTurn();
        bool isTargetSquareDefendedByOpponent = board.SquareIsAttackedByOpponent(move.TargetSquare);
        board.UndoSkipTurn();

        try
        {
            if (board.IsRepeatedPosition())
            {
                return -1_000;
            }
            ;

            if (board.IsInCheckmate())
            {
                return int.MaxValue;
            }

            if (board.IsInStalemate())
            {
                return int.MinValue;
            }

            if (
                move.IsCapture
                && (
                    move.CapturePieceType >= move.MovePieceType || !isTargetSquareDefendedByOpponent
                )
            )
            {
                return int.MaxValue;
            }

            board.ForceSkipTurn();
            try
            {
                if (
                    board.SquareIsAttackedByOpponent(move.StartSquare)
                    && !board.SquareIsAttackedByOpponent(move.TargetSquare)
                )
                {
                    return int.MaxValue;
                }
                if (
                    board.SquareIsAttackedByOpponent(move.TargetSquare)
                    && !isTargetSquareDefendedByMe
                )
                {
                    return int.MinValue;
                }

                if (
                    board.SquareIsAttackedByOpponent(move.StartSquare) && !isStartSquareDefendedByMe
                )
                {
                    return int.MaxValue;
                }
            }
            finally
            {
                board.UndoSkipTurn();
            }

            if (isInCheck && !board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                return 1_000;
            }

            if (move.IsPromotion && !board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                return int.MaxValue;
            }

            if (
                move.MovePieceType == PieceType.Pawn
                && (
                    (move.TargetSquare.Rank >= 5 && isWhiteToMove)
                    || ((move.TargetSquare.Rank <= 2 && !isWhiteToMove))
                )
                && !board.SquareIsAttackedByOpponent(move.TargetSquare)
            )
            {
                return 3_000;
            }

            return 0;
        }
        finally
        {
            board.UndoMove(move);
        }
    }
}
