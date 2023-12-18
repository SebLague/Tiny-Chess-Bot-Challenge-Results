namespace auto_Bot_606;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_606 : IChessBot
{
    /**
     * Main function to calculate next best move
     */
    public Move Think(Board board, Timer timer)
    {
        // Max depth of search
        int maxDepth = 3; // Default less than 5 seconds left
        long timeRemaining = timer.MillisecondsRemaining;

        if (timeRemaining > 20000)
        {
            maxDepth = 5; // More than 20 seconds
        }
        else if (timeRemaining > 5000)
        {
            maxDepth = 4; // Between 5 and 20 seconds
        }

        // Placeholder for best move
        Move bestMove = Move.NullMove;
        // White wants to maximise score, while black wants to minimise
        double bestValue = (board.IsWhiteToMove) ? double.MinValue : double.MaxValue;

        var legalMoves = board.GetLegalMoves(capturesOnly: false);

        foreach (var move in legalMoves)
        {
            board.MakeMove(move);
            // Maximise for white, minimise for black
            double moveValue = Minimax(board, maxDepth - 1, double.MinValue, double.MaxValue, board.IsWhiteToMove);
            board.UndoMove(move);

            if (board.IsWhiteToMove ? moveValue > bestValue : moveValue < bestValue)
            {
                bestValue = moveValue;
                bestMove = move;
            }
        }

        return bestMove;
    }

    /**
     * Minimax search tree function
     */
    double Minimax(Board board, int depth, double alpha, double beta, bool isMaximizing)
    {
        if (depth == 0) return EvaluateBoard(board);

        double bestValue = isMaximizing ? double.MinValue : double.MaxValue;
        var legalMoves = board.GetLegalMoves();
        foreach (var move in legalMoves)
        {
            board.MakeMove(move);
            double eval = Minimax(board, depth - 1, alpha, beta, !isMaximizing);
            board.UndoMove(move);

            bestValue = isMaximizing ? Math.Max(bestValue, eval) : Math.Min(bestValue, eval);

            if (isMaximizing) alpha = Math.Max(alpha, eval);
            else beta = Math.Min(beta, eval);

            if (beta <= alpha) break;
        }
        return bestValue;
    }

    // Array to hold the board evals for certain positions as white
    private static double[][] pieceEvalWhite = new double[][]
    {
        // Pawn
        new double[]
        {
            0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,
            5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0,  5.0,
            1.0,  1.0,  2.0,  3.0,  3.0,  2.0,  1.0,  1.0,
            0.5,  0.5,  1.0,  2.5,  2.5,  1.0,  0.5,  0.5,
            0.0,  0.0,  0.0,  2.0,  2.0,  0.0,  0.0,  0.0,
            0.5, -0.5, -1.0,  0.0,  0.0, -1.0, -0.5,  0.5,
            0.5,  1.0, 1.0,  -2.0, -2.0,  1.0,  1.0,  0.5,
            0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0
        },
        // Knight
        new double[]
        {
            -5.0, -4.0, -3.0, -3.0, -3.0, -3.0, -4.0, -5.0,
            -4.0, -2.0,  0.0,  0.0,  0.0,  0.0, -2.0, -4.0,
            -3.0,  0.0,  1.0,  1.5,  1.5,  1.0,  0.0, -3.0,
            -3.0,  0.5,  1.5,  2.0,  2.0,  1.5,  0.5, -3.0,
            -3.0,  0.0,  1.5,  2.0,  2.0,  1.5,  0.0, -3.0,
            -3.0,  0.5,  1.0,  1.5,  1.5,  1.0,  0.5, -3.0,
            -4.0, -2.0,  0.0,  0.5,  0.5,  0.0, -2.0, -4.0,
            -5.0, -4.0, -3.0, -3.0, -3.0, -3.0, -4.0, -5.0
        },
        // Bishop
        new double[]
        {
            -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0,
            -1.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -1.0,
            -1.0,  0.0,  0.5,  1.0,  1.0,  0.5,  0.0, -1.0,
            -1.0,  0.5,  0.5,  1.0,  1.0,  0.5,  0.5, -1.0,
            -1.0,  0.0,  1.0,  1.0,  1.0,  1.0,  0.0, -1.0,
            -1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0, -1.0,
            -1.0,  0.5,  0.0,  0.0,  0.0,  0.0,  0.5, -1.0,
            -2.0, -1.0, -1.0, -1.0, -1.0, -1.0, -1.0, -2.0
        },
        // Rook
        new double[]
        {
            0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0,
            0.5,  1.0,  1.0,  1.0,  1.0,  1.0,  1.0,  0.5,
            -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5,
            -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5,
            -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5,
            -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5,
            -0.5,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -0.5,
            0.0,   0.0, 0.0,  0.5,  0.5,  0.0,  0.0,  0.0
        },
        // Queen
        new double[]
        {
            -2.0, -1.0, -1.0, -0.5, -0.5, -1.0, -1.0, -2.0,
            -1.0,  0.0,  0.0,  0.0,  0.0,  0.0,  0.0, -1.0,
            -1.0,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -1.0,
            -0.5,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -0.5,
             0.0,  0.0,  0.5,  0.5,  0.5,  0.5,  0.0, -0.5,
            -1.0,  0.5,  0.5,  0.5,  0.5,  0.5,  0.0, -1.0,
            -1.0,  0.0,  0.5,  0.0,  0.0,  0.0,  0.0, -1.0,
            -2.0, -1.0, -1.0, -0.5, -0.5, -1.0, -1.0, -2.0
        },
        // No room for king
    };

    // Store reverse arrays for black
    private static double[][] pieceEvalBlack;
    static Bot_606()
    {
        pieceEvalBlack = new double[pieceEvalWhite.Length][];
        for (int i = 0; i < pieceEvalWhite.Length; i++)
        {
            pieceEvalBlack[i] = (double[])pieceEvalWhite[i].Clone();
            Array.Reverse(pieceEvalBlack[i]);
        }
    }

    /**
     * Determine who the board state favours
     * This function counts pieces to determine who is winning
     * A positive value favours white, while a negative value favours black
     */
    private double EvaluateBoard(Board board)
    {
        double value = 0;

        // Check checkmate
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? 99999 : -99999;

        // Check draw
        if (board.IsDraw()) return 0;

        // Reward checks
        if (board.IsInCheck()) value += board.IsWhiteToMove ? 10 : -10;

        // If no checkmate evaluate other pieces
        for (int pieceTypeInt = 1; pieceTypeInt < 6; pieceTypeInt++)
        {
            var pieceType = (PieceType)pieceTypeInt;

            // Difference in number of pieces of type between white & black
            var differenceInPieceCount =
                board.GetPieceList(pieceType, true).Count
                - board.GetPieceList(pieceType, false).Count;

            // Positional evaluation array
            var pieceEvalScore = EvaluateBitboard(board.GetPieceBitboard(pieceType, true), pieceEvalWhite[pieceTypeInt - 1])
                               - EvaluateBitboard(board.GetPieceBitboard(pieceType, false), pieceEvalBlack[pieceTypeInt - 1]);

            // Multipy by number of pieces for each side
            switch (pieceTypeInt)
            {
                case 1:
                    pieceEvalScore += differenceInPieceCount * 10;
                    break;
                case 2:
                case 3:
                    pieceEvalScore += differenceInPieceCount * 30;
                    break;
                case 4:
                    pieceEvalScore += differenceInPieceCount * 50;
                    break;
                case 5:
                    pieceEvalScore += differenceInPieceCount * 90;
                    break;
                    // No king for now
            }

            // Add to value
            value += pieceEvalScore;

        }
        return value;
    }

    /**
     * Function to calculate the strength of the position
     * of pieces of a certain type
     */
    private static double EvaluateBitboard(ulong bitboard, double[] evalArray)
    {
        double score = 0;

        while (bitboard != 0)
        {
            // Get the index of the least significant bit that's set to 1
            int index = BitOperations.TrailingZeroCount(bitboard);

            // Add the corresponding value from the evalArray
            score += evalArray[index];

            // Clear the least significant bit that's set to 1
            bitboard &= bitboard - 1;
        }

        return score;
    }

}