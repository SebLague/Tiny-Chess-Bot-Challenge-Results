namespace auto_Bot_83;
using ChessChallenge.API;
using System;

public class Bot_83 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    readonly int[,] positionTables =
    {
        // Pawn
        {
             0,  0,  0,   0,   0,  0,  0,  0,
            10, 10, 10,  10,  10, 10, 10, 10,
             2,  2,  4,   6,   6,  4,  2,  2,
             1,  1,  2,   5,   5,  2,  1,  1,
             0,  0,  0,   4,   4,  0,  0,  0,
             1, -1, -2,   0,   0, -2, -1,  1,
             1,  2,  2,  -4,  -4,  2,  2,  1,
             0,  0,  0,   0,   0,  0,  0,  0
        },

        // Knight
        {
           -10, -8, -6, -6, -6, -6, -8, -10,
            -8, -4,  0,  0,  0,  0, -4,  -8,
            -6,  0,  2,  3,  3,  2, 0,   -6,
            -6,  1,  3,  4,  4,  3, 1,   -6,
            -6,  0,  3,  4,  4,  3, 0,   -6,
            -6,  1,  2,  3,  3,  2, 1,   -6,
            -8, -4,  0,  1,  1,  0, -4,  -8,
           -10, -8, -6, -6, -6, -6, -4, -10
        },

        // Bishop
        {
           -4, -2, -2, -2, -2, -2, -2, -4,
           -2,  0,  0,  0,  0,  0,  0, -2,
           -2,  0,  1,  2,  2,  1,  0, -2,
           -2,  1,  1,  2,  2,  1,  1, -2,
           -2,  0,  2,  2,  2,  2,  0, -2,
           -2,  2,  2,  2,  2,  2,  2, -2,
           -8, -4,  0,  1,  1,  0, -4, -8,
           -4, -2, -2, -2, -2, -2, -2, -4
        },

        // Rook
        {
            0, 0, 0, 0, 0, 0, 0,  0,
            1, 2, 2, 2, 2, 2, 2,  1,
           -1, 0, 0, 0, 0, 0, 0, -1,
           -1, 0, 0, 0, 0, 0, 0, -1,
           -1, 0, 0, 0, 0, 0, 0, -1,
           -1, 0, 0, 0, 0, 0, 0, -1,
           -1, 0, 0, 0, 0, 0, 0, -1,
            0, 0, 0, 1, 1, 0, 0,  0
        },

        // Qeen
        {
            -4, -2, -2, -10, -10, -2, -2, -4,
            -2,  0,  0,   0,   0,  0,  0, -2,
            -2,  0,  1,   1,   1,  1,  0, -2,
            -1,  0,  1,   1,   1,  1,  0, -1,
             0,  0,  1,   1,   1,  1,  0, -1,
            -2,  1,  1,   1,   1,  1,  0, -2,
            -2,  0,  1,   0,   0,  0,  0, -2,
            -4, -2, -2,  -1,  -1, -2, -2, -4
        },

        // King
        {
            -3, -4, -4, -5, -5, -4, -4, -3,
            -3, -4, -4, -5, -5, -4, -4, -3,
            -3, -4, -4, -5, -5, -4, -4, -3,
            -3, -4, -4, -5, -5, -4, -4, -3,
            -2, -3, -3, -4, -4, -3, -3, -2,
            -1, -2, -2, -2, -2, -2, -2, -1,
             2,  2,  0,  0,  0,  0,  2,  2,
             2,  3,  1,  0,  0,  1,  3,  2
        }
    };


    Move[] moveHistory = new Move[4];
    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();

        Move move = ComputeBestMove(board, board.IsWhiteToMove, 2).Item1;

        // If the move is already been played chose another randomly
        for (int i = 0; i < moveHistory.Length; i++)
        {
            if (move == moveHistory[i])
            {
                move = legalMoves[rng.Next(legalMoves.Length)];
            }
        }

        // Add move to history
        for (int i = 1; i < moveHistory.Length; i++)
        {
            moveHistory[i] = moveHistory[i - 1];
        }
        moveHistory[0] = move; ;

        return move;
    }


    private (Move, int) ComputeBestMove(Board board, bool maximize, int depth)
    {
        int captureValue, eval, bestEval = maximize ? int.MinValue : int.MaxValue;
        Move bestMove = Move.NullMove;
        Move[] legalMoves = board.GetLegalMoves();

        // Return early if there are no valid moves
        if (legalMoves.Length == 0)
            return (bestMove, bestEval);

        // Recursivelly evaluate each move
        foreach (Move move in legalMoves)
        {
            // Incentivate piece capture
            captureValue = pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
            eval = maximize ? captureValue : -captureValue;

            board.MakeMove(move);

            // Check for check mate
            if (board.IsInCheckmate())
                eval += maximize ? -100000 : 100000;

            if (depth > 0)
                eval += ComputeBestMove(board, !maximize, depth - 1).Item2;
            else
                eval += Evaluate(board);

            if (maximize && eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
            else if (!maximize && eval < bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }

            board.UndoMove(move);
        }

        return (bestMove, bestEval);
    }


    private int Evaluate(Board board)
    {
        int eval = 0;

        // Add piece values
        for (int i = (int)PieceType.Pawn; i < (int)PieceType.King; i++)
        {
            PieceList white = board.GetPieceList((PieceType)i, true);
            PieceList black = board.GetPieceList((PieceType)i, true);

            for (int j = 0; j < white.Count; j++)
                eval += pieceValues[i] + positionTables[i - 1, white[j].Square.Index] * 4;

            for (int j = 0; j < black.Count; j++)
                eval -= pieceValues[i] + positionTables[i - 1, 63 - black[j].Square.Index] * 4;
        }

        return eval;
    }
}