namespace auto_Bot_8;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_8 : IChessBot
{
    private Dictionary<PieceType, int> pieceValues;

    private int moveNum = 0;

    public Bot_8()
    {
        // Populate piece costs
        pieceValues = new Dictionary<PieceType, int>();
        pieceValues.Add(PieceType.King, 10000);
        pieceValues.Add(PieceType.Queen, 500);
        pieceValues.Add(PieceType.Knight, 150);
        pieceValues.Add(PieceType.Rook, 100);
        pieceValues.Add(PieceType.Bishop, 100);
        pieceValues.Add(PieceType.Pawn, 10);
    }

    public Move Think(Board board, Timer timer)
    {
        // Determine if normal board start
        if (moveNum == 0)
        {
            if (!board.IsWhiteToMove && ((board.AllPiecesBitboard & 0xFFFF000000000000) != 0xFFFF000000000000))
            {
                moveNum += 10;
            }
            else if (board.IsWhiteToMove && board.AllPiecesBitboard != 0xFFFF00000000FFFF)
            {
                moveNum += 10;
            }
        }

        // Standard starting moves
        if (moveNum == 0)
        {
            moveNum++;
            if (board.IsWhiteToMove)
            {
                return new Move("b1c3", board);
            }
            else
            {
                return new Move("b8c6", board);
            }
        }
        else if (moveNum == 1)
        {
            moveNum++;
            if (board.IsWhiteToMove)
            {
                return new Move("g1f3", board);
            }
            else
            {
                return new Move("g8f6", board);
            }
        }
        else if (moveNum == 2)
        {
            moveNum++;
            if (board.IsWhiteToMove)
            {
                return new Move("b2b3", board);
            }
            else
            {
                return new Move("b7b6", board);
            }
        }
        else if (moveNum == 3)
        {
            moveNum++;
            if (board.IsWhiteToMove)
            {
                return new Move("g2g3", board);
            }
            else
            {
                return new Move("g7g6", board);
            }
        }

        // Get legal moves
        Move[] legalMoves = board.GetLegalMoves();
        Dictionary<Move, int> rankedMoves = new Dictionary<Move, int>();

        // Determine the danger value of each move
        foreach (Move move in legalMoves)
        {
            // Temprarily make the move
            board.MakeMove(move);
            int dangerScore = 0;

            // Iterate over the board and look for friendly pieces
            for (char i = 'a'; i <= 'h'; i++)
            {
                for (char j = '1'; j <= '8'; j++)
                {
                    Square s = new Square(i.ToString() + j);
                    Piece p = board.GetPiece(s);
                    if (p.PieceType != PieceType.None)
                    {
                        // Determine if the piece is ours
                        if (board.IsWhiteToMove == p.IsWhite)
                        {
                            // Add appropriate danger score
                            if (board.SquareIsAttackedByOpponent(s))
                            {
                                dangerScore += pieceValues[p.PieceType];
                            }
                        }
                    }
                }
            }

            // Undo the move and add the result to the dictionary
            board.UndoMove(move);
            rankedMoves.Add(move, dangerScore);
        }

        int lowest = Int32.MaxValue;
        Move lowestMove = legalMoves[0];
        foreach (KeyValuePair<Move, int> m in rankedMoves)
        {
            if (m.Value < lowest)
            {
                lowest = m.Value;
                lowestMove = m.Key;
            }
        }

        return lowestMove;
    }
}