namespace auto_Bot_498;
/*
 * Kinglet
 *
 * Chess Engine Submission for the Sebastian Lague Chess Challenge
 * https://github.com/SebLague/Chess-Challenge
 *
 * Authors:
 * Skolin (https://github.com/Skoolin)
 * World  (https://github.com/codedeliveryservice)
 * Abyss  (https://github.com/Jakur)
 *
 * slightly less golfed code on github made public after the end of the challenge:
 * https://github.com/Skoolin/Secret-Chess-Challenge
 *
 * The challenge was great fun, thanks to Sebastian Lague for organising it and also
 * thanks to the other participants and the great discord community for making it
 * very enjoyable and competitive. Good luck to everyone in the tournament!
 */

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_498 : IChessBot
{
    private Move bestMove;

    /// <summary>
    /// <see href="https://www.chessprogramming.org/Transposition_Table">Transposition Table</see>
    /// for caching previously computed positions during search.
    /// </summary>
    private readonly
    (
        ulong,  // Zobrist
        int,    // Depth
        int,    // Evaluation
        int,    // Node type
        Move    // Best move
    )[] transpositionTable = new (ulong, int, int, int, Move)[4194304];

    /// <summary>
    /// <see href="https://www.chessprogramming.org/History_Heuristic">History Heuristic</see> for ordering quiet moves.
    /// </summary>
    private readonly int[] historyTable = new int[4096];

    /// <summary>
    /// <see href="https://www.chessprogramming.org/Killer_Move">Killer Move Heuristic</see> for ordering quiet moves.
    /// Used to retrieve moves that caused a beta cutoff in sibling nodes.
    /// </summary>
    private readonly Move[] killerMoves = new Move[1024]; // MAX_GAME_LENGTH = 1024

    /// <summary>
    /// Tightly packed <see href="https://www.chessprogramming.org/Piece-Square_Tables">Piece-Square Tables</see>.
    /// </summary>
    private readonly byte[] pieceSquareTables = new[] {
          282266529097455053078799364m,  2455919912745106374728623108m,  3386797516431034209084386052m,  4935431509659988786767212548m,
         4623519203538509630205605124m,  4928159028382896809030460420m,  4919701269940020779892941828m,   283465991737381893962998788m,
         4306785287174623693249196061m,  6172167345646778025007526687m,  6798390920280049164742310684m,  6184251881691207953985125151m,
         6806843937762779303503609117m,  7416133087452950235235501593m,  7413715272853297914227537166m,  6166089567246312935719123209m,
         5545934344726408926104007435m,  6788710069059982090422926095m,  7724423412988483705405259284m,  8033903663476395000086151445m,
         8038729921805153149300918294m,  7724404504784780671022745626m,  8026631218733624710112886805m,  6480410354417102158320187660m,
         4929386880782585395265746184m,  6794759494522601928612591117m,  7723214487308196977710217997m,  8348224450575394892306071055m,
         8350632839034632866990471954m,  8348214968876604148506771472m,  7729240245243244616223569680m,  6175765788827324583463500555m,
         3999713462245760773649871366m,  5862672928194510734078194699m,  7103035597184991926547068683m,  8037530514933872240074110478m,
         8035103200114651698059753486m,  7416133180328127843165550349m,  6792317975636973077153855502m,  5243679204196604490360173320m,
         3378316145942088905196711430m,  4926950158122208307057537547m,  6172143789362025367686886410m,  6789909605768598753627616522m,
         6791113809221167563237242637m,  6480415132478751653932774667m,  5237620444964642779316368914m,  4616232555091577144835059467m,
         2758132459595079043790875397m,  3996072480721656458214321419m,  4922114399431180703583973385m,  5544711233498253872944722696m,
         5544701807140537563279405835m,  5227958464622172391882896399m,  3678110841365349248083570963m,  2749660460480540245411704842m,
          900008696921407705696314372m,  1517779290601516084563160580m,  2754515034776499494753610244m,  3681761249173421225934138628m,
         2135535662706613888346369540m,  3680542823136497829114425348m,  1821210171323644351172128516m,   276184047857016610036851716m,
    }.SelectMany(decimal.GetBits).SelectMany(BitConverter.GetBytes).ToArray();

    /// <summary>
    /// The main search method of the engine. It uses <see href="https://www.chessprogramming.org/Iterative_Deepening">Iterative Deepening</see>
    /// </summary>
    public Move Think(Board board, Timer timer)
    {
        for (int depth = 0; 35 * timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining && ++depth < 64;)
            AlphaBeta(depth, -100_000_000, 100_000_000, true, true);

        return bestMove;

        int AlphaBeta(int depth, int alpha, int beta, bool nullMoveAllowed = true, bool root = false)
        {
            bool inCheck = board.IsInCheck();

            // Check extension in case of forcing sequences
            if (depth >= 0 && inCheck)
                depth += 1;

            bool inQSearch = depth <= 0;

            // Static evaluation using Piece-Square Tables (https://www.chessprogramming.org/Piece-Square_Tables)
            int mgScore = 0, egScore = 0, phase = 0;
            // Colors are represented by the xor value of the PSQT flip
            foreach (int xor in new[] { 56, 0 })
            {
                for (int piece = 0; piece < 6; piece++)
                {
                    ulong bitboard = board.GetPieceBitboard((PieceType)piece + 1, xor is 56);
                    while (bitboard != 0)
                    {
                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ xor;
                        int index = piece + 16 * square;

                        if (piece == 0 && (0x101010101010101UL << (square & 7) & bitboard) > 0)
                            egScore -= 9;

                        mgScore += pieceSquareTables[index];
                        egScore += pieceSquareTables[index + 6];
                        phase += 0b_0100_0010_0001_0001_0000 >> 4 * piece & 0xF;
                    }
                }

                mgScore = -mgScore;
                egScore = -egScore;
            }

            // Interpolate between game phases and add a bonus for the side to move
            int staticScore = 46 + (mgScore * phase + egScore * (24 - phase)) * (board.IsWhiteToMove ? 1 : -1),
                bestScore = -20_000_000, // Mate score
                moveCount = 0,           // Number of moves played in the current position
                nodeFlag = 3,            // Upper bound flag
                score;                   // Score of the current move

            if (inQSearch)
            {
                bestScore = staticScore;
                if (staticScore >= beta) return staticScore;
                if (alpha < staticScore) alpha = staticScore;
            }
            else if (!root && (board.IsRepeatedPosition() || board.IsFiftyMoveDraw()))
                return 0;

            // Transposition table lookup
            ulong zobrist = board.ZobristKey;
            var (ttZobrist, ttDepth, ttScore, ttFlag, ttMove) = transpositionTable[zobrist % 4194304];

            // The TT entry is from a different position, so no best move is available
            if (ttZobrist != zobrist)
                ttMove = default;
            else if (!root && ttDepth >= depth && (ttFlag != 3 && ttScore >= beta || ttFlag != 2 && ttScore <= alpha))
                return ttScore;
            else
                staticScore = ttScore;

            bool pvNode = alpha != beta - 1;

            if (!inQSearch && !root && !pvNode && !inCheck)
            {
                // Static null move pruning (reverse futility pruning)
                if (depth < 8 && beta <= staticScore - 236 * depth)
                    return staticScore;

                // Null move pruning: check if we beat beta even without moving
                if (nullMoveAllowed && depth >= 2 && staticScore >= beta)
                {
                    board.ForceSkipTurn();
                    score = -AlphaBeta(depth - 4 - depth / 6, -beta, 1 - beta, false);
                    board.UndoSkipTurn();
                    if (score >= beta) return beta;
                }
            }

            // Internal iterative reductions
            if (pvNode && depth >= 6 && ttMove == default)
                depth -= 2;

            var moves = board.GetLegalMoves(inQSearch);

            // Evaluate moves for Move Ordering (https://www.chessprogramming.org/Move_Ordering)
            Array.Sort(moves.Select(move =>
                // 1. PV move retrieved from the transposition table
                move == ttMove ? 0
                // 2. Queen promotion, don't bother with underpromotions
                : move.PromotionPieceType is PieceType.Queen ? 1
                // 3. Captures using MVV-LVA
                : move.IsCapture ? 1000 - 10 * (int)move.CapturePieceType + (int)move.MovePieceType
                // 4. Killer Move Heuristic
                : killerMoves[board.PlyCount] == move ? 10000
                // 5. History Heuristic with Negative Plausibility
                : 100_000_000 - historyTable[move.RawValue & 4095]
            ).ToArray(), moves);

            foreach (Move move in moves)
            {
                if (moveCount++ > 0 && !inQSearch && !root && !pvNode && !inCheck)
                {
                    // Late move pruning: if we've tried enough moves at low depth, skip the rest
                    if (depth < 4 && moveCount >= 8 * depth)
                        break;

                    // Futility pruning: if static score is far below alpha and this move is unlikely to raise it,
                    // this and later moves probably won't
                    if (depth < 6 && staticScore + 360 * depth + 290 < alpha && !move.IsCapture && !move.IsPromotion)
                        break;
                }

                board.MakeMove(move);

                if (
                    // full search in qsearch
                    inQSearch
                    || moveCount == 1
                    || (
                        // late move reductions
                        moveCount <= 5
                        || depth <= 2
                        || alpha < (score = -AlphaBeta(depth - moveCount / 15 - depth / 5 - (pvNode ? 1 : 2), -alpha - 1, -alpha))
                        )
                    &&
                        // zero window search
                        alpha < (score = -AlphaBeta(depth - 1, -alpha - 1, -alpha))
                        && score < beta
                        && pvNode
                    )
                    // full window search
                    score = -AlphaBeta(depth - 1, -beta, -alpha);

                board.UndoMove(move);

                // Avoid polling the timer at low depths, so it doesn't affect performance
                if (depth > 3 && 3 * timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining)
                    return 0;

                if (score > bestScore)
                    bestScore = score;

                if (score > alpha)
                {
                    nodeFlag = 1; // PV node
                    alpha = score;
                    ttMove = move;

                    if (root)
                        bestMove = ttMove;

                    if (score >= beta)
                    {
                        nodeFlag = 2; // Fail high
                        if (!move.IsCapture)
                        {
                            UpdateHistory(move, depth);
                            killerMoves[board.PlyCount] = move;
                        }

                        break;
                    }
                }

                if (!move.IsCapture)
                    UpdateHistory(move, -depth);
            }

            // Checkmate or stalemate
            if (!inQSearch && moveCount < 1)
                return inCheck ? board.PlyCount - 20_000_000 : 0;

            transpositionTable[zobrist % 4194304] = (zobrist, depth, bestScore, nodeFlag, ttMove);
            return bestScore;

            void UpdateHistory(Move move, int bonus)
            {
                ref int entry = ref historyTable[move.RawValue & 4095];
                entry += 32 * bonus * depth - entry * depth * depth / 512;
            }
        }
    }
}
