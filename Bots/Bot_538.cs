namespace auto_Bot_538;
/*
-----------------------{|---()---|}-----------------------
             Bot Created By James Bond 73
     Special Thanks To WhiteMouse for the help given
       Inspiration taken from Tyrant and JacquesRW
-----------------------{|---()---|}-----------------------
*/
using ChessChallenge.API;
using System;

public class Bot_538 : IChessBot
{
    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    private readonly Move[] killers = new Move[2048];
    private readonly int[] moveScores = new int[218];


    // Transposition Tables
    struct Transposition
    {
        public ulong zobristHash;
        public Move move;
        public int evaluation;
        public sbyte depth;
        public byte flag;
    };


    // Transposition Table Initialiser
    private Transposition[] m_TPTable = new Transposition[0x800000];


    // Piece Value Counter
    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }


    public Move Think(Board board, Timer timer)
    {
        Move bestmoveRoot = Move.NullMove;


        // Reset history tables
        var historyHeuristics = new int[2, 7, 64];


        // Reset Null Move Pruning var
        bool do_null = true;


        // Iterative Depening Vars
        int intThinkTime = timer.MillisecondsRemaining / 30;
        int intPlyDepth = 2;


        // Iterative Deepening
        while (intPlyDepth < 90)
        {
            int rootValue = Negamax(-30000, 30000, intPlyDepth, 0);

            if (timer.MillisecondsElapsedThisTurn >= intThinkTime)
            {
                break;
            }

            if (rootValue > 99000)
            {
                break;// Mate found, no point in searching deeper
            }

            intPlyDepth += 1;

        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;

        // Eval based on Piece Square Tables
        int Evaluate()
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
                        ulong file = 0x101010101010101UL << (ind & 7);
                        mg += getPstVal(ind) + pieceVal[piece];
                        eg += getPstVal(ind + 64) + pieceVal[piece];

                        // Doubled pawns penalty
                        if (piece == 0 && (0x101010101010101UL << (ind & 7) & mask) > 0)
                        {
                            mg -= 22;
                            eg -= 35;
                        }
                    }

                }

                mg = -mg;
                eg = -eg;
            }

            return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
        }


        int Negamax(int alpha, int beta, int intDepth, int ply)
        {
            // Vars initializer
            ulong key = board.ZobristKey;
            bool qsearch = intDepth <= 0;
            bool notRoot = ply > 0;
            int best = -99_999_999;
            int movesScored = 0;


            // Check for repetition
            if (notRoot && board.IsRepeatedPosition())
                return 0;


            // Check Extensions
            if (board.IsInCheck()) intDepth++;


            // TT Checks
            ref Transposition transposition = ref m_TPTable[board.ZobristKey & 0x7FFFFF];

            if (notRoot && transposition.zobristHash == board.ZobristKey && transposition.depth >= intDepth && (
                transposition.flag == 1 // exact score
                    || transposition.flag == 2 && transposition.evaluation >= beta // lower bound, fail high
                    || transposition.flag == 3 && transposition.evaluation <= alpha // upper bound, fail low
            ))
            {
                return transposition.evaluation;
            }

            // Static Evaluation
            int eval = Evaluate();


            // Qsearch
            if (qsearch)
            {
                best = eval;
                if (best >= beta)
                    return best;
                alpha = Math.Max(alpha, best);
            }

            else if (!board.IsInCheck())
            {
                // Reverse Futility Pruning
                if (intDepth <= 6 && eval - 80 * intDepth >= beta) return eval;


                // Null Move Pruning
                if (do_null && intDepth >= 2)
                {
                    do_null = false;
                    int R = intDepth > 6 ? 4 : 3;
                    board.TrySkipTurn();
                    int score = -Negamax(-beta, 1 - beta, intDepth - R - 1, ply + 1);
                    board.UndoSkipTurn();
                    if (score >= beta)
                    {
                        return score;
                    }
                }

            }
            do_null = true;


            // Move Gen
            Span<Move> moveSpan = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moveSpan, qsearch && !board.IsInCheck());

            // Move Scorer
            foreach (Move move in moveSpan)
                moveScores[movesScored++] = -(
                // Hash move
                move == transposition.move ? 9_000_000 :

                // MVVLVA
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :

                // Killers
                killers[ply] == move ? 900_000 :

                // History
                historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            // Move Sorter
            moveScores.AsSpan(0, moveSpan.Length)
                .Sort(moveSpan);


            Move bestMove = Move.NullMove;
            int startingAlpha = alpha;


            // Negamax / Search moves / Main Loop
            for (int i = 0; i < moveSpan.Length; i++)
            {
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 98888;

                Move move = moveSpan[i];
                board.MakeMove(move);
                int score = -Negamax(-beta, -alpha, intDepth - 1, ply + 1);
                board.UndoMove(move);

                // New best move
                if (score > best)
                {
                    best = score;
                    bestMove = move;
                    if (ply == 0) bestmoveRoot = move;

                    alpha = Math.Max(alpha, score);

                    // AB Pruning
                    if (alpha >= beta)
                    {
                        // Add Killer moves and History Heuristics
                        if (!move.IsCapture)
                        {
                            historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += intDepth * intDepth;
                            killers[ply] = move;
                        }
                        break;
                    }


                }
            }


            // Checkmate Detection and Draw Detection
            if (!qsearch && moveSpan.Length == 0) return board.IsInCheck() ? -99999 + ply : 0;


            // Transposition Table Inputs
            transposition.evaluation = best;
            transposition.zobristHash = key;
            transposition.move = bestMove;
            transposition.depth = (sbyte)intDepth;
            transposition.flag = (byte)(best < startingAlpha ? 3 : best >= beta ? 2 : 1);

            return best;
        }

    }
}