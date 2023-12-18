namespace auto_Bot_612;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public struct TTEntry
{
    public int eval;
    public int type;
    public int depth;
}

public class Bot_612 : IChessBot
{
    Move moveToMake;
    bool abort;
    int remainingTime;

    int NodesVisited;//#DEBUG

    const int inf = 9999999;
    const int checkmate = 5000000;

    Dictionary<ulong, TTEntry> ttt = new();

    public Move Think(Board board, Timer timer)
    {
        NodesVisited = 0;//#DEBUG
        abort = false;
        remainingTime = timer.MillisecondsRemaining / 35;

        int maxDepth = 2;
        Move bestMove = moveToMake = Move.NullMove;

        while (!abort)
        {
            DivertedConsole.Write($"{maxDepth} - {timer.MillisecondsElapsedThisTurn}/{remainingTime} - {NodesVisited}");
            ttt = new();

            bestMove = moveToMake; // save current best move in case the next search iteration aborts
            Search(board, timer, 0, maxDepth, -inf, inf);
            maxDepth++;
        }

        return bestMove;
    }

    int Search(Board board, Timer timer, int currentDepth, int maxDepth, int alpha, int beta)
    {
        NodesVisited++;

        if (ttt.ContainsKey(board.ZobristKey))
        {
            var tte = ttt[board.ZobristKey];

            if (tte.depth <= currentDepth)
            {
                if (tte.type == 0) { return tte.eval; } // exact
                else if (tte.type == -1) { alpha = Math.Max(alpha, tte.eval); } // Lower
                else if (tte.type == 1) { beta = Math.Min(beta, tte.eval); } // Upper

                if (alpha >= beta) { return tte.eval; }
            }
        }

        Move[] legalMoves = board.GetLegalMoves();

        if (currentDepth == maxDepth)
        {
            return QuiesceSearch(board, alpha, beta);
        }

        if (legalMoves.Length == 0)
        {
            if (board.IsInCheckmate())
            {
                return -checkmate;
            }
            else
            {
                return 0;
            }
        }

        int bestEval = -inf;
        int evalType = 1; // Upperbound

        foreach (Move move in OrderMoves(legalMoves, board))
        {
            if (timer.MillisecondsElapsedThisTurn > remainingTime && !moveToMake.IsNull)
            {
                abort = true;
                break;
            }

            board.MakeMove(move);

            int eval = -Search(board, timer, currentDepth + 1, maxDepth, -beta, -alpha);
            if (eval > bestEval)
            {
                bestEval = eval;
                if (currentDepth == 0)
                {
                    moveToMake = move;
                }
            }

            if (bestEval > alpha)
            {
                evalType = 0; // exact
                alpha = bestEval;
            }

            board.UndoMove(move);

            if (alpha >= beta)
            {
                if (!ttt.ContainsKey(board.ZobristKey))
                    ttt.Add(board.ZobristKey, new TTEntry() { eval = beta, type = -1, depth = currentDepth }); // Lower bound
                break;
            }

        }

        if (!ttt.ContainsKey(board.ZobristKey))
            ttt.Add(board.ZobristKey, new TTEntry() { eval = alpha, type = evalType, depth = currentDepth });
        return bestEval;
    }

    int QuiesceSearch(Board board, int alpha, int beta)
    {

        int baseline = Evaluate(board);
        if (baseline >= beta)
            return beta;
        if (alpha < baseline)
            alpha = baseline;

        foreach (Move m in OrderMoves(board.GetLegalMoves(true), board))
        {
            board.MakeMove(m);
            int eval = -QuiesceSearch(board, -beta, -alpha);
            board.UndoMove(m);

            if (eval >= beta)
                return beta;
            if (eval > alpha)
                alpha = eval;
        }
        return alpha;
    }

    IEnumerable<Move> OrderMoves(IEnumerable<Move> moves, Board board)
    {
        Dictionary<Move, int> moveScores = new();

        foreach (Move move in moves)
        {
            int score = 0;

            if (move.IsCapture)
            {
                score += 500;
            }

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                score -= 500;
            }

            if (moveToMake == move)
            {
                score += 2000;
            }

            moveScores.Add(move, score);
        }

        return from moveScore in moveScores orderby moveScore.Value descending select moveScore.Key;
    }

    public int Evaluate(Board board)
    {
        int eval = 0;

        int matWhite = EvaluateMaterial(board, true);
        int matBlack = EvaluateMaterial(board, false);

        bool isEndgame = matWhite + matBlack < 2401;

        eval += matWhite - matBlack;
        eval += EvaluatePosition(board, isEndgame, true) - EvaluatePosition(board, isEndgame, false);

        return eval * (board.IsWhiteToMove ? 1 : -1);
    }

    public int EvaluateMaterial(Board board, bool isWhite)
    {
        int eval = 0;

        // Evaluate material
        eval += board.GetPieceList(PieceType.Pawn, isWhite).Count() * 100;
        eval += board.GetPieceList(PieceType.Knight, isWhite).Count() * 310;
        eval += board.GetPieceList(PieceType.Bishop, isWhite).Count() * 320;
        eval += board.GetPieceList(PieceType.Rook, isWhite).Count() * 500;
        eval += board.GetPieceList(PieceType.Queen, isWhite).Count() * 900;

        return eval;
    }

    public int EvaluatePosition(Board board, bool isEndgame, bool isWhite)
    {
        int eval = 0;
        ulong bb;

        bb = board.GetPieceBitboard(PieceType.Pawn, isWhite);
        eval += 25 * popCount(254979734628096 & bb);
        eval += 50 * popCount(71802610419499008 & bb);
        eval += -15 * popCount(6690816 & bb);

        bb = board.GetPieceBitboard(PieceType.Knight, isWhite);
        eval += 15 * popCount(66229406269440 & bb);
        eval -= 30 * popCount(18429716493353731071 & bb);

        bb = board.GetPieceBitboard(PieceType.Bishop, isWhite);
        eval += 10 * popCount(26492373172224 & bb);
        eval -= 10 * popCount(18411139144890810879 & bb);

        bb = board.GetPieceBitboard(PieceType.Rook, isWhite);
        eval += 10 * popCount(71776119061217280 & bb);

        bb = board.GetPieceBitboard(PieceType.Queen, isWhite);
        eval += 5 * popCount(66229406401536 & bb);
        eval -= 5 * popCount(18411139144874033663 & bb);

        bb = board.GetPieceBitboard(PieceType.King, isWhite);
        if (isEndgame)
        {
            eval += 20 * popCount(66229406269440 & bb);
            eval -= 30 * popCount(18439922444862211071 & bb);
        }
        else
        {
            eval += 20 * popCount(50115 & bb);
            eval -= 30 * popCount(18446744073709486080 & bb);
        }

        return eval;
    }

    int popCount(ulong x)
    {
        int count = 0;
        while (x > 0)
        {
            count++;
            x &= x - 1;
        }
        return count;
    }
}