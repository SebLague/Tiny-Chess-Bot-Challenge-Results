namespace auto_Bot_476;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_476 : IChessBot
{
    // Define globals to save tokens
    readonly int CHECKMATE = 20000;
    Board board;
    Timer timer;
    int[] timeDivisor = { 45, 25 }; // {OPening, MG/EG}
    int time_limit, nodes;
    Move best_move_root, lastBestMove;

    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;
        time_limit = timer.MillisecondsRemaining / timeDivisor[board.PlyCount > 20 ? 1 : 0]; // Checks to see if its move 10 to determine phase

        // Iterative Deepening Loop
        for (int depth = 1; ;)
        {
            lastBestMove = best_move_root;
            int score = Negamax(++depth, 0, -CHECKMATE, CHECKMATE);
            DivertedConsole.Write("Depth: " + depth + " | Nodes: " + nodes + " || Eval: " + score + " | Best move: " + best_move_root);
            // Check if time is expired
            if (timer.MillisecondsElapsedThisTurn > time_limit)
                break;

            // If a checkmate is found, exit search early to save time
            if (score > CHECKMATE / 2)
                break;
        }
        return best_move_root;
    }

    private int Negamax(int depth, int ply, int alpha, int beta)
    {
        // Increment node counter
        nodes++;
        // Define search variables
        bool root = ply == 0;
        bool q_search = depth <= 0;
        bool in_check = board.IsInCheck();
        int best = -CHECKMATE * 2;
        int turn = board.IsWhiteToMove ? 1 : 0;

        // Check for draw by repetition
        if (!root && board.IsRepeatedPosition()) return 0;
        // Second draw check
        if (board.IsDraw()) return 0;

        if (in_check) depth++;

        // Delta Pruning
        if (q_search)
        {
            best = Eval();
            if (best >= beta) return beta;
            alpha = Math.Max(alpha, best);
        }

        Move[] moves = board.GetLegalMoves(q_search && !in_check);
        // Move Ordering
        int[] move_scores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            // MVV-LVA
            move_scores[i] = move.IsCapture ? 1000 * (int)move.CapturePieceType - (int)move.MovePieceType : 0;
            // Punishes king moves
            if ((int)move.MovePieceType == 6) move_scores[i] -= 38;
            // Orders lastBestMove as first
            if (moves[i] == lastBestMove) move_scores[i] += 10000;
        }

        Move best_move = Move.NullMove;
        int start_alpha = alpha;
        for (int i = 0; i < moves.Length; i++)
        {
            // Check if time is expired
            if (timer.MillisecondsElapsedThisTurn > time_limit) return CHECKMATE;

            // Sort moves in one-iteration bubble sort
            for (int j = i; ++j < moves.Length;)
                if (move_scores[i] < move_scores[j])
                    (moves[i], moves[j], move_scores[i], move_scores[j]) =
                    (moves[j], moves[i], move_scores[j], move_scores[i]);

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Negamax(depth - 1, ply + 1, -beta, -alpha);
            board.UndoMove(move);

            if (Math.Abs(score) == CHECKMATE) return CHECKMATE;

            if (score > best)
            {
                best = score;
                best_move = move;
                // Update bestmove
                if (root) best_move_root = move;
                // Improve alpha
                alpha = Math.Max(alpha, best);
                // Fail-high
                if (alpha >= beta) break;
            }
        }

        // If there are no moves return either checkmate or draw
        if (!q_search && moves.Length == 0) return in_check ? -CHECKMATE + ply : 0;

        return best;
    }

    // PeSTO Evaluation Function
    readonly int[] phase_weight = { 0, 1, 1, 2, 4, 0 };
    // thanks for the compressed pst implementation Tyrant
    // None, Pawn, Knight, Bishop, Rook, Queen, King 
    private short[] pvm = { 82, 337, 365, 477, 1025, 20000, // Middlegame
                                     94, 281, 297, 512, 936, 20000}; // Endgame
                                                                     // Big table packed with data from premade piece square tables
                                                                     // Unpack using PackedEvaluationTables[set, rank] = file
    private decimal[] PackedPestoTables = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };

    private int[][] UnpackedPestoTables;

    // TODO: King Safety
    // TODO: Pawn Structure
    // TODO: Mobility
    private int Eval()
    {
        unpack();
        // Define evaluation variables
        int mg = 0, eg = 0, phase = 0;
        // Iterate through both players
        foreach (bool stm in new[] { true, false })
        {
            // Iterate through all piece types
            for (int piece = -1; ++piece < 6;)
            {
                // Get piece bitboard
                ulong bb = board.GetPieceBitboard((PieceType)(piece + 1), stm);

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
            mg = -mg;
            eg = -eg;
        }

        // In case of premature promotion
        phase = Math.Min(phase, 24);

        // King tropism
        double kingSafetyBonus = 0;
        for (int i = 1; i <= 63; i++)
        {
            Square kingSquare = board.GetKingSquare(board.IsWhiteToMove);
            Square pieceSquare = new Square(i);
            Piece piece = board.GetPiece(pieceSquare);
            if ((int)piece.PieceType != 0 && (int)piece.PieceType != 6)
            {
                int distance = Math.Abs(pieceSquare.Rank - kingSquare.Rank) + Math.Abs(pieceSquare.File - kingSquare.File);
                int factor = pvm[(int)piece.PieceType - 1] / (distance * 20); // needs to be tuned
                // +/- for the maximising/minimising player
                kingSafetyBonus += factor * ((piece.IsWhite == board.IsWhiteToMove) ? 1 : -1);
            }
        }
        // Tapered eval
        return ((mg * phase + eg * (24 - phase)) / 24 + Convert.ToInt32(Math.Round(kingSafetyBonus))) * (board.IsWhiteToMove ? 1 : -1);
    }

    public void unpack()
    {
        UnpackedPestoTables = new int[64][];
        for (int i = 0; i < 64; i++)
        {
            int pieceType = 0;
            UnpackedPestoTables[i] = decimal.GetBits(PackedPestoTables[i]).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + pvm[pieceType++]))
                .ToArray();
        }
    }
}