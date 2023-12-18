namespace auto_Bot_22;
using ChessChallenge.API;
using System;

public class Bot_22 : IChessBot
{
    Board board;
    Random rng = new Random();

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        return GenerateMove(board.IsWhiteToMove, (int)Math.Log10(timer.MillisecondsRemaining));
    }

    public Move GenerateMove(bool color, int depth)
    {
        int score;
        int alpha = int.MinValue + 1;
        int beta = int.MaxValue - 1;

        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -AlphaBeta(!color, -beta, -alpha, depth - 1);
            board.UndoMove(move);

            if (score >= beta)
            {
                alpha = score;
                bestMove = move;
                break;
            }
            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    int AlphaBeta(bool color, int alpha, int beta, int depth)
    {

        if (depth == 0) return Quiesce(color, alpha, beta, (int)Math.Sqrt(board.PlyCount) / 3);

        int score;
        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -AlphaBeta(!color, -beta, -alpha, depth - 1);
            board.UndoMove(move);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
    int Quiesce(bool color, int alpha, int beta, int depth)
    {
        int eval = Evaluate(color);

        if (depth == 0) return eval;
        if (eval >= beta) return beta;
        if (alpha < eval) alpha = eval;

        int score;
        Move[] moves = board.GetLegalMoves(true);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Quiesce(!color, -beta, -alpha, depth - 1);
            board.UndoMove(move);

            if (score >= beta) return beta;
            if (score >= alpha) alpha = score;
        }
        return alpha;
    }

    int Evaluate(bool color)
    {

        int eval = 0;
        PieceList[] pieces = board.GetAllPieceLists();

        for (int i = 0; i < 6; i++)
        {
            foreach (Piece piece in pieces[i])
            {
                eval += Values[i];
                eval += PieceSquareTable[i, 4 * (7 - piece.Square.Rank) + PSTIndex[piece.Square.File]];
            }
        }
        for (int i = 0; i < 6; i++)
        {
            foreach (Piece piece in pieces[i + 6])
            {
                eval -= Values[i];
                eval -= PieceSquareTable[i, 4 * piece.Square.Rank + PSTIndex[piece.Square.File]];
            }
        }
        return (color ? eval : -eval) + rng.Next(5) - 2;
    }

    static readonly int[] Values = { 10, 30, 32, 50, 90, 10000 };

    static readonly int[,] PieceSquareTable = {
        {
            5, 5, 5, 5,
            5, 5, 5, 5,
            3, 3, 3, 4,
            1, 1, 1, 3,
            0, 0, 1, 2,
           -1,-1,-1,-2,
            1, 1, 0,-1,
            0, 0, 0, 0,
        },{
            -5,-4,-3,-3,
            -4,-2, 0, 0,
            -3, 0, 1, 1,
            -3, 1, 1, 1,
            -3, 0, 1, 1,
            -3, 1, 1, 1,
            -4,-2, 0, 0,
            -5,-1,-3,-3,
        },{
            -2,-1,-1,-1,
            -1, 0, 0, 0,
            -1, 0, 1, 1,
            -1, 1, 1, 2,
            -1, 0, 2, 1,
            -1, 1, 1, 1,
            -1, 1, 0, 0,
            -2,-1,-1,-1,
        },{
            1, 1, 1, 1,
            1, 2, 2, 2,
           -1, 0, 0, 0,
           -1, 0, 0, 0,
           -1, 0, 0, 0,
           -1, 0, 0, 0,
           -1, 0, 0, 0,
            0,-1, 1, 2,
        }, {
            -2,-1,-1,-1,
            -1, 0, 0, 0,
            -1, 0, 1, 1,
            -2, 0, 1, 1,
            -1,-1, 1, 1,
            -1, 0, 0, 1,
            -1, 0, 0, 0,
            -2,-1,-1, 0,
        },{
            -3,-4,-4,-5,
            -3,-4,-4,-5,
            -3,-4,-4,-5,
            -3,-4,-4,-5,
            -2,-3,-3,-4,
             0,-2,-2,-2,
             2, 2, 0, 0,
             2, 3, 1, 0,
        }
    };
    static readonly int[] PSTIndex = {
        0, 1, 2, 3, 3, 2, 1, 0
    };
}
