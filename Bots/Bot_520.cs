namespace auto_Bot_520;
using ChessChallenge.API;
using System;

public class Bot_520 : IChessBot
{
    static Random rng = new();

    struct Node
    {
        public double score = 0;     // how favorable a position is
        public ushort simCount = 0;  // how many games we play
        public Move move;            // move to play to get to the evaluated state
        public Node[] children = new Node[0];
        public bool hasUpdatedChildren = false;
        public bool isWhite;

        public Node(Move Move, bool IsWhite)
        {
            this.move = Move;
            this.isWhite = IsWhite;
        }

        public void GenerateChildren(Board board)
        {
            this.children = Node.GetChildren(this, board);
            this.hasUpdatedChildren = true;
        }

        public static Node[] GetChildren(Node node, Board board)
        {
            var moves = board.GetLegalMoves();
            int i = 0;
            var children = new Node[moves.Length];
            for (; i < moves.Length; i++)
            {
                board.MakeMove(moves[i]);
                children[i] = new Node(moves[i], !board.IsWhiteToMove);
                board.UndoMove(moves[i]);
            }
            return children;
        }

        public void AddScore(double score)
        {
            if (!this.isWhite) score *= -1;
            this.score += score;
            this.simCount += 1;
        }

        public override string ToString()
        {
            return $"Score: {this.score}, Sims: {this.simCount}";
        }

    }

    public Move Think(Board board, Timer timer)
    {
        var moves = board.GetLegalMoves();
        var plays = new Node[moves.Length];
        int i = 0;
        for (; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsInCheckmate()) return moves[i]; // play mate in one without doing any extra calculations
            plays[i] = new Node(moves[i], !board.IsWhiteToMove); // init setup
            board.UndoMove(moves[i]);
        }
        for (i = 0; i < 1000; i++)
        {
            UCBSelectAndExpand(ref plays, i, ref board);
        }
        while (timer.MillisecondsRemaining > timer.OpponentMillisecondsRemaining && timer.MillisecondsElapsedThisTurn < (timer.GameStartTimeMilliseconds) / 150) // custom crafted time function, it ends up spending more time thinking when it's "ahead" on the timer and less (sometimes almost none, if it has a good buffer of boards precalculated) when it's behind
        {
            UCBSelectAndExpand(ref plays, i, ref board);
            i++;
        }
        var bestMoveToPlay = plays[0].move;
        var bestScore = double.MinValue;
        foreach (Node play in plays)
        {
            var currentScore = (double)play.score / play.simCount;
            if (currentScore > bestScore)
            {
                bestScore = currentScore;
                bestMoveToPlay = play.move;
            }
        }
        return bestMoveToPlay;
    }

    double UCBSelectAndExpand(ref Node[] nodes, int simIter, ref Board board)
    {
        if (nodes.Length == 0) return Math.Tanh(Playout(board, 0));
        double bestUCB = -1000;
        int bestIndex = 0;
        for (int i = 0; i < nodes.Length; i++)
        {
            var child = nodes[i];
            var currentUCB = (double)child.score / (child.simCount + 0.00001) + Math.Sqrt(4 * Math.Log(simIter) / (child.simCount + 0.00001));
            if (currentUCB > bestUCB)
            {
                bestUCB = currentUCB;
                bestIndex = i;
            }
        }
        ref var bestChild = ref nodes[bestIndex];
        board.MakeMove(bestChild.move);
        double ret;
        if (!bestChild.hasUpdatedChildren)
        {
            bestChild.GenerateChildren(board);
            if (bestChild.children.Length == 0)
            {
                ret = Math.Tanh(Playout(board, 0));
                board.UndoMove(bestChild.move);
                return ret;
            }
            else
            {
                ref var leaf = ref bestChild.children[rng.Next(bestChild.children.Length)];
                board.MakeMove(leaf.move);
                ret = Math.Tanh(Playout(board, 0));
                leaf.AddScore(ret);
                board.UndoMove(leaf.move);
            }
        }
        else ret = UCBSelectAndExpand(ref bestChild.children, simIter, ref board);
        bestChild.AddScore(ret);
        board.UndoMove(bestChild.move);
        return ret;
    }

    double GetPieceValue(PieceType piece)
    {
        return Math.Pow((double)piece, 2);
    }

    double Playout(Board board, short depth)
    {
        var moves = board.GetLegalMoves();
        if (moves.Length == 0)
        {
            if (board.IsDraw()) return 0;
            else
            {
                if (board.IsWhiteToMove) return short.MinValue;
                else return short.MaxValue;
            }
        }
        else
        {
            var move = moves[rng.Next(moves.Length)];
            double ret = 0;
            if (depth < 150)
            {
                board.MakeMove(move);
                ret += Playout(board, ++depth);
                board.UndoMove(move);
            }
            if (board.IsWhiteToMove)
            {
                ret += GetPieceValue(move.CapturePieceType);
                ret += GetPieceValue(move.PromotionPieceType);
            }
            else
            {
                ret -= GetPieceValue(move.CapturePieceType);
                ret -= GetPieceValue(move.PromotionPieceType);
            }
            return ret;
        }
    }
}