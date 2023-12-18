namespace auto_Bot_428;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_428 : IChessBot
{
    private const double MinCheckmateValue = 1000;

    private readonly double[] PieceValues = new[]
    {
        0,
        1,
        3,
        3,
        5,
        9,
        MinCheckmateValue,
    };

    private Board _board;
    private Timer _timer;

    private Random _random = new Random();
    private Dictionary<ulong, Move> _hashMoves = new();
    private Dictionary<(int Depth, ulong BoardHash), (double Score, Move Move, bool BetaCutoff, bool AlphaCutoff)> _transpositionTable = new();
    private Move _searchBestMove = Move.NullMove;

#if DEBUG
    private int _nodesSearched = 0;

    private int _transpositionTableHits = 0;
    private int _transpositionTableCutoffs = 0;
#endif

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;

        var depth = 1;
        var previousMove = Move.NullMove;
        var score = 0D;
        var timeout = TimeSpan.FromMilliseconds(Math.Min(Math.Max(timer.MillisecondsRemaining - 2000, 25), 2000));
        var sameMoveCounter = 0;

        while (_timer.MillisecondsElapsedThisTurn < timeout.TotalMilliseconds && Math.Abs(score) < MinCheckmateValue && (sameMoveCounter < 2 || timer.MillisecondsElapsedThisTurn < 250))
        {
#if DEBUG
            _nodesSearched = 0;
            _transpositionTableHits = 0;
            _transpositionTableCutoffs = 0;
#endif
            _transpositionTable.Clear();
            var newScore = Search(timeout, board.IsWhiteToMove, depth++);
#if DEBUG
            DivertedConsole.Write($"{depth - 1, -3} {_nodesSearched, -8} {_searchBestMove, -12} {_transpositionTableCutoffs}/{_transpositionTableHits}");
            if (_timer.MillisecondsElapsedThisTurn >= timeout.TotalMilliseconds)
            {
                DivertedConsole.Write("(timed out)");
            }
            else
                score = newScore;
#endif

            sameMoveCounter = _searchBestMove == previousMove ? sameMoveCounter + 1 : 0;
            previousMove = _searchBestMove;
        }

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Green;
        DivertedConsole.Write($"{_searchBestMove,-12} {score:F4}");
        Console.ResetColor();
#endif

        return _searchBestMove;
    }

    private double Search(TimeSpan timeout, bool playingAsWhite, int maxDepth, int depth = 0, double alpha = double.MinValue, double beta = double.MaxValue, int extensionsRemaining = 16)
    {
        if (_timer.MillisecondsElapsedThisTurn >= timeout.TotalMilliseconds)
            return 0;

#if DEBUG
        _nodesSearched++;
#endif

        var isOurTurn = !(playingAsWhite ^ _board.IsWhiteToMove);

        if (_board.IsInCheckmate())
            return (isOurTurn ? -MinCheckmateValue : MinCheckmateValue) * (maxDepth - depth + 1);
        else if (_board.IsDraw())
            return 0;
        else if (depth >= maxDepth)
            return BoardEvaluation(playingAsWhite);

        var transpositionTableKey = (depth, _board.ZobristKey);
        Move priorityMove;

        if (_transpositionTable.TryGetValue(transpositionTableKey, out var record))
        {
#if DEBUG
            _transpositionTableHits++;
            _transpositionTableCutoffs++;
#endif

            if (record.BetaCutoff)
                beta = Math.Min(beta, record.Score);
            else if (record.AlphaCutoff)
                alpha = Math.Max(alpha, record.Score);
            else
                return record.Score;

#if DEBUG
            _transpositionTableCutoffs--;
#endif

            priorityMove = record.Move;
        }
        else
            _hashMoves.TryGetValue(_board.ZobristKey, out priorityMove);

        var bestMove = Move.NullMove;
        var bestMoveScore = isOurTurn ? double.MinValue : double.MaxValue;

        bool betaCutoff = false, alphaCutoff = false;

        foreach (var move in _board.GetLegalMoves()
            .OrderByDescending(x => EstimateMoveImportance(x))
            .OrderByDescending(x => priorityMove == x))
        {
            _board.MakeMove(move);
            try
            {
                var eval = BoardEvaluation(playingAsWhite);

                var extend = extensionsRemaining >= 1
                    && _board.IsInCheck();

                var score = Search(
                    timeout,
                    playingAsWhite,
                    extend ? maxDepth + 1 : maxDepth,
                    depth: depth + 1,
                    alpha: alpha,
                    beta: beta,
                    extensionsRemaining: extend ? extensionsRemaining - 1 : extensionsRemaining);

                if (_timer.MillisecondsElapsedThisTurn >= timeout.TotalMilliseconds)
                    return 0;

                if (isOurTurn)
                {
                    if (score >= beta)
                    {
                        betaCutoff = true;
                        bestMove = move;
                        bestMoveScore = score;
                        break;
                    }
                    alpha = Math.Max(score, alpha);
                }
                else
                {
                    if (score <= alpha)
                    {
                        alphaCutoff = true;
                        bestMove = move;
                        bestMoveScore = score;
                        break;
                    }
                    beta = Math.Min(score, beta);
                }
                if (isOurTurn && score > bestMoveScore || !isOurTurn && score < bestMoveScore)
                {
                    bestMoveScore = score;
                    bestMove = move;
                    if (depth == 0)
                        _searchBestMove = move;
                }
            }
            finally
            {
                _board.UndoMove(move);
            }
        }

        _hashMoves[_board.ZobristKey] = bestMove;
        _transpositionTable[transpositionTableKey] = (bestMoveScore, bestMove, betaCutoff, alphaCutoff);

        return bestMoveScore;
    }

    // We value:
    //  - having pieces (obviously)
    //  - how many squares are threatened in the center of the board
    //  - not having threatened undefended pieces (such pieces are not counted in material cost)
    private double BoardEvaluation(bool playingAsWhite)
    {
        var evaluation = 0D;

        var isOurTurn = !(playingAsWhite ^ _board.IsWhiteToMove);
        var allOurAttacksBitboard = GetColorAttacksBitboard(_board, playingAsWhite);
        var allTheirAttacksBitboard = GetColorAttacksBitboard(_board, !playingAsWhite);

        // material (hanging pieces do not count)
        foreach (var piece in _board.GetAllPieceLists().SelectMany(x => x))
        {
            var rank = piece.Square.Rank;
            var file = piece.Square.File;

            var pieceValue = PieceValues[(int)piece.PieceType];

            if (!(playingAsWhite ^ piece.IsWhite)) // if our piece
            {
                if (isOurTurn || !BitboardHelper.SquareIsSet(~allOurAttacksBitboard & allTheirAttacksBitboard & (playingAsWhite ? _board.WhitePiecesBitboard : _board.BlackPiecesBitboard), piece.Square))
                {
                    evaluation += pieceValue;
                }
            }
            else if (!isOurTurn || !BitboardHelper.SquareIsSet(~allTheirAttacksBitboard & allOurAttacksBitboard & (playingAsWhite ? _board.BlackPiecesBitboard : _board.WhitePiecesBitboard), piece.Square))
            {
                evaluation -= pieceValue;
            }
        }

        // center control (0.1 of a pawn)
        ulong centerBitboard = 0b0000000000000000001111000011110000111100001111000000000000000000;
        var centerControl = (BitboardHelper.GetNumberOfSetBits(allOurAttacksBitboard & centerBitboard) - BitboardHelper.GetNumberOfSetBits(allTheirAttacksBitboard & centerBitboard)) / 16D;
        evaluation += centerControl / 10;

        return evaluation;
    }

    private double EstimateMoveImportance(Move move)
    {
        var captureFactor = move.IsCapture ? PieceValues[(int)move.CapturePieceType] : 0;
        var pieceValueFactor = move.MovePieceType == PieceType.King ? 0 : PieceValues[(int)move.MovePieceType];
        return 10000 * captureFactor + 10 * pieceValueFactor + _random.NextDouble();
    }

    private ulong GetColorAttacksBitboard(Board board, bool white)
    {
        var result = 0UL;
        foreach (var pieceList in board.GetAllPieceLists())
        {
            if (pieceList.IsWhitePieceList ^ white)
                continue;
            foreach (var piece in pieceList)
                result |= BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, white);
        }

        return result;
    }
}