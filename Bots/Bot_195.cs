namespace auto_Bot_195;
using ChessChallenge.API;
using System;
public class Bot_195 : IChessBot
{

    Move bestmoveRoot = Move.NullMove;
    int[] mg_value = { 82, 337, 365, 477, 1025, 10000 }, eg_value = { 94, 281, 297, 512, 936, 10000 }, gamephase = { 0, 1, 1, 2, 4, 0 }, pstSqValues = new int[64 * 12];
    decimal[] psts = { 21127509999105692897333363842M, 5944761693418028170774882673M, 16117748108129223748M, 620340135427774994334377586M, 11264121225890864241505390264M, 123983413673828384M, 5921479561974464703542683492M, 11556751242593477065653651584M, 2747892176477299011M, 10607587711295382368100168753M, 5613360198980259466914064550M, 8690464557325984904M, 5654543347257551575956337764M, 15536223011547037908462766473M, 2392582971040569480M, 31978833805358215718061998626M, 19343110721004956322767986M, 9521510185422684485M, 21127510006263387027157697604M, 31650000931709875845268469640M, 18446461496071570500M, 4973676660142558572104274722M, 6005217429217293623977394976M, 2532204003528347904M, 10874524790559605299903550258M, 15888029909217472437557675076M, 3693569835299713826M, 16175664804722145426389872930M, 21146772186208318439878312755M, 6148877230516032835M, 5302505075674570821099341396M, 12196288404421611785476933732M, 2848693289714415191M, 350749344613133840534697539M, 10976400717821328334559475845M, 2767013872805226050M };


    static string ConvertDecToHex(decimal dec) { return String.Format("{0,8:X8}{1,8:X8}{2,8:X8}{3,8:X8}", decimal.GetBits(dec)[3], decimal.GetBits(dec)[2], decimal.GetBits(dec)[1], decimal.GetBits(dec)[0]); }

    //These 2 functions are used to make dec psqt work, they are worth 127 tokens and store 12*64*2 values inside 36 decimals (=36 tokens) TOTAL = ~170
    private int GetSqValueFromSqIndex(int sqIndex, int pieceType, int endGameAddition) { return pstSqValues[(64 * (pieceType % 6)) + ((pieceType <= 5) ? sqIndex : 63 - sqIndex) + endGameAddition]; }
    private void GetAllSqValues()
    {
        //we use range 6 to get all 6 different piece types
        for (int i = 0; i < 12; i++)
        {
            //then we use Range 64 to get each square
            for (int j = 0; j < 64; j++)
            {
                byte b = (byte)ConvertDecToHex(psts[(i * 3) + (j / 24)]).Substring((j / 24 == 2) ? 16 : 8, (j / 24 == 2) ? 16 : 24)[j % 24];
                pstSqValues[i * 64 + j] = (b < '9') ? b - '0' : b - 'K';

            }
        }
    }

    private int Evaluate(Board board)
    {
        //midgame-endgame, 0-whiteMg 1-whiteEg 2-blackMg 3-blackEg
        int[] mgeg = new int[4]; int phase = 0;
        //Eval each piece

        foreach (bool isWhite in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int pind = (int)p - 1 + (isWhite ? 0 : 6);
                ulong bitMask = board.GetPieceBitboard(p, isWhite);
                while (bitMask != 0)
                {
                    phase += gamephase[pind % 6];
                    int ind = BitboardHelper.ClearAndGetIndexOfLSB(ref bitMask), sqMidValue = GetSqValueFromSqIndex(ind, pind, 0), sqEndValue = GetSqValueFromSqIndex(ind, pind, 384);
                    mgeg[(isWhite ? 0 : 2)] += sqMidValue + mg_value[pind % 6];
                    mgeg[(isWhite ? 1 : 3)] += sqEndValue + eg_value[pind % 6];
                }
            }
        }

        phase = (int)MathF.Min(phase, 24);
        return ((mgeg[0] - mgeg[2]) * phase + (mgeg[1] - mgeg[3]) * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }





    // the search function of https://github.com/JacquesRW is very well optimized, so i use his in conjunction with my attemps to store the pst data in decimal numbers
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = (1 << 20);
    TTEntry[] tt = new TTEntry[entries];
    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0, notRoot = ply > 0;

        int best = -30000;

        // Check for repetition (this is much more important than material and 50 move rule draws)
        if (notRoot && board.IsDraw())
            return 0;

        TTEntry entry = tt[key % entries];

        // TT cutoffs
        if (notRoot && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;

        int eval = Evaluate(board);

        // Quiescence search is in the same function as negamax to save tokens
        if (qsearch)
        {
            best = eval;
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }

        // Generate moves, only captures in qsearch
        Move[] moves = board.GetLegalMoves(qsearch);
        int[] scores = new int[moves.Length];

        // Score moves
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            // TT move
            if (move == entry.move) scores[i] = 1000000;
            // https://www.chessprogramming.org/MVV-LVA
            else if (move.IsCapture) scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;

        // Search moves
        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

            // Incrementally sort moves
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if (score > best)
            {
                best = score;
                bestMove = move;
                if (ply == 0) bestmoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;

            }
        }

        // (Check/Stale)mate
        if (!qsearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        // Did we fail high/low or get an exact score?
        int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;

        // Push to TT
        tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);

        return best;
    }

    public Move Think(Board board, Timer timer)
    {
        //CreatePstFile();
        GetAllSqValues();
        bestmoveRoot = Move.NullMove;

        for (int depth = 1; depth <= 50; depth++)
        {
            Search(board, timer, -30000, 30000, depth, 0);

            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
}