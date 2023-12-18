namespace auto_Bot_480;
using ChessChallenge.API;
using System;

public class Bot_480 : IChessBot
{
    int allowedElapse;
    (Move, Move)[] refutation = new (Move, Move)[64];
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    long[] pieceValue = { 0, 3280094, 13480281, 14600297, 19080512, 41000936, 0 };
    const int TTsize = 1 << 20;
    Move[] TT = new Move[TTsize];
    Board _board;
    Timer _timer;
    public int Search(
        int alpha,
        int beta,
        int depth,
        int ply,
        int nullDepth,
        long evaluation,
        int phase
    )
    {
        // Time Management
        if (_timer.MillisecondsElapsedThisTurn > allowedElapse)
            throw new Exception();

        // Draw Detection
        if (
            _board.IsInsufficientMaterial()
            || _board.IsRepeatedPosition()
            || _board.FiftyMoveCounter >= 100
        )
            return 0;

        int bestScore = -64000,
            score = 0,
            sinceLastIncrease = 0;

        bool qsearch = depth <= 0;

        if (qsearch)
        {
            decimal openingEval = Math.Round(evaluation / 40000m);

            bestScore =
                (
                    phase * (int)openingEval
                    + (24 - phase) * (int)(evaluation - 40000m * openingEval)
                ) / 24
                + ply;

            if (bestScore >= beta)
                return bestScore;
            if (bestScore < alpha - 975 - 40 * (24 - phase))
                return alpha;
            alpha = Math.Max(alpha, bestScore);
        }
        ulong key = _board.ZobristKey;
        Move entry = TT[key % TTsize];

        if (ply > 0 && depth > 5 && nullDepth > 0 && !_board.IsInCheck() && entry != Move.NullMove)
        {
            _board.TrySkipTurn();
            bestScore = -Search(
                -beta,
                -alpha,
                depth - 5,
                ply + 1,
                nullDepth - 1,
                -evaluation,
                phase
            );
            _board.UndoSkipTurn();
            alpha = Math.Max(alpha, bestScore);
            if (alpha >= beta)
                return bestScore;
        }

        Move[] moves = _board.GetLegalMoves(qsearch);
        int[] moveOrder = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            moveOrder[i] = -(
                move == entry
                    ? 188
                    : move == refutation[ply].Item1
                        ? 187
                        : move == refutation[ply].Item2
                            ? 186
                            : move.IsCapture
                                ? (int)move.CapturePieceType * 7 - (int)move.MovePieceType + 151 : move.IsPromotion
                                    ? (int)move.PromotionPieceType + 145
                                    : move.IsCastles
                                        ? 145
                                        : (int)move.MovePieceType * phase
                                            - (int)move.MovePieceType * (24 - phase)
            );
        }

        Array.Sort(moveOrder, moves);

        foreach (Move move in moves)
        {
            _board.MakeMove(move);
            int capturedPiece = (int)move.CapturePieceType,
                movedPiece = (int)move.MovePieceType,
                promotedPiece = (int)move.PromotionPieceType,
                resultPiece = move.IsPromotion ? promotedPiece : movedPiece,
                currentPhase = Math.Clamp(
                    phase - piecePhase[capturedPiece] + piecePhase[promotedPiece],
                    0,
                    24
                );

            long currentEval =
                -evaluation
                - pieceValue[capturedPiece]
                + pieceValue[movedPiece]
                - pieceValue[resultPiece];

            bool LMR = sinceLastIncrease > 3 & !(capturedPiece > 0 || _board.IsInCheck());

            score = -Search(
                -beta,
                -alpha,
                LMR ? depth - 3 : depth - 1,
                ply + 1,
                nullDepth,
                currentEval,
                currentPhase
            );

            if (score > alpha && score < beta && LMR)
                score = -Search(
                    -beta,
                    -alpha,
                    depth - 1,
                    ply + 1,
                    nullDepth,
                    currentEval,
                    currentPhase
                );

            _board.UndoMove(move);

            sinceLastIncrease++;

            if (score <= bestScore)
                continue;

            bestScore = score;
            alpha = Math.Max(alpha, bestScore);

            if (refutation[ply].Item1 != move)
                refutation[ply].Item2 = refutation[ply].Item1;
            refutation[ply].Item1 = move;
            sinceLastIncrease = 0;

            if (alpha >= beta)
                break;
        }
        if (moves.Length <= 0)
            if (qsearch)
                return _board.IsInCheckmate()
                    ? -32000 + ply
                    : _board.IsInStalemate()
                        ? 0
                        : bestScore;
            else
                return _board.IsInCheck() ? -32000 + ply : 0;

        TT[key % TTsize] = refutation[ply].Item1;
        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        long evaluation = 0;
        int phase = 0;

        for (int piece = 2; piece < 14; piece++)
        {
            int pieceType = piece >> 1,
                pieceColor = piece & 1,
                colorMult = pieceColor == 0 ^ board.IsWhiteToMove ? -1 : 1,
                pieceCount = BitboardHelper.GetNumberOfSetBits(
                    board.GetPieceBitboard((PieceType)pieceType, pieceColor == 0)
                );

            evaluation += colorMult * pieceCount * pieceValue[pieceType];
            phase += pieceCount * piecePhase[pieceType];
        }

        allowedElapse = Math.Min(
            timer.MillisecondsRemaining / 2,
            timer.MillisecondsRemaining / 30 + timer.IncrementMilliseconds
        );

        int CurrentDepth = 0;
        Move bestMove = Move.NullMove;

        try
        {
            for (
                CurrentDepth = 1;
                CurrentDepth <= 100;
                CurrentDepth++, bestMove = refutation[0].Item1
            )
                Search(-32000, 32000, CurrentDepth, 0, 1, evaluation, phase);
        }
        catch { }
        return bestMove;
    }
}