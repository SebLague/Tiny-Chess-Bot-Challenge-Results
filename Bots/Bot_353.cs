namespace auto_Bot_353;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_353 : IChessBot
{
    Board CurrentBoard;
    Timer CurrentTimer;
    Move BestMove;
    int Depth;
    long[] tables =
    {
        131355561780838,
        4919094222992310272,
        168882984063269,
        1160672266878976,
        -1311432595201605324,
        -4596836959896080948,
        -4996004912068124398,
        -6907780608819152214,
        19941264781312,
        -1152657617789648895,
        -4994597537184616431,
        4962171676048047,
        -3612223494284194338,
        -4842025203565312480,
        -1311730974999524760,
        -3861614275051066164
    };
    int[][] pieceSquareTables = new int[8][];
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 0 };

    record struct Entry(ulong key, Move move, int depth, int score, int nodeType);
    Entry[] transpositionTable = new Entry[0x3FFFFF];

    public Bot_353()
    {
        for (int i = -1; ++i < 8;)
        {
            pieceSquareTables[i] = new int[32];
            for (int j = -1; ++j < 32;)
            {
                int x = (int)((tables[2 * i + (j > 15 ? 1 : 0)] >> 4 * (15 - j % 16)) & 15);
                pieceSquareTables[i][j] = x == 7 ? 80 :
                                        x < 9 ? x * 5 :
                                        x == 9 ? 50 :
                                        x < 15 ? 90 - 10 * x : -5;
            }
        }
    }

    public Move Think(Board board, Timer timer)
    {
        CurrentBoard = board;
        CurrentTimer = timer;
        Depth = 0;
        do Search(++Depth, -100000, 100000); while (timer.MillisecondsElapsedThisTurn * 30 < timer.MillisecondsRemaining);
        return BestMove;
    }

    int Search(int depth, int alpha, int beta)
    {
        ulong zobrist = CurrentBoard.ZobristKey;
        bool notRoot = depth != Depth;
        int bestEval = -200000;

        if (notRoot && CurrentBoard.IsRepeatedPosition()) return 0;

        Entry entry = transpositionTable[zobrist % 0x3FFFFF];
        if (notRoot && entry.key == zobrist && entry.depth >= depth && (entry.nodeType == 1
        || entry.nodeType == 0 && entry.score <= alpha
        || entry.nodeType == 2 && entry.score >= beta)) return entry.score;

        var moves = CurrentBoard.GetLegalMoves(depth <= 0).OrderByDescending
        (
            move => move == entry.move ? 10000 : move.IsCapture ? 10 * pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] : 0
        ).ToArray();

        if (depth <= 0)
        {
            bestEval = Evaluation();
            if (bestEval >= beta) return beta;
            alpha = Math.Max(alpha, bestEval);
        }
        else if (moves.Length == 0) return CurrentBoard.IsInCheck() ? Depth - depth - 100000 : 0;

        Move bestMove = Move.NullMove;
        int originalAlpha = alpha;
        foreach (Move move in moves)
        {
            CurrentBoard.MakeMove(move);
            int eval = -Search(depth - 1, -beta, -alpha);
            CurrentBoard.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;

                if (!notRoot) BestMove = bestMove;
                alpha = Math.Max(alpha, bestEval);
                if (alpha >= beta) break;
            }

            if (CurrentTimer.MillisecondsElapsedThisTurn * 30 > CurrentTimer.MillisecondsRemaining) return 200000;
        }
        transpositionTable[zobrist % 0x3FFFFF] = new(zobrist, bestMove, depth, bestEval, bestEval >= beta ? 2 : bestEval > originalAlpha ? 1 : 0);
        return bestEval;
    }

    int Evaluation()
    {
        int eval = 0;
        int side = 2;
        for (; --side >= 0;)
        {
            int material = 0;
            for (int i = 7; --i > 1;) material += CurrentBoard.GetPieceList((PieceType)i, side > 0).Count * pieceValues[i];
            float endgameWeight = 1 - Math.Min(material / 1650f, 1);

            for (int i = 0; ++i < 7;)
                for (ulong mask = CurrentBoard.GetPieceBitboard((PieceType)i, side > 0); mask != 0;)
                {
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * side;
                    int file = square % 8;
                    int index = (file > 3 ? file ^ 7 : file) + (square / 8) * 4;
                    //eval += pieceValues[i] + (int)((1 - endgameWeight) * pieceSquareTables[i][index] + endgameWeight * pieceSquareTables[i == 1 ? 0 : i == 6 ? 7 : i][index]);
                    int normal = pieceSquareTables[i][index];
                    eval += pieceValues[i] + (int)(i % 5 == 1 ? (1 - endgameWeight) * normal + endgameWeight * pieceSquareTables[i == 1 ? 0 : 7][index] : normal);
                }

            eval *= -1;
        }
        return (CurrentBoard.IsWhiteToMove ? 1 : -1) * eval;
    }
}