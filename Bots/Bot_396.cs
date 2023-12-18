namespace auto_Bot_396;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

// Baby Squid
// by Jonas Tyroller - https://www.youtube.com/@jonastyroller

public class Bot_396 : IChessBot
{
    // Piece values:
    int[] pieceValues = { 200, 750, 750, 1250, 2250, 0 };

    // Pools:
    Move[][] allPossibleMoves = new Move[50][]; // [depth][move]
    MoveEvaluation[][] moveEvaluationsPool = new MoveEvaluation[50][]; // [depth][move]
    List<MoveEvaluation>[] moveEvaluations = new List<MoveEvaluation>[50]; // [depth]

    // Other variables:
    Move bestMove;
    Timer timer;
    Board board;
    float timeBudgetForThisTurn;

    // Saving tokens:
    bool IsWhiteToMove => board.IsWhiteToMove;
    bool OutOfTime => timer.MillisecondsElapsedThisTurn > timeBudgetForThisTurn;

    // INITIALIZATION
    // Initializing required data structures:
    // -------------------------------------------------------------------------------------------------------------------

    public Bot_396()
    {
        for (int i = 0; i < 50; i++)
        {
            allPossibleMoves[i] = new Move[218];
            moveEvaluations[i] = new List<MoveEvaluation>();
            moveEvaluationsPool[i] = new MoveEvaluation[218];
            for (int j = 0; j < 218; j++)
            {
                moveEvaluationsPool[i][j] = new MoveEvaluation();
            }
        }
    }

    // OPENING DICTIONARY
    // For the opening dictionary:
    // -------------------------------------------------------------------------------------------------------------------

    private Dictionary<ulong, ushort> openingTable = new Dictionary<ulong, ushort>
    {
        { 18446462598732906495, 3364 }, // e2e4
        { 18446462599001337855, 666 },  // e2e4, c7c5
        { 18445336716274364415, 4013 }, // e2e4, c7c5, g1f3
        { 18445336716276461503, 723 },  // e2e4, c7c5, g1f3, d7d6
        // -                            // e2e4, e7e5
        { 18441959068093444095, 4013},  // e2e4, e7e5, g1f3
        // -                            // e2e4, e7e5, g1f3, b8c6
        { 18297848278066196415, 3929},  // e2e4, e7e5, g1f3, b8c6, f1b5
        // -                            // e2e4, b8c6
        { 18302351808971993087, 3299},  // e2e4, b8c6, d2d4
        // -                            // e2e4, e7e6
        { 18441976591560011775, 3299},  // e2e4, e7e6, d2d4
        // --------------------------------------------------------------------------------- 
        // -                            // e2e3,
        { 18446462598733950975, 731},   // e2e3, d7d5
        // ---------------------------------------------------------------------------------
        // -                           // d2d4
        { 18446462598867122175, 731 }, // d2d4, d7d5
        // -                           // d2d4, d7d5, c2c4
        { 18444210833480283135, 788 }, // d2d4, d7d5, c2c4, e7e6
        // ---------------------------------------------------------------------------------
        // -                           // b1c3
        { 18446462598733168637, 731 }, // b1c3, d7d5
        // ---------------------------------------------------------------------------------
        // -                           // g1f3
        { 18446462598735003583, 731 }, // g1f3, d7d5
        // -                           // g1f3, d7d5, d2d4
        { 18444210833415272383, 788},  // g1f3, d7d5, d2d4, e7e6
    };

    private Move DecodeMove(ushort encoded, Board b)
    {
        int from = encoded >> 6;  // get the first 6 bits
        int to = encoded & 63;    // mask out the last 6 bits
        return new Move(ToMoveString(from) + ToMoveString(to), b);
    }

    private string ToMoveString(int pos)
    {
        int row = 8 - (pos / 8);
        char column = (char)(97 + pos % 8); // 97 is ASCII value for 'a'
        return $"{column}{row}";
    }

    // THINK FUNCTION
    // The main function returning the best move:
    // -------------------------------------------------------------------------------------------------------------------

