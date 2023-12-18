namespace auto_Bot_614;
using ChessChallenge.API;
using System;
using static ChessChallenge.API.BitboardHelper;
using static System.Math;

public class Bot_614 : IChessBot
{
    public int maxSearchTime, searchingDepth, lastScore;

    Timer timer;
    Board board;

    Move searchBestMove, rootBestMove;

    // this tuple is 24 bytes, so the transposition table is precisely 192MiB (~201 MB)
    readonly (
        ulong, // hash
        ushort, // moveRaw
        int, // score
        int, // depth
        int // bound BOUND_EXACT=[1, 2147483647), BOUND_LOWER=2147483647, BOUND_UPPER=0
    )[] transpositionTable = new (ulong, ushort, int, int, int)[0x800000];

    // piece-to history tables, per-color
    readonly int[,,] history = new int[2, 7, 64];

    readonly ulong[] packedData = {
        0x0000000000000000, 0x2328170f2d2a1401, 0x1f1f221929211507, 0x18202a1c2d261507,
        0x252e3022373a230f, 0x585b47456d65321c, 0x8d986f66a5a85f50, 0x0002000300070005,
        0xfffdfffd00060001, 0x2b1f011d20162306, 0x221c0b171f15220d, 0x1b1b131b271c1507,
        0x232d212439321f0b, 0x5b623342826c2812, 0x8db65b45c8c01014, 0x0000000000000000,
        0x615a413e423a382e, 0x6f684f506059413c, 0x82776159705a5543, 0x8b8968657a6a6150,
        0x948c7479826c6361, 0x7e81988f73648160, 0x766f7a7e70585c4e, 0x6c7956116e100000,
        0x3a3d2d2840362f31, 0x3c372a343b3a3838, 0x403e2e343c433934, 0x373e3b2e423b2f37,
        0x383b433c45433634, 0x353d4b4943494b41, 0x46432e354640342b, 0x55560000504f0511,
        0x878f635c8f915856, 0x8a8b5959898e5345, 0x8f9054518f8e514c, 0x96985a539a974a4c,
        0x9a9c67659e9d5f59, 0x989c807a9b9c7a6a, 0xa09f898ba59c6f73, 0xa1a18386a09b7e84,
        0xbcac7774b8c9736a, 0xbab17b7caebd7976, 0xc9ce7376cac57878, 0xe4de6f70dcd87577,
        0xf4ef7175eedc7582, 0xf9fa8383dfe3908e, 0xfffe7a81f4ec707f, 0xdfe79b94e1ee836c,
        0x2027252418003d38, 0x4c42091d31193035, 0x5e560001422c180a, 0x6e6200004d320200,
        0x756c000e5f3c1001, 0x6f6c333f663e3f1d, 0x535b55395c293c1b, 0x2f1e3d5e22005300,
        0x004c0037004b001f, 0x00e000ca00be00ad, 0x02e30266018800eb, 0xffdcffeeffddfff3,
        0xfff9000700010007, 0xffe90003ffeefff4, 0x00000000fff5000d,
    };

    // bitshift amount is implicitly modulo 64, also used in pst part of eval function
    int EvalWeight(int item) => (int)(packedData[item >> 1] >> item * 32);

    public Move Think(Board boardOrig, Timer timerOrig)
    {
        board = boardOrig;
        timer = timerOrig;

        maxSearchTime = timer.MillisecondsRemaining / 4;
        searchingDepth = 1;
        do
            try
            {
                // Aspiration windows
                if (Abs(lastScore - Negamax(lastScore - 20, lastScore + 20, searchingDepth)) >= 20)
                    Negamax(-32000, 32000, searchingDepth);
                rootBestMove = searchBestMove;
            }
            catch
            {
                // out of time
                break;
            }
        while (
            ++searchingDepth <= 200
                && timer.MillisecondsElapsedThisTurn < maxSearchTime / 10
        );

        return rootBestMove;
    }

