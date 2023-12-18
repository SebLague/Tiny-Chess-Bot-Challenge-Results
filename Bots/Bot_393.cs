namespace auto_Bot_393;
using ChessChallenge.API;
using System;
using System.Linq;

// ReSharper disable once CheckNamespace
public class Bot_393 : IChessBot
{
    // Pawn, Knight, Bishop, Rook, Queen, King 
    private static readonly int[] PieceValues =
        {
            77, 302, 310, 434, 890, 0,
            109, 331, 335, 594, 1116, 0,
        },
        MoveScores = new int[218],

        UnpackedPestoTables =
            new[]
            {
                59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m,
                78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m,
                77059410983631195892660944640m, 61307098105356489251813834752m,
                77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m,
                2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m,
                7780689186187929908113377023m, 2486769613674807657298071274m,
                934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m,
                9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m,
                9302688995903440861301845277m, 4030554014361651745759368192m,
                78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m,
                11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m,
                9337766883211775102593666830m, 4676129865778184699670239740m,
                75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m,
                11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m,
                6534267870125294841726636036m, 3120251651824756925472439792m,
                74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m,
                7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m,
                1563108101768245091211217423m, 78303310575846526174794479097m,
                70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m,
                2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m,
                76410151742261424234463229975m, 72367527118297610444645922550m,
                64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m,
                75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m,
                69305332238573146615004392448m, 63422661310571918454614119936m,
            }.SelectMany(packedTable =>
                decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                    // No point in only taking 12 bytes. Since we never access the last 4 anyway, we can just leave them as garbage
                    .Select((square, index) => (int)((sbyte)square * 1.461) + PieceValues[index % 12])
                    .ToArray()
            ).ToArray();

