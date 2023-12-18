namespace auto_Bot_67;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_67 : IChessBot
{
    int[] pieceValues = { 100, 300, 300, 500, 900, 1000 }; //set the values for the pices pawn, knight, bishop, rook, queen, king

    public Move Think(Board board, Timer timer)
    {
        return GetBestMove(board); //calls method / object dunno what it is.
    }

    int bestScore = 0; //init the variable

    // create the dictonary to store all the moves and the scores from the evaluation "dunny why it only work with dynamic
    IDictionary<dynamic, int> listen = new Dictionary<dynamic, int>();

    Move GetBestMove(Board board)
    {
        System.Random random = new();
        Move[] moves = board.GetLegalMoves(); //get all the legal moves. and adds it to ? object i think.
        //stolen from the video to get it to pick a random move.
        Move bestMove = moves[random.Next(moves.Length)];

        /* DEBUG */
        //DivertedConsole.Write("\n"); //used for debugging to seperate each ply.

        foreach (Move move in moves)
        {
            board.MakeMove(move); //Make the move on the board
            int score = evaluation(board, move); //send the move to the evalutation "method or function dunno what its called. and add the return as score.
            listen.Add(move, score); //add the move and the score, to the dictonary 
            board.UndoMove(move); //undo the move so i can itterate over all the moves.

            /* DEBUG */
            //DivertedConsole.Write(move + " Score: " + score + " Best: " + bestScore); //used for debug.

            var min = listen.MinBy(x => x.Value); //finds the lowest score(value) in the dictonary. and at it to the var.
            var max = listen.MaxBy(x => x.Value); //finds the higest score(value) in the dictonary. and at it to the var.

            /* DEBUG */
            //DivertedConsole.Write("Lavest key:" + min.Key); //used for debug
            //DivertedConsole.Write(bestMove); //used for debug

            if (MoveIsCheckmate(board, move))
            {
                bestMove = move; //Add the move to the Best move variable and jumps out the foreach
                /* DEBUG */
                //DivertedConsole.Write("Found CheckMate" + move); //Used for debug
                break;
            }

            /* Not usefull to have here. since it will just end bad. But its here just in case. i need it
            if (MoveIsCheck(board, move))
            {
                bestScore = move;
                DivertedConsole.Write("Found check: " + move);
                break;
            }
            */

            //This if statement is only when the bot is playing white.
            if (board.IsWhiteToMove)
            {

                if (max.Value > bestScore) //if the value is greater the the bestscore it will jump in here.
                {
                    bestScore = max.Value; //set the new best score
                    bestMove = max.Key; //set the bestmove from the Dynamic key.

                }
                else if (max.Value == bestScore) //If they are equal pick one random of the moves and play it.
                {
                    bestScore = max.Value;
                    /* DEBUG */
                    //DivertedConsole.Write(max.Value + "==" + bestScore); //used for debug


                }
                else if (max.Value < bestScore)
                {
                    bestScore = max.Value;
                    bestMove = max.Key;
                    /* DEBUG */
                    //DivertedConsole.Write("Bestmove Dict" + max.Value, max.Key); //used for debug
                }
                else
                {
                    bestMove = moves[random.Next(moves.Length)]; //if none of the above apply it will just pick a random move.
                }
            }
            //Here is only when the bot is Black
            else
            {
                if (min.Value < bestScore) //if the value is lower the the bestscore it will jump in here.
                {
                    bestScore = min.Value; //set the new best score
                    bestMove = min.Key; //set the best move to the Dynamic key.

                }
                else if (min.Value == bestScore) //If they are equal pick one random of the moves and play it
                {
                    bestScore = min.Value;
                    /* DEBUG */
                    //DivertedConsole.Write(min.Value + "==" + bestScore); /used for debug


                }
                else if (min.Value > bestScore)
                {
                    bestScore = min.Value;
                    bestMove = min.Key;
                    /* DEBUG */
                    //DivertedConsole.Write("Bestmove Dict" + min.Value, min.Key); //used for debug
                }
                else
                {
                    bestMove = moves[random.Next(moves.Length)]; //pick one random of the moves and play it
                }
            }
        }

        DivertedConsole.Write("Played " + bestMove + "Score: " + bestScore); //debug just to se the played moved and the score.
        listen.Clear(); //clear the dictonary to next move.
        return bestMove; //send the best move up to the function / object that calls this loop.
    }

    int evaluation(Board board, Move move) //function / object to do the calculation to get the score.  *Help from discord A_RandomNoob*
    {
        PieceList[] pieceLists = board.GetAllPieceLists(); //add all the pieces in a list i think.
        int evaluate = 0; //init the evaluate.

        for (int i = 0; i < 6; i++) // for loop for 6 times
        {
            int whiteCount = board.GetPieceList((PieceType)(i + 1), true).Count; //set variable to all the pices pawn, knight, bishop, rook, queen, king from the list and see if it´s white
            int blackCount = board.GetPieceList((PieceType)(i + 1), false).Count; //set variable to all the pices pawn, knight, bishop, rook, queen, king from the list and see if it´s black
            evaluate += pieceValues[i] * (whiteCount - blackCount); //add the values to the evalutate where it multiply the values from the pices, with the count of the pices, and then try and when first run its should be = 0
        }
        /* DEBUG */
        //Debug.WriteLine("evaluation for " + move +" - "+ evaluate); Used for debug

        return evaluate; //return the values to the place it got called.
    }

    //stolen this from evil bot. since it just give meaning
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    //Only here just in case i can use it for something. /also stolen from Evilbot
    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }
}