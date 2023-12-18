namespace auto_Bot_388;
using ChessChallenge.API;

public class Bot_388 : IChessBot
{
    // Piece values: pawn, knight, bishop, rook, queen, king
    float[] pieceValues = { 100.0f, 300.0f, 300.0f, 500.0f, 900.0f, 10000.0f, -100.0f, -300.0f, -300.0f, -500.0f, -900.0f, -10000.0f };
    float[] pawnScaleFactor = { 0.05f, 0.02f, 0.03f, 0.1f, 0.1f, 0.01f, 0.02f, 0.05f };
    float[] pawnRanks = { 0.0f, 0.2f, -0.5f, 0.3f, 1.3f, 2.0f, 4.0f, 0.0f };

    public Move Think(Board board, Timer timer)
    {
        // Null move is the worst thing
        Move bestMove = Move.NullMove;
        float bestEval = -999999;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int depth = timer.MillisecondsRemaining > 10000 ? 3 : 2;

            float myEval = -1 * alphaBeta(board, depth, depth + 2, -9999999, 9999999, timer);
            if (myEval > bestEval)
            {
                bestEval = myEval;
                bestMove = move;
            }

            board.UndoMove(move);
        }

        return bestMove;
    }

    private float getPositionEval(Board board)
    {
        float eval = 0;
        PieceList[] myPieceLists = board.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            eval += myPieceLists[i].Count * pieceValues[i] * 1.2f;
        }
        foreach (Piece pawn in myPieceLists[0])
        {
            eval += pawnScaleFactor[pawn.Square.File] * pawnRanks[pawn.Square.Rank];
        }
        foreach (Piece pawn in myPieceLists[6])
        {
            eval -= pawnScaleFactor[pawn.Square.File] * pawnRanks[7 - pawn.Square.Rank];
        }

        return eval * (board.IsWhiteToMove ? 1 : -1);
    }

    private float alphaBeta(Board board, int depth, int depthLimit, float alpha, float beta, Timer timer)
    {
        if (depth == 0)
        {
            return getPositionEval(board);
        }

        // TODO implement checkmate and draws here instead of in the loop?
        if (board.GetLegalMoves().Length == 0 && !board.IsInCheckmate()) { return 0; }

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            float eval;
            if (board.IsInCheckmate())
            {
                eval = 99999;
            }
            else if (board.IsDraw())
            {
                eval = 0;
            }
            else
            {
                int newDepth = depth + determineExtension(board) - 1;
                eval = -1 * alphaBeta(board, newDepth < depthLimit ? newDepth : depthLimit, depthLimit - 1, -beta, -alpha, timer);
            }

            if (eval >= beta)
            {
                board.UndoMove(move);
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
            }

            board.UndoMove(move);
        }

        return alpha;
    }

    private int determineExtension(Board board)
    {
        if (board.IsInCheck() || board.GetLegalMoves().Length == 1)
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }
}