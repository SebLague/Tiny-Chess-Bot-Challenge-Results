namespace auto_Bot_392;
// #define INFO

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_392 : IChessBot
{
    Move bestmoveRoot;
    (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[0x400000];
    static int[] pvm = {  77, 302, 310, 434,  890, 0,
                         109, 331, 335, 594, 1116, 0 },
                moveScores = new int[256],
                UnpackedPestoTables = new[] {
                    59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m,
                    77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m,
                    934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m,
                    78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m,
                    75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m,
                    74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m,
                    70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m,
                    64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m,
                }.SelectMany(packedTable =>
                new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                            .Select((square, index) => (int)((sbyte)square * 1.461) + pvm[index % 12])
                        .ToArray()
                ).ToArray();
    int searchMaxTime;
#if INFO
    long nodes; // #DEBUG
#endif

    public Move Think(Board board, Timer timer)
    {
#if INFO
        DivertedConsole.Write(""); // #DEBUG
        nodes = 0; // #DEBUG
#endif
        var HistoryHeuristics = new int[2, 4096];
        var killer = new Move[2048];
        searchMaxTime = timer.MillisecondsRemaining / 10;
        for (int depth = 2, eval, alpha = -9999999, beta = 9999999; ;)
        {
            eval = PVS(alpha, beta, depth, 0);
            if (timer.MillisecondsElapsedThisTurn >= searchMaxTime / 3)
                return bestmoveRoot;
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
#if INFO
                string evalWithMate = eval.ToString(); // #DEBUG
                if (Math.Abs(eval) > 999900 && Math.Abs(eval) <= 999999) // #DEBUG
                { // #DEBUG
                    evalWithMate = eval < 0 ? "-M" : "M"; // #DEBUG
                    evalWithMate += Math.Ceiling((double)(999999 - Math.Abs(eval)) / 2).ToString(); // #DEBUG
                } // #DEBUG
                int timeElapsed = timer.MillisecondsElapsedThisTurn; // #DEBUG
                DivertedConsole.Write("info string depth: {0, 2} | eval: {1, 6} | nodes: {2, 9} | nps: {3, 8} | time: {4, 5}ms | bestmove: {5}{6}", // #DEBUG
                              depth, // #DEBUG
                              evalWithMate, // #DEBUG
                              nodes, // #DEBUG
                              1000 * nodes / (timeElapsed + 1), // #DEBUG
                              timeElapsed, // #DEBUG
                              bestmoveRoot.StartSquare.Name,  // #DEBUG
                              bestmoveRoot.TargetSquare.Name // #DEBUG
                ); // #DEBUG
#endif
                alpha = eval - 17; beta = eval + 17;
                depth++;
            }
        }
        int Evaluate()
        {
            int mg = 0, eg = 0, phase = 0, stm = 2, piece, sq;
            for (; --stm >= 0; mg = -mg, eg = -eg)
            {
                for (piece = -1; ++piece < 6;)
                {
                    for (ulong bb = board.GetPieceBitboard((PieceType)(piece + 1), stm > 0); bb != 0;)
                    {
                        sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ 56 * stm;
                        mg += UnpackedPestoTables[sq * 12 + piece];
                        eg += UnpackedPestoTables[sq * 12 + piece + 6];
                        phase += 0x00042110 >> piece * 4 & 0x0F;
                        if (piece == 2 && bb != 0)
                        {
                            mg += 23;
                            eg += 62;
                        }
                        if (piece == 0 && (0x101010101010101UL << (sq & 7) & bb) > 0)
                        {
                            mg -= 15;
                            eg -= 15;
                        }
                        if (piece == 3 && (0x101010101010101UL << (sq & 7) & board.GetPieceBitboard(PieceType.Pawn, stm > 0)) == 0)
                        {
                            mg += 13;
                            eg += 10;
                        }
                    }
                }
            }
            return (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24) + 16;
        }
        int PVS(int alpha, int beta, int depth, int ply, bool canNullMove = true)
        {
#if INFO
            nodes++; // #DEBUG
#endif
            ulong key = board.ZobristKey;
            var (entryKey, entryMove, entryDepth, entryScore, entryBound) = tt[key & 0x3FFFFF];
            bool qsearch = depth <= 0,
                notRoot = ply > 0,
                canFPrune = false,
                isCheck = board.IsInCheck(),
                notPV = beta - alpha == 1;
            int best = -9999999,
                eval = Evaluate(),
                TTFlag = 2,
                movesSearched = 0,
                movesScored = 0;

            if (notRoot && board.IsRepeatedPosition())
                return 0;

            if (notRoot && entryKey == key && entryDepth >= depth && (
                    entryBound == 1 ||
                    entryBound == 2 && entryScore <= alpha ||
                    entryBound == 3 && entryScore >= beta
            )) return entryScore;

            if (isCheck) depth++;

            int Search(int newAlpha, int R = 1, bool canNull = true) => eval = -PVS(-newAlpha, -alpha, depth - R, ply + 1, canNull);

            if (qsearch)
            {
                best = eval;
                if (best >= beta) return best;
                alpha = Math.Max(alpha, best);
            }

            else if (notPV && !isCheck)
            {
                int staticEval = Evaluate();
                if (depth <= 7 && staticEval - 74 * depth >= beta)
                    return staticEval;

                if (depth >= 2 && staticEval >= beta && canNullMove)
                {
                    board.ForceSkipTurn();
                    Search(beta, 3 + depth / 4 + Math.Min(6, (staticEval - beta) / 175), false);
                    board.UndoSkipTurn();

                    if (beta <= eval)
                        return eval;
                }
                canFPrune = depth <= 8 && staticEval + depth * 141 <= alpha;
            }

            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, qsearch);
            foreach (Move move in moves)
                moveScores[movesScored++] = -(
                    move == entryMove ? 9_000_000 :
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    killer[ply] == move ? 900_000 : HistoryHeuristics[ply & 1, move.RawValue & 4095]
                );

            moveScores.AsSpan(0, moves.Length).Sort(moves);

            if (!qsearch && moves.Length == 0) return board.IsInCheck() ? ply - 999999 : 0;

            Move bestMove = entryMove;

            foreach (Move move in moves)
            {
                bool TokenReducer = move is { TargetSquare.Rank: 1 };
                if (depth > 2 && timer.MillisecondsElapsedThisTurn >= searchMaxTime)
                    return 9999999;

                if (canFPrune && !(movesSearched == 0 || move.IsCapture || move.IsPromotion))
                    continue;

                board.MakeMove(move);
                if (movesSearched++ == 0 || qsearch ||
                    (movesSearched < 6 || depth < 2 ||
                        (Search(alpha + 1, (notPV ? 2 : 1) + movesSearched / 13 + depth / 9) > alpha)) &&
                        alpha < Search(alpha + 1))
                    Search(beta);
                board.UndoMove(move);

                if (eval > best)
                {
                    best = eval;
                    if (eval > alpha)
                    {
                        alpha = eval;
                        bestMove = move;
                        if (ply == 0)
                            bestmoveRoot = move;
                        TTFlag = 1;
                    }
                    if (alpha >= beta)
                    {
                        if (!move.IsCapture)
                        {
                            HistoryHeuristics[ply & 1, move.RawValue & 4095] += depth * depth;
                            killer[ply] = move;
                        }
                        TTFlag = 3;
                        break;
                    }
                }
            }
            tt[key & 0x3FFFFF] = new(key, bestMove, depth, best, TTFlag);

            return best;

        }
    }
}
