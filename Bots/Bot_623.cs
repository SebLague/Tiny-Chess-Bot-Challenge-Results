namespace auto_Bot_623;
using ChessChallenge.API;
using System;

/**
 * LoevBot - by Loev06
 * 
 * Made for the Chess Coding Challenge by Sebastian Lague.
 * https://github.com/SebLague/Chess-Challenge
 * 
 * This is my serious attempt at making a chess bot.
 * Next to this bot, I also made a video playback bot within this challenge (BadAppleBot)
 * 
 * Features:
 * - Iterative deepening
 *   - Aspiration window
 *
 * - Negamax with alpha-beta pruning
 *   - Transposition table
 *   - Null move reduction
 *   - Check extensions
 *   - Late move reduction
 *   - Move grading
 *     - PV-nodes
 *     - MVV-LVA
 *     - Killer moves
 *     - History heuristic
 *
 * - Quiescence search (Search function when depth <= 0)
 * - Eval
 *   - Reduced PeSTO tables (5 bits per value, containing midgame and engame piece square tables)
 */

public class Bot_623 : IChessBot
{
    int[] pieceValues = {82, 337, 365, 477, 1025,  0,
                         94, 281, 297, 512,  936,  0};
    ulong[] psts = { 187369446605816303, 380043467255254511, 452104393136187887, 447633746685174255, 478039642314496495, 588340443311606255, 557905963574867439, 450980624583427567, 489076535739127803, 628861848104937471, 590656021312431094, 628972901962829818, 629041006224263159, 700967790561319934, 624402227868876787, 589397043921040365, 591534488154416974, 633297241506208623, 629958025801125682, 591854451380739826, 626724360995432151, 738157634344386230, 740336868535984946, 590559190203028268, 518454346122671725, 626611107065742928, 663803223896278543, 661621761692456433, 661693229946162641, 697654957829545456, 663944991067358769, 552376478896375340, 441789796216878603, 520811698014600718, 625487440543008206, 660495857521082832, 659300656103344593, 623307041339393487, 588405244745467376, 477995548003416523, 447454444657193451, 519443905516090862, 590549290339221966, 623166301703520749, 623201489296836079, 625489538602976719, 555649692769602003, 481303976344958413, 413638964544843274, 485700960891838990, 556599703028282892, 584816471003576844, 587069302716709389, 555508923035436530, 522785255824176627, 450763971368366540, 303234771799344623, 382080820458041839, 450797000772165103, 477716311808846319, 414838540318683631, 481158846175653359, 453046596257722863, 342599451665478127 };

    // sizeof(TTEntry) = 24, allocate 100 mb:
    // 100 * 2^20 / 24 ~= 4369067
    // Round down to nearest power of 2:
    // 2^22 = 4194304 = 0x40_0000 entries (96 mb)
    TTEntry[] tt = new TTEntry[0x40_0000];

    struct TTEntry
    {
        public ulong key;
        public int score, depth;
        public byte flag;
        public Move bestMove;
        public TTEntry(ulong _key, int _score, byte _flag, int _depth, Move _bestMove)
        {
            (key, score, flag, depth, bestMove) = (_key, _score, _flag, _depth, _bestMove);
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] killers = new Move[512];
        int[,,] history = new int[2, 64, 64];
        Move bestMove = board.GetLegalMoves()[0]; // To prevent illegal moves with < 32 ms left.

        for (int depth = 1, eval, alpha = -100_000_050, beta = 100_000_050; ;)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining >> 5) break;

            eval = Search(alpha, beta, depth, 0, true);

