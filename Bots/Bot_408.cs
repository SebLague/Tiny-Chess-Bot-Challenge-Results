namespace auto_Bot_408;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_408 : IChessBot
{
    private readonly Random rng = new();
    private const int MaxScore = 1000;
    private const int TimeConsumption = 12;  // fraction of the clock to use each turn
    private const int TimeThreshold = 16;  // how aggressively to increase the search depth
    private const double InitialSearchScale = 1.6;  // coefficient for initial searchDepth value
    private int searchDepth = 0;

    // idea taken from:
    // https://github.com/SebLague/Chess-Coding-Adventure/blob/Chess-V2-UCI/Chess-Coding-Adventure/src/Core/Search/TranspositionTable.cs
    internal struct CacheItem
    {
        public ulong key;
        public int score;
    }
    internal CacheItem[] evalCache = new CacheItem[1 << 24];  // table size: 256MB

    // How long do we have to think about our move?
    private int ThinkingTimeGoal(Timer timer)
    {
        int remaining = timer.MillisecondsRemaining / TimeConsumption;
        int evenSplit = timer.GameStartTimeMilliseconds / (2 * TimeConsumption);
        int goal = Math.Min(remaining, evenSplit) + timer.IncrementMilliseconds;
        return Math.Min(goal, timer.MillisecondsRemaining / 2) + 1;
    }

    // Try to evaluate how good a position is.
    // Higher number -> better for the current player.
    private int BoardScore(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        int ownMoveCount = moves.Length;
        board.ForceSkipTurn();
        int otherMoveCount = board.GetLegalMoves().Length;
        board.UndoSkipTurn();
        return ownMoveCount - otherMoveCount;
        // note that board.IsInCheck is forced to false
    }

    // Wrapper around BoardScore which uses the evaluation table.
    private int CachedBoardScore(Board board)
    {
        ulong key = board.ZobristKey % (ulong)evalCache.Length;
        if (evalCache[key].key == board.ZobristKey)
            return evalCache[key].score;
        int score = BoardScore(board);
        evalCache[key].key = board.ZobristKey;
        evalCache[key].score = score;
        return score;
    }

    // Produce an array of moves sorted by estimate, probably-better moves first.
    private Move[] SortedMoves(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        int[] moveScores = moves.Select(move =>
        {
            board.MakeMove(move);
            int score = CachedBoardScore(board);
            board.UndoMove(move);
            return score;
        }).ToArray();
        Array.Sort(moveScores, moves);
        return moves;
    }

    // Use alpha-beta search to determine a score for a position.
    internal int Search(Board board, int depth, int lowerBound, int upperBound)
    {
        // avoid checkmate and draws
        if (board.IsInCheckmate())
            return board.PlyCount - MaxScore;

        if (board.IsDraw())
            return 0;

        if (depth <= 0 && !board.IsInCheck())
            return CachedBoardScore(board);

        int bestScore = -MaxScore;
        foreach (Move move in SortedMoves(board))
        {
            board.MakeMove(move);

            // play a checkmate move immediately
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return MaxScore - board.PlyCount - 1;
            }

            // search recursively
            // the quality of the move to us is how bad it makes the opponent's position
            int moveScore = -Search(board, depth - 1, -upperBound, -Math.Max(bestScore, lowerBound));
            board.UndoMove(move);

            // beta cut off
            if (moveScore > upperBound)
            {
                // stop evaluating this position, as it's
                // "too good" for the opponent to allow us to reach
                return MaxScore;
            }

            bestScore = Math.Max(bestScore, moveScore);
        }

        return bestScore;
    }

    // Select a random move from a list.
    private Move RandomMove(IList<Move> moves)
    {
        return moves[rng.Next(moves.Count)];
    }

    // Top-level search function, returns a list of best moves.
    internal List<Move> FindBestMoves(Board board, int depth)
    {
        int bestScore = -MaxScore;
        List<Move> bestMoves = new List<Move>();
        foreach (Move move in SortedMoves(board))
        {
            board.MakeMove(move);

            // play a checkmate move immediately
            if (board.IsInCheckmate())
            {
                bestMoves.Clear();
                bestMoves.Add(move);
                return bestMoves;
                // don't need to undo the move
            }

            // evaluate the position after this move
            int moveScore = -Search(board, depth - 1, -MaxScore, -bestScore);

            // update the list of best moves
            if (moveScore == bestScore)
            {
                bestMoves.Add(move);
            }
            else if (moveScore > bestScore)
            {
                bestScore = moveScore;
                bestMoves.Clear();
                bestMoves.Add(move);
            }

            board.UndoMove(move);
        }

        return bestMoves;
    }

    // Entry point for a Chess Challenge bot.
    public Move Think(Board board, Timer timer)
    {
        int thinkingTimeGoal = ThinkingTimeGoal(timer);

        // set initial search depth based on the game length
        if (searchDepth == 0)
            searchDepth = (int)(InitialSearchScale * Math.Log(timer.GameStartTimeMilliseconds, TimeThreshold));

        bool wasInCheck = board.IsInCheck();

        List<Move> bestMoves = FindBestMoves(board, searchDepth);

        if (!wasInCheck)  // don't update the search depth in special cases
        {
            if (timer.MillisecondsElapsedThisTurn > thinkingTimeGoal && searchDepth > 1)
                searchDepth--;
            else if (timer.MillisecondsElapsedThisTurn < thinkingTimeGoal / TimeThreshold)
                searchDepth++;
        }

        // play a random move from the best moves
        return RandomMove(bestMoves);
    }
}