namespace auto_Bot_583;
using ChessChallenge.API;
using System;

/* TINY CHESS DUCK by @rocketduck_m07
 * 
 * #################################
 * 
 * Features:
 * 
 * - Fail-Soft Alpha Beta Search
 * - Quiescent Search
 * - MVV-LVA Move Ordering
 * - Transpostion Table
 * - Iterative Deepening
 * - Piece-Square Tables Only Evaluation
 * 
 * #################################
 */

public class Bot_583 : IChessBot
{
    struct Transposition // credit goes to selenaut
    {
        public ulong zKey = 0;
        public Move move = Move.NullMove;
        public int eval = 0;
        public sbyte
            depth = -128,
            flag = 0; // INVALID = 0, LOWER = 1, UPPER = 2, EXACT = 3;

        public Transposition() { }
    }
    Transposition[] tpt = new Transposition[0x7FFFFF + 1];
    Board theBoard;
    Move rootBestMove;
    int[]
        piecePhaseValue = { 0, 1, 1, 2, 4, 0 },
        middlegamePieceValue = { 82, 337, 365, 477, 1025, 0 },
        endgamePieceValue = { 94, 281, 297, 512, 936, 0 },
        unpackedPST = new int[768];
    // Packing algorithm can be found at https://github.com/m0hossam/tiny-chess-duck/blob/main/Chess-Challenge/src/My%20Bot/PackingPST.cs
    ulong[] packedPST = {
        9259542123273814144,
        16357001140413309557,
        8829195605423724908,
        8254401669090808169,
        7313418688563415655,
        7384914337435197812,
        6737222767702746730,
        9259542123273814144,
        11081220660097301,
        3987876604305901423,
        5889764664614570412,
        8615829970516021910,
        8323936949179225464,
        7599697423020169584,
        7154940515368202861,
        1687519861085072745,
        7170907476791297912,
        7390528431378371153,
        8117082644194436478,
        8972740228098131838,
        8830870139635010180,
        9263780805260055178,
        9552012216381055361,
        6880781611516057963,
        11577242485982338987,
        11214168400861567660,
        8904630919850999184,
        7527071450275215468,
        6658137190229968489,
        6009895851999132511,
        6084482357074295353,
        7886789834650901350,
        7241961428581395373,
        7519176848743963830,
        8318016057608023993,
        7306369598957387393,
        8603695488949846909,
        8251286653394652805,
        6735286635485691265,
        9182408126746812750,
        4582289962092626573,
        11348908854965197411,
        8617781306342151786,
        8028920214486217308,
        5728408685346316109,
        8246770769504399717,
        9333560970455320968,
        8188824274210166926,
        9259542123273814144,
        18374119105817541887,
        16061197207704294100,
        11572154847021273489,
        10198820791839654783,
        9549736231487373176,
        10198551484841362041,
        9259542123273814144,
        5069491205526864157,
        7455822975978661964,
        7524541400582942039,
        8035431733674084462,
        7960834280560624750,
        7601372000551791722,
        6227482657520642388,
        7155491318799420992,
        8244812703126220648,
        8681963116054150258,
        9401405507207856260,
        9045915848980202370,
        8828055359350013303,
        8394015409149540721,
        8245661556352184677,
        7599658875216755567,
        10199125451369908357,
        10055849173233928323,
        9765923324499426173,
        9548631222335405954,
        9477131093459761269,
        8971306245150767216,
        8825507717624329597,
        8611590021041063020,
        8617240532094257812,
        8040227885603724928,
        7820089199224394633,
        9481933937386764708,
        7970407823045732247,
        8099037312892111493,
        7667768075636792416,
        6873735846648900695,
        3917408671579997295,
        8399651535887701899,
        9984928491787627661,
        8689300325139585667,
        7961402723962161525,
        7889615596832196471,
        7310895313622170479,
        5430896352595961941
    };

    public Bot_583()
    {
        // Unpack PST
        for (int i = 0; i < 96; i++)
            for (int j = 0; j < 8; j++)
            {
                int shift = (8 * (7 - j));
                unpackedPST[j + i * 8] = (int)((packedPST[i] & ((ulong)0b11111111 << shift)) >> shift) - 128; // this is just bitmasking
            }
    }

    public int MoveScore(Move move)
    {
        int score = 0;

        if (move.IsCapture)
            score += 10 * middlegamePieceValue[(int)move.CapturePieceType - 1] - middlegamePieceValue[(int)move.MovePieceType - 1];
        if (move.IsPromotion)
            score += middlegamePieceValue[(int)move.PromotionPieceType - 1];

        return score;
    }

