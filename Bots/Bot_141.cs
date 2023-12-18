namespace auto_Bot_141;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_141 : IChessBot
{
    int time_per_move = -1;

    public Move Think(Board board, Timer timer)
    {
        // Current timer usage assumes 50 moves
        if (time_per_move == -1)
        {
            time_per_move = Convert.ToInt32((timer.GameStartTimeMilliseconds + timer.IncrementMilliseconds) / 50 * 0.95);
        }

        // Default case
        Move returned_move;

        // The best series of moves from last iteration
        List<Move> moves = new List<Move>();

        // Iterative deepening
        int cur_depth = 1;
        while (true)
        {
            (moves, int score) = MiniMax(board, timer, cur_depth++, board.IsWhiteToMove, moves);
            returned_move = moves.Last();

            // If the time was up 12345 was returned, else store the answer
            if (score == 12345) break;
        }

        return returned_move;
    }

    private (List<Move>, int) MiniMax(Board board, Timer timer, int depth_left, bool maximizing, List<Move> best_moves_last_time, int alpha = int.MinValue, int beta = int.MaxValue)
    {

        // If the game was won by the opponent
        if (board.IsInCheckmate()) return (new List<Move>(), maximizing ? -99999 : 99999);

        // If the game was a draw
        if (board.IsDraw()) return (new List<Move>(), 0);

        // If we reached the max depth go to capture only mode up to 10 deep
        if (depth_left == 0) return (new List<Move>(), EvaluateBoard(board));

        // Default values
        int best_score = maximizing ? int.MinValue : int.MaxValue;
        List<Move> best_moves = new List<Move>(best_moves_last_time);

        // Get the bitboard for enemy pawn attacks for the move ordering
        ulong enemy_pawn_attacks = board.GetPieceList(PieceType.Pawn, !maximizing).Count != 0 ? PawnAttackBitboard(board.GetPieceList(PieceType.Pawn, !maximizing), !maximizing) : 0;

        // Gets and orders all the legal moves
        IEnumerable<Move> ordered_moves = board.GetLegalMoves()
        .OrderByDescending(move => MoveOrderScore(move, enemy_pawn_attacks))
        .Select(move => move);

        // The best move last time to be put up front
        int best_moves_count = best_moves_last_time.Count - 1;
        if (best_moves_count > -1)
        {
            Move move_to_front = best_moves_last_time[best_moves_count]; // This required a separate statement, I do not know why
            ordered_moves = ordered_moves.OrderBy(move => move == move_to_front ? 0 : 1);
            best_moves_last_time.RemoveAt(best_moves_count);
        }

        // For each of the legal moves
        foreach (Move move in ordered_moves)
        {
            board.MakeMove(move);

            // Go a level deeper
            (List<Move> returned_moves, int returned_score) = MiniMax(board, timer, depth_left - 1, !maximizing, best_moves_last_time, alpha, beta);

            // If our thinking time is up
            if (timer.MillisecondsElapsedThisTurn >= time_per_move) return (best_moves, 12345);

            board.UndoMove(move);

            // If the returned score is better than the current best score
            if (maximizing ? returned_score > best_score : returned_score < best_score)
            {

                // Best move/score bookkeeping
                best_score = returned_score;
                best_moves = new List<Move>(returned_moves) { move }; // Create new list to avoid fuckery

                // Alpha-beta pruning
                if (maximizing) alpha = Math.Max(alpha, best_score);
                else beta = Math.Min(beta, best_score);

                // Break loop if needed
                if (alpha >= beta) break;
            }
        }

        return (best_moves, best_score);
    }

    int EvaluateBoard(Board board)
    {
        // All pieces on the board for both sides
        PieceList[] piece_list_list = board.GetAllPieceLists();

        // Evaluation of each piece from both sides
        int[] evaluation_scores = { 100, 300, 320, 500, 800, 99999, -100, -300, -320, -500, -800, -99999 };

        // For each piece add the score of that piece to the total
        int score = evaluation_scores.Zip(piece_list_list, (score, pieceList) => score * pieceList.Count).Sum();

        // Pawn positions
        score += PieceEvaluation(piece_list_list[0], PawnEvaluation, piece_list_list[0].Count > 0 ? PawnAttackBitboard(piece_list_list[0], true) : 0UL, board.GetPieceBitboard(PieceType.Pawn, true));
        score -= PieceEvaluation(piece_list_list[6], PawnEvaluation, piece_list_list[6].Count > 0 ? PawnAttackBitboard(piece_list_list[6], false) : 0UL, board.GetPieceBitboard(PieceType.Pawn, false));

        // Knight positions
        score += PieceEvaluation(piece_list_list[1], KnightEvaluation, 0UL, 0UL);
        score -= PieceEvaluation(piece_list_list[7], KnightEvaluation, 0UL, 0UL);

        // Bishop positions
        score += PieceEvaluation(piece_list_list[2], BishopEvaluation, 0UL, 0UL);
        score -= PieceEvaluation(piece_list_list[8], BishopEvaluation, 0UL, 0UL);

        // Rook positions
        score += PieceEvaluation(piece_list_list[3], RookEvaluation, 0UL, 0UL);
        score -= PieceEvaluation(piece_list_list[9], RookEvaluation, 0UL, 0UL);

        // Queen positions
        score += PieceEvaluation(piece_list_list[4], KnightEvaluation, 0UL, 0UL) * 2;
        score -= PieceEvaluation(piece_list_list[10], KnightEvaluation, 0UL, 0UL) * 2;

        return score;
    }

    int PieceEvaluation(PieceList piece_list, Func<Square, ulong, bool, ulong, int> evaluation, ulong bitboard, ulong bitboard2)
    {

        // If there are no pieces of this type
        if (piece_list.Count == 0) return 0;

        // Return the sum of score changes
        return piece_list.Sum(piece => evaluation(piece.Square, bitboard, piece.IsWhite, bitboard2));
    }

    int PawnEvaluation(Square square, ulong bitboard, bool white, ulong bitboard2) =>
        Math.Abs(white ? 7 : 0 - square.Rank) * 7 * ((PassedPawnMask(square) & bitboard2) == 0 ? 2 : 1) + (7 - Math.Abs(square.File * 2 - 7)) * 3 + (BitboardHelper.SquareIsSet(bitboard, square) ? 15 : 0) - 20;


    int KnightEvaluation(Square square, ulong bitboard, bool white, ulong bitboard2) =>
        (14 - Math.Abs(square.File * 2 - 7) - Math.Abs(square.Rank * 2 - 7)) * 5 - 40;


    int BishopEvaluation(Square square, ulong bitboard, bool white, ulong bitboard2) =>
        BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Bishop, square, 0UL)) * 4;


    int RookEvaluation(Square square, ulong bitboard, bool white, ulong bitboard2) =>
        Math.Abs(white ? 7 : 0 - square.Rank) * 4 + (BitboardHelper.SquareIsSet(bitboard, square) ? 25 : 0);


    int MoveOrderScore(Move move, ulong opponent_pawn_attacks)
    {
        int[] piece_scores = { 0, 100, 320, 300, 500, 800, 99999 };

        // Score for promoting
        int move_score = piece_scores[(int)move.PromotionPieceType];

        // Score for capturing
        if ((int)move.CapturePieceType > 0) move_score += 10 * piece_scores[(int)move.CapturePieceType] - piece_scores[(int)move.MovePieceType];

        // Score for not going to a pawn protected square
        if (BitboardHelper.SquareIsSet(opponent_pawn_attacks, move.TargetSquare)) move_score -= piece_scores[(int)move.MovePieceType];

        return move_score;
    }

    ulong PawnAttackBitboard(PieceList pawn_piece_list, bool white) =>
        pawn_piece_list                                // Get all the pawns                                                        
        .Select(piece => BitboardHelper                // For each pawn do the following
        .GetPawnAttacks(piece.Square, white))          // Get the attack bitboard of that pawn
        .Aggregate((current, next) => current | next); // Combine all individual bitboards 


    ulong PassedPawnMask(Square square) =>
        (0x0101010101010101UL << square.File | 0x0101010101010101UL << Math.Max(0, square.File - 1) | 0x0101010101010101UL << Math.Min(7, square.File + 1)) & ulong.MaxValue << 8 * (square.Rank + 1);

}