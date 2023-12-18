namespace auto_Bot_234;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_234 : IChessBot
{

    // Maximum depth of the search tree
    int maxDepth = 20;


    // Values below are used for heuristics when choosing moves

    // None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 1000 };
    int checkmateValue = 1000000;
    static int checkValue = 45; // Less than half a pawn
    int castleValue = 60;
    int drawValue; // Depends on if we are loosing or not, includes stalemate
    int drawValueWhenWinning = -1000;
    int drawValueWhenLosing = 70;

    int pawnMoveValue = 0; // Updated later in game
    int pawnMoveValueLateGame = 55; // More than for check
    // int distanceBetweenkingsEarlyGameValue = 20;
    int distanceBetweenKingsEndGameValue = -checkValue;
    int chaseKingWhenOpponentValueBelow = 1900;
    int attackedPiecesValueDenominator = 5; // The value of pieces being attacked after the last searched move is divided by this



    // Values below are updated during the game
    int ourPiecesValue = 0;
    int opponentPiecesValue = 0;
    int ourMaterialAdvantage = 0;

    bool weAreWinningSlightly = false;
    bool weAreWinningSubstantially = false;
    bool weAreLoosingSubstantially = false;
    bool isEndGame = false;


    // int number_of_moves_evaluated = 0;




    public Move Think(Board board, Timer timer)
    {
        // number_of_moves_evaluated = 0;

        // Set how much time can be spent on searching
        int maxMilisecondsPerTurn = milisecondsForMove(timer);

        ourPiecesValue = RandomExtensions.PlayerPiecesValue(board, pieceValues, true);
        opponentPiecesValue = RandomExtensions.PlayerPiecesValue(board, pieceValues, false);
        ourMaterialAdvantage = ourPiecesValue - opponentPiecesValue;

        isEndGame = (2 * timer.MillisecondsRemaining) < timer.GameStartTimeMilliseconds || opponentPiecesValue < pieceValues.Sum();

        weAreWinningSlightly = ourMaterialAdvantage > 0;
        weAreWinningSubstantially = ourMaterialAdvantage > 600;
        weAreLoosingSubstantially = ourMaterialAdvantage < -600;


        // Push pawns in late game
        if (isEndGame)
        {
            pawnMoveValue = pawnMoveValueLateGame;
        }


        // A draw is good if we are loosing
        if (!weAreLoosingSubstantially)
        {
            drawValue = drawValueWhenWinning;
        }
        else
        {
            drawValue = drawValueWhenLosing;
        }


        Move[] moves = board.GetLegalMoves();
        int[] ratings = new int[moves.Length]; // The (negative) rating of each move is stored here for sorting. Initialized to 0


        Random rng = new();
        rng.Shuffle(moves); // Shuffle so that if identically valuable moves are found we play a random one of them
        Move moveToPlay = moves[0]; // Move that is currently chosen as the one that should be played


        // Iterative deepening search
        int depth = 1;
        while ((1 * timer.MillisecondsElapsedThisTurn) < maxMilisecondsPerTurn && depth <= maxDepth)
        {

            // Sort array of moves using ratings from previous iterations to check good moves first
            // No need to sort using captures here thanks to iterative deepening
            Array.Sort(ratings, moves);


            // Select initial move for comparison. Then find the best move to play
            int highestValueScore = -checkmateValue; // (alpha)
            int beta = checkmateValue;
            Move bestMoveThisIteration = moveToPlay;

            for (int moveIndx = 0; moveIndx < moves.Length; moveIndx++)
            {
                Move move = moves[moveIndx];

                // End if out of time
                if (timer.MillisecondsElapsedThisTurn > maxMilisecondsPerTurn)
                {
                    // DivertedConsole.Write("Number of moves evaluated: " + number_of_moves_evaluated);
                    // DivertedConsole.Write("Out of time. Completed full search at depth of: " + (depth-1));
                    return bestMoveThisIteration; // Moves are evaluated in order of the best from previous iteration so this should be better than found in previous iteration
                }

                // Recursively calculate the rating for this move
                int rating = RateMoveRecursively(board, move, timer, maxMilisecondsPerTurn, depth, 1, 0, highestValueScore, beta);

                ratings[moveIndx] = -rating;
                if (rating > highestValueScore)
                {
                    highestValueScore = rating;
                    bestMoveThisIteration = move;
                }
            }

            // Save the best move of this iteration
            moveToPlay = bestMoveThisIteration;
            depth++;
        }

        // DivertedConsole.Write("Number of moves evaluated: " + number_of_moves_evaluated);
        return moveToPlay;
    }



    // This function calculates a score for a move recursively up to a given depth.
    // For us, the score is maximized, for the opponent it is minimized (score multiplier of 1 or -1).
    // Alpha - beta pruning is used to speed up the search
    private int RateMoveRecursively(Board board, Move move, Timer timer, int timeLimit, int depth,
                                    int scoreMultiplier, int scoreSoFar, int alpha, int beta)
    {

        if (timer.MillisecondsElapsedThisTurn > timeLimit)
        {
            return -checkmateValue;
        }

        // number_of_moves_evaluated += 1;


        // Update the score for this move

        Square otherKingSquare = board.GetKingSquare(!board.IsWhiteToMove);
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
        scoreSoFar += scoreMultiplier * capturedPieceValue;

        if (move.IsCastles)
        {
            scoreSoFar += scoreMultiplier * castleValue;
        }
        if (move.IsPromotion)
        {
            scoreSoFar += scoreMultiplier * (pieceValues[(int)move.PromotionPieceType] - pieceValues[(int)PieceType.Pawn]);
        }
        if (move.MovePieceType == PieceType.Pawn)
        {
            scoreSoFar += scoreMultiplier * pawnMoveValue;
        }
        if (move.MovePieceType == PieceType.King && weAreWinningSlightly && opponentPiecesValue < chaseKingWhenOpponentValueBelow && scoreMultiplier > 0)
        { // maximizing (us)
            // Push with the king
            int initialDistance = otherKingSquare.DistancebetweenSquares(move.StartSquare);
            int finalDistance = otherKingSquare.DistancebetweenSquares(move.TargetSquare);
            scoreSoFar += (finalDistance - initialDistance) * distanceBetweenKingsEndGameValue;
        }


        // Make the move and evaluate the situation

        board.MakeMove(move);

        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return scoreMultiplier * checkmateValue;
        }
        if (board.IsInCheck())
        {
            scoreSoFar += scoreMultiplier * checkValue;
        }
        if (board.IsDraw() || board.IsInStalemate())
        {
            scoreSoFar += drawValue;
        }



        // 
        // Search through the next possible moves of the other player
        //

        scoreMultiplier *= -1;  // The player changes so we switch the direction of score optimization
        Move[] nextMoves = board.GetLegalMoves();

        if (depth > 1 && nextMoves.Length > 0)
        {

            RandomExtensions.SortMoves(board, nextMoves, pieceValues);
            int bestFinalScoreAfterNextMove = -checkmateValue * scoreMultiplier;


            foreach (Move nextMove in nextMoves)
            {

                int rating = RateMoveRecursively(board, nextMove, timer, timeLimit, depth - 1, scoreMultiplier, scoreSoFar, alpha, beta);

                if (rating * scoreMultiplier > bestFinalScoreAfterNextMove * scoreMultiplier)
                {
                    bestFinalScoreAfterNextMove = rating;

                    // alpha-beta pruning
                    if (scoreMultiplier > 0)
                    { // Maximizing player
                        alpha = Math.Max(alpha, rating);
                    }
                    else
                    {                 // Minimising player
                        beta = Math.Min(beta, rating);
                    }
                    if (beta <= alpha)
                    {       // There is another path at least as good
                        bestFinalScoreAfterNextMove += scoreMultiplier; // The score can only get better. Improving by 1 allows for sorting moves.
                        break;
                    }

                }
            }

            board.UndoMove(move);
            return bestFinalScoreAfterNextMove;

        }
        else
        { // Run out of depth or moves
            int attackedPiecesValue = RandomExtensions.valueOfPiecesAttacked(board, pieceValues);
            scoreSoFar += scoreMultiplier * attackedPiecesValue / attackedPiecesValueDenominator;
            board.UndoMove(move);
            return scoreSoFar;
        }

    }




    // Helper functions below

    // Choose how much time can be used up on this move
    int milisecondsForMove(Timer timer)
    {
        int maxMilisecondsPerTurn = timer.GameStartTimeMilliseconds / 200 +
        (timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining) / 4 +
        timer.IncrementMilliseconds;

        maxMilisecondsPerTurn = Math.Max(timer.IncrementMilliseconds, Math.Max(maxMilisecondsPerTurn, timer.GameStartTimeMilliseconds / 500));

        return maxMilisecondsPerTurn;
    }

}






