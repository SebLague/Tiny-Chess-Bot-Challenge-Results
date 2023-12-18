namespace auto_Bot_355;
using ChessChallenge.API;
using System;
public class Bot_355 : IChessBot
{
    Move bestmoveRoot = Move.NullMove;
    // public int nodes_visited = 0;
    int[] pieceVal = { 0, 101, 300, 305, 510, 1080, 20000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    // TT & some search code search adopted from https://github.com/jw1912/ 
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public float depth, score, bound;
        public TTEntry(ulong _key, Move _move, float _depth, float _score, float _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = 1 << 20;
    TTEntry[] tt = new TTEntry[entries];
    public bool curr_color;
    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }
    public Move Think(Board board, Timer timer)
    {
        curr_color = board.IsWhiteToMove ? true : false;
        bestmoveRoot = Move.NullMove;
        for (int depth = 1; depth <= 70; depth++)
        {
            Search(board, timer, -30000, 30000, depth, 0);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 35)
                break;
        }
        // if (curr_color)
        //     DivertedConsole.Write(Evaluate(board) / 100);
        // else
        //     DivertedConsole.Write(-Evaluate(board) / 100);
        // DivertedConsole.Write(nodes_visited);
        // nodes_visited = 0;
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
    public float Evaluate(Board board)
    {
        // nodes_visited++;
        float middlegame = 0, endgame = 0, gamephase = 0;
        int ind;
        for (int i = 0; i <= 1; i++)
        {
            int pawn_center = 0, pawn_center_extended = 0, pawn_shield = 0;
            int f1 = 0;
            for (int j = 1; j <= 6; j++)
            {
                bool tf_flag = i == 0 ? true : false;
                ulong mask = board.GetPieceBitboard((PieceType)j, tf_flag);
                if (j == 1 && f1 == 0)
                {
                    pawn_center += BitboardHelper.GetNumberOfSetBits(mask & 103481868288) * 10;
                    pawn_center_extended += BitboardHelper.GetNumberOfSetBits(mask & 66229406269440) * 6;
                    pawn_shield += BitboardHelper.GetNumberOfSetBits(mask & BitboardHelper.GetKingAttacks(board.GetKingSquare(tf_flag))) * 5;
                    middlegame += pawn_center + pawn_center_extended;
                    endgame += pawn_center + pawn_center_extended;
                    f1 = 1;
                }
                if (j == 3 && BitboardHelper.GetNumberOfSetBits(mask) == 2)
                {
                    middlegame += 25;
                    endgame += 25;
                }
                while (mask != 0)
                {
                    gamephase += piecePhase[j];
                    ind = 128 * (j - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (tf_flag ? 56 : 0);
                    middlegame += getPstVal(ind) + pieceVal[j];
                    endgame += getPstVal(ind + 64) + pieceVal[j];
                }
            }
            middlegame = -middlegame;
            endgame = -endgame;
        }
        return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24);
    }
    public float Search(Board board, Timer timer, float alpha, float beta, float depth, float ply)
    {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        float best = -30000;
        if (notRoot && board.IsRepeatedPosition())
            return 0;
        TTEntry entry = tt[key % entries];
        if (notRoot && entry.key == key && entry.depth >= depth && (entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;

        Move[] moves = board.GetLegalMoves(qsearch);
        int[] scores = new int[moves.Length];
        // Score moves
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (move == entry.move) { scores[i] = 100000; }
            // else if (move.IsPromotion)
            // {
            //     scores[i] = 750;
            // }
            else if (move.IsCapture)
            {
                scores[i] = (100 * (int)move.CapturePieceType) - (10 * (int)move.MovePieceType);
                if (curr_color)
                {
                    scores[i] += (move.TargetSquare.Index / 8) - (move.StartSquare.Index / 8);
                }
                else
                {
                    scores[i] -= (move.TargetSquare.Index / 8) - (move.StartSquare.Index / 8);
                }
            }
        }


        float eval = Evaluate(board);
        if (qsearch)
        {
            best = eval;
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }

        Move bestMove = Move.NullMove;
        float origAlpha = alpha;
        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 35) return 30000;
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }
            Move move = moves[i];
            board.MakeMove(move);
            float score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);
            if (score > best)
            {
                best = score;
                bestMove = move;
                if (ply == 0) bestmoveRoot = move;
                alpha = Math.Max(alpha, score);
                if (alpha >= beta) break;
            }
        }
        if (!qsearch && moves.Length == 0) return board.IsInCheck() ? (10 * ply) - 30000 : 0;
        float bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;
        tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);
        return best;
    }
}

