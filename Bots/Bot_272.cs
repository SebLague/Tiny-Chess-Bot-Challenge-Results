namespace auto_Bot_272;
using ChessChallenge.API;
using System.Collections.Generic;

// V1 : simple approach, try to look as deep as possible while managing time
// and use material difference as heuristic
public class Bot_272 : IChessBot
{
    // constants

    private static float OO = float.PositiveInfinity;
    private static int N_MOVE_BENCHMARK_ = 60; // to go start -> endgame and then endgame -> finish
    private static double TIME_FRACTION_FOR_ENDGAME = 0.25f;
    private static double TARGET_TIMER_LEFT_ = 0.01f;
    private double TIME_FRACTION_DURING_ENDGAME_ = 1 - System.Math.Pow(TARGET_TIMER_LEFT_, 1.0f / N_MOVE_BENCHMARK_); // TODO change this to a constant

    // attributes

    private int move_played_ = 0;
    private int duration_for_this_turn_ = 0; // ms
    private System.Random rng_ = new();
    private Dictionary<ulong, EvaluatedMove_> previous_position_eval_ = new();

    // methods

    public Move Think(Board board, Timer timer)
    {
        SearchInfo_ search_info = new(board, timer, is_root: true);

        if (move_played_ == 0)
        {
            InitializeGame_(search_info);
        }

        InitializeTurn_(search_info);

        // first pass at depth 1
        // for evey legal move we have, we evaluate the resulting position
        // that lets us find an OK move quickly and store some good moves to favour pruning in deeper searches
        EvaluatedMove_ best_move = SearchForBestMove_(search_info, out bool _); // TODO store the best N moves

        // following passes with iterative deepening
        // use even depths to consider the opponent's response
        EvaluatedMove_ new_best_move;
        for (int max_depth = 2; max_depth < 15; max_depth += 2)
        {
            // TODO check that we re-use knowledge from the previous searches
            search_info = new(board, timer, max_depth: max_depth);

            // assume the old best move is always forgotten because we have better accuracy now in the estimate
            // but need to store the old one since this call could be interrupted by the timer
            new_best_move = SearchForBestMove_(search_info, out bool hasTimedOut);

            if (hasTimedOut)
                break;

            // TODO here we could check how long we just took
            // and, assuming the next pass will take longer, break out if the remaining time is smaller
            // that can save time for the endgame

            best_move = new_best_move;
        }

        move_played_++;

        DivertedConsole.Write("Move {0} : found move {1} at depth {2}", move_played_, best_move.move, best_move.depth);

        return GetValidMove(best_move.move, board);
    }

    private Move GetValidMove(Move search_result, Board board)
    {
        if (search_result != Move.NullMove)
            return search_result;

        // if no good move was found, return a randomly selected one
        Move[] all_moves = board.GetLegalMoves();
        return all_moves[rng_.Next(all_moves.Length)];
    }

    private void InitializeTurn_(SearchInfo_ search_info)
    {
        // a : turn count below limit
        //      the end time is current milliseconds - time_per_turn
        // this was computed and set during initialization, so nothing to do here
        if (move_played_ < N_MOVE_BENCHMARK_)
        {
            return;
        }

        // b : turn count at or above limit
        //      the end time is current milliseconds * (1 - some_fraction)
        duration_for_this_turn_ = (int)System.Math.Ceiling(search_info.timer.MillisecondsRemaining * TIME_FRACTION_DURING_ENDGAME_);
    }

    private void InitializeGame_(SearchInfo_ search_info)
    {
        int thinking_time_total_ = search_info.timer.GameStartTimeMilliseconds;

        int thinking_time_for_endgame = (int)(thinking_time_total_ * TIME_FRACTION_FOR_ENDGAME);

        // constant value used for the N first turns "before endgame"
        duration_for_this_turn_ = (thinking_time_total_ - thinking_time_for_endgame) / N_MOVE_BENCHMARK_;
    }

