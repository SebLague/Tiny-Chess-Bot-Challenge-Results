namespace auto_Bot_267;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_267 : IChessBot
{
    Dictionary<string, int> pieceValues = new()
    {
        {"Null", 0},
        {"Black Pawn", -100},
        {"White Pawn", 100},
        {"Black Bishop", -300},
        {"White Bishop", 300},
        {"Black Knight", -325},
        {"White Knight", 325},
        {"Black Rook", -500},
        {"White Rook", 500},
        {"Black Queen", -900},
        {"White Queen", 900},
        {"Black King", -10000},
        {"White King", 10000}
    };
    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        Move bestMove = ProduceMoveFromMinimax(5, board, isWhite);
        return bestMove;
    }
    /* OTHER FUNCTIONS START HERE */
    int EvaluateBoard(Board board)
    {
        int value = 0;
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                Square square = new((file * 8) + rank);
                string pieceName = board.GetPiece(square).ToString();
                value += pieceValues[pieceName];
                if (board.IsInCheckmate())
                {
                    value += board.IsWhiteToMove ? 99999 : -99999;
                }
            }
        }
        return value;
    }
    int Minimax(int depth, Board board, int alpha, int beta, bool isMaximizingPlayer)
    {
        Move[] gameMoves = board.GetLegalMoves();
        if (depth == 0)
        {
            return -EvaluateBoard(board);
        }

        if (isMaximizingPlayer)
        {
            int localBestMove = -99999;
            for (int i = 0; i < gameMoves.Length; i++)
            {
                board.MakeMove(gameMoves[i]);
                localBestMove = Math.Max(localBestMove, Minimax(depth - 1, board, alpha, beta, !isMaximizingPlayer));
                board.UndoMove(gameMoves[i]);
                alpha = Math.Max(alpha, localBestMove);
                if (beta <= alpha)
                {
                    return localBestMove;
                }
            }
            return localBestMove;
        }
        else
        {
            int localBestMove = 99999;
            for (int i = 0; i < gameMoves.Length; i++)
            {
                board.MakeMove(gameMoves[i]);
                localBestMove = Math.Min(localBestMove, Minimax(depth - 1, board, alpha, beta, !isMaximizingPlayer));
                board.UndoMove(gameMoves[i]);
                beta = Math.Min(alpha, localBestMove);
                if (beta <= alpha)
                {
                    return localBestMove;
                }
            }
            return localBestMove;
        }
    }
    Move ProduceMoveFromMinimax(int depth, Board board, bool isMaximizingPlayer)
    {
        Move[] gameMoves = board.GetLegalMoves();
        int localBestMove = -99999;
        Move bestMove = gameMoves[0];

        for (int i = 0; i < gameMoves.Length; i++)
        {
            board.MakeMove(gameMoves[i]);
            int value = Minimax(depth - 1, board, board.IsWhiteToMove ? -100000 : 100000, board.IsWhiteToMove ? 100000 : 100000, !isMaximizingPlayer);
            board.UndoMove(gameMoves[i]);
            if (value >= localBestMove)
            {
                localBestMove = value;
                bestMove = gameMoves[i];
            }
        }
        return bestMove;
    }
}