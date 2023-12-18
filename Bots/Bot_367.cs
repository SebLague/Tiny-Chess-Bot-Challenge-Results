namespace auto_Bot_367;
using ChessChallenge.API;
using System;

public class Bot_367 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        float maxEval = -1000;
        Move bestMove = Move.NullMove;
        foreach (var item in moves)
        {
            board.MakeMove(item);
            var eval = minimax(board, 3) * -WhiteToMove(board);
            if (eval > maxEval)
            {
                maxEval = eval;
                bestMove = item;
            }
            board.UndoMove(item);
        }
        DivertedConsole.Write(maxEval);
        return bestMove;

    }

    public float Evaluate(Board board)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        if (board.IsInCheckmate())
        {
            return WhiteToMove(board) * -1000;
        }

        else
        {
            if (board.IsDraw())
            {
                return 0;
            }
        }
        return pieces[0].Count + pieces[1].Count * 3 + pieces[2].Count * 3 + pieces[3].Count * 5 + pieces[4].Count * 9 - pieces[6].Count + pieces[7].Count * 3 + pieces[8].Count * 3 + pieces[9].Count * 5 + pieces[10].Count * 9;
    }

    /// <summary>
    /// Returns 1 if it's white's move and -1 if it's black's move.
    /// </summary>
    public int WhiteToMove(Board board)
    {
        return board.IsWhiteToMove == true ? 1 : -1;
    }

    public float minimax(Board board, int depth)
    {
        Move[] moves = board.GetLegalMoves();
        float maxEval = -1000;
        Move bestMove = Move.NullMove;
        if (depth == 0)
        {
            return maxEval;
        }
        foreach (var item in moves)
        {
            board.MakeMove(item);
            var eval = minimax(board, depth - 1) * WhiteToMove(board);
            maxEval = MathF.Max(maxEval, eval);
            board.UndoMove(item);
        }
        return maxEval;
    }
}