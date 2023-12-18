namespace auto_Bot_326;
using ChessChallenge.API;

public class Bot_326 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int bestMoveEval = 1000000 * (board.IsWhiteToMove ? -1 : 1);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int moveEval = AlphaBeta(board, 3, int.MinValue, int.MaxValue, board.IsWhiteToMove);
            board.UndoMove(move);
            if ((moveEval > bestMoveEval && board.IsWhiteToMove) || (moveEval <= bestMoveEval && !board.IsWhiteToMove))
            {
                bestMove = move;
                bestMoveEval = moveEval;
            }
        }
        // DivertedConsole.Write(bestMoveEval);
        return bestMove;
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return Eval(board);
        }

        if (maximizingPlayer)
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                if (eval >= beta)
                {
                    return beta;
                }
                if (eval >= alpha)
                {
                    alpha = eval;
                }
            }
            return alpha;
        }
        else
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                if (eval <= alpha)
                {
                    return alpha;
                }
                if (eval <= beta)
                {
                    beta = eval;
                }
            }
            return beta;
        }
    }

    private int Eval(Board board)
    {
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -10000 : 10000;
        }
        int eval = 0;
        int[] values = { 0, 100, 300, 300, 500, 900, 0 };
        foreach (PieceList pList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pList)
            {
                if (piece.IsWhite)
                {
                    eval += values[((int)piece.PieceType)];
                }
                else
                {
                    eval -= values[((int)piece.PieceType)];
                }
            }
        }
        return eval;
    }
}
