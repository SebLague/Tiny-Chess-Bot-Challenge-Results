namespace auto_Bot_456;
using ChessChallenge.API;
using System;

public class Bot_456 : IChessBot
{
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = {
    657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086,
    364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588,
    421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452,
    162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453,
    347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514,
    329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460,
    257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958,
    384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824,
    365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484,
    329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
    347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452,
    384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716,
    366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428,
    329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844,
    329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863,
    419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224,
    366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995,
    365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612,
    401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596,
    67159620133902};
    struct Transposition
    {
        public ulong hash;
        public Move move;
        public int eval;
        public int depth;
        public byte flag;
    };

    Transposition[] TTable;
    ulong entries = 0x7FFFFF;
    Move[] killerMoves;

    Board lboard;

    public Bot_456()
    {
        killerMoves = new Move[1024];
        TTable = new Transposition[entries + 1];
    }

    public Move Think(Board board, Timer timer)
    {
        lboard = board;
        Move bestMove = TTable[board.ZobristKey & entries].move;
        for (int d = 1; ; d++)
        {
            Search(d, -99999, 99999, board.IsWhiteToMove ? 1 : -1);
            bestMove = TTable[board.ZobristKey & entries].move;
            if (timer.MillisecondsElapsedThisTurn * 120 > timer.MillisecondsRemaining) break;
        }
        return bestMove;
    }
    int Search(int depth, int alpha, int beta, int color)
    {
        bool qsearch = depth <= 0;
        int bestEval = -99999;
        int sAlpha = alpha;

        ref Transposition transposition = ref TTable[lboard.ZobristKey & entries];
        if (transposition.hash == lboard.ZobristKey && transposition.flag != 0 && transposition.depth >= depth)
        {
            int tEval = transposition.eval;
            if (transposition.flag == 1) return tEval;
            else if (transposition.flag == 2 && tEval >= beta) return tEval;
            else if (transposition.flag == 3 && tEval <= alpha) return tEval;
        }

        if (lboard.IsDraw()) return 0;
        if (lboard.IsInCheckmate()) return lboard.PlyCount - 99999;

        int eval = Evaluate(color);

        if (!lboard.IsInCheck() && !qsearch && depth <= 3 && eval >= beta + 200 * depth) return eval;

        Move[] moves = lboard.GetLegalMoves(qsearch && !lboard.IsInCheck());
        if (moves.Length == 0) return eval;
        if (qsearch)
        {
            if (eval >= beta) return beta;
            alpha = eval > alpha ? eval : alpha;
        }

        int[] movesScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int score = 0;
            int movePieceType = (int)move.MovePieceType;
            int capturePieceType = (int)move.CapturePieceType;
            int promotionType = (int)move.PromotionPieceType;

            Transposition tt = TTable[lboard.ZobristKey & entries];
            if (tt.move == move && tt.hash == lboard.ZobristKey)
                score += 100000;

            if (move == killerMoves[lboard.ZobristKey & 1023]) score += 10000;

            if (capturePieceType != 0) score = 100 * pieceValues[capturePieceType] - pieceValues[movePieceType];

            if (move.IsPromotion) score += pieceValues[promotionType] - 100;
            else if (movePieceType == 6) score -= 10;
            else
            {
                if (lboard.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    score -= pieceValues[movePieceType];
                }
            }

            movesScores[i] = score;
        }

        Array.Sort(movesScores, moves);
        Array.Reverse(moves);

        Move bestMove = moves[0];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            lboard.MakeMove(move);
            int val = -Search(depth - 1, -beta, -alpha, -color);
            lboard.UndoMove(move);
            if (val > bestEval)
            {
                bestEval = val;
                bestMove = move;
            }

            alpha = Math.Max(alpha, bestEval);
            if (alpha >= beta) break;
        }

        if (!qsearch)
        {
            transposition.eval = bestEval;
            transposition.hash = lboard.ZobristKey;
            transposition.move = bestMove;
            if (bestEval < sAlpha)
                transposition.flag = 3;
            else if (bestEval >= beta)
            {
                transposition.flag = 2;
                if (!bestMove.IsCapture)
                    killerMoves[depth] = bestMove;
            }
            else transposition.flag = 1;
            transposition.depth = depth;
        }

        return bestEval;
    }

    int GetPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    //from EvilBot tier 2 with some minor tweaks to save tokens
    int Evaluate(int color)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = 1; p <= 6; p++)
            {
                int ind;
                ulong mask = lboard.GetPieceBitboard((PieceType)p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[p];
                    ind = 128 * (p - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += GetPstVal(ind) + pieceValues[p];
                    eg += GetPstVal(ind + 64) + pieceValues[p];
                }
            }
            mg = -mg;
            eg = -eg;
        }
        return (mg * phase + eg * (24 - phase)) / 24 * color;
    }
}