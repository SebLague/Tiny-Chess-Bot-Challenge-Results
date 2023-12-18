namespace auto_Bot_628;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.Convert;
using static System.Math;

// King Gᴀᴍʙᴏᴛ, A Joke Bot by toanth (aka ToTheAnd, which is easier to pronounce I guess)
// Thanks to everyone on the discord server who helped me write this engine (I can't even list all names, you know who you are!)

// King Gᴀᴍʙᴏᴛ Ⅳ is a standard, albeit strong, challenge engine where the King Middlegame Piece Square Table has been
// replaced with this:
// 255, 255, 255, 255, 255, 255, 255, 255, 
// 255, 255, 255, 255, 255, 255, 255, 255, 
// 255, 255, 255, 255, 255, 255, 255, 255, 
// 250, 250, 250, 250, 250, 250, 250, 250, 
// 200, 200, 200, 200, 200, 200, 200, 200, 
// 100, 100, 100, 100, 100, 100, 100, 100, 
//  50,  50,  50,  50,  50,  50,  50,  50, 
//   0,   0,   0,   0,   5,   0,   0,   0, 

// Features:
// - Alpha-Beta Pruning Negamax
// - Quiescent Search
// - Move Ordering:
// -- TT Move
// -- MVV-LVA
// -- Two Killer Moves
// -- History Heuristic
// - Transposition Table (move ordering and cutoffs, also used in place of static eval)
// - Iterative Deepening
// - Aspiration Windows
// - Principle Variation Search
// - Check Extensions
// - Pawn Move to 2nd/7th Rank Extensions (aka Passed Pawn Extensions)
// - Null Move Pruning
// - Late Move Reductions
// - Reverse Futility Pruning
// - Futility Pruning
// - Late Move Pruning
// - Internal Iterative Reductions
// - Time Management with a soft and hard bound
// - Eval Function:
// -- Piece Square Tables
// -- King on (Semi) Open File Malus
// -- Doubled Pawns Malus
// -- All eval weights tuned specifically for Gᴀᴍʙᴏᴛ using Gedas' tuner (https://github.com/GediminasMasaitis/texel-tuner),
//      tuned with datasets generated from self play (of a prior version based on my public general tuned PSTs) and
//      two publicly available datasets, lichess-big3-resolved and the zurichess quiet-labeled v7 dataset.

public class Bot_628 : IChessBot
{

    private (ulong, Move, short, byte, sbyte)[] tt = new (ulong, Move, short, byte, sbyte)[0x80_0000];

    private Move bestRootMove;

    // The "compression" is basically the same as my public (i.e., posted on Discord) compression to ulongs,
    // except that I'm now compressing to decimals and I'm not using the PeSTO values anymore.
    // Instead, these are my own tuned values, which are different from my public tuned values
    // because they are specifically tuned for King Gᴀᴍʙᴏᴛ's playstyle.
    // The values were tuned using Gedas' tuner (https://github.com/GediminasMasaitis/texel-tuner)
    // tuned under the assumption that the king mg table is modified.

