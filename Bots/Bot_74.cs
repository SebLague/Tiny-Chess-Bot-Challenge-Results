namespace auto_Bot_74;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_74 : IChessBot
{
    List<string> all_files = new List<string> { "a", "b", "c", "d", "e", "f", "g", "h" };
    public double board_evaluator(Board board)
    {
        //None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6
        List<double> piece_value = new List<double> { 1.0, 3.0, 3.0, 5.0, 9.0 }; // verify piece value
        double board_value_white = 0.0;
        double board_value_black = 0.0;
        for (int i = 1; i < 6; i++)
        {
            board_value_white += board.GetPieceList((PieceType)i, true).Count * piece_value[i - 1];
            board_value_black += board.GetPieceList((PieceType)i, false).Count * piece_value[i - 1];
        }
        var factor_proba = 1.0; //0.001;
        if (board_value_white + board_value_black == 0)
        {
            return 0.0;
        }
        return factor_proba * (2 * ((board_value_white) / (board_value_white + board_value_black)) - 1); //0.1, with 1.0 this mean that the probability of wining is the fraction of piece value of our color
        // Be carefull with to big value this can lead to prefer wining some pieces more than checkmates
    }

    public double Simulate(Board board)
    {
        bool AmIWhite = !board.IsWhiteToMove;
        var max_depth = 20; //25; //50; 
        var depth = 0;
        while (!board.IsInCheckmate() && depth < max_depth && !board.IsDraw())
        {
            Random rng = new();
            Move moveToPlay;
            var strategy_num = rng.Next(3);
            if (strategy_num == 1)
            {
                moveToPlay = get_best_move(board, false); // Maybe we suppose that the openent will be to aggressive or to safe
            }
            else if (strategy_num == 2)
            {
                moveToPlay = get_best_move(board, true);
            }
            else
            {
                Move[] strat3_moves = board.GetLegalMoves();
                moveToPlay = strat3_moves[rng.Next(strat3_moves.Length)];
            }
            board.MakeMove(moveToPlay);
            depth += 1;
        }
        if (board.IsDraw())
        {
            return 0.0;
        }
        else if (!board.IsInCheckmate())
        {
            double board_value = board_evaluator(board);
            //DivertedConsole.Write(board_value);
            if (AmIWhite)
            {
                return board_value;
            }
            else
            {
                return -board_value;
            }
        }
        else
        {
            if ((board.IsWhiteToMove && AmIWhite) || (!board.IsWhiteToMove && !AmIWhite))
            {
                return -1.0;
            }
            else
            {
                return 1.0;
            }
        }
    }

    public int get_best_move_index(List<double> c_val, List<int> nb_exploration, int nb_sim_done)
    {
        var nb_possible_moves = c_val.Count;
        double exploration_factor = 0.005 * Math.Sqrt(2); //0.1
        var win_proba = new List<double>(new double[nb_possible_moves]);
        for (int i = 0; i < nb_possible_moves; i++)
        {
            win_proba[i] = (c_val[i] / nb_exploration[i]) + (exploration_factor * ((double)Math.Sqrt(nb_sim_done) / (double)nb_exploration[i]));
        }
        double maxValue = win_proba.Max();

        int maxIndex = win_proba.ToList().IndexOf(maxValue);
        return maxIndex;
    }

    public List<Move> get_safe_move(Board board, Move[] allMoves)
    {
        List<Move> safe_moves = new List<Move>();
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            bool safe = true;
            foreach (Move move_adv in board.GetLegalMoves())
            {
                if (move_adv.TargetSquare == move.TargetSquare)
                {
                    safe = false;
                }
            }
            if (safe)
            {
                safe_moves.Add(move);
            }
            board.UndoMove(move);
        }
        return safe_moves;
    }
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move get_best_move(Board board, bool safe)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }
        }
        List<Move> allSafeMoves;
        if (safe)
        {
            allSafeMoves = get_safe_move(board, allMoves);
        }
        else
        {
            allSafeMoves = allMoves.ToList();
        }
        foreach (Move move in allSafeMoves)
        {
            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        var c_val = new List<double>(new double[moves.Length]);
        var nb_exploration = new List<int>(new int[moves.Length]);
        var nb_simmulations = 500;

        var estimated_lenght = 100;

        string origin_fen = board.GetFenString();
        for (int nb_done_sim = 0; nb_done_sim < nb_simmulations; nb_done_sim++)
        {
            int ind_chosen_move;
            if (nb_done_sim < moves.Length)
            { // at least one time of each
                ind_chosen_move = nb_done_sim;
            }
            else
            {
                ind_chosen_move = get_best_move_index(c_val, nb_exploration, nb_done_sim);
            }

            if (nb_done_sim == moves.Length)
            {
                var milliseconds_per_simulation = timer.MillisecondsElapsedThisTurn / moves.Length;
                if (milliseconds_per_simulation == 0.0)
                {
                    nb_simmulations = moves.Length * 2;
                }
                else if (board.PlyCount < estimated_lenght - 10)
                {
                    var nb_coup_restant = estimated_lenght - board.PlyCount;
                    var time_per_cout = timer.MillisecondsRemaining / nb_coup_restant;
                    nb_simmulations = (int)(time_per_cout / milliseconds_per_simulation);
                }
                else
                {
                    var time_per_cout = timer.MillisecondsRemaining / 50;
                    nb_simmulations = (int)(time_per_cout / milliseconds_per_simulation); ;
                }
            }

            Move moveToPlay = moves[ind_chosen_move];

            Board new_board = Board.CreateBoardFromFEN(origin_fen); // Maybe very slow to do this
            new_board.MakeMove(moveToPlay);
            c_val[ind_chosen_move] += Simulate(new_board);
            nb_exploration[ind_chosen_move] += 1;
            //DivertedConsole.Write(nb_done_sim);
        }
        var win_proba = new List<float>(new float[moves.Length]);
        for (int i = 0; i < moves.Length; i++)
        {
            win_proba[i] = (float)c_val[i] / (float)(nb_exploration[i]);
            // DivertedConsole.Write(nb_exploration[i]+":"+win_proba[i]);
        }
        float maxValue = win_proba.Max();
        // DivertedConsole.Write("max value:"+maxValue);
        //int maxIndex = win_proba.ToList().IndexOf(maxValue);
        int maxIndex = win_proba.IndexOf(maxValue);

        return moves[maxIndex];
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

}