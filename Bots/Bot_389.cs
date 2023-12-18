namespace auto_Bot_389;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

public class Bot_389 : IChessBot
{
    // 1     2     3    4    5
    //key   move eval depth type
    private (ulong, Move, int, int, int)[] entries = new (ulong, Move, int, int, int)[4194304];

    private Move bestMove0, voidMove = Move.NullMove;
    private static int[] pieceVal = //mg-eg pairs
        {
        82, 94,
        337, 281,
        365, 297,
        477, 512,
        1025, 936,
        0, 0
        };
    private readonly int[] psqt = new[]
          { 25802991419576529536463796191m, 16996220847677541500643916185m, 25794407891813157864087594827m, 38985286316542696125996791689m, 79227860209022314669995325407m,
            57845728966736168422594301821m, 29060912098868044495677671979m, 38985286316542738643378665963m, 20559832399879891216865202240m, 61586265065843290066476035718m,
            22026248996935164355188545747m, 16386763480232143704877515082m,  6444889590503295418488402436m, 20574339129741244385363923917m, 17629962199975648473167615568m,
             3815437343080996757928760263m,  8354528860717427215770331338m, 36651867804657803974332611985m, 51680278788491056625042242265m, 17492038463244580127602826083m,
            22811052805158518644495172498m, 41522279217847493389645903329m, 22757630223834448445675845785m, 20315197530171144248518870034m, 70329245014098556982487637493m,
            19030545715098785951331859418m, 11653904430499746826186998536m, 15021723709554212308386378822m, 42854742292285933872504830571m, 41442135345864755076314327462m,
            21496025579514693938792266018m, 19251966480846032706449307225m, 71566677803994059031475865547m, 40189068429533187255763362835m, 45427023774303173990914810646m,
             6386843158547896109371925961m, 39397357090025293962747518038m, 67987546099897820937321875791m, 45372991075728523072469400784m,  8958565869178327385822765902m,
            12542413550212493724894288963m, 10255365786445681878001654934m, 13968562176128237188543461254m, 55440011522033732114063391136m, 52957820665750670735084421699m,
            43092707733980225685358582697m, 27983543164738667168662226640m,  8922530449741118306526311691m }.SelectMany((compressedVal, index) =>
                                    Enumerable.Range(0, 16).Select(i =>
                                    (int)(22.28164 * Tan((int)(compressedVal / (decimal)Pow(64d, i) % 64) * 0.04488 - 1.39128))
                                    //Uses Tan for lossy compression, could have been tuned
                                    + pieceVal[index / 4]
                                    )).ToArray(); //This is in centipawns

