namespace auto_Bot_294;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

class Bot_294 : IChessBot
{
    Board board;
    Timer timer;
    Dictionary<ulong, TranspositionTableEntry> transpositionTable;

    public Bot_294()
    {
        for (var pv = 0; pv < unPackedPositionValues.Length; pv++)
        {
            unPackedPositionValues[pv] = new short[64];
            for (var r = 0; r < 8; r++)
                for (var c = 0; c < 4; c++)
                    unPackedPositionValues[pv][(r + 1) * 8 - c - 1] =
                    unPackedPositionValues[pv][r * 8 + c] =
                        (sbyte)(packedPositionValues[pv * 4 + r / 2] >> r % 2 * 32 + c * 8);
        }
    }

    public Move Think(Board brd, Timer tmr)
    {
        transpositionTable = new();
        board = brd;
        timer = tmr;

        for (var depth = 1; ; depth++)
            if (Search(depth, -5_000_000 /*int.MinValue*/, 5_000_000 /*int.MaxValue*/) is 100_000
                || timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 75)
                break; // Stop searching if we are running out of time
        return transpositionTable[board.ZobristKey].move;
    }

    int Search(int depth, int alpha, int beta)
    {
        // Perform terminal position evaluation
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return QuiescenceSearch(alpha, beta);

        // Check transposition table for existing entries
        var hash = board.ZobristKey;
        if (transpositionTable.TryGetValue(hash, out var tt) && tt.depth >= depth)
        {
            if (tt.nodeType == 0 /*NodeType.Exact*/)
                return tt.score;
            if (tt.nodeType == -1 /*NodeType.LowerBound*/)
                alpha = Math.Max(alpha, tt.score);
            else if (tt.nodeType == 1 /*NodeType.UpperBound*/)
                beta = Math.Min(beta, tt.score);

            if (alpha >= beta)
                return tt.score;
        }

        var originalAlpha = alpha; // Store the original alpha value
        var bestScore = -5_000_000; // int.MinValue;
        var bestMove = Move.NullMove;

        foreach (var nextMove in board.GetLegalMoves()
                                .OrderByDescending(x => x.IsCapture ? x.CapturePieceType : PieceType.None)
                                .ThenBy(x => x.IsCapture ? x.MovePieceType : PieceType.King)
                                .ThenByDescending(x => x.IsCastles)
                                .ThenByDescending(x => x.IsPromotion ? x.PromotionPieceType : PieceType.None)
                                .ThenBy(x => x.MovePieceType)
                                )
        {
            board.MakeMove(nextMove);
            var score = -Search(depth - 1, -beta, -alpha);
            board.UndoMove(nextMove);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = nextMove;
            }

            if (score > alpha)
                alpha = score;
            if (alpha >= beta)
                break;
        }

        // Update transposition table with the best score
        transpositionTable[hash] = new TranspositionTableEntry(bestScore, depth,
            bestScore <= originalAlpha
                ? 1 /*NodeType.UpperBound*/
                : bestScore >= beta
                    ? -1 /*NodeType.LowerBound*/
                    : 0 /*NodeType.Exact*/
            , bestMove);

