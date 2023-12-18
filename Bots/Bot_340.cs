namespace auto_Bot_340;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_340 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> safe_moves = new List<Move>();

        foreach (Move move in moves)
        {
            //Avoid killing other pieces
            if (move.IsCapture) continue;

            //Since the move won't kill a piece, it can be added to the list of safe moves.
            safe_moves.Add(move);

            //Check for checkmate
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                return move;
            }
            board.UndoMove(move);
        }

        var rand = new Random();

        //Oh no! The bot might have to fight!
        if (safe_moves.Count <= 0)
        {

            foreach (var move in moves)
            {
                board.MakeMove(move);
                if (!board.TrySkipTurn() || !board.IsInCheck())
                {
                    //Resort to violence
                    return move;
                }
                board.UndoSkipTurn();
                board.UndoMove(move);
            }

        }

        //Find best move. Start with placeholder
        Move best_move = Move.NullMove;
        Double best_move_quality = -999; //Quality of best move
        bool makes_check = false;

        while (safe_moves.Count > 0)
        {
            var move = safe_moves[rand.Next(safe_moves.Count)];
            var move_quality = getMoveQuality(board, move);

            //Simulate the movement
            board.MakeMove(move);
            var in_check = board.IsInCheck();
            board.UndoMove(move);

            //Prioritize moves that places enemy in check
            if (in_check != makes_check)
            {
                if (!in_check)
                {
                    safe_moves.Remove(move);
                    continue;
                }
            }

            //Check if move is better
            if (move_quality < best_move_quality)
            {
                safe_moves.Remove(move);
                continue;
            }

            makes_check = in_check;
            best_move = move;
            best_move_quality = move_quality;

            safe_moves.Remove(move);
        }
        return best_move;
    }

    static double getDistance(Square a, Square b)
    {
        var difference_x = Math.Abs(a.Rank - b.Rank);
        var difference_y = Math.Abs(a.File - b.File);
        return Math.Sqrt(difference_x + difference_y);
    }

    //Find best move towards enemy king. Returns move quality, higher == better.
    double getMoveQuality(Board board, Move move)
    {
        //Different function for king
        if (move.MovePieceType == PieceType.King)
        {
            return kingMoveQuality(board, move);
        }

        var king_square = board.GetKingSquare(!board.IsWhiteToMove); //Find enemy king
        var current_distance = getDistance(move.StartSquare, king_square); //Current distance to king
        var move_distance = getDistance(move.TargetSquare, king_square); //Distance to king after move.

        //Don't want to crowd the king too much.
        if (move_distance < 2) return 0;



        //Return how much closer to king the move would get you
        return current_distance - move_distance;
    }

    int getNeighbouringEnemies(Board board, Square square)
    {
        int enemies = 0;
        for (int x = -1; x < 2; x++)
        {
            for (int y = -1; y < 2; y++)
            {
                var sqr = new Square(square.Rank + x, square.File + y);
                if (sqr.Index < 0 || sqr.Index > 63) continue;
                if (board.GetPiece(sqr).IsWhite != board.IsWhiteToMove) { enemies++; } //Check if enemy
            }
        }
        return enemies;
    }

    double kingMoveQuality(Board board, Move move)
    {
        //Compare how many enemies currently surround the king, with how many will surround the king
        var current_enemies = getNeighbouringEnemies(board, move.StartSquare);
        var new_enemies = getNeighbouringEnemies(board, move.TargetSquare);

        return current_enemies - new_enemies;
    }
}