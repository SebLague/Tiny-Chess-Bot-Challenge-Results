namespace auto_Bot_248;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_248 : IChessBot
{

    int thinkdepth;
    int[] pieceValues = { 100, 300, 325, 500, 900, 10000 };
    PieceType[] types = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };
    Dictionary<ulong, int> hash = new();
    public Move Think(Board board, Timer timer)
    {
        //Thinkdepth is 3 for t>9s
        thinkdepth = Math.Min(3, timer.MillisecondsRemaining / 3000);
        int Mainscore;
        Move moveToPlay = new();
        int player = con(board.IsWhiteToMove, 2) - 1;
        int MainbestScore = -10001 * player;
        foreach (Move move in SortMoves(board))
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }
            Mainscore = MinMax(board, 0, -10000, 10000, -player);
            if ((player == 1 && Mainscore > MainbestScore) || (player == -1 && Mainscore < MainbestScore))
            {
                MainbestScore = Mainscore;
                moveToPlay = move;
            }
            board.UndoMove(move);
        }
        return moveToPlay;
    }


    int MinMax(Board board, int depth, int alpha, int beta, int player)
    {
        int bestScore = -10000 * player;
        if (board.IsDraw()) return -500 * player;
        if (depth > thinkdepth) return EvaluatePosition(board);

        foreach (Move move in SortMoves(board))
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return 10000 * player;
            }
            if (!hash.ContainsKey(board.ZobristKey)) hash.TryAdd(board.ZobristKey, MinMax(board, depth + 1, alpha, beta, -player));

            hash.TryGetValue(board.ZobristKey, out int score);
            board.UndoMove(move);

            if (player == 1 && score > bestScore)
            {
                bestScore = score;
                alpha = score;
            }
            else if (player == -1 && score < bestScore)
            {
                bestScore = score;
                beta = score;
            }
            if (alpha > beta) break;
        }
        return bestScore;
    }

    int EvaluatePosition(Board board)
    {
        int result = 0;

        //Add up all the pieces
        for (int i = 0; i < 5; i++)
        {
            result += (board.GetPieceList(types[i], true).Count * pieceValues[i]) - (board.GetPieceList(types[i], false).Count * pieceValues[i]);
        }

        //Check how well you have developed your Pawns
        Square PawnSquare;
        foreach (Piece pawn in board.GetPieceList(types[0], true))
        {
            PawnSquare = pawn.Square;
            if (PawnSquare.File < 5 && PawnSquare.File > 2 && PawnSquare.Rank > 1) result += (PawnSquare.Rank - 1) * 3;
            if (PawnSquare.Rank > 1) result += PawnSquare.Rank - 1;
        }
        foreach (Piece pawn in board.GetPieceList(types[0], false))
        {
            PawnSquare = pawn.Square;
            if (PawnSquare.File < 5 && PawnSquare.File > 2 && PawnSquare.Rank < 6) result += (PawnSquare.Rank - 6) * 3;
            if (PawnSquare.Rank < 6) result += PawnSquare.Rank - 6;
        }
        // Check if King is on his side of the board
        result += con(board.GetKingSquare(true).Rank < 1, 5) - con(board.GetKingSquare(true).Rank < 1, 5);

        // Calculate Slider-Piece-Efficiency
        for (int i = 2; i < 5; i++) result += activeSquares(i, true, board) - activeSquares(i, false, board);


        //Check who "dominates" the center
        for (int k = -1; k <= 1; k += 2)
        {
            for (int i = 1; i <= 3; i++)
            {
                foreach (Piece p in board.GetPieceList(types[i], k == 1))
                {
                    Square s = p.Square;
                    if (s.Rank < 6 && s.Rank > 1)
                    {
                        result += 5 * k;
                        if (s.File < 5 && s.File > 2) result += 3 * k;
                    }
                }
            }

        }
        return result;
    }


    //Calculates how many squares a slider-Piece controls
    int activeSquares(int piece, Boolean white, Board board)
    {
        PieceList sq = board.GetPieceList(types[piece], white);
        int result = 0;
        for (int i = 0; i < sq.Count; i++)
        {
            result += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(types[piece], sq[i].Square, board));
        }
        return result / 2;
    }

    //Basic Move-Sorting to make Cutoffs more likely
    Move[] SortMoves(Board board)
    {
        return board.GetLegalMoves()
                    .OrderByDescending(move => Eval(move))
                    .ToArray();
    }


    //Evaluates a Move based on it's properties
    int Eval(Move m)
    {
        return con(m.IsCapture, 40) + con(m.IsPromotion, 100) +
        con(m.IsCastles, 20) + con(m.MovePieceType != PieceType.Pawn, 10) +
        con(m.TargetSquare.Rank > 2 && m.TargetSquare.Rank < 5, 10) + con(m.TargetSquare.File > 2 && m.TargetSquare.File < 5, 10);
    }

    //Converts a boolean into an int
    int con(Boolean val, int mult) { return mult * Convert.ToInt32(val); }
}
