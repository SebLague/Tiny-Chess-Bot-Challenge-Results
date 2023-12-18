namespace auto_Bot_369;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_369 : IChessBot
{

    // flag if the Bot is supposed to be playing as white
    private bool isWhite;

    private int SEARCH_DEPTH = 5;
    private Dictionary<string, Move> transpositionTable = new Dictionary<string, Move>();



    private static int CountBits(ulong number)
    {
        //thanks chatGPT
        int count = 0;
        while (number > 0)
        {
            count += (int)(number & 1);
            number >>= 1;
        }
        return count;
    }


    /// <summary>
    /// Returns an estimate of the current board evaluation 
    /// currently just counts the number of pieces
    /// </summary>
    /// <param Board="board">The board to be evaluated.</param>
    /// <returns int="evaluation">The evaluation of the board.</returns>
    private double Evaluate(Board board)
    {
        ulong pieces;
        ulong opponent;
        if (isWhite)
        {
            pieces = board.WhitePiecesBitboard;
            opponent = board.BlackPiecesBitboard;
        }
        else
        {
            pieces = board.BlackPiecesBitboard;
            opponent = board.WhitePiecesBitboard;
        }
        //DivertedConsole.Write(CountBits(pieces));

        return (double)(CountBits(pieces) - CountBits(opponent));
    }



    // /// <summary>
    // /// Returns an estimate of the current board evaluation 
    // /// currently just counts the number of pieces
    // /// </summary>
    // /// <param Board="board">The board to be evaluated.</param>
    // /// <returns int="evaluation">The evaluation of the board.</returns>
    // private double Evaluate(Board board){
    //     PieceList[] pieceLists = board.GetAllPieceLists();
    //     int[] pieceValues = {1, 3, 3, 5, 9, 0, -1, -3, -3, -5, -9, 0};
    //     int count = 0;

    //     for(int i = 0; i < pieceLists.Length; i++){
    //         count += pieceLists[i].Count * pieceValues[i];
    //     }
    //     if (isWhite){
    //         return count;
    //     } else {
    //         return 0 - count;
    //     }
    // }

    /// <summary>
    /// Performs an alpha beta search on the board, deteremining 
    /// the best move to make given the specified depth
    /// </summary>
    /// <param Board="board">The board to be searched.</param>
    /// <param int="depth">The depth to search to.</param>
    /// <returns Value="value"> Value of the node </returns>
    private double Search(Board board, int depth, bool maximise, double alpha, double beta, bool verbose = false)
    {

        // if the depth is 0 the node is a leaf node
        if (depth <= 0)
        {

            if (maximise)
            {
                return Evaluate(board);

            }
            return 0 - Evaluate(board);

        }
        else if (board.IsInCheckmate())
        {
            /* if we're looking to maximise, and there is
            a checkmate, then the opponent has just moved,
            so the checkmate means a loss for us
            */

            if (maximise)
            {
                return double.NegativeInfinity;
            }
            // else we're winning!
            return double.PositiveInfinity;

        }
        else if (board.IsDraw())
        {
            return 0;

        }


        // if there's no reason to stop the search just continue
        Move[] moves = board.GetLegalMoves();
        double score;
        double bestScore;
        int newdepth;

        //sorting starting values for the best value
        if (maximise)
        {
            bestScore = double.NegativeInfinity;
        }
        else
        {
            bestScore = double.PositiveInfinity;
        }

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);

            // if (board.IsInCheck() && maximise && false){
            //     // search more where we are putting the opponent in check
            //     newdepth = depth;
            // } else {
            //     newdepth = depth - 1;
            // }


            score = Search(board, depth - 1, !(maximise), alpha, beta);


            //check to see if we should update the score
            if ((score > bestScore && maximise) || (bestScore > score && !maximise))
            {
                bestScore = score;

            }
            board.UndoMove((Move)moves[i]);


            if (maximise)
            {
                if (score > alpha)
                {
                    alpha = score;
                }
            }
            else
            {
                if (score < beta)
                {
                    beta = score;
                }
            }

            if (alpha >= beta)
            {
                return bestScore;
            }



        }
        return bestScore;

    }


    /// <summary>
    /// Initializes a new instance of the <see cref="Person"/> class.
    /// </summary>
    /// <param name="name">The name of the person.</param>
    /// <param name="age">The age of the person.</param>

    public Move Think(Board board, Timer timer)
    {
        string FENstring = board.GetFenString();
        Move bestMove = Move.NullMove;

        // checking if we already have the board value in our transpoition table
        if (transpositionTable.ContainsKey(FENstring))
        {
            transpositionTable.TryGetValue(FENstring, out bestMove);
            return bestMove;
        }


        isWhite = board.IsWhiteToMove;

        double bestScore = double.NegativeInfinity;
        Move[] moves = board.GetLegalMoves();
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;
        Random random = new Random();
        int moveVariability = 0;




        /// Loops through and searches each possible node 
        /// for the best score
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove((Move)moves[i]);
            double score = Search(board, SEARCH_DEPTH, false, alpha, beta);
            board.UndoMove((Move)moves[i]);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = moves[i];
            }

            //adjusting beta
            if (score > alpha)
            {
                alpha = score;

                if (alpha >= beta)
                {
                    return bestMove;
                }
            }
        }

        // if there's gonna be a checkmate just pick a random mvoe
        if (bestMove.IsNull)
        {
            DivertedConsole.Write("Checkmate accepted");
            DivertedConsole.Write(board.GetFenString());
            transpositionTable[FENstring] = moves[random.Next(0, moves.Length - 1)];
            return moves[random.Next(0, moves.Length - 1)];
        }

        transpositionTable[FENstring] = bestMove;
        return bestMove;
    }
}