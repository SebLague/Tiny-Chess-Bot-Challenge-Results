namespace auto_Bot_296;
using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

public class Bot_296 : IChessBot
{
    Timer _timer;
    bool TimeIsUp => _timer.MillisecondsElapsedThisTurn * (_panicking ? 10 : 30) > _timer.MillisecondsRemaining;

    Board _board;

    bool _panicking;
    int _lastScore;

    // zobristhash, move, depth, lowerBoundsScore
    const ulong TTSize = 2_000_000;
    (ulong, Move, int, int)[] _transpositionTable = new (ulong, Move, int, int)[TTSize];

    long[] _historyTable;

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        _historyTable = new long[64];

        Move lastBestMove = Move.NullMove;
        _lastScore = -100000; // lastScore is used for panicking
        for (int i = 1; ; i++)
        {
            // alternately, we could start by panicking so it will complete the first branch; this probably isn't helpful though, is it?
            _panicking = false;
            _lastScore = Search(-100000, 100000, i, out var nextBestMove, true);

            // includes optimization where we include partial result
            if (TimeIsUp || i >= 99) // if we get to depth 99, it probably means we have already searched the entire tree multiple times, so let's just return the result
            {
                return nextBestMove.IsNull ? lastBestMove : nextBestMove;
            }

            lastBestMove = nextBestMove;
        }
    }

    int Search(int alpha, int beta, int depth, out Move bestMove, bool isRoot = false)
    {
        bestMove = Move.NullMove;

        bool isNormalSearch = depth > 0;

        if (TimeIsUp)
            return beta; // ensures we always fail low at the caller

        if (_board.IsDraw())
            return 0;

        if (_board.IsInCheckmate())
            return -90000 + _board.PlyCount;

        // don't lookup real TT entry in qsearch
        var ttEntry = isNormalSearch ? _transpositionTable[_board.ZobristKey % TTSize] : default;
        bool ttEntryIsMatch = ttEntry.Item1 == _board.ZobristKey; // item1 is zobrist hash

        var moves = _board.GetLegalMoves().OrderByDescending(move =>
        {
            if (ttEntryIsMatch && ttEntry.Item2 == move) // item2 is move
                // 10,000,000,000
                return 10_000_000_000;

            if (move.IsCapture)
                // between 900,000,000 and 100,000,000
                // should improve move ordering, if this works correctly
                return EvaluatePositivePieceMaterial(move.CapturePieceType) * 1_000_000 -
                    EvaluatePositivePieceMaterial(move.MovePieceType) * 1_000;

            // lower than 100,000,000
            return _historyTable[move.TargetSquare.Index];
        });

        // If there are limited responses, then we need to change how we evaluate. We can't assume a stand pat, and we also want to search a little deeper.
        // Since limited moves can cause a search explosion, only allow them to a depth of... say 6.
        bool possibleZugzwang = _board.IsInCheck() || moves.Count() <= 5 && depth > -6;

        // simple hard cap of 50 ply
        //if (depth <= -50)
        //    return Evaluate();

        int score;

        if (!isNormalSearch && !possibleZugzwang) // protection against zugzwang
        {
            score = Evaluate();

            if (score >= alpha)
                alpha = score;

            if (alpha >= beta)
                return alpha;
        }

        // null move pruning; if a null move raises beta, then this position is probably extremely good
        // TODO: only do null move pruning if beta is less than a mating score... otherwise, NMP is pretty silly.
        if (isNormalSearch && !possibleZugzwang && depth >= 2)
        {
            // we'll just assume this works; possibleZugzwang should rule out us being in check
            _board.TrySkipTurn();
            score = -Search(-beta, -beta + 1, depth - 2, out _);
            _board.UndoSkipTurn();

            if (score >= beta)
                return score;
        }

        int lateMoveNumber = 0;
        foreach (var move in moves)
        {
            // delta pruning
            bool goodCapture = move.IsCapture && EvaluatePositivePieceMaterial(move.CapturePieceType) + 200 > alpha;

            // would be nice to include checks, but we don't know which moves are checks ahead of time
            if (isNormalSearch || goodCapture || move.IsPromotion || possibleZugzwang)
            {
                if (!move.IsCapture)
                    lateMoveNumber++;

                _board.MakeMove(move);

                int reduction = 1;

                // we don't check for "IsInCheck" here, because this gets undone below if it is in check
                if (depth >= 2 && lateMoveNumber >= 12 && !move.IsPromotion)
                    reduction++;

                if (_board.IsInCheck())
                    reduction = 0;

                score = -Search(-beta, -alpha, depth - reduction, out _);

                // research for LMR if we raised alpha
                if (score > alpha && reduction > 1)
                    score = -Search(-beta, -alpha, depth - 1, out _);

                _board.UndoMove(move);

                if (TimeIsUp)
                    return beta; // ensures we always fail low at the caller; returning here prevents a bug where the root thinks it hit a beta cutoff and sets the best move!

                if (score > alpha)
                {
                    bestMove = move;
                    alpha = score;
                }

                if (isRoot)
                    _panicking = alpha < _lastScore - 30; // if we're at the root, and haven't reached what we thought was a good move last time, then we start panicking

                if (alpha >= beta)
                    break;
            }
        }

        // didn't fail low (found a best move)
        // Don't save these values in qsearch
        if (isNormalSearch && bestMove != Move.NullMove)
        {
            _transpositionTable[_board.ZobristKey % TTSize] = (_board.ZobristKey, bestMove, depth, alpha);
            if (!bestMove.IsCapture)
                _historyTable[bestMove.TargetSquare.Index]++;
        }

        return alpha;
    }

    static int[] PieceMaterialsArray = new[] { 0, 100, 290, 310, 510, 900, 0 };

    int EvaluatePositivePieceMaterial(PieceType pieceType) => PieceMaterialsArray[(int)pieceType];

    // returns a tuple of (middlegame, endgame)
    (int, int) EvaluatePositiveMiddlegameAndEndgamePieceSquareTable(PieceType pieceType, Square originalSquare, bool isWhite)
    {
        var squareAsIfWhite = SquareAsIfWhite(originalSquare, isWhite);
        int homeRowDisadvantage = squareAsIfWhite.Rank == 0 ? 10 : 0;
        var fileCenterDistance = Math.Abs(4 - squareAsIfWhite.File);
        var rankCenterDistance = Math.Abs(4 - squareAsIfWhite.Rank);
        var centerDistance = fileCenterDistance + rankCenterDistance;

        switch (pieceType)
        {
            case PieceType.Pawn:
                return (
                    squareAsIfWhite.Rank * 1,
                    squareAsIfWhite.Rank * 10 // pushed pawns are super powerful endgame
                );
            case PieceType.Knight:
                return (
                    -centerDistance * 2 - homeRowDisadvantage,  // knights are better in the center
                    -centerDistance * 2                         // knights are better in the center
                );
            case PieceType.Bishop:
                return (
                    -homeRowDisadvantage,   // bishops are best on longer diagonals... but how to do this?
                    0                       // bishops are best on longer diagonals... but how to do this?
                );
            case PieceType.Rook:
                return (
                    -fileCenterDistance + (squareAsIfWhite.Rank >= 6 ? 20 : 0), // rooks are better on 7th and 8th, and generally better toward the center (in the middle game)
                    squareAsIfWhite.Rank >= 6 ? 1 : 0                           // rooks are better on 7th and 8th, and generally better toward the center (in the middle game)
                );
            case PieceType.Queen:
                return (0, 0); // queens are good everywhere
            case PieceType.King:
                // increase middlegame king value for each pawn touching
                int shiftAmount = originalSquare.Index - 9; // make sure we get original square, not flipped square
                ulong kingPawnShieldMask = (shiftAmount > 0) ? 0b00000000_00000000_00000000_00000000_00000000_00000111_00000101_00000111UL << shiftAmount : 0b00000000_00000000_00000000_00000000_00000000_00000111_00000101_00000111UL >> -shiftAmount;
                ulong allowedPawnsMask = 0b11100111_11100111_11100111_00000000_00000000_11100111_11100111_11100111UL;
                ulong kingPawnShield = _board.GetPieceBitboard(PieceType.Pawn, isWhite) & allowedPawnsMask & kingPawnShieldMask;
                int kingPawnShieldCount = BitOperations.PopCount(kingPawnShield);
                return (
                    -squareAsIfWhite.Rank * 20 + fileCenterDistance + kingPawnShieldCount * 15, // increase value in corners (increase distance from center), and behind pawns
                    -centerDistance * 3 // increase value in center
                );

        }
        throw null; // unreachable
    }

    Square SquareAsIfWhite(Square square, bool isWhite) => isWhite ? square : new Square(square.File, 7 - square.Rank);

    int Evaluate()
    {
        int cumulativeNonpawnMaterialEvaluation = 0, whiteMaterialEvaluation = 0, whiteMiddlegameEvaluation = 0, whiteEndgameEvaluation = 0;

        // TODO: add bonus if side can castle?

        foreach (var piece in _board.GetAllPieceLists().SelectMany(x => x))
        {
            var materialEvaluation = EvaluatePositivePieceMaterial(piece.PieceType);
            if (piece.PieceType != PieceType.Pawn)
                cumulativeNonpawnMaterialEvaluation += materialEvaluation;

            whiteMaterialEvaluation += materialEvaluation * (piece.IsWhite ? 1 : -1);

            var (middlegameScore, endgameScore) = EvaluatePositiveMiddlegameAndEndgamePieceSquareTable(piece.PieceType, piece.Square, piece.IsWhite);
            whiteMiddlegameEvaluation += middlegameScore * (piece.IsWhite ? 1 : -1);
            whiteEndgameEvaluation += endgameScore * (piece.IsWhite ? 1 : -1);
        }

        // TODO: can change order of operations to remove parens; makes the math more confusing
        var isMiddleGame = (Math.Clamp(cumulativeNonpawnMaterialEvaluation, 1500, 2000) - 1500.0) / 500.0;
        int whiteEvaluation = whiteMaterialEvaluation + (int)(whiteMiddlegameEvaluation * isMiddleGame + whiteEndgameEvaluation * (1 - isMiddleGame));
        return (_board.IsWhiteToMove ? whiteEvaluation : -whiteEvaluation) + 10;
    }
}