namespace auto_Bot_519;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_519 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return SearchBestMoveMinMax(board, timer);
    }

    public Move SearchBestMoveMinMax(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 8 * 1000)
        {
            initialDepth = 1;
        }
        else
        if (timer.MillisecondsRemaining < 15 * 1000)
        {
            initialDepth = 2;
        }
        else
        if (timer.MillisecondsRemaining < 20 * 1000)
        {
            initialDepth = 3;
        }
        else
        if (timer.MillisecondsRemaining < 25 * 1000)
        {
            initialDepth = 4;
        }
        else
        {
            initialDepth = 5;
        }
        return MinMax(board, initialDepth);
    }

    int isWhiteMove = 0;
    int initialDepth = 5;

    public Move MinMax(Board board, int depth)
    {
        isWhiteMove = board.IsWhiteToMove ? 1 : -1;
        System.Random rand = new System.Random();
        Move[] legalMoves = board.GetLegalMoves();

        List<Move> bestMoves = new List<Move>();
        float chosenScore = float.NegativeInfinity;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            float score = AlphaBetaPruning(board, depth - 1, float.NegativeInfinity, float.PositiveInfinity, false);
            board.UndoMove(move);

            DivertedConsole.Write(move.StartSquare.Name + " to " + move.TargetSquare.Name + ": " + score);

            if (score > chosenScore)
            {
                bestMoves.Clear();
            }
            if (score >= chosenScore)
            {
                bestMoves.Add(move);
                chosenScore = score;
            }
        }

        Move chosenMove = bestMoves[rand.Next(bestMoves.Count)];
        DivertedConsole.Write("CHOSEN MOVE:");
        DivertedConsole.Write(chosenMove.StartSquare.Name + " to " + chosenMove.TargetSquare.Name + ": " + chosenScore);

        return chosenMove;
    }

    public float AlphaBetaPruning(Board node, int depth, float α, float β, bool maximizingPlayer)
    {
        float value = 0;
        if (depth == 0 || node.IsInCheckmate() || node.IsDraw())
        {
            return EvalBoard(node, depth);
        }

        if (maximizingPlayer)
        {
            value = float.NegativeInfinity;
            foreach (Move move in node.GetLegalMoves())
            {
                node.MakeMove(move);
                value = Math.Max(value, AlphaBetaPruning(node, depth - 1, α, β, false));
                node.UndoMove(move);


                α = Math.Max(α, value);
                if (value >= β)
                {
                    break;
                }
            }
            return value;
        }
        else
        {
            value = float.PositiveInfinity;
            foreach (Move move in node.GetLegalMoves())
            {
                node.MakeMove(move);
                value = Math.Min(value, AlphaBetaPruning(node, depth - 1, α, β, true));
                node.UndoMove(move);


                β = Math.Min(β, value);
                if (value <= α)
                {
                    break;
                }
            }
            return value;
        }
    }


    public float EvalBoard(Board board, int depth)
    {
        float far = ((initialDepth - depth) / (float)initialDepth) / 10f;
        if (board.IsInCheckmate())
        {
            return (board.IsWhiteToMove ? -1000 : 1000) * isWhiteMove - far;
        }
        if (board.IsDraw())
        {
            return 0 - far;
        }

        float check = (board.IsWhiteToMove ? 1 : -1) * (board.IsInCheck() ? 0.5f : 0);
        float queenDiff = (board.GetPieceList(PieceType.Queen, true).Count - board.GetPieceList(PieceType.Queen, false).Count) * 9;
        float bishopDiff = (board.GetPieceList(PieceType.Bishop, true).Count - board.GetPieceList(PieceType.Bishop, false).Count) * 3;
        float knightDiff = (board.GetPieceList(PieceType.Knight, true).Count - board.GetPieceList(PieceType.Knight, false).Count) * 3;
        float rookDiff = (board.GetPieceList(PieceType.Rook, true).Count - board.GetPieceList(PieceType.Rook, false).Count) * 5;
        float pawnDiff = (board.GetPieceList(PieceType.Pawn, true).Count - board.GetPieceList(PieceType.Pawn, false).Count) * 1;

        return (queenDiff + bishopDiff + knightDiff + rookDiff + pawnDiff + check) * isWhiteMove - far;
    }
}