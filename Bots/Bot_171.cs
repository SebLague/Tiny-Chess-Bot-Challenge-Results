namespace auto_Bot_171;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_171 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // Deciding depth
        int depth = 2 + (int)(Math.Log(timer.MillisecondsRemaining / 1000) / Math.Log(4));
        if (timer.MillisecondsRemaining <= 2000)
        {
            depth = 2;
        }
        if (board.GetLegalMoves().Length <= 10 || board.GetAllPieceLists().Length <= 5)
        {
            depth++;
        }
        DivertedConsole.Write($"DEPTH: {depth}, {timer.MillisecondsRemaining}");

        // Moves Pruning
        List<Move> candidates = new List<Move>();
        List<Move> rejected = new List<Move>();
        foreach (Move move in board.GetLegalMoves())
        {
            bool isCandidate = move.IsCapture || move.IsCastles || move.IsPromotion ||
                            move.TargetSquare.Index == 27 || move.TargetSquare.Index == 28 ||
                            move.TargetSquare.Index == 35 || move.TargetSquare.Index == 36;

            board.MakeMove(move);
            if (isCandidate || board.IsInCheck())
            {
                candidates.Add(move);
            }
            else
            {
                rejected.Add(move);
            }
            board.UndoMove(move);
        }

        if (candidates.Count < 14)
        {
            for (int i = 0; i < 14 && rejected.Count > 0; i++)
            {
                Move rand_move = rejected[new System.Random().Next(rejected.Count)];
                rejected.Remove(rand_move);
                candidates.Add(rand_move);
            }
        }

        // Using DeepSearch for testing each move
        Move best_move = candidates[0];
        int best_value = board.IsWhiteToMove ? int.MinValue : int.MaxValue;

        foreach (Move try_move in candidates)
        {
            board.MakeMove(try_move);
            int try_value = DeepSearch(board, depth, int.MinValue, int.MaxValue);

            if ((!board.IsWhiteToMove && try_value > best_value) || (board.IsWhiteToMove && try_value < best_value))
            {
                best_value = try_value;
                best_move = try_move;
            }

            board.UndoMove(try_move);
        }

        DivertedConsole.Write($"{best_move}, Evaluation: {best_value}");
        return best_move;

    }

    private int DeepSearch(Board board, int depth, int alpha, int beta)
    {
        // BASE CASE: checkmate
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        }

        // BASE CASE: draw
        if (board.IsDraw())
        {
            return 0;
        }

        // BASE CASE: depth = 0
        if (depth == 0)
        {
            return Evaluate(board);
        }

        // Moves Pruning
        List<Move> candidates = new List<Move>();
        List<Move> rejected = new List<Move>();

        foreach (Move move in board.GetLegalMoves())
        {
            bool isCandidate = move.IsCapture || move.IsCastles || move.IsPromotion ||
                            move.TargetSquare.Index == 27 || move.TargetSquare.Index == 28 ||
                            move.TargetSquare.Index == 35 || move.TargetSquare.Index == 36;

            board.MakeMove(move);

            if (isCandidate || board.IsInCheck())
            {
                candidates.Add(move);
            }
            else
            {
                rejected.Add(move);
            }

            board.UndoMove(move);
        }

        if (candidates.Count < 10)
        {
            for (int i = 0; i < 10 && rejected.Count > 0; i++)
            {
                Move rand_move = rejected[new System.Random().Next(rejected.Count)];
                rejected.Remove(rand_move);
                candidates.Add(rand_move);
            }
        }

        // Recursive call
        if (board.IsWhiteToMove)
        {
            int maxEval = int.MinValue;
            foreach (Move move in candidates)
            {
                board.MakeMove(move);
                maxEval = Math.Max(maxEval, DeepSearch(board, depth - 1, alpha, beta));
                board.UndoMove(move);
                alpha = Math.Max(alpha, maxEval);

                // Beta cut-off
                if (beta <= alpha)
                {
                    break;
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in candidates)
            {
                board.MakeMove(move);
                minEval = Math.Min(minEval, DeepSearch(board, depth - 1, alpha, beta));
                board.UndoMove(move);
                beta = Math.Min(beta, minEval);

                // Alpha cut-off
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minEval;
        }
    }

    int Evaluate(Board board)
    {
        int white_material = 0, black_material = 0;
        int[] values = { 0, 10, 30, 30, 50, 90, 0 };

        foreach (PieceList piece_list in board.GetAllPieceLists())
        {
            int materialValue = piece_list.Count * values[(int)piece_list.TypeOfPieceInList];

            foreach (Piece piece in piece_list)
            {
                if (2 <= piece.Square.Index % 8 && piece.Square.Index % 8 <= 5 && piece.Square.Index > 16 && piece.Square.Index < 47)
                {
                    materialValue += 5;
                }
            }

            if (piece_list.IsWhitePieceList)
            {
                white_material += materialValue;
            }
            else
            {
                black_material += materialValue;
            }
        }

        return white_material - black_material;
    }
}