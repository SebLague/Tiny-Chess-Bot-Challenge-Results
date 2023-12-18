namespace auto_Bot_532;
using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class Bot_532 : IChessBot
{
    int Depth = 4;
    Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>()
    {
        {PieceType.None, 0 },
        {PieceType.Pawn, 100},
        {PieceType.Knight, 300},
        {PieceType.Bishop, 300},
        {PieceType.Rook, 500},
        {PieceType.Queen, 900},
        {PieceType.King, 10000}
    };
    List<Move> bestEquallMoves = new List<Move>();
    int bestEquallScore;
    public Move Think(Board board, Timer timer)
    {
        Move[] allmoves = board.GetLegalMoves();
        Move bestMove = allmoves[0];

        foreach (Move move in allmoves)
        {
            int score = GetScoreToDepth(board, move, Depth, 1);
            DivertedConsole.Write("Think; score={0}", score);

            if (score > bestEquallScore)
            {
                bestEquallScore = score;
                bestEquallMoves.Clear();
                bestEquallMoves.Add(move);
            }
            else if (score == bestEquallScore)
            {
                bestEquallScore = score;
                bestEquallMoves.Add(move);
            }
        }
        Random rng = new();
        Move moveToPlay = bestEquallMoves[rng.Next(bestEquallMoves.Count)];
        bestEquallMoves.Clear();
        bestEquallScore = 0;
        return moveToPlay;
    }
    /// <summary>
    /// Gets combined score after taking into account possible future moves.
    /// </summary>
    int GetScoreToDepth(Board board, Move move, int recursionDepth, int isMe)
    {
        // DivertedConsole.Write("  GetScoreToDepth; depth={0}, isMe={1}", recursionDepth, isMe);
        int thisMoveScore = moveScore(move, board, isMe, recursionDepth);

        if (recursionDepth == 1)
        {
            return thisMoveScore;
        }

        int bestNextScore = 0;
        board.MakeMove(move);
        Move[] allmoves = board.GetLegalMoves();
        foreach (Move nextMove in allmoves)
        {
            int nextScore = GetScoreToDepth(board, nextMove, recursionDepth - 1, isMe * -1);

            if (isMe == 1)
            {
                if (nextScore > bestNextScore)
                {
                    bestNextScore = nextScore;
                }
            }
            else
            {
                if (nextScore < bestNextScore)
                {
                    bestNextScore = nextScore;
                }
            }
        }
        board.UndoMove(move);


        int combinedScore = thisMoveScore + bestNextScore;
        // DivertedConsole.Write("  GetScoreToDepth; depth={0}, combinedScore={1}", recursionDepth, combinedScore);
        return combinedScore;
    }

    int moveScore(Move move, Board board, int isMe, int recursionDepth)
    {
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[capturedPiece.PieceType];
        int checkmateValue = 0;
        if (MoveIsCheckmate(board, move))
        {
            checkmateValue = 100000 + 1000 * recursionDepth;
        }
        return (checkmateValue + capturedPieceValue) * isMe;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}