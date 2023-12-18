namespace auto_Bot_434;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_434 : IChessBot
{
    Dictionary<ulong, BoardState> max_transpositions = new Dictionary<ulong, BoardState>();
    Dictionary<ulong, BoardState> min_transpositions = new Dictionary<ulong, BoardState>();
    private int is_white = 1;
    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    struct BoardState
    {
        public Move m;
        public int depth;
        public double score, alpha, beta;
        public BoardState(Move move, int d, double s, double a, double b)
        {
            m = move;
            depth = d;
            score = s;
            alpha = a;
            beta = b;
        }
    }

    //credit for all the evaluation scores go to this absolute genius: https://github.com/JacquesRW/Chess-Challenge/tree/main
    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    public double evaluate(Board board)
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

        return (mg * phase + eg * (24 - phase)) / 24 * is_white;
    }

    public (double score, Move m) minimax(int depth, int initial_depth, Board b, bool max_or_min, double alpha, double beta)
    {
        if (b.IsDraw())
        {
            return (-1000000000, Move.NullMove);
        }

        double bestScore = -99999999;
        if (!max_or_min)
        {
            bestScore = bestScore * -1;
        }
        Move[] m1 = b.GetLegalMoves(true);
        Move[] m2 = b.GetLegalMoves();
        Move[] moves;

        if (depth <= 0)
        {
            moves = m1;
            bestScore = evaluate(b);
            if ((bestScore >= beta && max_or_min) || (bestScore <= alpha && !max_or_min))
            {
                return (bestScore, Move.NullMove);
            }
            alpha = Math.Max(alpha, bestScore);
        }
        else
        {
            if (b.TrySkipTurn())
            {
                (double null_score, Move m) = minimax(0, initial_depth, b, !max_or_min, alpha, beta);
                b.UndoSkipTurn();
                if ((null_score >= beta && max_or_min) || (null_score <= alpha && !max_or_min))
                {
                    return (null_score, m);
                }
            }
            moves = new Move[m1.Length + m2.Length];
            m1.CopyTo(moves, 0);
            m2.CopyTo(moves, m1.Length);
        }
        if (max_or_min)
        {
            if (max_transpositions.ContainsKey(b.ZobristKey))
            {
                //DivertedConsole.Write("New board: " + ((Board)transpositions[b.ZobristKey]).ZobristKey);
                //DivertedConsole.Write("Old board: " + b.ZobristKey);
                //DivertedConsole.Write(((Board)transpositions[b.ZobristKey]).ZobristKey);
                BoardState state = max_transpositions[b.ZobristKey];
                //DivertedConsole.Write("Board hash " + b.ZobristKey + " transposition found");
                if (initial_depth - depth > state.depth)
                {
                    //DivertedConsole.Write("Transposition activated");
                    return (state.score, state.m);
                }
                //if(transpositions[b.ZobristKey] != null)
                //return ((double)transpositions[b.ZobristKey], Move.NullMove);
                //return minimax(depth, b, false, alpha, beta);
            }

            Move bestMove = new Move();

            foreach (Move move in moves.Distinct())
            {
                b.MakeMove(move);
                if (b.IsInCheckmate())
                {
                    b.UndoMove(move);
                    return (double.PositiveInfinity, move);
                }
                int extend = b.IsInCheck() ? 1 : 0;
                double score = minimax(depth - 1 + extend, initial_depth, b, false, alpha, beta).Item1;
                //DivertedConsole.Write("Depth: " + depth + " Number of moves left: " + b.GetLegalMoves().Length + " This move's score: " + score);
                if (score > bestScore)
                {
                    bestMove = move;
                    bestScore = score;
                }
                b.UndoMove(move);
                alpha = Math.Max(alpha, bestScore);
                if (beta <= alpha)
                {
                    max_transpositions[b.ZobristKey] = new BoardState(bestMove, initial_depth - depth, bestScore, alpha, beta);
                    //transpositions[b.ZobristKey] = b;
                    return (bestScore, bestMove);
                }
            }

            max_transpositions[b.ZobristKey] = new BoardState(bestMove, initial_depth - depth, bestScore, alpha, beta);

            return (bestScore, bestMove);
        }
        else
        {
            if (min_transpositions.ContainsKey(b.ZobristKey))
            {

                //DivertedConsole.Write("Board hash " + b.ZobristKey + " transposition found");
                BoardState state = min_transpositions[b.ZobristKey];
                if (initial_depth - depth > state.depth)
                {
                    //DivertedConsole.Write("Transposition activated");
                    return (state.score, state.m);
                }
            }
            Move bestMove = new Move();
            foreach (Move move in moves.Distinct().ToArray())
            {
                b.MakeMove(move);
                if (b.IsInCheckmate())
                {
                    b.UndoMove(move);
                    return (double.NegativeInfinity, move);
                }
                int extend = b.IsInCheck() ? 1 : 0;
                double score = minimax(depth - 1 + extend, initial_depth, b, true, alpha, beta).Item1;
                //DivertedConsole.Write("Depth: " + depth + " Number of moves left: " + b.GetLegalMoves().Length + " This move's score: " + score);
                if (score < bestScore)
                {
                    bestMove = move;
                    bestScore = score;
                }
                b.UndoMove(move);
                beta = Math.Min(beta, bestScore);
                if (beta <= alpha)
                {
                    min_transpositions[b.ZobristKey] = new BoardState(bestMove, initial_depth - depth, bestScore, alpha, beta);
                    return (bestScore, bestMove);
                }
            }

            min_transpositions[b.ZobristKey] = new BoardState(bestMove, initial_depth - depth, bestScore, alpha, beta);

            return (bestScore, bestMove);
        }
    }
    public Move Think(Board board, Timer timer)
    {
        if (!board.IsWhiteToMove)
        {
            is_white = -1;
        }
        int ms_total = timer.MillisecondsRemaining;
        double score = 0;
        Move m = board.GetLegalMoves()[0];
        int iter = 1;
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;
        while (timer.MillisecondsElapsedThisTurn < (int)(.02 * ms_total))
        {
            //DivertedConsole.Write("Depth: " + iter);
            (score, m) = minimax(iter, iter, board, true, alpha, beta);
            //DivertedConsole.Write("Score is " + score + " at iteration " + iter);
            iter++;
        }
        //DivertedConsole.Write("Depth: " + iter);
        if (m.IsNull)
        {
            m = board.GetLegalMoves()[0];
        }
        return m;
    }
}
