namespace auto_Bot_626;
// Bot name: drip leab
// The authors are in no particular order
// Authors: JeffLegendPower (discord id: jefflegendpower)
// AtadOfAChad (discord id: atadofachadgladbadradsadmadlad)
// Gonumen (discord id: Gonumen#9433)
// KevinStriker (discord id: kevinstriker)

// People who majorly helped (ALSO AUTHORS!!!!) with trying out the NN eval (turned out to be a failure but they still deserve their places):
// Octopus (discord id: codephoenix0)
// krz (discord id: 444pts)
// Outer Cloud (discord id: outercloud)s

// table (kinda he dint really do much but hes here) (discord id: asthmagod)

// Honorable mentions:
// A_randomnoob (discord id: __arandomnoob): He made the uci impl and told me how to use cutechess
// Gedas (discord id: justgedas): For making his texel tuner which i used to fine tune values
// Toanth (discord id: toanth): For literally everything
// Tyrant (discord id: tyrant0565): For creating the original PST packing code and holding the community closer together with his bot
// And everyone in the Chess Engine Coding discord for creating such a great community

using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

public class Bot_626 : IChessBot
{
    // Here to save a few tokens
    // tried very hard to use gonumen eval but always lost like a billion elo
    private static readonly int[] scores = new int[218],
        PSTS = new[] {
            57598170397150904946862889216m, 68442263483821321132427227392m, 70933887896001020259289652480m, 2524388521510331388374414080m, 77418523583061249821821504000m, 78031411064465689550228939520m, 75518063675816398478495825152m, 53261725109948020105028550656m,
            77996380292035904030205867567m, 4993010120347774762168746305m, 7806194725391235258501173808m, 3798577336053278713078029891m, 8145874472346130301794128439m, 10586648477621085664971734575m, 8406941150647171045757812740m, 1577681083016066806648607470m,
            1871530707991255319602003960m, 5910556539072607462527997699m, 9966507516159485458867429914m, 12443591669123709000266951196m, 12147362414940405037918667807m, 10582988792308294354417435697m, 10551528312661143224269613090m, 4352147245776166046200697346m,
            78941765301832425304122785004m, 7159367669691137135126840830m, 11194795057362707782076536578m, 13690003948110661227288143107m, 13702041131146888862559181073m, 12451992943912071288799570446m, 10581775218912820407730310414m, 4992896857238056696968713468m,
            76462239500991595023928194277m, 3757450394412829508230053624m, 9025958654409870650145835256m, 12137724232542384183678470403m, 12136501139767436129348882179m, 9343877736622082878979050751m, 6851030120772505040368765699m, 3127528855539837437875190771m,
            74287358357397534701969729507m, 24254295836773815295473398m, 4685872367452793865668657143m, 7159348762133969967121826551m, 7161752502012860244949338625m, 5608259155842784971672716283m, 1258477740247976483049768206m, 77690399408029540005328777975m,
            69640233265408702103882424035m, 75524084674999515226820437750m, 78307036597800147246780183284m, 1563221549822730538218879468m, 2184581123656249372129098490m, 615385817343187506919176965m, 75486546657178728883316126228m, 71748566599088126957522776306m,
            62210165493153164996799217664m, 66238330193475382413117940992m, 70264085972688307919705660416m, 74278919433294263541314872576m, 69021272672841737965396945152m, 74282503690503632997246431744m, 68376896172506137681727710720m, 62807341643471117982927213824m,
        }.SelectMany(packedTable =>
        decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                    .Select((square, index) => (int)((sbyte)square * 1.461) + new[] {
                        77, 302, 310, 434, 890, 0, // Middlegame
                        109, 331, 335, 594, 1116, 0 // Endgame
                    }[index % 12])
            .ToArray()
        ).ToArray();

