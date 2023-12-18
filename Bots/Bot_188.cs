namespace auto_Bot_188;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_188 : IChessBot
{
    class ValuedMove
    {
        public int Value { get; set; }
        public Move Move { get; set; }
    }

    class TTEntry : ValuedMove
    {
        public int Depth { get; set; }
        public int Flag { get; set; }
    }

    private readonly int[] PIECE_VALUES = { 0, 1000, 3100, 3200, 5000, 9000, 100000 };
    Dictionary<ulong, TTEntry> _transpositionTable;
    ValuedMove[,] _killers;
    int[,] _historyHeuristic;
    Move _bestMove;
    Timer _timer;
    int _turnMilliseconds;
    int _maxDepth;

    bool Continue => _timer.MillisecondsElapsedThisTurn < _turnMilliseconds;

    static int[] CentreManhattanDistance;

    public Move Think(Board board, Timer timer)
    {
        // Moved from illegal namespace -- seb
        CentreManhattanDistance = new int[64];
        for (int squareA = 0; squareA < 64; squareA++)
        {
            Square coordA = new Square(squareA);
            int fileDstFromCentre = Math.Max(3 - coordA.File, coordA.File - 4);
            int rankDstFromCentre = Math.Max(3 - coordA.Rank, coordA.Rank - 4);
            CentreManhattanDistance[squareA] = fileDstFromCentre + rankDstFromCentre;
        }

        _transpositionTable = new();
        _killers = new ValuedMove[2, 100];
        _historyHeuristic = new int[64, 64];
        _bestMove = Move.NullMove;
        _timer = timer;
        _turnMilliseconds = board.PlyCount < 20
            ? 50 * (board.PlyCount + 20)
            : timer.MillisecondsRemaining / 30;

        for (_maxDepth = 4; _maxDepth <= 100 && Continue; _maxDepth += 1)
        {
            NegaMax(board, _maxDepth, -100000000, 100000000, board.IsWhiteToMove ? 1 : -1);
        }

        return _bestMove;
    }

    int NegaMax(Board board, int depth, int alpha, int beta, int player)
    {
        if (!Continue)
            return alpha;

        int initialAlpha = alpha;

        if (_transpositionTable.TryGetValue(board.ZobristKey, out TTEntry? info) && info.Depth >= depth)
        {
            if (info.Flag == 0)  // Exact
                return info.Value;
            else if (info.Flag == 1)  // Lower
                alpha = Math.Max(alpha, info.Value);
            else if (info.Flag == 2)  // Upper
                beta = Math.Min(beta, info.Value);

            if (alpha >= beta)
                return info.Value;
        }

        var moves = board.GetLegalMoves();

        if (depth <= 0 || board.IsInCheckmate() || board.IsDraw())
            return GetBoardScore(board, depth, moves) * player;

        var orderedMoves = moves
            .OrderByDescending(move => GetMoveScore(move, info, depth))
            .ThenByDescending(move => _historyHeuristic[move.StartSquare.Index, move.TargetSquare.Index]);

        int moveNr = 0;
        Move bestMove = Move.NullMove;
        foreach (Move move in orderedMoves)
        {
            board.MakeMove(move);
            int newDepth = moveNr < 20 ? depth - 1 : depth - 2;  // Late move reduction
            var score = -NegaMax(board, newDepth, -beta, -alpha, -player);
            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
            }
            if (alpha >= beta)
            {
                _killers[0, _maxDepth - depth] = new ValuedMove { Value = alpha, Move = bestMove };
                break;
            }

            moveNr++;
        }

        if (depth == _maxDepth && Continue && bestMove != Move.NullMove)
            _bestMove = bestMove;

        _transpositionTable[board.ZobristKey] = new TTEntry
        {
            Value = alpha,
            Depth = depth,
            Flag = alpha <= initialAlpha ? 2 : alpha >= beta ? 1 : 0,
            Move = bestMove
        };

        if (_killers[1, _maxDepth - depth] is null || alpha > _killers[1, _maxDepth - depth].Value)
            _killers[1, _maxDepth - depth] = new ValuedMove { Value = alpha, Move = bestMove };

        _historyHeuristic[bestMove.StartSquare.Index, bestMove.TargetSquare.Index] += depth * depth;

        return alpha;
    }

    int GetMoveScore(Move move, TTEntry? info, int depth)
    {
        if (move == info?.Move)
            return 100000;
        if (move.IsCapture)
            return 10000 + (int)move.CapturePieceType * 10 + (10 - (int)move.MovePieceType);
        if (move == _killers[0, _maxDepth - depth]?.Move)
            return 1000;
        if (move == _killers[1, _maxDepth - depth]?.Move)
            return 100;
        if (move.IsPromotion)
            return 10;
        if (move.IsCastles)
            return 1;
        return 0;
    }

    int GetBoardScore(Board board, int depth, Move[] moves)
    {
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -9999999 - depth : 9999999 + depth;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        var movementScore = moves.Length * (board.IsWhiteToMove ? 1 : -1);
        var pieceScore = board.GetAllPieceLists().SelectMany(pl => pl).Sum(p => GetPieceScore(board, p));

        return movementScore + pieceScore;
    }

    int GetPieceScore(Board board, Piece piece)
    {
        var score = (
            PIECE_VALUES[(int)piece.PieceType]
            - CentreManhattanDistance[piece.Square.Index] * 5
        ) * (piece.IsWhite ? 1 : -1);

        if (piece.PieceType == PieceType.Pawn)
        {
            score += piece.Square.Rank * piece.Square.Rank;
        }

        return score;
    }
}