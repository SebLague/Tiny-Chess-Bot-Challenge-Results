namespace auto_Bot_572;
using ChessChallenge.API;
using System;
public class Bot_572 : IChessBot
{
    int[] pieceValues = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    bool timesUp = false;

    static ulong mask = 0x7FFFFF;
    TTEntry[] TT = new TTEntry[mask + 1];
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }
    Move bestmoveRoot;
    public Move Think(Board board, Timer timer)
    {
        bestmoveRoot = Move.NullMove;
        timesUp = false;
        for (int depth = 1; depth <= 100; depth++)
        {
            NegaMax(depth, board, -2147483647, 2147483647, timer, false, 0);
            if (timesUp)
            {
                break;
            }
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
    // Created by JW
    int GetPieceBonus(int index)
        => (int)(((psts[index / 10] >> (6 * (index % 10))) & 63) - 20) * 8;
    int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;
        foreach (bool color in new[] { true, false })
        {
            for (int p = 0; p < 7; p++)
            {
                ulong mask = board.GetPieceBitboard((PieceType)p, color);
                while (mask != 0)
                {
                    phase += piecePhase[p];
                    int ind = 128 * (p - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (color ? 56 : 0);
                    mg += GetPieceBonus(ind) + pieceValues[p];
                    eg += GetPieceBonus(ind + 64) + pieceValues[p];
                }
            }
            mg = -mg;
            eg = -eg;
        }
        return (mg * phase + eg * (24 - phase) / 24) * (board.IsWhiteToMove ? 1 : -1);
    }
    int NegaMax(int depth, Board board, int alpha, int beta, Timer timer, bool qSearch, int ply)
    {
        timesUp = timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30;
        if (timesUp || ply > 0 && board.IsRepeatedPosition()) return 0;
        if (board.IsInCheckmate()) return -10000000 + board.PlyCount;
        if (depth == 0 && !qSearch) return NegaMax(0, board, alpha, beta, timer, true, ply + 1);
        TTEntry entry = TT[board.ZobristKey & mask];

        // TT cutoffs
        if (ply > 0 && entry.key == board.ZobristKey && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;

        int best = -10000000;

        if (qSearch)
        {
            best = Evaluate(board);
            if (best >= beta) return best;
            if (alpha < best) alpha = best;
        }
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, qSearch);
        OrderMoves(moves, board);
        bool extend = false;

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            extend = board.IsInCheck();
            int score = -NegaMax(qSearch ? 0 : depth - 1 + (extend ? 1 : 0), board, -beta, -alpha, timer, qSearch, ply + 1);
            board.UndoMove(move);
            timesUp = timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30;
            if (timesUp) return 0;
            if (score > best)
            {
                best = score;
                bestMove = move;
                if (ply == 0) bestmoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;
            }
        }
        // Did we fail high/low or get an exact score?
        int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;

        // Push to TT
        TT[board.ZobristKey & mask] = new TTEntry(board.ZobristKey, bestMove, depth, best, bound);

        return best;
    }

    public void OrderMoves(Span<Move> moves, Board board)
    {
        int[] scores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType != PieceType.None) scores[i] += pieceValues[(int)moves[i].CapturePieceType] - pieceValues[(int)moves[i].MovePieceType]; // mvv lva
            if (TT[board.ZobristKey & mask].move == moves[i]) scores[i] += 10000000; // if the move is found earlier then bias it more in the search
            scores[i] = -scores[i];
        }
        scores.AsSpan(0, moves.Length).Sort(moves);
    }
}