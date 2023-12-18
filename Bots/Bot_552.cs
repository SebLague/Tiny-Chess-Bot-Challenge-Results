namespace auto_Bot_552;
//#define DEBUG

using ChessChallenge.API;
using System;
using System.Linq;
public class Bot_552 : IChessBot
{



    // Tyrants PSTs
    // why is tyrant so good at C#?

    private readonly int[] UnpackedPestoTables = new[] { 59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m, 77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m, 934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m, 78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m, 75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m, 74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m, 70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m, 64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m, }
    .SelectMany(packedTable =>
        decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                    // No point in only taking 12 bytes. Since we never access the last 4 anyway, we can just leave them as garbage
                    .Select((square, index) => (int)((sbyte)square * 1.461) + new[] { 92, 353, 355, 507, 1098, 0, 98, 278, 291, 515, 917, 0 }[index % 12])
                .ToArray()
        ).ToArray();


    Move rootBestMove;
    // Piece values in order of - NULL, PAWN, BISHOP , KNIGHT, ROOK, QUEEN, KING    
    // const int TTlength= 0x400000;
    (ulong, Move, int, int, int)[] TTtable = new (ulong, Move, int, int, int)[0x400000];

    public Move Think(Board board, Timer timer)
    {


        // Clear stuff
        var kMoves = new Move[2048];
        var hMoves = new int[2, 7, 64];
        var scores = new int[218];
        // Eval function
        int staticEvalPos()
        {
            int middlegame = 0, endgame = 0, sideToMove = 2, square, gamephase = 0;
            for (; --sideToMove >= 0; middlegame *= -1, endgame *= -1)
                for (int piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {

                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 28;
                            endgame += 49;
                        }

                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            middlegame -= 12;
                            endgame -= 31;
                        }

                        middlegame += UnpackedPestoTables[square * 16 + piece];
                        endgame += UnpackedPestoTables[square * 16 + piece + 6];
                    }
            // Tempo bonus to help with aspiration windows
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24) + gamephase / 2;
        }

        // Search function
        int Negamax(int depth, int alpha, int beta, int ply, bool notLastMoveNull)
        {

            bool isNotRoot = ply++ > 0, InCheck = board.IsInCheck(), notPvNode = alpha + 1 == beta, fprune = false, qsearch;
            ulong key = board.ZobristKey;

            // Check for repetition
            if (isNotRoot && board.IsRepeatedPosition()) return 0;

            int score, bestScore = -6000001;
            // Local function
            int Search(int nextAlpha, int Reduction = 1, bool canNull = true) => score = -Negamax(depth - Reduction, -nextAlpha, -alpha, ply, canNull);

            // Check Extensions
            if (InCheck) depth++;

            var (ttKey, ttMove, ttDepth, ttScore, ttBound) = TTtable[key % 0x400000];

            // TT cutoff
            if (ttKey == key)
            {
                if (notPvNode && ttDepth >= depth && (
                    ttBound == 3 // exact score
                        || ttBound == 2 && ttScore >= beta // lower bound, fail high
                        || ttBound == 0 && ttScore <= alpha // upper bound, fail low
                )) return ttScore;
            }

            // Internal Iterative Reductions (IIR)
            else if (depth > 4) depth--;

            ttBound = ttDepth = ttScore = 0;


            if (qsearch = depth <= 0)
            {
                bestScore = staticEvalPos();
                if (bestScore >= beta) return bestScore;
                alpha = Math.Max(alpha, bestScore);
            }
            else if (!InCheck && notPvNode)
            {

                int staticEval = staticEvalPos();
                if (depth <= 8)
                {
                    if (staticEval - 75 * depth >= beta) return staticEval;
                    fprune = staticEval + 140 * depth <= alpha;
                }

                if (staticEval >= beta && notLastMoveNull && depth >= 2)
                {
                    board.TrySkipTurn();
                    Search(beta, 3 + depth / 5, false);
                    board.UndoSkipTurn();
                    if (score > beta) return score;
                }
            }

            Span<Move> allMoves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref allMoves, qsearch);

            // Move ordering
            foreach (Move move in allMoves)
                scores[ttScore++] = -(ttMove == move && ttKey == key ? 1000000000 :
                                move.IsCapture ? 100000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                                move.IsPromotion ? 96000 * (int)move.PromotionPieceType :
                                kMoves[ply] == move ? 95000 :
                                hMoves[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            scores.AsSpan(0, allMoves.Length).Sort(allMoves);

            Move bestMove = ttMove;


            // Tree search
            foreach (Move move in allMoves)
            {

                if (timer.MillisecondsElapsedThisTurn * 30 >= timer.MillisecondsRemaining) depth /= 0;

                // Futility pruning
                if (fprune && ttDepth != 0 && scores[ttDepth] > -96000) continue;
                // if (notPvNode && bestScore <= 20000 && depth <= 6 && ttDepth > 2 + depth * depth && scores[ttDepth] > -95000) break;   

                board.MakeMove(move);
                // PVS + LMR
                bool canReduce = ttDepth > 3 && depth > 3;
                if (ttDepth++ == 0 || qsearch ||
                // If PV-node / qsearch, search(beta)
                Search(alpha + 1, canReduce ? notPvNode ? 3 : 2 : 1) < 999999 && score > alpha && (score < beta || canReduce)
                // If null-window search fails-high, search(beta)
                ) Search(beta);
                board.UndoMove(move);


                // Update best move if neccesary
                if (score > bestScore)
                {
                    bestScore = score;
                    if (!isNotRoot) rootBestMove = move;
                    // Only update bestMove on alpha improvements   
                    if (bestScore > alpha)
                    {
                        alpha = bestScore;
                        bestMove = move;
                        ttBound = 3;
                        // Fail-High condition
                        if (alpha >= beta)
                        {
                            if (!move.IsCapture)
                            {
                                kMoves[ply] = move;
                                hMoves[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                            }
                            ttBound--;
                            break;
                        }

                    }
                }

            } // End of tree search

            // Check stale or checkmate
            // If bestScore hasnt been updated, we cant be in qsearch, because then bestScore is staticEval
            // and there must be no legal moves to update bestScore
            // therefore, since there are no legal moves, we are in check/stalemate.
            if (bestScore == -6000001)
                return InCheck ? ply - 30000 : 0;

            // Check fail-high/low/exact score for TT
            // int bound = bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1;

            TTtable[key % 0x400000] = (key,
                                    bestMove,
                                    depth,
                                    bestScore,
                                    ttBound);

            return bestScore;

        }
#if DEBUG //#DEBUG
                int globalEval = 0, globalDepth = 0; // #DEBUG 
#endif //#DEBUG

        try
        {
            for (int depth = 0, alpha = -600000, beta = 600000; ;)
            {
                // Aspiration windows
                // TODO : Try different aspiration windows

                int eval = Negamax(depth, alpha, beta, 0, true);

                if (eval <= alpha) alpha -= 62;
                else if (eval >= beta) beta += 62;
                else
                {
                    alpha = eval - 17;
                    beta = eval + 17;
                    depth++;

#if DEBUG //#DEBUG
                        globalEval = eval;// #DEBUG 
                        globalDepth = depth; //#DEBUG
#endif //#DEBUG

                }
            }
        }
        catch { }

#if DEBUG //#DEBUG
                DivertedConsole.Write("Evaluation : {0} || Time : {1} || Depth : {2}" , //#DEBUG
                                globalEval, //#DEBUG
                                timer.MillisecondsElapsedThisTurn, //#DEBUG
                                globalDepth); // #DEBUG
#endif //#DEBUG
        return rootBestMove;
    }
}