    public Move Think(Board b, Timer t)
    {
        // DivertedConsole.Write("BABY SQUID IS THINKING... ----------------"); //#debug

        // Opening Table
        if (b.PlyCount < 5)
        {
            ushort encodedMove;
            if (openingTable.TryGetValue(b.AllPiecesBitboard, out encodedMove))
            {
                //DivertedConsole.Write("MOVE FROM OPENING TABLE USED"); //#debug
                return DecodeMove(encodedMove, b);
            }

            //DivertedConsole.Write("Current positon bitboard: " + b.AllPiecesBitboard); //#debug
        }

        // This saves us from having to pass these as arguments to each function:
        board = b; timer = t;

        // Generate a default move in case the move generation fails or in case we are giga low on time:
        Span<Move> moves = allPossibleMoves[0].AsSpan();
        board.GetLegalMovesNonAlloc(ref moves);
        bestMove = moves[0];

        // Actually if we have less than four ms left, it might be best to just return, so we don't run out:
        if (timer.MillisecondsRemaining <= 3)
            return bestMove;

        // Basic time management. If we have more time on the clock than our oponent we can think for a bit longer:
        // WARNING: This time management rule does not work as well against enemies with infinite time. Might need some adjustment for that.
        timeBudgetForThisTurn = timer.MillisecondsRemaining / 20f + Math.Max(0, timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining) / 7f;

        // Evaluate our position using pice values. This is so we can judge if a draw would be a desirable outcome:
        float score = 0;
        PieceList[] pieces = board.GetAllPieceLists();
        for (int i = 0; i < 5; i++)
        {
            score += pieces[i].Count * pieceValues[i];
            score -= pieces[i + 6].Count * pieceValues[i];
        }
        score *= IsWhiteToMove ? 1f : -1f;

        // ITERATIVE DEEPENING
        // We calculate the best move with increasing search depth:
        // -------------------------------------------------------------------

        // We keep track of our best streak meaning the if the best move stays the best as we increase the depth:
        ulong bestMoveStreakRawValue = 0;
        int bestMoveStreak = 0;

        // We also keep track of how long the last deepening step took us to execute:
        int clockLast = timer.MillisecondsRemaining;

        // We'll start at depth zero and work our way up to 50 (which we will never reach):
        for (int depth = 1; depth < 50; depth++)
        {
            // This returns the score of the best move but also sets the bestMove variable to the best move:
            float bestMoveScore = SearchMove(float.MinValue, float.MaxValue, depth, true, score, 0);

            // If the best move has not changed despite the increased search depth...
            if (bestMoveStreakRawValue == bestMove.RawValue)
            {
                // ...we keep track of that.
                bestMoveStreak++;

                // If the best move stays the same we can save some thinking time and interrupt the search:
                if (bestMoveStreak == 4 && depth >= 6)
                {
                    // DivertedConsole.Write("Obvious move detected. Saving some thinking time."); //#debug
                    break;
                }
            }
            else
            {
                // ...otherwise we reset the streak.
                bestMoveStreak = 1;
                bestMoveStreakRawValue = bestMove.RawValue;
            }

            // Print the best move of this deepening step:
            // DivertedConsole.Write(bestMoveScore + " for " + bestMove + " (" + bestMoveStreak + " streak)"); //#debug

            // If we are out of time, stop the iterative deepening:
            if (OutOfTime)
                break;

            // Calculate how long the last depth took us to calculate:
            int thinkingTimePassed = clockLast - timer.MillisecondsRemaining;
            // DivertedConsole.Write("Calculated in "+thinkingTimePassed); //#debug

            // If it is unlikely for us to finish another whole depth step, cancel early, cause usually that means deminishing returns from here on:
            if (timer.MillisecondsElapsedThisTurn + thinkingTimePassed * 10 > timeBudgetForThisTurn)
            {
                // DivertedConsole.Write("Not worth it to go any deeper than this."); //#debug
                break;
            }

            // Reset the clock to measure time spent thinking:
            clockLast = timer.MillisecondsRemaining;
        }

        // Encode best move in this position:
        // DivertedConsole.Write("Move code for " + bestMove.ToString() + ": " + EncodeMove(bestMove.ToString())); //#debug

        // Return the best move we came up with:
        return bestMove;
    }

    // SEARCH FUNCTION
    // Searches the space of available moves at the given depth and can set the bestMove variable:
    // -------------------------------------------------------------------------------------------------------------------