    private EvaluatedMove_ SearchForBestMove_(SearchInfo_ search_info, out bool hasTimedOut)
    {
        hasTimedOut = false;

        // beginning of a new search : clear the cache
        if (search_info.current_depth == 0)
        {
            previous_position_eval_.Clear();
        }

        if (previous_position_eval_.TryGetValue(search_info.board.ZobristKey, out EvaluatedMove_ cached_result))
        {
            return cached_result;
        }

        EvaluatedMove_ result = new()
        {
            move = Move.NullMove,
            score = -OO, // worst possible score, since we will try to maximize a corrected score
            depth = search_info.current_depth,
        };

        // TODO include the evaluated sequence of moves here, so we can understand
        // why it's seeing what it sees

        // terminal positions : do not recurse
        if (search_info.board.IsDraw())
        {
            result.score = 0; // sign doesn't matter
            return result; // don't cache these, they're easy to evaluate
        }
        if (search_info.board.IsInCheckmate())
        {
            result.score = -OO; // being mated is very bad
            return result; // don't cache these, they're easy to evaluate
        }

        // we have looked far enough ahead,
        // actually evaluate the resulting position
        if (search_info.current_depth >= search_info.max_depth)
        {
            // direct evaluation returns an "objective" score
            // we convert to "favorable for current player" score before returning
            result.score = search_info.score_correcter * EvaluateBoardForWhite_(search_info.board);
            previous_position_eval_[search_info.board.ZobristKey] = result;
            return result;
        }

        // recursive tree search : loop over all legal moves to find better options
        foreach (Move move in search_info.board.GetLegalMoves())
        {
            // explore one possible move from current position
            search_info.board.MakeMove(move);
            EvaluatedMove_ best_opposing_move = SearchForBestMove_(search_info.Recurse(), out hasTimedOut);
            search_info.board.UndoMove(move);

            // check we don't spend too much time
            if (hasTimedOut || search_info.timer.MillisecondsElapsedThisTurn > duration_for_this_turn_)
            {
                hasTimedOut = true;
                return result; // content doesn't matter
            }

            // the evaluation returned is for the other player ; need to reverse it
            EvaluatedMove_ new_option = best_opposing_move.Invert();

            // this option is better than the current best if
            // - it has a higher score
            // - it has the same positive score, but with lower depth (be quick if winning)
            // - it has the same zero or negative score, but with higher depth (delay if not winning)
            float scoreDelta = new_option.score - result.score;
            int depthDelta = new_option.depth - result.depth;
            if (scoreDelta > 0 || (scoreDelta == 0 && (new_option.score > 0 && depthDelta < 0 || new_option.score <= 0 && depthDelta > 0)))
            {
                result.move = move;
                result.score = new_option.score;
                result.depth = new_option.depth;
            }

            bool isMaximizer = search_info.score_correcter > 0;
            search_info.alpha = isMaximizer ? System.Math.Max(search_info.alpha, result.score) : search_info.alpha;
            search_info.beta = isMaximizer ? search_info.beta : System.Math.Min(search_info.beta, -result.score);

            if (isMaximizer && result.score >= search_info.beta || !isMaximizer && -result.score <= search_info.alpha)
            {
                break;
            }
        }

        previous_position_eval_[search_info.board.ZobristKey] = result;
        return result;
    }

    private float EvaluateBoardForWhite_(Board board)
    {
        int material_value = 0;
        foreach (PieceList piece_list in board.GetAllPieceLists())
        {
            if (piece_list.Count <= 0)
                continue;

            Piece example = piece_list.GetPiece(0);
            int piece_value = 3; // knight/bishop by default ; also used for the kings since this value doesn't matter
            if (example.IsPawn)
                piece_value = 1;
            if (example.IsRook)
                piece_value = 5;
            if (example.IsQueen)
                piece_value = 9;

            material_value += (example.IsWhite ? 1 : -1) * piece_value * piece_list.Count;
        }

        if (board.IsInCheck())
            material_value += (board.IsWhiteToMove ? -1 : 1) * 2; // being in check is seen as badly as losing 2 pawns

        return material_value;
    }

    // utility nested types
    private struct EvaluatedMove_
    {
        public float score; // = 0
        public Move move; // = NullMove
        public int depth; // = 0

        public EvaluatedMove_ Invert()
        {
            return new()
            {
                score = -score,
                move = move,
                depth = depth
            };
        }
    }

    private struct SearchInfo_
    {
        public Board board;
        public Timer timer;
        public int score_correcter = 0;
        public int max_depth = 0;
        public int current_depth = 0;

        public float alpha = 0; // for a b pruning
        public float beta = 0;

        public SearchInfo_(Board board, Timer timer, int max_depth = 1, bool is_root = false)
        {
            this.board = board;
            this.timer = timer;
            this.max_depth = max_depth;

            score_correcter = board.IsWhiteToMove ? 1 : -1;

            current_depth = 0; // assume the constructor always generates root info

            alpha = -OO;
            beta = +OO;
        }

        public SearchInfo_ Recurse()
        {
            SearchInfo_ new_info = new()
            {
                board = this.board, // share the board and timer objects
                timer = this.timer,

                score_correcter = -this.score_correcter, // reverse the correction factor

                current_depth = this.current_depth + 1, // advance towards max_depth
                max_depth = this.max_depth,

                alpha = this.alpha,
                beta = this.beta,
            };

            return new_info;
        }
    }
}
