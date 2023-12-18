namespace auto_Bot_591;

using ChessChallenge.API;
using System;
using System.Linq;

// Rigel
// By Antares
//
// Mentions: Gediminas Masaitas (Texel Tuner)
//           Tyrant (Inspired ideas)

public class Bot_591 : IChessBot
{
    // All evaluation weights have been tuned with Gedas' opensource Texel Tuner.
    // All the weights of PSTs, mobility, and material compressed into decimals.
    static int[] extractedWeights = new[] {
        69251902583734272318372872320M, 33290986577732883123276598221M, 36653947830725806750755485537M,
        33891576387814153902622668924M, 39769430593307724501406280285M, 18331303952854017275903115392M,
        55298181605741978464225688443M, 48149912587893467471538607056M, 43797538000786107621475975798M,
        34793491903412878687183538319M, 19890911780118340742485798705M, 37618850841811801098036527940M,
        49990931255051978816010094970M, 41315628180713555130979817105M, 38854311621544809735917896830M,
        37596943470319573912592942209M, 51862320424685470164898911900M, 48828115359492973334145645469M,
        31998298998813055885107294828M, 33266514275310400196067158634M, 39141926942608039251073263704M,
        54722832970674537845130556284M, 36971900453588309112052144753M, 41941874756269037439776436869M,
        36663671554120709935975330420M, 41633617857967651391421773940M, 46361896742735109514598710143M,
        32368557641266104752908828273M, 26756590075162876177957951312M, 14065102646439713892132355666M,
        25830591727064574293639851364M, 45057064138957445444221953888M, 60320481908012036576577093760M,
        49103589702312419123237800646M, 27345190777288560842329913206M, 22699213183600280812235151190M,
        39769430591495000367001070177M, 12208349130483086266248233088M, 47224918587345804056162763114M,
        39799815042107308093166751884M, 47203119647404492730665110905M, 32639148443416952831655772818M,
        46593864161424550341217964642M, 38842184529150031048663469457M, 39463596049229349744510011273M,
        36672167294268065048630493315M, 39449013140783283463354613116M, 36049556366667832162057221501M,
        44432342724559802756794256521M, 40066826366047504928633748623M, 42259860285171413198538967177M,
        33246014797378787401745072516M, 36976783377568175011384620407M, 41922650705828828130726606965M,
        49380395419705025581117315458M, 52511736202594377444932035502M, 35736340282870840005161091177M,
        20498887912030039480002837368M, 34776371790502295129543036757M, 40745185161729216363217125485M,
        54053026251579947684338966160M, 41961374463351401365278927278M, 45661711058926305668083648884M,
        18686930134887287588277358226M, 39464790825816126743139025019M, 308929141123064786799442707M,
        39769430595395432221031629184M, }.SelectMany(compressedWeights =>
        decimal.GetBits(compressedWeights).Take(3).SelectMany(BitConverter.GetBytes)
            .Select(element => (int)(sbyte)(element - 128)).ToArray()).ToArray();
    // Each weight is extracted above, with each element subtracted by 128 to
    // reverse the range of [0, 256) to [-128, 128).

