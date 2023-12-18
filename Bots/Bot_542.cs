namespace auto_Bot_542;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_542 : IChessBot
{
    private readonly Dictionary<PieceType, int> piecePoints = new Dictionary<PieceType, int>
    {
        [PieceType.Pawn] = 1,
        [PieceType.Knight] = 3,
        [PieceType.Bishop] = 3,
        [PieceType.Rook] = 5,
        [PieceType.Queen] = 9,
        [PieceType.King] = 9
    };
    private readonly Random rand = new Random(0);

    public Move Think(Board board, Timer timer)
    {
        var legalMoves = board.GetLegalMoves();
        Move bestMove = legalMoves[0];
        float bestMoveScore = this.CalculateMoveScore(board, bestMove);

        foreach (var move in legalMoves)
        {
            float moveScore = this.CalculateMoveScore(board, move);
            if (moveScore > bestMoveScore)
            {
                bestMove = move;
                bestMoveScore = moveScore;
            }
        }

        DivertedConsole.Write(bestMove + ", Score: " + bestMoveScore);
        return bestMove;
    }

    private float CalculateMoveScore(Board board, Move move)
    {
        board.MakeMove(move);

        if (board.IsDraw())
        {
            board.UndoMove(move);
            return -10000;
        }

        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return 10000;
        }

        board.UndoMove(move);

        bool squareIsAttackedByOpponent = board.SquareIsAttackedByOpponent(move.StartSquare);
        bool targetSquareIsAttackedByOpponent = board.SquareIsAttackedByOpponent(move.TargetSquare);
        bool squareIsDefended = this.IsSquareDefended(board, move.StartSquare);
        bool targetSquareDefendedAfterMove = this.IsTargetSquareDefendedAfterMove(board, move);
        if (
            squareIsAttackedByOpponent
            && !squareIsDefended
            && (!targetSquareIsAttackedByOpponent || targetSquareDefendedAfterMove)
        )
        {
            return 400
                + (move.IsCapture ? this.piecePoints[move.CapturePieceType] : 0)
                - (targetSquareIsAttackedByOpponent ? this.piecePoints[move.MovePieceType] * 5 : 0)
                - (
                    Math.Abs(4 - move.TargetSquare.Rank)
                    + Math.Abs(4 - move.TargetSquare.File)
                    - Math.Abs(4 - move.StartSquare.Rank)
                    - Math.Abs(4 - move.StartSquare.File)
                ) / 100f;
        }

        if (move.IsCapture && !board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            return 300 + this.piecePoints[move.MovePieceType];
        }

        if (
            move.IsCapture
            && this.piecePoints[move.CapturePieceType] > this.piecePoints[move.MovePieceType]
        )
        {
            return 200
                + this.piecePoints[move.CapturePieceType]
                - this.piecePoints[move.MovePieceType];
        }

        if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            board.MakeMove(move);
            if (board.IsInCheck())
            {
                board.UndoMove(move);
                return 100 + rand.Next(5);
            }
            board.UndoMove(move);

            return 0
                - (
                    move.MovePieceType == PieceType.Pawn
                        ? 0
                        : (
                            Math.Abs(4 - move.TargetSquare.Rank)
                            + Math.Abs(4 - move.TargetSquare.File)
                            - Math.Abs(4 - move.StartSquare.Rank)
                            - Math.Abs(4 - move.StartSquare.File)
                        ) / 100f
                )
                - (move.MovePieceType == PieceType.King ? 100 : 0)
                - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 100 : 0)
                - rand.Next(100) / 100f;
        }

        return -10000;
    }

    private bool IsSquareDefended(Board board, Square square)
    {
        if (board.TrySkipTurn())
        {
            if (board.SquareIsAttackedByOpponent(square))
            {
                board.UndoSkipTurn();
                return true;
            }
            board.UndoSkipTurn();
        }
        return false;
    }

    private bool IsTargetSquareDefendedAfterMove(Board board, Move move)
    {
        board.MakeMove(move);
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            board.UndoMove(move);
            return true;
        }
        board.UndoMove(move);
        return false;
    }
}