namespace auto_Bot_204;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_204 : IChessBot
{

    float[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };

    public Move Think(Board board, Timer timer)
    {
        return Evaluate(board);
    }

    private Move Evaluate(Board board)
    {
        List<Move> moves = new List<Move>(board.GetLegalMoves());
        moves.Sort((a, b) => -GetScore(a, board).CompareTo(GetScore(b, board)));
        double score = GetScore(moves[0], board);
        if (!board.IsWhiteToMove)
            score = -score;
        DivertedConsole.Write("Move " + board.PlyCount + ": " + moves[0] + " with score " + score);
        return moves[0];
    }

    private double GetScore(Move move, Board board)
    {
        double score = 0;
        bool white = board.IsWhiteToMove;
        bool endgame = board.GetPieceList(PieceType.Queen, !white).Count == 0;

        Move? attacker = null;
        if (board.TrySkipTurn())
        {
            attacker = IsSpaceProtected(board, move.StartSquare);
            board.UndoSkipTurn();
        }

        board.MakeMove(move);
        if (board.IsDraw())
        {
            board.UndoMove(move);
            return 0;
        }
        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return 1000;
        }

        float avgRank = 0, avgFile = 0; // Shake that king to the sides of the board
        float avgMoves = 0;
        foreach (Move mv in board.GetLegalMoves())
        {
            board.MakeMove(mv);
            if (board.IsInCheckmate())
            {
                board.UndoMove(mv);
                board.UndoMove(move);
                return -1000;
            }
            avgMoves += board.GetLegalMoves().Length;
            avgRank += board.GetKingSquare(!white).Rank;
            avgFile += board.GetKingSquare(!white).File;
            board.UndoMove(mv);
        }
        avgRank /= board.GetLegalMoves().Length;
        avgFile /= board.GetLegalMoves().Length;
        avgMoves /= board.GetLegalMoves().Length;
        score -= (3.5 - (Math.Abs(3.5 - avgRank))
                + (3.5 - (Math.Abs(3.5 - avgFile)))
        ) / 25.0;
        score += avgMoves / 5000.0;

        if (move.IsEnPassant)
            score += 0.02; // Cool factor
        if (move.IsCastles)
            score += 0.01; // Cool factor
        if (move.TargetSquare.File != move.StartSquare.File && (white && move.StartSquare.File == 0) || (!white && move.StartSquare.File == 8))
            score += 1.0 / board.PlyCount; // Get it off the home rank

        Move? protector = IsSpaceProtected(board, move.TargetSquare);

        if (attacker != null && attacker != protector)
            score += (GetPieceScore(move.MovePieceType) - GetPieceScore(attacker.Value.MovePieceType)) * 5.0;

        if (move.IsCapture)
        { // Captures
            score += GetPieceScore(move.CapturePieceType) * 2.5;

            List<Move> moves = new List<Move>(board.GetLegalMoves());
            if (protector != null)
            {
                // If the piece is defended, it's not worth as much
                score -= GetPieceScore(move.MovePieceType) * 2.0;
                score -= 1.0 / GetPieceScore(protector.Value.MovePieceType) * 2.0;
            }
        }
        if (move.MovePieceType == PieceType.Pawn)
        {
            score += 0.05 * Math.Abs(move.StartSquare.Rank - move.TargetSquare.Rank) * 8.0 / board.PlyCount;
            score += (3.5 - (Math.Abs(3.5 - move.StartSquare.File))) / 100.0;
        }
        if (protector == null)
        { // Forking is pog
            if (board.TrySkipTurn())
            {
                foreach (Move mv in board.GetLegalMoves(true))
                {
                    if (mv.StartSquare != move.TargetSquare)
                        continue;
                    Move? prot = IsSpaceProtected(board, mv.TargetSquare);
                    if (prot != null)
                    {
                        score += GetPieceScore(mv.CapturePieceType) / GetPieceScore(prot.Value.MovePieceType);
                    }
                    else
                    {
                        score += GetPieceScore(mv.CapturePieceType);
                    }
                }
                board.UndoSkipTurn();
            }
        }

        if (protector != null)
            score -= GetPieceScore(move.MovePieceType);
        if (move.IsPromotion) // Promote to queen
            score += GetPieceScore(move.PromotionPieceType);
        score -= board.GetLegalMoves().Length / 50.0; // Less moves for the opponent is better

        foreach (PieceType type in Enum.GetValues<PieceType>())
        {
            if (type == PieceType.None)
                continue;
            score += GetPieceScore(type) * board.GetPieceList(type, white).Count * 2.5;
            score -= GetPieceScore(type) * board.GetPieceList(type, !white).Count * 2.5;

            if (endgame)
            {
                // Endgame baby
                foreach (Piece piece in board.GetPieceList(type, white))
                {
                    score -= GetDistance(piece.Square, board.GetKingSquare(!white)) / 100.0;
                }
            }
        }

        board.UndoMove(move);
        return score;
    }

    private Move? IsSpaceProtected(Board board, Square square)
    {
        List<Move> moves = new List<Move>(board.GetLegalMoves(true)).FindAll(m => m.TargetSquare == square);
        if (moves.Count == 0)
            return null;
        moves.Sort((a, b) => GetPieceScore(a.MovePieceType).CompareTo(GetPieceScore(b.MovePieceType)));
        return moves[0];
    }

    private float GetPieceScore(PieceType type)
    {
        return pieceValues[(int)type];
    }

    private double GetDistance(Square a, Square b, bool squared = false)
    {
        double dist = Math.Pow(a.Rank - b.Rank, 2) + Math.Pow(a.File - b.File, 2);
        return squared ? dist : Math.Sqrt(dist);
    }
}