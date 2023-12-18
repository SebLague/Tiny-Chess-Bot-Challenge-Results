namespace auto_Bot_630;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_630 : IChessBot
{
    // Variable initialization
    public PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen };
    public int[] pieceValues = { 100, 290, 310, 515, 1100 };
    public Move currentBestMove;
    public int currentBestEvaluation;

    // Transposition table
    private const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;
    public struct Transposition
    {
        public Transposition(ulong zhash, int eval, byte d, Move m)
        {
            zobristHash = zhash;
            evaluation = eval;
            depth = d;
            flag = -2;
            move = m;
        }
        public Move move;
        public ulong zobristHash = 0;
        public int evaluation = 0;
        public byte depth = 0;
        public sbyte flag = -2;
    }

    static ulong Transposition_Mask = 0x7FFFFF;

    private Transposition[] TranspositionTable = new Transposition[Transposition_Mask + 1];

    // Determine which side is winning a given position
    int boardEval(Board board, int depth)
    {
        if (board.IsInCheckmate())
        {
            return (-9999995 + depth);
        }
        if (board.IsDraw())
        {
            return 0;
        }

        int eval = 0;

        for (int i = 0; i < 5; i++)
        {
            eval += BitOperations.PopCount(board.GetPieceBitboard(pieceTypes[i], board.IsWhiteToMove)) * pieceValues[i] - BitOperations.PopCount(board.GetPieceBitboard(pieceTypes[i], !board.IsWhiteToMove)) * pieceValues[i];
        }

        eval += board.GetLegalMoves().Length;

        if (board.IsInCheck()) eval -= 10;

        return eval;
    }

    int search(Board board, int alpha, int beta, int ply, int depth, int extensionsLeft, bool qsearch)
    {
        // end of game scenarios
        if (board.IsInCheckmate()) return -9999995 + depth;
        if (board.IsDraw()) return 0;

        // variables
        int score = -9999999;
        bool isCheck = board.IsInCheck();

        // go through the legal moves and sort them using MVV LVA move ordering
        Move[] legalMoves = board.GetLegalMoves();
        int[] scores = new int[legalMoves.Length];
        for (int i = 0; i < legalMoves.Length; i++) scores[i] = mvvlva(legalMoves[i]);
        Array.Sort(scores, legalMoves);
        Array.Reverse(legalMoves);
        int eval = boardEval(board, depth);

        if (eval < alpha - (100 * depth)) return eval;

        if (qsearch)
        {
            if (eval > beta) return beta;
            if (eval > alpha) alpha = eval;
        }

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            if (!qsearch || move.IsCapture || move.IsPromotion || isCheck || (board.IsInCheck() && extensionsLeft > 0))
            {
                score = -search(board, -beta, -alpha, ply, depth + 1, extensionsLeft - (board.IsInCheck() && qsearch ? 1 : 0), depth + 1 >= ply);
            }
            board.UndoMove(move);
            if (score >= beta) return beta;
            if (score > alpha)
            {
                alpha = score;
                if (depth == 0) currentBestMove = move;
            }
        }

        return alpha;

        // expand the search tree
        /*if (qsearch)
        {
            int eval = boardEval(board, depth);
            if (eval > beta) return beta;
            if (eval > alpha) alpha = eval;

            foreach (Move qmove in legalMoves)
            {
                board.MakeMove(qmove);
                if (qmove.IsCapture || qmove.IsPromotion || isCheck || (board.IsInCheck() && extensionsLeft > 0))
                {
                    score = -search(board, -beta, -alpha, ply, depth + 1, extensionsLeft - (board.IsInCheck() ? 1 : 0), true);
                }
                board.UndoMove(qmove);
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }

            return alpha;
        }
        else
        {
            foreach(Move move in legalMoves)
            {
                board.MakeMove(move);
                score = -search(board, -beta, -alpha, ply, depth + 1, 2, depth+1 >= ply);
                board.UndoMove(move);
                if (score >= beta) return beta;
                if (score > alpha)
                {
                    alpha = score;
                    if (depthZero)
                    {
                        currentBestMove = move;
                        currentBestEvaluation = score;
                    }
                }
            }
        }
        return score;*/
    }

    int mvvlva(Move move) { return (int)move.CapturePieceType * 100 - (int)move.MovePieceType; }

    // Returns the move decided to be the best (this is the function that will be called by the rest of the program)
    public Move Think(Board board, Timer timer)
    {
        currentBestEvaluation = -9999999;
        for (int i = 1; i < 6; i++)
        {
            currentBestEvaluation = search(board, -9999999, 9999999, i, 0, 2, false);
        }

        // Actually return the best move
        return currentBestMove;
    }
}