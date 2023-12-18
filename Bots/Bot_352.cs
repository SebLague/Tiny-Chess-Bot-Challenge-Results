namespace auto_Bot_352;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_352 : IChessBot
{
    // Null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] pieceValues = { 0, 1, 3, 3, 5, 24, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Random random = new Random();
        Move[] moves = board.GetLegalMoves();
        int bestMove = 0;
        double bestMoveScore = -10000000000000;
        int i = 0;
        foreach (Move move in moves)
        {
            if (isCheckMate(board, move))
            {
                return move;
            }
            double moveScore = evalMove(board, move);
            if (moveScore >= bestMoveScore)
            {
                bestMoveScore = moveScore;
                bestMove = i;
            }
            ++i;
        }
        if (bestMove == 0 && bestMoveScore == 0)
        {
            DivertedConsole.Write("BEST " + bestMoveScore);
            return moves[random.Next(0, moves.Length)];
        }
        DivertedConsole.Write("BEST " + bestMoveScore);
        return moves[bestMove];
    }

    bool isCheckMate(Board board, Move move)
    {
        board.MakeMove(move);
        bool Mate = board.IsInCheckmate();
        board.UndoMove(move);
        return Mate;
    }

    double evalMove(Board board, Move move)
    {
        board.MakeMove(move);
        double score = pieceValues[(int)move.CapturePieceType];
        score -= Math.Abs(3.5 - move.TargetSquare.File) * (board.GetAllPieceLists().Length / 32d) / 5.0;
        score -= (move.TargetSquare.Rank == move.StartSquare.Rank ? 10 : 0);
        board.UndoMove(move);
        score += (move.IsPromotion && !board.SquareIsAttackedByOpponent(move.TargetSquare) ? 10 : 0);
        score -= (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
        score += (board.SquareIsAttackedByOpponent(move.StartSquare) && !board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0) / 1.1;
        board.MakeMove(move);
        score -= (board.IsDraw() && (board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove).ToArray().Length >= board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).ToArray().Length) ? 15 : 0);
        score += (board.IsDraw() && (board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).ToArray().Length >= board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).ToArray().Length) ? 10 : 0);
        score -= (move.MovePieceType == PieceType.King && board.GetAllPieceLists().Length >= 16 ? 32 : 0);
        score += (board.IsInCheck() ? 33 - board.GetAllPieceLists().Length : 0) / 5.0;
        board.UndoMove(move);
        DivertedConsole.Write(score);
        return score;
    }
}