    private readonly (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[0x400000];

    // keeping killers and counter moves together to save tokens
    // 4096, 1 for each ply (killers), +6 for each piece type (counter moves)
    private readonly Move[] killers = new Move[4096];
    // counterMoves = new Move[4096, 7];

    Move rootMove;

    public Move Think(Board board, Timer timer)
    {
        // Reset history
        var history = new int[4096, 7];

        // Last move for countermove heuristic
        // Move previousMove = default;

        // Soft bound search time along with stuff for ID
        int maxSearchTime = timer.MillisecondsRemaining / 13,
            depth = 2, alpha = -999999, beta = 999999, eval;

        // Iterative deepening (ID)
        for (; ; )
        {
            // aspiration windows
            // idea is that depth 5 eval -> depth 6 eval wont change that much
            // so we can save time by narrowing alpha and beta to closer to eval.
            eval = Search(depth, alpha, beta, 0, true);

            // Soft bound exceeded
            if (timer.MillisecondsElapsedThisTurn > maxSearchTime / 3)
                return rootMove;

            // Aspiration windows
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
                // Set up aspiration window for the next search
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }

        // This method doubles as our PVS and QSearch in order to save tokens
        int Search(int depth, int alpha, int beta, int ply, bool shouldNMP)
        {

            // Declare some reused variables to save tokens
            bool inCheck = board.IsInCheck(),
                shouldFP = false,
                notRoot = ply++ > 0,
                nullWindow = beta - alpha == 1,
                qsearch;

            // Draw detection
            if (notRoot && board.IsRepeatedPosition())
                return -30;

            ulong zobristKey = board.ZobristKey;
            var (entryKey, entryMove, entryScore, entryDepth, entryFlag) = transpositionTable[zobristKey & 0x3FFFFF];

            int bestEval = -9999999,
                newTTFlag = 2,
                eval;

            if (entryKey == zobristKey && nullWindow && entryDepth >= depth && Abs(entryScore) < 50000)
            {
                switch (entryFlag)
                {
                    // Exact hit
                    case 1:
                        return entryScore;
                    // Fail low
                    case 3:
                        alpha = Max(alpha, entryScore);
                        break;
                    // Fail high
                    case 2:
                        beta = Min(beta, entryScore);
                        break;
                }

                if (beta <= alpha) return entryScore;
            }

            // Little thing to save tokens
            entryScore = entryDepth = 0;

            // Local method to save tokens
            int InternalSearch(int nextAlpha, int redux = 1, bool shouldNMPInternal = true) =>
                eval = -Search(depth - redux, -nextAlpha, -alpha, ply, shouldNMPInternal);

            // Check extensions
            if (inCheck)
                depth++;

            // QSearch
            if (qsearch = depth <= 0)
            {
                bestEval = Evaluate();
                if (bestEval >= beta)
                    return bestEval;
                alpha = Max(alpha, bestEval);
            }

            else if (nullWindow && !inCheck)
            {
                int staticEval = Evaluate();

                if (depth <= 7)
                {
                    // RFP margins
                    // if we're up a lot in material, dont waste time searching further,
                    // return staticeval.
                    if (staticEval - 74 * depth >= beta)
                        return staticEval;

                    // FP margins
                    // if we are down 141 points or more in material
                    // its likely none of the moves will raise alpha,
                    // skip all quiet moves.
                    shouldFP = staticEval + depth * 141 <= alpha;
                }

                // Null Move Pruning (NMP)
                // if we can do nothing and still get a fail-high
                // this position is very good, so just return null window search.
                // saves time because we can reduce depth of null window search.
                if (depth >= 2 && staticEval >= beta && shouldNMP)
                {
                    board.ForceSkipTurn();
                    InternalSearch(beta, 3 + depth / 4 + Min(6, (staticEval - beta) / 175), false);
                    board.UndoSkipTurn();

                    // NMP fail high
                    if (eval >= beta)
                        return eval;
                }
            }

            // Stackalloc moves to speed up a little
            Span<Move> moveSpan = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moveSpan, qsearch);

            // Move ordering
            foreach (Move move in moveSpan)
                scores[entryDepth++] = -( // Reverse the scores because Array.Sort sorts lowest to highest and we want highest to lowest
                // TT Move
                entryKey == zobristKey && move == entryMove ? 9_000_000 :
                // MVV-LVA
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                // Killers
                killers[ply] == move ? 900_000 :
                // History
                history[move.RawValue & 4095, (int)move.MovePieceType]);

            scores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

            foreach (Move move in moveSpan)
            {
                // Hard bound
                if (depth > 2 && timer.MillisecondsElapsedThisTurn > maxSearchTime)
                    return 99999;

                // Futility pruning (FP)
                if (shouldFP && !(entryScore == 0 || move.IsCapture || move.IsPromotion))
                    continue;

                board.MakeMove(move);

                // PVS + LMR
                // The idea of PVS is that the first move in move ordering is the best move.
                // We prove this by doing a full window search on the first move and a null window search on all following moves.
                // however, if null window search fails high, we need to research with full window.

                if (entryScore++ == 0 || qsearch ||

                    // Don't do a reduced search if these conditions aren't met
                    (entryScore < 6 || depth < 2 ||

                        // Start with a null window search with reduction
                        InternalSearch(alpha + 1, (nullWindow ? 2 : 1) + entryScore / 13 + depth / 9) > alpha) &&

                        // Reduced search passed alpha, so try a null window search with no reduction
                        // !null window is there because if nullwindow, ie alpha + 1 == beta,
                        // search(alpha + 1) == search(beta), dont waste time searching same thing again
                        alpha < InternalSearch(alpha + 1) && !nullWindow)

                    // Full window search
                    InternalSearch(beta);

                board.UndoMove(move);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    // improving alpha
                    if (eval > alpha)
                    {
                        alpha = eval;
                        entryMove = move;
                        // exact flag
                        newTTFlag = 1;

                        // Update the best root move
                        if (!notRoot)
                            rootMove = move;
                        // Fail high cutoff
                        if (alpha >= beta)
                        {
                            // Set TT flag to fail high
                            newTTFlag = 3;

                            if (move.IsCapture) break;
                            // Update history and killers (quiets only)
                            history[move.RawValue & 4095, (int)move.MovePieceType] += depth * depth;
                            killers[ply] = move;
                            // update tables
                            // idea is that if a move causes fail-high, it may be very good even in other nodes.

                            break;
                        }
                    }
                }
            }

            // Checkmate/stalemate detection
            // if besteval == -9999999, we cant be in qsearch, because then bestEval = staticeval,
            // and there must have been no moves to look at to raise best eval.
            // since there are no moves to look at, we are in check/stale mate.
            if (bestEval == -9999999)
                return inCheck ? ply - 99999 : -30;

            // TT insertion
            transpositionTable[zobristKey & 0x3FFFFF] = (
                zobristKey,
                entryMove,
                bestEval,
                depth,
                newTTFlag);

            return bestEval;
        }

        // Eval from Tyrant
        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        // middlegame += UnpackedPestoTables[piece * 64 + square]; // Also credit to Gonumen for this
                        // endgame += UnpackedPestoTables[piece * 64 + square + 384]; // And this!
                        middlegame += PSTS[square * 16 + piece];
                        endgame += PSTS[square * 16 + piece + 6];

                        // Bishop pair
                        // Rook pair failed :(
                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 20;
                            endgame += 66;
                        }

                        // Doubled pawns
                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            middlegame -= 12;
                            endgame -= 36;
                        }
                    }
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)
                + 16;
        }
    }
}
