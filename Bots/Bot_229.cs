namespace auto_Bot_229;
using ChessChallenge.API;
using System;
using System.Linq;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class Bot_229 : IChessBot
{

    // Pawn, Knight, Bishop, Rook, Queen, King 
    private readonly int[] _pieceValues =
    {
        82, 337, 365, 477, 1025, 10000, // Middlegame
        94, 281, 297, 512, 936, 10000 // Endgame
    },
        _moveScores = new int[128];


    // unpacked pesto table
    private readonly int[][] _unpackedPestoTables;

    // match types for transposition table
    // private const sbyte Exact = 0, LowerBound = -1, UpperBound = 1, Invalid = -2;

    private Move _bestMoveRoot = Move.NullMove;

    //14 bytes per entry, likely will align to 16 bytes due to padding (if it aligns to 32, recalculate max TP table size)
    private readonly (ulong, int, int, int, Move)[] _transpositionTable = new (ulong, int, int, int, Move)[1_048_576UL];

    public Bot_229()
    {
        // Big table packed with data from premade piece square tables
        // Unpack using PackedEvaluationTables[set, rank] = file
        _unpackedPestoTables = new[]
        {
            63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m,
            75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m,
            936945638387574698250991104m, 75531285965747665584902616832m,
            77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m,
            3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m,
            4977175895537975520060507415m, 2475894077091727551177487608m,
            2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m,
            3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m,
            9301461106541282841985626641m, 2793818196182115168911564530m,
            77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m,
            5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m,
            5619082524459738931006868492m, 649197923531967450704711664m,
            75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m,
            4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m,
            1890741055734852330174483975m, 76772801025035254361275759599m,
            75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m,
            4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m,
            1557077491473974933188251927m, 77376040767919248347203368440m,
            73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m,
            3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m,
            78580145051212187267589731866m, 75798434925965430405537592305m,
            68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m,
            77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m,
            74568981255592060493492515584m, 70529879645288096380279255040m,
        }.Select(packedTable =>
        {
            var pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select(square => (int)((sbyte)square * 1.461) + _pieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }

    public Move Think(Board board, Timer timer)
    {
        int maxThinkTime = timer.MillisecondsRemaining / 25,
            depth = 1;

        _bestMoveRoot = Move.NullMove;

        // Killer moves: keep track on great moves that caused a cutoff to retry them
        // Based on a lookup by depth, we keep the best 2 moves
        var killerMoves = new Move[1000];

        // side, move from, move to
        var moveHistory = new int[2, 65536];

        // https://www.chessprogramming.org/Iterative_Deepening
        for (; ; )
        {
            Search(depth, 0, -9999999, 9999999, true);

            // check if we're out of time
            if (timer.MillisecondsElapsedThisTurn >= maxThinkTime) break;
            depth++;
        }

        return _bestMoveRoot.IsNull ? board.GetLegalMoves().First() : _bestMoveRoot;

        int Search(int depth, int ply, int alpha, int beta, bool allowNullMove)
        {
            // declare these all at once to save tokens
            bool quiesceSearch = depth <= 0,
                notRoot = ply > 0,
                isInCheck = board.IsInCheck(),
                notPrincipleVariation = beta - alpha == 1,
                canPrune = false;

            // check for draws
            if (notRoot && board.IsRepeatedPosition()) return 0;

            var boardZobrist = board.ZobristKey;
            var (zobristHash, score, ttDepth, flag, ttMove) = _transpositionTable[boardZobrist % 1_048_576UL];

            // check extensions
            if (isInCheck) depth++;

            // transposition table lookup
            if (zobristHash == boardZobrist && notRoot &&
                ttDepth >= depth)
            {
                // 0 = exact, -1 = lower bound, 1 = upper bound
                if (flag == -1)
                    alpha = Math.Max(alpha, score);
                else if (flag == 1)
                    beta = Math.Min(beta, score);

                if (alpha >= beta || flag == 0)
                    return score;
            }

            int bestScore = -9999999,
                moveScoreIndex = 0;

            // search function alias for the next iteration, trying to save some tokens
            int NextSearch(int newAlpha, int newBeta, int depthReduction = 1, bool allowNull = true) =>
                -Search(depth - depthReduction, ply + 1, newAlpha, newBeta, allowNull);

            if (quiesceSearch)
            {
                bestScore = Evaluate();
                if (bestScore >= beta) return bestScore;
                alpha = Math.Max(alpha, bestScore);
            }
            // no pruning in q-search
            // null move pruning only when allowed and we're not in check
            else if (!isInCheck && notPrincipleVariation)
            {
                // reverse futility pruning
                var staticEval = Evaluate();
                if (staticEval - 82 * depth >= beta) return staticEval;

                if (allowNullMove)
                {
                    board.TrySkipTurn();
                    // depth reduction factor used for null move pruning, commented out for tokens
                    // private const int DepthReductionFactor = 3;
                    var nullMoveScore = NextSearch(-beta, -beta + 1,
                        4, false);
                    board.UndoSkipTurn();

                    // beta cutoff
                    if (nullMoveScore >= beta)
                        return beta;
                }

                // check for futility pruning conditions, use depth * pawn value
                canPrune = depth <= 4 && staticEval + 82 * depth <= alpha;

            }

            // TODO: Can we make this buffer smaller?
            Span<Move> moves = stackalloc Move[128];
            // use non-alloc version for the speeeeeeddddddd
            board.GetLegalMovesNonAlloc(ref moves, quiesceSearch && !isInCheck);
            // create buffer based on span size, note that GetLegalMovesNonAlloc changes the length of the span
            // based on the number of moves available.
            // assign scores
            foreach (var move in moves)
                // negate all scores so we don't have to reverse the move list later
                _moveScores[moveScoreIndex++] = -(
                    ttMove == move ? 9_000_000 :
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    move.IsPromotion ? 10_000 :
                    killerMoves[ply] == move ? 900_000 :
                    moveHistory[ply & 1, move.RawValue & 4095]
                );


            // sort moves using the negative scores as keys
            _moveScores.AsSpan(0, moves.Length).Sort(moves);

            // check for terminal position            
            if (!quiesceSearch && moves.IsEmpty) return isInCheck ? -100_000 + ply : 0;

            var bestMove = Move.NullMove;
            int startingAlpha = alpha,
                movesSearched = 0;

            foreach (var move in moves)
            {
                // futility pruning
                var isTacticalMove = move.IsCapture || move.IsPromotion || isInCheck;
                if (!isTacticalMove && notPrincipleVariation && canPrune && movesSearched > 0) continue;

                board.MakeMove(move);

                // first child searches with normal window, otherwise do a null window search
                // combines null window and LMR conditions
                // should be equivalent to:
                // if (movesSearched++ == 0 || quiesceSearch)
                //     eval = NextSearch(-beta, -alpha);

                // else if (movesSearched >= (notPrincipleVariation ? 3 : 7) && depth >= 3 &&
                //          !isTacticalMove)
                // {
                //     // null window search
                //     eval = NextSearch(-(alpha + 1), -alpha, 2);
                // }
                // else eval = alpha + 1;
                var eval = movesSearched++ == 0 || quiesceSearch ? NextSearch(-beta, -alpha) :
                    movesSearched >= (notPrincipleVariation ? 3 : 7) && depth >= 3 && !isTacticalMove
                        ? NextSearch(-alpha - 1, -alpha, 2) : alpha + 1;

                // check result to see if we need to do a full re-search
                // if we fail high, we re-search
                if (eval > alpha &&
                    alpha < (eval = NextSearch(-alpha - 1, -alpha)) && eval < beta)
                    eval = NextSearch(-beta, -eval);

                board.UndoMove(move);

                if (eval > bestScore)
                {
                    bestScore = eval;
                    bestMove = move;
                    // update move at root
                    if (!notRoot) _bestMoveRoot = move;

                    // update alpha and check for beta cutoff
                    alpha = Math.Max(alpha, eval);
                    if (alpha >= beta)
                    {
                        if (!quiesceSearch && !bestMove.IsCapture)
                        {
                            // add it to history
                            moveHistory[ply & 1, move.RawValue & 4095] += depth * depth;
                            // add to killer moves table
                            killerMoves[ply] = bestMove;
                        }
                        break;
                    }
                }

                // check if time expired
                if (timer.MillisecondsElapsedThisTurn >= maxThinkTime)
                    return 100_000;
            }

            // after finding the best move, store it in the transposition table
            // note we use the original alpha
            _transpositionTable[boardZobrist % 1_048_576UL] =
            (
                boardZobrist,
                bestScore,
                depth,
                // 0 = exact, -1 = lower bound, 1 = upper bound
                bestScore >= beta ? -1 : bestScore > startingAlpha ? 0 : 1,
                bestMove
            );

            return bestScore;
        }

        int Evaluate()
        {
            int middleGame = 0, endGame = 0, phase = 0, sideToMove = 2, piece, square;

            // Loop through each side (white and black)
            // Always from white's perspective (flip sign if necessary)
            for (; --sideToMove >= 0; middleGame = -middleGame, endGame = -endGame)
                for (piece = -1; ++piece < 6;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        // PeSTO evaluation
                        // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
                        // values for pieces: 0 for pawn, 1 for knight, 2 for bishop, 3 for rook, 4 for queen
                        phase += 0x00042110 >> piece * 4 & 0x0F;

                        // A number between 0 to 63 that indicates which square the piece is on, flip for black
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        middleGame += _unpackedPestoTables[square][piece];
                        endGame += _unpackedPestoTables[square][piece + 6];
                    }

            return (middleGame * phase + endGame * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
        }
    }
}