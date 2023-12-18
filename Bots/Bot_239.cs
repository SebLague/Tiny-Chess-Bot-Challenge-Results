namespace auto_Bot_239;
using ChessChallenge.API;
using System;

public class Bot_239 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int universalDepth = 5;
    Move bestmove;

    public Move Think(Board board, Timer timer)
    {
        NegaMax(board, universalDepth, -1000000, 1000000);
        return bestmove;
    }

    int NegaMax(Board board, int depth, int alpha, int beta)
    {
        int score = 0;

        if (board.IsInCheckmate())
            return -99999 + board.PlyCount;

        if (board.IsDraw())
            return 0;

        if (depth == 0)
            return Quiesce(board, alpha, beta);

        foreach (Move move in OrderMoves(board, board.GetLegalMoves()))
        {
            board.MakeMove(move);
            score = -NegaMax(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;
            if (score > alpha)
            {
                alpha = score;
                if (depth == universalDepth)
                    bestmove = move;

            }
        }
        return alpha;
    }


    int Evaluate(Board board)
    {
        int colorMult, score = 0;

        foreach (PieceList piecelist in board.GetAllPieceLists())
        {
            colorMult = piecelist.IsWhitePieceList == board.IsWhiteToMove ? 1 : -1;
            score += piecelist.Count * colorMult * pieceValues[(int)(piecelist.TypeOfPieceInList)];
        }
        return score;
    }

    Move[] OrderMoves(Board board, Move[] moves)
    {
        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            moveScores[i] = 0;

            if (move.IsCapture)
                moveScores[i] += 100 * (int)move.CapturePieceType - (int)move.MovePieceType;

            if (move.IsPromotion)
                moveScores[i] += 100000;

            board.MakeMove(move);
            if (board.IsInCheckmate())
                moveScores[i] += 100000000;
            if (board.IsInCheck())
                moveScores[i] += 1000;
            board.UndoMove(move);
        }

        // Sort highest scored moves first
        Array.Sort(moveScores, moves);
        Array.Reverse(moves);
        return moves;
    }

    int Quiesce(Board board, int alpha, int beta)
    {
        int stand_pat = Evaluate(board);
        if (stand_pat >= beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        foreach (Move move in OrderMoves(board, board.GetLegalMoves(true)))
        {
            board.MakeMove(move);
            int score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }
        return alpha;
    }
}