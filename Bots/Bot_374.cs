namespace auto_Bot_374;
using ChessChallenge.API;
using System;

public class Bot_374 : IChessBot //jazz 1.2
{
    public int[] pieceValues = { 10, 30, 30, 50, 90, 0 };
    public int getEval(Board board, bool Iswhite)
    {
        int eval = 0;
        PieceList[] pieceslist = board.GetAllPieceLists();
        for (int i = 0; i < pieceslist.Length; i++)
        {
            if (i < pieceslist.Length / 2)
            {
                eval += pieceslist[i].Count * pieceValues[i];
            }
            else
            {
                eval -= pieceslist[i].Count * pieceValues[i - 6];
            }
        }
        if (Iswhite)
        {
            return eval;
        }
        else
        {
            return -eval;
        }
    }
    public Move[] mvvlva(Move[] moves)
    {
        int[] movescores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            movescores[i] = 0;
            if (moves[i].IsCapture)
            {
                movescores[i] = 100 * (int)moves[i].CapturePieceType - (int)moves[i].MovePieceType;
            }
        }
        Array.Sort(movescores, moves);
        Array.Reverse(moves);
        return moves;
    }
    public (Move, int) search(Board board, bool iswhite, int depth, int alpha, int beta)
    {
        if (board.IsInCheckmate())
        {
            return (Move.NullMove, -9999);
        }
        if (board.IsDraw())
        {
            return (Move.NullMove, -1);
        }
        bool qsearch = depth <= 0;
        int maxscore = -100000;
        Move[] moves = board.GetLegalMoves(qsearch);
        if (qsearch)
        {
            int standpat = getEval(board, iswhite);
            if (standpat >= beta)
            {
                return (Move.NullMove, beta);
            }
            if (alpha < standpat)
            {
                alpha = standpat;
            }
        }
        if (moves.Length == 0)
        {
            return ((Move.NullMove, getEval(board, iswhite)));
        }
        Move bestmove = moves[0];
        moves = mvvlva(moves);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -search(board, !iswhite, depth - 1, -beta, -alpha).Item2;
            board.UndoMove(move);
            if (score > maxscore)
            {
                maxscore = score;
                bestmove = move;
            }
            alpha = Math.Max(alpha, maxscore);
            if (alpha >= beta)
            {
                break;
            }
        }
        return (bestmove, maxscore);
    }
    public Move Think(Board board, Timer timer)
    {
        bool white = board.IsWhiteToMove;
        var (bestmove, eval) = search(board, white, 4, -100000, 100000);
        DivertedConsole.Write(eval);
        return bestmove;
    }
}