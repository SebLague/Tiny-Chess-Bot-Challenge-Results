namespace auto_Bot_334;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Stackfish 1.1b
// (Not related in any way to Stockfish)
// Weights for scoring are somewhat arbitrary, but selected in a way that makes it easier to debug
// by having different score metrics use different ranges of the scale. Checkmates for example score ~100K.
// Alive pieces are 100x(piecevalue) so scores in the 1000s are mostly due to pieces on board, attacking
// enemy pieces score 5 times the piece value, so they probably will amount to a couple of hundreds,
// Overlaps in the scoring makes for interesting decisions. For example attacking several valuable pieces
// from the opponent could be worth more than having a piece on the board (but never more than a checkmate).
public class Bot_334 : IChessBot
{
    Random rnd = new Random();

    public int squareAttackValue(Board b, Square sq)
    {
        Piece pieceAtSquare = b.GetPiece(sq);
        // "Attacking" a square of our own color grants no score
        if (pieceAtSquare.IsWhite == b.IsWhiteToMove) return 1;
        return 5 * pieceValue(pieceAtSquare.PieceType);
    }

    public int boardPiecesScore(Board b)
    {
        int sideMult = b.IsWhiteToMove ? 1 : -1;
        int score = 0;

        for (int i = 0; i < 64; i++)
        {
            Square sq = new Square(i);
            Piece p = b.GetPiece(sq);
            if (p.PieceType != PieceType.None)
            {
                score += (p.IsWhite ? 100 : -100) * pieceValue(p.PieceType);
            }
            if (b.SquareIsAttackedByOpponent(sq)) score -= sideMult * squareAttackValue(b, sq);
        }
        score += sideMult * b.GetLegalMoves().Length;
        // Unfortunately the only way I found to evaluate pieces being attacked by the current player
        // is skipping the turn and evaluating from the opponent's perspective. This is rather slow,
        // but it works.
        if (b.TrySkipTurn())
        {
            // We are also deducting from the score the amount of moves our opponent has.
            // This should make the bot prefer moves that restrict them.
            score -= sideMult * b.GetLegalMoves().Length;
            for (int i = 0; i < 64; i++)
            {
                Square sq = new Square(i);
                if (b.SquareIsAttackedByOpponent(sq)) score += sideMult * squareAttackValue(b, sq);
            }
            b.UndoSkipTurn();
        }
        return score;
    }
    public int pieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return 1;
            case PieceType.Knight: return 3;
            case PieceType.Bishop: return 4;
            case PieceType.Rook: return 5;
            case PieceType.Queen: return 10;
            case PieceType.King: return 1;
        }
        return 0;
    }

    public int moveScore(Board b, Move m, int depth)
    {
        int score = 0;
        int sideMult = b.IsWhiteToMove ? 1 : -1;
        // The following small score adjustments are meant to be simple tie breakers
        // i.e: If the bot can't choose one move over another, this hopes to tilt the
        // scale towards the better moves, by detecting unfavorable scenarios

        // Discourage moving the king ----------
        // When pieces are too far to attack the enemy king, and all moves basically lead
        // to not a single clear best move in the 2 or 3 moves that this bot can see ahead,
        // the bot has chances of just randomly moving the king, wasting time.
        // This discourages that behavior.
        if (m.MovePieceType == PieceType.King) score -= sideMult * 5;
        // Encourage castling
        if (m.IsCastles) score += sideMult * 10;

        b.MakeMove(m);
        // End-Cases
        if (b.IsInCheckmate())
        {
            // Adding a small offset for depth will allow us to hopefully pick a faster checkmate
            // in case of several paths leading to one
            score = sideMult * (50000 + 50000 * depth);
        }
        else if (b.IsRepeatedPosition() || b.IsDraw())
        {
            score = 0;
        }
        else if (depth < 1)
        {
            score = boardPiecesScore(b);
        }
        else
        {
            // Deep thinking
            score += bestMove(b, depth - 1).Item2;
        }
        b.UndoMove(m);
        return score;
    }
    public (Move, int) randomMoveWithScore(List<(Move, int)> movesWithScore, int scoreToMatch)
    {
        List<(Move, int)> filteredList = movesWithScore.FindAll(m => m.Item2 == scoreToMatch);
        return filteredList.ElementAt(rnd.Next(filteredList.Count));
    }

    public (Move, int) bestMove(Board board, int depth)
    {
        Move[] moves = board.GetLegalMoves();
        if (moves.Length < 1) return (Move.NullMove, 0);
        List<(Move, int)> movesWithScores = new List<(Move, int)>();
        int maxScore = -9999999;
        int minScore = 9999999;
        for (int m = 0; m < moves.Length; m++)
        {
            int score = moveScore(board, moves[m], depth);
            movesWithScores.Add((moves[m], score));
            if (score <= minScore) minScore = score;
            if (score >= maxScore) maxScore = score;
        }

        return board.IsWhiteToMove ?
            randomMoveWithScore(movesWithScores, maxScore) :
            randomMoveWithScore(movesWithScores, minScore);
    }

    public Move Think(Board board, Timer timer)
    {
        return bestMove(board, 2).Item1;
    }
}