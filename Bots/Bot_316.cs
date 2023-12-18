namespace auto_Bot_316;
using ChessChallenge.API;
using System;
using System.Linq;
using static ChessChallenge.API.BitboardHelper;

public class Bot_316 : IChessBot
{
    // Evaluate constants
    static int[] pieceValues = { 100, 300, 350, 500, 900, 0, /*<-middlegame|endgame->*/ 150, 275, 325, 525, 999, 0 };

    // Compressed Piece-Square tables used for evaluation, ComPresSTO
    int[][] psts = new[] {
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
                    // Using positions evaled since it's an integer than initializes to zero and is assgined before being used again 
                    .Select(square => (int)((sbyte)square * 1.461) + pieceValues[posEvaled++ % 12]).ToArray()).ToArray();

    // variables outside of Methods, to safe Tokens
    Board board;
    Timer timer;

    Random random = new();

    // best Move for this turn and the best turn for the current depth
    Move bestMove, bestIterativeMove;
    // the eval of the best Move, the eval of the best Move for the current depth
    // and the maximum Search time
    int bestEval, bestIterativeEval, maxSearchTime;
    // the number of chess positions evaled this turn
    static int posEvaled;

    // the count of the entries that can be stored in the transposition table
    //const int ttEntries = 4194304 == 1 << 22;
    // the transposition table with the following parameters:
    // zorbist-key, best Move, evaluation, depth, flags
    (ulong, Move, int, sbyte, byte)[] tt = new (ulong, Move, int, sbyte, byte)[4194304];

    // killermoves
    Move[] killermoves = new Move[1024];

    // history Heuristics
    int[,,] hh = new int[2, 64, 64];

    public Move Think(Board b, Timer t)
    {
        // update the variables
        board = b;
        timer = t;

        // calculate the maximum search time
        maxSearchTime = GetMaxSearchTime();

        // reset the best moves and the evaluation of them
        bestMove = bestIterativeMove = board.GetLegalMoves()[random.Next(board.GetLegalMoves().Length)];
        bestEval = bestIterativeEval = posEvaled = 0;

        // if there is enough time for the move calculation
        if (timer.MillisecondsRemaining > 1000)
            // Iterative deepening
            for (sbyte depth = 0; depth < 100; depth++)
            {
                // search for the best move with the current depth
                NegaMax(depth, 0, -99999, 99999);

                // if too much time has elapsed of there is a mate
                if (timer.MillisecondsElapsedThisTurn >= maxSearchTime || bestEval > 99899)
                    break;

                bestMove = bestIterativeMove;
                bestEval = bestIterativeEval;
            }

        hh = new int[2, 64, 64];

        return bestMove;
    }

    int NegaMax(sbyte depth, int ply, int alpha, int beta)
    {
        posEvaled++;

        bool quienceSearch = depth <= 0;

        if (ply > 0)
        {
            if (board.IsDraw())
                return 0;

            alpha = Math.Max(alpha, ply - 99999);
            beta = Math.Max(beta, -ply - 99999);

            if (alpha >= beta)
                return alpha;
        }

        ulong zorbist = board.ZobristKey;
        ref var ttEntry = ref tt[zorbist % 4194304];
        Move entryMove = ttEntry.Item2;
        byte entryFlags = ttEntry.Item5;
        int entryEval = ttEntry.Item3;

        if (ply > 0 && ttEntry.Item1 == zorbist && ttEntry.Item4 >= depth && (entryFlags == 3 || entryFlags == 2 && entryEval >= beta || entryFlags == 1 && entryEval <= alpha))
            return entryEval;

        int eval;

        if (quienceSearch)
        {
            eval = Evaluate();
            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);
        }

        var moves = board.GetLegalMoves(quienceSearch).OrderByDescending(move =>
            move == entryMove ? 10000000 : move.IsCapture ? 2000000 * (move.CapturePieceType - move.MovePieceType) : killermoves[depth] == move ? 1000000 : hh[board.IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index]
        ).ToArray();

        int bestPosEval = -99999;
        int originAlpha = alpha;
        Move bestPosMove = new();

        if (moves.Length == 0 && !quienceSearch)
            return board.IsInCheck() ? bestPosEval/*== -99999*/ + ply : 0;

        foreach (var move in moves)
        {
            if (timer.MillisecondsElapsedThisTurn > maxSearchTime)
                return 99999;

            board.MakeMove(move);

            eval = -NegaMax((sbyte)(depth - (board.IsInCheck() ? 0 : 1)), ply + 1, -beta, -alpha);

            board.UndoMove(move);

            if (eval >= beta)
            {
                ttEntry = new(zorbist, move, eval, depth, 2);

                if (!move.IsCapture)
                {
                    // add to killer moves & history heuristics
                    killermoves[depth] = move;
                    hh[board.IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index] += depth * depth;
                }

                return beta;
            }

            if (eval > bestPosEval)
            {
                bestPosMove = move;
                bestPosEval = eval;

                alpha = Math.Max(alpha, eval);

                if (ply == 0)
                {
                    bestIterativeMove = move;
                    bestIterativeEval = eval;
                }
            }

        }

        byte bound = (byte)(bestPosEval >= beta ? 2 : bestPosEval > originAlpha ? 3 : 1);

        ttEntry = new(zorbist, bestPosMove, bestPosEval, depth, bound);

        return alpha;
    }

    int GetMaxSearchTime() => Math.Min(GetNumberOfSetBits(board.AllPiecesBitboard) * 25, timer.MillisecondsRemaining / 30);

    int Evaluate()
    {
        int score = 0;
        // ComPresSTO, credit to Tyrant (tyrant0565 on tiny chess bot programming discord server)
        int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
        for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
            for (piece = -1; ++piece < 6;)
                for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                {
                    // Gamephase, middlegame -> endgame
                    // Multiply, then shift, then mask out 4 bits for value (0-16)
                    gamephase += 0x00042110 >> piece * 4 & 0x0F;

                    // Material and square evaluation
                    square = ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                    middlegame += psts[square][piece];
                    endgame += psts[square][piece + 6];
                }
        // Tempo bonus
        int pstEval = (middlegame * gamephase + endgame * (24 - gamephase)) / 24 * IsWhiteToMove() + gamephase / 2;

        // Removed due to illegal namespace -- seb
        //score += ProtectedPawnsCount(true) - ProtectedPawnsCount(false);

        if (GetNumberOfSetBits(board.AllPiecesBitboard) > 18) // if it isn't the endgame
            score += KingSafetyOpening(true) - KingSafetyOpening(false);

        return score * IsWhiteToMove() + pstEval;
    }

    int KingSafetyOpening(bool white) => 100 * Math.Abs(board.GetKingSquare(white).Rank - (white ? 7 : 0));

    //int ProtectedPawnsCount(bool white) => 25 * GetNumberOfSetBits(ProtectedPawns(board.GetPieceBitboard(PieceType.Pawn, white), white));

    int IsWhiteToMove() => board.IsWhiteToMove ? 1 : -1;
}