        return bestScore;
    }

    int QuiescenceSearch(int alpha, int beta)
    {
        var score = Evaluate();

        if (score >= beta)
            return beta; // Perform cutoff if the standPat score is high
        if (alpha < score)
            alpha = score; // Raise the lower bound if standPat score is higher

        foreach (var move in board.GetLegalMoves(true)
                                .OrderByDescending(x => x.CapturePieceType)
                                .ThenBy(x => x.MovePieceType)
                                )
        {
            board.MakeMove(move);
            score = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta; // Cutoff
            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    int Evaluate()
    {
        if (board.IsInCheckmate())
            return -100_000;

        if (board.IsDraw())
            return 0;

        var totalEvaluation = 0;

        // Both sides have no queens or
        // Every side which has a queen has additionally no other pieces or one minor piece maximum.
        var bq = HasQueen(false);
        var wq = HasQueen(true);
        var endGame = !bq && !wq || bq && HasLessMajorPieces(false) || wq && HasLessMajorPieces(true);

        if (board.IsInCheck())
            totalEvaluation -= 5 + (8 - board.GetLegalMoves().Count(x => x.MovePieceType == PieceType.King)) * 10;

        var multiplier = board.IsWhiteToMove ? 1 : -1;

        for (var pieceType = 1 /* Pawn */; pieceType <= 6 /* King */; pieceType++)
        {
            var pieceTypeEnum = (PieceType)pieceType;
            var whiteBoard = board.GetPieceBitboard(pieceTypeEnum, true);
            var blackBoard = board.GetPieceBitboard(pieceTypeEnum, false);

            var whiteCount = BitOperations.PopCount(whiteBoard);
            var blackCount = BitOperations.PopCount(blackBoard);

            if (whiteCount == 0 && blackCount == 0) continue;

            totalEvaluation += multiplier * pieceValues[pieceType] * (whiteCount - blackCount);

            var unPV = unPackedPositionValues[pieceType - (endGame && pieceTypeEnum is PieceType.King ? 0 : 1)];
            var bonus = endGame && pieceTypeEnum is PieceType.Pawn ? pawnBonus : noBonus;

            while (whiteBoard != 0)
            {
                var squareIndex = BitOperations.TrailingZeroCount(whiteBoard) ^ 0b111000;
                totalEvaluation += multiplier * (unPV[squareIndex] + bonus[squareIndex >> 3]);
                whiteBoard &= whiteBoard - 1;
            }

            while (blackBoard != 0)
            {
                var squareIndex = BitOperations.TrailingZeroCount(blackBoard);
                totalEvaluation -= multiplier * (unPV[squareIndex] + bonus[squareIndex >> 3]);
                blackBoard &= blackBoard - 1;
            }
        }

        return totalEvaluation;

        bool HasQueen(bool isWhite) => board.GetPieceBitboard(PieceType.Queen, isWhite) != 0;

        bool HasLessMajorPieces(bool isWhite) =>
            BitOperations.PopCount(
                    board.GetPieceBitboard(PieceType.Bishop, isWhite)
                    | board.GetPieceBitboard(PieceType.Knight, isWhite)
                    | board.GetPieceBitboard(PieceType.Rook, isWhite)) <= 1;
    }

    record TranspositionTableEntry(int score, int depth, int /*NodeType*/ nodeType, Move move);
    // enum NodeType { Exact, UpperBound, LowerBound }
    static readonly short[] pieceValues = { 0, 200, 640, 660, 1000, 1800, 0 };
    static readonly short[] pawnBonus = { 0, 160, 100, 60, 40, -60, -100, 0 };
    static readonly short[] noBonus = { 0, 0, 0, 0, 0, 0, 0, 0 };
    static readonly short[][] unPackedPositionValues = new short[7][];
    static readonly ulong[] packedPositionValues =
    { 
        // pawns
        0x3232323200000000, 0x190A05051E140A0A,0x00F6FB0514000000, 0x00000000EC0A0A05,
        // knights
        0x0000ECD8E2E2D8CE, 0x140F05E20F0A00E2, 0x0F0A05E2140F00E2,0xE2E2D8CE0500ECD8,
        // bishops
        0x000000F6F6F6F6EC, 0x0A0505F60A0500F6, 0x0A0A0AF60A0A00F6, 0xF6F6F6EC000005F6,
        // rooks
        0x0A0A0A0500000000, 0x000000FB000000FB, 0x000000FB000000FB, 0x05000000000000FB,
        // queens
        0x000000F6FBF6F6EC, 0x050500FB050500F6, 0x050500F6050500FB, 0xFBF6F6EC000000F6,
        // kings middle game
        0xCED8D8E2CED8D8E2, 0xCED8D8E2CED8D8E2, 0xECECECF6D8E2E2EC, 0x000A1E1400001414,
        // kings end game
        0x00F6ECE2ECE2D8CE, 0x281EF6E21E14F6E2, 0x1E14F6E2281EF6E2, 0xE2E2E2CE0000E2E2
    };
}
