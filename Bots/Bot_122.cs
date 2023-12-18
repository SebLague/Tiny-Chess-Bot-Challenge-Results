namespace auto_Bot_122;
using ChessChallenge.API;

public class Bot_122 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        //tries for scholars mate with white, falls back to random moves

        Move[] moves = board.GetLegalMoves();
        //return moves[0];

        //fall back to random move, taken from video

        System.Random rng = new();
        Move move = moves[rng.Next(moves.Length)];
        int ply = board.PlyCount;

        Move move1 = new("e2e4", board);
        Move move2 = new("f1c4", board);
        Move move3 = new("d1h5", board);
        Move move4 = new("h5f7", board);

        Move[] scholarsMate = { move1, move2, move3, move4 };

        if (board.IsWhiteToMove && ply < 7)
        {

            //DivertedConsole.Write(board.PlyCount);
            //return moves[0];


            if (isMoveLegal(scholarsMate[ply / 2], moves))
            {
                move = scholarsMate[ply / 2];
            }




        }

        //else
        //{
        //}

        //foreach (Move randMove in moves) //taken from EvilBot.cs
        //{
        //    // Always play checkmate in one
        //    if (MoveIsCheckmate(board, randMove))
        //    {
        //        move = randMove;
        //        break;
        //    }
        //}
        return move;

    }

    bool isMoveLegal(Move move, Move[] legalMoves)
    {
        foreach (Move legal in legalMoves)
        {
            if (legal.Equals(move))
            {
                return true;
            }
        }

        return false;
    }

    //bool MoveIsCheckmate(Board board, Move move) //taken from EvilBot.cs
    //{
    //    board.MakeMove(move);
    //    bool isMate = board.IsInCheckmate();
    //    board.UndoMove(move);
    //    return isMate;
    //}
}