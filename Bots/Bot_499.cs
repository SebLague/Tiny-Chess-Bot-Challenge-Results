namespace auto_Bot_499;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_499 : IChessBot
{
    Move bestmoveRoot;


    // Pawn, Knight, Bishop, Rook, Queen, King 
    int[] piecePhase = { 0, 1, 1, 2, 4, 0 },
                          pieceValues = { 82, 337, 365, 477, 1025, 0,   // Middlegame
                                          94, 281, 297, 512, 936,  0 }; // Endgame

    // Big table packed with data from premade piece square tables
    // Unpack using PackedEvaluationTables[set, rank] = file
    decimal[] PackedPestoTables = {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 3110608541636285947269332480m, 936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 3730792265775293618620982364m, 3121489077029470166123295018m, 3747712412930601838683035969m, 3763381335243474116535455791m, 8067176012614548496052660822m, 4977175895537975520060507415m, 2475894077091727551177487608m,
        2458978764687427073924784380m, 3718684080556872886692423941m, 4959037324412353051075877138m, 3135972447545098299460234261m, 4371494653131335197311645996m, 9624249097030609585804826662m, 9301461106541282841985626641m, 2793818196182115168911564530m,
        77683174186957799541255830262m, 4660418590176711545920359433m, 4971145620211324499469864196m, 5608211711321183125202150414m, 5617883191736004891949734160m, 7150801075091790966455611144m, 5619082524459738931006868492m, 649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 4348529951871323093202439165m, 4990460191572192980035045640m, 5597312470813537077508379404m, 4980755617409140165251173636m, 1890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 2489164206166677455700101373m, 4338830174078735659125311481m, 4960199192571758553533648130m, 3420013420025511569771334658m, 1557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 1212557245150259869494540530m, 3081561358716686153294085872m, 3392217589357453836837847030m, 1219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m,
    };
    int[][] UnpackedPestoTables;

    // Transposition table
    (ulong, int, sbyte, sbyte, ushort)[] TT = new (ulong, int, sbyte, sbyte, ushort)[0x800000]; // (key, score, depth, flag, move)

    // Killer moves
    Move[,] killerMoves = new Move[256, 2];

    public Bot_499()
    {
        UnpackedPestoTables = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return new System.Numerics.BigInteger(packedTable).ToByteArray().Take(12)
                    .Select(square => (int)((sbyte)square * 1.461) + pieceValues[pieceType++])
                .ToArray();
        }).ToArray();
    }

    public Move Think(Board board, Timer timer)
    {

        (int depth, bestmoveRoot, var historyHeuristic) = (0, board.GetLegalMoves()[0], new int[2, 7, 64]);

        int Evaluate()
        {
            int mg = 0, eg = 0, phase = 0, square;

            for (int stm = 2; --stm >= 0; mg = -mg, eg = -eg)
                for (int piece = 6; piece > 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece--, stm > 0); mask > 0;)
                    {
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ stm * 56;

                        phase += piecePhase[piece];
                        mg += UnpackedPestoTables[square][piece];
                        eg += UnpackedPestoTables[square][piece + 6];
                    }

            return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
        }

        int Search(int alpha, int beta, int depth, int ply, bool nullMoveAvailable)
        {

            // Check for repetition (this is much more important than material and 50 move rule draws)
            if (++ply > 1 && (board.IsRepeatedPosition() || board.IsFiftyMoveDraw() || board.IsInsufficientMaterial()))
                return 0;

            ulong boardZKey = board.ZobristKey;

            Move bestMove = bestmoveRoot, move, movej;

            // TT cutoffs
            ref var ttEntry = ref TT[boardZKey & 0x7FFFFF];
            int best = -30000, origAlpha = alpha, score, ttFlag = ttEntry.Item4;
            bool isEntryValid = ttEntry.Item1 == boardZKey;
            if (ply > 1 && isEntryValid && ttEntry.Item3 >= depth
                && ttFlag * ttEntry.Item2 >= ttFlag * (ttFlag == 1 ? beta : alpha))
                return ttEntry.Item2;

            bool qsearch = depth <= 0,
                ignorePositionalMoves = false,
                parentInCheck = board.IsInCheck();

            // Quiescence search is in the same function as negamax to save tokens
            if (qsearch)
            {
                best = Evaluate();
                if (best >= beta)
                    return best;
                alpha = Math.Max(alpha, best);
            }

            else if (depth <= 8 && !parentInCheck)
            {
                // Reverse Futility Pruning
                score = Evaluate() - 120 * depth;

                // Extended futility pruning
                ignorePositionalMoves = score + 40 + 240 * depth <= alpha;

                // Null move pruning
                if (depth >= 3 && nullMoveAvailable)
                {
                    board.TrySkipTurn();
                    score = -Search(-beta, 1 - beta, depth - 3, ply, false);
                    board.UndoSkipTurn();
                }

                if (score >= beta)
                    return score;
            }

            // Generate moves, only captures in qsearch
            var moves = board.GetLegalMoves(qsearch);
            int numLegalMoves = moves.Length;

            var scores = new int[numLegalMoves];

            void doSearchIf(bool cond, int beta, int R = 1)
            {
                if (score > alpha && cond)
                    score = -Search(-beta, -alpha, depth - R, ply, nullMoveAvailable);
            }

            ref int histScore(Move move) => ref historyHeuristic[ply % 2, (int)move.MovePieceType, move.TargetSquare.Index];

            // Search moves
            for (int i = 0; i < numLegalMoves; i++)
            {

                if (30 * timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining)
                    return 123456789;

                // Incrementally sort moves
                move = moves[i];
                for (int j = i; j < numLegalMoves; j++)
                {
                    movej = moves[j];

                    // Initialize the scores here, for token efficiency (this uses only one for loop)
                    if (i == 0)
                        scores[j] = movej.RawValue == ttEntry.Item5 && isEntryValid ? 1000000 :
                                    movej.IsCapture ? 100 * (int)movej.CapturePieceType - (int)movej.MovePieceType :
                                    movej.IsPromotion ? 100 * (int)movej.PromotionPieceType :
                                    killerMoves[ply, 0] == movej || killerMoves[ply, 1] == movej ? 150 :
                                    histScore(movej) - 10000000;

                    if (scores[j] > scores[i])
                        (scores[i], scores[j], move, moves[j]) = (scores[j], scores[i], movej, move);
                }

                bool isTacticalMove = move.IsCapture || move.IsPromotion;

                // Extended futility pruning
                if (ignorePositionalMoves && !isTacticalMove && i > 0)
                    continue;


                board.MakeMove(move);

                score = alpha + 1;
                doSearchIf(i >= 4 && depth >= 3 && !(isTacticalMove || parentInCheck || board.IsInCheck()), score, 2);
                doSearchIf(i > 0, alpha + 1);
                doSearchIf(i == 0 || score < beta, beta);

                board.UndoMove(move);

                // New best move
                if (score > best)
                {
                    best = score;
                    bestMove = move;
                    if (ply == 1) bestmoveRoot = move;

                    // Improve alpha
                    alpha = Math.Max(alpha, score);

                    // Fail-high
                    if (alpha >= beta)
                    {
                        if (scores[i] < 0)
                            (killerMoves[ply, 0], killerMoves[ply, 1]) = (move, killerMoves[ply, 0]);

                        if (!isTacticalMove)
                            histScore(move) += depth * depth * depth;

                        break;
                    }

                }
            }

            // (Check/Stale)mate
            if (!qsearch && numLegalMoves == 0) return parentInCheck ? ply - 30000 : 0;

            // Write in transposition table  TODO: set depth to +inf for checkmates?
            ttEntry = (boardZKey, best, (sbyte)depth, (sbyte)(best >= beta ? 1 : best > origAlpha ? 0 : -1), bestMove.RawValue);

            return best;
        }

        while (++depth < 64)
        {
            Search(-30000, 30000, depth, 0, true);

            // Out of time
            if (60 * timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining)
                break;
        }

        return bestmoveRoot;
    }
}