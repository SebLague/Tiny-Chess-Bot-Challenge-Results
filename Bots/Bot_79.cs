namespace auto_Bot_79;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_79 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues =
    {
        0, 100, 300, 325, 500, 900, 10000
    };

    Dictionary<ulong, int> EvalCache = new();
    Dictionary<ulong, Move[]> MoveCache = new();

    int maxDepth;
    int visited;
    int recursion;

    int Eval(Board board)
    {
        visited++;
        ulong key = board.ZobristKey;

        //if (EvalCache.TryGetValue(key, out int eval)) return eval;
        int Boost(int x) => board.IsWhiteToMove ? x : -x;
        if (board.IsInCheckmate())
            return EvalCache[key] = Boost(100000);

        if (board.IsDraw())
            return EvalCache[key] = 0;

        int total = 0;
        if (board.IsInCheck()) // little annoying
            total += Boost(-5);

        var lists = board.GetAllPieceLists();
        PieceList wp = lists[0];
        PieceList bp = lists[6];

        for (int index = 0; index < 6; index++)
            total += (lists[index].Count - lists[index + 6].Count) * pieceValues[index + 1];
        int pieceCount = lists.Sum(a => a.Count) - 2 - wp.Count - bp.Count;

        int pb = Math.Min(1, 20 - pieceCount);
        foreach (Piece pawn in wp)
            total += pb * (pawn.Square.Rank - 1);

        foreach (Piece pawn in bp)
            total -= pb * (8 - pawn.Square.Rank);
        if (pieceCount < 5)
        {
            // End Game
            board.ForceSkipTurn();
            Square ks = board.GetPieceList(PieceType.King, board.IsWhiteToMove).GetPiece(0).Square;
            HashSet<Square> canMoveTo = new(), checkedSq = new();
            Stack<Square> toCheck = new();
            toCheck.Push(ks);
            while (toCheck.Count > 0)
            {
                Square sq = toCheck.Pop();
                for (int x = -1; x < 2; x++)
                    for (int y = -1; y < 2; y++)
                    {
                        Square ns = new(sq.File + x, sq.Rank + y);
                        if (ns is { File: >= 0 and < 8, Rank: >= 0 and < 8 } && checkedSq.Add(ns) && board.GetPiece(ns).IsNull &&
                            !board.SquareIsAttackedByOpponent(ns) && canMoveTo.Add(ns))
                            toCheck.Push(ns);
                    }
            }

            board.UndoSkipTurn();
            total += (5 - pieceCount) * Boost(64 - canMoveTo.Count);
        }

        else if (total > 0 && board.IsWhiteToMove || total < 0 && !board.IsWhiteToMove)
            total += 4 * Boost(20 - pieceCount); // I approve trades

        EvalCache[key] = total;
        return total;
    }

    int MinMax(Board board, int depth, int alpha, int beta)
    {
        if (depth == maxDepth)
            return Eval(board);

        var moves = GetMoves(board);
        // The following line decreases the amount of positions that have to be evaluated by 66% on average throughout a game
        Array.Sort(moves, (a, b) => b.IsCapture.CompareTo(a.IsCapture));
        if (board.IsWhiteToMove)
        {
            int maxEval = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = MinMax(board, depth + 1, alpha, beta);
                board.UndoMove(move);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                    break;
            }

            return maxEval;
        }

        int minEval = int.MaxValue;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = MinMax(board, depth + 1, alpha, beta);
            board.UndoMove(move);
            minEval = Math.Min(minEval, eval);
            beta = Math.Min(beta, eval);

            if (beta <= alpha)
                break;
        }

        return minEval;
    }

    Move[] GetMoves(Board board)
    {
        return MoveCache.TryGetValue(board.ZobristKey, out var allMoves) ? allMoves : MoveCache[board.ZobristKey] = board.GetLegalMoves();
    }

    public Move Think(Board board, Timer timer)
    {
        maxDepth = timer.MillisecondsRemaining < 10000 ? 3 : 4;
        if (recursion == 0)
            visited = 0;
        //DivertedConsole.Write("Eval: " + board.GetFenString());
        var allMoves = GetMoves(board);


        if (allMoves.Length == 1)
            return allMoves[0];

        // Pick a random move to play if nothing better is found
        int Boost(int x) => board.IsWhiteToMove ? x : -x;
        int highestPositionGain = -Boost(100000);
        List<Move> bestMoves = new();

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }

            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }


            // Find highest value capture
            int rating = MinMax(board, 0, int.MinValue, int.MaxValue);
            board.UndoMove(move);
            //DivertedConsole.Write(move + ": " + rating);

            if (rating == Boost(100000))
                return move;
            if (rating >= highestPositionGain && board.IsWhiteToMove)
            {
                if (rating > highestPositionGain)
                    bestMoves.Clear();

                bestMoves.Add(move);
                highestPositionGain = rating;
            }

            if (rating <= highestPositionGain && !board.IsWhiteToMove)
            {
                if (rating < highestPositionGain)
                    bestMoves.Clear();

                bestMoves.Add(move);
                highestPositionGain = rating;
            }
        }

        if ((board.IsWhiteToMove && highestPositionGain < 0 || !board.IsWhiteToMove && highestPositionGain > 0) && recursion < 5 && visited < 5000 && timer.MillisecondsRemaining > 5000 && timer.MillisecondsElapsedThisTurn < 1000)
        {
            recursion++;
            maxDepth++;
            Move move = Think(board, timer);
            maxDepth--;
            recursion--;
            return move;
        }

        // DivertedConsole.Write("ABP: " + visited);

        foreach (Move bestMove in bestMoves.Where(bestMove => bestMove.IsPromotion))
            return bestMove;

        foreach (Move bestMove in bestMoves.Where(bestMove => bestMove.IsCastles || bestMove.IsCapture))
            return bestMove;

        return bestMoves.Count == 0 ? allMoves[0] : bestMoves[0];
    }
}