namespace auto_Bot_50;
using ChessChallenge.API;
using System;
using System.Collections.Generic;



//****************************
// TLMChessBot by Möhrchen :)
//****************************
//Estimated Chess.com rating: 1250


//Link to doc: [https://seblague.github.io/chess-coding-challenge/documentation/]



public class Bot_50 : IChessBot
{
    #region ** MAIN **

    //RNG for the Monte Carlo Tree Search -> Now for the other opening approach
    private System.Random rng = new System.Random();

    //Initialize Bot
    public Bot_50()
    {
        //Precalculate the "square-evaluations" of all pieces
        for (int i = 0; i < 6; i++)
        {
            for (int b = 0; b < 64; b++)
            {
                int tempAmount = 0, tempAmount2 = 0;
                for (int j = 0; j < 4; j++)
                {
                    if (((squareULsEarlyGame[i, j] >> b) & 1ul) == 1) tempAmount += 1 << j;
                    if (((squareULsEndGame[i, j] >> b) & 1ul) == 1) tempAmount2 += 1 << j;
                }
                processedSquareULsEarlyGame[i, b] = tempAmount;
                processedSquareULsEarlyGame[i + 6, 63 - b] = -tempAmount;
                processedSquareULsEndGame[i, b] = tempAmount2;
                processedSquareULsEndGame[i + 6, 63 - b] = -tempAmount2;

                //Precalculate the transition between the early game evaluations and the endgame evaluations (currently using: linear function -0.03125x + 1)
                //That if statement positioned here saves 6 tokens compared to a new for loop
                if (b < 33) earlyGameMultiplier[b] = 1d - (endGameMultiplier[b] = Math.Clamp(-0.001d * (b * b) + 1d, 0d, 1d)); //-0.03125d * i + 1d, 0d, 1d) is the other function
            }
        }
    }

    public Move Think(Board board, Timer timer)
    {
        //Using the Monte Carlo Tree Search Algorithm only for the first move, to prevent having the same game scenario everytime
        if (evaluationType = board.PlyCount < 2) Minimax(board, 5); //Monte Carlo Tree Search
        else
        {
            //Minimax Iterative Deepening (min depth of 4)
            int searchDepth = 0, te = 0;

            //This timer management system manages the time like it would have at least 2 more moves to do (~3->~29, depending on the last depth search it needs to make)
            while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 100 || searchDepth < 4)
            {
                if (Math.Abs(te = Minimax(board, ++searchDepth)) == 30000)
                {
                    DivertedConsole.Write("Mate in " + searchDepth + " detected");
                    //Breaking out as soon as it has found a checkmate (positive or negative) => This prevents the bot from finding each turn new checkmates with the same length as before
                    break;
                }
            }

            DivertedConsole.Write(te + " [Depth = " + searchDepth + "]");
        }

