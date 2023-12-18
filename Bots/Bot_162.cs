namespace auto_Bot_162;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_162 : IChessBot
{
    // Transposition table to store position evaluations
    private Dictionary<long, double> transpositionTable = new Dictionary<long, double>();
    // Keep track of memory to avoid exceeding the memory limit (keep it to a safe 200MB)
    private long currentMemoryUsage = 0;
    private const long maxMemoryUsage = 200 * 1024 * 1024; // 200MB as bytes (MB => KB => B)

    private double Sigmoid(double x)
    {
        return 1.0 / (1.0 + Math.Exp(-x));
    }

    // Bonuses to encourage typically good positions (e.g. active pieces and a safe king)

    // Bonus for maximizing the euclidian distance from the center (this results in a circle)
    // Also, use sigmoid to constrain the bonus into the range [0,1]
    private double EdgeBonus(Square square)
    {
        return Sigmoid(Math.Sqrt(Math.Pow(3.5 - square.File, 2) + Math.Pow(3.5 - square.Rank, 2)));
    }

    // Bonus for minimizing the euclidian distance from the center
    // The 1.66 value is set so that the bonus has the same value as the EdgeBonus at the edge.
    private double CenterBonus(Square square)
    {
        return 1.66 - EdgeBonus(square);
    }

    // Funtion used to encourage promotion of pawns (ranks are 0 to 7).
    // Pawns should always be pushed! In the spirit of https://www.youtube.com/watch?v=-rjXim1ufd8
    // Note that the ranks have to be inverted for black.
    private double PawnPromotionBonus(Square square, bool isWhite)
    {
        // if white
        if (isWhite)
        {
            return 0.03 * (Math.Pow(square.Rank, 2) + 10);
        }
        // else
        return 0.03 * ((Math.Pow(7 - square.Rank, 2)) + 10);
    }

    // Function to interpolate between two bonus maps based on the fraction of pieces remaining
    // E.g. the king should be more active when there are fewer pieces on the board
    // This means with a high fraction of pieces remaining, the EdgeBonus is used, 
    // and with a low fraction of pieces remaining, the CenterBonus is used
    private double Interpolate(double fraction, double start_value, double end_value)
    {
        return fraction * start_value + (1 - fraction) * end_value;
    }

    private double EvaluateBoard(Board board)
    {
        // Add your custom evaluation logic here
        double evaluation = 0.0;

        bool bot_colour = board.IsWhiteToMove; // 1 if white, 0 if black

        PieceList[] allPieces = board.GetAllPieceLists();

        // Get number of pieces for opponent
        double opponentPiecesRemaining = 0.0;
        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if ((pieceType != PieceType.None) && (pieceType != PieceType.Pawn) && (pieceType != PieceType.King))
            {
                opponentPiecesRemaining += board.GetPieceList(pieceType, !bot_colour).Count;
            }
            if (pieceType == PieceType.Pawn)
            {
                opponentPiecesRemaining += 0.5 * board.GetPieceList(pieceType, !bot_colour).Count;
            }
        }
        opponentPiecesRemaining /= 11.0; // Fraction of total number of pieces (for interpolation)

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {

                // Assign each piece a value based on its type and current position
                // Also, interpolate the position bonus based on the remaining opponent pieces
                double pieceValue = (piece.IsWhite ? 1 : -1) *
                (
                    (piece.IsPawn ? 1 * Interpolate(opponentPiecesRemaining, CenterBonus(piece.Square), PawnPromotionBonus(piece.Square, piece.IsWhite)) : 0) +
                    (piece.IsKnight ? 3 * CenterBonus(piece.Square) : 0) +
                    (piece.IsBishop ? 3 * CenterBonus(piece.Square) : 0) +
                    (piece.IsRook ? 5 * EdgeBonus(piece.Square) : 0) +
                    (piece.IsQueen ? 9 * Interpolate(opponentPiecesRemaining, 1.0, CenterBonus(piece.Square)) : 0) +
                    (piece.IsKing ? 15 * Interpolate(opponentPiecesRemaining, EdgeBonus(piece.Square), CenterBonus(piece.Square)) : 0)
                );

                evaluation += pieceValue;
            }
        }

        return evaluation;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();

        // Move ordering: Look at captures, en passant (yay!:D) and promotions first
        Array.Sort(legalMoves, (a, b) =>
        {
            if (a.IsCapture && !b.IsCapture)
                return -1;
            if (!a.IsCapture && b.IsCapture)
                return 1;
            if (a.IsEnPassant && !b.IsEnPassant)
                return -1;
            if (!a.IsEnPassant && b.IsEnPassant)
                return 1;
            if (a.IsPromotion && !b.IsPromotion)
                return -1;
            if (!a.IsPromotion && b.IsPromotion)
                return 1;
            if (a.IsCastles && !b.IsCastles)
                return -1;
            if (!a.IsCastles && b.IsCastles)
                return 1;
            return 0;
        });

        bool isMaximizing = board.IsWhiteToMove;
        double bestScore = isMaximizing ? double.MinValue : double.MaxValue;
        Move bestMove = legalMoves[0];


        // Decrease the depth for time struggles
        // Using if statements instead of switch for fewer tokens
        int depth;
        if (timer.MillisecondsRemaining < 2000) // Below 2 seconds
        {
            depth = 1;
        }
        else if (timer.MillisecondsRemaining < 5000) // Below 5s
        {
            depth = 2;
        }
        else if (timer.MillisecondsRemaining < 15000) // Below 15s
        {
            depth = 3;
        }
        else if (timer.MillisecondsRemaining < 45000) // Below 45s
        {
            depth = 4;
        }
        else
        {
            depth = 5; // Default
        }

        foreach (Move move in legalMoves)
        {
            // Apply the move to the current board
            board.MakeMove(move);

            // Recursively evaluate the position using minimax
            double score = MiniMax(board, depth - 1, double.MinValue, double.MaxValue, !isMaximizing);

            // Undo the move to restore the original board state
            board.UndoMove(move);

            // Update the best move and score based on whether we're maximizing or minimizing
            if ((isMaximizing && score > bestScore) || (!isMaximizing && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private double MiniMax(Board board, int depth, double alpha, double beta, bool isMaximizing)
    {

        // Check the transposition table for the current board position
        long hash = (long)board.ZobristKey;
        if (transpositionTable.ContainsKey(hash))
        {
            return transpositionTable[hash];
        }

        // Termination Case 1: Checkmate
        if (board.IsInCheckmate())
        {
            return isMaximizing ? double.MinValue : double.MaxValue;
        }

        // Termination Case 2: Draw
        if (board.IsDraw())
        {
            return 0;
        }

        // Not Terminated => Evaluate Material
        if (depth == 0)
        {
            return EvaluateBoard(board);
        }

        Move[] legalMoves = board.GetLegalMoves();

        double bestScore = isMaximizing ? double.MinValue : double.MaxValue;

        foreach (Move move in legalMoves)
        {
            // Apply the move to the current board
            board.MakeMove(move);

            // Recursively evaluate the position using minimax with alpha-beta pruning
            double score = MiniMax(board, depth - 1, alpha, beta, !isMaximizing);

            // Undo the move to restore the original board state
            board.UndoMove(move);

            // Update the best score and alpha/beta based on whether we're maximizing or minimizing
            if (isMaximizing)
            {
                bestScore = Math.Max(bestScore, score);
                alpha = Math.Max(alpha, bestScore);
            }
            else
            {
                bestScore = Math.Min(bestScore, score);
                beta = Math.Min(beta, bestScore);
            }

            // Use alpha-beta pruning to prune the remaining nodes in this branch
            // if the current node is worse than the best node found so far
            if (beta <= alpha)
            {
                break;
            }
        }

        // Store the best score in the transposition table
        long entrySize = sizeof(long) + sizeof(double); // Estimate size of the entry
        if (currentMemoryUsage + entrySize <= maxMemoryUsage)
        {
            transpositionTable[hash] = bestScore;
            currentMemoryUsage += entrySize;
        }

        return bestScore;
    }
}