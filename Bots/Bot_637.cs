namespace auto_Bot_637;
/*

BOTvinnik v1.0
by Erik Fast (fasterik.net)

*/

using System;
using ChessChallenge.API;

public class Bot_637 : IChessBot
{
    readonly int[] eval_bonus = {
        // Material value
        100, 300, 330, 500, 900, 0,
        // Mobility bonus
        0, 10, 10, 5, 2, 0
    };

    Move search_move;
    int max_search_ms;
    bool search_expired;

    // Entry types:
    //  PV = 0
    //  FailHigh = 1
    //  FailLow = 2
    // (hash, score, depth, type, move)
    (ulong, int, int, int, Move)[] tt_entries = new (ulong, int, int, int, Move)[0x800000];

    int[] move_scores = new int[256];
    Move[] killer_moves = new Move[256];

    public Move Think(Board board, Timer timer)
    {
        search_expired = false;
        int time_left = timer.MillisecondsRemaining;

        // If there is <= 1 ms left, we want to return a legal move as quickly as possible.
        // Otherwise, we will use a % of remaining time plus increment.
        // If we are living on the increment, use 3/4 of remaining time.
        max_search_ms = time_left <= 1 ? 0 : Math.Min(time_left / 15 + timer.IncrementMilliseconds + 1, time_left * 3 / 4);

        // Starting at depth 2 improves move quality in time scrambles.
        int depth = 2, alpha = -50000, beta = 50000;
        for (; ; )
        {
            int score = search(board, timer, depth, 0, alpha, beta);
            if (max_search_ms == 0 || search_expired)
                break;

            // Aspiration windows: if the search fails low/high, extend the
            // window and re-search. Otherwise, initialize the window for the
            // next iteration centered around the current score.
            if (score <= alpha)
                alpha -= 100;
            else if (score >= beta)
                beta += 100;
            else
            {
                alpha = score - 25;
                beta = score + 25;
                depth++;
            }
        }

        return search_move;
    }

    int search(Board board, Timer timer, int depth, int ply, int alpha, int beta)
    {
        if (max_search_ms != 0 && timer.MillisecondsElapsedThisTurn >= max_search_ms)
        {
            search_expired = true;
            return 0;
        }

        bool is_root = ply == 0, quiesce = depth <= 0, in_check = board.IsInCheck();
        int move_index = 0, node_type = 0, fail_low = alpha, score, entry_score;
        Move best_move = default;

        // Draw detection.
        if (!is_root && !quiesce && (board.IsRepeatedPosition() || board.IsInsufficientMaterial()))
            return 0;

        // Check extension.
        if (in_check)
            depth++;

        // Transposition table lookup.
        ulong hash = board.ZobristKey;
        var (tt_hash, tt_score, tt_depth, tt_type, tt_move) = tt_entries[hash & 0x7fffff];

        // Skip searching this node if the TT score is valid.
        if (tt_hash == hash && tt_depth >= depth)
            switch (tt_type)
            {
                case 0:
                    if (!is_root)
                        // Return mate scores relative to the root.
                        return tt_score < -29000 ? tt_score + ply : tt_score > 29000 ? tt_score - ply : tt_score;
                    break;
                case 1:
                    if (tt_score >= beta)
                        return beta;
                    break;
                case 2:
                    if (tt_score <= alpha)
                        return alpha;
                    break;
            }


        if (quiesce)
        {
            // Establish a stand pat score for quiescence search.
            score = evaluate(board);
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, quiesce);

        // Checkmate/stalemate detection.
        if (!quiesce && moves.Length == 0)
            return in_check ? -30000 + ply : 0;

        // Sort moves.
        foreach (var move in moves)
        {
            move_scores[move_index++] = -(
                // TT move first
                tt_hash == hash && tt_move == move ? 1000 :
                // Killer move heuristic
                move == killer_moves[ply] ? 900 :
                // MVV-LVA
                move.IsCapture ? (int)move.CapturePieceType * 100 + 6 - (int)move.MovePieceType : 0
            );
        }
        move_scores.AsSpan(0, moves.Length).Sort(moves);

        move_index = 0;
        foreach (var move in moves)
        {
            board.MakeMove(move);

            // Principal variation search: assume that the first move is going
            // to be the PV move. For subsequent moves, do a null window search
            // first, and if it fails high then re-search with the full window.
            if (quiesce || move_index++ == 0)
                score = -search(board, timer, depth - 1, ply + 1, -beta, -alpha);
            else
            {
                score = -search(board, timer, depth - 1, ply + 1, -alpha - 1, -alpha);
                if (score > alpha)
                    score = -search(board, timer, depth - 1, ply + 1, -beta, -alpha);
            }

            board.UndoMove(move);

            if (search_expired)
                return 0;

            if (score >= beta)
            {
                node_type = 1;
                killer_moves[ply] = move;
                break;
            }

            if (score > alpha)
            {
                alpha = score;
                best_move = move;

                if (is_root)
                    search_move = best_move;
            }
        }

        if (node_type == 0)
        {
            score = entry_score = alpha;

            if (alpha == fail_low)
                node_type = 2;
            else
                // Store mate scores relative to the current ply.
                entry_score = score < -29000 ? score - ply : score > 29000 ? score + ply : score;
        }
        else
            score = entry_score = beta;

        if (!quiesce)
            tt_entries[hash & 0x7fffff] = (hash, entry_score, depth, node_type, best_move);

        return score;
    }

