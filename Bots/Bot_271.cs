namespace auto_Bot_271;
using ChessChallenge.API;
using System;

public class Bot_271 : IChessBot
{
    // Some arbitrary duration we allow to investigate a single move, in
    // milliseconds.
    // Obviously, the better the CPU, the better it gets.
    static private readonly int duration_per_investigation = 10000;

    // Some arbitrary level to not reach for investigation.
    // Obviously, the bigger, the better it (should) get.
    static private readonly int max_level = 3;

    // The score of each individual piece type.
    static private readonly int[] score_per_piece = { 0, 1, 3, 3, 3, 7, 9 };

    // The pseudo-random generator.
    private static readonly Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        DivertedConsole.Write(
            "Thinking at {0}", timer.MillisecondsRemaining);

        return ThinkUntil(
            board, timer,
            timer.MillisecondsRemaining - duration_per_investigation, 0).Item1;
    }

    static private (Move, int) ThinkUntil(Board board, Timer timer, int limit_ms, int level)
    {
        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0)
            return (Move.NullMove, 0);

        (Move, int) best = (moves[0], 0);
        int score;

        if (level >= max_level)
            return best;

        for (int i = 0;
            i < moves.Length && timer.MillisecondsRemaining > limit_ms; i++)
        {
            score = CalculateScoreOfMove(board, moves[i], timer, limit_ms, 0, level);

            if (score != 0)
                log(level, "- {0} ({3}) got a score of {1} (time={2})",
                    moves[i], score, timer.MillisecondsRemaining, moves[i].MovePieceType);

            if (score > best.Item2
                || (score == best.Item2
                    && rng.Next(board.GetAllPieceLists().Length + 1) == 0))
                best = (moves[i], score);
        }

        if (level == 0)
            log(level, "Got a result with {0} remaining, {1} (score={2})",
                timer.MillisecondsRemaining - limit_ms, best.Item1, best.Item2);

        return best;
    }

    static private int CalculateScoreOfMove(
        Board board, Move move, Timer timer, int limit_ms, int score, int level)
    {
        if (timer.MillisecondsRemaining <= limit_ms)
            // Too late ! Too bad, but let's stop here anyway.
            return 0;

        // Ok, first, we need to make sure we don't move the king for no reason.
        if (move.MovePieceType == PieceType.King
            && !board.IsInCheck()
            && !move.IsCastles)
        {
            log(level, "King {0} is moving on its own ! {1}", move, board.IsInCheck());
            score -= 2;
        }

        // The score of the move depends on the promoted piece.
        if (move.IsPromotion)
            score += 2;

        // The score of the move depends on the captured piece.
        if (move.IsCapture)
            score +=
                score_per_piece[(int)move.CapturePieceType]
                * score_per_piece[(int)move.MovePieceType];

        // Apply the move, so that we see what happens there.
        board.MakeMove(move);

        // Obviously, check and checkmate are interresting.
        if (board.IsInCheck())
            score += 15;
        if (board.IsInCheckmate())
            score += 30;

        //log(level, "Searching in board for {0}", move);

        (Move, int) best = ThinkUntil(board, timer, limit_ms, level + 1);

        //log(level, "\tConsidering best {0}", best);

        if (best.Item2 > 0 && best.Item1.TargetSquare == move.TargetSquare)
            // This move directly counters our own move, so let's think twice
            // about it.
            score -= 2;
        else if (best.Item2 > 0)
            // Feels right to remove the best score from our score, but
            // we are just hoping the player of this side will not see
            // this move, or will follow a (weird) strategy that does not
            // imply this move.
            score -= 1;

        // Undo the move, let's allow an analyse for another move.
        board.UndoMove(move);

        return score;
    }

    static private void log(int indent, string format, params object?[]? args)
    {
        DivertedConsole.Write(new String('\t', indent));
        DivertedConsole.Write(format, args);
    }
}