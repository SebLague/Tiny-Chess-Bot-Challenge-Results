namespace auto_Bot_95;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Bot_95 : IChessBot
{
    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        // opening
        switch (board.PlyCount, board.GetPiece(new Square("f6")).PieceType != 0, board.GetPiece(new Square("c3")).PieceType != 0)
        {
            case (0, _, _):
                return new Move("g1f3", board);
            case (1, _, true):
                return new Move("b8c6", board);
            case (2, true, _):
                return new Move("c2c4", board);
        }

        Move[] allMoves = board.GetLegalMoves();
        int[] allMoveValues = new int[allMoves.Length];

        Move bestMove = allMoves[rng.Next(allMoves.Length)]; // random choice

        for (int j = 0; j < allMoves.Length; j++)
        {
            // time limit
            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 60 /*max turn time in ms*/)
            {
                return bestMove;
            }

            allMoveValues[j] = evaluateMoveWithDepth(board, allMoves[j], 1);
        }

        bestMove = allMoves[randomIndexOfValue(allMoveValues, allMoveValues.Max())];

        return bestMove;
    }

    int randomIndexOfValue(int[] arr, int val)
    {
        var indices = new List<int> { };

        for (int i = 0; i < arr.Length; i++)
        {
            if (arr[i] == val)
            {
                indices.Add(i);
            }
        }

        return indices[rng.Next(indices.Count)];
    }

    int evaluateMoveWithDepth(Board board, Move move, int depth)
    {
        if (depth == 3 /*searchDepth*/)
        {
            return evaluateMove(board, move);
        }

        board.MakeMove(move);

        Move[] allMoves = board.GetLegalMoves();
        int[] allMoveValues = new int[allMoves.Length];

        for (int i = 0; i < allMoves.Length; i++)
        {
            //allMoveValues[i] += (int)((double)BigInteger.Pow(-1, depth % 2) * (double)evaluateMoveWithDepth(board, allMoves[i], depth + 1) * Math.Sqrt(depth)); <-- the square root falloff was a horrible idea
            allMoveValues[i] += (int)BigInteger.Pow(-1, depth % 2) * evaluateMoveWithDepth(board, allMoves[i], depth + 1);
            //                  ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^             
            //           subtract opponent's evaluation/ add bot's evaluation
        }

        board.UndoMove(move);

        // avoid error with array.min if array is empty
        if (allMoves.Length == 0)
        {
            return evaluateMove(board, move);
        }

        return allMoveValues.Min() + evaluateMove(board, move);
    }

    int evaluateMove(Board board, Move move)
    {
        int moveValue = 0;

        if (move.IsCapture)
        {
            moveValue += 100 * (int)move.CapturePieceType;

            // discurage capturing with piece of lower value
            if (move.MovePieceType > move.CapturePieceType)
            {
                moveValue -= 25 * ((int)move.MovePieceType - (int)move.CapturePieceType);
            }
        }

        board.MakeMove(move);

        // checkmate if possible
        if (board.IsInCheckmate())
        {
            moveValue += 1000000;//int.MaxValue;
        }

        // promotion possible
        if (move.IsPromotion)
        {
            moveValue += 600;
        }

        // avoid draws
        if (board.IsDraw())
        {
            moveValue = 0;
        }

        board.UndoMove(move);

        return moveValue;
    }
}