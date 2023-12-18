namespace auto_Bot_117;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_117 : IChessBot
{
    private const int DefaultSearchDepth = 4;
    private int searchDepth;
    private Dictionary<ulong, (int score, Move move)> transpositionTable;

    public Bot_117()
    {
        searchDepth = DefaultSearchDepth;
        transpositionTable = new Dictionary<ulong, (int score, Move move)>();
    }

    public Move Think(Board board, Timer timer)
    {
        // Initialize the transposition table for the current search
        transpositionTable.Clear();

        // Calculate the number of moves left in the game (assuming 40 moves total)
        int totalMoves = 200;
        int movesLeft = totalMoves - (board.PlyCount);

        // Calculate the time limit for each move with some extra buffer time
        int timeLimitPerMove = (int)(timer.MillisecondsRemaining / movesLeft) - 500;

        // Perform iterative deepening with increasing depths until the time limit is almost exhausted
        int currentDepth = 1;
        Move bestMove = Move.NullMove;

        while (currentDepth <= searchDepth && timer.MillisecondsRemaining > timeLimitPerMove)
        {
            if (board.GetAllPieceLists().Length < 10)
            {
                searchDepth = 16;
            }

            var result = AlphaBetaMinimax(board, currentDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove);
            bestMove = result.move;
            currentDepth++;
        }

        return bestMove;
    }

    private (int score, Move move) AlphaBetaMinimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        // Check if the current position is already in the transposition table
        if (transpositionTable.TryGetValue(board.ZobristKey, out var ttEntry) && ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue)
        {
            // Use the stored value from the transposition table if it is at the same or deeper depth
            if (ttEntry.move != Move.NullMove && ttEntry.score != int.MinValue && depth <= 0)
            {
                return ttEntry;
            }
        }

        if (depth == 0)
        {
            // Evaluate the board position using a simple evaluation function
            int score = EvalBoard(board);
            return (score, Move.NullMove);
        }

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
            {
                return (-10000, Move.NullMove);
            }
            else
            {
                return (10000, Move.NullMove);
            }
        }

        if (board.IsDraw())
        {
            if (board.IsWhiteToMove)
            {
                return (-8000, Move.NullMove);
            }
            else
            {
                return (8000, Move.NullMove);
            }
        }

        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;

        if (maximizingPlayer)
        {
            int bestScore = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, false).score;
                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta)
                {
                    // Alpha-beta pruning
                    break;
                }
            }

            // Store the result in the transposition table
            transpositionTable[board.ZobristKey] = (bestScore, bestMove);
            return (bestScore, bestMove);
        }
        else
        {
            int bestScore = int.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int score = AlphaBetaMinimax(board, depth - 1, alpha, beta, true).score;
                board.UndoMove(move);

                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                beta = Math.Min(beta, bestScore);
                if (alpha >= beta)
                {
                    // Alpha-beta pruning
                    break;
                }
            }

            // Store the result in the transposition table
            transpositionTable[board.ZobristKey] = (bestScore, bestMove);
            return (bestScore, bestMove);
        }
    }

    private int EvalBoard(Board board)
    {
        // A simple evaluation function that considers material advantage and checks/checkmates
        const float pawnValue = 1;
        const float rookValue = 5;
        const float knightValue = 3;
        const float bishopValue = 3;
        const float queenValue = 9;
        const float kingValue = 1000; // Higher weight for the king

        float whiteEval = 0, blackEval = 0;

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, true))
        {
            whiteEval += pawnValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, true))
        {
            whiteEval += knightValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, true))
        {
            whiteEval += bishopValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, true))
        {
            whiteEval += rookValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, true))
        {
            whiteEval += queenValue;
        }

        foreach (Piece piece in board.GetPieceList(PieceType.Pawn, false))
        {
            blackEval += pawnValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Knight, false))
        {
            blackEval += knightValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Bishop, false))
        {
            blackEval += bishopValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Rook, false))
        {
            blackEval += rookValue;
        }
        foreach (Piece piece in board.GetPieceList(PieceType.Queen, false))
        {
            blackEval += queenValue;
        }

        // Add higher weight for the king when it's closer to checkmate or check
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                whiteEval += kingValue; // White wins
            else
                blackEval += kingValue; // Black wins
        }
        else if (board.IsInCheck())
        {
            if (board.IsWhiteToMove)
                whiteEval += kingValue / 2; // White has the advantage in the check position
            else
                blackEval += kingValue / 2; // Black has the advantage in the check position
        }

        return (int)(whiteEval - blackEval);
    }
}