namespace auto_Bot_115;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public static class IntExtensions
{
    public static int MultiplyWithBool(this int value, bool condition)
    {
        return condition ? value : -value;
    }
}

public class Bot_115 : IChessBot
{
    private Random rand = new();

    // Evaluation values
    private readonly Dictionary<PieceType, int> pieceValues = new()
    {
        { PieceType.None, 0 },
        { PieceType.Pawn, 90 },
        { PieceType.Knight, 200 },
        { PieceType.Bishop, 220 },
        { PieceType.Rook, 400 },
        { PieceType.Queen, 800 },
        { PieceType.King, 2000 }
    };
    private int RankValue = 10;
    private int CentralBonus = 90;
    private int PeripheralPenalty = -20;
    private int LongDiagonalBonus = 50;
    private float attackedSquareRatio = 0.95f;

    private int EvaluatePiece(Piece piece)
    {
        return pieceValues[piece.PieceType].MultiplyWithBool(piece.IsWhite);
    }

    /// <summary>
    /// Pawns should be pushed forward as much as possible
    /// </summary>
    private int GetPushBonus(Piece piece)
    {
        if (piece.PieceType != PieceType.Pawn)
        {
            var score = piece.Square.Rank * RankValue;
            if (!piece.IsWhite)
                score = 7 * RankValue - score;

            return score.MultiplyWithBool(piece.IsWhite);
        }

        return 0;
    }

    /// <summary>
    /// Knights and Queens should prefer central positions
    /// </summary>
    private int GetMiddlePositionalBonus(Piece piece)
    {
        if (piece.PieceType is not (PieceType.Knight or PieceType.Queen))
            return 0;

        if (piece.Square.File is 3 or 4 &&
            piece.Square.Rank is 3 or 4)
            return CentralBonus.MultiplyWithBool(piece.IsWhite);

        if (piece.Square.Rank is 0 or 1 or 6 or 7 &&
            piece.Square.File is 0 or 1 or 6 or 7)
            return PeripheralPenalty.MultiplyWithBool(piece.IsWhite);

        return 0;
    }

    /// <summary>
    /// If a piece is under attack, reduce its score 
    /// </summary>
    private int AttackedSquare(Board board, Piece piece)
    {
        if (board.SquareIsAttackedByOpponent(piece.Square))
        {
            return (int)MathF.Round(-pieceValues[piece.PieceType].MultiplyWithBool(piece.IsWhite) * attackedSquareRatio);
        }

        return 0;
    }

    /// <summary>
    /// Bishop and Queens should prefer the long diagonal to maximise their vision
    /// </summary>
    private int GetLongDiagonalPositionalBonus(Piece piece)
    {
        if (piece.PieceType is not (PieceType.Bishop or PieceType.Queen))
            return 0;

        if (piece.Square.File == piece.Square.Rank)
        {
            return LongDiagonalBonus.MultiplyWithBool(piece.IsWhite);
        }

        return 0;
    }


    public int Evaluate(Board board, bool isWhite)
    {
        var allPieces = board.GetAllPieceLists();

        var evaluation = allPieces
            .Sum(pieceList => pieceList
                .Sum(piece => EvaluatePiece(piece) +
                              GetPushBonus(piece) +
                              GetMiddlePositionalBonus(piece) +
                              GetLongDiagonalPositionalBonus(piece) +
                              AttackedSquare(board, piece)));

        return evaluation.MultiplyWithBool(isWhite);
    }

    public Move Think(Board board, Timer timer)
    {
        var moves = board.GetLegalMoves();
        var moveToMake = moves[rand.Next(moves.Length)];
        var bestScore = 0;

        foreach (var move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }

            board.ForceSkipTurn();
            var newScore = Maximax(board, 1, board.IsWhiteToMove);
            board.UndoSkipTurn();
            board.UndoMove(move);

            if (newScore <= bestScore) continue;

            bestScore = newScore;
            moveToMake = move;
        }

        return moveToMake;
    }

    /// <summary>
    /// I wouldn't expect this to work particularly well for depth > 1. At that point I'd use the median score
    /// or something for a given move since summing will prefer moves that have more options
    /// </summary>
    public int Maximax(Board board, int depth, bool isWhite)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            return Evaluate(board, isWhite);
        }

        var moves = board.GetLegalMoves();
        var totalScore = 0;
        foreach (var move in moves)
        {
            board.MakeMove(move);
            board.ForceSkipTurn();

            var score = Maximax(board, depth - 1, isWhite);
            totalScore += score;

            board.UndoSkipTurn();
            board.UndoMove(move);
        }
        return totalScore;
    }
}