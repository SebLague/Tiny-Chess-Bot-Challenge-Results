namespace auto_Bot_318;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_318 : IChessBot
{
    Dictionary<ulong, double> Transpositions = new Dictionary<ulong, double>(); // Transpositions Dictionary
    Dictionary<Move, double> MovesEvaluations = new Dictionary<Move, double>(); // Previous Move Evaluation Dictionary

    public int Calculate_Material_per_Side(PieceList[] Pieces, bool white)
    {
        int pieces_value = 0;
        foreach (PieceList piece in Pieces)
        {
            int value_of_piece = piece.TypeOfPieceInList switch
            {
                PieceType.Pawn => 1,
                PieceType.Knight or PieceType.Bishop => 3,
                PieceType.Rook => 5,
                PieceType.Queen => 9,
                _ => 0,
            };
            pieces_value += (piece.IsWhitePieceList == white ? 1 : 0) * value_of_piece * piece.Count;
        }
        return pieces_value;
    }
    public int Calculate_Material(PieceList[] Pieces, bool white) //Calculates the Material Difference
    {
        return Calculate_Material_per_Side(Pieces, white) - Calculate_Material_per_Side(Pieces, !white);
    }

    public double King_Position_Evaluation(Board board)
    {
        bool white = board.IsWhiteToMove;
        Square my_king_square = board.GetKingSquare(white);
        Square his_king_square = board.GetKingSquare(!white);
        double my_king_eval = EvaluateKingSquare(my_king_square);
        double his_king_eval = EvaluateKingSquare(his_king_square);
        if (Calculate_Material_per_Side(board.GetAllPieceLists(), !white) < 18)
            return his_king_eval - my_king_eval;
        return my_king_eval - his_king_eval;

    }

    private double EvaluateKingSquare(Square kingSquare)
    {
        double king_eval = kingSquare.Rank switch
        {
            0 or 7 => 0.1,
            1 or 6 => -0.4,
            2 or 5 => -1.5,
            3 or 4 => -3.5,
        };

        king_eval += kingSquare.File switch
        {
            0 or 1 or 6 or 7 => 0.2,
            2 => 0.1,
            3 or 4 => -0.1,
            5 => -0.2,
        };

        return king_eval;
    }

    public double Normal_Pieces_Evaluation(Board board)
    {
        bool white = board.IsWhiteToMove;
        double my_pieces_eval = EvaluatePieceList(board.GetPieceList(PieceType.Knight, white)) +
            EvaluatePieceList(board.GetPieceList(PieceType.Bishop, white)) +
            EvaluatePieceList(board.GetPieceList(PieceType.Rook, white));
        double his_pieces_eval = EvaluatePieceList(board.GetPieceList(PieceType.Knight, !white)) +
            EvaluatePieceList(board.GetPieceList(PieceType.Bishop, !white)) +
            EvaluatePieceList(board.GetPieceList(PieceType.Rook, !white));
        return my_pieces_eval - his_pieces_eval;
    }

    private double EvaluatePieceList(PieceList pieceList)
    {
        return pieceList.Sum(piece => piece.Square.Rank > 1 && piece.Square.Rank < 6 ? 0.2 : 0);
    }

    public double Pawn_Structure_Evaluation(Board board)
    {
        bool white = board.IsWhiteToMove;
        double pawn_eval_score = 0;
        Square[] central_positions = new Square[]
        {
            new Square("e4"), new Square("e5"),
            new Square("d4"), new Square("d5"),
            new Square("c4"), new Square("c5")
        };

        pawn_eval_score = central_positions.Sum(square =>
        {
            Piece piece = board.GetPiece(square);
            return piece != null && piece.PieceType == PieceType.Pawn && piece.IsWhite == white
                ? 0.4
                : piece != null && piece.PieceType == PieceType.Pawn && piece.IsWhite != white
                ? -0.4
                : 0;
        });

        return pawn_eval_score;
    }

    public double Evaluate_Position(Board board)
    {
        if (board.IsInCheckmate())
            return -999;
        bool white = board.IsWhiteToMove;
        PieceList[] Pieces = board.GetAllPieceLists();
        return Calculate_Material(Pieces, white) + Pawn_Structure_Evaluation(board) +
            King_Position_Evaluation(board) + Normal_Pieces_Evaluation(board);
    }

    public double AlphaBetaWithTT(Board board, int depth, double alpha, double beta, bool player_is_bot)
    {
        if (depth <= 0)
            return Evaluate_Position(board);

        if (Transpositions.ContainsKey(board.ZobristKey))
            return Transpositions[board.ZobristKey];

        if (board.IsDraw()) //Prefer to lose than make a boring draw, also helps with no repetion draws in better position
            return -2;

        Move[] moves = board.GetLegalMoves();
        double maxEval = player_is_bot ? double.MinValue : double.MaxValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            double evaluation = AlphaBetaWithTT(board, depth - 1, alpha, beta, !player_is_bot);
            board.UndoMove(move);

            if (player_is_bot)
            {
                maxEval = Math.Max(maxEval, evaluation);
                alpha = Math.Max(alpha, evaluation);
            }
            else
            {
                maxEval = Math.Min(maxEval, evaluation);
                beta = Math.Min(beta, evaluation);
            }

            if (beta <= alpha)
                break;
        }

        return maxEval;
    }
    private Move[] SortMovesByPreviousEvaluation(Board board, Move[] moves, int previousDepth)
    {
        // Sort the moves based on their evaluation at the previous depth
        if (previousDepth > 0)
            return moves.OrderByDescending(move => MovesEvaluations[move]).ToArray();
        return moves;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 1)
            return moves[0];
        Move bestMove = new Move();
        int depth = 1;
        double max_Eval = double.MinValue;
        int time_to_move = timer.MillisecondsRemaining;
        bool move_found = false;
        int moves_in_opening_played_faster = board.PlyCount < 30 ? 160 : 50;
        while (!move_found)
        {
            Transpositions.Clear();
            double maxEval_Depth = double.MinValue;
            Move current_bestMove = new Move();

            Move[] sortedMoves = SortMovesByPreviousEvaluation(board, moves, depth - 2);
            MovesEvaluations.Clear();
            foreach (Move move in sortedMoves)
            {
                board.MakeMove(move);
                double moveEval = AlphaBetaWithTT(board, depth, double.MinValue, double.MaxValue, false);
                board.UndoMove(move);
                MovesEvaluations.Add(move, moveEval);
                if (moveEval > maxEval_Depth)
                {
                    current_bestMove = move;
                    maxEval_Depth = moveEval;
                }

                if (timer.MillisecondsElapsedThisTurn > time_to_move / moves_in_opening_played_faster)
                {
                    move_found = true;
                    break;
                }

            }
            if (!move_found)
                bestMove = current_bestMove;
            else if (maxEval_Depth + 0.5 > max_Eval)// Prefer to use the a move of depth 5 with 0.4 Evaluation instead of a move of
            {
                bestMove = current_bestMove;
                max_Eval = maxEval_Depth;
            }
            depth += 2;

        }

        return bestMove;
    }
}