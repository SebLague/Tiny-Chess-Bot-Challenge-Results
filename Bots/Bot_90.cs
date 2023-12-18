namespace auto_Bot_90;
using ChessChallenge.API;
using System;

public class Bot_90 : IChessBot
{
    private Random random;
    private const int MaxDepth = 3; // Depth for minimax search

    public Bot_90()
    {
        random = new Random();
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        // Check if there are any legal moves
        if (moves.Length == 0)
        {
            // If no legal moves, return a null move
            return Move.NullMove;
        }

        int bestScore = int.MinValue;
        Move bestMove = moves[0];

        // Apply minimax algorithm with alpha-beta pruning
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = Minimax(board, MaxDepth, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            return EvaluateBoard(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (isMaximizingPlayer)
        {
            int maxScore = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = Minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                maxScore = Math.Max(maxScore, score);
                alpha = Math.Max(alpha, score);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return maxScore;
        }
        else
        {
            int minScore = int.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = Minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                minScore = Math.Min(minScore, score);
                beta = Math.Min(beta, score);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minScore;
        }
    }

    // Evaluate the board position with a focus on checkmating the opponent
    private int EvaluateBoard(Board board)
    {
        // Check if the opponent is in checkmate
        if (board.IsInCheckmate())
        {
            return int.MaxValue; // Max score for checkmate
        }

        // Check if the bot is in checkmate (should be avoided)
        if (board.IsInCheckmate())
        {
            return int.MinValue; // Min score for being in checkmate
        }

        // Calculate the material value of the board
        int materialValue = GetMaterialValue(board);

        // Calculate the positional advantage of the board
        int positionalAdvantage = GetPositionalAdvantage(board);

        // Bonus for putting the opponent in check
        int checkBonus = GetCheckBonus(board);

        // Combine the different components of the evaluation function
        return materialValue + positionalAdvantage + checkBonus;
    }

    // Helper methods to calculate material value, positional advantage, and check bonus
    private int GetMaterialValue(Board board)
    {
        int materialValueWhite = 0;
        int materialValueBlack = 0;

        // Assign material values for each piece type
        const int pawnValue = 100;
        const int knightValue = 300;
        const int bishopValue = 300;
        const int rookValue = 500;
        const int queenValue = 900;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                int pieceValue = 0;

                if (piece.IsWhite)
                {
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            pieceValue = pawnValue;
                            break;
                        case PieceType.Knight:
                            pieceValue = knightValue;
                            break;
                        case PieceType.Bishop:
                            pieceValue = bishopValue;
                            break;
                        case PieceType.Rook:
                            pieceValue = rookValue;
                            break;
                        case PieceType.Queen:
                            pieceValue = queenValue;
                            break;
                    }
                    materialValueWhite += pieceValue;
                }
                else
                {
                    // Black pieces
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            pieceValue = pawnValue;
                            break;
                        case PieceType.Knight:
                            pieceValue = knightValue;
                            break;
                        case PieceType.Bishop:
                            pieceValue = bishopValue;
                            break;
                        case PieceType.Rook:
                            pieceValue = rookValue;
                            break;
                        case PieceType.Queen:
                            pieceValue = queenValue;
                            break;
                    }
                    materialValueBlack += pieceValue;
                }
            }
        }

        return materialValueWhite - materialValueBlack;
    }

    private int GetPositionalAdvantage(Board board)
    {
        // Calculate the positional advantage of the pieces on the board
        // In this placeholder implementation, we assume that white has a positional advantage
        // based on board control and centralization of pieces.
        int positionalAdvantageWhite = 100;
        int positionalAdvantageBlack = 0;
        return positionalAdvantageWhite - positionalAdvantageBlack;
    }

    private int GetCheckBonus(Board board)
    {
        // Calculate a bonus score for putting the opponent in check or achieving checkmate
        int checkBonus = 0;

        // Check if the opponent is in check
        if (board.IsInCheck())
        {
            checkBonus += 50;
        }

        // Check if the opponent is in checkmate (max bonus)
        if (board.IsInCheckmate())
        {
            checkBonus += 500;
        }

        return checkBonus;
    }
}
