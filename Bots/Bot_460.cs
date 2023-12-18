namespace auto_Bot_460;
using ChessChallenge.API;
using System;

public class Bot_460 : IChessBot
{
    private Timer _timer;
    private Board _board;
    private Move _iterationMove;
    private enum MoveFlag { Alpha, Exact, Beta }
    private record struct TranspositionTableEntry(ulong Key, int Score, int Depth, MoveFlag Flag, Move Move);
    private readonly TranspositionTableEntry[] _transpositionTable = new TranspositionTableEntry[0x7FFFFF];
    private int _maxScore;
    private ulong _desiredPosition = 0x2424817e0000;

    public Move Think(Board board, Timer timer)
    {
        _maxScore = BitboardHelper.GetNumberOfSetBits(_desiredPosition);
        _timer = timer;
        _board = board;

        Move bestMove = Move.NullMove;

        if (_board.AllPiecesBitboard == _desiredPosition) return bestMove;

        int alpha = -100000;
        int beta = 100000;

        for (int currentDepth = 1; currentDepth <= 100; currentDepth++)
        {
            int iterationScore = Negamax(alpha, beta, currentDepth, 0);

            if (_timer.MillisecondsElapsedThisTurn > _timer.MillisecondsRemaining / 30) break;

            bestMove = _iterationMove;

            if (iterationScore <= alpha || iterationScore >= beta)
            {
                alpha = -100000;
                beta = 100000;
                currentDepth--;
                continue;
            }

            alpha = iterationScore - 50;
            beta = iterationScore + 50;

            if (iterationScore > 50000)
                break;
        }

        return bestMove;
    }

    private int Negamax(int alpha, int beta, int depth, int ply)
    {
        bool isRoot = ply == 0;

        if (!isRoot && _board.IsRepeatedPosition()) return -1000;

        if (depth <= 0) return Evaluate();

        ulong positionKey = _board.ZobristKey;

        TranspositionTableEntry entry = _transpositionTable[positionKey % 0x7FFFFF];

        if (entry.Key == positionKey && !isRoot && entry.Depth >= depth &&
            (entry.Flag == MoveFlag.Exact ||
            (entry.Flag == MoveFlag.Alpha && entry.Score <= alpha) ||
            (entry.Flag == MoveFlag.Beta && entry.Score >= beta))) return entry.Score;

        Move[] moves = _board.GetLegalMoves();
        Move bestMove = Move.NullMove;
        int bestScore = -100000;
        int startAlpha = alpha;

        for (int i = 0; i < moves.Length; i++)
        {
            if (_timer.MillisecondsElapsedThisTurn > _timer.MillisecondsRemaining / 30) return 0;

            Move move = moves[i];

            _board.MakeMove(move);
            int score = Negamax(-beta, -alpha, depth - 1, ply + 1);
            _board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;

                alpha = Math.Max(alpha, bestScore);

                if (isRoot) _iterationMove = move;
                if (alpha >= beta) break;
            }
        }

        MoveFlag flag = bestScore >= beta ? MoveFlag.Beta :
            bestScore > startAlpha ? MoveFlag.Exact : MoveFlag.Alpha;

        _transpositionTable[positionKey % 0x7FFFFF] =
            new TranspositionTableEntry(positionKey, bestScore, depth, flag, bestMove);

        return bestScore;
    }

    int Evaluate()
    {
        int score = -BitboardHelper.GetNumberOfSetBits(_board.AllPiecesBitboard ^ _desiredPosition);

        if (_board.AllPiecesBitboard == _desiredPosition) return 100000 - _board.PlyCount;

        return score;
    }
}