    private readonly (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[0x400000];

    private readonly Move[] killers = new Move[2048];

    Move rootMove;

    public Move Think(Board board, Timer timer)
    {

        // Reset history tables
        var historyHeuristics = new int[2, 7, 64];

        // 1/13th of our remaining time, split among all of the moves
        int searchMaxTime = timer.MillisecondsRemaining / 13,
            // Progressively increase search depth, starting from 2
            depth = 2,
            alpha = -999999,
            beta = 999999,
            eval;

        // Iterative deepening loop
        for (; ; )
        {
            eval = PVS(depth, alpha, beta, 0, true);

            // Out of time -> soft bound exceeded
            if (timer.MillisecondsElapsedThisTurn > searchMaxTime / 3)
                return rootMove;

            // Gradual widening
            // Fell outside window, retry with wider window search
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {

                // Set up window for next search
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }

        // This method doubles as our PVS and QSearch in order to save tokens
        int PVS(int depth, int alpha, int beta, int plyFromRoot, bool allowNull)
        {


            // Declare some reused variables
            bool inCheck = board.IsInCheck(),
                canFPrune = false,
                notRoot = plyFromRoot++ > 0,
                notPV = beta - alpha == 1;

            // Draw detection
            if (notRoot && board.IsRepeatedPosition())
                return 0;

            ulong zobristKey = board.ZobristKey;
            var (entryKey, entryMove, entryScore, entryDepth, entryFlag) = transpositionTable[zobristKey & 0x3FFFFF];

            // Define best eval all the way up here to generate the standing pattern for QSearch
            int bestEval = -9999999,
                newTTFlag = 2,
                movesTried = 0,
                movesScored = 0,
                eval;

            int Search(int newAlpha, int R = 1, bool canNull = true) =>
                eval = -PVS(depth - R, -newAlpha, -alpha, plyFromRoot, canNull);

            if (inCheck)
                depth++;

            if (entryKey == zobristKey && notRoot && entryDepth >= depth && Math.Abs(entryScore) < 50000 && (
                    // Exact
                    entryFlag == 1 ||
                    // Upperbound
                    entryFlag == 2 && entryScore <= alpha ||
                    // Lowerbound
                    entryFlag == 3 && entryScore >= beta))
                return entryScore;

            if (entryMove == default && depth > 4)
                depth--;


            // Declare QSearch status here to prevent dropping into QSearch while in check
            bool inQSearch = depth <= 0;
            if (inQSearch)
            {
                // Determine if quiescence search should be continued
                bestEval = Evaluate();
                if (bestEval >= beta)
                    return bestEval;
                alpha = Math.Max(alpha, bestEval);
            }
            // No pruning in QSearch
            // If this node is NOT part of the PV and we're not in check
            else if (notPV && !inCheck)
            {
                int staticEval = Evaluate();

                switch (depth)
                {
                    case <= 7 when staticEval - 74 * depth >= beta:
                        return staticEval;
                    case >= 2 when staticEval >= beta && allowNull:
                        {
                            board.ForceSkipTurn();

                            Search(beta, 3 + depth / 4 + Math.Min(6, (staticEval - beta) / 175), false);
                            board.UndoSkipTurn();

                            // Failed high on the null move
                            if (eval >= beta)
                                return eval;
                            break;
                        }
                }

                // Extended futility pruning
                // Can only prune when at lower depth and behind in evaluation by a large margin
                canFPrune = depth <= 8 && staticEval + depth * 141 <= alpha;
            }

            // Generate appropriate moves depending on whether we're in QSearch
            Span<Move> moveSpan = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moveSpan, inQSearch && !inCheck);

            // Order moves in reverse order -> negative values are ordered higher hence the flipped values
            foreach (Move move in moveSpan)
                MoveScores[movesScored++] = -(
                    // Hash move
                    move == entryMove ? 9_000_000 :
                    // MVVLVA
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    // Killers
                    killers[plyFromRoot] == move ? 900_000 :
                    // History
                    historyHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            MoveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

            Move bestMove = entryMove;
            foreach (Move move in moveSpan)
            {
                if (depth > 2 && timer.MillisecondsElapsedThisTurn > searchMaxTime)
                    return 99999;

                // Futility pruning
                if (canFPrune && !(movesTried == 0 || move.IsCapture || move.IsPromotion))
                    continue;

                board.MakeMove(move);

                // LMR + PVS
                // Do a full window search if haven't tried any moves or in QSearch
                if (movesTried++ == 0 || inQSearch ||

                    // Otherwise, skip reduced search if conditions are not met
                    (movesTried < 6 || depth < 2 ||

                     // If reduction is applicable do a reduced search with a null window
                     (Search(alpha + 1, (notPV ? 2 : 1) + movesTried / 13 + depth / 9) > alpha)) &&

                    alpha < Search(alpha + 1))

                    Search(beta);

                board.UndoMove(move);

                if (eval <= bestEval) continue;
                bestEval = eval;
                if (eval > alpha)
                {
                    alpha = eval;
                    bestMove = move;
                    newTTFlag = 1;

                    // Update the root move
                    if (!notRoot)
                        rootMove = move;
                }

                // Cutoff
                if (alpha < beta) continue;
                // Update history tables
                if (!move.IsCapture)
                {
                    historyHeuristics[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] +=
                        depth * depth;
                    killers[plyFromRoot] = move;
                }

                newTTFlag = 3;
                break;
            }

            // Gamestate, checkmate and draws
            if (bestEval == -9999999)
                return inCheck ? plyFromRoot - 99999 : 0;

            // Transposition table insertion
            transpositionTable[zobristKey & 0x3FFFFF] = (
                zobristKey,
                bestMove,
                bestEval,
                depth,
                newTTFlag);

            return bestEval;
        }

        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        // Gamephase, middlegame -> endgame
                        // Multiply, then shift, then mask out 4 bits for value (0-16)
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        middlegame += UnpackedPestoTables[square * 16 + piece];
                        endgame += UnpackedPestoTables[square * 16 + piece + 6];

                        // Bishop pair bonus
                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 23;
                            endgame += 62;
                        }

                        // Doubled pawns penalty (brought to my attention by Y3737)
                        if (piece != 0 || (0x101010101010101UL << (square & 7) & mask) <= 0) continue;
                        middlegame -= 15;
                        endgame -= 15;
                    }

            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24) + 16;
        }
    }
}