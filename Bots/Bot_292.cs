namespace auto_Bot_292;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_292 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceVal = { 0, 100, 301, 311, 500, 900, 100000 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    int total = 0;
    Move depthMove;
    Move bestMove;
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, bound;
        public int score;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = (1 << 20);
    TTEntry[] transpositionTable = new TTEntry[entries];

    public Move Think(Board board, Timer timer)
    {
        int maxDepth = 1;
        bestMove = Move.NullMove;
        for (int depth = 1; depth < 100; depth++)
        {
            maxDepth = depth;
            moveEvaluater(board, timer, depth, -1000000, 1000000, 0);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
            bestMove = depthMove;
        }
        Move[] moves = board.GetLegalMoves();
        return bestMove == Move.NullMove ? moves[0] : bestMove;
    }
    private int moveEvaluater(Board board, Timer timer, int depth, int alpha, int beta, int ply)
    {
        if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 0;
        ulong zobristKey = board.ZobristKey;
        bool notRoot = ply > 0;
        if (notRoot && board.IsRepeatedPosition()) return 0;

        TTEntry entry = transpositionTable[zobristKey % entries];

        // TT cutoffs
        if (notRoot && entry.key == zobristKey && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;
        total++;
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;
        int best = positionEvaluator(board);
        if (depth <= 0) return best;

        List<Move> checks = new List<Move>();
        List<Move> captures = new List<Move>();
        List<Move> other = new List<Move>();
        foreach (Move move in moves)
        {
            if (move.IsCapture)
            {
                captures.Add(move);
            }
            else
            {
                board.MakeMove(move);
                if (board.IsInCheck())
                {
                    checks.Add(move);
                }
                else
                {
                    other.Add(move);
                }
                board.UndoMove(move);
            }
        }
        double origAlpha = alpha;
        best = -3000000;
        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 0;
            Move move = getMove(checks, captures, other, i);
            //DivertedConsole.Write(move);
            board.MakeMove(move);
            int score = -moveEvaluater(board, timer, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);
            if (score > best)
            {
                best = score;
                //bestMove = move;
                if (ply == 0) depthMove = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;

            }

        }
        int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;
        transpositionTable[zobristKey % entries] = new TTEntry(zobristKey, depthMove, depth, best, bound);
        return best;
    }
    private Move getMove(List<Move> checks, List<Move> captures, List<Move> others, int index)
    {
        if (index < checks.Count)
        {
            return checks[index];
        }
        else if (index < checks.Count + captures.Count)
        {
            return captures[index - checks.Count];
        }
        else
        {
            return others[index - (checks.Count + captures.Count)];
        }
    }
    private int materialDifference(Board board)
    {
        PieceList[] list = board.GetAllPieceLists();
        return list[0].Count + 3 * (list[1].Count + list[2].Count) + 5 * list[3].Count + 9 * list[4].Count - (list[6].Count + 3 * (list[7].Count + list[8].Count) + 5 * list[9].Count + 9 * list[10].Count);
    }
    private int positionEvaluator(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += getPstVal(ind) + pieceVal[piece];
                    eg += getPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }
    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }
}