static class RandomExtensions
{
    // Function for shuffling the moves
    public static void Shuffle<T>(this Random rng, T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            (array[k], array[n]) = (array[n], array[k]);
        }
    }


    // Manhattan distance between two squares
    public static int DistancebetweenSquares(this Square s1, Square s2)
    {
        return Math.Abs(s1.File - s2.File) + Math.Abs(s1.Rank - s2.Rank);
    }


    // The values of all pieces of one player summed up
    public static int PlayerPiecesValue(Board board, int[] pieceValues, bool forCurrentTurnPlayer)
    {
        int pieceTotalValue = 0;
        for (int pieceNum = 1; pieceNum < pieceValues.Length; pieceNum++)
        {
            PieceList pieces = board.GetPieceList((PieceType)pieceNum, board.IsWhiteToMove ^ (!forCurrentTurnPlayer));
            pieceTotalValue += pieces.Count * pieceValues[pieceNum];
        }
        return pieceTotalValue;
    }

    public static int valueOfPiecesAttacked(Board board, int[] pieceValues)
    {
        int totalCapturedValue = 0;
        Move[] captureMoves = board.GetLegalMoves(true);
        foreach (Move move in captureMoves)
        {
            totalCapturedValue += pieceValues[(int)move.CapturePieceType];
        }
        return totalCapturedValue;
    }


    // Sort moves, so that the most promising ones are evaluated first.
    public static void SortMoves(Board board, Move[] moves, int[] pieceValues)
    {
        int len = moves.Length;
        double[] captureValues = new double[len];

        for (int i = 0; i < len; i++)
        {
            // Prioritize captures of high-value figures using low-value figures
            captureValues[i] = -1.0 * pieceValues[(int)moves[i].CapturePieceType] / pieceValues[(int)moves[i].MovePieceType];
        }

        Array.Sort(captureValues, moves);
    }

}