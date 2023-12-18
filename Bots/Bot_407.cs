namespace auto_Bot_407;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_407 : IChessBot
{

    static readonly int[] PieceValue = new int[] { 0, 300, 915, 1000, 1690, 2850 };

    readonly Dictionary<int, (short score, byte state)> transpositionTable = new();

    public Move Think(Board board, Timer timer)
    {
        if (board.GameRepetitionHistory.Length < 3) transpositionTable.Clear();

        Move moveToPlay = default;

        uint Depth = 6;
        bool ceaseSearch = false;

        do searchMove(++Depth | 128, -20000, 20000, true, false);
        while (Depth < 64 && !ceaseSearch);
        return moveToPlay;



        int searchMove(uint depth, int alpha, int beta, bool setMove, bool extended)
        {
            if (board.IsInCheckmate()) return -20000;
            if (depth == 0) return 0;
            if (board.IsDraw()) return -alpha / 16;

            depth--;
            //
            // look up
            //
            int key = board.ZobristKey.GetHashCode();
            if (transpositionTable.TryGetValue(key, out var entry) &&
                (entry.state & 63U) >= depth &&
                !board.IsRepeatedPosition())
                if (entry.state < 64U || entry.state < 128U && (alpha = Math.Max(alpha, entry.score)) >= beta) return entry.score;
                else if (entry.state > 127U && alpha >= (beta = Math.Min(beta, entry.score))) return alpha;

            extended = extended || depth > 3 && board.IsInCheck();
            bool isQuiescence = depth < (extended ? 2U : 4U);

            Span<Move> moves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref moves, isQuiescence);
            int moveCount = moves.Length;
            if (moveCount == 0) return 0;
            MemoryExtensions.Sort(moves, static (lhs, rhs) => rhs.CapturePieceType - lhs.CapturePieceType + rhs.PromotionPieceType - lhs.PromotionPieceType);

            //
            // evaluation
            //

            int branchAlpha = -20000;
            if (isQuiescence) alpha = 0;
            if (setMove)
            {
                if (moveToPlay.RawValue == 0) moveToPlay = moves[0];
                else alpha = moveScore(moveToPlay);
                if (moveCount == 1) return 0;
            }

            int moveScore(Move move)
            {
                board.MakeMove(move);
                int score = moveCount + PieceValue[(int)move.CapturePieceType] + PieceValue[(int)move.PromotionPieceType] / 2;
                score -= searchMove(depth & 63, score - beta, score - alpha, false, extended);
                board.UndoMove(move);
                return score;
            }

            foreach (Move move in moves)
            {
                int score = moveScore(move);

                if (ceaseSearch) return alpha;
                ceaseSearch = timer.MillisecondsElapsedThisTurn * Math.Max(120 - board.PlyCount / 2, 30) > timer.MillisecondsRemaining;


                if (score > branchAlpha && (branchAlpha = score) > alpha)
                {
                    if (setMove) moveToPlay = move;


                    // state < 64       : excat
                    // 63 < state < 128 : lower bound
                    // 127 < state      : upper bound

                    alpha = score;
                    depth |= 128;

                    if (score >= beta)
                    {
                        alpha = 20000;
                        depth &= 127;
                        depth |= 64;
                        break;
                    }
                }
            }

            //
            // update transposition table
            //
            if (!isQuiescence)
            {

                entry.score = (short)branchAlpha;
                entry.state = (byte)depth;
                if (transpositionTable.ContainsKey(key)) transpositionTable[key] = entry;
                else if (transpositionTable.Count < 32700) transpositionTable.Add(key, entry);

            }
            return alpha;
        }

    }


}