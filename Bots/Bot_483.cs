namespace auto_Bot_483;

using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

// ReSharper disable once CheckNamespace
public class Bot_483 : IChessBot
{
    private readonly int[] _pieceValues = {
        82, 337, 365, 477, 1025, 0,
        94, 281, 297, 512, 936, 0
    }, _piecePhase = { 0, 1, 1, 2, 4, 0 }, _moveScores = new int[218];
    private readonly int[][] _unpackedPesto;
    private Move _bestMoveRoot;
    private bool _timeExpired;
    private int _phase, _searchTime, _i, _depth, _nodes, _lbound, _rbound, _eval;
    private Move[] _killerMoves;
    private readonly (ulong, Move, int, int, int)[] TT = new (ulong, Move, int, int, int)[0x400000];

    public Bot_483()
    {
        _unpackedPesto = new[] {
            63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m,  936945638387574698250991104m,   75531285965747665584902616832m,
            77047302762000299964198997571m, 3730792265775293618620982364m,  3121489077029470166123295018m,  3747712412930601838683035969m,  3763381335243474116535455791m,  8067176012614548496052660822m,  4977175895537975520060507415m,  2475894077091727551177487608m,
            2458978764687427073924784380m,  3718684080556872886692423941m,  4959037324412353051075877138m,  3135972447545098299460234261m,  4371494653131335197311645996m,  9624249097030609585804826662m,  9301461106541282841985626641m,  2793818196182115168911564530m,
            77683174186957799541255830262m, 4660418590176711545920359433m,  4971145620211324499469864196m,  5608211711321183125202150414m,  5617883191736004891949734160m,  7150801075091790966455611144m,  5619082524459738931006868492m,  649197923531967450704711664m,
            75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m,  4990460191572192980035045640m,  5597312470813537077508379404m,  4980755617409140165251173636m,  1890741055734852330174483975m,  76772801025035254361275759599m,
            75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m,  4338830174078735659125311481m,  4960199192571758553533648130m,  3420013420025511569771334658m,  1557077491473974933188251927m,  77376040767919248347203368440m,
            73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m,  3081561358716686153294085872m,  3392217589357453836837847030m,  1219782446916489227407330320m,  78580145051212187267589731866m, 75798434925965430405537592305m,
            68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
        }.Select(packedTable => new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
            .Select(square => (int)((sbyte)square * 1.461) + _pieceValues[_i++ % 12])
            .ToArray()
        ).ToArray();
    }

    public Move Think(Board board, Timer timer)
    {
        var historyHeuristic = new int[2, 7, 64];

        int Evaluate()
        {
            int endgame = 0, midgame = _phase = 0, side = 2;
            for (; --side >= 0; midgame = -midgame, endgame = -endgame)
                for (_i = 0; _i < 6; _i++)
                    for (ulong pieceBitboard = board.GetPieceBitboard((PieceType)_i + 1, side > 0); pieceBitboard != 0;)
                    {
                        _phase += _piecePhase[_i];
                        int idx = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitboard) ^ side * 56;
                        midgame += _unpackedPesto[idx][_i];
                        endgame += _unpackedPesto[idx][_i + 6];
                        if (_i != 2 || pieceBitboard == 0) continue;
                        midgame += 20;
                        endgame += 30;
                    }

            return (midgame * _phase + endgame * (24 - _phase)) / (board.IsWhiteToMove ? 24 : -24) + _phase / 2;
        }

        int Search(int alpha, int beta, int depth, int ply)
        {
            ulong key = board.ZobristKey;
            bool isInCheck = board.IsInCheck(), isQSearch = (isInCheck ? depth : --depth) <= 0, isRoot = ++ply == 0, doFutilityPruning = false, isZeroWidth = beta - alpha == 1;

            if (!isRoot && board.IsRepeatedPosition()) return 0;

            ref var entry = ref TT[key & 0x3FFFFF];
            int bestEval = -100000000, eval = entry.Item4, bound = entry.Item5, i = 0;
            if (!isRoot && entry.Item1 == key && entry.Item3 >= depth && (bound == 1 || bound == 2 && eval >= beta || bound == 3 && eval <= alpha)) return eval;

            int ZWSearch(int reduction) => eval = -Search(-alpha - 1, -alpha, depth - reduction, ply);

            if (isQSearch)
            {
                bestEval = Evaluate();
                if (bestEval >= beta) return bestEval;
                alpha = Max(alpha, bestEval);
            }

            else if (isZeroWidth && !isInCheck)
            {
                int staticEval = (eval = Evaluate()) - 96 * depth;
                if (depth <= 10 && staticEval >= beta) return staticEval;

                if (_phase > 0 && eval >= beta)
                {
                    board.TrySkipTurn();
                    ZWSearch(2 + depth / 5 + Min(6, (eval - beta) / 175));
                    board.UndoSkipTurn();
                    if (eval >= beta) return eval;
                }

                doFutilityPruning = depth <= 8 && staticEval + 237 * depth <= alpha;
            }

            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, isQSearch && !isInCheck);
            int moveCount = moves.Length, originalAlpha = alpha;
            if (!isQSearch && moveCount == 0) return isInCheck ? ply - 10000000 : 0;

            foreach (var move in moves)
            {
                var pieceType = (int)move.MovePieceType;
                _moveScores[i++] = -(move == entry.Item2 ? 10000000
                    : move.IsCapture ? 1000000 * (int)move.CapturePieceType - pieceType : _killerMoves[ply] == move ? 100000
                    : historyHeuristic[ply & 1, pieceType, move.TargetSquare.Index]);
            }
            Move bestMove = default;

            _moveScores.AsSpan(i = 0, moveCount).Sort(moves);
            if (isRoot) _bestMoveRoot = moves[0];
            foreach (var move in moves)
            {
                if (_timeExpired || ++_nodes % 2048 == 0 && (_timeExpired = timer.MillisecondsElapsedThisTurn >= _searchTime)) return 100000000;
                if (i++ > 0 && doFutilityPruning && !move.IsCapture && !move.IsPromotion) continue;
                board.MakeMove(move);
                if (i <= 1 || isQSearch || (depth < 2 || i <= 6 || ZWSearch(2) > alpha) && ZWSearch(0) > alpha && eval < beta) eval = -Search(-beta, -alpha, depth, ply);

                board.UndoMove(move);

                if (eval <= bestEval) continue;
                bestEval = eval;
                bestMove = move;
                if (isRoot) _bestMoveRoot = bestMove;
                alpha = Max(alpha, eval);
                if (alpha < beta) continue;
                if (move.IsCapture) break;
                _killerMoves[ply] = move;
                historyHeuristic[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] +=
                    isQSearch ? 1 : depth * depth;
                break;
            }
            entry = new(key, bestMove, depth, bestEval, bestEval < beta ? bestEval > originalAlpha ? 1 : 3 : 2);
            return bestEval;
        }

        (_depth, _timeExpired, _killerMoves, _searchTime, _lbound, _rbound) =
        (3, false, new Move[0xFF], timer.MillisecondsRemaining / 13, -10000000, 10000000);

        for (; _depth < 64 && timer.MillisecondsElapsedThisTurn * 3 <= _searchTime;)
            if ((_eval = Search(_lbound, _rbound, _depth, -1)) >= _rbound) _rbound += 62;
            else if (_eval <= _lbound) _lbound -= 62;
            else
            {
                _lbound = _eval - 17;
                _rbound = _eval + 17;
                _depth++;
            }

        return _bestMoveRoot;
    }
}