namespace auto_Bot_80;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public enum NodeType { PV, Cut, All }

public struct TranspositionTableEntry
{
    public int Score;
    public int Depth;
    public NodeType Type;
    public Move BestMove;
}

public struct MoveScore
{
    public Move Move;
    public int Score;
}

public class Bot_80 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int searchDepth = 7;
    Move[] killerMoves;
    int[,] historyScores;

    Dictionary<ulong, TranspositionTableEntry> transpositionTable = new Dictionary<ulong, TranspositionTableEntry>();

    public Bot_80()
    {
        killerMoves = new Move[searchDepth];
        historyScores = new int[64, 64];
    }

    public Move Think(Board board, Timer timer)
    {
        int alpha = int.MinValue;
        int beta = int.MaxValue;
        int bestScore = int.MinValue;
        Move bestMove = Move.NullMove;

        int timeForThisMove = CalculateTimeForMove(board, timer);

        List<MoveScore> moves = GenerateAndScoreMoves(board);
        moves.Sort((move1, move2) => move2.Score.CompareTo(move1.Score));

        for (int depth = 1; depth <= searchDepth; depth++)
        {
            foreach (MoveScore moveScore in moves)
            {
                if (timer.MillisecondsElapsedThisTurn >= timeForThisMove)
                {
                    break;
                }

                board.MakeMove(moveScore.Move);
                int score = -MiniMax(board, depth - 1, -beta, -alpha);
                board.UndoMove(moveScore.Move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = moveScore.Move;
                }
                alpha = Math.Max(alpha, score);
            }

            if (timer.MillisecondsElapsedThisTurn >= timeForThisMove)
            {
                break;
            }
        }

        return bestMove;
    }

    int MiniMax(Board board, int depth, int alpha, int beta)
    {
        ulong zobristKey = board.ZobristKey;

        if (transpositionTable.TryGetValue(zobristKey, out TranspositionTableEntry entry))
        {
            if (entry.Depth >= depth)
            {
                if (entry.Type == NodeType.PV)
                {
                    return entry.Score;
                }
                else if (entry.Type == NodeType.Cut)
                {
                    alpha = Math.Max(alpha, entry.Score);
                }
                else // entry.Type == NodeType.All
                {
                    beta = Math.Min(beta, entry.Score);
                }

                if (alpha >= beta)
                {
                    return entry.Score;
                }
            }
        }

        if (depth == 0)
        {
            return QuiescenceSearch(board, alpha, beta);
        }

        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? int.MinValue + 1 : int.MaxValue;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        if (depth >= 2 && !board.IsInCheck())
        {
            board.ForceSkipTurn();
            int score = -MiniMax(board, depth - 2, -beta, -beta + 1);
            board.UndoSkipTurn();

            if (score >= beta)
            {
                return score;
            }
        }

        Move bestMove = Move.NullMove;
        List<MoveScore> moves = GenerateAndScoreMoves(board);
        moves.Sort((move1, move2) => move2.Score.CompareTo(move1.Score));

        foreach (MoveScore moveScore in moves)
        {
            board.MakeMove(moveScore.Move);
            int score = -MiniMax(board, depth - 1, -beta, -alpha);
            board.UndoMove(moveScore.Move);

            if (score >= beta)
            {
                return score;
            }

            if (score > alpha)
            {
                alpha = score;
                bestMove = moveScore.Move;

                if (depth < killerMoves.Length && !moveScore.Move.Equals(killerMoves[depth]))
                {
                    killerMoves[depth] = moveScore.Move;
                }
                historyScores[moveScore.Move.StartSquare.Index, moveScore.Move.TargetSquare.Index]++;
            }
        }

        if (alpha > entry.Score || alpha < beta)
        {
            TranspositionTableEntry newEntry = new TranspositionTableEntry
            {
                Score = alpha,
                Depth = depth,
                BestMove = bestMove,
                Type = alpha >= beta ? NodeType.Cut : NodeType.All
            };

            if (alpha > entry.Score && alpha < beta)
            {
                newEntry.Type = NodeType.PV;
            }

            transpositionTable[zobristKey] = newEntry;
        }

        return alpha;
    }

    private int CalculateTimeForMove(Board board, Timer timer)
    {
        int timeRemaining = timer.MillisecondsRemaining;

        int timeBuffer = timeRemaining / 20;
        timeRemaining -= timeBuffer;

        int estimatedMovesLeft = Math.Max(20, board.PlyCount / 2);  // Assume each player makes 40 moves on average.
        int timeForThisMove = timeRemaining / estimatedMovesLeft;

        int minimumTime = 100;
        timeForThisMove = Math.Max(timeForThisMove, minimumTime);

        return timeForThisMove;
    }


    int EvaluateBoard(Board board, int alpha, int beta)
    {
        int score = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            score += pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count * (pieceList.IsWhitePieceList == board.IsWhiteToMove ? 1 : -1);

            if (score <= alpha || score >= beta)
            {
                return score;
            }
        }
        return score;
    }

    public List<MoveScore> GenerateAndScoreMoves(Board board, bool capturesOnly = false)
    {
        Move[] legalMoves = board.GetLegalMoves(capturesOnly);
        List<MoveScore> moves = new List<MoveScore>(legalMoves.Length);

        foreach (Move move in legalMoves)
        {
            int score = pieceValues[(int)move.CapturePieceType] * 100;

            for (int depth = 0; depth < killerMoves.Length; depth++)
            {
                if (move.Equals(killerMoves[depth]))
                {
                    score += 200 * (killerMoves.Length - depth);
                    break;
                }
            }

            score += historyScores[move.StartSquare.Index, move.TargetSquare.Index];

            moves.Add(new MoveScore { Move = move, Score = score });
        }

        return moves;
    }

    int QuiescenceSearch(Board board, int alpha, int beta)
    {
        int stand_pat = EvaluateBoard(board, alpha, beta);
        if (stand_pat >= beta)
            return beta;
        if (alpha < stand_pat)
            alpha = stand_pat;

        List<MoveScore> moves = GenerateAndScoreMoves(board, true);
        moves.Sort((move1, move2) => move2.Score.CompareTo(move1.Score));

        foreach (MoveScore moveScore in moves)
        {
            if (!moveScore.Move.IsCapture)
                continue;

            board.MakeMove(moveScore.Move);
            int score = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(moveScore.Move);

            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }
        return alpha;
    }

}