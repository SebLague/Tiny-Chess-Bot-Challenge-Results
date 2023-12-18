namespace auto_Bot_36;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_36 : IChessBot
{
    private int _turn = 0;

    public Move Think(Board board, Timer timer)
    {
        _turn++;

        Move[] moves = board.GetLegalMoves();

        // Always start with f2/f7 > f3/f6
        if (_turn == 1)
        {
            foreach (var move in moves)
            {
                if (move.StartSquare == new Square("f2") || move.StartSquare == new Square("f7"))
                {
                    return move;
                }
            }
        }
        // Follow up with g2/g7 > g4/g5
        else if (_turn == 2)
        {
            foreach (var move in moves)
            {
                if ((move.StartSquare == new Square("g2") && move.TargetSquare == new Square("g4")) || (move.StartSquare == new Square("g7") && move.TargetSquare == new Square("g5")))
                {
                    return move;
                }
            }
        }
        // If initial moves fail to lead to defeat, hang checkmate for several turns while moving pawns
        else if (_turn >= 3 && _turn <= 10)
        {
            foreach (var move in moves)
            {
                if (move.MovePieceType == PieceType.Pawn && move.StartSquare != new Square("f3") && move.StartSquare != new Square("f6") && move.StartSquare != new Square("g4") && move.StartSquare != new Square("g5"))
                {
                    if (!MoveIsCheckmate(board, move))
                    {
                        return move;
                    }
                }
            }
            // pick a random move if a pawn isnt available
            return ReturnRandomMove();
        }
        // If hanging a checkmate till turn 10 doesnt work, just start moving the king
        else
        {
            List<Move> kingMoves = new List<Move>();
            foreach (var move in moves)
            {
                if (move.MovePieceType == PieceType.King)
                {
                    kingMoves.Add(move);
                }
            }

            // Select a random king move to try and prevent stalemate to repetition
            if (kingMoves.Count > 1)
            {
                Move tryMove;
                Random rand = new();
                int attempts = 0;
                while (attempts < 10)
                {
                    tryMove = kingMoves[rand.Next(kingMoves.Count)];
                    if (!tryMove.IsCapture)
                    {
                        return tryMove;
                    }
                    attempts++;
                }
            }
            else if (kingMoves.Count == 1)
            {
                return kingMoves[0];
            }

            // pick a random move if the king cant move
            return ReturnRandomMove();
        }

        return ReturnRandomMove();

        // Test if this move gives checkmate
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        // Get a random move to return
        Move ReturnRandomMove()
        {
            Move tryMove;
            Random rand = new();
            int attempts = 0;
            while (attempts < 100)
            {
                tryMove = moves[rand.Next(moves.Length)];

                if (!tryMove.IsCapture)
                {
                    return tryMove;
                }
                attempts++;
            }
            return moves[rand.Next(moves.Length)];
        }
    }
}