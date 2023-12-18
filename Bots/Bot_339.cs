namespace auto_Bot_339;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_339 : IChessBot
{
    private List<ulong> m_ZobristKey = new();

    private int m_WhitePieceSum = 0;
    private int m_BlackPieceSum = 0;

    public Move Think(Board board, Timer timer)
    {
        int depth = Math.Min(10, Math.Max(3, GetDepthAccordingToTimeAndMoveCount(timer.MillisecondsRemaining / 1000, board.GameMoveHistory.Length)));
        Move m = Calculate(board, int.MinValue, int.MaxValue, depth, 16).Move ?? Move.NullMove;

        if (m == Move.NullMove)
            m = board.GetLegalMoves()[0];

        m_ZobristKey.Add(board.ZobristKey);

        return m;
    }

    // Lord forgive me for my sins

    private int GetDepthAccordingToTimeAndMoveCount(int secondsRemaining, int moveCount) => moveCount < 6 ? 4 : secondsRemaining > 3600 ? 8 : secondsRemaining > 1800 ? 7 : secondsRemaining > 900 ? 6 : secondsRemaining < 10 ? 4 : 5;

    (int Score, Move? Move) Calculate(Board board, int α, int β, int depth, int numEstimations)
    {
        if (depth == 0)
            return (Evaluate(board, false), null);

        bool isWhite = board.IsWhiteToMove;
        (int score, Move? move) candidate = (isWhite ? int.MinValue : int.MaxValue, Move.NullMove);
        var legalMoves = board.GetLegalMoves(true).Concat(board.GetLegalMoves().Except(board.GetLegalMoves(true)));

        foreach (var legalMove in legalMoves)
        {
            board.MakeMove(legalMove);

            if (board.IsInCheckmate())
            {
                candidate.score = isWhite ? int.MaxValue : int.MinValue;
                candidate.move = legalMove;
                board.UndoMove(legalMove);

                return candidate;
            }

            int estimation = (board.IsInCheck() || legalMove.IsPromotion) && numEstimations < 16 ? 1 : 0;

            var GetBestMove = Calculate(board, α, β, depth - 1 + estimation, numEstimations + estimation);

            board.UndoMove(legalMove);


            if (isWhite ? GetBestMove.Score > candidate.score : GetBestMove.Score < candidate.score)
            {
                candidate.score = GetBestMove.Score;
                candidate.move = legalMove;
            }

            if (m_ZobristKey.Contains(board.ZobristKey) || board.FiftyMoveCounter >= 50)
                candidate.score = 0;

            if (isWhite ? candidate.score >= β : α >= candidate.score)
                break;

            if (isWhite) α = Math.Max(α, candidate.score); else β = Math.Min(β, candidate.score);
        }

        return candidate;
    }

    public int Evaluate(Board b, bool debug = false)
    {
        // Who doesn't hate stalemate?
        if (b.IsInStalemate())
            return b.IsWhiteToMove ? int.MinValue : int.MaxValue;
        if (b.IsInCheckmate())
            return b.IsWhiteToMove ? int.MaxValue : int.MinValue;

        m_WhitePieceSum = GetPieceListSum(b.GetAllPieceLists(), true, b);
        m_BlackPieceSum = GetPieceListSum(b.GetAllPieceLists(), false, b);

        return m_WhitePieceSum + m_BlackPieceSum;
    }

    private int GetPieceListSum(PieceList[] pieceLists, bool isWhitePieceList, Board b)
    {
        return pieceLists.Where(x => x.IsWhitePieceList == isWhitePieceList).Sum(list =>
        list?.Sum(p =>
        {
            var w = isWhitePieceList ? 1 : -1;
            return p.PieceType switch
            {
                PieceType.Pawn => (100 + EvaluatePawnPosition(p.Square, isWhitePieceList)) * w,
                PieceType.Rook => 500 * w,
                PieceType.Bishop => 330 * w,
                PieceType.Knight => (320 + EvaluateKnightPosition(p.Square)) * w,
                PieceType.Queen => 900 * w,
                PieceType.King => (20000 + EvaluateKingPosition(p.Square, b.IsWhiteToMove, b.HasKingsideCastleRight(isWhitePieceList))) * w,
            };
        }) ?? 0);
    }

    private int EvaluateKingPosition(Square square, bool isWhite, bool hasKingsideCastleRight)
    {
        if ((isWhite && square.Rank == 0 && square.File == 6) || (!isWhite && square.Rank == 7 && square.File == 1))
            return 20;
        if (hasKingsideCastleRight)
            return -20;
        return -20;
    }

    private int EvaluateKnightPosition(Square square)
    {
        if (square.File == 0 || square.File == 7)
            return -30;
        return (int)(10 / Math.Abs(4.5 - square.File)) + (int)(10 / Math.Abs(4.5 - square.Rank));
    }

    private int EvaluatePawnPosition(Square square, bool isWhite)
    {
        if (square.Rank == 1 && isWhite && square.File > 4 || (square.Rank == 6 && !isWhite && square.File < 3))
            return 30;
        else if (square.Rank > 0 && isWhite || (square.Rank < 7 && !isWhite))
            return isWhite ? square.Rank * 10 : (7 - square.Rank) * 10;
        return 0;
    }
}
