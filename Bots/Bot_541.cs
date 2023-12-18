namespace auto_Bot_541;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
//using System.Reflection.Metadata;
using static System.Numerics.BitOperations;

public class Bot_541 : IChessBot
{
    // score for a drawn game.
    // naturally should be zero. increasing it means we actively try to avoid draws.
    public const int CONTEMPT = 0;

    // are we playing with the white pieces?
    bool playingWhite;



    // transposition table.
    // instead of the typical score/depth/flag, we store lower and upper bound separately, potentially with different heights.
    struct TableEntry
    {
        public ulong key;
        public Move pv;
        public int depthLow, depthHigh;
        public int scoreLow, scoreHigh;
    }

    const ulong ttable_size = ((1 << 24) / 2);
    TableEntry[] ttable = new TableEntry[ttable_size];


    // bitboards
    const ulong bbAll = 0xffffffffffffffff; // all squares
    const ulong bbCenter4 = 66229406269440; // central 4x4 squares
    const ulong bbCenter6 = 35604928818740736; // central 6x6 squares

    // Terms in the evaluation function. tuples of [piece type, bit-borard, score].
    // the scores are determined by doing a linear regression on a large number of self-play games. A won/lost game is represented by a score of +/- 10000.
    (int, ulong, short)[] terms = {
        //0.1564  0.3002  0.2666  0.5257  0.7009  0.0111  0.0152  0.0104 -0.0057  0.0153  0.0163  0.0289  0.0515  0.0162  0.049 
        (1, bbAll, 1564),
        (2, bbAll, 3002),
        (3, bbAll, 2666),
        (4, bbAll, 5257),
        (5, bbAll, 7009),
        (1, bbCenter4, 111),
        (2, bbCenter4, 152),
        (3, bbCenter4, 104),
        (4, bbCenter4, -57),
        (5, bbCenter4, 153),
        (1, bbCenter6, 163),
        (2, bbCenter6, 289),
        (3, bbCenter6, 515),
        (4, bbCenter6, 162),
        (5, bbCenter6, 490),
     };

    // evaluate material for one side
    public int evaluate_material(Board board, bool white)
    {
        int score = 0;
        foreach (var (piece_type, bb, value) in terms)
            score += value * PopCount(board.GetPieceBitboard((PieceType)piece_type, white) & bb);

        return score;
    }

    // evaluate a position without doing any further moves
    // NOTE: this does not check for checkmate or draw!
    public int evaluate_static(Board board)
    {
        var score = evaluate_material(board, true) - evaluate_material(board, false);
        if (!board.IsWhiteToMove)
            score = -score;
        return score;
    }

    // generate moves and sort them approximately
    public void generate_moves(Board board, ref Span<Move> moves, bool captureOnly = false)
    {
        board.GetLegalMovesNonAlloc(ref moves, captureOnly);
        if (moves.Length <= 1)
            return;

        var piece_values = new int[] { 0, 100, 300, 300, 500, 900, 0 };
        // sort the moves
        int eval_move(Move move)
        {
            int score = 0;
            if (move.IsCapture)
                score += 10 * piece_values[(int)move.CapturePieceType] - piece_values[(int)move.MovePieceType];
            if (move.IsPromotion)
                score += 900;
            return -score;
        }
        MemoryExtensions.Sort(moves.Slice(0, moves.Length), (a, b) =>
             eval_move(a).CompareTo(eval_move(b)));
    }