    // Modified king middle game table to make the king lead his army (as he should)
    private static readonly byte[] weights = new[]
        // tuned values with additional score, Gᴀᴍʙᴏᴛ
{ 208063153150458622325293100m, 15682342277095336718331215916m, 17862073254533007938894580780m, 28993848255878051352383863852m, 25266692137604337784659873324m, 27430631391026990066160962348m, 26172111076271539771879985964m, 2682663467954403624427526444m, 26172176967737096112208692334m, 34270865850678084886824511630m, 37098534027005422770695479919m, 33410058887660581824132911522m, 38065636866709363910008874376m, 41426422051708640261174580371m, 41145918500806541029066570290m, 32696617406943010943609569808m, 30515871621706581698507796759m, 35798891418545368124946486831m, 41410725296276936301272013900m, 43581941734889116348673274963m, 44228660433808512153548805979m, 42649765607883535731683557227m, 42897547900264106899276737630m, 35172516818391737146598859311m, 27428293858110792459561755149m, 37672717475748669943891062567m, 42643858022592253047452960811m, 46690104665254284038682551598m, 46711803828559790887905954117m, 45446025309083856093597116987m, 42961668674194836996316034120m, 36128800718065307670610151467m, 23712032203204831173005824768m, 33650630551661478982315566624m, 41085538750994661787626009372m, 46063871480082052131389403697m, 46059026332213898642908085811m, 42640179464900655546098547240m, 38605932446840062652789718847m, 33638455847133473433555131418m, 21846636013514157421274677248m, 29271849175800440073925907996m, 36125249777110266856902518557m, 39828208453283882833982744602m, 40148564461283172764058089006m, 37672641769220111003978721570m, 31763322379079945569862315340m, 27724315178777278485335002408m, 15960347770659249787776022786m, 24627141640160101683000392736m, 28945438909283628048857977369m, 32670153803534771908499299599m, 33611864607805264413621640486m, 30483145438992624403376529723m, 24567805326371884075653355875m, 18635657795503422635219310633m, 6357816596486659951681209388m, 11307220660319016242922275628m, 17499371361932403565171125548m, 23675759742632019172112219948m, 15341396734006724797127803180m, 23078503145270531520487051564m, 14377844760509158994642944300m, 5388182382925415220359798828m, 5388182382925414868201832704m, 60331228170297136319483124743m }
        .SelectMany(decimal.GetBits).SelectMany(BitConverter.GetBytes).ToArray();


    public Move Think(Board board, Timer timer)
    {
        var history = new long[2, 7, 64];
        var killers = new Move[256];

        for (int depth = 1, alpha = -30_000, beta = 30_000; depth < 63 && timer.MillisecondsElapsedThisTurn <= timer.MillisecondsRemaining / 64;)
        {
            int score = negamax(depth, alpha, beta, 0, false);
            if (score <= alpha) alpha = score;
            else if (score >= beta) beta = score;
            else
            {
                alpha = beta = score;
                ++depth;
            }

            alpha -= 20;
            beta += 20;
        }

        return bestRootMove;

        int negamax(int remainingDepth, int alpha, int beta, int halfPly, bool allowNmp)
        {
            if (board.IsRepeatedPosition())
                return 0;
            ulong ttIndex = board.ZobristKey & 0x7f_ffff;
            var (ttKey, bestMove, ttScore, ttFlag, ttDepth) = tt[ttIndex];

            bool isNotPvNode = alpha + 1 >= beta,
                inCheck = board.IsInCheck(),
                allowPruning = isNotPvNode && !inCheck,
                trustTTEntry = ttKey == board.ZobristKey,
                stmColor = board.IsWhiteToMove;

            int phase = 0, mg = 7, eg = 7;
            foreach (bool isWhite in new[] { stmColor, !stmColor })
            {
                ulong pawns = board.GetPieceBitboard(PieceType.Pawn, isWhite);
                int numDoubledPawns = BitboardHelper.GetNumberOfSetBits(pawns & pawns << 8),
                    piece = 6;
                while (piece >= 1)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece--, isWhite); mask != 0;)
                    {
                        phase += weights[1024 + piece];
                        int psqtIndex = 16 * (BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^
                                              56 * ToInt32(isWhite)) + piece;

                        mg += weights[psqtIndex] + (34 << piece) + weights[piece + 1040];
                        eg += weights[psqtIndex + 6] + (55 << piece) + weights[piece + 1046];
                    }

                if (ToBoolean(0x0101_0101_0101_0101UL << board.GetKingSquare(!isWhite).File & pawns))
                    mg -= 40;

                mg = numDoubledPawns * 9 - mg;
                eg = numDoubledPawns * 32 - eg;
            }

            int bestScore = -32_000,
                standPat = trustTTEntry ? ttScore : (mg * phase + eg * (24 - phase)) / 24,
                moveIdx = 0,
                passedPawnExtension = 0,
                childScore;

            // Idea of using a local function for the recursion to save tokens originally from Tyrant (I think?),
            // whose engine was very influential (for many strong engines in the discord server) in general.
            // That being said, I've noticed a lot of similarities to his engines that have arisen through some kind
            // of "convergent evolution" when saving tokens instead of through copy-pasting / reading source code.
            int search(int minusNewAlpha, int reduction = 1, bool allowNullMovePruning = true) =>
                childScore = -negamax(remainingDepth - reduction + passedPawnExtension, -minusNewAlpha, -alpha, halfPly + 2, allowNullMovePruning);

