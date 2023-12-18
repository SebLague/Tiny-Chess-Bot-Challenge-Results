namespace auto_Bot_152;
// I have not written C# in any capacity in years. I have also barely ever
// played chess. So this is a mess

using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_152 : IChessBot
{
    public const int INFINITY = 999999;
    public const int MAX_DEPTH = 4;

    // 0) None (0)
    // 1) Pawn (100)
    // 2) Knight (325)
    // 3) Bishop (350)
    // 4) Rook (500)
    // 5) Queen (900)
    // 6) King (10000)
    public static int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };
    static Dictionary<ulong, int> transpositionTable = new Dictionary<ulong, int>();

    public Move Think(Board board, Timer timer)
    {
        int bestEval = -INFINITY;

        Move[] moves = getOrderedMoves(board);
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -negamax(board, -INFINITY, INFINITY, 0);
            board.UndoMove(move);


            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        return bestMove;
    }

    ///A negamax implementation derived from the following sources
    /// https://en.wikipedia.org/wiki/Negamax
    /// https://www.chessprogramming.org/Negamax
    private int negamax(Board board, int alpha, int beta, int depth)
    {
        if (depth >= MAX_DEPTH || board.IsInCheckmate() || board.IsInCheckmate())
        {
            return quiesceSearch(board, alpha, beta);
        }

        int value = -INFINITY;
        foreach (Move move in getOrderedMoves(board))
        {
            board.MakeMove(move);
            if (transpositionTable.ContainsKey(board.ZobristKey))
            {
                value = transpositionTable[board.ZobristKey];
            }
            else
            {
                value = Math.Max(value, -negamax(board, -beta, -alpha, depth + 1));
                transpositionTable[board.ZobristKey] = value;
            }
            board.UndoMove(move);
            alpha = Math.Max(alpha, value);

            if (alpha >= beta)
            {
                break; // Prune this branch, we don't need to search any more nodes.
            }
        }

        return value;
    }

    private int quiesceSearch(Board board, int alpha, int beta)
    {
        int standPat = evaluate(board);
        if (standPat >= beta)
        {
            return beta;
        }
        else if (alpha < standPat)
        {
            alpha = standPat;
        }

        Move[] captures = getOrderedMoves(board, true);
        foreach (Move capture in captures)
        {
            board.MakeMove(capture);
            int score = -quiesceSearch(board, -beta, -alpha);
            board.UndoMove(capture);

            if (score >= beta)
            {
                return beta;
            }
            else if (score > alpha)
            {
                alpha = score;
            }
        }

        return alpha;
    }

    private Move[] getOrderedMoves(Board board, bool capturesOnly = false)
    {
        Move[] moves = board.GetLegalMoves(capturesOnly);
        int[] moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            int moveScore = 0;
            Piece movingPiece = board.GetPiece(moves[i].StartSquare);
            Piece capturePiece = board.GetPiece(moves[i].TargetSquare);

            // Prioritize captures based on the value of the captured piece.
            if (moves[i].IsCapture && !capturePiece.IsNull)
            {
                moveScore += 10 * pieceValues[(int)capturePiece.PieceType];
            }
            // Prioritize moving to a promotion
            if (moves[i].IsPromotion)
            {
                moveScore += pieceValues[(int)moves[i].PromotionPieceType];
            }
            //// Penalize moving into sqaures that are attacked by the opponent.
            if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
            {
                moveScore -= 5 * pieceValues[(int)movingPiece.PieceType];
            }
            //// Prioritize moving out of the way of attacks
            if (board.SquareIsAttackedByOpponent(moves[i].StartSquare))
            {
                moveScore += pieceValues[(int)movingPiece.PieceType];
            }

            moveScores[i] = moveScore;
        }

        // Sort the moves based on the scores in moveScores
        Array.Sort(moveScores, moves);
        // Reverse the list so the biggest scores come first.
        Array.Reverse(moves);

        return moves;
    }

    // Evaluate the board and return a value based on the current pieces in play.
    private int evaluate(Board board)
    {
        int whiteEval = countBoard(board, true);
        int blackEval = countBoard(board, false);

        int evaluation = whiteEval - blackEval;

        if (board.IsWhiteToMove)
        {
            return evaluation;
        }
        else
        {
            return -evaluation;
        }
    }

    // Count all of the pieces on the board.
    private int countBoard(Board board, bool isWhite)
    {
        bool isWhiteToMove = board.IsWhiteToMove;
        int value = 0;
        int bishopCount = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                int rank = piece.Square.Rank;
                int file = piece.Square.File;

                if (piece.IsWhite != isWhite)
                {
                    continue; // This piece is not our color. Skip it.
                }

                if (board.IsInCheck())
                {
                    if (isWhiteToMove == isWhite)
                    {
                        value -= 100000;
                    }
                    else
                    {
                        value += 100000;
                    }
                }

                if (piece.IsBishop)
                {
                    bishopCount += 1;
                }

                /// https://www.chessprogramming.org/Simplified_Evaluation_Function
                // Some sort of strategy to keep peices in the correct places.
                // This function alone takes up a lot of time

                // Keep pieces off the outside of the board. Enforces some form of center control
                if ((rank == 0 || rank == 7 || file == 0 || file == 7) && !piece.IsPawn)
                {
                    value -= 10;
                }


                value += pieceValues[(int)piece.PieceType];
            }
        }

        // Add a bonus for the bishop pair advantage
        // https://www.chessprogramming.org/Bishop_Pair
        if (bishopCount >= 2)
        {
            value += pieceValues[1] / 2;
        }

        return value;
    }
}