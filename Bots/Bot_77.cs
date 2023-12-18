namespace auto_Bot_77;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Might still be private at the time of reading this
// https://github.com/maartenpeels/PetitePawnWizard

public class Bot_77 : IChessBot
{
    // Super ugly, but saves tokens by not having to pass these around
    private Board? _board;
    private Timer? _timer;
    private int _maxMoveTime;

    // _, pawn, knight, bishop, rook, queen, king
    private readonly int[] _pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    // 6 Piece-Square Tables
    // Values range from -50 to 50
    // Add 50 so that values range from 0 to 100
    // 100 can be stored in 7 bits
    // 7 * 7 = 49 bits = ulong(64 bits)
    private readonly ulong[] _pieceSquareTablesEncoded =
    {
        440500004290610, 88315370538290, 88315370539570, 87973115333170, 87973115333170, 88315370539570, 88315370538290,
        695353180210, 88658978407780, 88318076030820, 220259471366500, 219915873982820, 219915873982820,
        220259471366500, 88318076030820, 88658978407780, 88658957437500, 176278985283900, 308221722877510,
        351858590687440, 351858590687440, 308221722877510, 176278985283900, 88658957437500, 88660299614775,
        176278985366455, 352202187989180, 395839055799115, 395839055799115, 352202187989180, 176278985366455,
        88660299614775, 89005239175730, 176622582667570, 352545785454770, 396182653182790, 396182653182790,
        352545785454770, 176622582667570, 89003896998450, 89346152204855, 176967522393005, 308908917726760,
        352889382838450, 352889382838450, 308908917726760, 176966180215725, 89346152204855, 90376944354615,
        134360104554300, 177654716995900, 221633839930270, 221633839930270, 177653374818620, 134360104554300,
        90376944354615, 2413340098610, 46740087113010, 90033357457970, 133671577848370, 133671577848370, 90033357457970,
        46740087113010, 2413340098610
    };

    private readonly int[][] _pieceSquareTables = new int[7][];

    private enum NodeTypes
    {
        Exact,
        LowerBound,
        UpperBound
    }

    private readonly Dictionary<ulong, (int score, Move move, int depth, NodeTypes type)> _transpositionTable =
        new();

    private const ulong TableSize = 10000000;

    public Bot_77()
    {
        DecodePieceSquareTables();
    }

    public Move Think(Board b, Timer t)
    {
        _maxMoveTime = (int)Math.Min((float)t.GameStartTimeMilliseconds / 40, t.MillisecondsRemaining * 0.1);
        _board = b;
        _timer = t;

        var bestMove = _board.GetLegalMoves().First();
        var depth = 1;
        var alpha = -2147483648;
        var beta = 2147483647;

        while (depth < 20 && _timer.MillisecondsElapsedThisTurn < _maxMoveTime)
        {
            var result = NegaMax(depth, 0, alpha, beta);

            // Aspiration Window
            if (result.score <= alpha || result.score >= beta)
            {
                alpha = -2147483648;
                beta = 2147483647;
                continue;
            }

            bestMove = result.move;
            alpha = result.score - 30;
            beta = result.score + 30;
            depth++;
        }

        return bestMove;
    }

    private void OrderMoves(ref Span<Move> moves, Move moveToSearchFirst) => moves.Sort((a, b) =>
        -EvaluateMove(a, moveToSearchFirst)
            .CompareTo(EvaluateMove(b, moveToSearchFirst)));

    private void DecodePieceSquareTables()
    {
        for (var index = 0; index < 7; index++)
        {
            var table = new int[64];
            for (var i = 0; i < 64; i++)
            {
                table[i] = (int)((_pieceSquareTablesEncoded[i] >> (index * 7)) & 0x7F) - 50;
            }

            _pieceSquareTables[index] = table;
        }
    }

