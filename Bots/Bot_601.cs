namespace auto_Bot_601;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_601 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    Board board;
    int multiplier => board.IsWhiteToMove ? 1 : -1;
    ulong blackBitBoard => board.BlackPiecesBitboard;
    ulong whiteBitBoard => board.WhitePiecesBitboard;

    int pieceCount => BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

    ulong opponentBitBoard => board.IsWhiteToMove ? blackBitBoard : whiteBitBoard;
    ulong myBitBoard => board.IsWhiteToMove ? whiteBitBoard : blackBitBoard;
    Move[] legalMoves => board.GetLegalMoves();
    public Move Think(Board board2, Timer timer)
    {
        board = board2;
        var result = MiniMax(4, int.MinValue, int.MaxValue, board.IsWhiteToMove);

        return result.Item2 ?? legalMoves.First();
    }

    (int, Move?) MiniMax(int depth, int alpha, int beta, bool maximizing)
    {
        if (depth == 0) return (StaticEvaluation(), null);
        int maxEval = maximizing ? int.MinValue : int.MaxValue;
        Move? bestMove = null;
        if (maximizing)
        {
            foreach (var move in legalMoves)
            {
                board.MakeMove(move);
                var eval = MiniMax(depth - 1, alpha, beta, false).Item1;
                var lastMaxEval = maxEval;
                maxEval = Math.Max(maxEval, eval);
                if (lastMaxEval != maxEval) bestMove = move;
                board.UndoMove(move);
                alpha = Math.Max(maxEval, alpha);
                if (beta <= alpha) break;
            }

            return (alpha, bestMove);
        }
        else
        {
            foreach (var move in legalMoves)
            {
                board.MakeMove(move);
                var eval = MiniMax(depth - 1, alpha, beta, true).Item1;
                var lastMaxEval = maxEval;
                maxEval = Math.Min(maxEval, eval);
                if (lastMaxEval != maxEval) bestMove = move;
                board.UndoMove(move);
                beta = Math.Min(maxEval, beta);
                if (beta <= alpha) break;
            }
            return (beta, bestMove);
        }
    }

    int OpeningAndMiddleGameEval()
    {
        var totalEval = 0;

        // This will be current side evaluation and so will need a multipler.
        int[] centerIndex = { 27, 28, 35, 36 };
        foreach (var index in centerIndex)
        {
            var square = new Square(index);
            if (board.SquareIsAttackedByOpponent(square))
            {
                totalEval -= 1;
            }

            if (legalMoves.Any(x => x.TargetSquare == square))
            {
                totalEval += 1;
            }
        }

        return totalEval;
    }

    int EndGameEval()
    {
        return multiplier * legalMoves.Length;
    }

    int StaticEvaluation()
    {
        int totalEval = 0;
        if (board.IsInCheckmate()) totalEval += 10000;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            var wPieceList = board.GetPieceList(pieceType, true);
            var bPieceList = board.GetPieceList(pieceType, false);
            if (pieceType != PieceType.None)
                totalEval += pieceValues[(int)pieceType] * (wPieceList.Count - bPieceList.Count);

            if (pieceType == PieceType.Pawn)
                totalEval += (wPieceList.Count(x => x.Square.Rank >= 4) - bPieceList.Count(x => x.Square.Rank <= 3)) * 10;
        }

        if (board.GameMoveHistory.Length <= 30)
            totalEval += OpeningAndMiddleGameEval() * multiplier;

        if (pieceCount <= 10)
            totalEval += EndGameEval() * multiplier;

        totalEval += multiplier * legalMoves.Length / 5;
        return totalEval;
    }
}