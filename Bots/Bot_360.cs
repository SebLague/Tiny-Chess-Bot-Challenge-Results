namespace auto_Bot_360;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using static ChessChallenge.API.PieceType;
// importing static for less token use
using static System.Math;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

// ReSharper disable once CheckNamespace
public class Bot_360 : IChessBot
{
    private Board _board;
    private const int Infinity = 100000000;
    private const int CheckmateVal = -Infinity / 2;
    private Dictionary<ulong, (int, int)> _transpositionTable = new();
    private Dictionary<ulong, int> _quiesceTranspositionTable = new();
    private Timer _timer = new(0);
    private int _thinkTime;

    public Move Think(Board board, Timer timer)
    {
        _timer = timer;
        _transpositionTable = new();
        _quiesceTranspositionTable = new();
        _board = board;
        var bestMoveOfLastIteration = Move.NullMove;

        _thinkTime = Min(timer.MillisecondsRemaining >> 5, 200);
        for (var depth = 0; timer.MillisecondsElapsedThisTurn <= _thinkTime; depth++)
        {
            (var currentScore, bestMoveOfLastIteration) =
                RootSearch(-Infinity, Infinity, depth, bestMoveOfLastIteration);
            if (currentScore == -CheckmateVal) break;
        }

        return bestMoveOfLastIteration;
    }

    private (int, Move) RootSearch(int alpha, int beta, int depth,
        Move bestMoveOfLastIteration)
    {
        var moves = _board.GetLegalMoves();
        ReorderMoves(moves, bestMoveOfLastIteration);
        var bestMove = moves[0];
        foreach (var move in moves)
        {
            if (_timer.MillisecondsElapsedThisTurn >= _thinkTime) break;
            _board.MakeMove(move);
            var score = -Search(-beta, -alpha, depth);
            _board.UndoMove(move);
            if (score >= beta) return (beta, move);
            if (score <= alpha) continue;
            alpha = score;
            bestMove = move;
            if (alpha == -CheckmateVal) break;
        }

        return (alpha, bestMove);
    }

    private void RemoveEntriesIfMemoryIsCritical()
    {
        if (_transpositionTable.Count * 16 + _quiesceTranspositionTable.Count * 12 > 256000 && _transpositionTable.Count != 0)
            _transpositionTable.Remove(_transpositionTable.ElementAt(0).Key);
    }

    private int Search(int alpha, int beta, int depth)
    {
        if (_transpositionTable.TryGetValue(_board.ZobristKey, out var storedEval) && depth <= storedEval.Item2)
            return storedEval.Item1;
        if (_board.IsInCheckmate()) return CheckmateVal;
        if (_board.IsDraw()) return 0;
        if (depth == 0) return QuiesceSearch(alpha, beta);
        var moves = _board.GetLegalMoves();
        ReorderMoves(moves, Move.NullMove);
        foreach (var move in moves)
        {
            _board.MakeMove(move);
            var score = -Search(-beta, -alpha,
                depth - 1
                + Convert.ToInt32(_board.IsInCheck())
                + Convert.ToInt32(move is { MovePieceType: Pawn, TargetSquare.Rank: 6 or 1 }));
            _board.UndoMove(move);
            if (score >= beta) return beta;
            alpha = Max(score, alpha);
        }

        RemoveEntriesIfMemoryIsCritical();
        _transpositionTable[_board.ZobristKey] = (alpha, depth);
        return alpha;
    }

    private int QuiesceSearch(int alpha, int beta)
    {
        if (_quiesceTranspositionTable.TryGetValue(_board.ZobristKey, out var storedEval)) return storedEval;
        if (_board.IsInCheckmate()) return CheckmateVal;
        if (_board.IsDraw()) return 0;

        var standingPat = Evaluate();
        if (standingPat >= beta) return beta;
        alpha = Max(standingPat, alpha);

        var moves = _board.GetLegalMoves(true);
        ReorderMoves(moves, Move.NullMove);
        foreach (var quietMove in moves)
        {
            _board.MakeMove(quietMove);
            var score = -QuiesceSearch(-beta, -alpha);
            _board.UndoMove(quietMove);

            if (score >= beta) return beta;
            alpha = Max(score, alpha);
        }

        RemoveEntriesIfMemoryIsCritical();
        _quiesceTranspositionTable[_board.ZobristKey] = alpha;
        return alpha;
    }

