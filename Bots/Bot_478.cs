namespace auto_Bot_478;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_478 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 0 };

    Dictionary<ulong, (int Score, int Depth, int Flag)> ttable = new();

    public Move Think(Board board, Timer timer)
    {
        var rng = new Random();
        var bestMove = Move.NullMove;

        // int numEvals = 0; // #DEBUG
        Negamax(0, -1_000_000_000, 1_000_000_000, board.IsWhiteToMove ? 1 : -1);
        // DivertedConsole.Write($"NewBot (numEvals): {numEvals}\t| score: {result}\t| ttable.Count: {ttable.Count}\t| hard: {HardLimit()} soft: {SoftLimit()}"); // #DEBUG
        return bestMove;

        bool SoftLimit() => timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 15;
        bool HardLimit() => timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 10;

        int Negamax(int depth, int alpha, int beta, int color)
        {
            if (board.IsDraw()) return -9;
            if (board.IsInCheckmate()) return -1_000_000_000 + depth;

            var alphaOrig = alpha;
            var qSearch = depth > (timer.MillisecondsRemaining > 15_000 ? 5 : 4)/*Q_SEARCH_DEPTH*/ || HardLimit();

            if (depth > 0 && ttable.TryGetValue(board.ZobristKey, out var entry) && entry.Depth <= depth)
            {
                if (entry.Flag == 0 /*EXACT*/) return entry.Score;
                if (entry.Flag == 1 /*LOWERBOUND*/) alpha = Math.Max(alpha, entry.Score);
                if (entry.Flag == 2 /*UPPERBOUND*/) beta = Math.Min(beta, entry.Score);
                if (alpha >= beta) return entry.Score;
            }
            if (depth > 0 && qSearch)
            {
                var standPat = Evaluate() * color;
                if (standPat >= beta) return beta;
                alpha = Math.Max(alpha, standPat);
                if (SoftLimit()) return alpha;
            }

            var value = qSearch ? alpha : -1_000_000_000;

            foreach (var nextMove in board
                         .GetLegalMoves(qSearch)
                         .OrderByDescending(m =>
                             (m.IsCapture || m.IsPromotion
                                 ? pieceValues[(int)m.CapturePieceType] - pieceValues[(int)m.MovePieceType] + 2000
                                 : 0) + rng.Next(100)
                             )
                     )
            {
                board.MakeMove(nextMove);
                var score = -Negamax(depth + 1, -beta, -alpha, -color);
                board.UndoMove(nextMove);

                if (qSearch && score >= beta)
                    return beta;

                if (score > value)
                {
                    if (depth == 0) bestMove = nextMove; // at the root of the tree, update the best move
                    value = score;
                }

                alpha = Math.Max(alpha, value);
                if (alpha >= beta) break;
            }

            if (qSearch) value = alpha;

            ttable[board.ZobristKey] = (
                value,
                depth,
                value <= alphaOrig
                    ? 2 /*UPPERBOUND*/
                    : value >= beta
                        ? 1 /*LOWERBOUND*/
                        : 0 /*EXACT*/
                );
            return value;
        }

        int Evaluate()
        {
            // numEvals++; // #DEBUG
            return Enum.GetValues<PieceType>().Sum(pieceType =>
                (
                    BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceType, true))
                    - BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(pieceType, false))
                ) * pieceValues[(int)pieceType]
            );
        }
    }
}