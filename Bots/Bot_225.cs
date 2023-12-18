namespace auto_Bot_225;
using ChessChallenge.API;

public class Bot_225 : IChessBot
{
    readonly int[] pieceValues = {
        0,
        100,
        300,
        300,
        500,
        900,
        10000
    };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];

        int minimalOpponentMoves = 1000000000;

        foreach (Move move in allMoves)
        {
            if (MoveLeadsToCheckmate(board, move))
            {
                board.UndoMove(move);
                continue;
            }

            board.MakeMove(move);

            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                moveToPlay = move;

                break;
            }

            if (minimalOpponentMoves > board.GetLegalMoves().Length)
            {
                minimalOpponentMoves = board.GetLegalMoves().Length;
                moveToPlay = move;
            }

            board.UndoMove(move);
        }

        return moveToPlay;
    }

    bool MoveLeadsToCheckmate(Board board, Move move)
    {
        board.MakeMove(move);

        Move[] allMoves = board.GetLegalMoves();

        foreach (Move _move in allMoves)
        {
            board.MakeMove(_move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(_move);
                return true;
            }

            board.UndoMove(_move);
        }

        board.UndoMove(move);

        return false;
    }
}