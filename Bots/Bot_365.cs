#define DEBUG
namespace auto_Bot_365;

using ChessChallenge.API;
using System;


public class Bot_365 : IChessBot
{
    // track positions visited
    int numPositionsVisited;
    int numPositionsVisitedTotal;

    // "global" best move/score tracking
    Move bestMove;
    float bestScore = 0;

    const float TIMER_FRACTION_ALLOTTED = 1.0F / 120;
    const int MAX_DEPTH = 30;

    // initialize transposition table
    const int N_TT_ENTRIES = 1 << 20;
    TableEntry[] tTable = new TableEntry[N_TT_ENTRIES];
    // sizeof(TableEntry) * N_TT_ENTRIES = 8 * (1 >> 20) = 8388608 bytes = 8.388608 MB (max is 256 in rules)

    public Move Think(Board board, Timer timer)
    {
        numPositionsVisited = 0;

        int depth = 1;
        // https://www.chessprogramming.org/Iterative_Deepening
        for (; depth == 1 || (!TimeOut(timer) && depth < MAX_DEPTH); depth++)
        {
            Negamax(board, timer, depth, 0, -3000, 3000);
        }

#if DEBUG
        DivertedConsole.Write("\nBest {0} ({1}). Positions Evaluated: {2}, ({3} total). Time to Move: {4} seconds. Max depth: {5}.",
            bestMove, bestScore, numPositionsVisited, numPositionsVisitedTotal, timer.MillisecondsElapsedThisTurn / 1000.0, depth);
#endif

        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }

    // https://www.chessprogramming.org/Negamax
    public float Negamax(Board board, Timer timer, int depth, int ply, float alpha, float beta)
    {
        numPositionsVisited += 1; // update positions visited
        numPositionsVisitedTotal += 1;
        var moves = board.GetLegalMoves();

        if (board.IsInCheckmate()) return -30000.0F + ply; // checkmate bad, further checkmate better
        if (board.IsRepeatedPosition()) return 0; // draw by repetition
        if (depth == 0) return Evaluation(board, moves.Length); // max depth reached, return static evaluation

        ulong key = board.ZobristKey;
        TableEntry entry = tTable[key % N_TT_ENTRIES]; // retrieve TT entry
        // If there is a TT entry that has >= depth, return the evaluation already done
        if (entry is not null && entry.zobristKey == key && entry.depth >= depth)
        {
            return entry.chosenMoveEvaluation;
        }

        var movePrios = new int[moves.Length]; // move priorities to search "good" moves first
        for (int i = 0; i < moves.Length; i++) // https://www.chessprogramming.org/MVV-LVA
        {
            if (moves[i].IsCapture) movePrios[i] = 100 * ((int)moves[i].CapturePieceType - (int)moves[i].MovePieceType);
            else movePrios[i] = 0;
        }

        var maxScore = -30000.0F;
        var maxMove = Move.NullMove;

        for (int i = 0; i < moves.Length; i++) // main move search loop
        {
            for (int j = i + 1; j < moves.Length; j++) // Incrementally sort moves by priority, shift good prio left
                if (movePrios[i] > movePrios[j])
                    (moves[i], moves[j], movePrios[i], movePrios[j]) = (moves[j], moves[i], movePrios[j], movePrios[i]);

            board.MakeMove(moves[i]);
            var score = -Negamax(board, timer, depth - 1, ply + 1, -beta, -alpha); // make recursive search
            board.UndoMove(moves[i]);

            if (score > maxScore)
            {  // If we found a new best
                maxScore = score;
                maxMove = moves[i];
                if (ply == 0) (bestMove, bestScore) = (moves[i], score); // we are the root, tell Think() this is best
            }

            // https://www.chessprogramming.org/Alpha-Beta
            if (score > alpha) alpha = score;
            if (score >= beta) break;
        }

        tTable[key % N_TT_ENTRIES] = new TableEntry(key, maxMove, maxScore, depth); // update TT with search just made

        return maxScore;
    }

    // https://www.chessprogramming.org/Evaluation#Side_to_move_relative
    public static float Evaluation(Board board, int mobility)
    {
        // wp, wn, wb, wr, wq, wk, bp, bn, bb, br, bq, bk
        var pieceLists = board.GetAllPieceLists();

        float evaluation = 0.0F;
        evaluation += 1.0F * (pieceLists[0].Count - pieceLists[6].Count); // Pawns
        evaluation += 3.0F * (pieceLists[1].Count - pieceLists[7].Count); // Knights
        evaluation += 3.0F * (pieceLists[2].Count - pieceLists[8].Count); // Bishops
        evaluation += 5.0F * (pieceLists[3].Count - pieceLists[8].Count); // Rooks
        evaluation += 9.0F * (pieceLists[4].Count - pieceLists[10].Count); // Queens

        return (evaluation * (board.IsWhiteToMove ? 1 : -1)) + 0.1F * mobility;
    }

    public static bool TimeOut(Timer timer) => timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining * TIMER_FRACTION_ALLOTTED;
}


// https://www.chessprogramming.org/Transposition_Table
public class TableEntry
{
    public ulong zobristKey; // hash of position
    public Move chosenMove; // the best move previously found
    public float chosenMoveEvaluation; // said move's evaluation
    public int depth; // what depth we found this position at

    public TableEntry(ulong key, Move move, float eval, int _depth)
    {
        zobristKey = key;
        chosenMove = move;
        chosenMoveEvaluation = eval;
        depth = _depth;
    }
}