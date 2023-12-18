namespace auto_Bot_595;
// https://github.com/sparemind/myte
// 1016/1024 tokens
// (Additional helper functions and comments stripped for submission size)

using ChessChallenge.API;
using System;

public class Bot_595 : IChessBot
{
    // Transposition table. Entries are ~32 bytes each based on the GC report
    // (hash, bestMove, depth, score, nodeType)
    readonly (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[4194304]; // 2^22 gives ~128 MB of TT space

    public Move Think(Board board, Timer timer)
    {
        // Piece material values
        int[] staticData = { 0, 100, 335, 343, 436, 1037, 0, 125, 213, 242, 457, 730, 0 };
        var dynamicData = new int[8192]; // 8 * 1024
        var killerMoves = new Move[128];
        var counterMoves = new Move[8192]; // 64x64x2 table

        int _i, _offset; // Global variables reused to save tokens

        void PopulateData()
        {
            // initData consists of chunks of six 64-bit bitmaps, one for each PieceType/GamePhase combination.
            // Each one is a losslessly compressed representation of a piece-square table or other constant. Each
            // bitmap n represents the n-th bit of the bitboard. An additional table is included at the start,
            // which provides the baseline offsets for each subsequent PST plus misc. constants.
            ulong[] initData =
            {
                0x0000a841c30338f2, 0x000195e18704359c, 0x0000690047880794, 0x00017c00040017ae, 0x0000300000001492, 0x00014c0000002bec, // baselines + constants
                0xffd2e70f398deeff, 0xfff44d31ae15f9ff, 0xff31ebb3b79f9fff, 0x00413135a6070f00, 0x0086beb6a778ff00, 0xff60404858ffffff, // midgame pawn
                0x6837c2d7387c1b00, 0x49eec3c03d6ae712, 0x41dd2a547dd77d83, 0x8026ae73fce23d7c, 0xb5f92cc2e5c3feff, 0x0000503c3a3c0000, // midgame knight
                0x8dc18390666da000, 0x9e955c639a8afbff, 0xcf62c24cedc92808, 0x1395fb0a76ac73f7, 0xc01ffbdbcf0dfeff, 0x0060043c38f20000, // midgame bishop
                0x29d422d20ea0ccff, 0xb2dc94f13491a9e0, 0xb68c8cb587991d3b, 0x81b412b941e33e33, 0x382463c2397dfccc, 0xff7afc7cfefeffff, // midgame rook
                0x01f7fb28cb30ebb5, 0x010b393fe7b8a376, 0x4bfcd9b90d4f1dee, 0x0fbfd649c5ac8bf6, 0xd83c9f0e36f600e7, 0x000060f0f8f8fc18, // midgame queen
                0xcf5ceba8e9a45200, 0x13dc187d8fe65134, 0xdf089882fe5faec0, 0x92d0888080000000, 0x5b28988080000000, 0xc6c7677f7fffffff, // midgame king
                0xff1cd8e195fb67ff, 0xff1f972130eee6ff, 0x00ac6b86280c2200, 0x0053004184ea8f00, 0xff00000043e8ffff, 0x000000000017ff00, // endgame pawn
                0xe1fdd3ffcb517966, 0x6602372593095c99, 0x32db0431dd319619, 0x51a9932fe76add81, 0x807074e05ac63e7e, 0x0000081c2c3c0000, // endgame knight
                0xa186c2116a00c8f0, 0xc01725b228c6c9f3, 0x296ded8161a40ae1, 0x019958b6203bf614, 0xd22ea373619fffff, 0x00001c0c9e600000, // endgame bishop
                0x5895b23d2f624716, 0x02d6709280786068, 0x4f75d0e00427a543, 0x44ebafcfc0801a41, 0x3f3e7e403f7fffbe, 0x0000013fffffffff, // endgame rook
                0x25ee61e412b5e326, 0xa0a861a4c1686aa3, 0x54971f59c38be727, 0x55093f21f5d407e3, 0x03f73f0939391bfa, 0x0000c0fefefefc1c, // endgame queen
                0xec1bb25ff8cc3c77, 0x10594f396516ef41, 0x39ab7985f45f7e3b, 0x440a9977efdf38f2, 0x1cb7b5bdbe64e6d8, 0x207c7e7e7fffffe4 //  endgame king
            };

            // bitmapIdx / 6 is the PieceType, bitmapIdx % 6 is the bit index
            for (var bitmapIdx = 0; bitmapIdx < 78; bitmapIdx++)
            {
                for (_i = 0, _offset = dynamicData[bitmapIdx / 6]; _i < 64 && bitmapIdx % 6 == 0 && bitmapIdx / 6 != 0;)
                    dynamicData[bitmapIdx / 6 * 64 + _i++] = _offset - 68;
                for (_i = 0; _i < 64; _i++)
                    dynamicData[bitmapIdx / 6 * 64 + _i] +=
                        BitboardHelper.SquareIsSet(initData[bitmapIdx], new Square(_i)) ? 1 << (bitmapIdx % 6) : 0; // Modified to not use illegal namespace -- Seb
            }
        }

        if (dynamicData[1] == 0) PopulateData();

        var nodes = 0;
        bool stop = false, // Early exit flag if over time limit
            canNMP = true; // Can try null move pruning
        Move bestMove = default, candidateBestMove = default;
        int phase, mg, eg, limit = timer.MillisecondsRemaining / 20; // TODO inline if not reused

        // Negamax Alpha-Beta Pruning
        int search(int ply, int remainingDepth, int alpha, int beta)
        {
            // Check timer and exit early if past time limit
            if ((++nodes & 2047) == 0 && bestMove != default && timer.MillisecondsElapsedThisTurn > limit)
                stop = true;
            if (stop) return 0;

            var inCheck = board.IsInCheck();
            if (inCheck) remainingDepth++; // Don't want to enter quiescence search if in check

            // QUIESCENCE SEARCH is inlined to the regular search to save tokens since they're mostly identical.
            // This flag controls whether we are in the main or quiescence search stage.
            var qs = remainingDepth <= 0;

            // STATIC EVALUATION: Based on material count, piece-square tables, bishop pair, and doubled pawns. All values
            // are interpolated between a midgame and endgame value based on the number of non-pawn pieces remaining.
            int Evaluate()
            {
                phase = mg = eg = 0;
                foreach (var color in new[] { 56, 0 })
                {
                    for (var pieceType = 1; pieceType < 7; pieceType++)
                    {
                        var pieceBB = board.GetPieceBitboard((PieceType)pieceType, color == 56);
                        while (pieceBB != 0)
                        {
                            phase += dynamicData[14 + pieceType]; // Phase score
                            var squareIdx = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB);
                            var pstIdx = 64 * pieceType + squareIdx ^ color;
                            if (pieceType == 3 && pieceBB != 0)
                            {
                                mg += 23;
                                eg += 40;
                            }

                            // If there's still a pawn on this pawn's file, it's doubled
                            if (pieceType == 1 && (0x101010101010101UL << (squareIdx & 7) & pieceBB) != 0)
                            {
                                mg -= 10;
                                eg -= 20;
                            }

                            mg += dynamicData[pstIdx] + staticData[pieceType];
                            eg += dynamicData[pstIdx + 384] + staticData[pieceType + 6];
                        }
                    }

                    mg = -mg;
                    eg = -eg;
                }

                return (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
            }

            if (ply > 0 && board.IsDraw()) return 0;
            if (qs && (alpha = Math.Max(alpha, Evaluate())) >= beta) return alpha;

            // TRANSPOSITION TABLE LOOKUP: Check if we've already searched this position before. If so, and if
            // the stored search was deep enough and is valid we can return the cached score. Even if we can't
            // use it though we can still use the stored best move to improve move ordering.
            var positionHash = board.ZobristKey;
            var (ttHash, ttMove, ttDepth, ttScore, ttNodeType) = tt[positionHash & 4194303];
            if (positionHash == ttHash // TT hit
                && beta - alpha == 1 // Isn't null window
                && ply > 1
                && ttDepth >= remainingDepth // Not from a shallower search
                && (ttScore >= beta ? ttNodeType > 0 : ttNodeType < 2)) // Valid node
                return ttScore;

            // STATIC NULL MOVE PRUNING: Like null move pruning (below) but instead of actually running a search we estimate
            // it based on the static evaluation plus a large margin proportional to the remaining depth.
            if (!qs &&
                !inCheck &&
                (ttScore = Evaluate()) - 80 * remainingDepth >= beta)
                return ttScore - 80 * remainingDepth;

            // NULL MOVE PRUNING: If we're up by a lot, pretend to give the opponent a free turn (i.e. move twice in a row)
            // and see what they can do. If they can't significantly improve their score then we can stop searching since
            // this line is really bad for them and they'll never choose it.
            var reduction = 3 + remainingDepth / 6;
            if (!qs &&
                canNMP &&
                !inCheck &&
                // phase != 0 && // Zugzwang check TODO evaluate() may not always run; ok?
                remainingDepth >= reduction
               )
            {
                board.ForceSkipTurn();
                canNMP = false;
                ttScore = -search(ply + 1, remainingDepth - reduction, -beta, 1 - beta);
                canNMP = true;
                board.UndoSkipTurn();
                if (ttScore >= beta && Math.Abs(ttScore) < 900_000) return beta;
            }

            var moves = board.GetLegalMoves(qs);
            // Is the game over? (If in quiet search, we don't want to return a mate score)
            if (moves.Length == 0) return qs ? alpha : inCheck ? ply - 1_000_000 : 0;

            // MOVE ORDERING: Transposition table, MVV-LVA, Killer moves, Counter moves
            var moveRanks = new int[moves.Length];
            int moveIdx = 0, cmOffset = board.IsWhiteToMove ? 0 : 4096;
            foreach (var move in moves)
                moveRanks[moveIdx++] = -(move == ttMove ? 50_000 :
                    move.IsCapture ? 1_024 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    ((move == killerMoves[ply] ? 501 : 0) + (move == counterMoves[cmOffset + (move.RawValue & 4095)] ? 10 : 0)));
            Array.Sort(moveRanks, moves);

            var bestScore = -99999999;
            ttMove = default;
            ttNodeType = 0; // Upper Bound
            foreach (var move in moves)
            {
                board.MakeMove(move);

                // PRINCIPAL VARIATION SEARCH: If our move ordering is actually good then we can do a full window search for
                // the first move and a null window for the rest. The null searches will be much faster if they don't find
                // a better move, but if they fail high then we'll have to repeat the search with a full window.
                // Reuse ttScore to save tokens 
                if (moveIdx++ == 0 // Use full window for first move (TT move)
                    || qs // Don't do PVS in quiescence search
                    || move.IsCapture // Use full window for captures
                    || remainingDepth < 2 // No point in PVS for shallow searches
                    || (ttScore = -search(ply + 1, remainingDepth - 1, -alpha - 1, -alpha)) > alpha)
                    ttScore = -search(ply + 1, remainingDepth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (ttScore > bestScore)
                {
                    bestScore = ttScore;
                    ttMove = move;

                    if (ttScore > alpha)
                    {
                        alpha = ttScore;
                        ttNodeType = 1; // Exact

                        if (ply == 0) candidateBestMove = move;
                        if (ttScore >= beta)
                        {
                            if (!move.IsCapture) killerMoves[ply] = move;
                            counterMoves[cmOffset + (move.RawValue & 4095)] = move;
                            ttNodeType++; // (2) Lower Bound
                            break;
                        }
                    }
                }
            }

            if (!stop) tt[positionHash & 4194303] = (positionHash, ttMove, remainingDepth, bestScore, ttNodeType);
            return bestScore;
        }

        for (var depth = 1; depth < 50; depth++)
        {
            search(0, depth, -99999999, 99999999);
            if (stop) break;
            bestMove = candidateBestMove;
        }

        return bestMove;
    }
}