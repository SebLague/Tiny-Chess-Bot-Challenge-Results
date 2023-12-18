namespace auto_Bot_420;
using ChessChallenge.API;
using System;

public class Bot_420 : IChessBot
{
    readonly float[] weights = new float[1543];

    // thanks Selenaut for transposition table without dictionary
    readonly (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[999979];

    public Bot_420()
    {
        for (int i = 1543; i-- != 0;)
        {
            weights[i] = (raw_weights[i / 8] >> (i % 8 * 8) & 0xFF) / 255f * 2.7869244f - 1.5312289f;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        // simple NN eval
        int eval()
        {
            var a1 = new float[769];
            var a2 = new float[2];
            //for (int i = 0; i < 769; i++) a1[i] = 0;
            var activate = (float z) => z < 0f ? z / 16f : z;

            for (int i = 7; --i != 0;)
            {
                foreach (var p in board.GetPieceList((PieceType)i, true))
                    a1[i + 5 + p.Square.Index * 12] = 1f;
                foreach (var p in board.GetPieceList((PieceType)i, false))
                    a1[i - 1 + p.Square.Index * 12] = 1f;
            }
            a1[768] = board.IsWhiteToMove ? 1f : 0f;

            for (int i = 2; i-- != 0;)
            {
                for (int j = 769; j-- != 0;)
                    a2[i] += a1[j] * weights[i * 769 + j];
                a2[i] = activate(a2[i] + weights[1538 + i]);
            }

            float a3 = weights[1542];
            for (int i = 2; i-- != 0;)
                a3 += a2[i] * weights[1540 + i];

            int eval = (int)((activate(a3) - 0.5f) * 64f * 130f);
            return board.IsWhiteToMove ? eval : -eval;
        }


        Move bestMoveRoot = default;
        var killers = new Move[128];
        // History tables stoled from JW's repo
        var history = new int[2, 4096];

        // https://www.chessprogramming.org/Negamax
        // https://www.chessprogramming.org/Quiescence_Search
        // All in once: Qs + Nega + PVS + LMR
        // thanks to Tyrant for search compression ideas

        int search(int alpha, int beta, int depth, int ply)
        {
            bool inCheck = board.IsInCheck(), qs = depth <= 0;

            int bestScore = -30000, moveIdx = 0;

            // Avoid repetitions if possible
            if (ply > 0 && board.IsRepeatedPosition())
                return 0;

            // QS static
            if (qs && (bestScore = alpha = Math.Max(alpha, eval())) >= beta)
                return alpha;

            // Search ext (without limit on them)
            if (inCheck)
                depth++;

            ulong key = board.ZobristKey;
            var (ttKey, ttMove, ttDepth, score, ttFlag) = tt[key % 999979];

            // TT Cutoffs
            if (beta - alpha == 1 && ttKey == key && ttDepth >= depth && (score >= beta ? ttFlag > 0 : ttFlag < 2))
                return score;

            // Reverse Futility Pruning
            if (!qs && !inCheck && depth <= 8 && eval() >= beta + 120 * depth)
                return beta;

            // Generate moves
            Span<Move> moves = stackalloc Move[220];
            board.GetLegalMovesNonAlloc(ref moves, qs);
            Span<int> scores = stackalloc int[moves.Length];

            // Checkmate/Stalemate
            if (moves.Length == 0)
                return qs ? alpha : inCheck ? ply - 30_000 : 0;

            // Score moves
            foreach (Move move in moves)
                scores[moveIdx++] = -(
                    move == ttMove
                        ? 900_000_000
                        : move.IsCapture
                            ? 100_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType
                            : move == killers[ply]
                                ? 80_000_000
                                : history[ply % 2, move.RawValue & 4095]
                );

            // in-place span sort
            MemoryExtensions.Sort(scores, moves);

            ttMove = default;
            moveIdx = ttFlag = 0;

            foreach (Move move in moves)
            {
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 15)
                    return 30000;

                board.MakeMove(move);

                // PVS + LMR
                if (moveIdx++ == 0
                    || depth < 2
                    || move.IsCapture
                    || (score = -search(-alpha - 1, -alpha, depth - 2 - moveIdx / 16, ply + 1)) > alpha)
                    score = -search(-beta, -alpha, depth - 1, ply + 1);

                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    ttMove = move;
                    if (score > alpha)
                    {
                        alpha = score;
                        ttFlag = 1;

                        if (ply == 0)
                            bestMoveRoot = move;

                        if (alpha >= beta)
                        {
                            if (!move.IsCapture)
                            {
                                killers[ply] = move;
                                history[ply % 2, move.RawValue & 4095] += depth;
                            }

                            ttFlag = 2;

                            break; // update TT later
                        }
                    }
                }
            }

            tt[key % 999979] = (key, ttMove, depth, bestScore, ttFlag);

            return bestScore;
        }

        int iterDepth = 1;
        //while (timer.MillisecondsElapsedThisTurn < 500)
        // TODO something a bit more clever
        while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 70)
            search(-30000, 30000, iterDepth++, 0);

