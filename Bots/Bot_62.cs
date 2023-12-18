namespace auto_Bot_62;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_62 : IChessBot
{
    readonly Random random = new();

    // Max depth for Minimax algorithm
    readonly int maxDepth = 3;

    // Some openings for bot to choose from for opening game
    readonly List<string[]> openingBook = new()
    {
        new string[] { "e2e4", "c7c5", "g1f3", "d7d6", "d2d4", "c5d4", "f3d4" }, // Sicilian Defence
        new string[] { "e2e4", "e7e5", "g1f3", "c8c6", "f1b5", "a7a6", "b5a4" }, // Ruy Lopez Opening
        new string[] { "d2d4", "g8f6", "c2c4", "e7e6", "g1f3", "d7d5", "b1c3" }, // Queen's Gambit Declined
        new string[] { "d2d4", "d7d5", "c2c4", "d5c4", "g1f3", "c8g4", "d1a4" }, // Queen's Gambit Accepted
        new string[] { "g1f3", "g8f6", "d2d4", "d7d5", "c2c4", "c7c6", "b1c3" }, // Slav Defence
        new string[] { "g2g3", "d7d5", "g1f3", "g8f6", "f1g2", "c7c6", "e1g1" }, // Kings's Indian Attack
        new string[] { "a2a4", "e7e5", "g2g3", "d7d5", "f1g2", "f7f5", "c2c3" }, // Ware Opening
        new string[] { "b1a3", "d7d5", "c2c3", "g8f6", "d2d4", "c8f5", "g1f3" }, // Sodium Attack
    };

    // Store the bot randomly selected opening
    string[] Openings { set; get; }

    public Bot_62()
    {
        Openings = openingBook[random.Next(openingBook.Count)];
    }

    public Move Think(Board board, Timer timer)
    {
        // If bot is playing black and white plays an opening book move from openingBook, then have black follow opening
        if (!board.IsWhiteToMove && board.PlyCount < Openings.Length)
        {
            string[]? opening = openingBook.Find(opening => opening[board.PlyCount - 1] == board.GameMoveHistory[board.PlyCount - 1].ToString().Split("'")[1]);
            if (opening != null)
                Openings = opening;
        }

        Span<Move> legalMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref legalMoves);

        // Finish playing all legal book moves before having bot evaluate moves
        if (board.PlyCount < Openings.Length)
        {
            // Check if move is legal
            Move openingMoveToPlay = new(Openings[board.PlyCount], board);
            if (legalMoves.Contains(openingMoveToPlay))
                return openingMoveToPlay;
        }

        // Finding best move to play using Minimax algorithm
        int bestScore = int.MinValue;
        Move bestMove = Move.NullMove;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            int score = Minimax(maxDepth, int.MinValue + 1, int.MaxValue - 1, false, board);

            // Skip drawing moves
            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    int Minimax(int depth, int alpha, int beta, bool maximizingPlayer, Board board)
    {
        if (depth == 0 || board.IsInCheckmate())
            return EvaluatePosition(board);

        int bestEval = maximizingPlayer ? int.MinValue + 1 : int.MaxValue - 1;
        Span<Move> legalMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref legalMoves);

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);

            // Skip drawing moves
            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            int eval = Minimax(depth - 1, alpha, beta, !maximizingPlayer, board);
            bestEval = maximizingPlayer ? Math.Max(bestEval, eval) : Math.Min(bestEval, eval);

            board.UndoMove(move);

            if (maximizingPlayer)
            {
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            else
            {
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
        }

        return bestEval;
    }

    // Get rough evaluation based on number of pieces on the board
    int EvaluatePosition(Board board)
    {
        int evaluation = 0;
        bool isWhite = board.IsWhiteToMove;

        ulong botPieces = isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        ulong opponentPieces = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

        evaluation += CountBits(botPieces & board.GetPieceBitboard(PieceType.Pawn, isWhite)) * 100;
        evaluation += CountBits(botPieces & board.GetPieceBitboard(PieceType.Knight, isWhite)) * 220;
        evaluation += CountBits(botPieces & board.GetPieceBitboard(PieceType.Bishop, isWhite)) * 225;
        evaluation += CountBits(botPieces & board.GetPieceBitboard(PieceType.Rook, isWhite)) * 500;
        evaluation += CountBits(botPieces & board.GetPieceBitboard(PieceType.Queen, isWhite)) * 900;

        evaluation -= CountBits(opponentPieces & board.GetPieceBitboard(PieceType.Pawn, !isWhite)) * 100;
        evaluation -= CountBits(opponentPieces & board.GetPieceBitboard(PieceType.Knight, !isWhite)) * 220;
        evaluation -= CountBits(opponentPieces & board.GetPieceBitboard(PieceType.Bishop, !isWhite)) * 225;
        evaluation -= CountBits(opponentPieces & board.GetPieceBitboard(PieceType.Rook, !isWhite)) * 500;
        evaluation -= CountBits(opponentPieces & board.GetPieceBitboard(PieceType.Queen, !isWhite)) * 900;

        return evaluation;
    }

    // Helper function to count the number of pieces from its bitboard
    int CountBits(ulong bits)
    {
        int count = 0;
        while (bits != 0)
        {
            count++;
            bits &= bits - 1;
        }
        return count;
    }
}