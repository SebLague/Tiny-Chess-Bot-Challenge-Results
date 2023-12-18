namespace auto_Bot_172;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_172 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move[] pickMoves = board.GetLegalMoves(true);
        Random random = new();
        Move moveToPlay = moves[random.Next(moves.Length)];

        Dictionary<Move, int> takeMoves = new Dictionary<Move, int>();
        Dictionary<Move, int> escapeMoves = new Dictionary<Move, int>();

        List<Move> importance1 = new List<Move>();
        List<Move> importance2 = new List<Move>();
        List<Move> importance3 = new List<Move>();

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            if (isThereCheckmate(board) && isMoveDefendsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            Square? square = isThereAnyOppenentMoveThatTakesOurValuablePiece(board);

            if (square != null)
                if (move.StartSquare == square && squareIsNotAttacked(board, move))
                    escapeMoves.Add(move, pieceValues[(int)board.GetPiece(move.StartSquare).PieceType]);
                else if (isMoveDefendsThreat(board, move, (Square)square) && squareIsNotAttacked(board, move))
                    escapeMoves.Add(move, pieceValues[(int)board.GetPiece((Square)square).PieceType]);

            if (moveIsChecked(board, move) && squareIsNotAttacked(board, move))
                importance1.Add(move);

            if (squareIsNotAttacked(board, move) && moveIsThreatining(board, move))
                importance2.Add(move);
        }

        foreach (Move pickMove in pickMoves)
        {
            if (squareIsNotAttacked(board, pickMove))
                takeMoves.Add(pickMove, pieceValues[(int)board.GetPiece(pickMove.TargetSquare).PieceType]);
            else if (board.GetPiece(pickMove.TargetSquare).PieceType >= board.GetPiece(pickMove.StartSquare).PieceType)
                takeMoves.Add(pickMove, pieceValues[(int)board.GetPiece(pickMove.TargetSquare).PieceType] - pieceValues[(int)board.GetPiece(pickMove.StartSquare).PieceType]);
        }

        var mutualsEscapeTake = takeMoves.Intersect(escapeMoves);
        var mutuals12 = importance1.Intersect(importance2);
        if (mutualsEscapeTake.Any())
            moveToPlay = mutualsEscapeTake.First().Key;
        else if (takeMoves.Count > 0 && escapeMoves.Count > 0)
            foreach (KeyValuePair<Move, int> takeMove in takeMoves)
                foreach (KeyValuePair<Move, int> escapeMove in escapeMoves)
                    if (takeMove.Value >= escapeMove.Value)
                        moveToPlay = takeMove.Key;
                    else
                        moveToPlay = escapeMove.Key;

        else if (takeMoves.Any())
            moveToPlay = takeMoves.OrderByDescending(x => x.Value).First().Key;
        else if (escapeMoves.Any())
            moveToPlay = escapeMoves.OrderByDescending(x => x.Value).First().Key;
        else if (mutuals12.Any())
            moveToPlay = mutuals12.First();
        else if (importance1.Any())
            moveToPlay = importance1[0];
        else if (importance2.Any())
            moveToPlay = importance2[0];

        return moveToPlay;
    }

    bool squareIsNotAttacked(Board board, Move moveToPlay)
    {
        bool isNotAttacking = true;
        board.MakeMove(moveToPlay);
        Move[] moves = board.GetLegalMoves(true);
        foreach (Move move in moves)
            if (move.TargetSquare == moveToPlay.TargetSquare)
            {
                isNotAttacking = false;
                break;
            }

        board.UndoMove(moveToPlay);
        return isNotAttacking;
    }

    bool moveIsThreatining(Board board, Move move)
    {
        bool threaten = false;
        Square ourPieceSquare = move.TargetSquare;
        board.MakeMove(move);
        if (board.TrySkipTurn())
        {
            Move[] ourMoves = board.GetLegalMoves(true);
            foreach (Move ourMove in ourMoves)
                if (ourMove.StartSquare == ourPieceSquare && board.GetPiece(ourMove.TargetSquare).PieceType >= board.GetPiece(ourMove.StartSquare).PieceType)
                    threaten = true;
            board.UndoSkipTurn();
        }
        board.UndoMove(move);

        return threaten;
    }

    bool moveIsChecked(Board board, Move move)
    {
        board.MakeMove(move);
        if (board.IsInCheck())
        {
            board.UndoMove(move);
            return true;
        }
        else
        {
            board.UndoMove(move);
            return false;
        }
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool isThereCheckmate(Board board)
    {
        bool checkmate = false;
        if (board.TrySkipTurn())
        {
            Move[] opponentMoves = board.GetLegalMoves();
            foreach (var opponentMove in opponentMoves)
            {
                board.MakeMove(opponentMove);
                if (board.IsInCheckmate())
                {
                    checkmate = true;
                    board.UndoMove(opponentMove);
                    break;
                }
                else
                    board.UndoMove(opponentMove);
            }
            board.UndoSkipTurn();
        }
        return checkmate;
    }

    bool isMoveDefendsCheckmate(Board board, Move move)
    {
        bool isDefends = true;
        board.MakeMove(move);
        Move[] opponentMoves = board.GetLegalMoves();
        foreach (Move opponentMove in opponentMoves)
        {
            board.MakeMove(opponentMove);
            if (board.IsInCheckmate())
            {
                isDefends = false;
                board.UndoMove(opponentMove);
                break;
            }
            else
                board.UndoMove(opponentMove);
        }
        board.UndoMove(move);

        return isDefends;
    }

    bool isMoveDefendsThreat(Board board, Move move, Square dangerousSquare)
    {
        bool isDefendsThreat = true;
        board.MakeMove(move);
        Move[] opponentMoves = board.GetLegalMoves(true);
        foreach (Move opponentMove in opponentMoves)
            if (opponentMove.TargetSquare == dangerousSquare)
                isDefendsThreat = false;

        board.UndoMove(move);
        return isDefendsThreat;
    }

    Square? isThereAnyOppenentMoveThatTakesOurValuablePiece(Board board)
    {
        if (board.TrySkipTurn())
        {
            Move[] opponentMoves = board.GetLegalMoves(true);
            Square? dangerousSquare = null;
            PieceType dangerousValue = 0;
            foreach (var opponentMove in opponentMoves)
            {
                PieceType pieceTake = board.GetPiece(opponentMove.TargetSquare).PieceType;

                if (pieceTake > dangerousValue)
                {
                    dangerousValue = pieceTake;
                    dangerousSquare = opponentMove.TargetSquare;
                }
            }
            board.UndoSkipTurn();
            return dangerousSquare;
        }
        else
            return null;
    }
}