    public int Evaluate()
    {
        int gamePhase = 0,
            middlegameScore = 0,
            endgameScore = 0;


        PieceList[] pieceLists = theBoard.GetAllPieceLists();
        for (int i = 0; i < 12; i++)
        {
            bool isWhitePieceList = pieceLists[i].IsWhitePieceList;
            for (int j = 0; j < pieceLists[i].Count; j++)
            {
                Square square = pieceLists[i].GetPiece(j).Square;
                int file = square.File,
                    rank = square.Rank,
                    index = isWhitePieceList ? file + (8 * (7 - rank)) : file + (8 * rank);

                middlegameScore += (middlegamePieceValue[i % 6] + unpackedPST[index + 64 * (i % 6)]) * (isWhitePieceList ? 1 : -1);
                endgameScore += (endgamePieceValue[i % 6] + unpackedPST[48 + index + 64 * (i % 6)]) * (isWhitePieceList ? 1 : -1);
                gamePhase += piecePhaseValue[i % 6];
            }
        }

        // Tapered Eval
        int middlegamePhase = Math.Min(24, gamePhase); // in case of early promotion
        return ((middlegameScore * middlegamePhase + endgameScore * (24 - middlegamePhase)) / 24) * (theBoard.IsWhiteToMove ? 1 : -1);
    }

    /*
     * Credit goes to selenaut for TT code
     * Credit goes to jw1912 for incremental sort and qsearch code
     */
    public int Search(int alpha, int beta, int depth, bool root)
    {
        if (theBoard.IsDraw())
            return 0;
        if (theBoard.IsInCheckmate())
            return -999999999; // should this be alpha? does it make a difference?

        ref Transposition tp = ref tpt[theBoard.ZobristKey & 0x7FFFFF];

        if (!root && tp.zKey == theBoard.ZobristKey && tp.depth > depth)
            if (tp.flag == 3 || (tp.flag == 1 && tp.eval >= beta) || (tp.flag == 2 && tp.eval <= alpha))
                return tp.eval;

        bool qsearch = depth <= 0;
        if (qsearch)
        {
            int standPat = Evaluate();
            if (standPat >= beta)
                return standPat;
            alpha = Math.Max(alpha, standPat);
            if (depth < -9) // cutoff after 9 consecutive captures to reduce time
                return alpha;
        }

        int startingAlpha = alpha;

        Move[] moves = theBoard.GetLegalMoves(qsearch);
        if (moves.Length == 0)
            return alpha;

        int bestEval = -1999999999;
        Move bestMove = moves[0];

        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
            moveScores[i] = moves[i] == tp.move ? 999999999 : MoveScore(moves[i]);

        for (int i = 0; i < moves.Length; i++)
        {
            for (int j = i + 1; j < moves.Length; j++) // incremental selection sort
            {
                if (moveScores[j] > moveScores[i])
                {
                    (moveScores[i], moveScores[j]) = (moveScores[j], moveScores[i]);
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                }
            }

            theBoard.MakeMove(moves[i]);
            int score = -Search(-beta, -alpha, depth - 1, false);
            theBoard.UndoMove(moves[i]);

            if (score > bestEval)
            {
                bestEval = score;
                bestMove = moves[i];
                if (root)
                    rootBestMove = bestMove;
            }

            if (score >= beta)
                break;
            if (score > alpha)
                alpha = score;
        }

        if (!qsearch)
        {
            tp.eval = bestEval;
            tp.zKey = theBoard.ZobristKey;
            tp.move = bestMove;
            tp.depth = (sbyte)depth;
            tp.flag = (sbyte)(bestEval < startingAlpha ? 2 : bestEval >= beta ? 1 : 3);
        }

        return bestEval;
    }

    public Move Think(Board board, Timer timer)
    {
        theBoard = board;

        // max depth = 4 for low time, 5 for normal time, 8 for endgame
        for (int i = 1; i <= (timer.MillisecondsRemaining < 10000 ? 4 : BitboardHelper.GetNumberOfSetBits(theBoard.AllPiecesBitboard) < 7 ? 8 : 5); i++)
            Search(-999999999, 999999999, i, true);

        return rootBestMove == Move.NullMove ? theBoard.GetLegalMoves()[0] : rootBestMove;
    }
}