namespace auto_Bot_113;
using ChessChallenge.API;
using System;

public class Bot_113 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        // I was going to document my code but I realised I was sacrificing my sleep time

        int depth = 3;

        if (board.IsInCheck())
        {
            depth++;
        }

        Move move = Compute(board, depth);

        if (timer.MillisecondsRemaining > 0)
        {
            return move;
        }

        else return Move.NullMove;

    }

    public Move Compute(Board board, int depth)
    {

        Move[] moves = board.GetLegalMoves();

        Random rng = new();
        Move best_move = moves[rng.Next(moves.Length)];

        int side = (board.IsWhiteToMove) ? 1 : -1;
        long eval = -side * 100000;
        long temp_eval = 0;

        for (int i = 0; i < moves.Length; i++)
        {

            board.MakeMove(moves[i]);

            if (board.IsInCheckmate())
            {
                eval = side * 100000;
                best_move = moves[i];
            }

            else if (board.IsDraw())
            {
                temp_eval = 0;
            }

            else
            {
                if (moves[i].IsPromotion)
                {
                    temp_eval = Evaluate(board, depth, -100000, 100000);
                }

                else
                {
                    temp_eval = Evaluate(board, depth - 1, -100000, 100000);
                }
            }

            if (side * temp_eval > side * eval)
            {
                best_move = moves[i];
                eval = temp_eval;
            }

            board.UndoMove(moves[i]);

            if (board.IsInCheckmate())
            {
                break;
            }

        }

        return best_move;

    }

    public int Evaluate(Board board, int depth, int alpha, int beta)
    {

        Move[] moves = board.GetLegalMoves();

        int side = (board.IsWhiteToMove) ? 1 : -1;
        int eval = (depth >= 1) ? (-side * 100000) : 0;
        int temp_eval;

        if (depth >= 1)
        {
            for (int i = 0; i < moves.Length; i++)
            {

                board.MakeMove(moves[i]);

                if (board.IsInCheckmate())
                {
                    eval = side * 100000;
                    board.UndoMove(moves[i]);
                    break;
                }

                else if (board.IsDraw())
                {
                    temp_eval = 0;
                }

                else
                {
                    temp_eval = Evaluate(board, depth - 1, alpha, beta);
                }

                if (side * temp_eval > side * eval)
                {
                    eval = temp_eval;
                }

                if (side == 1)
                {

                    if (eval > beta)
                    {
                        board.UndoMove(moves[i]);
                        return eval;
                    }

                    if (eval > alpha)
                    {
                        alpha = eval;
                    }

                }

                else if (side == -1)
                {

                    if (eval < alpha)
                    {
                        board.UndoMove(moves[i]);
                        return eval;
                    }

                    if (eval < beta)
                    {
                        beta = eval;
                    }

                }

                board.UndoMove(moves[i]);

            }

        }

        else
        {

            int[] piece_values = { 0, 1000, 3000, 3000, 5000, 8000, 0 };

            for (int j = 0; j < 64; j++)
            {

                Piece piece = board.GetPiece(new Square(j));
                int piece_side = (piece.IsWhite) ? 1 : -1;

                if ((int)piece.PieceType != 1 || (piece.Square.File != 0 && piece.Square.File != 7))
                {

                    if ((int)piece.PieceType == 2 && moves.Length <= 40)
                    {
                        eval += piece_side * 100;
                    }

                    else if ((int)piece.PieceType == 3 && moves.Length > 40)
                    {
                        eval += piece_side * 100;
                    }

                    eval += piece_side * piece_values[(int)piece.PieceType];
                }

                else
                {
                    eval += piece_side * 800;
                }

                if ((int)piece.PieceType == 1)
                {

                    if (piece.IsWhite)
                    {
                        eval += 5 * (piece.Square.Rank - 2);
                    }

                    else
                    {
                        eval -= 5 * (7 - piece.Square.Rank);
                    }

                }

            }

            board.MakeMove(Move.NullMove);

            for (int j = 0; j < moves.Length; j++)
            {

                if ((int)moves[j].MovePieceType != 6)
                {
                    eval += side * 10;
                }

                else
                {
                    eval -= 5;
                }

            }

            Move[] opponent_moves = board.GetLegalMoves();

            for (int j = 0; j < opponent_moves.Length; j++)
            {
                if ((int)moves[j].MovePieceType != 6)
                {
                    eval -= side * 10;
                }

                else
                {
                    eval += 5;
                }
            }

            board.UndoMove(Move.NullMove);

            if (board.IsInCheck())
            {
                eval += side * 10;
            }



        }

        return eval;

    }

}