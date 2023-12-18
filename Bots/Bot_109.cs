namespace auto_Bot_109;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_109 : IChessBot
{
    private Dictionary<PieceType, int> pieceValues;

    public Bot_109()
    {
        // Populate piece costs
        pieceValues = new Dictionary<PieceType, int>();
        pieceValues.Add(PieceType.King, 10000);
        pieceValues.Add(PieceType.Queen, 500);
        pieceValues.Add(PieceType.Knight, 150);
        pieceValues.Add(PieceType.Rook, 100);
        pieceValues.Add(PieceType.Bishop, 100);
        pieceValues.Add(PieceType.Pawn, 10);
    }

    private int dangerScore(Board board)
    {
        int dangerScore = 0;

        // Iterate over the board and look for friendly pieces
        for (char i = 'a'; i <= 'h'; i++)
        {
            for (char j = '1'; j <= '8'; j++)
            {
                Square s = new Square(i.ToString() + j);
                Piece p = board.GetPiece(s);
                if (p.PieceType != PieceType.None)
                {
                    // Determine if the piece is ours
                    if (board.IsWhiteToMove == p.IsWhite)
                    {
                        // Add appropriate danger score
                        if (board.SquareIsAttackedByOpponent(s))
                        {
                            dangerScore += pieceValues[p.PieceType];
                        }
                    }
                }
            }
        }

        return dangerScore;
    }

    private int recursiveMoveMinMax(Board board, int depth, bool min, int alpha, int beta)
    {
        Move[] legalMoves = board.GetLegalMoves();
        int[] moveRankings = new int[legalMoves.Length];
        int currentHighest = 0;
        int currentLowest = 0;

        // Recursive bit
        if (depth <= 2)
        {
            for (int i = 0; i < legalMoves.Length; i++)
            {
                board.MakeMove(legalMoves[i]);
                moveRankings[i] = recursiveMoveMinMax(board, depth + 1, !min, alpha, beta);
                board.UndoMove(legalMoves[i]);

                if (moveRankings[i] < moveRankings[currentLowest])
                {
                    currentLowest = i;
                }

                if (moveRankings[i] > moveRankings[currentHighest])
                {
                    currentHighest = i;
                }

                if (min)
                {
                    beta = Math.Min(beta, moveRankings[currentLowest]);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
                else
                {
                    alpha = Math.Max(alpha, moveRankings[currentHighest]);
                    if (beta <= alpha)
                    {
                        break;
                    }
                }
            }
        }
        // Base case
        // Only check danger scores of immediate children
        else
        {
            for (int i = 0; i < legalMoves.Length; i++)
            {
                board.MakeMove(legalMoves[i]);
                moveRankings[i] = dangerScore(board);

                if (moveRankings[i] < moveRankings[currentLowest])
                {
                    currentLowest = i;
                }

                if (moveRankings[i] > moveRankings[currentHighest])
                {
                    currentHighest = i;
                }

                board.UndoMove(legalMoves[i]);
            }
        }

        if (min)
            return currentLowest;
        else
            return currentHighest;
    }

    private Move moveMinMax(Board board)
    {
        Move[] legalMoves = board.GetLegalMoves();
        int[] moveRankings = new int[legalMoves.Length];

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            moveRankings[i] = recursiveMoveMinMax(board, 1, false, int.MinValue, int.MaxValue);
            board.UndoMove(legalMoves[i]);
        }

        Move bestMove = legalMoves[0];
        int bestScore = moveRankings[0];

        for (int i = 1; i < legalMoves.Length; i++)
        {
            if (moveRankings[i] < bestScore)
            {
                bestMove = legalMoves[i];
                bestScore = moveRankings[i];
            }
        }

        return bestMove;
    }

    public Move Think(Board board, Timer timer)
    {
        return moveMinMax(board);
    }
}