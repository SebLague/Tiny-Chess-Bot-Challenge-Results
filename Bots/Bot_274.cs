namespace auto_Bot_274;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_274 : IChessBot
{
    Dictionary<(ulong, int), (Move, int)> transposition_table = new Dictionary<(ulong, int), (Move, int)>();
    public Move Think(Board board, Timer timer)
    {
        Move[] current_moves = board.GetLegalMoves();
        int[] peice_values = { 0, 100, 300, 320, 500, 900, 99999 };
        int checkmate_value = 100000;
        int positive_infinity = 99999999;
        int negative_infinity = -99999999;
        int timeout_value = 7654321;
        var rng = new Random();

        float minimax(bool max, int depth, int depth_increase, int alpha, int beta, int number_of_extensions, int max_time)
        {
            if (timer.MillisecondsElapsedThisTurn > max_time)
            {
                return timeout_value;
            }

            if (board.IsDraw())
            {
                if (max)
                {
                    return 50;
                }
                return -50;
            }

            if (depth < 1)
            {
                return SearchCaptures(max, alpha, beta);
            }

            return FindBestMove(max, depth, depth_increase, alpha, beta, false, number_of_extensions, max_time).Item2;
        }

        (Move, int) IterativeDeepening(int max_depth, int max_time)
        {
            // Iterate until we've used too much time, at depth of 1, then 2, so on.
            (Move, int) output = (new Move(), 0);

            for (int depth = 1; depth < max_depth; depth++)
            {
                if (timer.MillisecondsElapsedThisTurn > max_time)
                {
                    break;
                }
                else
                {
                    (Move, int) possible_output = FindBestMove(false, depth, 0, -999999999, 999999999, true, 0, max_time);

                    if (possible_output.Item2 == timeout_value)
                    {
                        break;
                    }

                    output = possible_output;
                }
            }

            return output;
        }

        (Move, int) FindBestMove(bool max, int depth, int depth_increase, int alpha, int beta, bool top, int number_of_extensions, int max_time)
        {
            Move[] moves = SortMovesBestWorst(board.GetLegalMoves());

            int highest_score = max ? negative_infinity : positive_infinity;

            try
            {
                return transposition_table[(board.ZobristKey, depth)];
            }
            catch (KeyNotFoundException) { }

            if (moves.Length == 0)
            {
                return board.IsInCheck() ? (new Move(), max ? -checkmate_value + depth_increase : checkmate_value - depth_increase) : (new Move(), 0);
            }

            Move highest_scoring_move = moves[0];

            foreach (var move in moves)
            {
                board.MakeMove(move);

                int extention = number_of_extensions < 12 && board.IsInCheck() ? 1 : 0;
                int move_score = (int)minimax(!max, depth - 1 + extention, depth_increase + 1 - extention, alpha, beta, number_of_extensions + extention, max_time);

                board.UndoMove(move);

                if (move_score == timeout_value)
                {
                    return (moves[0], move_score);
                }

                int previous_highscore = highest_score;

                if (max)
                {
                    highest_score = Math.Max(highest_score, move_score);
                    if (highest_score > beta)
                    {
                        break;
                    }
                    alpha = Math.Max(alpha, highest_score);
                }
                else
                {
                    highest_score = Math.Min(highest_score, move_score);
                    if (highest_score < alpha)
                    {
                        break;
                    }
                    beta = Math.Min(beta, highest_score);
                }

                if (previous_highscore != highest_score)
                {
                    highest_scoring_move = move;
                }
            }

            transposition_table.Add((board.ZobristKey, depth), (highest_scoring_move, highest_score));
            return (highest_scoring_move, highest_score);
        }

        int SearchCaptures(bool max, int alpha, int beta)
        {
            var captures = SortMovesBestWorst(board.GetLegalMoves(true));

            bool white = max ? board.IsWhiteToMove : !board.IsWhiteToMove;

            if (captures.Length == 0)
            {
                return (int)GetSideScore(white);
            }

            int highest_score = (int)GetSideScore(white);

            foreach (Move capture in captures)
            {
                board.MakeMove(capture);
                int score = SearchCaptures(!max, alpha, beta);
                board.UndoMove(capture);

                if (max)
                {
                    highest_score = Math.Max(highest_score, score);
                    if (highest_score > beta)
                    {
                        break;
                    }
                    alpha = Math.Max(alpha, highest_score);
                }
                else
                {
                    highest_score = Math.Min(highest_score, score);
                    if (highest_score < alpha)
                    {
                        break;
                    }
                    beta = Math.Min(beta, highest_score);
                }
            }

            return highest_score;
        }

        Move[] SortMovesBestWorst(Move[] moves)
        {
            List<Move> ordered_moves = new List<Move>();
            List<Tuple<int, Move>> unordered_scored_moves = new List<Tuple<int, Move>>();

            foreach (var move in moves)
            {
                int score_estimation = 0;
                int peice_moved_value = peice_values[(int)board.GetPiece(move.StartSquare).PieceType];
                int peice_captured_value = peice_values[(int)board.GetPiece(move.TargetSquare).PieceType];

                score_estimation += peice_captured_value - peice_moved_value;

                board.MakeMove(move);

                if (board.IsInCheck())
                {
                    score_estimation += 100;
                }
                if (board.IsInCheckmate())
                {
                    score_estimation += 99999;
                }

                board.UndoMove(move);

                unordered_scored_moves.Add(Tuple.Create(score_estimation, move));
            }

            unordered_scored_moves.Sort((x, y) => y.Item1.CompareTo(x.Item1));

            foreach (var scored_move in unordered_scored_moves)
            {
                ordered_moves.Add(scored_move.Item2);
            }

            return ordered_moves.ToArray();
        }

        int NumPeices()
        {
            int value = 0;

            foreach (var peice_list in board.GetAllPieceLists())
            {
                value += peice_list.Count;
            }

            return value;
        }

        float CalculateEndgameWeight()
        {
            return (1f - NumPeices() / 32f) * 2;
        }

        float EndgameEval()
        {
            float evaluation = 0;

            var opponent_king_square = board.GetKingSquare(!board.IsWhiteToMove);
            var opponent_file = opponent_king_square.File;
            var opponent_rank = opponent_king_square.Rank;

            // Add the distance from the center to the evaluation
            evaluation += Math.Max(3 - opponent_file, opponent_file - 4) + Math.Max(3 - opponent_rank, opponent_rank - 4);

            var current_king_square = board.GetKingSquare(board.IsWhiteToMove);
            var current_king_file = current_king_square.File;
            var current_king_rank = current_king_square.Rank;

            // Add 14 (the max distance apart) minus the distance between kings
            evaluation += 14 - (Math.Abs(current_king_file - opponent_file) + Math.Abs(current_king_rank - opponent_rank));

            float end_eval = evaluation * 10 * CalculateEndgameWeight();

            return end_eval;
        }

        float GetSideScore(bool white)
        {
            float total_score = 0;

            foreach (PieceList piece_list in board.GetAllPieceLists())
            {
                int piece_value = peice_values[(int)piece_list.TypeOfPieceInList] * piece_list.Count;
                if (piece_list.IsWhitePieceList == white)
                {
                    total_score += piece_value;
                }
                if (piece_list.IsWhitePieceList != white)
                {
                    total_score -= piece_value;
                }
            }
            total_score -= EndgameEval();

            return total_score;
        }

        return IterativeDeepening(30, timer.MillisecondsRemaining > 60000 ? 5000 : 500).Item1;
    }
}