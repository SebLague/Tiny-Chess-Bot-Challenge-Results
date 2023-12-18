namespace auto_Bot_403;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
/* 
*  It's not the best but it beats me every time :p
*  It uses the minimax algorithm with alpha-beta pruning and a sketchy transposition table
*  I hope the other bots can show it who's the boss hehe
*  
*  I love your content and I hope people have made some awesome bots for this challenge
*  Keep up the amazing work!
*  Greetings from Germany <3
*  - Bobinou :)
*/
public class Bot_403 : IChessBot
{
    int maxDepth = 4;

    // I don't know how to check the MB size of the bot so feel free to decrease it if necessary 
    int maxTableCount = 1000000;

    Dictionary<ulong, int> transpositionTable = new Dictionary<ulong, int>();

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int randomMoveIndex = new Random().Next(allMoves.Length);
        Move bestMove = allMoves[randomMoveIndex];
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int moveValue = Minimax(board, maxDepth - 1, alpha, beta, false);
            board.UndoMove(move);

            if (moveValue > alpha)
            {
                alpha = moveValue;
                bestMove = move;
            }
        }

        if (transpositionTable.Count > maxTableCount)
        {
            List<ulong> keysToRemove = new List<ulong>();
            int count = 0;
            foreach (KeyValuePair<ulong, int> entry in transpositionTable)
            {
                keysToRemove.Add(entry.Key);
                count++;
                if (count >= 100000)
                {
                    break;
                }
            }
            foreach (ulong key in keysToRemove)
            {
                transpositionTable.Remove(key);
            }
        }
        return bestMove;
    }

    int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
    {
        ulong zobristKey = board.ZobristKey;

        if (transpositionTable.ContainsKey(zobristKey))
        {
            return transpositionTable[zobristKey];
        }

        if (depth == 0)
        {
            int score = EvaluatePosition(board);
            if (transpositionTable.Count < maxTableCount) transpositionTable[zobristKey] = score;
            return score;
        }

        Move[] allMoves = board.GetLegalMoves();

        if (isMaximizingPlayer)
        {
            int bestValue = int.MinValue;
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int value = Minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                bestValue = Math.Max(bestValue, value);
                alpha = Math.Max(alpha, bestValue);
                if (beta <= alpha)
                {
                    break;
                }
            }
            if (transpositionTable.Count < maxTableCount) transpositionTable[zobristKey] = bestValue;
            return bestValue;
        }
        else
        {
            int bestValue = int.MaxValue;
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int value = Minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                bestValue = Math.Min(bestValue, value);
                beta = Math.Min(beta, bestValue);
                if (beta <= alpha)
                {
                    break;
                }
            }
            if (transpositionTable.Count < maxTableCount) transpositionTable[zobristKey] = bestValue;
            return bestValue;
        }
    }

    int EvaluatePosition(Board board)
    {
        int score = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Piece piece = board.GetPiece(new Square(i, j));
                int value = GetPieceValue(piece);
                score += (board.IsWhiteToMove == piece.IsWhite ? value : -value);
            }
        }
        return score;
    }

    int GetPieceValue(Piece piece)
    {
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                return 1;
            case PieceType.Knight:
            case PieceType.Bishop:
                return 5;
            case PieceType.Rook:
                return 10;
            case PieceType.Queen:
                return 50;
            case PieceType.King:
                return int.MaxValue;
            default:
                return 0;
        }
    }
}