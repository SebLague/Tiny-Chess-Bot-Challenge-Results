namespace auto_Bot_358;
using ChessChallenge.API;
using System;

public class Bot_358 : IChessBot
{
    public int maxDepth = 3;
    Move bestMove;

    public static int Evaluate(Board board)
    {
        int[] pieceValues = { 100, 310, 330, 500, 900, 99999 };
        int materialVal = 0;
        int mobilityVal = board.GetLegalMoves().Length;
        PieceList[] list = board.GetAllPieceLists();
        int colour = board.IsWhiteToMove ? 1 : -1;

        for (int i = 0; i < 6; i++) materialVal += (list[i].Count - list[i + 6].Count) * pieceValues[i];

        return (int)(colour * (materialVal + mobilityVal / 10.0));
    }

    public int Quiesce(Board board, int alpha, int beta)
    {
        int stand_pat = Evaluate(board);

        if (stand_pat >= beta) return beta;
        if (alpha < stand_pat) alpha = stand_pat;

        Move[] captureMoves = board.GetLegalMoves(true);
        foreach (Move move in captureMoves)
        {
            board.MakeMove(move);
            int score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }


    public int Search(Board board, int depth, int alpha, int beta)
    {
        // If the search reaches the desired depth or the end of the game, evaluate the position and return its value
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return -999999 + (maxDepth - depth);
        if (depth == 0) return Quiesce(board, alpha, beta);

        Move[] legalMoves = board.GetLegalMoves();
        int bestEval = -999999;
        int eval;

        // Generate and loop through all legal moves for the current player
        foreach (Move move in legalMoves)
        {
            // Make the move on a temporary board and call search recursively
            board.MakeMove(move);
            eval = -Search(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            // Update the best move and prune if necessary
            if (eval > bestEval)
            {
                bestEval = eval;

                if (depth == maxDepth) bestMove = move;
                // Improve alpha
                alpha = Math.Max(alpha, eval);

                if (alpha >= beta) break;

            }
        }

        return bestEval;
    }

    public Move Think(Board board, Timer timer)
    {
        Search(board, maxDepth, -999999, 999999);

        return bestMove;
    }
}