namespace auto_Bot_37;
using ChessChallenge.API;
using System;

public class Bot_37 : IChessBot
{
    private Move _lastMove;
    private bool _isWhite;
    public Move Think(Board board, Timer timer)
    {
        _isWhite = board.IsWhiteToMove;
        _lastMove = PlayHighestRatedMove(board, 2, out _);
        return _lastMove;
    }

    private Move PlayHighestRatedMove(Board board, int depth, out int score)
    {
        score = int.MinValue;
        Move bestMove = Move.NullMove;
        var moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            int moveScore = GetMoveScore(move, board);
            if (depth > 0)
            {
                board.MakeMove(move);
                PlayHighestRatedMove(board, depth - 1, out var childScore);
                childScore = -childScore;
                board.UndoMove(move);

                if (moveScore + childScore > score)
                {
                    score = moveScore + childScore;
                    bestMove = move;
                }
            }
            else
            {
                if (moveScore > score)
                {
                    score = moveScore;
                    bestMove = move;
                }
            }
        }

        return bestMove;
    }

    private int GetMoveScore(Move move, Board board)
    {
        int score = 0;
        if (move.IsCapture)
        {
            score += GetCaptureRating(move.CapturePieceType);
        }

        if (move.MovePieceType == PieceType.King)
        {
            score -= 50;
        }

        if (move.MovePieceType == _lastMove.MovePieceType)
        {
            score -= 30;
        }

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            score -= GetCaptureRating(move.MovePieceType);
        }

        if (move.IsEnPassant)
        {
            score += 10000; // r/AnarchyChess ftw
        }

        score += GetSquareScore(move, board);

        return score;
    }

    private int GetSquareScore(Move move, Board board)
    {
        int score = 0;
        if (move.IsPromotion)
        {
            score += 50;
        }

        if (move.TargetSquare == _lastMove.StartSquare)
        {
            score -= 30;
        }

        score += GetDistanceToKingScore(move, board);

        return score;
    }

    private int GetDistanceToKingScore(Move move, Board board)
    {
        var kingSquare = board.GetKingSquare(!_isWhite);

        int distance = GetDistance(move.TargetSquare, kingSquare);
        int score = 20 - distance;

        return score;
    }

    private int GetDistance(Square moveTargetSquare, Square kingSquare)
    {
        int xDistance = moveTargetSquare.File - kingSquare.File;
        int yDistance = moveTargetSquare.Rank - kingSquare.Rank;
        return Math.Abs(xDistance + yDistance);
    }


    private int GetCaptureRating(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.Pawn:
                return 10;
            case PieceType.Knight:
                return 30;
            case PieceType.Bishop:
                return 30;
            case PieceType.Rook:
                return 50;
            case PieceType.Queen:
                return 100;
            case PieceType.King:
                return 500;
            default:
                return 0;
        }
    }
}