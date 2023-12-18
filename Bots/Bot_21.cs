namespace auto_Bot_21;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_21 : IChessBot
{
    // This function will return a score for the played move. A better score means a better move.
    // After evaluating each possible move available, the bot will play the best move it finds.
    // The tuning of this function will determine how well the bot performs.
    public int Evaluate(Board b, Move m, bool recursion_allowed)
    {
        int score = 0;

        // reward captures
        if (m.IsCapture)
        {
            switch (m.CapturePieceType)
            {
                case (PieceType.Pawn):
                    score += 10;
                    break;
                case (PieceType.Knight):
                    score += 30;
                    break;
                case (PieceType.Bishop):
                    score += 30;
                    break;
                case (PieceType.Rook):
                    score += 50;
                    break;
                case (PieceType.Queen):
                    score += 90;
                    break;
            }
        }

        // reward developing moves
        if (m.MovePieceType != PieceType.King && m.TargetSquare.Rank < m.StartSquare.Rank)
        {
            score += 1;
        }

        // reward promoting to Queen
        if (m.IsPromotion && m.PromotionPieceType == PieceType.Queen)
        {
            score += 30;
        }

        b.MakeMove(m);

        // reward checks. not as much as captures.
        if (b.IsInCheck())
        {
            score += 9;
        }

        // reward checkmate
        if (b.IsInCheckmate())
        {
            score += 1000000;
        }

        // punish creating vulnerabilities
        if (recursion_allowed)
        {
            int enemy_best = 0;
            foreach (Move enemyMove in b.GetLegalMoves())
            {
                int enemy_score = Evaluate(b, enemyMove, false);
                if (enemy_score > enemy_best)
                {
                    enemy_best = enemy_score;
                }
            }
            score -= enemy_best;
        }

        b.UndoMove(m);

        return score;
    }

    public Move Think(Board board, Timer timer)
    {
        Random rng = new Random();
        Move[] moves = board.GetLegalMoves();
        int[] scores = new int[moves.Length];
        List<Move> options = new List<Move>();

        // Analyze possible moves
        for (int i = 0; i < moves.Length; i++)
        {
            scores[i] = Evaluate(board, moves[i], true);
        }

        // initialize options with a random move
        options.Add(moves[rng.Next(moves.Length)]);
        int max = int.MinValue;

        for (int i = 0; i < scores.Length; i++)
        {
            if (scores[i] == max)
            {
                options.Add(moves[i]);
            }

            if (scores[i] > max)
            {
                max = scores[i];
                options.Clear();
                options.Add(moves[i]);
            }
        }

        return options[rng.Next(options.Count)];
    }
}