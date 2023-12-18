namespace auto_Bot_530;
//////////////////////////////////////////////////////////////////////
//                           ALGERNON                               //
// Chess-playing bot, created for Sebastian Lague's Chess Challenge //
// https://www.youtube.com/watch?v=iScy18pVR58                      //
//////////////////////////////////////////////////////////////////////

/*
 Special thanks goes to:

 https://github.com/jw1912
 for providing Tier 2 opponent bot, used as a basic framework for Algernon

 https://github.com/Tyrant7
 for providing evaluation function and PST compression algorithm, used in Algernon

 https://github.com/GediminasMasaitis
 for creating a UCI implementation of the challenge framework and a guide to setting it up for automated tournaments in CuteChess,
 used to test Algernon 
*/

using ChessChallenge.API;
using System;
using System.Linq;
public class Bot_530 : IChessBot
{
    private readonly (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[0x400000];
    private readonly int[] moveScores = new int[218];

    private int[,,] historyHeuristics;
    private readonly Move[] killers = new Move[2048];

    private readonly int[] PieceValues = { 82, 337, 365, 477, 1025, 0,
                                           94, 281, 297, 512, 936, 0 };

    private readonly int[][] UnpackedPestoTables;

    int intMaxTime;

    public Bot_530()
    {
        UnpackedPestoTables = new[] {
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
                    .Select(square => (int)((sbyte)square * 1.461) + PieceValues[intMaxTime++ % 12])
                .ToArray()
        ).ToArray();
    }

    public Move Think(Board board, Timer timer)
    {

        Move bestRootMove = board.GetLegalMoves()[0];

        historyHeuristics = new int[2, 7, 64];

        int intPlyDepth = 2;

        intMaxTime = timer.MillisecondsRemaining;

        bool boolNullOK = true;

        while (intPlyDepth < 90)
        {

            int rootValue = NegaMax(intPlyDepth, 0, -99_999_999, 99_999_999);

            if (rootValue > 999_000) break;

            if (timer.MillisecondsElapsedThisTurn * 30 >= intMaxTime) break;

            intPlyDepth += 1;

        }
        return bestRootMove;


        int NegaMax(int intDepth, int ply, int alpha, int beta)
        {

            bool notRoot = ply > 0;

            if (notRoot && board.IsRepeatedPosition()) return 0;

            bool inCheck = board.IsInCheck();
            if (inCheck) intDepth++;

            int intStandPat = Evaluate();

            bool qsearch = intDepth <= 0;

            ulong zobristKey = board.ZobristKey;
            ref var entry = ref transpositionTable[zobristKey & 0x3FFFFF];

            int bestValue = -99_999_999,
                intStartingAlpha = alpha,
                entryScore = entry.Item3,
                entryFlag = entry.Item5,
                movesScored = 0;

            if (entry.Item1 == zobristKey && notRoot && entry.Item4 >= intDepth && (
                    // Exact
                    entryFlag == 1 ||
                    // Upperbound
                    entryFlag == 2 && entryScore <= alpha ||
                    // Lowerbound
                    entryFlag == 3 && entryScore >= beta))
                return entryScore;


            bool canFPrune = false;

            if (qsearch)
            {
                bestValue = intStandPat;
                if (bestValue >= beta) return bestValue;
                alpha = Math.Max(alpha, bestValue);
            }
            else if (!inCheck)
            {
                if (intStandPat - 110 * intDepth >= beta) return intStandPat;

                if (boolNullOK && intDepth > 2)
                {
                    boolNullOK = false;

                    board.TrySkipTurn();
                    int intNMPEval = -NegaMax(intDepth - 4 - intDepth / 6, ply + 1, -beta, 1 - beta);
                    board.UndoSkipTurn();

                    if (intNMPEval >= beta) intDepth -= 4;
                }

                canFPrune = intDepth < 10 && intStandPat + intDepth * 85 <= alpha;
            }

            boolNullOK = true;

            Span<Move> s_moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref s_moves, qsearch);

            foreach (Move move in s_moves)
                moveScores[movesScored++] = -(
                move == entry.Item2 ? 9_000_000 :
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                killers[ply] == move ? 900_000 :
                historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            moveScores.AsSpan(0, s_moves.Length).Sort(s_moves);

            if (!qsearch && s_moves.IsEmpty) return inCheck ? ply - 99999 : 0;

            Move bestMove = Move.NullMove;

            int intI = -1;
            foreach (Move s_move in s_moves)
            {
                intI++;

                if (intDepth > 2 && timer.MillisecondsElapsedThisTurn * 30 >= intMaxTime) return 99_999_999;

                if (canFPrune && (intI > 0 && !s_move.IsCapture && !s_move.IsPromotion && !(killers[ply] == s_move))) continue;

                board.MakeMove(s_move);

                int Reduction = intI < 5 || intDepth < 3 || s_move.IsCapture ? 1 : 2;
            lSearch:
                int Value = -NegaMax(intDepth - Reduction, ply + 1, -beta, -alpha);

                if (Value > alpha && Reduction-- > 1) goto lSearch;

                board.UndoMove(s_move);

                if (Value > bestValue)
                {

                    bestValue = Value;

                    alpha = Math.Max(alpha, bestValue);

                    bestMove = s_move;

                    if (!notRoot) bestRootMove = s_move;

                    if (alpha >= beta)
                    {
                        if (!s_move.IsCapture)
                        {
                            historyHeuristics[ply & 1, (int)s_move.MovePieceType, s_move.TargetSquare.Index] += intDepth * intDepth;
                            killers[ply] = s_move;
                        }
                        break;
                    }

                }

            }

            entry = new(
            zobristKey,
            bestMove,
            bestValue,
            intDepth,
            bestValue >= beta ? 3 : bestValue <= intStartingAlpha ? 2 : 1);

            return bestValue;
        }

        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)

                for (piece = -1; ++piece < 6;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {

                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        middlegame += UnpackedPestoTables[square][piece];
                        endgame += UnpackedPestoTables[square][piece + 6];

                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 32;
                            endgame += 32;
                        }

                    }

            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24) + gamephase / 2;

        }

    }
}