namespace auto_Bot_27;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

/*
 * THE PanicKing chessbot is designed to lose.  The King is claustrophobic and will prefer to get clear of his own team.  
 * He will prefer to be near enemy pieces.  Once he only has a few pieces remaining, he will also seek board edges or corners.
 */


public class Bot_27 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Console.Clear();

        Move[] moves = board.GetLegalMoves();


        Move worstMove = AssessMoves(board, moves);
        return worstMove;
    }

    private int AssessKingPosition(Board board)
    {


        Square kingSquare = board.GetKingSquare(!board.IsWhiteToMove);
        int file = kingSquare.File;
        int rank = kingSquare.Rank;
        List<Square> squaresList = new List<Square>();

        int[] offsets = { -2, -1, 0, 1, 2 }; // Represents the offsets for both files and ranks

        foreach (int fileOffset in offsets)
        {
            foreach (int rankOffset in offsets)
            {
                if (fileOffset == 0 && rankOffset == 0)
                    continue; // Skip the case when both offsets are 0, which represents the kingSquare itself

                int newFile = file + fileOffset;
                int newRank = rank + rankOffset;

                Square newSquare = new Square(newFile, newRank);
                if (isValid(newSquare))
                {
                    squaresList.Add(newSquare);
                }
            }
        }

        Square[] squares = squaresList.ToArray();

        bool isValid(Square square)
        {
            return (0 <= square.File && square.File <= 7 && 0 <= square.Rank && square.Rank <= 7);
        }


        //get number of own pieces remaining
        int pawns = board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove).Count;
        int rooks = board.GetPieceList(PieceType.Rook, !board.IsWhiteToMove).Count;
        int knights = board.GetPieceList(PieceType.Knight, !board.IsWhiteToMove).Count;
        int bishops = board.GetPieceList(PieceType.Bishop, !board.IsWhiteToMove).Count;
        int queen = board.GetPieceList(PieceType.Queen, !board.IsWhiteToMove).Count;
        int teamSize = pawns + rooks + knights + bishops + queen;

        int mostValidSquares = 24;
        int outOfBounds = mostValidSquares - squares.Length;
        int positionScore = 0;
        if (teamSize > 3)
        {
            positionScore = 0 - outOfBounds;
        }
        else
        {
            positionScore = 240 - outOfBounds;
        }

        foreach (Square square in squares)
        {

            if (board.GetPiece(square).PieceType == PieceType.None)
            {
                positionScore += 0;
            }
            else if (board.IsWhiteToMove != board.GetPiece(square).IsWhite)
            {
                positionScore -= 3;
            }
            else
            {
                positionScore += 1;
            }

        }
        return positionScore;
    }

    private Move AssessMoves(Board board, Move[] moves)
    {
        int bestMoveScore = -100;
        Move bestMove = moves[0];
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int moveScore = AssessKingPosition(board);
            board.UndoMove(move);
            if (moveScore > bestMoveScore)
            {
                bestMove = move;
                bestMoveScore = moveScore;
            }


        }
        DivertedConsole.Write($"Move was {bestMove}, with a score of {bestMoveScore}");
        return bestMove;
    }

}

