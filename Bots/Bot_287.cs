namespace auto_Bot_287;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_287 : IChessBot
{

    // The concept of this bot is simple, a novel approach of greedy playouts supported by an odd neural network.
    // Given how small, fast and efficient this bot is, it is unreasonably good at playing chess, though will be trounced by any bot that can look more than about 3 moves ahead. 

    // Leaky Relu

    private static double Hidden(double x)
    {
        return Math.Max(x * 0.1, x);
    }

    // Sigmoid

    private static double Sigmoid(double x)
    {
        double z = Bot_287.Exp(-x);
        return 1.0 / (1.0 + z);
    }

    // Accurate and fast approximation of Exp(...)
    // Understanding why this works is left as an exercise to the intelligent reader

    private static double Exp(double n)
    {
        n = (1.0 + n / 1024.0);
        n *= n; n *= n; n *= n; n *= n;
        n *= n; n *= n; n *= n; n *= n;
        n *= n; n *= n; // Same as ^1024
        return n;
    }

    private static readonly Random RANDOM = new();
    private bool amIWhite = true;

    public Move Think(Board map, Timer timer)
    {
        Move[] moves = map.GetLegalMoves();

        amIWhite = map.IsWhiteToMove;

        double maxScore = double.NegativeInfinity;
        List<Move> maxMoves = new();

        foreach (Move move in moves)
        {
            map.MakeMove(move);

            try
            {
                double getScore = PlayOut(map);
                if (getScore > maxScore)
                {
                    maxScore = getScore;
                    maxMoves.Clear();
                    maxMoves.Add(move);
                }
                else if (getScore == maxScore)
                {
                    maxMoves.Add(move);
                }
            }
            finally
            {
                map.UndoMove(move);
            }
        }

        return ChooseMove(maxMoves);
    }

    // The special sauce, the scoring function
    // Nothing radical here, 99% of the time we're calling GetWinChance(...)

    private static double GetScore(Board map, bool getWhiteScore)
    {
        if (map.IsInCheckmate())
        {
            // Is there anything worse than losing a game of chess?
            if (map.IsWhiteToMove)
            {
                return getWhiteScore ? double.NegativeInfinity : double.PositiveInfinity;
            }
            else
            {
                return getWhiteScore ? double.PositiveInfinity : double.NegativeInfinity;
            }
        }

        if (map.IsDraw())
        {
            return 0.5;
        }
        else
        {
            return GetWinChance(map, getWhiteScore);
        }
    }

    private static int GetPieceCount(Board map, PieceType type)
    {
        return map.GetPieceList(type, true).Count - map.GetPieceList(type, false).Count;
    }

    // Estimated chance of a given coloured piece winning

    private static double GetWinChance(Board map, bool gettingWhiteChance)
    {
        double wPN = GetPieceCount(map, PieceType.Pawn);
        double wKN = GetPieceCount(map, PieceType.Knight);
        double wBH = GetPieceCount(map, PieceType.Bishop);
        double wRK = GetPieceCount(map, PieceType.Rook);
        double wQN = GetPieceCount(map, PieceType.Queen);
        double getChance = GetWinChance(wPN, wKN, wBH, wRK, wQN);
        return gettingWhiteChance ? getChance : (1.0 - getChance); // Black Win Chance = (1.0 - White Win Chance), obviously
    }

    // Imagine this as a single-neuron, single-layer neural network (if such a thing can even exists)
    // Weights come from a custom dataset of Monte-Carlo chess playouts using simulated Darwinian Evolution (in a custom Java chess engine I made)
    // The idea is simple: material advantage is the strongest single indicator of who is winning the game
    // This technique obviously ignores things like position advantage etc. but those are like, hard to measure so w/e

    private static double GetWinChance(double wPN, double wKN, double wBH, double wRK, double wQN)
    {
        // Chance of White winning: 0 = certain loss, 1 = certain win, 0.5 = draw
        return Sigmoid(
          +2.126051
          + Hidden(
              -6.571200
              + (wPN * +0.395870)
              + (wKN * +0.590101)
              + (wBH * +0.918704)
              + (wRK * +1.738920)
              + (wQN * +3.711245)
          ) * +3.260494
      );
    }

    private double PlayOut(Board map)
    {
        return PlayOut(map, 2); // Depth value of two was found through experimental analysis to be the best balance between short-term and long-term play
    }

    private double PlayOut(Board map, int depth)
    {
        bool isFinished = (depth == 0) || map.IsDraw() || map.IsInCheckmate();
        if (isFinished)
        {
            return GetScore(map, amIWhite);
        }

        Move move = GetGreedyMove(map);

        map.MakeMove(move);
        try
        {
            return PlayOut(map, depth - 1);
        }
        finally
        {
            map.UndoMove(move);
        }
    }

    // Get the greediest possible move according to our own custom GetScore(...) method, defined above

    private static Move GetGreedyMove(Board map)
    {
        Move[] moves = map.GetLegalMoves();

        bool getWhiteScore = map.IsWhiteToMove;

        double maxScore = double.NegativeInfinity;
        List<Move> maxMoves = new();

        foreach (Move move in moves)
        {
            map.MakeMove(move);
            try
            {
                double getScore = GetScore(map, getWhiteScore);
                if (getScore > maxScore)
                {
                    maxScore = getScore;
                    maxMoves.Clear();
                    maxMoves.Add(move);
                }
                else if (getScore == maxScore)
                {
                    maxMoves.Add(move);
                }
            }
            finally
            {
                map.UndoMove(move);
            }
        }

        return (maxMoves.Count > 0) ? ChooseMove(maxMoves) : ChooseMove(moves); // Something went wrong, just choose a random move
    }

    private static Move ChooseMove(Move[] moves)
    {
        return moves[RANDOM.Next(moves.Length)];
    }

    private static Move ChooseMove(List<Move> moves)
    {
        return moves[RANDOM.Next(moves.Count)];
    }
}