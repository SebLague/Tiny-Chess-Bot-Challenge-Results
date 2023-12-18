namespace auto_Bot_25;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_25 : IChessBot
{
    // Define piece values
    private readonly Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>()
    {
        { PieceType.Pawn, 1 },
        { PieceType.Knight, 3 },
        { PieceType.Bishop, 3 },
        { PieceType.Rook, 5 },
        { PieceType.Queen, 9 },
        { PieceType.King, 1000 }  // Value arbitrarily high because losing the king means losing the game
    };

    // Set maximum depth of search tree
    private const int maxDepth = 3;

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = Move.NullMove;
        double bestScore = double.NegativeInfinity;

        // Get all legal moves
        Move[] moves = board.GetLegalMoves();

        // Use Minimax algorithm to determine the best move
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            double score = Minimax(board, maxDepth, double.NegativeInfinity, double.PositiveInfinity, false);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private double Minimax(Board board, int depth, double alpha, double beta, bool isMaximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return EvaluateBoard(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (isMaximizingPlayer)
        {
            double maxEval = double.NegativeInfinity;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                maxEval = Math.Max(maxEval, Minimax(board, depth - 1, alpha, beta, false));
                board.UndoMove(move);
                alpha = Math.Max(alpha, maxEval);
                if (beta <= alpha)
                {
                    break;  // Alpha-beta pruning
                }
            }
            return maxEval;
        }
        else
        {
            double minEval = double.PositiveInfinity;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                minEval = Math.Min(minEval, Minimax(board, depth - 1, alpha, beta, true));
                board.UndoMove(move);
                beta = Math.Min(beta, minEval);
                if (beta <= alpha)
                {
                    break;  // Alpha-beta pruning
                }
            }
            return minEval;
        }
    }

    private double EvaluateBoard(Board board)
    {
        double score = 0;

        PieceList[] pieceLists = board.GetAllPieceLists();
        foreach (PieceList pieceList in pieceLists)
        {
            double pieceValue = pieceValues[pieceList.TypeOfPieceInList];
            score += pieceList.IsWhitePieceList ? pieceValue * pieceList.Count : -pieceValue * pieceList.Count;
        }

        return score;
    }
}