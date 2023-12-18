namespace auto_Bot_224;
using ChessChallenge.API;
using System;

public class Bot_224 : IChessBot
{
    //Logic stats
    int[] selfPieceWeights =
    {
        0, //Unused - None
        100, //Pawn
        200, //Knight
        300, //Bishop
        350, //Rook
        500, //Queen
        900, //King
    };
    const int repeatMoveHeuristic = -50;
    const int capKingHeuristic = 30;
    const int checkmateHeuristic = 500;
    const int moveSquareThreatenedHeuristic = -30;
    const int pushKingHeuristic = 40;
    const int kingAdvanceHeuristic = 40;
    const int promotionHeuristic = 60;
    const int centerSquareWeight = 10;

    //Decision data
    Move[] repeatMoveArray = new Move[8];
    int repeatMoveIndex = 0;

    //State cache
    Board _board;
    bool isWhiteTeam;
    Random fallbackRand = new(150);

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        Move[] moves = GetLegalMoves();

        if (timer.MillisecondsRemaining < 500)
        {
            //Panic time!
            //We can NOT run out of time, so use a faster method of picking
            //based on random chance - it's at least something
            DivertedConsole.Write("PANIC TIME");
            return moves[fallbackRand.Next(moves.Length)];
        }

        isWhiteTeam = _board.IsWhiteToMove;
        int enemyPiecesCount = 0;
        int myPieceCount = 0;

        foreach (PieceType thisType in Enum.GetValues(typeof(PieceType)))
        {
            if (thisType == PieceType.None)
            {
                continue;
            }

            enemyPiecesCount += _board.GetPieceList(thisType, !isWhiteTeam).Count;
            myPieceCount += _board.GetPieceList(thisType, isWhiteTeam).Count;
        }
        //Determine how close we are to the endgame
        int finalPushKingHeuristic = 0;
        int finalKingAdvanceHeuristic = 0;

        if (enemyPiecesCount <= 8)
        {
            double endgameLerpAmt = InverseLerp(8.0, 1.0, (double)enemyPiecesCount);
            finalPushKingHeuristic = (int)Math.Round(BotLerp(0.0, (double)pushKingHeuristic, endgameLerpAmt));
            finalKingAdvanceHeuristic = (int)Math.Round(BotLerp(0.0, (double)kingAdvanceHeuristic, endgameLerpAmt));
        }

        int searchDepth = 3;
        if (enemyPiecesCount + myPieceCount <= 8)
        {
            searchDepth = 4;
        }

        //Acquire all the move values and pick the best one
        int highestMoveIndex = 0;
        int highestValue = int.MinValue;

        for (int i = 0; i < moves.Length; i++)
        {
            var thisMove = moves[i];
            //Apply the move so we can do heuristics outside of minimax later
            _board.MakeMove(thisMove);
            //First, run the results of minimax
            int thisMoveValue = MiniMaxAbsolute(searchDepth, true, isWhiteTeam ? 1 : -1);

            //Add first stage heuristic modifiers for this single move

            //Check if this move was made previously and decrease score
            //This is to ensure that the game doesn't draw as easily
            foreach (Move repeatMove in repeatMoveArray)
            {
                if (repeatMove.IsNull)
                {
                    continue;
                }

                if (repeatMove.Equals(thisMove))
                {
                    thisMoveValue += repeatMoveHeuristic;
                    break;
                }

            }
            //Check if this new move now threatens
            //the opponent's king, if so this is good
            //for trapping it but not if we're too deep in the endgame
            if (_board.IsInCheck() && enemyPiecesCount > 8)
            {
                thisMoveValue += capKingHeuristic;
            }
            //Check if it like super threatens the opponent king,
            //and if so that's a big reason to make this move
            if (_board.IsInCheckmate())
            {
                thisMoveValue += checkmateHeuristic;
            }
            var targetSquare = thisMove.TargetSquare;
            //For pawns and knights, center positions are better
            if (enemyPiecesCount > 8)
            {
                if (thisMove.MovePieceType == PieceType.Pawn || thisMove.MovePieceType == PieceType.Knight)
                {
                    if (targetSquare.Rank >= 2 && targetSquare.Rank <= 5
                    && targetSquare.File >= 2 && targetSquare.File <= 5)
                    {
                        thisMoveValue += centerSquareWeight;

                        if (targetSquare.Rank >= 3 && targetSquare.Rank <= 4
                        && targetSquare.File >= 3 && targetSquare.File <= 4)
                        {
                            thisMoveValue += centerSquareWeight;
                        }
                    }
                }
            }
            //Add weight as we approach endgame
            //based on how far the opponent king is pushed to the corners
            //This helps to acquire checkmate
            var enemyKingSquare = _board.GetKingSquare(!isWhiteTeam);
            int opponentKingDist = Math.Max(3 - enemyKingSquare.File, enemyKingSquare.File - 4) + Math.Max(3 - enemyKingSquare.Rank, enemyKingSquare.Rank - 4);
            int kingPushBonus = (int)Math.Round(BotLerp(0.0, finalPushKingHeuristic, opponentKingDist / 6.0));
            thisMoveValue += kingPushBonus;
            //Add weight to move our king towards opponent king
            var ourKingSquare = _board.GetKingSquare(isWhiteTeam);
            int distBetweenKings = Math.Abs(ourKingSquare.File - enemyKingSquare.File) + Math.Abs(ourKingSquare.Rank - enemyKingSquare.Rank);
            int kingAdvanceBonus = (int)Math.Round(BotLerp(0.0, finalKingAdvanceHeuristic, (14.0 - (double)distBetweenKings) / 14.0));
            thisMoveValue += kingAdvanceBonus;

            //Now undo the move to return the board state
            _board.UndoMove(thisMove);

            //Add second stage heuristics
            //That apply "before" we make this move

            //Check if this move will directly threaten our piece
            //This is to ensure fewer "even trades" that don't get us anywhere
            if (_board.SquareIsAttackedByOpponent(thisMove.TargetSquare))
            {
                thisMoveValue += moveSquareThreatenedHeuristic;
            }
            //Add bonus for promotion
            if (thisMove.IsPromotion)
            {
                thisMoveValue += promotionHeuristic;
            }

            //Heuristics complete

            if (thisMoveValue > highestValue)
            {
                highestValue = thisMoveValue;
                highestMoveIndex = i;
            }
            if (timer.MillisecondsRemaining < 500)
            {
                //Panic time!
                DivertedConsole.Write("PANIC TIME 2: THE PANICKING");
                break;
            }
        }