            if (inCheck) ++remainingDepth;

            bool inQsearch = remainingDepth <= 0;

            if (inQsearch && (alpha = Max(alpha, bestScore = standPat)) >= beta)
                return standPat;

            if (isNotPvNode && ttDepth >= remainingDepth && trustTTEntry
                // Very token-efficient cutoff condition based on an implementation by cj5716 in the nn example bot by jw
                && (ttScore >= beta ? ttFlag != 1 : ttFlag != 0))
                return ttScore;

            ttFlag = 1;
            if (remainingDepth > 3 && bestMove == default)
                --remainingDepth;

            if (allowPruning)
            {
                if (!inQsearch && remainingDepth < 5 && standPat >= beta + 64 * remainingDepth)
                    return standPat;

                if (remainingDepth >= 4 && allowNmp && standPat >= beta)
                {
                    board.ForceSkipTurn();
                    search(beta, 3 + remainingDepth / 4, false);
                    board.UndoSkipTurn();
                    if (childScore >= beta)
                        return childScore;
                }
            }

            var legalMoves = board.GetLegalMoves(inQsearch);

            var moveScores = new long[legalMoves.Length];
            foreach (Move move in legalMoves)
                moveScores[moveIdx++] = -(move == bestMove ? 2_000_000_000
                    : move.IsCapture ? (int)move.CapturePieceType * 268_435_456 - (int)move.MovePieceType
                    : move == killers[halfPly] || move == killers[halfPly + 1] ? 250_000_000
                    : history[ToInt32(stmColor), (int)move.MovePieceType, move.TargetSquare.Index]);

            // This condition is slightly better than using `IsInCheckmate` and `IsDraw`, also not too token-hungry
            // Thanks to @cj5716 for this suggestion.
            if (moveIdx == 0)
                return inQsearch ? bestScore : inCheck ? halfPly - 30_000 : 0;

            Array.Sort(moveScores, legalMoves);

            moveIdx = 0;
            foreach (Move move in legalMoves)
            {
                bool uninterestingMove = moveScores[moveIdx] > -250_000_000;
                if (remainingDepth <= 5 && bestScore > -29_000 && allowPruning
                    && (uninterestingMove && standPat + 300 + 64 * remainingDepth < alpha
                        || moveIdx > 7 + remainingDepth * remainingDepth))
                    break;
                // same number of tokens as the `is` statement, but without a bugged token count (I hope)
                passedPawnExtension = ToInt32(move.MovePieceType == PieceType.Pawn && move.TargetSquare.Rank % 5 == 1);
                board.MakeMove(move);
                if (moveIdx++ == 0 ||
                    alpha < search(alpha + 1,
                        moveIdx >= (isNotPvNode ? 3 : 4)
                        && remainingDepth > 3
                        && uninterestingMove
                            // reduction values based on values from Stormphrax, originally from the Viridithas engine
                            ? Min((int)(1.0 + Log(remainingDepth) * Log(moveIdx) / 2.36) + ToInt32(!isNotPvNode), remainingDepth)
                            : 1
                        ) && childScore < beta)
                    search(beta);

                board.UndoMove(move);

                if (timer.MillisecondsElapsedThisTurn * 16 > timer.MillisecondsRemaining)
                    return 30_999;

                bestScore = Max(childScore, bestScore);
                if (childScore <= alpha)
                    continue;

                bestMove = move;

                if (halfPly == 0) bestRootMove = bestMove;
                alpha = childScore;
                ++ttFlag;

                if (childScore < beta) continue;
                ttFlag = 0;

                if (move.IsCapture) break;

                if (move != killers[halfPly])
                    killers[halfPly + 1] = killers[halfPly];
                killers[halfPly] = move;

                history[ToInt32(stmColor), (int)move.MovePieceType, move.TargetSquare.Index]
                    += 1L << remainingDepth;

                break;
            }

            tt[ttIndex] = (board.ZobristKey, bestMove, (short)bestScore, ttFlag, (sbyte)remainingDepth);

            return bestScore;
        }
    }
}