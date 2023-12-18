namespace auto_Bot_467;
using ChessChallenge.API;

public class Bot_467 : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();


        for (int i = 0; i < moves.Length; i++)
        {
            // En Passant is Forced! :)
            if (moves[i].IsEnPassant)
            {
                return moves[i];
            }
            // Play any checkmates
            board.MakeMove(moves[i]);
            if (board.IsInCheckmate())
            {
                return moves[i];
            }
            board.UndoMove(moves[i]);

        }
        //Look x moves ahead based on game state
        int steps = 2;
        if (timer.MillisecondsRemaining < 20000 || board.PlyCount < 6)
        {
            steps = 1;
        }


        int index = 0;
        double best = -1000;
        double test = 0;

        if (moves.Length == 1)
        {
            return moves[0];
        }

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            test = Calculate(board, steps);
            if (test > 900)
            {
                return moves[i];
            }
            test = Modify(board, moves[i], test);
            if (test > best)
            {
                index = i;
                best = test;
            }

            board.UndoMove(moves[i]);
        }

        return moves[index];

    }


    public double Calculate(Board board, int steps)
    {

        Move[] moves = board.GetLegalMoves();
        double best = -1000;
        double test = 0;

        if (board.IsInCheckmate())
        {
            return 1000;
        }

        if (board.IsRepeatedPosition())
        {
            return -1000;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);

            if (steps > 0)
            {
                test = EnemyCalculate(board, steps - 1);
            }
            else
            {
                test = Evaluate(board);

            }

            test = Modify(board, moves[i], test);

            if (test > 900)
            {
                board.UndoMove(moves[i]);
                return -test;
            }

            if (test > best)
            {
                best = test;
            }

            board.UndoMove(moves[i]);
        }

        return -best;
    }

    public double EnemyCalculate(Board board, int steps)
    {

        Move[] moves = board.GetLegalMoves();
        double best = -1000;
        double test = 0;

        if (board.IsInCheckmate())
        {
            return 1000;
        }

        if (board.IsRepeatedPosition() || board.IsDraw())
        {
            return 0;
        }

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);

            if (steps > 0)
            {
                test = Calculate(board, steps);
            }
            else
            {
                test = Evaluate(board);

            }

            test = Modify(board, moves[i], test);

            if (test > best)
            {
                best = test;
            }

            board.UndoMove(moves[i]);
        }

        return -best;
    }

    public double Modify(Board board, Move move, double score)
    {
        double updated = score;
        if (move.IsCapture)
        {
            updated += .01;
        }
        if (board.PlyCount < 100 && (int)move.MovePieceType == 6)
        {
            updated -= .01;
        }
        return updated;
    }

    public double Evaluate(Board board)
    {
        if (board.IsInCheckmate())
        {
            return 1000;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsRepeatedPosition())
        {
            return 0;
        }
        bool Goodcolor = !board.IsWhiteToMove;
        int GoodPawns = board.GetPieceList(PieceType.Pawn, Goodcolor).Count;
        int GoodKnights = board.GetPieceList(PieceType.Knight, Goodcolor).Count;
        int GoodBishops = board.GetPieceList(PieceType.Bishop, Goodcolor).Count;
        int GoodRooks = board.GetPieceList(PieceType.Rook, Goodcolor).Count;
        int GoodQueens = board.GetPieceList(PieceType.Queen, Goodcolor).Count;
        int BadPawns = board.GetPieceList(PieceType.Pawn, !Goodcolor).Count;
        int BadKnights = board.GetPieceList(PieceType.Knight, !Goodcolor).Count;
        int BadBishops = board.GetPieceList(PieceType.Bishop, !Goodcolor).Count;
        int BadRooks = board.GetPieceList(PieceType.Rook, !Goodcolor).Count;
        int BadQueens = board.GetPieceList(PieceType.Queen, !Goodcolor).Count;
        return (GoodPawns * 1.0 + GoodKnights * 3.3 + GoodBishops * 3.5 + GoodRooks * 5.0 + GoodQueens * 9.0) - (BadPawns * 1.0 + BadKnights * 3.3 + BadBishops * 3.5 + BadRooks * 5.0 + BadQueens * 9.0);

    }
}