        DivertedConsole.Write("Highest value: " + highestValue);
        var highestMove = moves[highestMoveIndex];

        //Add this chosen move to the repeat move array
        repeatMoveArray[repeatMoveIndex] = highestMove;
        repeatMoveIndex++;
        if (repeatMoveIndex >= repeatMoveArray.Length)
        {
            repeatMoveIndex = 0;
        }
        return highestMove;
    }

    //MAIN LOGIC

    /// <summary>
    /// Run Minimax on the current move
    /// </summary>
    /// <param name="depth">How many times to continue the search</param>
    /// <param name="thisTeamTurn">Whether we're making the move or not</param>
    /// <returns></returns>
    int MiniMaxAbsolute(int depth, bool thisTeamTurn, int teamMul, int alpha = int.MaxValue, int beta = -int.MaxValue)
    {

        //Since there might not be legal moves by this point,
        //We need to escape if the game has ended
        if (_board.IsDraw())
        {
            //This is bad for everyone
            if (isWhiteTeam)
            {
                return -40000 * teamMul;
            }
            return 40000 * teamMul;
        }
        if (_board.IsInCheckmate())
        {
            //If black's turn is now, we receive 1 in teamMul
            //And since checkmate is really bad for black,
            //we use positive value to return really a good scenario to white
            return 40000 * teamMul;
        }

        if (depth == 0)
        {
            //Count up number of pieces we have left
            //And also subtract the number of enemy pieces left
            //(with some modifiers)

            int finalValue = 0;
            foreach (PieceType thisType in Enum.GetValues(typeof(PieceType)))
            {
                //For each type, get the count and multiply by the weight of that type
                if (thisType == PieceType.None)
                {
                    continue;
                }

                finalValue += (_board.GetPieceList(thisType, true).Count *
                    selfPieceWeights[(int)thisType]);
                finalValue -= (_board.GetPieceList(thisType, false).Count *
                    (selfPieceWeights[(int)thisType]));
            }
            return finalValue * teamMul;
        }

        //Get the new moves for the next turn
        var nextMoves = GetLegalMoves();
        //Since we're now looking at the NEXT moves and not the one that was just made,
        //thisTeamTurn actually means we should minimize, not maximize
        int bestValue = int.MaxValue;
        if (nextMoves.Length == 0)
        {
            DivertedConsole.Write("UH OH");
        }

        //DivertedConsole.Write(nextMoves.Length);
        foreach (Move nextMove in nextMoves)
        {
            //Make the next move for next iteration/eval
            _board.MakeMove(nextMove);

            //Higher value = we're winning
            //Lower value = enemy is winning
            int thisVal = -MiniMaxAbsolute(depth - 1, !thisTeamTurn, -teamMul, -beta, -alpha);

            //Return the board state
            _board.UndoMove(nextMove);

            bestValue = Math.Min(thisVal, bestValue);
            alpha = Math.Min(thisVal, alpha);
            if (alpha <= beta)
            {
                break;
            }
        }

        return bestValue;
    }


    //HELPERS

    /// <summary>
    /// Helper to return legal moves for saving characters
    /// </summary>
    /// <returns></returns>
    Move[] GetLegalMoves()
    {
        return _board.GetLegalMoves();
    }

    /// <summary>
    /// Linear interpolation between two floats
    /// </summary>
    double BotLerp(double v1, double v2, double by)
    {
        return v1 * (1 - by) + v2 * by;
    }

    /// <summary>
    /// Reverse interpolation
    /// </summary>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="amt"></param>
    /// <returns></returns>
    double InverseLerp(double v1, double v2, double amt)
    {
        return (amt - v1) / (v2 - v1);
    }
}