    // Transposition Table: key | move | depth | score | flag
    (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[4194304];

    // Killers, an important move ordering heuristic
    Move[] killers = new Move[1024];

    // The returned best move
    Move bestMoveRoot;

    public Move Think(Board board, Timer timer)
    {
        // History Table: side | selectedPiece | targetSquare
        // combines history & evaluations: 4096 + 1024 size table
        var history = new int[5120];

        int returnEval = 0,
            iterativeDepth = 0;

        // Make negamax a local function so we don't have to pass more parameters
        int Negamax(int alpha, int beta, int depth, int ply)
        {
            // Initialize some variables in one place to save tokens declaring type
            bool inCheck = board.IsInCheck(), zwNode = alpha == beta - 1, color = board.IsWhiteToMove;
            int bestScore = -1000000, moveSortIndex = 0, currentCount = 0, flag = 2, mg = 0, eg = 0, gamePhase = 0, returnScore;
            ulong hashKey = board.ZobristKey;

            // Check Extensions
            // Increase the depth while you are in check
            // 15.2 +- 13.8 .... 5 Tokens
            if (inCheck) depth++;

            // TT Flags ordered such that Exact, Lowerbound, Upperbound = {1, 0, 2}
            // TT cutoffs:
            // Return scores for positions that were searched previously under certain conditions
            // including depth and ttFlag
            var (ttKey, ttMove, ttDepth, ttScore, ttFlag) = transpositionTable[hashKey % 4194304];
            if (zwNode && ttKey == hashKey && ttDepth >= depth &&
                ttFlag != 0 | ttScore >= beta &&
                ttFlag != 2 | ttScore <= alpha
            ) return ttScore;

            // Qsearch after check extensions
            // 26.3 +- 16.9 .... 1 Token
            bool qsearch = depth <= 0;

            // Local Function to save tokens, inspired from Tyrant
            // 9 Tokens Saved (+24 Tokens - 33 Tokens)
            int tokenNegamax(int newAlpha, int reduction = 1) =>
                returnScore = -Negamax(-newAlpha, -alpha, depth - reduction, ply + 1);

            /* EVALUATION FUNCTION
             * 
             */

            void evaluate()
            {
                for (int pieceType = 0; ++pieceType <= 6;)
                    for (ulong pieceBB = board.GetPieceBitboard((PieceType)pieceType, color); pieceBB != 0;)
                    {
                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB),

                            // Get the square of the piece relative to black's perspective
                            // with an xor 56 trick
                            relSquare = square ^ (color ? 56 : 0),

                            // Get the mobility count for a piece that attacks squares excluding your own pieces
                            mobilityCount = BitboardHelper.GetNumberOfSetBits(
                                BitboardHelper.GetPieceAttacks((PieceType)pieceType, new Square(square), board, color)
                                & ~(color ? board.WhitePiecesBitboard : board.BlackPiecesBitboard)
                                ),

                            phase = 0;

                        int value() =>
                            // PST evaluation
                            // 64 * (pieceType - 1) + relativeSquare + 384 * phase
                            extractedWeights[64 * pieceType - 64 + relSquare + 384 * phase] +

                            // Mobility evaluation
                            // 50+ Elo
                            // 768 + (pieceType - 1) + 6 * phase
                            extractedWeights[767 + pieceType + 6 * phase] * mobilityCount +

                            // Material evaluation
                            // 780 + (pieceType - 1) + 6 * phase and increment the phase after it is called
                            // effectively we do (quantizedWeight + 128) * 3.8 but distributed out with error
                            (int)(extractedWeights[779 + pieceType + 6 * phase++] * 3.8 + 488);

                        mg += value();
                        eg += value();

                        gamePhase += extractedWeights[791 + pieceType];
                    }

                // Flip the evaluation for the opponent
                mg = -mg;
                eg = -eg;
                color ^= true;
            }

            // Call evaluation for both sides
            evaluate();
            evaluate();

            // Tempo + Tapered Eval * side
            int staticEval = 11 + (mg * gamePhase + eg * (24 - gamePhase)) / 24;

            /* END OF EVALUATION
             * 
             */

            // Improving Heuristic:
            // If our current ply's evaluation is better than 2 plies ago, then we are improving.

            // history table extends from [0, 4096) so we effectively save our current evalution to
            // ply + 2 (which is index 4096 + 2 == 4098),
            // and retrieve past evaluation with ply (which is index 4096).
            history[ply + 4098] = staticEval;
            int improving = staticEval > history[ply + 4096] ? 1 : 0;


            // Combining Qsearch in Negamax,
            // standpat cutoffs
            if (qsearch && (bestScore = staticEval) >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore); // Qsearch handling

            // Negamax Forward Pruning and Early Exits
            if (ply > 0 && !qsearch)
            {

                // Draw Detection
                if (board.IsDraw()) return 0;

                // IIR
                if (!zwNode && depth >= 4 && ttMove == default) depth--;

                if (zwNode && !inCheck)
                {
                    // RFP (Reverse Futility Pruning)
                    // 47.1 +/- 20.3 .... 23 Tokens
                    // Less aggressive margins if the current side is improving,
                    // since it means the previous move must have been bad
                    if (depth <= 6 && staticEval - beta >= 120 * depth >> improving) return staticEval;

                    // NMP (Null Move Pruning)
                    // 80+ Elo
                    // 50 Tokens
                    if (depth >= 2 && staticEval >= beta && gamePhase > 0)
                    {
                        // Scale reductions on depth and increase it when improving
                        // since the evaluation must be better now
                        board.ForceSkipTurn();
                        tokenNegamax(beta, 3 + depth / 5 + improving);
                        board.UndoSkipTurn();
                        if (returnScore >= beta) return beta;
                    }
                }
            }

            var moves = board.GetLegalMoves(qsearch);

            if (!qsearch && moves.Length == 0) return inCheck ? ply - 900000 : 0;

            var scores = new int[moves.Length];

            // Assign scores for each move.
            // Scores are negated as the array is sorted in ascending order
            foreach (Move move in moves)
                scores[moveSortIndex++] = -(
                    ttKey == hashKey && move == ttMove ? 50000000 :
                    move.IsCapture ? 300000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    move == killers[ply] ? 100000 :
                    history[move.RawValue & 4095]
                );


            // Sort the array in ascending order based on the scores as a key.
            Array.Sort(scores, moves);

            // Move Loop
            Move bestMove = default;
            foreach (Move move in moves)
            {
                // Hard Time Limit
                // This allocates much more time, which tries to allow for a depth to finish before timing out
                if (depth > 2 && timer.MillisecondsElapsedThisTurn * 12 >= timer.MillisecondsRemaining) return 1000000;

                bool quiet = !move.IsCapture;
                if (quiet && zwNode && !inCheck)
                {
                    // LMP (Late Move Pruning)
                    // 50+ Elo
                    if (depth <= 4 && currentCount >= depth * (5 + 2 * improving)) break;

                    // FP (Futility Pruning)
                    // 79.0 +- 34.7 .... 14 Tokens
                    if (staticEval + depth * 128 <= alpha && currentCount >= 3) break;
                }

                board.MakeMove(move);

                // This LMR/PVS if statement framework is inspired from Tyrant et al.,
                // but all values and heuristics inside are original.
                // LMR + PVS is probably worth over 100 elo for ~50 tokens-
                // it is a very important heuristic.

                // PVS (Principal Variation Search)
                // Full search on first move for PVS, as we assume it is the best move.
                // Always conduct full searches in qsearch.
                if (currentCount == 0 || qsearch ||

                    // LMR (Late Move Reductions)
                    // If certain conditions are met, we can reduce late moves more.
                    // We should reduce pvNodes (!zwNode) less because they are the
                    // principle variation and more important.

                    (currentCount <= 3 || depth <= 2 || inCheck || !quiet ||
                     tokenNegamax(alpha + 1, 2 + depth / 9 + currentCount / 12 + Convert.ToInt32(zwNode)) > alpha) &&

                     // Zero Window Search / 3-fold.
                     // If a reduced search raised alpha then search with a zero-window.
                     // Search with a zero window if it's too risky to reduce, and it is not the principal variation
                     tokenNegamax(alpha + 1) > alpha && returnScore < beta)

                    // Full Depth & Window search
                    tokenNegamax(beta);

                // ---

                board.UndoMove(move);

                // -- Score Updating --
                if (returnScore > bestScore)
                {
                    // Fail Soft Updating
                    // (Returning and using scores outside of the alpha-beta bound)
                    // 63.0 +- 38.7 .... 11 Tokens
                    bestScore = returnScore;

                    if (returnScore > alpha)
                    {
                        flag = 1;   // Exact flag
                        alpha = returnScore;

                        // Best move saving only on alpha raises
                        bestMove = move;
                        if (ply == 0) bestMoveRoot = move;

                        if (alpha >= beta)
                        {
                            flag = 0;   // Lower bound flag

                            if (quiet)
                            {
                                // Killers
                                killers[ply] = move;

                                // History Bonus
                                // 81.6 +- 29.2 .... 60 Tokens (Total: 150 Elo .... 60 Tokens)
                                history[move.RawValue & 4095] += depth * depth;
                            }

                            break;
                        }
                    }
                }

                currentCount++;
            }
            // Record to the TT in an always-replace fashion
            // Do not record null moves to TT on possible fail-lows
            // 40.1 +- 26.3 .... 6 Tokens

            // Make sure qsearch entries are recorded with depth 0 so they
            // are treated the same
            // 35.4 +- 27.6 .... 8 Tokens
            transpositionTable[hashKey % 4194304] = (hashKey, bestMove == default ? ttMove : bestMove, qsearch ? 0 : depth, bestScore, flag);

            // Return fail soft score
            return bestScore;
        }
        // Soft Time Limit
        // This allocates much less time, which tries to prevent searching
        // another depth that can't be completed by timing out earlier
        for (; timer.MillisecondsElapsedThisTurn * 50 < timer.MillisecondsRemaining;)
        {
            // Aspiration Windows
            // 90.3 +- 45.6 .... ~40 Tokens
            int delta = ++iterativeDepth >= 5 ? 15 : 1000000,
                alpha = returnEval,
                beta = returnEval;
            do
            {
                alpha -= delta;
                beta += delta;
                delta *= 3;
                returnEval = Negamax(alpha, beta, iterativeDepth, 0);
            } while (returnEval <= alpha || returnEval >= beta);
        }
        return bestMoveRoot;
    }
}