    int evaluate(Board board)
    {
        int mg_score = 0, eg_score = 0, stage = 0;

        for (int color = 0; color < 2; color++)
        {
            bool is_white = color == 0;

            ulong enemy_king_zone = 1UL << board.GetKingSquare(!is_white).Index;
            enemy_king_zone |= enemy_king_zone >> 1 & 0x7f7f7f7f7f7f7f7fUL;
            enemy_king_zone |= enemy_king_zone << 1 & 0xfefefefefefefefeUL;
            enemy_king_zone |= is_white ? enemy_king_zone >> 8 : enemy_king_zone << 8;

            // King pawn shelter
            mg_score -= 25 * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Pawn, !is_white));
            enemy_king_zone |= enemy_king_zone << 8;
            enemy_king_zone |= enemy_king_zone >> 8;

            for (int type = 0; type < 6; type++)
            {
                ulong bb = board.GetPieceBitboard((PieceType)type + 1, is_white);
                int count = BitboardHelper.GetNumberOfSetBits(bb),
                    material_score = count * eval_bonus[type];

                // Stage contribution: pawn/king -> 0, knight/bishop/rook -> 1, queen -> 2
                // Multiply by a magic number and shift produces the same values without a lookup table.
                stage += count * (0x1a * type >> type & 3);

                // Material value.
                mg_score += material_score;
                eg_score += material_score;

                while (bb != 0)
                {
                    int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb),
                        file = sq & 7,
                        rank = sq >> 3;

                    if (type == 0)
                    {
                        // Put pawns in the center.
                        if ((1UL << sq & 0x0000001818000000UL) != 0)
                            mg_score += 33;

                        // Push pawns in the endgame.
                        eg_score += (rank ^ 7 * color) * 17;

                        // Penalize doubled pawns.
                        if ((0x0101010101010101UL << file & bb) != 0)
                        {
                            mg_score -= 25;
                            eg_score -= 25;
                        }
                    }

                    if (type == 5)
                    {
                        // Move the king away from the center in the
                        // middlegame, and towards it in the endgame.
                        int center_manhattan_distance = (file ^ file - 4 >> 8) + (rank ^ rank - 4 >> 8) & 7;
                        mg_score += center_manhattan_distance * 25;
                        eg_score += center_manhattan_distance * -25;
                    }

                    if (type <= 4)
                    {
                        // Mobility and king safety.
                        ulong attacks = BitboardHelper.GetPieceAttacks((PieceType)type + 1, new Square(sq), board, is_white);
                        int mobility_score = BitboardHelper.GetNumberOfSetBits(attacks) * eval_bonus[type + 6];
                        mg_score += mobility_score + BitboardHelper.GetNumberOfSetBits(attacks & enemy_king_zone) * 13;
                        eg_score += mobility_score;
                    }
                }
            }

            // Flip the perspective. When we break out of the loop, score will
            // be from white's perspective again.
            mg_score = -mg_score;
            eg_score = -eg_score;
        }

        return (mg_score * stage + eg_score * (16 - stage)) / (board.IsWhiteToMove ? 16 : -16);
    }
}