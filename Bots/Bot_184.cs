namespace auto_Bot_184;
using ChessChallenge.API;
using System;

struct ScoredMove
{
    public Move move;
    public float score;

    public ScoredMove(Move newMove, float newScore)
    {
        move = newMove;
        score = newScore;
    }
}
public class Bot_184 : IChessBot
{
    private Random random = new Random();
    private int getPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.None: return 0;
            case PieceType.Pawn: return 1;
            case PieceType.Knight: return 3;
            case PieceType.Bishop: return 3;
            case PieceType.Rook: return 5;
            case PieceType.Queen: return 9;
            case PieceType.King: return 1000;
        }
        return 0;
    }

    private float getMoveScore(Move move, Board board)
    {
        float score = 0;
        if (move.MovePieceType == PieceType.Pawn)
        {
            if (move.IsEnPassant)
            {
                score += 20;
            }
            if (move.IsPromotion)
            {
                score += getPieceValue(move.PromotionPieceType);
            }
        }

        if (move.IsCastles)
        {
            score += 1;
        }

        if (move.IsCapture)
        {
            score += getPieceValue(move.CapturePieceType);
        }

        float diff = move.TargetSquare.Rank - move.StartSquare.Rank;
        if (board.IsWhiteToMove)
        {
            score += diff * 0.05f;
        }
        else
        {
            score -= diff * 0.05f;
        }

        return score;
    }

    private bool isWinning(Board board)
    {
        int friendlyPoints = 0;
        int enemyPoints = 0;

        for (int i = 1; i < 6; i++)
        {
            PieceList friendlyPieces = board.GetPieceList((PieceType)i, board.IsWhiteToMove);
            foreach (Piece piece in friendlyPieces)
            {
                friendlyPoints += getPieceValue((PieceType)i);
            }

            PieceList enemyPieces = board.GetPieceList((PieceType)i, !board.IsWhiteToMove);
            foreach (Piece piece in enemyPieces)
            {
                enemyPoints += getPieceValue((PieceType)i);
            }
        }

        return friendlyPoints > enemyPoints;
    }

    private int getNumMoves(Board board, bool isWhite)
    {
        int numMoves = 0;
        for (int i = 1; i < 7; i++)
        {
            PieceList pieces = board.GetPieceList((PieceType)i, isWhite);
            foreach (Piece piece in pieces)
            {
                numMoves += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks((PieceType)i, piece.Square, board, isWhite));
            }
        }
        return numMoves;
    }

    private ScoredMove getBestMove(Board board, int n)
    {
        Move[] moves = board.GetLegalMoves();
        ScoredMove bestMove = new ScoredMove(Move.NullMove, float.MinValue);
        if (moves.Length != 0)
        {
            bestMove.move = moves[0];
        }

        foreach (Move move in moves)
        {
            float moveScore = getMoveScore(move, board);

            if (n > 0)
            {
                int numMoves = getNumMoves(board, board.IsWhiteToMove);
                board.MakeMove(move);

                int futureNumMoves = getNumMoves(board, !board.IsWhiteToMove);
                moveScore += (futureNumMoves - numMoves) * 0.1f;

                if (board.IsInCheckmate())
                {
                    moveScore += 1000;
                }
                else if (board.IsDraw())
                {
                    if (!isWinning(board)) //if enemy is winning
                    {
                        moveScore -= 50;
                    }
                    else
                    {
                        moveScore += 50;
                    }
                }
                else
                {
                    ScoredMove enemyBestMove = getBestMove(board, n - 1);
                    moveScore -= enemyBestMove.score;
                    if (isWinning(board) && move.IsCapture && enemyBestMove.move.IsCapture &&
                        getPieceValue(move.CapturePieceType) == getPieceValue(enemyBestMove.move.CapturePieceType)) //don't trade pieces if enemy is winning
                    {
                        moveScore -= 0.5f;
                    }
                }

                board.UndoMove(move);
            }

            if (moveScore == bestMove.score)
            {
                if (random.Next(0, 2) == 1)
                {
                    bestMove = new ScoredMove(move, moveScore);
                }
            }
            if (moveScore > bestMove.score)
            {
                bestMove = new ScoredMove(move, moveScore);
            }
        }
        return bestMove;
    }

    public Move Think(Board board, Timer timer)
    {
        if (board.GetFenString() == board.GameStartFenString && board.PlyCount == 0)
        {
            return new Move("e2e4", board);
        }

        int searchDepth = 3;

        int numPieces = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

        if (numPieces < 12)
        {
            searchDepth = 4;
        }

        if (timer.MillisecondsRemaining < 15000)
        {
            searchDepth--;
        }

        return getBestMove(board, searchDepth).move;
    }
}