namespace auto_Bot_104;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_104 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        bool white_turn = board.IsWhiteToMove;
        int len = moves.Length;
        int[] eval = new int[len];
        int best_eval = white_turn ? -100 : 100;
        for (int i = 0; i < len; i++)
        {
            eval[i] = minmax_eval(board, moves[i], 1, timer);
            // if(i%8==0) DivertedConsole.Write("\n");
            // DivertedConsole.Write(moves[i]);
            // DivertedConsole.Write(": ");
            // DivertedConsole.Write(eval[i]);
            // DivertedConsole.Write("\t ");
            if ((white_turn && eval[i] > best_eval) || (!white_turn && eval[i] < best_eval))
            {
                best_eval = eval[i];
            }
        }
        List<int> best_moves = new List<int>(); // get index of moves with best evaluation
        for (int i = 0; i < len; i++)
        {
            if (eval[i] == best_eval) best_moves.Add(i);
        }
        Random rand = new Random();
        int index_selected = best_moves[rand.Next(0, best_moves.Count)]; // get a random index from the list of best moves
        // DivertedConsole.Write("\n\nSelected ");
        // DivertedConsole.Write(moves[index_selected]);
        //  DivertedConsole.Write("\tEvaluation: ");
        // DivertedConsole.Write(best_eval);
        return moves[index_selected]; // return random move with best evaluation
    }
    int minmax_eval(Board board, Move selected_move, int depth, Timer timer)
    {
        board.MakeMove(selected_move);
        bool white_turn = board.IsWhiteToMove;
        int best_eval = white_turn ? -100 : 100;
        if (board.IsDraw())
        {
            board.UndoMove(selected_move);
            return 0;
        }
        if (board.IsInCheckmate())
        {
            board.UndoMove(selected_move);
            return best_eval;
        }
        Move[] moves = board.GetLegalMoves();
        if (depth > 3)
        {
            best_eval = get_evaluation(board.GetAllPieceLists()); // depth of the board
            // if (depth<4) best_eval+= white_turn ? -5 : 5; // give low priority if bot is thinking for long
        }
        else
        {
            int eval;
            for (int i = 0; i < moves.Length; i++)
            {
                eval = minmax_eval(board, moves[i], depth + 1, timer);
                if ((white_turn && eval > best_eval) || (!white_turn && eval < best_eval))
                {
                    if (eval == 100 || eval == -100)
                    {
                        board.UndoMove(selected_move);
                        return eval;
                    }
                    best_eval = eval;
                }
            }
        }
        board.UndoMove(selected_move);
        return best_eval;
    }
    int get_evaluation(PieceList[] pieces_list)
    {
        int eval = 0;
        for (int i = 0; i < 5; i++)
        {
            eval += (pieces_list[i].Count - pieces_list[i + 6].Count) * picece_value[i];
        }
        return eval;
    }
    int[] picece_value = { 1, 3, 3, 5, 9 };
}