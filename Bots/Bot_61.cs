namespace auto_Bot_61;
using ChessChallenge.API;
using System;

public class Bot_61 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int depth = 4;
        int[] piece_values = { 100, 320, 330, 500, 900, 1000 };
        int[] knight_white = { -50, -40, -30, -30, -30, -30, -40, -50, -40, -20, 0, 0, 0, 0, -20, -40, -30, 0, 10, 15, 15, 10, 0, -30, -30, 5, 15, 20, 20, 15, 5, -30, -30, 0, 15, 20, 20, 15, 0, -30, -30, 5, 10, 15, 15, 10, 5, -30, -40, -20, 0, 5, 5, 0, -20, -40, -50, -40, -30, -30, -30, -30, -40, -50 };
        int[] bishop_white = { -20, -10, -10, -10, -10, -10, -10, -20, -10, 0, 0, 0, 0, 0, 0, -10, -10, 0, 5, 10, 10, 5, 0, -10, -10, 5, 5, 10, 10, 5, 5, -10, -10, 0, 10, 10, 10, 10, 0, -10, -10, 10, 10, 10, 10, 10, 10, -10, -10, 5, 0, 0, 0, 0, 5, -10, -20, -10, -10, -10, -10, -10, -10, -20 };
        int[] queen_white = { -20, -10, -10, -5, -5, -10, -10, -20, -10, 0, 0, 0, 0, 0, 0, -10, -10, 0, 5, 5, 5, 5, 0, -10, -5, 0, 5, 5, 5, 5, 0, -5, 0, 0, 5, 5, 5, 5, 0, -5, -10, 5, 5, 5, 5, 5, 0, -10, -10, 0, 5, 0, 0, 0, 0, -10, -20, -10, -10, -5, -5, -10, -10, -20 };
        int[] king_white = { -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -20, -30, -30, -40, -40, -30, -30, -20, -10, -20, -20, -20, -20, -20, -20, -10, 20, 20, 0, 0, 0, 0, 20, 20, 20, 30, 10, 0, 0, 10, 30, 20 };
        Move final_move = FindBestMove(depth);
        if (final_move.RawValue == 0)
        {
            Move[] allMoves = board.GetLegalMoves();
            Random rng = new();
            final_move = allMoves[rng.Next(allMoves.Length)];
        }
        return final_move;

        Move FindBestMove(int depth)
        {
            int best_eval = int.MinValue;
            Move best_move = new();
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = Minimax(depth - 1, false, int.MinValue, int.MaxValue);
                board.UndoMove(move);
                if (eval > best_eval)
                {
                    best_eval = eval;
                    best_move = move;
                }
            }
            return best_move;
        }

        int Minimax(int depth, bool is_maximizing_player, int alpha, int beta)
        {
            if (depth == 0)
            {
                return EvaluateBoard();
            }

            int max_eval = int.MinValue;
            int min_eval = int.MaxValue;
            int eval;

            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                eval = Minimax(depth - 1, !is_maximizing_player, alpha, beta);
                board.UndoMove(move);

                if (is_maximizing_player)
                {
                    max_eval = Math.Max(max_eval, eval);
                    alpha = Math.Max(alpha, eval);
                    if (beta <= alpha)
                        break;
                }
                else
                {
                    min_eval = Math.Min(min_eval, eval);
                    beta = Math.Min(beta, eval);
                    if (beta <= alpha)
                        break;
                }
            }

            return is_maximizing_player ? max_eval : min_eval;
        }

        int EvaluateBoard()
        {
            int WM = 0;
            int BM = 0;
            int additional_points = 0;
            int[] knight_values = board.IsWhiteToMove ? knight_white : Reverse(knight_white);
            int[] bishop_values = board.IsWhiteToMove ? bishop_white : Reverse(bishop_white);
            int[] queen_values = board.IsWhiteToMove ? queen_white : Reverse(queen_white);
            int[] king_values = board.IsWhiteToMove ? king_white : Reverse(king_white);

            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(new Square(i));
                if (!piece.IsNull)
                {
                    int piece_index = (piece.IsPawn) ? 0 : (piece.IsKnight) ? 1 : (piece.IsBishop) ? 2 : (piece.IsRook) ? 3 : (piece.IsQueen) ? 4 : 5;
                    int piece_value = piece_values[piece_index];

                    if (piece.IsWhite)
                    {
                        WM += piece_value;
                        additional_points += piece_index == 1 ? knight_values[i] : (piece_index == 2 ? bishop_values[i] : (piece_index == 4 ? queen_values[i] : king_values[i]));
                    }
                    else
                    {
                        BM += piece_value;
                        additional_points += piece_index == 1 ? knight_values[i] : (piece_index == 2 ? bishop_values[i] : (piece_index == 4 ? queen_values[i] : king_values[i]));
                    }
                }
            }

            int evaluation = WM - BM;
            int mapRange = board.IsWhiteToMove ? 1 : -1;
            return (evaluation * mapRange) + additional_points;
        }

        int[] Reverse(int[] array)
        {
            for (int i = 0; i < array.Length / 2; i++)
            {
                (array[array.Length - i - 1], array[i]) = (array[i], array[array.Length - i - 1]);
            }
            return array;
        }
    }
}