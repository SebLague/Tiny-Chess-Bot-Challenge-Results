namespace auto_Bot_619;
using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class MonteCarloTree
{
    public static Board Board;
    public static Move[] RanMoves;
    public static Random Ran;
    public static Stack<Move> UndoMoves;

    public readonly MonteCarloTree? Parent;
    public MonteCarloTree[] Children;
    public readonly Move[] Moves;
    public bool IsLeave;
    public int Visits;
    public float Score;
    public Move Move;

    public MonteCarloTree(Move move, MonteCarloTree? parent = null)
    {
        Move = move;
        Parent = parent;
        MakeMove();
        Moves = Board.GetLegalMoves();
        UndoMove();
        IsLeave = true;
        Visits = 0;
        Score = 0;
    }

    public MonteCarloTree TreeTraversal(float parentScore)
    {
        var current = this;
        while (!current.IsLeave)
        {
            if (current.Children.Length <= 0) break;
            current.MakeMove();
            current = current.BestMove(parentScore);
        }

        current.MakeMove();

        if (current.Visits == 0) return current;
        current.Expansion();
        if (current.Children.Length <= 0) return current;
        current = current.Children[0];
        current.MakeMove();
        return current;

    }

    private void Expansion()
    {
        Children ??= new MonteCarloTree[Moves.Length];

        for (ushort i = 0; i < Children.Length; i++)
        {
            if (Children[i] == null)
            {
                Children[i] = new MonteCarloTree(Moves[i], this);
                break;
            }
        }

        IsLeave = false;
    }

    public float Rollout(bool botColorIsWhite)
    {
        while (!(Board.IsInCheckmate() || Board.IsDraw()))
        {
            var span = RanMoves.AsSpan()[..150];
            Board.GetLegalMovesNonAlloc(ref span);
            var move = span[Ran.Next(span.Length)];
            UndoMoves.Push(move);
            Board.MakeMove(move);
        }

        var result = Board.IsInCheckmate() ? botColorIsWhite == Board.IsWhiteToMove ? 1 : 0 : 0.5F;
        while (UndoMoves.Count > 0)
            Board.UndoMove(UndoMoves.Pop());
        UndoParentMove();
        return result;
    }

    public void BackPropagation(float score)
    {
        Visits++;
        Score += score;
        Parent?.BackPropagation(score);
    }

    private double Ucb3(float parentScore) => Score / Visits + Math.Sqrt(2 * Math.Log(parentScore, Math.E) / Visits);

    public MonteCarloTree BestMove(float parentScore)
    {
        double KeySelector(MonteCarloTree item) => item != null ? item.Ucb3(parentScore) : double.MaxValue;

        var current = Children[0];
        double result = 0;
        for (ushort i = 0; i < Children.Length; i++)
        {
            var keySelector = KeySelector(Children[i]);

            if (!(keySelector > result)) continue;
            result = keySelector;
            Children[i] ??= new MonteCarloTree(Moves[i], this);
            current = Children[i];
        }
        return current;
    }

    private void MakeMove() => Board.MakeMove(Move);

    private void UndoMove() => Board.UndoMove(Move);

    private void UndoParentMove()
    {
        UndoMove();
        Parent?.UndoParentMove();
    }

}

public class Bot_619 : IChessBot
{
    public Bot_619()
    {
        MonteCarloTree.RanMoves = new Move[150];
        MonteCarloTree.UndoMoves = new Stack<Move>(200);
        MonteCarloTree.Ran = new Random();
    }
    public Move Think(Board board, Timer timer)
    {
        var botColorIsWhite = board.IsWhiteToMove;
        MonteCarloTree.Board = board;
        var parent = new MonteCarloTree(Move.NullMove);
        for (var i = 0; i < 4000; i++)
        {
            var current = parent.TreeTraversal(parent.Score);
            var score = current.Rollout(botColorIsWhite);
            current.BackPropagation(score);
        }
        return parent.BestMove(parent.Score).Move;
    }

}