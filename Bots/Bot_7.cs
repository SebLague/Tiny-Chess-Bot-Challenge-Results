namespace auto_Bot_7;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_7 : IChessBot
{
    readonly Random _random = new();

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Piece[] piecesBeingAttackedOrderedByPieceImportance = GetPiecesBeingAttackedOrderedByPieceImportance(board);

        if (piecesBeingAttackedOrderedByPieceImportance.Length > 0)
        {
            foreach (Piece pieceBeingAttacked in piecesBeingAttackedOrderedByPieceImportance)
            {
                List<Move> pieceBeingAttackedNonDangerousMoves = GetPieceNonDangerousMoves(board, pieceBeingAttacked);

                if (pieceBeingAttackedNonDangerousMoves.Count > 0)
                {
                    return pieceBeingAttackedNonDangerousMoves.GetRandom(_random);
                }

                List<Move> pieceBeingAttackedAttackMoves = GetPieceAttackMoves(board, pieceBeingAttacked);

                if (pieceBeingAttackedAttackMoves.Count > 0)
                {
                    return pieceBeingAttackedAttackMoves.GetRandom(_random);
                }
            }
        }

        List<Move> nonDangerousMovesAttackingMoves = GetNonDangerousAttackingMovesOrderedByPieceImportance(board);

        if (nonDangerousMovesAttackingMoves.Count > 0)
        {
            return nonDangerousMovesAttackingMoves[0];
        }

        Move[] nonDangerousMoves = GetNonDangerousMovesOrderedByPieceImportance(board);

        if (nonDangerousMoves.Length > 0)
        {
            return nonDangerousMoves.GetRandom(_random);
        }

        return moves.GetRandom(_random);
    }

    List<Piece> GetMyPieces(Board board)
    {
        List<Piece> ret = new();

        PieceList[] allPieceLists = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieceLists)
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                {
                    continue;
                }

                ret.Add(piece);
            }
        }

        return ret;
    }

    Piece[] GetPiecesBeingAttackedOrderedByPieceImportance(Board board)
    {
        List<Piece> ret = new();

        List<Piece> myPieces = GetMyPieces(board);

        foreach (Piece piece in myPieces)
        {
            bool isAttacked = board.SquareIsAttackedByOpponent(piece.Square);

            if (!isAttacked)
            {
                continue;
            }

            ret.Add(piece);
        }

        return ret.OrderByDescending(o => o.PieceType).ToArray();
    }

    Move[] GetNonDangerousMovesOrderedByPieceImportance(Board board)
    {
        Move[] legalMoves = board.GetLegalMoves();

        List<Move> nonDangerousMoves = new();

        foreach (Move legalMove in legalMoves)
        {
            bool isAttacked = board.SquareIsAttackedByOpponent(legalMove.TargetSquare);

            if (isAttacked)
            {
                continue;
            }

            nonDangerousMoves.Add(legalMove);
        }

        return nonDangerousMoves.OrderByDescending(o => o.MovePieceType).ToArray();
    }

    List<Move> GetNonDangerousAttackingMovesOrderedByPieceImportance(Board board)
    {
        List<Move> ret = new();

        Move[] nonDangerousMoves = GetNonDangerousMovesOrderedByPieceImportance(board);
        Move[] captureMoves = board.GetLegalMoves(true);

        foreach (Move nonDangerousMove in nonDangerousMoves)
        {
            foreach (Move captureMove in captureMoves)
            {
                bool nonDangerousMoveIsCapture = nonDangerousMove.StartSquare == captureMove.StartSquare;

                if (!nonDangerousMoveIsCapture)
                {
                    continue;
                }

                bool isAttacked = board.SquareIsAttackedByOpponent(captureMove.TargetSquare);

                if (isAttacked)
                {
                    continue;
                }

                ret.Add(captureMove);
            }
        }

        return ret;
    }

    List<Move> GetPieceNonDangerousMoves(Board board, Piece piece)
    {
        List<Move> nonDangerousMoves = new();

        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            bool isPiece = move.StartSquare == piece.Square;

            if (!isPiece)
            {
                continue;
            }

            bool isAttacked = board.SquareIsAttackedByOpponent(move.TargetSquare);

            if (isAttacked)
            {
                continue;
            }

            nonDangerousMoves.Add(move);
        }

        return nonDangerousMoves;
    }

    List<Move> GetPieceAttackMoves(Board board, Piece piece)
    {
        List<Move> ret = new();

        Move[] moves = board.GetLegalMoves(true);

        foreach (Move move in moves)
        {
            bool isPiece = move.StartSquare == piece.Square;

            if (!isPiece)
            {
                continue;
            }

            ret.Add(move);
        }

        return ret;
    }
}

public static class Extensions
{
    public static T GetRandom<T>(this List<T> list, Random random)
    {
        int randomNumber = random.Next(0, list.Count);
        return list[randomNumber];
    }

    public static T GetRandom<T>(this T[] array, Random random)
    {
        int randomNumber = random.Next(0, array.Length);
        return array[randomNumber];
    }
}