            if (eval <= alpha)
                alpha -= 100;
            else if (eval >= beta)
                beta += 100;
            else
            {
                // DivertedConsole.Write($"[{depth}] {bestMove} ({eval})"); // #DEBUG
                alpha = eval - 30;
                beta = eval + 30;
                depth++;
            }
        }
        return bestMove;

        int Search(int alpha, int beta, int depth, int ply, bool nullMoveAllowed)
        {
            bool isQuiescence = depth <= 0,
                 isRoot = ply == 0;

            if (!isRoot && board.IsRepeatedPosition()) return 0;

            ulong key = board.ZobristKey;
            TTEntry ttEntry = tt[key & 0x3F_FFFF];

            if (ttEntry.key == key && ttEntry.depth >= depth)
            {
                int score = ttEntry.score;
                if (ttEntry.flag == 1 && score > alpha) alpha = score;  // Lower bound
                if (ttEntry.flag == 2 && score < beta) beta = score;    // Upper bound
                if (ttEntry.flag == 3 || alpha >= beta)
                {
                    if (isRoot) bestMove = ttEntry.bestMove;
                    return score;   // Exact score or fail high/low
                }
            }

            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, isQuiescence);
            int moveCount = moves.Length;

            int staticEval = -100_000_999,
                originalDepth = depth;

            if (isQuiescence)
            {

                // ** Eval ** //

                int mg = 0,
                    eg = 0,
                    phase = 0;

                foreach (bool isWhite in new[] { true, false })
                {
                    for (int type = 0; type < 6; type++)
                    {
                        ulong pieces = board.GetPieceBitboard((PieceType)(type + 1), isWhite);

                        while (pieces != 0)
                        {
                            ulong sqData = psts[BitboardHelper.ClearAndGetIndexOfLSB(ref pieces) ^ (isWhite ? 56 : 0)];
                            mg += GetPstValue(sqData, type * 2) + pieceValues[type];
                            eg += GetPstValue(sqData, type * 2 + 1) + pieceValues[type + 6];

                            phase += 0x042110 >> type * 4 & 0xf;

                            if (type == 0)
                            {

                            }
                        }
                    }
                    (mg, eg) = (-mg, -eg);
                }

                staticEval = (mg * phase + eg * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);

                // ** End eval ** //

                if (staticEval >= beta) return beta; // prune
                if (staticEval > alpha) alpha = staticEval;

            }
            else
            { // Not quiescence
                if (moveCount == 0) return board.IsInCheck() ? -10_000_000 + ply : 0;

                if (board.IsInCheck())
                    depth++;
                else if (nullMoveAllowed && !isRoot)
                { // Depth check might not be necessary
                    board.ForceSkipTurn();
                    int nullMoveScore = -Search(-beta, -beta + 1, depth - (depth > 6 ? 5 : 4), ply + 1, false);
                    board.UndoSkipTurn();

                    if (nullMoveScore >= beta) depth -= 4;
                }
            }

            // Grade moves
            Span<int> scores = stackalloc int[moveCount];
            for (int i = 0; i < moveCount; i++)
            {
                Move move = moves[i];
                scores[i] = ttEntry.key == key && ttEntry.bestMove == move ? 100_000_000 // Previous PV
                    : move.IsCapture ? (int)move.CapturePieceType * 5_000_000 + (int)move.MovePieceType // MVV/LVA
                    : killers[ply] == move ? 1_000_000 // Previous killer move
                    : history[ply & 1, move.StartSquare.Index, move.TargetSquare.Index]; // Other
            }

            // Saves a lot of tokens, but sorts the entire span, which is not necessary in case of an early cutoff:
            // MemoryExtensions.Sort(scores, moves);

            Move localBestMove = default;
            byte flag = 2; // Upper bound by default

            for (int i = 0; i < moveCount; i++)
            {
                // Incremental sort:
                int bestIndex = i;
                for (int j = i + 1; j < moveCount; j++)
                {
                    if (scores[j] > scores[bestIndex])
                        bestIndex = j;
                }

                Move move = moves[bestIndex];
                moves[bestIndex] = moves[i];
                scores[bestIndex] = scores[i];

                board.MakeMove(move);

                bool needsFullSearch = true;
                int eval = 0;

                if (i >= 3 && depth >= 3 && !move.IsCapture)
                {
                    eval = -Search(-alpha - 1, -alpha, depth - 2, ply + 1, nullMoveAllowed);
                    needsFullSearch = eval > alpha;
                }

                if (needsFullSearch)
                {
                    eval = -Search(-beta, -alpha, depth - 1, ply + 1, nullMoveAllowed);
                }

                board.UndoMove(move);

                // if (isRoot) DivertedConsole.Write($"[{depth}] {move} ({score})"); // #DEBUG
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining >> 5) return staticEval;

                if (eval > staticEval)
                {
                    staticEval = eval;
                    localBestMove = move;
                    if (isRoot) bestMove = move;

                    if (eval > alpha)
                    {
                        flag = 3;
                        alpha = eval; // improve alpha (eval is now exact)
                    }

                    if (eval >= beta)
                    {
                        flag = 1;

                        if (!move.IsCapture)
                        {
                            killers[ply] = localBestMove;
                            history[ply & 1, move.StartSquare.Index, move.TargetSquare.Index] += depth * depth;
                        }

                        break; // fail-high -> lower bound
                    }
                }
            }

            tt[key & 0x3F_FFFF] = new TTEntry(key, staticEval, flag, originalDepth, localBestMove);
            return staticEval;
        }
    }


    int GetPstValue(ulong sqData, int ind)
        // Data: per square, from lsb to msb in 5-bit words: pawn mg, pawn eg, knight mg, (...), king eg
        => (int)((sqData >> 5 * ind & 0b11111) - 15) * 8;
}