        return bestMoveRoot;
    }

    // NN 769 -> 2 -> 1 to search all En Passant captures & evaluate position (poorly trained) (works worse than just PST)
    // (poorly trained) (works worse than just PST) (im too stupid to somehow compress larger NNs)
    readonly ulong[] raw_weights = {
        0x87998139506B7076, 0x516B6D7685948F93, 0x86988C8C8C988139, 0x907F8239536B6C76, 0x526B6D8788958F92, 0x81988D8F91768238, 0x8F788138526D6F90, 0x54736F8884968D8F, 0x7E978D948F77833A, 0x8D968337536B6E7B, 0x516C727885998C91, 0x84928F9189988135, 0x8C8A8336516B6F68, 0x4F656B6A85998A91, 0x84998C918F8C8334, 0x8B8C81324C64626C,
        0x4E67696E87988B93, 0x84998F918E8D8335, 0x918982364E696670, 0x5064606D84998C8F, 0x85988E8F918E822D, 0x8C8D802D5265706A, 0x53666C6A869A8E8E, 0x849A8D928D89812F, 0x8E35823954686D70, 0x5463647380998E93, 0x8299908F8A8A8237, 0x918C823452666573, 0x50626172849B8F93, 0x849B8E938F8C8235, 0x918C823353606073, 0x54696471829B8F90,
        0x849A8C918F8B8133, 0x8F8E7F315861626F, 0x5763666E83999293, 0x82988C8E8E308033, 0x8C2F813456686A75, 0x57646875829A8F90, 0x81988F918C898337, 0x8E87813556626478, 0x545F6078849A928F, 0x849B8C8F918A8233, 0x928B8134535F6176, 0x565F5F77849B8C8D, 0x819A8F908F898034, 0x923A7F3456626373, 0x56646675859A928F, 0x839A8D938E2C8031,
        0x9139813357686E77, 0x5A656C7981999293, 0x8697918F8E878137, 0x9188833357606476, 0x556266758399918B, 0x88998F938D888231, 0x8E4C813355626678, 0x56646578859A908E, 0x829A8F8E8C867F32, 0x8E3D7F345A656877, 0x5A666977829B9290, 0x819B939090337E31, 0x8E3C803359657177, 0x5960687B82968F94, 0x8297918D8F8A8133, 0x8E8881325763687B,
        0x586164788599908B, 0x8499908D91888134, 0x908B81325564657B, 0x5661657C839A8E8D, 0x869A8C8F8A878030, 0x8F897F3154626677, 0x57646C7A85998E90, 0x859990918F387F33, 0x91887F315A616D79, 0x56656E768296928F, 0x8693908D908B8030, 0x8E8B812F5462687D, 0x5668687D85998F8D, 0x86998D918E8A8233, 0x8F8D843453666883, 0x5461697683999090,
        0x87988E918E887F2F, 0x8C897E32555C6C77, 0x59636D788597918E, 0x849894928E868131, 0x8A7780304E657294, 0x5665718081989090, 0x81958F9188758031, 0x92858034516A6C8C, 0x4F666A7A85928F93, 0x82968E938F98813A, 0x8D7C80324A656E8F, 0x4E6B6F7E84948B90, 0x80968D8E8E768035, 0x8E757C3253667477, 0x57656F7784968E91, 0x82978F8E8C7A7D30,
        0x857FA59693907991, 0x989180822E5B696B, 0x3356666C8082A697, 0x7780A89395948283, 0x9794868430586972, 0x315667707A80A693, 0x8C80A69796987C84, 0x999277843056686C, 0x35576B6F7B81A897, 0x897DA79696928184, 0x9991908133576A6D, 0x2E5D676B777FA794, 0x7D82A69796948F84, 0x98948D82305A656E, 0x3058646E7E82A598, 0x8180A59695958C82,
        0x94958D8231576568, 0x3159676A8680A496, 0x7F83A59393938D83, 0x938F8C833257656B, 0x3059646A7E82A396, 0x7B83A59496968D83, 0x94948C84305A636A, 0x325B676B7C82A497, 0x3880A49692958D82, 0x93969082315B686E, 0x315864677B80A698, 0x7F81A49594948B81, 0x94938C8232576669, 0x325864678182A996, 0x7E83A89594938B82, 0x93958C8233596568,
        0x305865677E80A897, 0x7B83A59891928E82, 0x93928D8230596669, 0x3059656D3483A797, 0x317DA69797938D81, 0x95948E813158676A, 0x355A67697A81A696, 0x7D84A9998F968A81, 0x95948B8034586366, 0x315564697B7FA898, 0x7C85A89593938C81, 0x93938C8332566166, 0x335764667B83A897, 0x3B81A89994908C82, 0x95948D82305B6669, 0x2F5A68693081A897,
        0x367FA69696968A81, 0x94958D8133596B6D, 0x345765677782AA95, 0x7882A79794938980, 0x9395898233556267, 0x315764627A84A897, 0x4783A89794968B81, 0x95958C8331586463, 0x315763607580A895, 0x3783AA9997958E82, 0x96958E8230596466, 0x305B65693181A896, 0x3580A69695958A82, 0x92928E8235586D6E, 0x335764677181A697, 0x7280A69895958B82,
        0x9493898233546467, 0x325562657482A899, 0x7482A79697938E81, 0x94948E8230546463, 0x2F5467637082A794, 0x6E82A99594948F83, 0x95938F832F586465, 0x2E5B646B307EA893, 0x6B81A69793918D83, 0x96948A8434566970, 0x3156656B6A82A494, 0x6D80A69395908D83, 0x9693898430536164, 0x35526B6B6F80A695, 0x7182A89596929183, 0x91968C823253686A,
        0x2D5667626D81A594, 0x6D83A69591948D83, 0x97948C832D58676C, 0x3056696C6882A793, 0x7A82A38C93937C85, 0x9396768435586A6F, 0x31566C699781A497, 0x9983A59495907583, 0x92917B8430586F70, 0x32576B6B7580AC91, 0x757FA58F92948383, 0x9895778430566C6C, 0x30576D6D787CA690, 0x757FA69693989083, 0x92907C822E56686D, 0x3056676F7880A693,
        0xB900FFA0A88883,
    };
}