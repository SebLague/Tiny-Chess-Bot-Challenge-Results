namespace auto_Bot_342;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_342 : IChessBot
{
    private Dictionary<ulong, RaveStats> botMemory = new Dictionary<ulong, RaveStats>(); // Store RAVE statistics
    private bool botColor; // 1 for white and 0 for black
    private Move bestMove;

    public Move Think(Board board, Timer timer)
    {
        botColor = board.IsWhiteToMove;
        Move[] moves = board.GetLegalMoves();
        int n = 4000;
        for (int i = 0; i < n; i++)
        {
            Board boardCopy = Board.CreateBoardFromFEN(board.GetFenString());
            UCT(boardCopy);
        }

        double bestValue = board.IsWhiteToMove ? double.MinValue : double.MaxValue;
        bestMove = moves[0]; // Initialize bestMove to the first move

        foreach (Move move in moves)
        {
            double Nparent = botMemory.ContainsKey(board.ZobristKey) ? botMemory[board.ZobristKey].N : 1;
            board.MakeMove(move);
            RaveStats stats = botMemory[board.ZobristKey];
            double N = stats.N;
            double RS = stats.ResultSum;
            // Calculate best ratio
            double nodeValue = RS / N;
            board.UndoMove(move);
            // Update best move if the current move has a better value
            if ((board.IsWhiteToMove && nodeValue > bestValue) || (!board.IsWhiteToMove && nodeValue < bestValue))
            {
                bestValue = nodeValue;
                bestMove = move;
            }
        }
        return bestMove;
    }

    public double Playout(Board _board)
    {
        Board board = Board.CreateBoardFromFEN(_board.GetFenString());
        int cpt = 400;
        Move[] moves = board.GetLegalMoves();
        System.Random rng = new();

        while (cpt > 0 && moves.Length > 0)
        {
            Move randomMove = moves[rng.Next(moves.Length)];
            board.MakeMove(randomMove);
            moves = board.GetLegalMoves();
            cpt--;
        }

        if (board.IsInCheckmate())
        {
            return Convert.ToDouble(!board.IsWhiteToMove); // If white cannot move, they've lost.
        }

        return 0.5; // Game ends in a draw
    }

    public double UCT(Board board)
    {
        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0)
        {
            if (board.IsDraw()) return 0.5;
            return Convert.ToDouble(!board.IsWhiteToMove); // If white cannot move, they've lost.
        }

        double bestValue = board.IsWhiteToMove ? double.MinValue : double.MaxValue;
        bestMove = moves[0]; // Initialize bestMove to the first move


        foreach (Move move in moves)
        {
            double Nparent = botMemory.ContainsKey(board.ZobristKey) ? botMemory[board.ZobristKey].N : 1;

            board.MakeMove(move);
            double nodeValue = RAVE(board, Nparent); // Calculate the RAVE value
            board.UndoMove(move);

            // Update best move if the current move has a better value
            if ((board.IsWhiteToMove && nodeValue > bestValue) || (!board.IsWhiteToMove && nodeValue < bestValue))
            {
                bestValue = nodeValue;
                bestMove = move;
            }
        }

        board.MakeMove(bestMove);
        double result = Playout(board);

        // Update botMemory statistics
        if (botMemory.ContainsKey(board.ZobristKey))
        {
            botMemory[board.ZobristKey].N += 1;
            botMemory[board.ZobristKey].ResultSum += result;
        }
        else
        {
            botMemory[board.ZobristKey] = new RaveStats(1, result);
        }

        return result;
    }

    public double RAVE(Board board, double Nparent)
    {
        if (botMemory.ContainsKey(board.ZobristKey))
        {
            RaveStats stats = botMemory[board.ZobristKey];
            double N = stats.N;
            double RS = stats.ResultSum;

            // Calculate RAVE value
            double value = RS / N + 2 * Math.Sqrt(Math.Log(Nparent) / N);

            return value;
        }
        else
        {
            return Playout(board);
        }

    }
}

public class RaveStats
{
    public double N { get; set; } // Number of times the position has been played
    public double ResultSum { get; set; } // Sum of results for the position

    public RaveStats(double n, double resultSum)
    {
        N = n;
        ResultSum = resultSum;
    }
}