namespace auto_Bot_201;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_201 : IChessBot
{
    int[] pieceValues = { -1, 100, 300, 300, 500, 900, 10000 };
    enum StartingSide
    {
        Top,
        Bottom
    }
    StartingSide side;
    bool botColorWhite;
    int moveCount = 0;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        if (moveCount == 0)
        {
            DetermineSide(board);
        }
        moveCount++;

        if (ChargeForward(board, moves, out Move forwardMove))
        {
            return forwardMove;
        }

        if (GetRandom(moves, out Move randomMove))
        {
            return randomMove;
        }
        return moves[0];
    }
    private void SetSide(StartingSide newSide)
    {
        side = newSide;
    }
    private void DetermineSide(Board board)
    {
        botColorWhite = board.IsWhiteToMove;

        int kingRank = board.GetKingSquare(botColorWhite).Rank;
        if (kingRank == 0)
        {
            SetSide(StartingSide.Top);
        }
        if (kingRank == 7)
        {
            SetSide(StartingSide.Bottom);
        }
    }
    public bool ChargeForward(Board board, Move[] moves, out Move foundMove)
    {
        List<Move> forwardMoves = new List<Move>();
        Move highestValueForwardMove = new();
        int highestValueFound = -1;
        foreach (var item in moves)
        {
            if (MoveIsCheckmate(board, item))
            {
                foundMove = item;
                return true;
            }
            if (IsTileForward(item, side))
            {
                forwardMoves.Add(item);
                int moveScore = DetermineScore(board, item);
                if (moveScore >= highestValueFound)
                {
                    highestValueForwardMove = item;
                    highestValueFound = moveScore;
                }
            }
        }

        if (forwardMoves.Count > 0)
        {
            /*
                if (GetRandom(forwardMoves.ToArray(), out Move randomForwardMove))
                {
                    foundMove = randomForwardMove;
                    return true;
                }*/
            foundMove = highestValueForwardMove;
            return true;
        }

        foundMove = new();
        return false;
    }
    private static bool IsTileForward(Move move, StartingSide side)
    {
        switch (side)
        {
            case StartingSide.Top:
                if (move.StartSquare.Rank < move.TargetSquare.Rank)
                    return true;
                break;
            case StartingSide.Bottom:
                if (move.StartSquare.Rank > move.TargetSquare.Rank)
                    return true;
                break;
            default:
                break;
        }
        return false;
    }
    public bool GetRandom(Move[] moves, out Move foundMove)
    {
        if (moves.Length > 0)
        {
            int random = new Random().Next();
            foundMove = moves[random % moves.Length];
            return true;
        }
        foundMove = new();
        return false;
    }
    private int DetermineScore(Board board, Move move)
    {
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

        return capturedPieceValue;
    }
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}