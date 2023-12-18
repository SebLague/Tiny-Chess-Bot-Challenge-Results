namespace auto_Bot_618;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Bot_618 : IChessBot
{
    int MaxSearchDepth = 100;
    int MaxTime;
    Board board;
    Timer timer;

    Dictionary<ulong, int[]> transpositionTable = new();
    // [Depth, Score, Type]
    // Types:
    // 0: Exact
    // 1: Lower Bound
    // 2: Upper Bound

    Move[] killerMoves = new Move[16];
    int[,] historyTable = new int[64, 64];

    int[] PieceValue = new int[] { 0, 100, 320, 330, 500, 900, 2_000 };
    int[] PieceTypes = new int[] { 2, 3, 4, 5 };

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        int sign = board.IsWhiteToMove ? -1 : 1;
        MaxTime = timer.MillisecondsRemaining / 10;

        PriorityQueue<Move, int> orderedMoves = new();
        foreach (Move move in board.GetLegalMoves()) orderedMoves.Enqueue(move, MoveEval(move));

        Move bestMove = orderedMoves.Peek();
        for (int depth = 1; depth <= MaxSearchDepth; depth++)
        {
            // Sort moves based on the previous iteration's evaluations
            PriorityQueue<Move, int> orderedNewMoves = new();

            while (orderedMoves.TryDequeue(out Move move, out int _))
            {
                board.MakeMove(move);

                orderedNewMoves.Enqueue(move, sign * Minimax(depth - 1, int.MinValue, int.MaxValue));

                board.UndoMove(move);
            }
            if (timer.MillisecondsElapsedThisTurn > MaxTime) return bestMove;

            orderedMoves = orderedNewMoves;

            if (orderedMoves.TryPeek(out bestMove, out int score) && score <= -10_000) return bestMove;
        }
        return bestMove;
    }

    private int Minimax(int depth, int alpha, int beta)
    {
        bool white = board.IsWhiteToMove;
        int score = 0;

        if (depth <= 0 || timer.MillisecondsElapsedThisTurn > MaxTime)
        {
            if (board.IsDraw()) return 0; // Draw
            if (board.IsInCheckmate()) return white ? -100_000 : 100_000; // Checkmate

            // Calculate evaluation for position

            // Material
            foreach (PieceType pieceType in PieceTypes)
                score += PieceValue[(int)pieceType] *
                    (board.GetPieceList(pieceType, true).Count -
                    board.GetPieceList(pieceType, false).Count);

            // Evaluate king safety
            //int whiteKingSquare = board.GetKingSquare(true).Index;
            //int blackKingSquare = board.GetKingSquare(false).Index;

            if (board.IsInCheck()) score += white ? -50 : 50;

            // Calculate evaluation of pawn structure
            ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
            ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);

            // defended pawns
            score += 20 * (BitCount(whitePawns & (whitePawns << 7 | whitePawns << 9)) -
                BitCount(blackPawns & (blackPawns >> 7 | blackPawns >> 9)));

            // pawn advancement
            int[] rankValues = new int[] { 100, 100, 120, 120, 130, 140 };
            int counter = 0;
            for (ulong rankBitboard = 0xFF00; rankBitboard <= 0x00FF000000000000; rankBitboard <<= 8)
                score += rankValues[counter] * BitCount(whitePawns & rankBitboard) -
                    rankValues[5 - counter++] * BitCount(blackPawns & rankBitboard);

            ulong adjacentFiles = 0x0505050505050505;
            for (ulong fileBitboard = 0x0202020202020202; fileBitboard >= 0x0101010101010100; fileBitboard <<= 1, adjacentFiles <<= 1)
            {
                int t1 = BitCount(whitePawns & fileBitboard);
                int t2 = BitCount(blackPawns & fileBitboard);
                // doubled pawns
                score += 30 * (Math.Min(1 - t1, 0) + Math.Max(t2 - 1, 0));

                // isolated white pawns
                if ((whitePawns & adjacentFiles) == 0) score -= 20 * t1;

                // isolated black pawns
                if ((blackPawns & adjacentFiles) == 0) score += 20 * t2;
            }

            ulong whiteCopy = whitePawns;
            ulong file;
            // white passed pawns
            while (whitePawns != 0)
            {
                file = 0x8080808080808080 << BitOperations.TrailingZeroCount(whitePawns);
                if (((file << 2 & 0xFEFEFEFEFEFEFEFE | file & 0x7F7F7F7F7F7F7F7F) & blackPawns) == 0) score += 50;
                whitePawns &= whitePawns - 1;
            }

            // black passed pawns
            while (blackPawns != 0)
            {
                file = ~(0xFEFEFEFEFEFEFEFE >> 63 - BitOperations.TrailingZeroCount(blackPawns));
                if (((file & 0xFEFEFEFEFEFEFEFE | file >> 2 & 0x7F7F7F7F7F7F7F7F) & whiteCopy) == 0) score -= 50;
                blackPawns &= blackPawns - 1;
            }

            return score;
        }

        // Check transposition table
        if (transpositionTable.TryGetValue(board.ZobristKey, out var entry) && entry[0] >= depth)
        {
            score = entry[1];
            switch (entry[2])
            {
                case 1:
                    alpha = Math.Max(alpha, score);
                    break;
                case 2:
                    beta = Math.Min(beta, score);
                    break;
                default:
                    return score;
            }

            if (beta <= alpha) return score;
        }

        // Get legal moves
        Move[] legalMoves = board.GetLegalMoves();

        if (legalMoves.Length == 0)
        {
            return board.IsDraw() ? 0 : white ? -100_000 : 100_000;
        }

        // Sort legal moves
        Array.Sort(
            legalMoves.Select(move => move.Equals(killerMoves[depth]) ? int.MinValue :
            MoveEval(move) - historyTable[move.StartSquare.Index, move.TargetSquare.Index]).ToArray(),
            legalMoves
            );

        int type = white ? 1 : 2;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);

            if (move.IsCapture ||
                move.IsPromotion ||
                board.IsInCheck() ||
                (score = Minimax(depth - 2, alpha, alpha + 1)) > alpha && score < beta)
                score = Minimax(depth - 1, alpha, beta);

            //score = Minimax(depth - 1, alpha, beta);

            board.UndoMove(move);

            if (white) alpha = Math.Max(alpha, score);
            else beta = Math.Min(beta, score);

            if (beta <= alpha)
            {
                killerMoves[depth] = move; // Store the current move as the new killer move
                historyTable[move.StartSquare.Index, move.TargetSquare.Index] += depth * depth; // Increase the score for successful moves
                type = 0;
                break;
            }
        }
        transpositionTable[board.ZobristKey] = new int[] { depth, score = white ? alpha : beta, type };

        return score;
    }

    int MoveEval(Move move) => PieceValue[(int)move.MovePieceType] - PieceValue[(int)move.CapturePieceType];
    int BitCount(ulong bitboard) => BitOperations.PopCount(bitboard);
}