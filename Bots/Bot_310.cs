namespace auto_Bot_310;
using ChessChallenge.API;
using System;
using System.Linq;



public class Bot_310 : IChessBot
{


    Move bestmoveRoot = Move.NullMove;

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

    private readonly int[][] UnpackedPestoTables = new int[64][];

    public Bot_310()
    {
        // Precompute PSTs
        UnpackedPestoTables = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select(square => (int)((sbyte)square * 1.461) + pvm[pieceType++]))
                .ToArray();
        }).ToArray();
    }



    record struct TTEntry(ulong key, Move move, int depth, int score, int bound);

    TTEntry[] tt = new TTEntry[0x400000];

    int[,,] history = new int[2, 7, 64];


    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {
        if (board.IsInCheck()) depth++;

        bool QSearch = depth <= 0,
              notRoot = ply > 0;

        if (notRoot && board.IsRepeatedPosition()) return 0;

        int best = -200_000,
            mg = 0,
            eg = 0,
            phase = 0,
            stm = 2,
            piece,
            square;


        ulong key = board.ZobristKey;

        TTEntry entry = tt[key % 0x3FFFFF];

        if (notRoot &&
            entry.key == key &&
            entry.depth >= depth &&
            (entry.bound == 3                         // exact score
            || entry.bound == 2 && entry.score >= beta  // lower bound, fail high
            || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
            )
           ) return entry.score;




        if (QSearch)
        {

            for (; --stm >= 0; mg = -mg, eg = -eg)
                for (piece = -1; ++piece < 6;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, stm > 0); mask != 0;)
                    {
                        // Gamephase, middlegame -> endgame
                        phase += phase_weight[piece];

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * stm;
                        mg += UnpackedPestoTables[square][piece];
                        eg += UnpackedPestoTables[square][piece + 6];
                    }
            best = (mg * phase + eg * (24 - phase)) / 24
                 * (board.IsWhiteToMove ? 1 : -1);




            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);

        } //qsearch



        Move[] moves = board.GetLegalMoves(QSearch).OrderByDescending(
            move =>
                move == entry.move ? 10_000_000 :
                move.IsCapture ? 100_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                move.IsPromotion ? 10_000
                                   : history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]
        ).ToArray();

        Move bestMove = Move.NullMove;

        int origAlpha = alpha;
        bool pvFound = false; // Flag to indicate if PV was found in the current search

        // Search moves
        int i = 0;
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int score = 0;
            bool betaSearch = true;

            if (!(move.IsCapture || move.IsPromotion)
                && depth > 1
                && !pvFound
                && i > 0)
            {                                                              //LMR
                score = -Search(board, timer, -alpha - 1, -alpha, depth - 1 - ((i >= 3) ? 1 : 2), ply + 1);  // Do a reduced-depth search

                betaSearch = alpha < score && score < beta; // If the score falls inside the aspiration window, do a full-depth search
            }
            if (betaSearch)
                score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);  // Full-depth search for the first move, if it's a capture move or if PV was already found

            board.UndoMove(move);


            if (score > best)
            {
                best = score;
                bestMove = move;
                if (ply == 0) bestmoveRoot = move;

                alpha = Math.Max(alpha, score);

                if (alpha >= beta)
                {
                    if (!QSearch && !move.IsCapture) history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    break;
                }

                // If we found a new best score (improved alpha), we have found a PV
                pvFound = score > origAlpha;
            }

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 321321321;

            i++;
        }

        if (!QSearch && moves.Length == 0)
            return board.IsInCheck() ? ply - 1_000_000  // checkmate
                                     : 0;             // stalemate

        tt[key % 0x3FFFFF] = new TTEntry(key, bestMove, depth, best, best >= beta ? 2 : best > origAlpha ? 3 : 1);

        return best;
    }







    public Move Think(Board board, Timer timer)
    {
        bestmoveRoot = board.GetLegalMoves()[0];
        if (timer.MillisecondsRemaining < 300) return bestmoveRoot;


        // very basic tiny opening book 
        // adds some variety instead of the bot always starting Nf3
        // can save a few seconds on the timer

        ulong opening = board.AllPiecesBitboard
        switch
        {
            //_hgfedcba_hgfedcba_hgfedcba_hgfedcba_hgfedcba_hgfedcba_hgfedcba_hgfedcba
            //_88888888_77777777_66666666_55555555_44444444_33333333_22222222_11111111                              Who.   Position            - Options 
            0b_11111111_11111111_00000000_00000000_00000000_00000000_11111111_11111111 => 0x1C0C_1C0C_1B0B_1B0B, // white.                     - d4, e4
            0b_11111111_11111111_00000000_00000000_00001000_00000000_11110111_11111111 => 0x2D3E_2333_2D3E_2333, // black. d4                  - d5, Nf3
            0b_11111111_11110111_00000000_00001000_00001000_00000000_11110111_11111111 => 0x1A0A_1A0A_1A0A_1A0A, // white. d4 d5               - c4
            0b_11111111_11110111_00000000_00001000_00001100_00000000_11110011_11111111 => 0x0000_2C34_1A23_2A32, // black. d4 d5, c4           - c6, dxc4, e6
            0b_11111111_11100111_00010000_00001000_00001100_00000000_11110011_11111111 => 0x1201_1201_1201_1201, // white. d4 d5, c4 e6        - Nc3
            0b_10111111_11111111_00100000_00000000_00001000_00000000_11110111_11111111 => 0x1A0A_1A0A_1A0A_1A0A, // white. d4 Nf6              - c4
            0b_10111111_11111111_00100000_00000000_00001100_00000000_11110011_11111111 => 0x2E36_2E36_2C34_2232, // black. d4 Nf6, c4          - c5, e6, g6
            0b_10111111_10111111_01100000_00000000_00001100_00000000_11110011_11111111 => 0x1201_160E_1201_160E, // white. d4 Nf6, c4 g6       - g3, Nc3
            0b_10111111_10111111_01100000_00000000_00001100_00000100_11110011_11111101 => 0x363D_231B_363D_231B, // black. d4 Nf6, c4 g6, Nc3  - d5 Bg7
            0b_10111111_11101111_00110000_00000000_00001100_00000000_11110011_11111111 => 0x1506_1201_1201_160E, // white. d4 Nf6, c4 e6       - g3, Nc3, Nf3
            0b_11111111_11111111_00000000_00000000_00010000_00000000_11101111_11111111 => 0x0000_2C34_2434_2232, // black. e4                  - c5, e5, e6
            0b_11111111_11101111_00000000_00010000_00010000_00000000_11101111_11111111 => 0x1506_1D0D_1506_1D0D, // white. e4 e5               - f4, Nf3
            0b_11111111_11101111_00000000_00010000_00010000_00100000_11101111_10111111 => 0x2D3E_2A39_2D3E_2A39, // black. e4 e5, Nf3          - Nc6, Nf6
            0b_11111101_11101111_00000100_00010000_00010000_00100000_11101111_10111111 => 0x1201_1B0B_1A05_2105, // white. e4 e5, Nf3 Nc6      - Bb5, Bc4, d4, Nc3
            0b_11111111_11111011_00000000_00000100_00010000_00000000_11101111_11111111 => 0x1506_1C0C_1506_1C0C, // white. e4 c5               - d4, Nf3
            0b_11111111_11111111_00000000_00010000_00010000_00000000_11101111_11111111 => 0x1B0B_1B0B_1B0B_1B0B, // white. e4 e6               - d4
            0b_11111111_11111111_00000000_00010000_00011000_00000000_11100111_11111111 => 0x2333_2333_2333_2333, // black. e4 e6, d4           - d5
            0b_11111111_11110111_00000000_00011000_00011000_00000000_11100111_11111111 => 0x1201_1201_1201_1201, // white. e4 e6, d4 d5        - Nc3

            _ => 0
        };
        opening >>= 16 * new Random().Next(4);
        if (opening > 0)
            return new Move(new Square((int)opening & 0xff).Name + new Square((int)opening >> 8 & 0xff).Name, board);


        int depth = 1,
            alpha = -100_000,
            beta = 100_000,
            eval;

        for (;
             depth < 99 &&
             (eval = Search(board, timer, alpha, beta, depth++, 0)) != 321321321;

             alpha -= eval <= alpha ? 100 : eval < beta ? 20 : 0,
             beta += eval >= beta ? 100 : eval > alpha ? 20 : 0
            ) ;

        return bestmoveRoot;
    }








}