    public Move Think(Board pos, Timer timer)
    {
        var (useTimeInMillis, pieceDestnHistory, priorities, killerMoves) =
            (timer.MillisecondsRemaining / 40,
            new int[2, 7, 64],
            new int[218],
            new Move[128]);

        for (int iterDepth = 1, alpha = -3000000, beta = 3000000, eval; ;)
        {
            eval = AlphaBetaSearch(iterDepth, 0, -3000000, 3000000, true);

            if (timer.MillisecondsElapsedThisTurn >= useTimeInMillis || iterDepth > 90)
                return bestMove0;

            if (eval <= alpha) alpha -= 1488;
            else if (eval >= beta) beta += 1488;
            else
            {
                alpha = eval - 408;
                beta = eval + 408;
                iterDepth++;
            }
        }

        // Fail-soft alpha-beta search with move ordering, quiescence search, mate distance pruning, transposition table and killer heuristics.
        // Stores mate scores accurately in TT entries which is unique I think.
        // Returns evaluation of the current position "pos", measured in 1/24ths of a centipawn.
        int AlphaBetaSearch(int depth, int plyFromRoot, int alpha, int beta, bool canNMP)
        {
            var (bestMove, isQuiescence, posKey, score, futile, moveCount, notPV, inCheck) =
               (voidMove,
                depth <= 0,
                pos.ZobristKey,
                pos.PlyCount - 2450000,
                false,
                0,
                beta == alpha + 1,
                pos.IsInCheck());

            if (!isQuiescence && inCheck) depth++;

            //Mate distance pruning
            alpha = Max(alpha, score); //score = we lose by checkmate this ply
            beta = Min(beta, ~score); //~score = -mateScore-1 = 2450000 - (pos.PlyCount + 1), we win by checkmate next ply
            if (alpha >= beta) return alpha;

            killerMoves[plyFromRoot + 2] = voidMove;


            if (plyFromRoot != 0 && pos.IsRepeatedPosition())
                return 0;

            //note that the "score" variable, and hence the Search method, should be used carefully after this point

            ref int pieceHistoryEntry(Move k) => ref pieceDestnHistory[plyFromRoot & 1, (int)k.MovePieceType, k.TargetSquare.Index];
            int Search(int beta2, int R = 1, bool nmp = true) => score = -AlphaBetaSearch(Max(0, depth - R), plyFromRoot + 1, -beta2, -alpha, nmp);
            //TT initialisations
            var (entryKey, ttMove, ttScore, ttDepth, entryType) = entries[posKey & 4194303];
            if (Abs(ttScore) > 2400000) ttScore -= Sign(ttScore) * pos.PlyCount; //Mate score adjustment

            if (entryKey != posKey || ttDepth < depth ||
                entryType == 1 && ttScore > alpha || entryType == 2 && ttScore < beta)
            { //TT cannot be used here
                var (type,
                     M,
                     bestScore,
                     priorityIndex) =

                    (1,
                     pos.GetLegalMoves(isQuiescence),
                     -3000000,
                     0);
                if (isQuiescence)
                {
                    if ((bestScore = alpha = Max(alpha, Evaluate())) >= beta)
                        return alpha;
                }
                else if (notPV && !inCheck)
                {
                    score = Evaluate();  //reuse score as approximate evaluation of the position
                    if (depth < 8 && score - 1777 * depth >= beta) return score; //Non-PV node futility pruning
                    if (canNMP && score - 5688 / depth >= beta)
                    {
                        pos.ForceSkipTurn();
                        Search(beta, 3 + depth / 4 + Min(6, (score - beta) / 4200), false);
                        pos.UndoSkipTurn();

                        if (score >= beta) return score;
                    }
                    futile = depth < 9 && score + 3375 * depth <= alpha; //Additional futility pruning
                }

                //move ordering
                foreach (Move move in M)
                    priorities[priorityIndex++] = -(move == ttMove ? 2000000000 :                                         //TT move
                                       move.IsCapture ? 1000000 * (int)move.CapturePieceType - (int)move.MovePieceType :  //MVVLVA
                                       move == killerMoves[plyFromRoot] ? 900000 :                                        //Killer moves
                                       pieceHistoryEntry(move));                                                          //History heuristic


                Array.Sort(priorities, M, 0, priorityIndex); //priorityIndex = M.Length
                foreach (Move m in M) //fail-soft implementation
                {
                    if (futile && !(moveCount == 0 || m.IsCapture || m.IsPromotion)) continue;
                    pos.MakeMove(m);
                    if (moveCount++ == 0 || isQuiescence ||
                        (moveCount < 6 || depth < 2 || Search(alpha + 1, (notPV ? 2 : 1) + moveCount / 13 + depth / 9) > alpha) && //LMR
                        Search(alpha + 1) > alpha) //PVS
                        Search(beta);
                    pos.UndoMove(m);
                    if (depth > 1 && timer.MillisecondsElapsedThisTurn >= useTimeInMillis)
                        goto evalEnd; //end the search
                    if (score > bestScore)
                    {
                        bestMove = m;
                        bestScore = score;
                        if (score >= beta)
                        {
                            type = 2;
                            break;
                        }
                        if (score > alpha) (type, alpha) = (0, score);
                    }
                }
                if (bestScore == -3000000) return inCheck ? score : 0;
                //Note that if not in quiescence, in check and no moves are searched, "score" still stores the mated value.
                alpha = bestScore;
                if (!isQuiescence || type != 1)
                    entries[posKey & 4194303] = (posKey, bestMove,
                                Abs(alpha) > 2400000 ? alpha + Sign(alpha) * pos.PlyCount : alpha, //Mate score adjustment
                                depth, type); //for qsearch entries, depth should be 0 because of how search function is called

                /*Note that
                 * alpha might be <= starting alpha, or >= beta (fail-soft) at this point
                 */
            }
            else //this is TT
            {
                alpha = ttScore; //TT value calculated previously
                bestMove = ttMove;
            }

            //write to heuristics on fail-high (both in TT cut-nodes and in full search)
            if (alpha >= beta && !bestMove.IsCapture)
            {
                killerMoves[plyFromRoot] = bestMove;
                pieceHistoryEntry(bestMove) += depth * depth;
            }

        evalEnd:
            if (plyFromRoot == 0 && bestMove != voidMove) bestMove0 = bestMove;
            return alpha;
        }

        int Evaluate()
        {//"eval = -eval" version costs 1 extra token for now, but might save tokens if I decide to improve the eval
            var (mgEval, egEval, phase, colorN) //mg and eg values are centipawns, phase is 0 to 24
                    =
                (0, 0, 0, 3);

            for (; (colorN -= 2) >= -1;) //thats 1, -1
                for (int piece = 0; ++piece <= 6;) //thats 1 to 6
                    for (ulong mask = pos.GetPieceBitboard((PieceType)piece, colorN != 1); mask != 0;)
                    {
                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ colorN & 56;
                        phase += 0x00421100 >> piece * 4 & 15;
                        mgEval += colorN * psqt[square += piece - 1 << 7];
                        egEval += colorN * psqt[square + 64];
                    }

            return (mgEval * phase + egEval * (24 - phase)) * (pos.IsWhiteToMove ? -1 : 1) + 17 * phase; //1/24th of a centipawn as unit
        }
    }
}