        //If it runs out of time, itll play the illegal move "Null" -> This would be easily patchable but in cost of tokens
        return bestSavedMove;
    }

    #endregion

    #region ** SEARCH **

    private int MINIMAX_DEPTH, MINIMAX_DEPTH_END;
    private Move bestSavedMove;

    private int Minimax(Board board, int depth)
    {
        //A classic alpha beta pruned minimax algorithm
        MINIMAX_DEPTH = depth;
        MINIMAX_DEPTH_END = 0;
        return (board.IsWhiteToMove) ? Max(board, MINIMAX_DEPTH, int.MinValue, int.MaxValue) : Min(board, MINIMAX_DEPTH, int.MinValue, int.MaxValue);
    }

    private Move[] GetSortedLegalMoves(Board board)
    {
        System.Span<Move> nsm = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref nsm);
        List<Move> m = new List<Move>();
        for (int i = 0; i < nsm.Length; i++)
        {
            if (nsm[i].IsCapture) m.Insert(0, nsm[i]);
            else m.Add(nsm[i]);
        }
        return m.ToArray();
    }

    private int Max(Board board, int depth, int alpha, int beta)
    {
        //Check if wished search depth is reached or if the board is in a terminal state
        if (depth == MINIMAX_DEPTH_END || board.IsInCheckmate() || board.IsDraw()) return PositionalEvaluation(board);

        int maxValue = alpha;
        Move[] moveList = GetSortedLegalMoves(board);
        for (int m = 0; m < moveList.Length; m++)
        {

            board.MakeMove(moveList[m]);
            if (depth == 1 && moveList[m].IsCapture && !evaluationType) MINIMAX_DEPTH_END = -1;
            int value = Min(board, depth - 1, maxValue, beta); //Calling this function recursively (saves a lot of tokens)
            board.UndoMove(moveList[m]);

            //Replace saved value if a better move got found
            if (value > maxValue)
            {
                maxValue = value;

                //Replacing the currently saved Move if its one of the playable moves (first depth)
                if (depth == MINIMAX_DEPTH) bestSavedMove = moveList[m];

                //Alpha Beta Pruning
                if (maxValue >= beta) break;
            }
        }
        return maxValue;
    }

    private int Min(Board board, int depth, int alpha, int beta)
    {
        //Almost the same as "Max", just for the other player
        if (depth == MINIMAX_DEPTH_END || board.IsInCheckmate() || board.IsDraw()) return PositionalEvaluation(board);

        int minValue = beta;
        Move[] moveList = GetSortedLegalMoves(board);
        for (int m = 0; m < moveList.Length; m++)
        {
            board.MakeMove(moveList[m]);
            if (depth == 1 && moveList[m].IsCapture && !evaluationType) MINIMAX_DEPTH_END = -1;
            int value = Max(board, depth - 1, alpha, minValue);
            board.UndoMove(moveList[m]);

            if (value < minValue)
            {
                minValue = value;
                if (depth == MINIMAX_DEPTH) bestSavedMove = moveList[m];
                if (minValue <= alpha) break;
            }
        }
        return minValue;
    }

    #endregion

    #region ** EVALUATIONS **

    //EvaluationType ([= true] > Monte Carlo Tree Search / [= false] > Minimax)
    private bool evaluationType = false;


    //Factors which are indicating how important each piece is (first 6 are whites and second 6 are blacks)
    private readonly int[] evaluationFactors = new int[12] { 10, 30, 33, 50, 90, 0, -10, -30, -33, -50, -90, 0 };


    //4 ulongs for each piece to have the evaluation values of 0-15 for each square on which they can stand on (this method is espacially token effective, since the real values can be extracted in a low amount of code)
    private readonly ulong[,] squareULsEarlyGame = new ulong[6, 4] { //=> EARLY GAME
        { 72057489844666368, 72057489941200896, 71777112205295616, 72056597605515264 }, //Pawns
        { 66229402337280, 138797278691328, 66512870178816, 138538465099776 }, //Knights
        { 283467847680, 1120081920, 1519993344, 603979776 }, //Bishops
        { 90, 16711740, 189, 0 }, //Rooks
        { 3938364, 9252, 6168, 24 }, //Queen
        { 65316, 189, 126, 195 }  //King
    };
    private readonly ulong[,] squareULsEndGame = new ulong[6, 4] { //=> ENDGAME
        { 72057589759731456, 72057589742960640, 71777218556067840, 72056494526300160 }, //Pawns
        { 138797169770496, 72852350828544, 72826472103936, 65970697666560 }, //Knights
        { 66229406269440, 0, 0, 0 }, //Bishops
        { 1099494850815, 1099511627520, 18446742974197923840, 0 }, //Rooks
        { 707126099968, 280815279734784, 103481868288, 0 }, //Queen
        { 35539906700934656, 66229406269440, 0, 0 }  //King
    };
    private readonly int[,] processedSquareULsEarlyGame = new int[12, 64], processedSquareULsEndGame = new int[12, 64];
    private readonly double[] earlyGameMultiplier = new double[33], endGameMultiplier = new double[33];

    private int PositionalEvaluation(Board board)
    {
        //Returning 0 if the board position is a draw
        if (board.IsDraw()) return 0;

        //Evaluating checkmates as (-)30000
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -30000 : 30000;

        //=> Non special position evaluation
        int eval = 0;

        //Giving a bonus of (-)6 for having castling rights
        if (board.HasKingsideCastleRight(true) || board.HasQueensideCastleRight(true)) eval += 6;
        if (board.HasKingsideCastleRight(false) || board.HasQueensideCastleRight(false)) eval -= 6;

        PieceList[] pls = board.GetAllPieceLists();
        int totalPieceCount = 0;
        for (int i = 0; i < 12; i++)
        {
            //Evaluating piece values
            eval += evaluationFactors[i] * pls[i].Count;
            totalPieceCount += pls[i].Count;
        }
        for (int i = 0; i < 12; i++)
        {
            //Evaluate piece positioning
            for (int j = 0; j < pls[i].Count; j++)
            {
                if (evaluationType && (j == 1 || j == 7)) continue;
                int index = pls[i][j].Square.Index;
                eval += (int)((processedSquareULsEarlyGame[i, index] * earlyGameMultiplier[totalPieceCount]) + (processedSquareULsEndGame[i, index] * endGameMultiplier[totalPieceCount])); //+ (processedSquareULsEndGame[i, index] * endGameMultiplier[totalPieceCount])
            }
        }

        if (evaluationType) eval += rng.Next(0, 9) - 4;

        return eval;
    }

    #endregion

    #region ** Removed Monte Carlo Tree Search **

    //Originally used for the opening, but worse & much more token costly than the current opening randomizer


    //private int EVALUATE(Board board)
    //{
    //    //The general evaluation function which checks on which way the board position should get evaluated
    //    if (evaluationType) return RandomFutureMoveEvaluation(board);
    //    return PositionalEvaluation(board);
    //}

    //private int RandomFutureMoveEvaluation(Board board)
    //{
    //    //Monte Carlo Tree Search
    //
    //    int evaluationFromRandomMoves = 0;
    //    List<Move> movesListForUndo = new List<Move>();
    //
    //    //500 Iterations (Rollouts)
    //    for (int i = 0; i < 320; i++)
    //    {
    //        //Playing 3 random moves into the future
    //        for (int m = 0; m < 11; m++)
    //        {
    //            if (board.IsInCheckmate() || board.IsDraw()) break;
    //
    //            Move[] tempMoves = board.GetLegalMoves();
    //            Move futureMove = tempMoves[rng.Next(tempMoves.Length)];
    //            movesListForUndo.Add(futureMove);
    //            board.MakeMove(futureMove);
    //        }
    //
    //        //Evaluating the emerged board position
    //        evaluationFromRandomMoves += PositionalEvaluation(board);
    //
    //        //Undoing the moves in the reversed order
    //        for (int u = movesListForUndo.Count - 1; u > -1; u--) board.UndoMove(movesListForUndo[u]);
    //        movesListForUndo.Clear();
    //    }
    //
    //    return evaluationFromRandomMoves;
    //}

    #endregion
}