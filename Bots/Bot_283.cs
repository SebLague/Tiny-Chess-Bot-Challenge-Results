namespace auto_Bot_283;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_283 : IChessBot
{

    private List<int> moveIndicesToCalculate = new List<int>();

    private static readonly int[] pieceValues = new int[]
    {
    0, // Placeholder for PieceType.Empty
    10, // PieceType.Pawn
    30, // PieceType.Rook
    30, // PieceType.Knight
    50, // PieceType.Bishop
    90, // PieceType.Queen
    900 // PieceType.King
    };

    private static readonly int MateValue = 100000;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int maxEval = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        int depth = 4; // adjust as needed

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }

            if (!board.IsDraw())
            {
                int eval = AlphaBeta(board, timer, depth - 1, alpha, beta, false);

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                {
                    break; // Beta cutoff
                }
            }

            board.UndoMove(move);
        }
        return bestMove;
    }

    private int AlphaBeta(Board board, Timer timer, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            return Evaluate(board, depth);
        }

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, timer, depth - 1, alpha, beta, false);
                board.UndoMove(move);

                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                {
                    break; // Beta cutoff
                }
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, timer, depth - 1, alpha, beta, true);
                board.UndoMove(move);

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                {
                    break; // Alpha cutoff
                }
            }
            return minEval;
        }
    }

    private int Evaluate(Board board, int remainingDepth)
    {
        if (board.IsInCheckmate())
        {
            return (remainingDepth + 1) * MateValue; // Mate-in-(remainingDepth + 1)
        }
        else if (board.IsInStalemate() || board.IsRepeatedPosition())
        {
            return int.MinValue; // Stalemate
        }

        int score = 0;

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                Square square = new Square(file, rank);
                Piece piece = board.GetPiece(square);

                int value = pieceValues[(int)piece.PieceType];

                int centralControlValue = EvaluateCentralControl(piece, square);

                value += centralControlValue;

                // Evaluate piece position
                if (board.IsWhiteToMove)
                {
                    score += piece.IsWhite ? value : -value;
                }
                else
                {
                    score += !piece.IsWhite ? value : -value;
                }
            }
        }

        return score;
    }

    private int EvaluateCentralControl(Piece piece, Square square)
    {
        int centralControlValue = 0;

        if (square.File >= 3 && square.File <= 4 && square.Rank >= 3 && square.Rank <= 4)
        {
            centralControlValue += 10;
        }
        else if (square.File >= 2 && square.File <= 5 && square.Rank >= 2 && square.Rank <= 5)
        {
            centralControlValue += 5;
        }
        else if (square.File >= 1 && square.File <= 6 && square.Rank >= 1 && square.Rank <= 6)
        {
            centralControlValue += 2;
        }

        return centralControlValue;
    }
}