    public int Negamax(int alpha, int beta, int depth)
    {
        // abort search if out of time, but we must search at least depth 1
        if (timer.MillisecondsElapsedThisTurn >= maxSearchTime && searchingDepth > 1)
            throw new Exception();

        ref var tt = ref transpositionTable[board.ZobristKey & 0x7FFFFF];
        var (ttHash, ttMoveRaw, score, ttDepth, ttBound) = tt;

        bool
            ttHit = ttHash == board.ZobristKey,
            nonPv = alpha + 1 == beta,
            inQSearch = depth <= 0,
            pieceIsWhite;
        int
            eval = 0x000b000a, // tempo
            bestScore = board.PlyCount - 30000,
            oldAlpha = alpha,

            // search loop vars
            moveCount = 0, // quietsToCheckTable = [0, 4, 5, 10, 23]
            quietsToCheck = 0b_010111_001010_000101_000100_000000 >> depth * 6 & 0b111111,

            // temp vars
            tmp = 0;
        if (ttHit)
        {
            if (ttDepth >= depth && ttBound switch
            {
                2147483647 /* BOUND_LOWER */ => score >= beta,
                0 /* BOUND_UPPER */ => score <= alpha,
                // exact cutoffs at pv nodes causes problems, but need it in qsearch for matefinding
                _ /* BOUND_EXACT */ => nonPv || inQSearch,
            })
                return score;
        }
        else if (depth > 3)
            // Internal iterative reduction
            depth--;

        // this is a local function because the C# JIT doesn't optimize very large functions well
        // we do packed phased evaluation, so weights are of the form (eg << 16) + mg
        int Eval(ulong pieces)
        {
            // use tmp as phase (initialized above)
            while (pieces != 0)
            {
                int pieceType, sqIndex;
                Piece piece = board.GetPiece(new(sqIndex = ClearAndGetIndexOfLSB(ref pieces)));
                pieceType = (int)piece.PieceType;
                // virtual pawn type
                // consider pawns on the opposite half of the king as distinct piece types (piece 0)
                pieceType -= (sqIndex & 0b111 ^ board.GetKingSquare(pieceIsWhite = piece.IsWhite).File) >> 1 >> pieceType;
                sqIndex =
                    // Material
                    // not incorporated into the psts to allow packing scheme
                    EvalWeight(112 + pieceType)
                        // psts
                        // piece square table weights are restricted to the range 0-255, so the
                        // bytes between mg and eg and above eg are empty, allowing us to
                        // interleave another packed weight in that space: ddCCddCCbbAAbbAA
                        + (int)(
                            packedData[pieceType * 64 + sqIndex >> 3 ^ (pieceIsWhite ? 0 : 0b111)]
                                >> (0x01455410 >> sqIndex * 4) * 8
                                & 0xFF00FF
                        )
                        // mobility
                        // with the virtual pawn type we get 16 consecutive bytes of unused space
                        // representing impossible pawns on the first/last rank, which is exactly
                        // enough space to fit the 4 relevant mobility weights
                        // treating the king as having queen moves is a form of king safety eval
                        + EvalWeight(11 + pieceType) * GetNumberOfSetBits(
                            GetSliderAttacks((PieceType)Min(5, pieceType), new(sqIndex), board)
                        )
                        // own pawn ahead
                        + EvalWeight(118 + pieceType) * GetNumberOfSetBits(
                            (pieceIsWhite ? 0x0101010101010100UL << sqIndex : 0x0080808080808080UL >> 63 - sqIndex)
                                & board.GetPieceBitboard(PieceType.Pawn, pieceIsWhite)
                        );
                eval += pieceIsWhite == board.IsWhiteToMove ? sqIndex : -sqIndex;
                // phaseWeightTable = [0, 0, 1, 1, 2, 4, 0]
                tmp += 0x0421100 >> pieceType * 4 & 0xF;
            }
            // the correct way to extract EG eval is (eval + 0x8000) >> 16, but this is shorter and
            // the off-by-one error is insignificant
            // the division is also moved outside Eval to save a token
            return (short)eval * tmp + eval / 0x10000 * (24 - tmp);
            // end tmp use
        }
        // using tteval in qsearch causes matefinding issues
        eval = ttHit && !inQSearch ? score : Eval(board.AllPiecesBitboard) / 24;

        if (inQSearch)
            // stand pat in quiescence search
            alpha = Max(alpha, bestScore = eval);
        else if (nonPv && eval >= beta && board.TrySkipTurn())
        {
            // Pruning based on null move observation
            bestScore = depth <= 4
                // Reverse Futility Pruning
                ? eval - 58 * depth
                // Adaptive Null Move Pruning
                : -Negamax(-beta, -alpha, (depth * 100 + beta - eval) / 186 - 1);
            board.UndoSkipTurn();
        }
        if (bestScore >= beta)
            return bestScore;

        if (board.IsInStalemate())
            return 0;

        var moves = board.GetLegalMoves(inQSearch);
        var scores = new int[moves.Length];
        // use tmp as scoreIndex
        tmp = 0;
        foreach (Move move in moves)
            // move ordering:
            // 1. hashmove
            // 2. captures (ordered by most valuable victim, least valuable attacker)
            // 3. quiets (ordered by history)
            scores[tmp++] -= ttHit && move.RawValue == ttMoveRaw ? 1000000
                : Max(
                    (int)move.CapturePieceType * 32768 - (int)move.MovePieceType - 16384,
                    HistoryValue(move)
                );
        // end tmp use

        Array.Sort(scores, moves);
        Move bestMove = default;
        foreach (Move move in moves)
        {
            // Delta pruning
            // deltas = [180, 390, 442, 718, 1332]
            // due to sharing of the top bit of each entry with the bottom bit of the next one
            // (expands the range of values for the queen) all deltas must be even (except pawn)
            if (inQSearch && eval + (0b1_0100110100_1011001110_0110111010_0110000110_0010110100_0000000000 >> (int)move.CapturePieceType * 10 & 0b1_11111_11111) <= alpha)
                break;

            board.MakeMove(move);
            int
                // Check extension
                nextDepth = board.IsInCheck() ? depth : depth - 1,
                reduction = (depth - nextDepth) * Max(
                    // Late move reduction
                    (moveCount * 93 + depth * 144) / 1000
                        // History reduction
                        + scores[moveCount] / 172,
                    0
                );
            if (board.IsRepeatedPosition())
                score = 0;
            else
            {
                // this crazy while loop does the null window searches for PVS: first it searches
                // with the reduced depth, and if it beats alpha it re-searches at full depth
                // ~alpha is equivalent to -alpha-1 under two's complement
                while (
                    moveCount != 0
                        && (score = -Negamax(~alpha, -alpha, nextDepth - reduction)) > alpha
                        && reduction != 0
                )
                    reduction = 0;
                if (moveCount == 0 || score > alpha)
                    score = -Negamax(-beta, -alpha, nextDepth);
            }

            board.UndoMove(move);

            if (score > bestScore)
            {
                alpha = Max(alpha, bestScore = score);
                bestMove = move;
            }
            if (score >= beta)
            {
                if (!move.IsCapture)
                {
                    // use tmp as change
                    // Increased history change when eval < alpha
                    // equivalent to tmp = eval < alpha ? -(depth + 1) : depth
                    // 1. eval - alpha is < 0 if eval < alpha and >= 0 otherwise
                    // 2. >> 31 maps numbers < 0 to -1 and numbers >= 0 to 0
                    // 3. -1 ^ depth = ~depth while 0 ^ depth = depth
                    // 4. ~depth = -depth - 1 = -(depth + 1)
                    // since we're squaring tmp, sign doesn't matter
                    tmp = eval - alpha >> 31 ^ depth;
                    tmp *= tmp;
                    foreach (Move malusMove in moves.AsSpan(0, moveCount))
                        if (!malusMove.IsCapture)
                            HistoryValue(malusMove) -= tmp + tmp * HistoryValue(malusMove) / 512;
                    HistoryValue(move) += tmp - tmp * HistoryValue(move) / 512;
                    // end tmp use
                }
                break;
            }

            // pruning techniques that break the move loop
            if (nonPv && depth <= 4 && !move.IsCapture && (
                // Late move pruning
                quietsToCheck-- == 1 ||
                // Futility pruning
                eval + 127 * depth < alpha
            ))
                break;

            moveCount++;
        }

        tt = (
            board.ZobristKey,
            alpha > oldAlpha // don't update best move if upper bound
                ? bestMove.RawValue
                : ttMoveRaw,
            Clamp(bestScore, -20000, 20000),
            Max(depth, 0),
            bestScore >= beta
                ? 2147483647 /* BOUND_LOWER */
                : alpha - oldAlpha /* BOUND_UPPER if alpha == oldAlpha else BOUND_EXACT */
        );

        searchBestMove = bestMove;
        return lastScore = bestScore;
    }

    ref int HistoryValue(Move move) => ref history[
        board.PlyCount & 1,
        (int)move.MovePieceType,
        move.TargetSquare.Index
    ];
}
