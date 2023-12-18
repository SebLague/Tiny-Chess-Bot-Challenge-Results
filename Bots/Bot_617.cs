namespace auto_Bot_617;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_617 : IChessBot
{

    //Piece-Square Tables
    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    readonly int[] pieceVal = { 0, 100, 310, 330, 500, 920, 50000 };
    readonly int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    readonly ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    struct Transposition
    {
        public ulong zobristHash;
        public Move move;
        public int evaluation;
        public sbyte depth;
        public byte flag;
    };

    Move[] m_killerMoves;
    Transposition[] m_TPTable;

    public Bot_617()
    {
        m_killerMoves = new Move[1024];
        m_TPTable = new Transposition[0x800000];
    }

    public Move Think(Board board, Timer timer)
    {
        Transposition bestMove = m_TPTable[board.ZobristKey & 0x7FFFFF];
        for (int depth = 1; ; depth++)
        {

            NegaScout(-1000000, 1000000, depth, 0, board);
            bestMove = m_TPTable[board.ZobristKey & 0x7FFFFF];

            if (timer.MillisecondsElapsedThisTurn * 100 > timer.MillisecondsRemaining) break;
        }
        return bestMove.move;
    }

    int NegaScout(int alpha, int beta, int depth, int ply, Board board)
    {
        bool qsearch = depth <= 0;
        bool pvNode = beta > alpha + 1;
        int startingAlpha = alpha;
        int bestScore = -1000005;

        //See if we've checked this board state before 
        ref Transposition transposition = ref m_TPTable[board.ZobristKey & 0x7FFFFF];

        if (!pvNode && transposition.zobristHash == board.ZobristKey && transposition.depth >= depth)
        {
            //If we have an "exact" score (a < score < beta) just use that
            if (transposition.flag == 1) return transposition.evaluation;
            //If we have a lower bound better than beta, use that
            if (transposition.flag == 2 && transposition.evaluation >= beta) return transposition.evaluation;
            //If we have an upper bound worse than alpha, use that
            if (transposition.flag == 3 && transposition.evaluation <= alpha) return transposition.evaluation;
        }

        if (board.IsDraw()) return -10;
        else if (board.IsInCheckmate()) return -999999 + ply;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // QSearch: https://www.chessprogramming.org/Quiescence_Search
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        if (qsearch)
        {
            int QSearchScore = Evaluation(board);
            if (QSearchScore >= beta) return beta;
            if (QSearchScore > alpha) alpha = QSearchScore;
        }
        //if (!pvNode && !qsearch && depth <= 2 && !board.IsInCheck() && QSearchScore >= beta + 125 * depth) return QSearchScore;

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Move Ordering
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        Move[] allMoves = board.GetLegalMoves(qsearch && !board.IsInCheck());
        if (allMoves.Length == 0) return alpha;
        int[] MoveScore = new int[allMoves.Length];

        for (int i = 0; i < allMoves.Length; i++)
        {
            Move move = allMoves[i];
            if (transposition.move == move && transposition.zobristHash == board.ZobristKey) MoveScore[i] = 100000;
            else if (move == m_killerMoves[ply]) MoveScore[i] = 10;
            else if (move.IsCapture) MoveScore[i] = 1000 + 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }
        Array.Sort(MoveScore, allMoves);
        Array.Reverse(allMoves);

        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
        // Search
        //+++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

        Move bestMove = allMoves.First();


        int betaOrBetter = beta;

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);

            int score = -NegaScout(-betaOrBetter, -alpha, depth - 1, ply + 1, board);

            if (alpha < score && score < beta && betaOrBetter != beta) score = -NegaScout(-beta, -score, depth - 1, ply + 1, board);

            board.UndoMove(move);

            //adjust search window

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, bestScore);

            if (alpha >= beta) break;
            if (!qsearch) betaOrBetter = alpha + 1;
        }

        //After finding best move from this board state,
        //update TT with new best move;
        //also mark if this is a "killer move"
        if (!qsearch)
        {
            transposition.evaluation = bestScore;
            transposition.zobristHash = board.ZobristKey;
            transposition.move = bestMove;
            if (bestScore < startingAlpha) transposition.flag = 3;
            else if (bestScore >= beta)
            {
                transposition.flag = 2;
                if (!bestMove.IsCapture) m_killerMoves[depth] = bestMove;
            }
            else transposition.flag = 1;
            transposition.depth = (sbyte)depth;
        }
        return alpha;
    }

    // Returns basic evaluation of the board
    // Player always wants to MAX Evaluation, so if Black runs Eval, the higher the better
    int Evaluation(Board board)
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
                    int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);
                    ind = 128 * (piece - 1) + squareIndex ^ (stm ? 56 : 0);
                    mg += GetPstVal(ind) + pieceVal[piece];
                    eg += GetPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        int Eval = (mg * phase + eg * (24 - phase)) / 24;

        return board.IsWhiteToMove ? Eval : -Eval;
    }
    public int GetPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

}