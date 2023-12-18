namespace auto_Bot_323;
using ChessChallenge.API;

public class Bot_323 : IChessBot
{
    int[] pieceValue = { 0, 100, 283, 323, 481, 935, 350 };

    // PeSTO
    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] mgValue = { 0, 82, 337, 365, 477, 1025, 200 };
    int[] egValue = { 0, 94, 281, 297, 512, 936, 400 };
    int[] gamephaseInc = { 0, 0, 1, 1, 2, 4, 0 };

    // Compressed MG PST [0, 63]
    // Compressed EG PST [64, 127]
    ulong[] psts = {
        503810006608864768, 602919995508694528, 595070571170462208, 560149002808933888, 514038796572489216, 538792043848564224, 579323364657055232, 591710475524658688, 609648990533115490, 575855506066202246, 554500819113288253, 569144119272182367, 567999547040095812, 572583397088098942, 534270870351781410, 544432576074857973, 566876854351775226, 604027179056558599, 579283776907548186, 559018688830309919, 554538158540804673, 583841273013208632, 601845793084220953, 552317143940116972, 557854241545772530, 554476555793352205, 563495869018033670, 546607390849029653, 543246181641786903, 548895484196492812, 561259446248426001, 536492933938436585, 521845222849105381, 575869737437237758, 546615046589399547, 533103160020448780, 525230667511394833, 527480251098877446, 539872860612089354, 519600034074321383, 561246212373063142, 561263826049687036, 552242342807482876, 525230639574166006, 527479162326765059, 543249454396360195, 560151141760394785, 546630417200431604, 578111622309449181, 584896738640735743, 568029126465147372, 504968847871702505, 528619351582440945, 559026358548187672, 587154046768892454, 586032475081782762, 560134633999982080, 617536844025277952, 590525156886518272, 516236670928387584, 586014976296599040, 545471574871347712, 604011725684454912, 592731847725131264, 493697777782954496, 537641955781732864, 556782262798503424, 556787757138547200, 564669053266265600, 593933654749058560, 581538855865746944, 557905949709727232, 563494773773016754, 596186555153375917, 592822049583898270, 596209642741693062, 596228319416409747, 619835940008337028, 602952944341983909, 589409154731815611, 588261268904190558, 596171155543929444, 602929853528287829, 593966632590124611, 599593932026281528, 627728228050001461, 626584733803329106, 591670843726585428, 568020356122000928, 601818242987068952, 604072252564593165, 607473029141256709, 606360324452932094, 614222931025210884, 606360321220157969, 580441536838744593, 556738266232502797, 572551444620110345, 600689049918259709, 604097531675698681, 607457629533913593, 602957327370831352, 587200224072045059, 564664630521608703, 555614557826426372, 573616866012427783, 589425638839418362, 600674746600275457, 602929838509533696, 595057329875449339, 584916538416935423, 566896633750989304, 546600759352318477, 564614058346000904, 581494866821241352, 591643361299394058, 592769249400322573, 581502553736851968, 571355158770329090, 557848766512058873, 517315263915855360, 538712871529821696, 553356168447632896, 564592073495791104, 545493552222153216, 561226455506729472, 549980668842949120, 528565455086354944
    };

    // https://www.chessprogramming.org/Shared_Hash_Table
    struct TTValue
    {
        public ulong key;
        public Move move;
        public int eval, depth;
        public TTValue(ulong _key, Move _move, int _eval, int _depth)
        {
            key = _key;
            move = _move;
            eval = _eval;
            depth = _depth;
        }
    };

    const int TABLESIZE = (1 << 10);
    TTValue[] tt = new TTValue[TABLESIZE];

    Move bestMoveRoot;

    public Move Think(Board board, Timer timer)
    {
        bestMoveRoot = Move.NullMove;
        for (int depth = 3; depth <= 100 && timer.MillisecondsElapsedThisTurn <= timer.MillisecondsRemaining / 35; depth++)
        {
            _ = Search(board, timer, depth, -500000, 500000, 0);
        }
        return bestMoveRoot.IsNull ? board.GetLegalMoves()[0] : bestMoveRoot;
    }

    // https://github.com/JacquesRW/Chess-Challenge
    // https://www.chessprogramming.org/Quiescence_Search
    // https://www.chessprogramming.org/Alpha-Beta
    // https://stackoverflow.com/questions/9964496/alpha-beta-move-ordering
    int Search(Board board, Timer timer, int depth, int alpha, int beta, int ply)
    {
        ulong key = board.ZobristKey;
        int bestScore = -500000;
        bool quiesceSearch = depth <= 0;
        Move bestMove = Move.NullMove;

        if (board.IsRepeatedPosition() || board.IsDraw())
            return 0;

        TTValue value = tt[key % TABLESIZE];
        if (value.key == key && value.depth >= depth)
            return value.eval;

        int eval = Evaluate(board);
        if (quiesceSearch)
        {
            bestScore = eval;
            if (bestScore >= beta)
                return bestScore;
            if (bestScore > alpha)
                alpha = bestScore;
        }

        Move[] moves = board.GetLegalMoves(quiesceSearch);
        int[] scores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            if (move == value.move) // if move is on Transposition Table forces to search first
                scores[i] = 6000000;
            else if (move.IsCapture) // gives a value based on MVV_LVA
                scores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 35)
                return 500000;

            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (ply == 0)
                    bestMoveRoot = move;
                if (score > alpha)
                    alpha = score;

                if (alpha >= beta)
                    break;
            }
        }

        tt[key % TABLESIZE] = new TTValue(key, bestMove, bestScore, depth);
        return bestScore;
    }

    int GetPST(int square, int piece)
    {
        return (short)(((psts[square] >> (piece * 10)) & 1023) - 512);
    }

    // https://github.com/JacquesRW/Chess-Challenge
    int Evaluate(Board board)
    {
        int mg = 0, eg = 0, gamephase = 0, pc, sq;
        foreach (bool white in new[] { true, false })
        {
            for (PieceType p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                pc = (int)p;
                foreach (Piece piece in board.GetPieceList(p, white))
                {
                    gamephase += gamephaseInc[pc];
                    sq = piece.Square.Index ^ (white ? 56 : 0);
                    mg += GetPST(sq, pc - 1) + mgValue[pc];
                    eg += GetPST(sq + 64, pc - 1) + egValue[pc];
                }
            }
            mg = -mg;
            eg = -eg;
        }
        return (mg * gamephase + eg * (24 - gamephase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }
}