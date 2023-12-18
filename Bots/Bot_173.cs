namespace auto_Bot_173;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_173 : IChessBot
{
    int searchTime;
    Move bestmoveRoot;
    Board board;
    Timer timer;

    int[] moveScores = new int[256];

    const int ttMask = 0x1FFFFF;

    // key, move, depth, eval, flag
    // flags: 0 = invalid, 1 = lowerbound, 2 = upperbound, 3 = exact
    (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[ttMask + 1];


    int[,,] history;


    bool TimeUp(int eval = 0) { return ((timer.MillisecondsElapsedThisTurn > searchTime || Math.Abs(eval) > 50_000) && bestmoveRoot != default) ? true : false; }

    public Move Think(Board b, Timer t)
    {

        board = b;
        timer = t;

        history = new int[2, 7, 64];

        searchTime = Math.Min(timer.MillisecondsRemaining / 30, 1000);
        for (int depth = 2, alpha = -100_000, beta = 100_000, eval, windowSize = 0; depth < 100;) // windowSize = 0 is never used
        {
            eval = Search(alpha, beta, depth, 0, Move.NullMove, true);

            if (TimeUp(eval)) break;


            if (eval <= alpha || eval >= beta)
            {
                alpha = eval - windowSize;
                beta = eval + windowSize;
                windowSize += 128;
            }
            else
            {
                alpha = eval - 10;
                beta = eval + 6;
                windowSize = 16;
                depth++;
            }
        }
        return bestmoveRoot;
    }
    int Search(int alpha, int beta, int depth, int ply, Move prevMove, bool pvNode)
    {
        bool isRoot = ply == 0, inCheck = board.IsInCheck(), capturesOnly = depth <= 0;
        var ttEntry = tt[board.ZobristKey & ttMask];
        int bestEval = -1_000_000, ttEval = ttEntry.Item4, ttFlag = ttEntry.Item5, eval;
        ulong zobristkey = board.ZobristKey;

        int searchShortcut(int beta, Move prev, bool pv, int depthReduction = 1) => eval = -Search(-beta, -alpha, depth - depthReduction, ply + 1, prev, pv);

        // Check extension
        if (inCheck) depth++;


        if (isRoot) goto playMoves;


        if (board.IsRepeatedPosition()) return -ply;

        if (ttEntry.Item1 == zobristkey && ttEntry.Item3 >= depth && Math.Abs(ttEval) < 50_000 && (
           ttFlag == 3 || // Exact
           ttFlag == 1 && ttEval >= beta && !pvNode || // Lowerbound
           ttFlag == 2 && ttEval <= alpha && !pvNode // Upperbound
           )) return ttEval;

        if (capturesOnly)
        {
            bestEval = Evaluate();
            if (bestEval >= beta) return beta;
            alpha = Math.Max(alpha, bestEval);
        }
        else if (!pvNode && !inCheck)
        {
            // Reverse futility pruning
            int evaluation = Evaluate();
            if (depth <= 8 && evaluation - 64 * depth >= beta) return evaluation;

            // Null move pruning
            if (depth >= 3)
            {
                board.ForceSkipTurn();
                searchShortcut(beta, Move.NullMove, false, 3);
                board.UndoSkipTurn();

                if (eval >= beta) return eval;
            }
        }

    playMoves:

        Span<Move> moveSpan = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moveSpan, capturesOnly);

        if (!capturesOnly && moveSpan.Length == 0) return inCheck ? -99_999 + ply : -ply;

        Move bestMove = default;
        int movesScored = 0, movesSearched = 0, origAlpha = alpha;

        // negative values because .Sort sorts ascending
        foreach (Move move in moveSpan)
            moveScores[movesScored++] = -(
            // tt move
            move == ttEntry.Item2 ? 10_000_000 :
            // Most Valuable Victim Least Valuable Attacker
            move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType : //  999_995 <= value <= 4_999_999
            // history heuristic
            history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]
            );

        moveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

        foreach (Move move in moveSpan)
        {
            if (TimeUp()) return 100_000;
            board.MakeMove(move);

            // Principal variation 
            if (movesSearched++ == 0) searchShortcut(beta, move, true);
            else
            {
                // No principal variation

                // No Late Move Reductions
                if ((movesSearched < 4 || depth < 3) && searchShortcut(alpha + 1, move, false) > alpha) searchShortcut(beta, move, false);
                // Late Move Reductions
                else if (searchShortcut(alpha + 1, move, false, 3) > alpha) searchShortcut(beta, move, false);// Research at full depth
            }

            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                if (bestEval > alpha)
                {
                    alpha = bestEval;
                    bestMove = move;
                    if (isRoot) bestmoveRoot = move;
                }

                if (alpha >= beta) // fail high
                {
                    if (!move.IsCapture)
                        // Update history heuristic
                        history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += 1 << depth;
                    break;
                }
            }
        }
        tt[zobristkey & ttMask] = (zobristkey, bestMove, depth, bestEval, bestEval >= beta ? 1 : bestEval > origAlpha ? 3 : 2);
        return bestEval;
    }

    private readonly short[] gamephase = { 0, 1, 1, 2, 4, 0 }, PieceValues = { 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0 };
    int Evaluate()
    {
        int mg = 0, eg = 0, phase = 0;

        for (int color = 2; --color >= 0; mg = -mg, eg = -eg)// color == 1 for white, 0 for black
        {
            for (int piece = -1; ++piece < 6;)
            {
                for (ulong bitboard = board.GetPieceBitboard((PieceType)piece + 1, color > 0); bitboard != 0;)
                {
                    phase += gamephase[piece];
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ 56 * color;
                    mg += pieceSquareTables[index][piece];
                    eg += pieceSquareTables[index][piece + 6];
                }
            }
        }
        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1) + phase / 2; // Tempo bonus decreases in endgame

    }
    private readonly int[][] pieceSquareTables;

    public Bot_173()
    {
        // Credit to Tyrant on Discord

        pieceSquareTables = new[] {
             63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
             77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
             2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
             77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
             75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
             75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
             73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
             68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
         }.Select(packedTable =>
         new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                     // Using searchTime since it's an integer than initializes to zero and is assigned before being used again 
                     .Select(square => (int)((sbyte)square * 1.461) + PieceValues[searchTime++ % 12])
                 .ToArray()
        ).ToArray();

    }
}