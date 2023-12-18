namespace auto_Bot_180;
using ChessChallenge.API;
using System;

public class Bot_180 : IChessBot
{
    private const int MaxDepth = 3;
    private bool _isWhite;
    private int _timePerTurn = 0;
    public Move Think(Board board, Timer timer)
    {
        if (_timePerTurn == 0)
        {
            _timePerTurn = timer.MillisecondsRemaining / 75;
        }
        _isWhite = board.IsWhiteToMove;

        var bestMove = new Move();
        var bestUtil = double.MinValue;

        foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            var util = MinDecision(board, 0, timer, double.MinValue, double.MaxValue);
            board.UndoMove(move);
            if (!(util > bestUtil)) continue;
            bestMove = move;
            bestUtil = util;
        }

        return bestMove;
    }

    private double MinDecision(Board board, int depth, Timer timer, double alpha, double beta)
    {
        var allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0 || depth >= MaxDepth || timer.MillisecondsElapsedThisTurn > _timePerTurn)
        {
            return GetBoardValue(board);
        }

        var min = double.MaxValue;

        foreach (var move in allMoves)
        {
            board.MakeMove(move);
            var util = MaxDecision(board, depth + 1, timer, alpha, beta);
            board.UndoMove(move);
            min = Math.Min(min, util);
            if (min <= alpha) return min;
            beta = Math.Min(beta, min);
        }

        return min;
    }

    private double MaxDecision(Board board, int depth, Timer timer, double alpha, double beta)
    {
        var allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0 || depth >= MaxDepth || timer.MillisecondsElapsedThisTurn > _timePerTurn)
        {
            return GetBoardValue(board);
        }

        var max = double.MinValue;

        foreach (var move in allMoves)
        {
            board.MakeMove(move);
            var util = MinDecision(board, depth + 1, timer, alpha, beta);
            board.UndoMove(move);
            max = Math.Max(max, util);
            if (max >= beta) return max;
            alpha = Math.Max(alpha, max);

        }

        return max;
    }

    private double GetBoardValue(Board board)
    {
        var kingScore = _isWhite
            ? 200 * (board.GetPieceList(PieceType.King, true).Count - board.GetPieceList(PieceType.King, false).Count)
            : 200 * (board.GetPieceList(PieceType.King, false).Count - board.GetPieceList(PieceType.King, true).Count);
        var queenScore = _isWhite
            ? 9 * (board.GetPieceList(PieceType.Queen, true).Count - board.GetPieceList(PieceType.Queen, false).Count)
            : 9 * (board.GetPieceList(PieceType.Queen, false).Count - board.GetPieceList(PieceType.Queen, true).Count);
        var rookScore = _isWhite
            ? 5 * (board.GetPieceList(PieceType.Rook, true).Count - board.GetPieceList(PieceType.Rook, false).Count)
            : 5 * (board.GetPieceList(PieceType.Rook, false).Count - board.GetPieceList(PieceType.Rook, true).Count);
        var bishopScore = _isWhite
            ? 3 * (board.GetPieceList(PieceType.Bishop, true).Count - board.GetPieceList(PieceType.Bishop, false).Count)
            : 3 * (board.GetPieceList(PieceType.Bishop, false).Count - board.GetPieceList(PieceType.Bishop, true).Count);
        var knightScore = _isWhite
            ? 3 * (board.GetPieceList(PieceType.Knight, true).Count - board.GetPieceList(PieceType.Knight, false).Count)
            : 3 * (board.GetPieceList(PieceType.Knight, false).Count - board.GetPieceList(PieceType.Knight, true).Count);
        var pawnScore = _isWhite
            ? (board.GetPieceList(PieceType.Pawn, true).Count - board.GetPieceList(PieceType.Pawn, false).Count)
            : (board.GetPieceList(PieceType.Pawn, false).Count - board.GetPieceList(PieceType.Pawn, true).Count);

        var mate = board.IsInCheckmate() ? 500 : 0;
        var check = board.IsInCheck() ? 150 : 0;

        var playerMoves = board.GetLegalMoves().Length;

        var skipped = board.TrySkipTurn();
        int? enemyMoves = null;
        if (skipped)
        {
            enemyMoves = board.GetLegalMoves().Length;
            board.UndoSkipTurn();
        }

        double movement = 0;
        if (enemyMoves is not null && playerMoves != enemyMoves)
        {
            movement = 0.1 * (playerMoves - (int)enemyMoves);
        }

        var alreadySeen = board.IsRepeatedPosition() ? -1000 : 0;

        return kingScore + queenScore + rookScore + bishopScore
               + knightScore + pawnScore
               + mate + check + movement
               + alreadySeen;
    }
}