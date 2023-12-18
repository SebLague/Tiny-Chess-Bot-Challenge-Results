namespace auto_Bot_338;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_338 : IChessBot
{
    // zKey, Move, eval, depth, flag
    private readonly (ulong, Move, int, int, int)[] TTArray = new (ulong, Move, int, int, int)[0x400000];

    public Board _board;
    private Timer _timer;
    private int _timeLimit;

    private Move _bestMove;
    private Move[] _killerMoves = new Move[2048];
    private int[,,] _historyHeuristic;

    public readonly int[] moveScores = new int[218],
        UnpackedPestoTables = Enumerable.Range(0, 768).Select(i =>
                new[] {
                    3125111004714186341844528163m, 67745789485899438210450698250m, 4955353952884821705629777140m,
                    78628549438240079225716989486m,77060549093910084896217299154m, 1855705761021220951418405633m,
                    5600967619797696873289159660m, 4934820992819312235784572160m,  77060563278816194664792587030m,
                    75823841479873406973759452443m,923609973831009362139156212m,   1856943152822389258200546827m,
                    2475899116475887666870614792m, 77661389911015786957971786496m, 10649697330710992953859974410m,
                    }.Select((w, j) =>
                    (sbyte)Buffer.GetByte(decimal.GetBits(w), i / 64) * (j == 14 ? 9 :
                    (int)((new[] {
                        0x10000000007eff1cUL, 0xffc381810007c3afUL, 0xeac3fdfd7e7e6000UL, 0x28027c7c7e087babUL,
                        0xf99ffffdc708c13dUL, 0x800000000e0e0f0UL,  0x40080d9fc87c6aeUL,  0x3cfe821000246320UL,
                        0x2858809100146611UL, 0xc685c258c70027c3UL, 0xf80100060f835f00UL, 0x6800000446000d24UL,
                        0xe001b290221c321UL, 0xae245a412083851eUL,
                    }[j] >> i % 64) % 2))
                ).Sum()).ToArray();

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        _historyHeuristic = new int[2, 7, 64]; // side to move, piece (0 is null), target square
        _timeLimit = timer.MillisecondsRemaining / 20;

        for (int searchDepth = 1, alpha = -32_000, beta = 32_000; ;)
        {
            int eval = Search(searchDepth, 0, alpha, beta, true);
            // TODO add early break when checkmate found
            if (2 * timer.MillisecondsElapsedThisTurn > _timeLimit)
                return _bestMove;
            if (eval < alpha)
                alpha -= 82;
            else if (eval > beta)
                beta += 82;
            else
            {
                alpha = eval - 41;
                beta = eval + 41;
                searchDepth++;
            }
        }
    }

    int Search(int depth, int plyFromRoot, int alpha, int beta, bool canNMP)
    {
        if (plyFromRoot > 1024)
            return Evaluate();
        bool isNotRoot = plyFromRoot > 0,
                isInCheck = _board.IsInCheck(),
                canFutilityPrune = false,
                canLMR = beta - alpha == 1 && !isInCheck;
        Move currentBestMove = default;

        if (isNotRoot && _board.IsRepeatedPosition())
            return 0;

        ulong zKey = _board.ZobristKey;
        ref var TTMatch = ref TTArray[zKey & 0x3FFFFF];
        int TTEvaluation = TTMatch.Item3,
            TTNodeType = TTMatch.Item5,
            bestEvaluation = -32_100,
            startAlpha = alpha,
            movesExplored = 0,
            evaluation,
            movesScored = 0;

        Move TTMove = TTMatch.Item2;
        if (TTMatch.Item1 == zKey &&
            isNotRoot &&
            TTMatch.Item4 >= depth &&
            (
                TTNodeType == 1 ||
                (TTNodeType == 0 && TTEvaluation <= alpha) ||
                (TTNodeType == 2 && TTEvaluation >= beta))
            )
            return TTEvaluation;

        if (isInCheck)
            depth++;
        bool isQSearch = depth <= 0;

        int MiniSearch(
            int newAlpha,
            int reduction = 1,
            bool canNullMovePrune = true) =>
            evaluation = -Search(depth - reduction, plyFromRoot + 1, -newAlpha, -alpha, canNullMovePrune);

        if (isQSearch)
        {
            bestEvaluation = Evaluate();
            if (bestEvaluation >= beta)
                return beta;
            alpha = Math.Max(alpha, bestEvaluation);
        }
        else if (canLMR) // Token save, the condition for LMR is !isPv and !isInCheck
        {
            // RMF
            evaluation = Evaluate();
            if (depth <= 4 && evaluation - 80 * depth > beta)
                return evaluation;

            // FP
            canFutilityPrune = depth <= 2 && evaluation + 150 * depth < alpha;

            // Pawn Endgame Detection
            //ulong nonPawnPieces = 0;
            //for (int i = 1; ++i < 6;) 
            //    nonPawnPieces |= _board.GetPieceBitboard((PieceType)i, true) | _board.GetPieceBitboard((PieceType)i, false);

            // NMP
            if (depth >= 2 && canNMP && evaluation >= beta)
            {
                _board.ForceSkipTurn();
                // MiniSearch(beta, 4 + depth / 3, false);

                // ~14.7 Elo
                MiniSearch(beta, 3 + depth / 4 + Math.Min(6, (evaluation - beta) / 175), false);
                //  && evaluation >= beta
                _board.UndoSkipTurn();
                if (evaluation >= beta)
                    return evaluation;
            }
        }

        Span<Move> moves = stackalloc Move[218];
        _board.GetLegalMovesNonAlloc(ref moves, isQSearch && !isInCheck);

        foreach (Move move in moves)
            moveScores[movesScored++] = -(
            move == TTMove ? 10_000_000 :
            move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
            move.PromotionPieceType == PieceType.Queen ? 950_000 : // ~17-18 Elo
            _killerMoves[plyFromRoot] == move ? 900_000 :
            _historyHeuristic[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

        moveScores.AsSpan(0, moves.Length).Sort(moves);

        if (!isQSearch && moves.Length == 0) return isInCheck ? -32_000 + plyFromRoot : 0;

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            bool isQuiet = !(move.IsCapture || move.IsPromotion);

            // LMP
            //if (canLMR && depth < 3 && isQuiet && movesExplored > 10 + 2 * depth * depth)
            //    break;

            if (canFutilityPrune && movesExplored > 0 && isQuiet)
                continue;

            _board.MakeMove(move);
            // if isQSearch => full search
            // else if movesExplored == 0 => PV Node => full search
            // else perform LMR or Null Window Search
            //      for both use Null Window, for LMR use reduction of 4
            //      if evaluation > alpha => full search
            if (isQSearch || movesExplored++ == 0 ||
                MiniSearch(alpha + 1, (movesExplored >= 7 && depth >= 2 && canLMR && isQuiet) ? 4 : 1) > alpha)
                MiniSearch(beta);

            _board.UndoMove(move);

            if (evaluation > bestEvaluation)
            {
                bestEvaluation = evaluation;
                currentBestMove = move;
                if (!isNotRoot)
                    _bestMove = move;

                alpha = Math.Max(alpha, evaluation);

                if (alpha >= beta)
                {
                    if (isQuiet)
                    {
                        _killerMoves[plyFromRoot] = move;
                        _historyHeuristic[plyFromRoot & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    }
                    break;
                }

            }
            // SPP
            //else if (depth <= 3 && isQuiet && evaluation + depth * 130 < bestEvaluation)
            //    break;

            if (_timer.MillisecondsElapsedThisTurn > _timeLimit)
                return 32_100;
        }

        TTMatch = new(
            zKey,
            currentBestMove,
            bestEvaluation,
            depth,
            bestEvaluation >= beta ? 2 : bestEvaluation <= startAlpha ? 0 : 1);

        return bestEvaluation;
    }

    #region EVALUATION

    public int Evaluate()
    {
        int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
        for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
            for (piece = 6; --piece >= 0;)
                for (ulong mask = _board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                {
                    // Gamephase, middlegame -> endgame
                    // Multiply, then shift, then mask out 4 bits for value (0-16)
                    gamephase += 0x00042110 >> piece * 4 & 0x0F;

                    // Material and square evaluation
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                    middlegame += UnpackedPestoTables[piece * 64 + square];
                    endgame += UnpackedPestoTables[piece * 64 + square + 384];

                    // Double pawns penalty
                    // ~11.2 Elo
                    if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                    {
                        middlegame -= 15;
                        endgame -= 15;
                    }

                    // Bishop pair
                    // ~10-11 Elo
                    if (piece == 2 && mask != 0)
                    {
                        middlegame += 23;
                        endgame += 62;
                    }
                }
        return (middlegame * gamephase + endgame * (24 - gamephase)) / (_board.IsWhiteToMove ? 24 : -24);
    }

    #endregion
}