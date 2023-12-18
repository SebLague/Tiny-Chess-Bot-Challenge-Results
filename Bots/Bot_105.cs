namespace auto_Bot_105;
using ChessChallenge.API;

public class Bot_105 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 400, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = new();
        int bestValue = -2147483648;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);

            if (isMate) { return move; }

            int value =
                2 * pieceValues[(int)move.CapturePieceType] -
                pieceValues[(int)move.MovePieceType] +
                pieceValues[(int)move.PromotionPieceType] +
                (move.IsEnPassant ? 200 : 0) +
                (move.IsCastles ? 150 : 0);

            if (value > bestValue)
            {
                bestMove = move;
                bestValue = value;
            }
        }

        return bestMove;
    }
}