    // evaluate a position to a given depth. return values in the (closed) interval [alpha, beta] are exact, outside might be a lower/upper bound.
    public int search(Board board, int depth, int alpha, int beta)
    {
        // check if the position in final. This needs to be done independent of depth
        if (board.IsInCheckmate())
            return -31000;
        if (board.IsDraw())
            return playingWhite == board.IsWhiteToMove ? -CONTEMPT : CONTEMPT;

        // check the transposition table
        ulong key = board.ZobristKey;
        var entry = ttable[key % ttable_size];
        var best_move = Move.NullMove;
        if (entry.key == key)
        {
            if (entry.depthLow >= depth && entry.scoreLow > beta)
                return entry.scoreLow;
            if (entry.depthHigh >= depth && entry.scoreHigh < alpha)
                return entry.scoreHigh;
            if (entry.depthLow >= depth && entry.depthHigh >= depth && entry.scoreHigh == entry.scoreLow)
                return entry.scoreLow;
            best_move = entry.pv; // might be NullMove
        }
        else
        {
            entry.key = key;
            entry.depthLow = 0;
            entry.depthHigh = 0;
            entry.scoreLow = -32000;
            entry.scoreHigh = 32000;
        }

        System.Span<Move> moves = stackalloc Move[256];

        int score = -32000;

        // at depth <= 0:
        //   * only generate "tactical" moves (i.e. captures. In principle, other big moves like checks/promotions would be good too)
        //   * pretend there is a "pass and go to static evaluation" move.
        if (depth <= 0)
        {
            score = evaluate_static(board);
            if (score > beta)
                return score;
        }
        generate_moves(board, ref moves, depth <= 0);


        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int s = -search(board, depth - 1, -beta, -Math.Max(alpha, score + 1));
            board.UndoMove(move);

            // in winning positions, we prefer a sooner win over a later one.
            // Otherwise, we could run in circles, not actually reaching the mate.
            // Also, prefering a later loss over a sooner one might give our opponent more chances to blunder.
            if (s > 30000)
                s -= 1;
            if (s < -30000)
                s += 1;

            if (s > score)
            {
                score = s;
                best_move = move;

                if (score > beta)
                    break;
            }
        }

        if (score >= alpha)
        {
            entry.depthLow = depth;
            entry.scoreLow = score;
        }
        if (score <= beta)
        {
            entry.depthHigh = depth;
            entry.scoreHigh = score;
        }

        entry.pv = best_move;

        ttable[key % ttable_size] = entry;
        return score;
    }


    public int search_root(Board board, Move move, int depth, int alpha)
    {
        board.MakeMove(move);
        int score = -search(board, depth, -32000, -alpha);
        board.UndoMove(move);
        return score;
    }



    public Move Think(Board board, Timer timer)
    {
        int slack = 10;
        var good_moves = new List<Move>();

        playingWhite = board.IsWhiteToMove;

        var moves = board.GetLegalMoves();
        if (moves.Length == 1)
            return moves[0];

        int best_score = -32000;
        int best_move = 0;

        int targetTime = timer.MillisecondsRemaining / 50;



        for (int depth = 0; ; depth++)
        {
            // search best-from-previous-depth first
            best_score = search_root(board, moves[best_move], depth, -32000);
            good_moves.Clear();
            good_moves.Add(moves[best_move]);

            // search alternative moves
            for (int i = 0; i < moves.Length; ++i)
                if (i != best_move)
                {
                    var score = search_root(board, moves[i], depth, best_score - slack);
                    if (score > best_score)
                    {
                        best_score = score;
                        best_move = i;
                        good_moves.Clear();
                        good_moves.Add(moves[i]);
                    }

                    else if (score >= best_score - slack)
                        good_moves.Add(moves[i]);

                    if (timer.MillisecondsElapsedThisTurn > targetTime)
                    {
                        // choose a random move from the good ones
                        Move move_choice = good_moves[new Random().Next(good_moves.Count)];
                        //DivertedConsole.Write("chose move among " + good_moves.Count + " good moves, score = " + best_score + ", depth = " + depth);
                        // DivertedConsole.Write("took move " + moves[best_move] + ", score = " + best_score + ", depth = " + depth);
                        return move_choice;
                    }
                }

            //DivertedConsole.Write("depth " + depth + " score " + best_score + " move " + moves[best_move]);
        }
    }



}