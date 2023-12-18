namespace auto_Bot_13;
using ChessChallenge.API;
using System;

public class Bot_13 : IChessBot
{
    public int counter;
    public Move[] myMoves = { };
    public Move Think(Board board, Timer timer)
    {

        if (board.IsWhiteToMove && board.WhitePiecesBitboard == 65535)
        {
            counter = 0;
            myMoves = new Move[3];
            myMoves[0] = new Move("e2e4", board);
            myMoves[1] = new Move("f1c4", board);
            myMoves[2] = new Move("d1h5", board);
        }
        if (!board.IsWhiteToMove & board.BlackPiecesBitboard == 18446462598732840960)
        {
            counter = 0;
            myMoves = new Move[3];
            myMoves[0] = new Move("e7e5", board);
            myMoves[1] = new Move("f8c5", board);
            myMoves[2] = new Move("d8h4", board);
        }

        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {

            if (counter == 3)
            {
                counter++;
                if (board.IsWhiteToMove)
                    return new Move("h5f7", board);
                else
                    return new Move("h4f2", board);
            }
            if (counter < myMoves.Length)
            {
                if (myMoves[counter].Equals(move))
                {
                    counter++;
                    return move;
                }
            }

            if (isCheckmate(board, move))
            {
                return move;
            }
        }

        Random rng = new();
        return moves[rng.Next(moves.Length)];
    }

    private bool isCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool checkmate = board.IsInCheckmate();
        board.UndoMove(move);
        return checkmate;
    }
}