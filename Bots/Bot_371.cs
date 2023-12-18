namespace auto_Bot_371;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;
public class Bot_371 : IChessBot
{


    // zobrist hash, move, depth, score, bound

    //TODO: use smaller types for depth, score and bound.
    // if we go int16, int8, int8 thats a whole byte smaller for each entry
    //will have to reorder so biggest types are first
    private readonly (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[5_000_000]; //5M entries is approx 128MB, will fluctuate due to GC

    private readonly Move[] killerTable = new Move[256];
    // public int positionsEvaluated = 0;
    public static int aspiration = 12;
    Move bestMoveRoot;
    /*
    PeSTO style tuned piece tables shamelessly stolen from TyrantBot
    */
    //pawn, knight, bishop, rook, queen, king
    private static readonly short[] PieceValues =  { 77, 302, 310, 434, 890, 0, // Middlegame
                                          109, 331, 335, 594, 1116, 0, }; // Endgame
                                                                          // Big table packed with data from premade piece square tables
                                                                          // Access using using PackedEvaluationTables[square][pieceType] = score
    private readonly int[][]
        UnpackedPestoTables = new[] {
            59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m,
            76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m,
            77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m,
            5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m,
            934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m,
            9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m,
            78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m,
            11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m,
            75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m,
            11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m,
            74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m,
            7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m,
            70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m,
            3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m,
            64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m,
            69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m,
        }.Select(packedTable =>
        new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                    .Select(square => (int)((sbyte)square * 1.461) + PieceValues[aspiration++ % 12])
                .ToArray()
        ).ToArray();

    public Move Think(Board board, Timer timer)
    {
        int[,,] historyTable = new int[2, 7, 64];
        int timeRemaining = timer.MillisecondsRemaining / 30;
        try
        {
            for (int depthLeft = 1, alpha = -36_000, beta = 36_000, maxEval; ;)
            {
                //iterative deepening
                maxEval = PVS(depthLeft, 0, alpha, beta);
                // DivertedConsole.Write("best move: {0}, value: {1}, depth: {2}\n", bestMoveRoot, maxEval, depthLeft);
                aspiration *= 2;
                if (maxEval <= alpha) alpha -= aspiration;
                else if (maxEval >= beta) beta += aspiration;
                else
                {
                    //reset aspiration window
                    aspiration = 12;
                    alpha = maxEval - aspiration;
                    beta = maxEval + aspiration;
                    depthLeft++;
                }
            }
        }
        catch
        {
            return bestMoveRoot;
        }


        int PVS(int depthLeft, int depthSoFar, int alpha, int beta)
        {
            if (timer.MillisecondsElapsedThisTurn > timeRemaining) depthSoFar /= 0;
            bool inCheck = board.IsInCheck(), notRoot = depthSoFar != 0, notPV = beta == alpha + 1, canFutilityPrune = false;
            if (inCheck) depthLeft++; //extend search depth if in check

            bool qsearch = depthLeft <= 0;
            Move bestMove = default;

            if (notRoot && board.IsRepeatedPosition()) return 0;

            ulong boardKey = board.ZobristKey;


            var (entryKey, entryMove, entryDepth, entryScore, entryBound) = transpositionTable[boardKey % 5_000_000];



            //transposition  table lookup
            if (notPV && entryKey == boardKey //verify that the entry is for this position (can very rarely be wrong)
                    && entryDepth >= depthLeft //verify that the entry is for a search of at least this depth
                    && (entryBound == 3 // exact score
                        || (entryBound == 2 && entryScore >= beta)// lower bound, fail high
                        || (entryBound == 1 && entryScore <= alpha)))
            {// upper bound, fail low
                return entryScore;
            }

            int maxEval = -36_000, eval, standPat = EvaluateBoard();
            //weird ass local method bs to save on tokens
            int Search(int newAlpha, int reduction = 1) => eval = -PVS(depthLeft - reduction, depthSoFar + 1, -newAlpha, -alpha);
            if (qsearch)
            {
                maxEval = standPat;
                if (maxEval >= beta) return beta;
                alpha = Max(alpha, maxEval);
            }
            else if (notPV && !inCheck)
            {
                //reverse futility pruning
                //Basic idea: if your score is so good you can take a big hit and still get the beta cutoff, go for it.
                if (standPat - 90 * depthLeft >= beta && depthLeft < 8) //TODO: tune this constant.
                    return beta; //fail hard, TODO: try fail soft

                if (depthLeft > 2)
                { //null move pruning  
                    board.ForceSkipTurn();
                    // eval = -PVS(depthLeft/2, depthSoFar + 1, -beta, -beta + 1);
                    Search(beta, 3 + depthLeft / 4);
                    board.UndoSkipTurn();
                    if (eval >= beta) return beta; //doing nothing was able to raise beta, so we can prune

                }

                canFutilityPrune = depthLeft <= 8 && standPat + depthLeft * 160 <= alpha;
            }


            Span<Move> legalMoves = stackalloc Move[256]; //stackalloc is faster than new
            board.GetLegalMovesNonAlloc(ref legalMoves, qsearch && !inCheck); //only generate captures in qsearch, but not if theres a check
            int origAlpha = alpha, numMoves = legalMoves.Length, moveIndex = -1;
            if (numMoves == 0 && !qsearch)
            {
                return inCheck ? -36_000 + depthSoFar : 0;
            }

            Span<int> scores = stackalloc int[numMoves];
            //lower score -> search first
            while (++moveIndex < numMoves)
            {
                /*
                Move ordering hierarchy:
                1. TT move
                2. captures (MVV/LVA)
                3. Killers
                4. history heuristic
                */
                bestMove = legalMoves[moveIndex];
                scores[moveIndex] = (bestMove == entryMove && entryKey == boardKey) ? -999_999_999 : //TT move
                    bestMove.IsCapture ? (int)bestMove.MovePieceType - 10_000_000 * (int)bestMove.CapturePieceType : //MVV/LVA
                    killerTable[depthSoFar] == bestMove ? -5_000_000 : //killers
                    historyTable[depthSoFar & 1, (int)bestMove.MovePieceType, bestMove.TargetSquare.Index]; //history heuristic
            }
            MemoryExtensions.Sort(scores, legalMoves);

            moveIndex = -1;
            while (++moveIndex < numMoves)
            {

                Move move = legalMoves[moveIndex];
                //use single ands to avoid compiler shortcutting on &&s
                //this way our increment is always executed
                //late move reduction condition
                bool quiet = scores[moveIndex] == 0; //move is not TT, capture, killer, or history
                if (canFutilityPrune && quiet) //can fp is only set to true in a not PV, not Qsearch if block, so safe to have it here.
                                               //scores[moveIndex] == 0 asserts that the move is quiet and hasnt caused beta cutoffs in the past
                    continue;
                //futility pruning
                board.MakeMove(move);
                //PVS
                //schizophrenia syntax incoming
                //saves 5 tokens. 
                if (moveIndex == 0 || qsearch || //conditions to do full window search

                    // only do a reduced search if the move is quiet
                    ((Search(alpha + 1, quiet ? 1 + (int)(Log(depthLeft) * Log(moveIndex) / 2) : 1) > alpha) && (quiet || eval < beta)))

                    Search(beta); //either in the PV, qsearch, or null window search failed high, so do a full window search

                board.UndoMove(move);

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                    // if (!notRoot && maxEval < beta && maxEval > origAlpha) 
                    if (!notRoot)
                        bestMoveRoot = move; //is verifying the bounds here actually needed?
                }

                alpha = Max(alpha, maxEval);

                if (alpha >= beta)
                {
                    //update history and killer move tables
                    if (!move.IsCapture)
                    {  //dont update history for captures
                        historyTable[depthSoFar & 1, (int)move.MovePieceType, move.TargetSquare.Index] -= depthLeft * depthLeft;
                        killerTable[depthSoFar] = move;
                    }
                    break;
                }
            }

            // Push to TT
            transpositionTable[boardKey % 5_000_000] = (boardKey, bestMove, depthLeft, maxEval, maxEval >= beta ? 2 : maxEval > origAlpha ? 3 : 1);

            return maxEval;
        }

        int EvaluateBoard() //Shamelessly stolen from TyrantBot
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = -1; ++piece < 6;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        // Gamephase, middlegame -> endgame
                        // Multiply, then shift, then mask out 4 bits for value (0-16)
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        middlegame += UnpackedPestoTables[square][piece];
                        endgame += UnpackedPestoTables[square][piece + 6];
                        //TODO: try and fit in stacked pawns

                        //bishop pair
                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 23;
                            endgame += 62;
                        }
                        // // Semi-open file bonus for rooks
                        //SPRT passed: Elo difference: 11.5 +/- 7.7, LOS: 99.8 %, DrawRatio: 30.0 %, n = 5428

                        if (piece == 3 && (0x101010101010101UL << (square & 7) & board.GetPieceBitboard(PieceType.Pawn, sideToMove > 0)) == 0)
                        {
                            middlegame += 22;
                            endgame += 10;
                        }

                        // Mobility bonus 

                        //mobility: SPRT passed: 12.4 +- 8.1, n = 4641
                        // int bonus = BitboardHelper.GetNumberOfSetBits(
                        //     BitboardHelper.GetPieceAttacks((PieceType)piece + 1, new Square(square ^ 56 * sideToMove), board, sideToMove > 0));
                        // middlegame += bonus;
                        // endgame += bonus * 2;



                    }
            // Tempo bonus to help with aspiration windows
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)
                // Tempo bonus to help with aspiration windows
                + gamephase;
        }


    }

}