    private int Evaluate()
    {
        var score = CountMaterialFor(true) - CountMaterialFor(false)
                    + _board.GetPieceList(Pawn, true).Sum(piece => ScorePawn(piece.Square, true))
                    - _board.GetPieceList(Pawn, false).Sum(piece => ScorePawn(piece.Square, false))
                    + _board.GetLegalMoves().Length; // legalMoves of opponent are missing

        // Mop Up score | thx Chess Programming Wiki
        if (_board.GetAllPieceLists()[0].Count == 0 && _board.GetAllPieceLists()[6].Count == 0)
        {
            var king1 = _board.GetKingSquare(true);
            var king2 = _board.GetKingSquare(false);
            var losingKing = _board.GetKingSquare(Sign(score) == 0);
            score += 20 * ((((losingKing.File - 4) >> 8) + ((losingKing.Rank - 4) >> 8)) & 7) +
                     4 * (14 - (Abs(king2.Rank - king1.Rank) + Abs(king2.File - king1.File)));
        }

        return score * (_board.IsWhiteToMove ? 1 : -1);
    }

    /*int MopUpScore(bool isWhiteLosing)
    {
        return 20 * ManhattanCenterDistance(_board.GetKingSquare(isWhiteLosing)) +
               4 * (14 - ManhattanDistance(_board.GetKingSquare(true), _board.GetKingSquare(false)));
    }

    private int ManhattanDistance(Square king1, Square king2)
    {
        return Abs(king2.Rank - king1.Rank) + Abs(king2.File - king1.File);
    }

    private int ManhattanCenterDistance(Square square)
    {
        return (((square.File - 4) >> 8) + ((square.Rank - 4) >> 8)) & 7;
    }*/
    // Rank 1 and 8 are impossible
    private readonly int[] _passedPawnBonuses = { 8, 10, 25, 40, 60, 90 };


    private int ScorePawn(Square position, bool isWhite)
    {
        // beautiful version | inspiration from Sebastion Lague
        // not the best code I've written
        /*var entireBoard = ulong.MaxValue;
        var areaInFrontOfThePawn = isWhite ? entireBoard << 8 * (position.Rank + 1) : entireBoard >> 8 * (8 - position.Rank);
        const ulong ABCFiles = 0xC1C1C1C1C1C1C1C1;
        var surroundingFiles = ABCFiles << (position.File+1);
        var wrapAroundMask = (ulong)0x101010101010101 << Abs(position.File - 7);

        BitboardHelper.VisualizeBitboard(areaInFrontOfThePawn & surroundingFiles & ~wrapAroundMask);
        var passedPawnMask = areaInFrontOfThePawn & surroundingFiles & ~wrapAroundMask;
        var pawnsInTheWay = _board.GetPieceBitboard(Pawn, !isWhite) & passedPawnMask;
        return pawnsInTheWay == 0 ? 0 : _passedPawnBonuses[isWhite ? position.Rank - 1 : 6 - position.Rank];*/

        return (_board.GetPieceBitboard(Pawn, !isWhite) & (isWhite ? ulong.MaxValue << 8 * (position.Rank + 1) : ulong.MaxValue >> 8 * (8 - position.Rank)) & 0xC1C1C1C1C1C1C1C1 << (position.File + 1) & ~((ulong)0x101010101010101 << Abs(position.File - 7))) == 0 ? 0 : _passedPawnBonuses[isWhite ? position.Rank - 1 : 6 - position.Rank];
    }

    // Nothing, Pawn, Knight, Bishop, Rook, Queen, King | thx EvilBot
    private readonly int[] _pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    private int CountMaterialFor(bool isWhite)
    {
        var offset = isWhite ? 0 : 6;
        var sum = 0;
        foreach (var pieceList in _board.GetAllPieceLists()[offset..(offset + 5)]) sum += pieceList.Count * _pieceValues[(int)pieceList.TypeOfPieceInList];

        return sum;
    }

    private void ReorderMoves(Move[] moves, Move bestMove)
    {
        Array.Sort(moves, (move1, move2) => ValueMove(move2) - ValueMove(move1));

        int ValueMove(Move move)
        {
            if (move == bestMove) return Infinity;
            // Promote to a high valued piece
            var guess = _pieceValues[(int)move.PromotionPieceType] * 2
            // Capture high value pieces with low value pieces
            //if (move.IsCapture) //leaving this out makes the bot more aggressive => better
                + _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType];

            // Discourage squares attacked by the opponent
            if (_board.SquareIsAttackedByOpponent(move.TargetSquare))
                guess -= _pieceValues[(int)move.MovePieceType];
            /*_board.MakeMove(move);
            // Use already calculated results
            // Todo include depth
            if (_transpositionTable.TryGetValue(_board.ZobristKey, out var storedEval))
                guess -= storedEval.Item1;

            // Is piece protected?
            if (_board.SquareIsAttackedByOpponent(move.TargetSquare))
                guess += _pieceValues[(int)move.MovePieceType] >> 1;
            _board.UndoMove(move);*/

            return guess;
        }
    }
}
