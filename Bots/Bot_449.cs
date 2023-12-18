namespace auto_Bot_449;
//#define UCI

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_449 : IChessBot
{
#if UCI
    long nodes;
#endif

    // Sardine 1.13
    // Originally based on https://github.com/JacquesRW/Chess-Challenge/blob/main/Chess-Challenge/src/My%20Bot/MyBot.cs
    // Search optimisations from https://github.com/Selenaut/Chess-Challenge-Selebot
    // Exact pesto encoding by folke 
    // Transposition table, aspiration windows and phase from https://github.com/Tyrant7/Chess-Challenge
    // Eval tuned by TOANTH
    // Any original code by Broxholme and Broxholme's brother-in-law

    Move bestMoveRoot;

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function

    static int[] pieceValues = { 77, 109, 268, 331, 310, 335, 434, 594, 890, 1116, 0, 0 }, psts = new decimal[] { 12747178815967656535090545732m, 18059204345024563389558771004m, 22055950347931878108110993948m, 2117859730488835174389516373.9m, 21127510001497488112933310.849m, 21127510003803823358278386756m, 24220013343914382064511375191m, 21753714577929312276339770439m, 4259014360851621014904788747.2m, 7890389518301573182978086.9503m, 22039058002028493401691014212m, 27034487452321832148360844874m, 37902739811475532262218358601m, 4285963886813573998770005.5939m, 4536664288130589831404798.2954m, 2174666650743449398246950.6965m, 4131559430851755007141021833.5m, 3481408660661981011773787.2771m, 4970809942756811613189810.0096m, 3232726436168422707885005.8913m, 34493549630863408304490182757m, 37923320310613240507683069.804m, 4473818197558914789486497.2936m, 3978159602272382515347656.2322m, 5684082806154313437325751.3854m, 4505502954134149913036081.0159m, 4005231417938809918135431154.1m, 33273881870385198225565973.886m, 4754292285849764286818507.5330m, 4165062824876912047074427.5354m, 4287396600930104764729193.4861m, 3945876054958621604101072.4995m, 30450835725597272776718770013m, 3451529679345998055084526218.2m, 4007040066056794679030679713.9m, 5906290527413918947453144.4612m, 5219126206583755392784587.3803m, 3916261155863567829830466.3739m, 4691064548774243270359178.1008m, 4101836002630673475280670.1971m, 4878332371261089419337797.5455m, 4040541589416119720968267.6624m, 39128576835481505089504911.772m, 46596281941015203721566839.923m, 375860536388108084922782506.10m, 450416032626980015847940081.88m, 3232478040031537362865936.8065m, 4014685710649176928897011.7230m, 4348691535992397239047300.7744m, 4102931160080628771257165.0702m, 6459614882704078920359861.8007m, 4258288589237473916808611.9135m, 5469626019582807873822783.2964m, 5840973531784842131303449.5403m, 14930802524085454209999344.573m, 1899368892878962951585151285m, 2206726061683694095851193837.1m, 3141337768551581873805699.5159m, 34463189399924606300471725.995m, 29539528921581174004611378.517m, 5652883975432996408026251.3001m, 4694223882410467232019432.5687m, 4413252401249475192977991.8469m, 1376057667893071963882773929.2m }
                .SelectMany(x => decimal.GetBits(x).Take(3))
                .SelectMany(BitConverter.GetBytes)
                .Select((x, i) => x - (i < 184 ? 68 : 129) + pieceValues[i / 64])
                .ToArray(), scores = new int[256];

    Move[] killers = new Move[1000];

    // https://www.chessprogramming.org/Transposition_Table

    private readonly (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[4194304];

    public Move Think(Board board, Timer timer)
    {
        var history = new int[65536];

#if UCI
        nodes = 0;
        int depth = 1;
        int score = 0;
#endif

        int Eval()
        {

            // evaluate
            int mg = 0, eg = 0, phase = 0;

            for (var i = 0; i < 12;)
            {
                int p = i / 2;
                ulong mask = board.GetPieceBitboard((PieceType)p + 1, i++ % 2 == 1);

                while (mask != 0)
                {
                    // Gamephase, middlegame -> endgame
                    // Multiply, then shift, then mask out  4 bits for value (0-16)
                    phase += 0x00042110 >> p * 4 & 0x0F;
                    var square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
                    var index = 128 * p + square ^ i % 2 * 56;
                    mg += psts[index];
                    eg += psts[index + 64];
                    if (p == 2 && mask != 0) { mg += 23; eg += 62; } // Bishop pair

                    if (p == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                    {
                        mg -= 15;
                        eg -= 15;
                    }
                }
                mg = -mg;
                eg = -eg;
            }

            return (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? -24 : 24) + 9;
        }

        // https://www.chessprogramming.org/Negamax
        // https://www.chessprogramming.org/Quiescence_Search
        int Search(int alpha, int beta, int depth, int ply, bool doNull)
        {
#if UCI
        nodes++;
#endif
            bool inCheck = board.IsInCheck(), qSearch = depth <= 0 && !inCheck, notRoot = ply > 0, canFPrune = false, notPvNode = beta - alpha == 1;

            // Check for repetition and material - if it's a 50 move draw, we draw
            if (notRoot && board.IsRepeatedPosition() | board.IsInsufficientMaterial())
                return 0;

            if (inCheck)
            {
                depth++;
                if (board.IsInCheckmate()) return ply - 30000;
            }

            ulong key = board.ZobristKey;
            var (ttKey, ttMove, ttDepth, ttScore, ttBound) = tt[key & 4194303];

            // TT cutoffs
            int best = -30000, i = 0, b = ttBound, s = ttScore, newTTBound = 0;

            if (notRoot && ttKey == key && qSearch | notPvNode & ttDepth >= depth &&
                 b != 2 | s >= beta // lower bound, fail high
                 && b != 0 | s <= alpha // upper bound, fail low
            ) return s;

            // Quiescence search is in the same function as negamax to save tokens
            if (qSearch)
            {
                // Stand pat
                best = Eval();
                if (best >= beta) return best;
                alpha = Math.Max(alpha, best);
            }
            else if (notPvNode && !inCheck)
            {
                // Static Move Pruning
                s = Eval();
                if (depth < 7 && s - 80 * depth >= beta) return s;

                // Null Move Pruning
                if (doNull && depth >= 2 && s >= beta)
                {
                    board.ForceSkipTurn();
                    int score = -Search(-beta, 1 - beta, depth - 3 - depth / 6, ply + 1, false);
                    board.UndoSkipTurn();
                    if (score >= beta) return score;
                }

                // Extended futility pruning
                // Can only prune when at lower depth and behind in evaluation by a large margin
                canFPrune = depth <= 5 && s + depth * 82 <= alpha;
            }

            // Generate moves, only captures in qSearch
            Span<Move> moves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref moves, qSearch);

            // Stalemate
            if (!qSearch && moves.IsEmpty) return 0;

            // Score moves
            foreach (var move in moves)
                scores[i++] = -(move == ttMove ? 1000000000 :
                                    move.IsCapture ? 10000001 * (int)move.CapturePieceType - (int)move.MovePieceType :
                                    killers[ply] == move ? 10000000 :
                                    history[move.RawValue]);

            Move bestMove = default;
            b = alpha;

            scores.AsSpan(0, moves.Length).Sort(moves);

            if (depth >= 5 && ttMove == default) depth--; // IIR

            // Search moves
            i = 0;
            foreach (var move in moves)
            {
                s = alpha + 1;

                if (DateTime.UtcNow.Ticks % 128 == 0 && timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 25) i /= 0;

                // Futility pruning
                if (canFPrune && i != 0 && !move.IsCapture)
                    continue;

                board.MakeMove(move);

                var dontDoFullSearch = !qSearch && i++ != 0;

                void S(int nextAlpha, int reduction) { if (s > alpha) s = -Search(-nextAlpha, -alpha, depth - ++reduction, ply + 1, doNull || dontDoFullSearch); }

                if (dontDoFullSearch)
                {
                    if (depth >= 2 && i >= 6)
                        S(alpha + 1, 1 + i / 12 + depth / 8); // LMR

                    S(alpha + 1, 0); // PVS
                }
                S(beta, 0); // Full search

                board.UndoMove(move);

                // New best move
                if (s > best)
                {
                    best = s;
                    if (s > alpha)
                    {
                        alpha = s;
                        bestMove = move;
                        newTTBound = 1;

                        if (!notRoot) bestMoveRoot = move;
                    }

                    // Fail-high
                    if (alpha >= beta)
                    {
                        if (!move.IsCapture)
                        {
                            killers[ply] = move;
                            history[move.RawValue] += depth * depth;
                        }
                        newTTBound = 2;
                        break;
                    }
                }
            }

            // Push to TT
            tt[key & 4194303] = new(key, bestMove == default ? ttMove : bestMove, depth, best, newTTBound);

            return best;
        }

        // https://www.chessprogramming.org/Iterative_Deepening
        try
        {
            for (int depth = 1, alpha = -999999, beta = 999999, eval; ;)
            {
                eval = Search(alpha, beta, depth, 0, true);
                // Gradual widening
                // Fell outside window, retry with wider window search
                if (eval <= alpha)
                    alpha -= 100;
                else if (eval >= beta)
                    beta += 100;
                else
                {
                    depth++;
                    // Set up window for next search
                    alpha = eval - 10;
                    beta = eval + 10;
                }

#if UCI
                // UCI Debug Logging
                DivertedConsole.Write("info depth {0,2} score {1,6} nodes {2,9} nps {3,8} time {4,5} pv {5}{6}",
                    depth,
                    score,
                    nodes,
                    1000 * nodes / (timer.MillisecondsElapsedThisTurn + 1),
                    timer.MillisecondsElapsedThisTurn,
                    bestMoveRoot.StartSquare.Name,
                    bestMoveRoot.TargetSquare.Name
                );
#endif

            }
        }
        catch { }
        return bestMoveRoot;

    }
}