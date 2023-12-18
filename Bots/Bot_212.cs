namespace auto_Bot_212;
//#define NCOUNT
//#define ENAL


using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_212 : IChessBot
{
    // Transposition Table
    record struct Transposition(ulong key, Move move, int score, double mvs);

    static ulong TTMask = 0x3FFFFF;
    Transposition[] TT = new Transposition[TTMask + 1];

    // History
    public int[,,] history = new int[2, 7, 65];
    public Move[] killers = new Move[1000];

    // Time this turn
    public int TimeLeft = 0;

    // Piece Values
    public int[] PieceValsMg = { 0, 82, 337, 365, 477, 1025, 0 };
    public int[] PieceValsEg = { 0, 94, 281, 297, 512, 936, 0 };

    // Piece Value Tables
    public sbyte[][] Tables;
    // Phase based on pieces
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };

    // Current Best Move
    Move root_best_move;
    int root_best_eval;

    // Stuff
    Board board;
    Timer timer;

#if NCOUNT
    int nodes = 0;
#endif

    public Move Think(Board _board, Timer _timer)
    {
        // Set stuff
        board = _board;
        timer = _timer;

#if NCOUNT
        nodes = 0;
#endif

        TimeLeft = timer.MillisecondsRemaining / 30;
        root_best_move = board.GetLegalMoves()[0];
        root_best_eval = -999999;

        Tables = new ulong[] { 15625477333024561368, 14842270201136109370, 14191141604541259730, 13972951038032799178, 13826858902128678589, 14770078190052627646, 14050932636772521909, 15625477333024561368, 15625477333024561368, 9186600431121760127, 3182370449517263926, 16855245895814738168, 15554255337641337317, 15048729608202674140, 15121637180715360485, 15625477333024561368, 9280089337670041728, 14402255353337130895, 297617096332612777, 17215605498274638287, 15054948566431227083, 14479611476306087873, 14252462183493182395, 13962773785697239936, 9266646313725571742, 11871699517130657983, 12665758053164041408, 14330704629643271111, 14329585301086327494, 14034577509322315201, 12448447271984350382, 10999697679809881531, 15050940618069433531, 12243620487661807806, 15491550366377704904, 15483373307174837716, 15916535799548863954, 16351149570180769752, 15706846721103947740, 14101277083809011127, 13891300075705844682, 14615530809849795792, 15913714297639325914, 15770448001428152789, 14976125587253746642, 14542646452156421580, 13675690290907825866, 14399073228570087361, 285944590462616312, 356377179498674419, 16723279293839502291, 14182112238875168192, 13969833862068223668, 13246169367488020395, 10507711396652042412, 13741573079486286789, 15988030348959867621, 15843905200787416547, 15407893894756556767, 15769312154242898908, 14830585380316700123, 14470290804382619860, 15406198391941157586, 14185437037030267599, 361136878611978428, 1077505765194052032, 1226968071707543499, 15696990664242281917, 15410144569036095183, 15989708130402294474, 15696707029200785589, 12013844354802108119, 17069464970689703631, 15633948216108051655, 16279381285709930180, 18163298845276040923, 17293817033022764230, 15988598783435324872, 13309475866891960770, 12665451271365508279, 16562751381306929047, 13525106624284645365, 14046409224579903695, 13027435680891716807, 11941202786122586023, 13675666994249255626, 16204453582174478297, 16641008296125594825, 14401640579413226894, 16424626646648547788, 16502321449654020578, 15849004860540251856, 14835402478533334214, 14978947002843518405, 14399095317890649533, 12520229876344796835 }
            .SelectMany(BitConverter.GetBytes)
            .Select(i => (sbyte)i)
            .Chunk(64)
            .ToArray();

        // Get the best move according to the evaluation function
        for (int i = 0; ;)
        {
#if NCOUNT
            nodes = 0;
#endif
            // Get the move
            deep_evaluate(++i, -999999, 999999, true, true);

#if NCOUNT
            DivertedConsole.Write("Depth: ");
            DivertedConsole.Write(i);
            DivertedConsole.Write(" | Nodes: ");
            DivertedConsole.Write(nodes);
#endif

            if (timer.MillisecondsElapsedThisTurn > TimeLeft || root_best_eval > 50000)
                break;
        }
        /*
#if NCOUNT
        DivertedConsole.Write("New: ");
        DivertedConsole.Write(nodes / (TimeLeft > 0 ? TimeLeft : 1) * 1000);
        DivertedConsole.Write("Eval: ");
        DivertedConsole.Write(root_best_eval);
#endif
        */


        return root_best_move;
    }

    public int evaluate()
    {
#if NCOUNT
        nodes++;
#endif
        // Initialize evaluations
        int mg_eval = 0,
            eg_eval = 0,
            phase = 0;
        bool color = board.IsWhiteToMove;

        bool[] vs = { true, false };
        // Go through each square and evaluate (PeSTO)
        foreach (bool col in vs)
        {
            int cc = color == col ? 1 : -1;

            for (int PT = 1; PT <= 6; PT++)
            {
                ulong bb = board.GetPieceBitboard((PieceType)PT, col);

                while (bb != 0)
                {
                    int i = BitboardHelper.ClearAndGetIndexOfLSB(ref bb);
                    phase += piecePhase[PT];

                    int ix = i ^ (col ? 56 : 0);

                    mg_eval += cc * (40 + PieceValsMg[PT] + Tables[2 * PT - 2][ix]);
                    eg_eval += cc * (40 + PieceValsEg[PT] + Tables[2 * PT - 1][ix]);
                }
            }
        }


        if (phase > 24) phase = 24;
        return (mg_eval * phase + eg_eval * (24 - phase)) / 24;
    }

    // Negamax
    public int deep_evaluate(int depth, int alpha, int beta, bool donull, bool isRoot = false)
    {
        bool qsearch = depth <= 0,
             reduce = beta - alpha <= 1 && !board.IsInCheck();
        int value = -999999,
            color = board.IsWhiteToMove ? 1 : 0,
            cdepth = Math.Clamp(depth, 0, 999);
        ulong masked = board.ZobristKey & TTMask;

        if (board.IsRepeatedPosition())
            return 0;

        Transposition TTentry = TT[masked];
        if (!isRoot && TTentry.mvs >= depth && TTentry.key == board.ZobristKey)
            return TTentry.score;

        int stat = evaluate();

        // Quiescence
        if (qsearch)
        {
            value = stat;
            if (value >= beta) return value;
            alpha = Math.Max(alpha, value);
        }
        else if (!isRoot && reduce)
        {
            // rfp
            if (stat - 85 * depth >= beta) return stat;

            // nmp
            if (donull && depth > 2)
            {
                board.TrySkipTurn();
                int score = -deep_evaluate(depth - 3 - depth / 3 - Math.Min((stat - beta) / 200, 3), -beta, 1 - beta, false);
                board.UndoSkipTurn();
                if (score >= beta) return score;
            }
        }
        // Get Moves, if in check allow any response in qsearch
        Move[] moves = board.GetLegalMoves(qsearch && !board.IsInCheck());

        // Move Ordering
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            scores[i] =
                move == TTentry.move ? -1000000000 :
                move.IsCapture ? scores[i] = -10000000 * (int)move.CapturePieceType + (int)move.MovePieceType :
                move == killers[cdepth] ? -1000000 :
                history[color, (int)move.MovePieceType, move.TargetSquare.Index];
        }
        Array.Sort(scores, moves);


        Move best_move = Move.NullMove;
        int begAlpha = alpha;

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (timer.MillisecondsElapsedThisTurn > TimeLeft) return 999999;

            board.MakeMove(move);

            int eval = 0;
            int search(int nalpha) => -deep_evaluate(depth - (board.IsInCheck() ? 0 : 1) - (i > 5 && reduce ? depth / 3 : 0), -nalpha, -alpha, donull);


            if (qsearch || i == 0 || (eval = search(alpha + 1)) > alpha)
                eval = search(beta);

            board.UndoMove(move);

            if (eval > value)
            {
                value = eval;
                best_move = move;

                if (isRoot) (root_best_move, root_best_eval) = (move, value);

                if (value > alpha)
                {
                    alpha = value;

                    // Only store moves which raise alpha
                    TT[masked] = new Transposition(masked, best_move, value, depth);

                    // History
                    if (!move.IsCapture)
                        history[color, (int)move.MovePieceType, move.TargetSquare.Index] -= depth * depth;
                }

                // Alpha-Beta-Pruning
                if (alpha >= beta)
                {
                    killers[cdepth] = move;
                    break;
                }
            }
        }
        if (moves.Length == 0 && !qsearch)
            return board.IsInCheck() ? board.PlyCount - 999999 : 0;
        return value;
    }
}