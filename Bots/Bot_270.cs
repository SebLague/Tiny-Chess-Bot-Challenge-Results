namespace auto_Bot_270;
using ChessChallenge.API;
using System;
using System.Linq;
using static System.Math;

public class Bot_270 : IChessBot
{
    private int _timeLimit;
    private int[]
        _historyHeuristics,
        _pieceValues = {
            82, 337, 365, 477, 1025, 0, // Midgame
            94, 281, 297, 512, 936, 0, // Endgame
        };

    private Move _rootBestMove;
    private readonly Move[] _counterMoves = new Move[65535],
                            _killerMoves = new Move[2000];

    // Flag Values: LOWERBOUND = 0, EXACT = 1, UPPERBOUND = 2
    private record struct Transposition(Move BestMove, ulong Key, int Eval, int Flag, int Depth);
    private readonly Transposition[] _transpositionTable = new Transposition[0x300000];
    private readonly int[][] _pstsUnpacked;

    public Bot_270()
    {
        // PSTs packed based on Tyrants packing algo
        _pstsUnpacked = new decimal[] {
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
                .Select(square => (int)((sbyte)square * 1.461) + _pieceValues[_timeLimit++ % 12])
                .ToArray()
        ).ToArray();
    }

    public Move Think(Board board, Timer timer)
    {
        _historyHeuristics = new int[65536];

        // Soft Bounded Time-Control (~50 ELO as opposed to hard bounds)
        _timeLimit = timer.MillisecondsRemaining / 13;

        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        int Search(int alpha, int beta, int depth, int depthFromRoot, bool nullMoveEnabled = false, Move previousMove = default)
        {
            bool isNotRoot = depthFromRoot++ > 0,
                 isNotPV = beta - alpha <= 1,
                 isInCheck = board.IsInCheck();

            if (isNotRoot && board.IsRepeatedPosition()) return 0;

            ulong key = board.ZobristKey;
            Transposition transposition = _transpositionTable[key & 0x2FFFFF];
            int alphaOriginal = alpha,
                bestEval = -9999999,
                eval,
                numMovesEvaluated = 0,
                transpositionEval = transposition.Eval,
                transpositionFlag = transposition.Flag;

            // Evaluate existing Transposition Table entry
            if (isNotRoot && transposition.Key == key && transposition.Depth >= depth && (
                transpositionFlag == 1 ||
                transpositionFlag == 0 && transpositionEval >= beta ||
                transpositionFlag == 2 && transpositionEval <= alpha
            )) return transpositionEval;

            // Check Extension
            if (isInCheck) depth++;

            // Internal Iterative Deepening
            else if (depth >= 4 && transposition.BestMove.IsNull) depth -= 2;

            // Quiescence Search
            bool isLeaf = depth <= 0;
            if (isLeaf)
            {
                bestEval = Evaluate();
                alpha = Max(alpha, bestEval);
                if (alpha >= beta) return bestEval;
            }
            else if (isNotPV && !isInCheck)
            {
                // Reverse Futility Pruning
                eval = Evaluate();
                if (eval - 80 * depth >= beta) return eval;

                // Null Move Heuristic
                if (nullMoveEnabled)
                {
                    board.ForceSkipTurn();
                    eval = -Search(-beta, 1 - beta, depth - 4, depthFromRoot);

                    // Mate Threat Extension
                    if (Search(-beta, -500_000, depth - 4, depthFromRoot) > 500_000) depth++;
                    board.UndoSkipTurn();

                    if (eval >= beta) return beta;
                }
            }

            // Inline function for token savings
            int SearchDeeper(Move previousMove, int newAlpha, int depthReduction = 1) =>
                -Search(newAlpha, -alpha, depth - depthReduction, depthFromRoot, nullMoveEnabled, previousMove);

            // Move Ordering
            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, isLeaf);

            // Check for Checkmate and Draws
            if (!isLeaf && moves.IsEmpty) return isInCheck ? depthFromRoot - 1_000_000 : 0;

            // Move Ordering
            moves
                .ToArray()
                .Select(aMove => -(
                     // Iterative Deepening for Move Ordering
                     aMove == transposition.BestMove ? 9_000_000 :

                     // MVV-LVA
                     aMove.IsCapture ? 1_000_000 * (int)aMove.CapturePieceType - (int)aMove.MovePieceType :

                     // Killer Moves
                     aMove == _killerMoves[depthFromRoot] ? 900_000 :

                     // Counter Moves
                     aMove == _counterMoves[previousMove.RawValue] ? 200_000 :

                     // History Heuristics
                     _historyHeuristics[aMove.RawValue]))
                .ToArray()
                .AsSpan()
                .Sort(moves);

            Move bestMove = default;
            foreach (Move aMove in moves)
            {
                if (isNotPV)
                {
                    // History Leaf Pruning
                    if (depth <= 0 && numMovesEvaluated >= 5 && _historyHeuristics[aMove.RawValue] <= 250) continue;

                    // Basic initial  Late Move Reduction
                    if (depthFromRoot >= 4 && numMovesEvaluated > 10 * depthFromRoot) break;
                }


                // Principal Variation Search w/ further Late Move Reduction
                board.MakeMove(aMove);
                if (numMovesEvaluated++ == 0 || isLeaf) eval = SearchDeeper(aMove, -beta);
                else
                {
                    eval = SearchDeeper(
                        aMove,
                        -alpha - 1,
                        depthFromRoot >= 3 && isNotPV && !aMove.IsCapture ?
                        (numMovesEvaluated + depthFromRoot) / 6 : 1
                    );
                    if (eval > alpha) eval = SearchDeeper(aMove, -beta);
                }

                board.UndoMove(aMove);
                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = aMove;
                    if (!isNotRoot) _rootBestMove = aMove;

                    alpha = Max(bestEval, alpha);
                    if (alpha >= beta)
                    {
                        if (!aMove.IsCapture)
                        {
                            _counterMoves[aMove.RawValue] = aMove;
                            _historyHeuristics[aMove.RawValue] += depth * depth;
                            _killerMoves[depthFromRoot] = aMove;
                        }

                        break;
                    }
                }

                if (timer.MillisecondsElapsedThisTurn > _timeLimit)
                    return 999_999;
            }

