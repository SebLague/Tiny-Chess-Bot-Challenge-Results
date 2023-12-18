namespace auto_Bot_300;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_300 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> kingMoves = new List<Move>();

        Random rng = new Random();
        var move = evaluateBestMove(moves, board);

        for (int m = 0; m < moves.Length; m++)
        {
            if (moves[m].StartSquare.Name.StartsWith(board.GetKingSquare(board.IsWhiteToMove).Name))
                kingMoves.Add(moves[m]);

            if (isCheckmateMove(moves[m], board))
                move = moves[m];
        }

        if (board.IsInCheck() && kingMoves.Count! > 1)
            move = kingMoves[rng.Next(kingMoves.Count - 1)];

        return move;
    }

    bool isCheckmateMove(Move move, Board board)
    {
        board.MakeMove(move);
        bool isCheckmate = board.IsInCheckmate();
        board.UndoMove(move);
        return isCheckmate;
    }

    Move evaluateBestMove(Move[] moves, Board board)
    {
        Random rng = new Random();
        int[] moveMapping = new[] { 5, 4, 3, 2, 1, 0 };

        var sortedMoves = moves
            .OrderBy(x => moveMapping[(int)(x.CapturePieceType)])
            .ToList();

        bool containsGoodMove = false;
        foreach (Move m in sortedMoves)
        {
            if (m.CapturePieceType != PieceType.None && containsGoodMove == false)
                containsGoodMove = true;
        }

        Move move = sortedMoves[0];

        if (!containsGoodMove)
        {
            move = moves[rng.Next(moves.Length)];
        }

        for (int m = 0; m < moves.Length; m++)
        {
            int moveIndex = rng.Next(moves.Length);
            board.MakeMove(moves[moveIndex]);
            if (board.IsDraw())
            {
                board.UndoMove(moves[moveIndex]);
                continue;
            }
            board.UndoMove(moves[moveIndex]);
            move = moves[moveIndex];
            break;
        }

        return move;
    }
}