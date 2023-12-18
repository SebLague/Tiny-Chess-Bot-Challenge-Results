namespace auto_Bot_466;
using ChessChallenge.API;

public class Bot_466 : IChessBot
{
    private short[] pieceWeights = { 0, 1, 3, 3, 5, 9, 255 };
    private short checkWeight = 8;
    private short pawnAdvanceWeight = 1;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        double bestWeight = 0;
        Move bestMove = moves[0];
        foreach (Move move in moves)
        {
            double weight = CalculateWeight(board, move);
            if (weight > bestWeight)
            {
                bestWeight = weight;
                bestMove = move;
            }
        }

        return bestMove;
    }

    private double CalculateWeight(Board board, Move move)
    {
        System.Random rng = new();
        double weight = rng.NextDouble();
        bool safe = IsMoveSafe(board, move);

        if (IsDrawMove(board, move))
        {
            return double.MinValue;
        }

        if (move.IsCapture)
        {
            weight += pieceWeights[(int)move.CapturePieceType];
        }

        if (move.MovePieceType == PieceType.Pawn)
        {
            weight += pawnAdvanceWeight;
        }

        if (!safe)
        {
            weight -= pieceWeights[(int)move.MovePieceType];
        }
        else
        {
            if (move.IsPromotion)
            {
                weight += pieceWeights[(int)move.PromotionPieceType];
            }

            if (IsCheckmateMove(board, move))
            {
                return double.MaxValue;
            }
            else if (IsCheckMove(board, move))
            {
                weight += checkWeight;
            }
        }

        return weight;
    }

    private bool IsCheckmateMove(Board board, Move move)
    {
        bool check;
        board.MakeMove(move);
        check = board.IsInCheckmate();
        board.UndoMove(move);
        return check;
    }

    private bool IsCheckMove(Board board, Move move)
    {
        bool check;
        board.MakeMove(move);
        check = board.IsInCheck();
        board.UndoMove(move);
        return check;
    }

    private bool IsDrawMove(Board board, Move move)
    {
        bool draw;
        board.MakeMove(move);
        draw = board.IsDraw();
        board.UndoMove(move);
        return draw;
    }

    private bool IsMoveSafe(Board board, Move move)
    {
        bool safe = !board.SquareIsAttackedByOpponent(move.TargetSquare);
        if (!safe)
        {
            board.MakeMove(move);
            safe = board.SquareIsAttackedByOpponent(move.TargetSquare);
            board.UndoMove(move);
        }

        return safe;
    }
}