namespace auto_Bot_589;
using ChessChallenge.API;
using System;

/* 
 *  Only factors in checkmate
 *  A pure chessbot
 */

public class Bot_589 : IChessBot
{
    private bool AmIWhite;

    private int Eval(Board board, int depth, int depth_limit)
    {
        float s = 0;
        if (board.IsInCheckmate() || board.IsDraw())
        {
            s -= 80;
        }
        if (depth >= depth_limit || s < 0)
        {
            if (AmIWhite == board.IsWhiteToMove)
            {
                return (int)s;
            }
            return (int)(s * -1.295); // Change to make bot more/less Agressive
        }
        int sum = 0;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            sum += Eval(board, depth + 1, depth_limit);
            board.UndoMove(move);


        }

        return sum / board.GetLegalMoves().Length;
    }

    public Move Think(Board board, Timer timer)
    {

        AmIWhite = board.IsWhiteToMove;
        Move[] moves = board.GetLegalMoves();
        Move best = moves[0];
        int best_score = 0;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (Eval(board, 0, 3) > best_score)
            {
                DivertedConsole.Write(best_score);
                best = move;
            }
            board.UndoMove(move);
        }
        return best;
    }
}