    private int Evaluate()
    {
        var eval = 0;
        foreach (var pl in _board.GetAllPieceLists())
        {
            var pieceValue = 2 * _pieceValues[(int)pl.TypeOfPieceInList] * (pl.IsWhitePieceList ? 1 : -1);
            var isKingAndEndgame = pl.TypeOfPieceInList == PieceType.King &&
                                   BitboardHelper.GetNumberOfSetBits(_board.AllPiecesBitboard) < 16;
            var pst = _pieceSquareTables[(int)pl.TypeOfPieceInList - 1 + (isKingAndEndgame ? 1 : 0)];
            if (!pl.IsWhitePieceList)
                pst = pst.Reverse().ToArray();

            eval += pl.Sum(p => (pst[p.Square.Index] * (_board.IsWhiteToMove ? 1 : -1)) + pieceValue);
        }

        return eval;
    }


    private float EvaluateMove(Move move, Move searchThisMoveFirst)
    {
        if (move.Equals(searchThisMoveFirst))
            return float.MaxValue;

        var score = 0;

        if (move.IsCapture)
        {
            score = 10 * _pieceValues[(int)_board.GetPiece(move.TargetSquare).PieceType] -
                    _pieceValues[(int)_board.GetPiece(move.StartSquare).PieceType];
            score += _board.SquareIsAttackedByOpponent(move.TargetSquare) ? -100 : 100;
        }

        if (!move.IsPromotion) return score;
        return score + _pieceValues[(int)move.PromotionPieceType];
    }

    private (Move move, int score) NegaMax(int depth, int ply, int alpha, int beta)
    {
        if (ply > 0 && _board.IsRepeatedPosition())
            return (Move.NullMove, 0);

        var originalAlpha = alpha;
        var ttIndex = _board.ZobristKey % TableSize;
        var moveToSearchFirst = Move.NullMove;

        if (_transpositionTable.TryGetValue(ttIndex, out var ttEntry))
        {
            if (ttEntry.depth >= depth)
            {
                switch (ttEntry.type)
                {
                    case NodeTypes.Exact:
                        return (ttEntry.move, ttEntry.score);
                    case NodeTypes.LowerBound:
                        alpha = Math.Max(alpha, ttEntry.score);
                        break;
                    case NodeTypes.UpperBound:
                        beta = Math.Min(beta, ttEntry.score);
                        break;
                }

                if (alpha >= beta)
                    return (ttEntry.move, ttEntry.score);
            }

            moveToSearchFirst = ttEntry.move;
        }

        if (depth == 0)
            return (Move.NullMove, Quiescence(alpha, beta));

        if (_board.IsInCheckmate())
            return (Move.NullMove, -9999);

        if (_board.IsDraw())
            return (Move.NullMove, 0);

        if (_board.IsInCheck() && depth < 20)
            depth++;

        Span<Move> moves = stackalloc Move[256];
        _board.GetLegalMovesNonAlloc(ref moves);
        OrderMoves(ref moves, moveToSearchFirst);

        var bestScore = -1000;
        var bestMove = moves[0];

        foreach (var move in moves)
        {
            _board.MakeMove(move);
            var score = -NegaMax(depth - 1, ply + 1, -beta, -alpha).score;
            _board.UndoMove(move);

            if (bestScore < score)
            {
                bestScore = score;
                bestMove = move;
            }

            alpha = Math.Max(alpha, bestScore);

            if (_timer.MillisecondsElapsedThisTurn > _maxMoveTime)
                break;

            if (alpha >= beta)
                break;
        }

        var nodeType = NodeTypes.Exact;
        if (bestScore <= originalAlpha)
            nodeType = NodeTypes.UpperBound;
        else if (bestScore >= beta)
            nodeType = NodeTypes.LowerBound;
        _transpositionTable[ttIndex] = (bestScore, bestMove, depth, nodeType);

        return (bestMove, bestScore);
    }

    private int Quiescence(int alpha, int beta)
    {
        var standPat = _board.IsWhiteToMove ? Evaluate() : -Evaluate();

        if (standPat >= beta)
            return beta;

        if (alpha < standPat)
            alpha = standPat;

        Span<Move> moves = stackalloc Move[256];
        _board.GetLegalMovesNonAlloc(ref moves, true);
        OrderMoves(ref moves, Move.NullMove);

        foreach (var move in moves)
        {
            _board.MakeMove(move);
            var score = -Quiescence(-beta, -alpha);
            _board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }
}