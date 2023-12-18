namespace auto_Bot_19;
using ChessChallenge.API;

public class Bot_19 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return choose(board);
    }

    private int GetCaptureValue(PieceType capturedPieceType, int[] values)
    {
        switch (capturedPieceType)
        {
            case PieceType.Pawn:
                return values[0];
            case PieceType.Rook:
                return values[1];
            case PieceType.Knight:
                return values[2];
            case PieceType.Bishop:
                return values[3];
            case PieceType.Queen:
                return values[4];

            default:
                return 0; // If unknown piece type, return a low value
        }
    }
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool isDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool good = board.IsDraw();
        board.UndoMove(move);
        return good;
    }
    int score(Board board, Move move, int[] values)
    {
        board.MakeMove(move);
        int score = 0;
        int check;
        if (board.IsInCheck())
        {
            check = -1;
        }
        else
        {
            check = 1;
        }
        int hasMovesLeft;
        if (board.GetLegalMoves().Length == 0)
        {
            hasMovesLeft = -1;
        }
        else
        {
            hasMovesLeft = 1;
        }
        int attacked; //board.SquareIsAttackedByOpponent()
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            attacked = -1;
        }
        else
        {
            attacked = 1;
        }
        score += check * 3;
        score += hasMovesLeft * 8;
        score += attacked * 5;
        score += GetCaptureValue(move.CapturePieceType, values);

        board.UndoMove(move);
        return score;
    }
    Move choose(Board board)
    {
        int[] values = { 1, 35, 50, 70, 120, 150 };
        Move[] moves = board.GetLegalMoves();
        Move bestMove = new Move();
        int bestValue = int.MinValue; // Initialize to a very low value

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            if (!(board.GetPiece(move.StartSquare).IsQueen && board.SquareIsAttackedByOpponent(move.TargetSquare)))
            {
                int captureValue = score(board, move, values);
                if (captureValue > bestValue)
                {
                    bestMove = move;
                    bestValue = captureValue;
                }
            }
        }

        Move result;
        if (bestMove != new Move())
        {
            result = bestMove;
        }
        else
        {
            System.Random rnd = new System.Random();
            int a = rnd.Next(moves.Length);
            int i = 0;

            while (!isDraw(board, moves[a]))
            {
                a = rnd.Next(moves.Length);
                if (i > 800)
                {
                    break;
                }
                i++;
            }

            result = moves[a];
        }
        return result;
    }
}