            // Store in Transposition Table
            _transpositionTable[key & 0x2FFFFF] = new(
                bestMove,
                key,
                bestEval,
                bestEval <= alphaOriginal ? 2 : bestEval >= beta ? 0 : 1,
                depth
            );

            return bestEval;
        }

        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        int Evaluate()
        {
            int midgameEval = 0,
                endgameEval = 0,
                phase = 0,
                sideToMove = 2;

            for (; --sideToMove >= 0; midgameEval = -midgameEval, endgameEval = -endgameEval)
                for (int pieceIdx = -1; ++pieceIdx < 6;)
                    for (ulong bitboard = board.GetPieceBitboard((PieceType)(pieceIdx + 1), sideToMove > 0); bitboard != 0;)
                    {
                        phase += pieceIdx < 5 ? pieceIdx : 0;
                        Square square = new(BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ 56 * sideToMove);
                        midgameEval += _pstsUnpacked[square.Index][pieceIdx];
                        endgameEval += _pstsUnpacked[square.Index][pieceIdx + 6];

                        // Bonus for Bishop Pairs
                        if (pieceIdx == 2 && bitboard != 0)
                        {
                            midgameEval += 20;
                            endgameEval += 40;
                        }
                    }

            return (midgameEval * phase + endgameEval * (32 - phase)) / (board.IsWhiteToMove ? 32 : -32);
        }

        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // ----------------------------------------------------------------------------------
        // Iterative Deepening with Aspiration Windows
        for (int depth = 2, alpha = -9999999, beta = 9999999; ;)
        {
            int eval = Search(alpha, beta, depth, 0, true);
            if (timer.MillisecondsElapsedThisTurn > _timeLimit / 3) return _rootBestMove;

            if (eval <= alpha) alpha -= 60;
            else if (eval >= beta) beta += 60;
            else
            {
                alpha = eval - 20;
                beta = eval + 20;
                depth++;
            }
        }
    }
}
