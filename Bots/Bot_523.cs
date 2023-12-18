namespace auto_Bot_523;
using ChessChallenge.API;
using System;







public class Bot_523 : IChessBot
{

    Move moveToPlay;
    PieceList[] pieces;
    Move[] killers = new Move[30];
    (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[4_000_000];
    int[,,] doubleHistory;

    public Move Think(Board board, Timer timer)
    {
        int who2move = board.IsWhiteToMove ? 1 : -1;
        pieces = board.GetAllPieceLists();
        doubleHistory = new int[2, 7, 64];
        for (int i = 1; i <= 20; i++)
        {
            negamax(i, who2move, -2147483648, 2147483640, board, 0, timer);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 100)
                break;
        }
        return moveToPlay;
    }

    int negamax(int depth, int color, int alpha, int beta, Board board, int ply, Timer timer)
    {
        ulong key = board.ZobristKey;
        bool notRoot = ply > 0;
        bool qSearch = depth <= 0;
        int mIndex = -1, newFlag = 1, max = -2147483648;

        Move tempBest = Move.NullMove;

        var (ttKey, ttMove, ttDepth, score, ttFlag) = transpositionTable[key % 4_000_000];
        if (notRoot && ttKey == key && ttDepth >= depth && (ttFlag == 2 || ttFlag == 1 && score <= alpha || ttFlag == 3 && score >= beta))
            return score;

        if (board.IsInCheckmate())
            return -2147000000;
        if ((board.IsDraw() || board.IsRepeatedPosition()) && ply != 0)
            return 0;
        if (qSearch)
        {
            max = newEval(board) * color;
            if (max >= beta) return max;
            alpha = Math.Max(alpha, max);
        }



        Move[] moves = board.GetLegalMoves(qSearch);
        int[] scores = new int[moves.Length];





        if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

        foreach (Move m in moves)
            scores[++mIndex] = -(m == ttMove ? 100_000_000 : m.IsCapture ? 10_000_000 * (int)m.CapturePieceType - (int)m.MovePieceType : m == killers[ply] ? 10_000_000 : doubleHistory[ply & 1, (int)m.MovePieceType - 1, m.TargetSquare.Index]);


        Array.Sort(scores, moves);




        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int temp = -negamax(depth - 1, -color, -beta, -alpha, board, ply + 1, timer);
            board.UndoMove(m);
            if (temp > max)
            {
                max = temp;
                tempBest = m;
                if (temp > alpha)
                {
                    alpha = temp;
                    newFlag = 2;
                    if (ply == 0)
                    {
                        moveToPlay = m;
                        if (max > 214700000)
                            break;
                    }
                }

                if (alpha > beta)
                {
                    if (!m.IsCapture)
                    {
                        doubleHistory[ply & 1, (int)m.MovePieceType - 1, m.TargetSquare.Index] += depth * depth;
                        killers[ply] = m;
                    }
                    newFlag++;
                    break;
                }
            }
        }
        transpositionTable[key % 4_000_000] = (key, tempBest, depth, max, newFlag);
        return max;
    }





    int[] pieceAttackValue = { 0, 4, 5, 21, 20, 1, 1 };
    int[] figuresmgs = { 0, 1100, 3900, 4700, 7000, 15000, 1 };




    int newEval(Board board)
    {
        int value = 0;
        ulong[] pawnAttacks = new ulong[2];
        for (int i = 0; i <= 6; i += 6)
        {
            foreach (Piece p in pieces[i])
            {
                pawnAttacks[p.IsWhite ? 0 : 1] |= BitboardHelper.GetPawnAttacks(p.Square, p.IsWhite);
            }
        }
        foreach (PieceList list in pieces)
        {
            value += list.Count * figuresmgs[(int)list.TypeOfPieceInList] * (list.IsWhitePieceList ? 1 : -1);
            foreach (Piece p in list)
            {
                bool isWhite = p.IsWhite;
                ulong attack = BitboardHelper.GetPieceAttacks(p.PieceType, p.Square, board, isWhite);
                value += BitboardHelper.GetNumberOfSetBits(attack & ~pawnAttacks[isWhite ? 1 : 0]) * pieceAttackValue[(int)p.PieceType] * (isWhite ? 1 : -1);
            }
        }
        return value;
    }
}

