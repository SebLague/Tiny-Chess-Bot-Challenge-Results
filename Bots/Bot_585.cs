namespace auto_Bot_585;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
struct TranspositionEntry
{
    public int score;
    public Move bestMove;
    public sbyte depth;
    public bool fullSearch;
}
public class Bot_585 : IChessBot
{
    private Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();
    private Board board;
    private Timer timer;
    private int[] pieceValues = { 100, 300, 300, 500, 900, 0 };
    private Move nextMove;
    private int tableSize = 64000000 / 16, oldestKeyIndex = 0, entryCount = 0, newKeyIndex = 0;
    private ulong[] keys;
    private int[] middleWeights;

    public Bot_585()
    {
        keys = new ulong[tableSize];
        middleWeights = new int[64];
        for (int f = 0; f < 8; f++)
            for (int r = 0; r < 8; r++)
            {
                middleWeights[f + 8 * r] = 5 - (int)((f - 3.5) * (f - 3.5) + (r - 3.5) * (r - 3.5));
            }
    }
    void SortMoves(Span<Move> moves, Move first)
    {
        Span<int> values = stackalloc int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move a = moves[i];
            values[i] = a.IsCapture ? pieceValues[(int)a.CapturePieceType - 1] - pieceValues[(int)a.MovePieceType - 1] - 1000 : 0;
            if (a == first)
                values[i] -= 1000000;
            if (a.MovePieceType == PieceType.King || a.MovePieceType == PieceType.Queen)
                values[i] += 10000;
            if (a.MovePieceType == PieceType.Knight || a.MovePieceType == PieceType.Bishop)
                values[i] -= 10000;
            if (a.IsCastles)
                values[i] -= 100000;
        }
        MemoryExtensions.Sort(values, moves);
    }
    int PositionalEval(PieceList ps, PieceList os, PieceList ks, bool white)
    {
        int score = 0;
        foreach (Piece p in ps)
        {
            bool passed = true;
            int advance = white ? p.Square.Rank - 1 : 6 - p.Square.Rank;
            foreach (Piece o in os)
            {
                int oppositeAdvance = white ? o.Square.Rank - 1 : 6 - o.Square.Rank;
                if (o.Square.File >= p.Square.File - 1 && o.Square.File <= p.Square.File + 1 && oppositeAdvance > advance)
                {
                    passed = false;
                    break;
                }
            }
            if (passed)
                score += advance * 5 + 100;
            if (p.Square.File == 3 || p.Square.File == 4)
                score += advance * 3;
        }
        foreach (Piece k in ks)
            score += middleWeights[k.Square.Index] / 2;
        return score;
    }
    int Evaluate()
    {
        int score = 0;
        PieceList[] lists = board.GetAllPieceLists();
        foreach (PieceList list in lists)
        {
            score += (list.IsWhitePieceList ? 1 : -1) * list.Count * pieceValues[(int)list.TypeOfPieceInList - 1];
        }
        score += PositionalEval(lists[0], lists[6], lists[1], true);
        score -= PositionalEval(lists[6], lists[0], lists[7], false);
        return board.IsWhiteToMove ? score : -score;
    }
    int Search(int depth, int alpha, int beta, bool writeBestMove, int timeEnd)
    {
        if (depth >= 0)
        {
            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return -1000000000 + board.PlyCount;
        }
        ulong key = board.ZobristKey;
        Move candidateBestMove = Move.NullMove;
        TranspositionEntry tableEntry;
        if (transpositionTable.ContainsKey(key))
        {
            tableEntry = transpositionTable[key];
            candidateBestMove = tableEntry.bestMove;
            if (depth <= tableEntry.depth && tableEntry.fullSearch)
            {
                board.MakeMove(candidateBestMove);
                bool repetition = board.IsRepeatedPosition();
                board.UndoMove(candidateBestMove);
                if (!repetition)
                {
                    if (writeBestMove)
                        nextMove = candidateBestMove;
                    return tableEntry.score;
                }
            }
        }
        int bestScore = -2000000000;
        Move bestMove = Move.NullMove;
        if (depth <= 0)
        {
            int staticEval = Evaluate();
            if (staticEval > beta)
                return staticEval;
            alpha = Math.Max(alpha, staticEval);
            bestScore = staticEval;
        }
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, depth <= 0);
        SortMoves(moves, candidateBestMove);
        bool fullSearch = true;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -Search(depth - 1, -beta, -alpha, false, timeEnd);
            board.UndoMove(move);
            if (score > bestScore)
            {
                alpha = Math.Max(alpha, score);
                bestScore = score;
                bestMove = move;
                if (score > beta)
                {
                    fullSearch = false;
                    break;
                }
            }
            if (depth >= 0 && timer.MillisecondsRemaining < timeEnd)
            {
                fullSearch = false;
                break;
            }
        }
        TranspositionEntry entry = new TranspositionEntry { score = bestScore, bestMove = bestMove, depth = (sbyte)depth, fullSearch = fullSearch };
        if (!transpositionTable.ContainsKey(key))
        {
            entryCount++;
            if (entryCount >= tableSize)
            {
                transpositionTable.Remove(keys[oldestKeyIndex++]);
                oldestKeyIndex %= tableSize;
            }
            keys[newKeyIndex++] = key;
            newKeyIndex %= tableSize;
            transpositionTable.Add(key, entry);
        }
        else if (depth >= 0 && depth > transpositionTable[key].depth)
        {
            transpositionTable.Remove(key);
            transpositionTable.Add(key, entry);
        }
        if (writeBestMove)
            nextMove = bestMove;
        return bestScore;
    }
    public Move Think(Board b, Timer t)
    {
        board = b;
        timer = t;
        byte depth = 0;
        int eval;
        while (depth < 20)
        {
            depth++;
            int timeEnd = timer.MillisecondsRemaining * 59 / 60;
            eval = Search(depth, -2000000000, 2000000000, true, timeEnd);
            if ((eval > 1000000 || eval < -1000000) && depth >= 1000000000 - Math.Abs(eval) - board.PlyCount)
                break;
            if (timer.MillisecondsRemaining < timeEnd)
                break;
        }
        return nextMove;
    }
}
