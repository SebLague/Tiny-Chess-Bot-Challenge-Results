namespace auto_Bot_593;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_593 : IChessBot
{
    // repo: https://github.com/OneStig/guppy
    // chess challenge docs: https://seblague.github.io/chess-coding-challenge/documentation/

    // TELEMETRY //
    // int nodes;
    // int eval_recount = 0;
    // ========= //


    int[,,] pst = new int[2, 6, 64];

    int[] value_mg = { 82, 237, 365, 477, 1025, 0 },
          value_eg = { 94, 281, 297, 512, 936, 0 },
          value_gp = { 0, 1, 1, 2, 4, 0 },
          attack_units = { 1, 2, 2, 3, 5, 0 },

          bw = { 0, 1 };

    Move best_move;

    // max search depth, size of transposition table
    const int max_depth = 60, tpt_size = 1 << 20;

    // zobrist key, depth, eval, bound, best move found 
    (ulong, int, int, int, Move)[] tpt = new (ulong, int, int, int, Move)[tpt_size];

    public Bot_593()
    {
        // raw data in constructor saves 99,840 bytes after gc lmao
        // 1st row is mg, 2nd row is eg
        decimal[] raw_mgeg_table = {324454911572041844052197376m, 75076251637434867814284861m, 29123027899232255186595905m, 679463589935167471377853969m, 622764552386721285493885210m, 9783870073220884m, 853792511368197730866298880m, 660107126339636692766873359m, 42355312132823125893347631m, 31507670913842921742821907m, 41193295289308132637156892m, 666184410397949141357830169m, 11019859289303915835304809m, 651696172032278943469292370m, 121058234499184519289533982m, 623839121419587375156298789m, 36267811662495560175459078m, 75696286717145482010127m, 649257115644407590980692487m, 104114097994879113462164263m, 47172125096491727721087003m, 62899666870273375367088154m, 635939857660055141050500632m, 699991661452951898981543430m, 651703762498292834060345644m, 29151173424785625276490241m, 139102041054936954514987067m, 138040011692296212609002524m, 683085603252313994626742043m, 625066618091819514183947529m, 36305572174512961990690053m, 741220517434551086583382787m, 621524939860012836400934721m, 657665316748116284395360020m, 680769522489397229590678804m, 743647989619289918984833294m, 16932298947359490929466638m, 749591013482441976939708680m, 1885485064m,
                                    419128914820612491296374784m, 162398385016552208672296094m, 10889823461391402009127480m, 621404478098643831804404241m, 19406643487212640098848260m, 9253627301729288m, 685609957376361150176821248m, 746023164048269947162773311m, 8545505272307244887320856m, 60524270313517289798380566m, 626270941214619540590174736m, 726675582325096560821153044m, 671022900017996762837837597m, 648019698721222534142759179m, 15727850803858238216477443m, 4849962864146558573348864m, 627491617637458285197264646m, 622632385744292846723404808m, 658909485323780599656092164m, 12127148025175477157503749m, 16958045854339547543116299m, 2479270154810991257324039m, 634709704167874128796451330m, 658904827232388861928806152m, 6089524068962989632523526m, 65386094019524721194172931m, 60720566972174612052846107m, 21850713382660521851879454m, 68994107384350856324328451m, 14580560447633344444522003m, 675867604947451165696074249m, 719407971043805646467514660m, 42371590137476262778259274m, 36376545995402102452789774m, 79912135087302895496878612m, 645609101597349316523460122m, 646905261709119178293511955m, 646877163543929968236435972m, 40204639516m };

        // pre process the pst to reduce method calls to query the raw table

        foreach (int phase in bw)
        {
            // only go to 384, because last 6 are empty 0s off the board to make multiples of 10
            for (int i = 0; i < 384; i++)
            {
                BigInteger segment = (BigInteger.Parse(raw_mgeg_table[phase * 39 + i / 10].ToString()) >> 9 * (i % 10)) & 0x1FF;

                pst[phase, i / 64, i % 64] = (short)((segment & 0xFF) * ((segment & (1 << 8)) != 0 ? -1 : 1));
            }
        }
    }

    bool time_constraint(Timer timer, int moves)
    {
        return timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining /
            (moves < 8 ? 60 : (moves < 30 ? 25 : 35));
    }

    int Eval(Board board)
    {
        if (tpt[board.ZobristKey % tpt_size].Item1 == board.ZobristKey)
            return tpt[board.ZobristKey % tpt_size].Item3;

        int tb_ind(int n) => (7 - n / 8) * 8 + (n % 8);
        int safety_table(int ind) => (ind > 60 ? 500 : ind * ind / 8) / 2;

        int gamePhase = 24, attack_unit_sum = 0, mg_sum = 0, eg_sum = 0;

        foreach (int color in bw)
        {
            bool is_white = color == 1;
            ulong opp_king_squares = BitboardHelper.GetKingAttacks(board.GetKingSquare(!is_white));

            for (int i = 0; i < 6; i++)
            {
                PieceList pl = board.GetPieceList((PieceType)(i + 1), is_white);
                gamePhase -= value_gp[i] * pl.Count;

                foreach (Piece p in pl)
                {
                    int ind = is_white ? tb_ind(p.Square.Index) : p.Square.Index;

                    mg_sum += pst[0, i, ind] + value_mg[i];
                    eg_sum += pst[1, i, ind] + value_eg[i];
                    // DivertedConsole.Write($"mg {p_type_ind} {ind} {pst[0, p_type_ind, ind]}");

                    // king safety
                    attack_unit_sum += safety_table(attack_units[i] *
                        BitboardHelper.GetNumberOfSetBits(
                            opp_king_squares & BitboardHelper.GetPieceAttacks(
                                (PieceType)(i + 1),
                                p.Square,
                                board,
                                is_white)
                        )
                    );
                }
            }

            attack_unit_sum *= -1;
            mg_sum *= -1;
            eg_sum *= -1;
        }

        // in case of promotion resulting in < 24
        gamePhase = (Math.Max(gamePhase, 0) * 256 + 12) / 24;

        return ((mg_sum * (256 - gamePhase)) + ((eg_sum + attack_unit_sum) * gamePhase)) / 256
                * (board.IsWhiteToMove ? -1 : 1);
    }

    int Negamax(Board board, int depth, int alpha, int beta, Timer timer, int ply)
    {
        // nodes++;

        if (board.IsInCheckmate()) return -20000 + ply;
        if (board.IsDraw() || ply > 0 && board.IsRepeatedPosition()) return 0;

        ulong bkey = board.ZobristKey, bkey_mod = bkey % tpt_size;

        int bestValue = -20000;

        bool quiescence = depth <= 0;

        var cur_tpt = tpt[bkey_mod];

        // 1. ensure not on the root node
        // 2. ensure not a collision with different key
        // 3. ensure stored info calculated deeper
        // 4. check if alternative better score was reached
        if (ply > 0 && cur_tpt.Item1 == bkey && cur_tpt.Item2 >= depth &&
            (cur_tpt.Item4 switch
            {
                3 => true,
                2 => cur_tpt.Item3 >= beta,
                1 => cur_tpt.Item3 <= alpha,
                _ => false
            })) return cur_tpt.Item3;

        if (quiescence)
        {
            bestValue = Eval(board);

            if (bestValue >= beta)
                return bestValue;

            alpha = Math.Max(alpha, bestValue);
        }

        int f_alpha = alpha;

        Move[] consider_moves = board.GetLegalMoves(quiescence);
        int[] priority = new int[consider_moves.Length];

        for (int i = 0; i < consider_moves.Length; i++)
        {
            Move cur = consider_moves[i];

            // start search with last found best
            if (consider_moves[i] == cur_tpt.Item5 && cur_tpt.Item1 == bkey)
                priority[i] = -20000;

            if (consider_moves[i].IsCapture)
                // subtracting backwards cause sort in ascending
                priority[i] = value_mg[(int)cur.MovePieceType - 1] - 100 * value_mg[(int)cur.CapturePieceType - 1];


            // prioritize checks. but maybe testing all these moves is expensive?
            board.MakeMove(cur);

            if (board.IsInCheck())
                priority[i] -= 10000;

            board.UndoMove(cur);
        }

        // maybe sorting on the fly is better when alpha >= beta
        // for now pre sort everything

        Array.Sort(priority, consider_moves);

        Move local_best = Move.NullMove;

        foreach (Move move in consider_moves)
        {
            if (!time_constraint(timer, board.GameMoveHistory.Length))
                return bestValue;

            board.MakeMove(move);
            int value = -Negamax(board, depth - 1, -beta, -alpha, timer, ply + 1);
            board.UndoMove(move);

            if (bestValue < value)
            {
                bestValue = value;
                local_best = move;
            }

            alpha = Math.Max(alpha, value);

            if (alpha >= beta) break;
        }

        if (ply == 0)
            best_move = local_best;

        tpt[bkey_mod] = (bkey, depth, bestValue, bestValue >= beta ? 2 : bestValue > f_alpha ? 3 : 1, local_best);

        return bestValue;
    }

    public Move Think(Board board, Timer timer)
    {
        // DivertedConsole.Write("board: " + Eval(board));
        int d = 1;

        do
        {
            // nodes = 0;
            int eval = Negamax(board, d, -20000, 20000, timer, 0);
            // DivertedConsole.Write($"Depth: {d}; \x1b[31m{best_move}\x1b[0m: {eval} ; Nodes searched: {nodes} @ {timer.MillisecondsElapsedThisTurn}ms");
            d++;
        } while (d <= max_depth && time_constraint(timer, board.GameMoveHistory.Length));

        return best_move;
    }
}