    public float SearchMove(float alpha, float beta, int depthleft, bool isTopLayer, float previousMovesEvalSum, int previousMoveCount)
    {
        // If the current board is a draw return a score of 0:
        if (board.IsDraw())
            return 0;

        // If the current board is a checkmate return a very low number but give points for delaying the outcome:
        if (board.IsInCheckmate())
            return -100000000 + 100000 * board.PlyCount;

        // If we reached the end of our search depth or we are out of time, return the score we calculated so far:
        if (depthleft == 0 || OutOfTime)
            return previousMovesEvalSum;

        // Generate the moves we can perform on the current board:
        Span<Move> moves = allPossibleMoves[depthleft].AsSpan();
        board.GetLegalMovesNonAlloc(ref moves);
        int moveCount = moves.Length;

        // Grab and prepare some lists from our pools:
        List<MoveEvaluation> mevaluations = moveEvaluations[depthleft];
        MoveEvaluation[] mevaluationsPool = moveEvaluationsPool[depthleft];
        mevaluations.Clear();

        // Check if this is already the final search depth:
        bool isFinalEvaluation = depthleft == 1;

        // This is essentially the most important part of our final position evaluation:
        int moveCountMinusPrevious = moveCount - previousMoveCount;

        // We add the move evaluations to our list and we also evaluate the moves for move ordering while doing so:
        for (int i = 0; i < moves.Length; i++)
            mevaluations.Add(SetAndEvaluateMove(mevaluationsPool[i], moves[i], previousMovesEvalSum, isFinalEvaluation, moveCountMinusPrevious));

        // Sort moves from best to worst to make sure more branches can be pruned:
        mevaluations.Sort((m1, m2) => m2.score.CompareTo(m1.score));

        // Sort the best move from the last deepening step to the top to searth that path first:
        if (isTopLayer)
        {
            for (int i = 0; i < mevaluations.Count; i++)
            {
                if (mevaluations[i].move.RawValue == bestMove.RawValue)
                {
                    mevaluations.Insert(0, mevaluations[i]);
                    mevaluations.RemoveAt(i + 1);
                    break;
                }
            }
        }

        // Recursively call the search for each move we have available:
        foreach (MoveEvaluation moveEval in mevaluations)
        {
            Move move = moveEval.move;

            // Make move, search, then undo the move:
            board.MakeMove(move);
            float score = -SearchMove(-beta, -alpha, depthleft - 1, false, -moveEval.score, moveCount);
            board.UndoMove(move);

            // Prune that shit if it's trash:
            if (score >= beta)
                return beta;

            // We found a new best move:
            if (score > alpha)
            {
                alpha = score;
                if (isTopLayer && !OutOfTime)
                    bestMove = move;
            }
        }

        // Return the best score we found:
        return alpha;
    }

    // EVALUATION FUNCTION
    // This evaluation function works a bit differently cause it only evaluates moves, not entire board positions:
    // -------------------------------------------------------------------------------------------------------------------

    MoveEvaluation SetAndEvaluateMove(MoveEvaluation moveEval, Move move, float previousMovesEvalSum, bool isFinalEvaluation, int availableMovesMinusEnemyMoves)
    {
        // The move we want to evaluate:
        moveEval.move = move;

        // Instead of starting from scratch each time we start with the sum of all previous move evaluations:
        float score = previousMovesEvalSum;

        // Give points for the position improvement of the move we are making:
        float startSquarePositionValue = PositionValueOfPiece(move.StartSquare, move.MovePieceType, IsWhiteToMove);
        float targetSquarePositionValue = PositionValueOfPiece(move.TargetSquare, move.MovePieceType, IsWhiteToMove);
        score += targetSquarePositionValue - startSquarePositionValue;

        // If this is the final evaluation:
        if (isFinalEvaluation)
        {
            // Give points for available moves minus the moves our enemy could make last turn:
            score += availableMovesMinusEnemyMoves / 2;

            // Consider the moved piece lost if it moves to a square atttacked by the opponent (this is to dampen the horizont effect):
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                score -= pieceValues[(int)move.MovePieceType - 1] + targetSquarePositionValue;
        }

        // Give points for capturing a pice
        if (move.IsCapture)
        {
            score += pieceValues[(int)move.CapturePieceType - 1] + PositionValueOfPiece(move.TargetSquare, move.CapturePieceType, !IsWhiteToMove);
        }

        // Give points for promoting a piece
        if (move.IsPromotion)
        {
            score += pieceValues[(int)move.PromotionPieceType - 1] - pieceValues[0];
        }

        // Set the score and return the evaluation:
        moveEval.score = score;
        return moveEval;
    }

    // POSITION VALUE EVALUATION
    // A helper function for the move evaluation above. It calculates the value of a piece given its position:
    // -------------------------------------------------------------------------------------------------------------------

    float PositionValueOfPiece(Square square, PieceType pieceType, bool colorIsWhite)
    {
        // Cache the position of the pice:
        int rank = square.Rank;
        int file = square.File;

        switch (pieceType)
        {
            // Pawns are worth more the further they have moved already:
            case PieceType.Pawn:
                if (colorIsWhite)
                    return rank - 1;
                else
                    return 6 - rank;

            // Knights and bishops are worth more in the center of the field:
            case PieceType.Bishop:
            case PieceType.Knight:
                float distance = Math.Abs(3.5f - rank) + Math.Abs(3.5f - file);
                return 6 - distance;
        }

        // All other piece types are not assigned with a position value:
        return 0;
    }

    // MOVE EVALUATION CLASS
    // Just holds a move and a corresponding score:
    // -------------------------------------------------------------------------------------------------------------------

    public class MoveEvaluation
    {
        public Move move;
        public float score;
    }
}