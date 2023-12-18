namespace auto_Bot_475;
using ChessChallenge.API;
using System;
using System.Linq;

using static ChessChallenge.API.BitboardHelper;


public class Bot_475 : IChessBot
{
    private int search_depth;
    private int gamephase;
    private Move root_pv;

    private readonly int[] piece_val = { 0, 100, 325, 325, 550, 1000, 0 };
    private readonly int[] piece_phase = { 0, 0, 1, 1, 2, 4, 0 };

    // (hash, move, score, depth_left, bound), bound -> 0 = exact, 1 = upper, 2 = lower
    private readonly (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[0x400000];
    private int[][] psts; //[64][12] (by-square first, then by-piece)

    public Bot_475()
    {
        var piece = 1;
        psts = new[] {
            68081899401212949351183330304m, 73670758688305006739676386304m, 76456142740886764516124651264m, 76459764832296416777719441408m, 77388210416883349169475165696m, 2488002336983511269253042176m, 625038129140127300638275584m, 76764385654472114123671652864m,
            77671099058986219668768021553m, 2488002374621520341078961219m, 2185766307453684100277675038m, 2500091559241791159129477680m, 2512143112078849396563708962m, 5894736370460825713047904063m, 3731977597694681001073640465m, 1856924113500281980014098682m,
            1844839726240160097001597181m, 2479530411815298632336416260m, 3718674710972641856099586573m, 2504903743272550611008364560m, 3123878466545058162140129824m, 6831630435415371414219866140m, 6821958973090107499163558924m, 1862945297204349609082558198m,
            77992654474699207681920400633m, 3417642810718263122256332806m, 3728355673365476362326510083m, 4359386580110792730384865802m, 4057159902985061531909231116m, 4975938766200710881319461382m, 4057155087810940711538526472m, 640735443009719621042244596m,
            76741416232876432519247821554m, 78626126955954404390506726143m, 3106958357204215464561346814m, 3742843967663181786644284934m, 4353332506133939941593386504m, 3735566708391126736759556611m, 1263313277649292760673749509m, 77386944823170346427460287732m,
            76434330129670873005871396083m, 78901757246745411010924444926m, 1867781020430096257828521726m, 3098476968482732981210318331m, 3719845968529293281113606658m, 2486732132016271658234808322m, 1245179371043083992714251280m, 77993816066023776856830245114m,
            75192758571465755628019053294m, 77667429688115488444098602496m, 910325808837181453615299318m, 2156728402863263876965793524m, 2467398763637647446846472440m, 915142622288290228153747724m, 78898102023685516804391762195m, 77043628594532215874014738165m,
            71472889010414324384558795776m, 74259486783167468614954448384m, 76429513205177051403192492800m, 77654145800469620261496745984m, 75203643682267893909105080320m, 77353094826434173662909886976m, 75811752294884674459042379264m, 72705965084712024536828539904m }
            .Select(packedTable => new System.Numerics.BigInteger(packedTable)
                .ToByteArray()
                .Take(12)
                .Select(square => (sbyte)square * 2 + piece_val[piece++ % 6])
                .ToArray())
            .ToArray();
    }

    public Move Think(Board board, Timer timer)
    {
        var history_table = new int[2, 7, 64]; // [side_to_move][piece_type][square]
        var killer_moves = new Move[2048];
        var time_allowed = 2 * timer.MillisecondsRemaining / (35 + 1444 / (board.PlyCount / 2 /* <- # of full moves */ + 67));

        search_depth = 2;
        for (int alpha = -99999, beta = 99999; ;)
        {
            int score = NegaMax(0, alpha, beta);
            if (timer.MillisecondsElapsedThisTurn > time_allowed || score > 49000) return root_pv;

            /* Aspiration Windows */
            if (score <= alpha)
                alpha -= 65;
            else if (score >= beta)
                beta += 65;
            else
            {
                alpha = score - 45;
                beta = score + 45;
                search_depth++;
            }
        }


        /* SEARCH ---------------------------------------------------------------------------------- */
        int NegaMax(int depth, int alpha, int beta, bool allow_null = true)
        {
            if (depth > 0 && board.IsRepeatedPosition()) return 0;

            int score = Eval(),
                depth_left = search_depth - depth,
                move_idx = 0;

            /* Get Transposition Values */
            ref var entry = ref tt[board.ZobristKey & 0x3FFFFF];
            if (entry.Item1 == board.ZobristKey && entry.Item4 >= depth_left && /* is this needed? -> */ depth > 0
                && (entry.Item5 == 0
                || entry.Item5 == 1 && entry.Item3 <= alpha
                || entry.Item5 == 2 && entry.Item3 >= beta))
                return entry.Item3;

            /* Quiescence Search (delta pruning) */
            bool q_search = depth >= search_depth, can_f_prune = false;
            if (q_search)
            {
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            else if (!board.IsInCheck() && (beta - alpha == 1/* || gamephase > 0*/))
            {
                /* Null Move Pruning */
                if (depth_left >= 2 && allow_null && gamephase > 0)
                {
                    board.ForceSkipTurn();
                    score = -NegaMax(depth + 3 + depth_left / 4, -beta, -alpha, false);
                    board.UndoSkipTurn();
                    if (score >= beta) return score; // fail soft
                }

                /* Extended Futility Pruning */
                if (depth_left <= 4 && score + 96 * depth <= alpha) can_f_prune = true;
            }

            /* Move Ordering */
            Move pv = default;
            Span<Move> moves = stackalloc Move[128];
            board.GetLegalMovesNonAlloc(ref moves, q_search);

            // checking for checkmate / stalemate
            if (!q_search && moves.IsEmpty) return board.IsInCheck() ? depth - 50000 : 0;

            Span<int> move_scores = stackalloc int[moves.Length];
            foreach (Move move in moves)
                move_scores[move_idx++] = -(
                    move == entry.Item2 && entry.Item5 == 0 ? 99999 // PV move
                    : (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) ? 99998 // queen promotion
                    : move.IsCapture ? 88888 - (int)move.CapturePieceType + (int)move.MovePieceType // MVV-LVA
                    : killer_moves[board.PlyCount] == move ? 77777 // killer moves
                    : history_table[board.IsWhiteToMove ? 0 : 1, (int)move.MovePieceType, move.TargetSquare.Index] // history heuristic
                );
            MemoryExtensions.Sort(move_scores, moves);

            /* Main Search */
            move_idx = 0;
            foreach (var move in moves)
            {
                board.MakeMove(move);

                // don't prune captures, promotions, or checks (also ensure at least one move is searched)
                if (can_f_prune && !(move.IsCapture || board.IsInCheck() || move.IsPromotion || move_idx == 0))
                {
                    board.UndoMove(move);
                    continue;
                }

                score = -NegaMax(depth + 1, -beta, -alpha, true);
                board.UndoMove(move);

                if (score > alpha)
                {
                    alpha = score;
                    pv = move;
                    if (depth == 0) root_pv = move;
                }
                if (score >= beta)
                {
                    if (!move.IsCapture)
                    {
                        if (gamephase > 0) history_table[board.IsWhiteToMove ? 0 : 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth_left * depth_left;
                        killer_moves[board.PlyCount] = move;
                    }
                    break;
                }

                if (depth > 2 && timer.MillisecondsElapsedThisTurn > time_allowed) return 55555;
            }

            /* Set Transposition Values */
            var best = Math.Min(alpha, beta);
            if (!q_search && entry.Item4 <= depth_left) // only update TT if entry is shallower than current search depth
                entry = (
                    board.ZobristKey,
                    pv,
                    best,
                    depth_left,
                    alpha >= beta ? 2 /* lower bound */
                    : pv != default ? 0 /* exact bound */
                    : 1 /* upper bound */
                );

            return best;
        }


        /* EVALUATION ------------------------------------------------------------------------------ */
        int Eval()
        {
            if (board.IsDraw()) return 0;

            int score,
                mg = 0, eg = 0,
                side_multiplier = board.IsWhiteToMove ? 1 : -1;

            gamephase = 0;
            foreach (bool is_white in new[] { true, false }) //true = white, false = black (can likely be optimized for tokens if PSTs are changed)
            {
                for (var piece_type = PieceType.None; piece_type++ < PieceType.King;)
                {
                    int piece = (int)piece_type;
                    ulong mask = board.GetPieceBitboard(piece_type, is_white);
                    while (mask != 0)
                    {
                        int lsb = ClearAndGetIndexOfLSB(ref mask);

                        // piece values are included in PSTs
                        mg += psts[lsb][piece - 1];
                        eg += psts[lsb][piece + 5];
                        if (piece == 3 && mask != 0) eg += 75; // bishop pair bonus

                        gamephase += piece_phase[piece];
                    }
                };

                mg = -mg;
                eg = -eg;
            }

            score = (mg * gamephase + eg * (24 - gamephase)) / 24; // max gamephase = 24

            return score * side_multiplier;
        }
    }
}