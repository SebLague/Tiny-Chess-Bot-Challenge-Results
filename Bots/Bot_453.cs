namespace auto_Bot_453;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_453 : IChessBot
{
    Board board;
    bool direction;

    int getPieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 1;
            case PieceType.Rook:
                return 3;
            case PieceType.Knight:
                return 4;
            case PieceType.Bishop:
                return 5;
            case PieceType.Queen:
                return 8;
            default:
                return 0;
        }
    }

    bool isForward(Move move, bool isPlayer)
    {
        if (direction ^ isPlayer)
        {
            return move.TargetSquare.Rank < move.StartSquare.Rank;
        }
        else
        {
            return move.TargetSquare.Rank > move.StartSquare.Rank;
        }
    }

    int Score(Move move, int minDepth, int maxDepth, bool isPlayer = true)
    {
        board.MakeMove(move);
        int sign = isPlayer ? 1 : -1;
        int capture = getPieceValue(move.CapturePieceType);
        int promotion = move.IsPromotion ? 10 : 0;
        int checkmate = board.IsInCheckmate() ? 30 : 0;
        int repeatedOrDraw = board.IsRepeatedPosition() || board.IsDraw() ? -10 : 0;
        int pawnMovedForwards = move.MovePieceType == PieceType.Pawn && isForward(move, isPlayer) && isPlayer ? 1 : 0;
        int score = (capture + promotion + checkmate) * sign + pawnMovedForwards + repeatedOrDraw;
        isPlayer = !isPlayer; // Move on to other player
        int bestScore = isPlayer ? 0 : int.MaxValue;
        if (maxDepth > 0 && (score != 0 || minDepth > 0))
        {
            foreach (Move newMove in board.GetLegalMoves()
                .OrderByDescending(m => Math.Abs(Score(m, 0, 0, isPlayer))).Take(10))
            {
                int newScore = Score(newMove, minDepth - 1, maxDepth - 1, isPlayer);
                if (isPlayer && newScore > bestScore)
                {
                    bestScore = newScore;
                }
                else if (!isPlayer && newScore < bestScore)
                {
                    bestScore = newScore;
                }
            }
            if (bestScore != int.MaxValue)
            {
                score += bestScore;
            }
        }
        board.UndoMove(move);
        return score;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        this.board = board;
        direction = board.GetKingSquare(board.IsWhiteToMove).Rank == 0;

        int bestScore = 0;
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            int score = Score(move, 2, 3);
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        if (bestMove != Move.NullMove)
        {
            return bestMove;
        }
        else
        {
            return moves[0];
        }
    }
}