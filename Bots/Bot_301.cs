namespace auto_Bot_301;
using ChessChallenge.API;
using System;


// To fix:
// Goes for draw in winning positions sometimes
// killer moves
// value for move ordering (10)
// aspiration window

// To improve:
// Evaluation
// - king safety
// - pawn structure
// - mobility

// To do:
// Delta pruning (futility pruning)
// Lazy evaluation + enhanced evaluation

//https://www.chessprogramming.org/Selectivity


public class Bot_301 : IChessBot
{
    Move bestmoveRoot;

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    // https://www.chessprogramming.org/Transposition_Table
    public record TTEntry(ulong key, Move move, int depth, int score, int bound);

    const int entries = 1 << 20;
    TTEntry[] tt = new TTEntry[entries];

    // Killer moves
    //HashSet<Move>[] killerMoves = new HashSet<Move>[50];

    public int getPstVal(int psq) => (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    //public void SetPrivateField(object obj, string fieldName, object value) => obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(obj, value);
    public int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += getPstVal(ind) + pieceVal[piece];
                    eg += getPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    // https://www.chessprogramming.org/Negamax
    // https://www.chessprogramming.org/Quiescence_Search
    public int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply, int nullDepth = 0)
    {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0, notRoot = ply > 0;
        int best = -30000, origAlpha = alpha;

        // Check for repetition (this is much more important than material and 50 move rule draws)
        if (board.IsDraw())
            return 0;
        if (board.IsInCheckmate())
            return -30000 + board.PlyCount;

        TTEntry entry = tt[key % entries];

        // TT cutoffs
        if (notRoot && entry != null && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;


        // Quiescence search is in the same function as negamax to save tokens
        if (qsearch)
        {
            best = Evaluate(board);
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }


        // Null move pruning
        if (nullDepth > 0 && depth > 1 && !board.IsInCheck())
        {
            board.TrySkipTurn(); // Perform the null move
            int nullMoveScore = -Search(board, timer, -beta, 1 - beta, depth - 1 - nullDepth, ply + 1);
            board.UndoSkipTurn(); // Undo the null move

            if (nullMoveScore >= beta)
                return nullMoveScore; // Cut-off the search early
        }

        // Generate moves, only captures in qsearch
        Move[] moves = board.GetLegalMoves(qsearch);
        int moveCount = moves.Length;
        int[] scores = new int[moveCount];

        // Score moves
        for (int i = 0; i < moveCount; i++)
        {
            Move move = moves[i];

            // TT move
            if (entry != null && move == entry.move) scores[i] = 1000000;
            // Killer moves
            //else if(killerMoves[ply].Contains(move)) scores[i] = 10000;
            // https://www.chessprogramming.org/MVV-LVA
            else if (move.IsCapture)
            {
                scores[i] = (int)move.CapturePieceType - (int)move.MovePieceType;
                scores[i] += (scores[i] < 0 && board.SquareIsAttackedByOpponent(move.TargetSquare)) ? scores[i] : 10;
                // TODO : fix value (10)
            }
        }

        Move bestMove = Move.NullMove;

        // Search moves
        for (int i = 0; i < moveCount; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

            // Incrementally sort moves
            for (int j = i + 1; j < moveCount; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score, extension = board.IsInCheck() ? 1 : 0;

            if (i <= 2 || depth <= 2 || move.IsCapture)
                score = -Search(board, timer, -beta, -alpha, depth - 1 + extension, ply + 1, nullDepth);
            else
            {
                score = -Search(board, timer, -alpha - 1, -alpha, depth - 2, ply + 1, nullDepth);
                if (score > alpha && score < beta)
                    score = -Search(board, timer, -beta, -score, depth - 1 + extension, ply + 1, nullDepth);
            }

            //int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if (score > best)
            {
                best = score;
                bestMove = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta)
                {
                    //killerMoves[ply].Add(move);
                    break;
                }

            }
        }

        if (ply == 0 && bestMove != Move.NullMove) bestmoveRoot = bestMove;

        // Push to TT
        tt[key % entries] = new TTEntry(key, bestMove, depth, best, best >= beta ? 2 : best > origAlpha ? 3 : 1);

        return best;
    }

    public Move Think(Board board, Timer timer)
    {
        // Fix repetition issue - 33 tokens
        // Reflection namespace not allowed. typeof(Board).GetField("depth", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(board, 1);

        Move[] moves = board.GetLegalMoves();

        bestmoveRoot = moves[0];

        if (moves.Length == 1)
        {
            //Console.BackgroundColor = ConsoleColor.Green;
            //DivertedConsole.Write("Only one move available");
            return bestmoveRoot;
        }

        // Killer move hash set initialization
        /*for (int i = 0; i < killerMoves.Length; i++)
            killerMoves[i] = new HashSet<Move>();*/


        // Tests on position
        //for (int i = 0; i < 3; i++)
        //{
        //    DivertedConsole.Write("\n*** SEARCH n°" + i + " ***\n");
        //    Think("6k1/3Q4/8/5K2/8/8/8/8 w - - 0 1", 5000);
        //}

        // Test positions
        //Test(positions, 1000);

        // Aspiration window
        //int alpha = -30000, beta = 30000;

        //Console.BackgroundColor = ConsoleColor.Blue;

        // https://www.chessprogramming.org/Iterative_Deepening
        for (int depth = 1; depth <= 50; depth++)
        {
            int score = Search(board, timer, -30000, 30000, depth, 0, 3);
            //int score = Search(board, timer, alpha, beta, depth, 0, 3); // aspiration window
            /*if (score != 30000 && (score <= alpha || score >= beta)) // Eval outside window
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                DivertedConsole.Write("Out of window");
                Console.BackgroundColor = ConsoleColor.Blue;
                // We need to do a full search
                alpha = -30000;
                beta = 30000;
                score = Search(board, timer, alpha, beta, depth, 0, 3);
            }
            alpha = score - 50;
            beta = score + 50;*/

            //DivertedConsole.Write("Grogros - Depth: " + depth + " Score: " + score + " Bestmove: " + bestmoveRoot);

            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
            {
                //DivertedConsole.Write("\n***Grogros - Depth: " + depth + " Score: " + score + "***\n");
                break;
            }

        }

        //Console.BackgroundColor = ConsoleColor.Black;

        return bestmoveRoot;
    }

    // Function that thinks for a certain amount of time on a given FEN position
    //public Move Think(string fen, int timeToSpend)
    //{

    //    Board board = Board.CreateBoardFromFEN(fen);


    //    //rootMoveOrder = SortedMoves(board, false);
    //    //bestMove = rootMoveOrder[0];

    //    Console.BackgroundColor = ConsoleColor.Green;

    //    // Displays the evaluation
    //    DivertedConsole.Write(Evaluate(board));

    //    Timer timer_test = new Timer(timeToSpend * 30);

    //    for (int depth = 1; depth <= 50; depth++)
    //    {
    //        int score = Search(board, timer_test, -30000, 30000, depth, 0, 3);

    //        DivertedConsole.Write("EvilBot_v2 - Depth: " + depth + " Score: " + score + " Bestmove: " + bestmoveRoot);

    //        // Out of time
    //        if (timer_test.MillisecondsElapsedThisTurn >= timer_test.MillisecondsRemaining / 30)
    //        {
    //            DivertedConsole.Write("\n***EvilBot_v2 - Depth: " + depth + " Score: " + score + "***\n");
    //            break;
    //        }

    //    }

    //    return bestmoveRoot;
    //}

    // Function that takes a position, an expected move and a maximum time to spend, and returns if the move calculated is the right one
    //public (Move, int, int, int, int) TestThink(string fen, Move expectedMove, int timeToSpend)
    //{
    //    Board board = Board.CreateBoardFromFEN(fen);
    //    Timer timer_test = new Timer(timeToSpend * 30);

    //    int bestScore = 0, bestDepth = 0;
    //    for (int depth = 1; depth <= 50; depth++)
    //    {
    //        int score = Search(board, timer_test, -30000, 30000, depth, 0, 3);

    //        // Out of time
    //        if (timer_test.MillisecondsElapsedThisTurn >= timer_test.MillisecondsRemaining / 30)
    //            break;
    //        else
    //        {
    //            bestScore = score;
    //            bestDepth = depth;
    //        }

    //    }
    //    Move bestMove = bestmoveRoot;

    //    board.MakeMove(expectedMove);
    //    tt = new TTEntry[entries];
    //    timer_test = new Timer(timeToSpend * 30);
    //    int expectedMoveScore = 0, expectedMoveDepth = 0;
    //    for (int depth = 1; depth <= 50; depth++)
    //    {
    //        int score = Search(board, timer_test, -30000, 30000, depth, 0, 3);

    //        // Out of time
    //        if (timer_test.MillisecondsElapsedThisTurn >= timer_test.MillisecondsRemaining / 30)
    //            break;
    //        else
    //        {
    //            expectedMoveScore = -score;
    //            expectedMoveDepth = depth + 1;
    //        }

    //    }

    //    return (bestMove, bestScore, bestDepth, expectedMoveScore, expectedMoveDepth);
    //}

    //// Function that tests a list of positions/moves couples
    //public void Test(List<(string, string)> positions, int timeToSpend)
    //{
    //    int nbSuccess = 0;
    //    int nbFailure = 0;

    //    foreach ((string, string) position in positions)
    //    {
    //        string fen = position.Item1;
    //        Move move = new Move(position.Item2, Board.CreateBoardFromFEN(fen));
    //        var (bestFoundMove, bestScore, bestDepth, expectedMoveScore, expectedMoveDepth) = TestThink(fen, move, timeToSpend);
    //        if (bestFoundMove == move)
    //        {
    //            Console.BackgroundColor = ConsoleColor.Green;
    //            DivertedConsole.Write("Success: " + fen);
    //            nbSuccess++;
    //        }
    //        else
    //        {
    //            Console.BackgroundColor = ConsoleColor.Red;
    //            DivertedConsole.Write("Failure: " + fen);
    //            nbFailure++;
    //        }
    //        DivertedConsole.Write("Expected move:  \t" + move + "\tScore: " + expectedMoveScore + "\tDepth: " + expectedMoveDepth);
    //        DivertedConsole.Write("Best found move:\t" + bestFoundMove + "\tScore: " + bestScore + "\tDepth: " + bestDepth);
    //        DivertedConsole.Write();
    //    }

    //    Console.BackgroundColor = ConsoleColor.Black;
    //    DivertedConsole.Write("Score: " + nbSuccess + "/" + (nbSuccess + nbFailure));
    //}


    //// List of positions/moves couples to test
    //public static List<(string, string)> positions = new List<(string, string)>()
    //{
    //    ("r3k2r/ppp2pp1/2p1bq2/2b4p/3PP1n1/2P2BP1/PP3P1P/RNBQK2R w KQkq - 3 11", "h2h3"), // +400
    //    ("3rk3/ppp2pp1/2p2q1r/2P1n3/4P2p/1QP3Pb/PP1NBP1P/R1B1R1K1 b - - 5 16", "d8d2"), // +500
    //    ("3rk2r/ppp2pp1/2p1bq2/2P4p/4P1B1/2P3P1/PP3P1P/RNBQK2R b KQk - 0 12", "e6g4"), // +200
    //    ("1rkb4/pNp5/8/2N4p/8/5B2/4K3/8 w - - 0 1", "f3c6"), // +150
    //    ("2k1r1r1/p1p4p/1qpb4/3PN3/2Q3b1/4B3/PP3PP1/2R2RK1 b - - 0 1", "e8e5"), // -200
    //    ("2kr2nr/1pp2ppp/3b4/1P3q2/2Pp1B2/5Q1P/RP3PP1/R5K1 w - - 0 1", "f3c6"), // #6
    //    ("4r1k1/5pP1/2q2Q1p/1p1RP3/p7/5RP1/5P1K/7r w - - 0 1", "h2h1"), // +1000
    //    ("6bk/7p/2q1PP2/2Pp4/3R4/5K2/8/B7 w - - 0 1", "f6f7"), // +50
    //    ("8/6Q1/2r1p3/2rkr3/2rrr2Q/8/7K/8 w - - 0 1", "h4d8"), // #21
    //    ("5rk1/pp4pp/4p3/2R3Q1/3n4/2q4r/P1P2PPP/5RK1 b - - 1 2", "c3g3"), // +500
    //    ("2b1q1r1/r3np1k/4pQpP/p2pP1B1/1ppP4/2P5/PPB2PP1/2KR3R w - - 0 1", "f6g7"), // #7
    //    ("r1b1k2r/pp1nqpp1/4p2p/3pP1N1/8/3BQ3/PP3PPP/2R2RK1 w kq - 0 1", "e3a7"), // +300
    //    ("8/k7/3p4/p2P1p2/P2P1P2/8/8/K7 w - - 0 1", "a1b1"), // +1000
    //    ("6k1/3Q4/8/4K3/8/8/8/8 w - - 0 1", "e5f6"), // #2
    //    ("4Q3/8/2K5/8/8/1k6/8/8 w - - 0 1", "e8e2"), // #5
    //    ("8/6k1/4K3/8/8/6P1/8/8 w - - 0 1", "e6f5"), // +1000
    //    ("1rkr4/6p1/b6p/1p1n4/3b4/8/PR2BPPP/Q5K1 w - - 0 1", "b2c2"), // +400
    //    ("1rkr4/2n3p1/b6p/1p6/3b4/8/P1R1BPPP/Q5K1 w - - 2 2", "e2g4"), // +500

    //};

}