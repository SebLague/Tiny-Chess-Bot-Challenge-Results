namespace auto_Bot_119;
// #define LOG_BEST
// #define CHECK_TIMER_OFTEN

using ChessChallenge.API;
using System;
using System.Linq;


public class Bot_119 : IChessBot
{
    struct TTEntry
    {
        public ulong hash = 0;
        public Move bestMove = Move.NullMove;
        public short score = 0;
        public sbyte depth = 0, boundType = 0;
        public TTEntry() { }
    }

    const sbyte EXACT = 1, LOWERBOUND = 2, UPPERBOUND = 3;

    const short INF = 10000;
    const short KING_VAL = 9000;

    const int TTSizeBitCount = 22;
    // const int TTshift = 64 - TTSizeBitCount;

    const ulong TTsize = (1UL << TTSizeBitCount);
    const ulong TTmask = TTsize - 1;

    // const ulong TTHashPrime = 345850088332294811UL;
    // const ulong TTHashPrime = 1;

    const int timeUseFactor = 60;

#if LOG_BEST
    ulong nodeCount;
#endif


    TTEntry[] transposition;

    Move bestMoveStart;
    Timer timer;

    // PeSTO Evaluation Function
    readonly int[] phase_weight = { 0, 1, 1, 2, 4, 0 };
    // thanks for the compressed pst implementation Tyrant
    // None, Pawn, Knight, Bishop, Rook, Queen, King 
    private readonly short[] pvm = { 82, 337, 365, 477, 1025, 20000, // Middlegame
                                     94, 281, 297, 512, 936, 20000}; // Endgame
                                                                     // Big table packed with data from premade piece square tables
                                                                     // Unpack using PackedEvaluationTables[set, rank] = file
    private readonly decimal[] PackedPestoTables = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };

    private readonly int[][] UnpackedPestoTables;

    public Bot_119()
    {
        this.transposition = new TTEntry[TTsize];

        // Precompute PSTs (stolen)
        UnpackedPestoTables = new int[64][];
        UnpackedPestoTables = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + pvm[pieceType++]))
                .ToArray();
        }).ToArray();
    }

    public Move Think(Board board, Timer _timer)
    {
        timer = _timer;

        bestMoveStart = board.GetLegalMoves()[0];

        for (sbyte i = 1;
            i < 127;
            i++)
        {
#if LOG_BEST
            nodeCount = 0;
#endif

            int res = search(board, i);

#if LOG_BEST
            DivertedConsole.Write("Depth: " + i + " \tScore: " + res + " \t" + bestMoveStart + " Nodes searched: " + nodeCount);
#endif

            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / timeUseFactor)
                break;

        }

#if LOG_BEST
        DivertedConsole.Write("Final Move decision:\n\t" + bestMoveStart);
#endif

        return bestMoveStart;
    }

    public int search(Board board, sbyte depth, sbyte ply = 0, int alpha = -INF, int beta = INF)
    {

#if LOG_BEST
        nodeCount++;
#endif
#if CHECK_TIMER_OFTEN
        if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / timeUseFactor)
            return INF;
#endif

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return -KING_VAL + ply;

        bool check = board.IsInCheck();
        if (check)
            depth++;

        bool qsearch = depth <= 0,
             start = ply == 0;



        int alphaOrig = alpha;
        ulong key = board.ZobristKey;

        Move bestMove = Move.NullMove;

        TTEntry TTvalue = this.transposition[hash(key)];

        if (TTvalue.hash == key)
        {
            if (TTvalue.depth >= depth)
            {
                switch (TTvalue.boundType)
                {
                    case EXACT:
                        return TTvalue.score;
                    case LOWERBOUND:
                        alpha = System.Math.Max(alpha, TTvalue.score);
                        break;
                    case UPPERBOUND:
                        beta = System.Math.Min(beta, TTvalue.score);
                        break;
                }
                if (alpha >= beta)
                {
                    return TTvalue.score;
                }
            }
            bestMove = TTvalue.bestMove;
        }

        int evaluation = eval(board);

        int bestScore = -INF;


        if (qsearch)
        {
            bestScore = evaluation;
            if (bestScore >= beta)
                return bestScore;

            alpha = Math.Max(alpha, bestScore);
        }


        Move[] moves = board.GetLegalMoves(qsearch && !check);
        int[] moveLastScore = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            Move m = moves[i];
            if (m == bestMove)
            {
                moveLastScore[i] = -INF;
                continue;
            }

            if (m.IsCapture)
            {
                moveLastScore[i] = (int)m.MovePieceType - 100 * (int)m.CapturePieceType;
            }
            // this is too expensive and slows the algorithm down
            // while not providing additional benefit
            // else {
            //     board.MakeMove(m);
            //     TTEntry move_val = this.transposition[hash(board.ZobristKey)];
            //     if (move_val.hash == board.ZobristKey) {
            //         moveLastScore[i] = move_val.score;
            //     } else {
            //         // moveLastScore[i] = eval(board);
            //     }
            //     board.UndoMove(m);
            // }
        }

        System.Array.Sort(moveLastScore, moves);


        foreach (var m in moves)
        {
            board.MakeMove(m);
            int score = -this.search(board, (sbyte)(depth - 1), (sbyte)(ply + 1), -beta, -alpha);
            board.UndoMove(m);

#if CHECK_TIMER_OFTEN
            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / timeUseFactor) {
                return INF;
            }
#endif

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = m;
                if (ply == 0)
                    bestMoveStart = m;

                alpha = System.Math.Max(alpha, bestScore);
            }

            if (alpha >= beta)
                break;
        }

        TTvalue.score = (short)bestScore;
        TTvalue.depth = depth;
        TTvalue.hash = key;
        TTvalue.bestMove = bestMove;
        if (bestScore >= beta)
        {
            TTvalue.boundType = LOWERBOUND;
        }
        else if (bestScore <= alphaOrig)
        {
            TTvalue.boundType = UPPERBOUND;
        }
        else
        {
            TTvalue.boundType = EXACT;
        }

        this.transposition[hash(key)] = TTvalue;

        return bestScore;
    }

    ulong hash(ulong key)
    {
        // return (key * TTHashPrime) >> TTshift;
        return key & TTmask;
    }


    // stolen evaluation function
    public int eval(Board board)
    {
        // Define evaluation variables
        int mg = 0, eg = 0, phase = 0;
        // Iterate through both players
        foreach (bool stm in new[] { true, false })
        {
            // Iterate through all piece types
            for (int piece = -1; ++piece < 6;)
            {
                // Get piece bitboard
                ulong bb = board.GetPieceBitboard((PieceType)piece + 1, stm);
                // Iterate through each individual piece
                while (bb != 0)
                {
                    // Get square index for pst based on color
                    int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ (stm ? 56 : 0);
                    // Increment mg and eg score
                    mg += UnpackedPestoTables[sq][piece];
                    eg += UnpackedPestoTables[sq][piece + 6];
                    // Updating position phase
                    phase += phase_weight[piece];
                }
            }
            // Flip sign of eval before switching sides
            mg = -mg;
            eg = -eg;
        }
        // Tapered evaluation
        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }
}
