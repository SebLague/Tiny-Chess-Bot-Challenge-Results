namespace auto_Bot_304;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_304 : IChessBot
{
    #region Packed PSQ Table
    // Thanks https://github.com/Tyrant7/Easy-PST-Packer/tree/main and http://www.talkchess.com/forum3/viewtopic.php?f=2&t=68311&start=19
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
    #endregion

    #region variables

    (ulong/*hashCode*/, int/*flag*/, int/*depth*/, int/*score*/, Move/*move*/)[] TT = new (ulong, int, int, int, Move)[0x4FFFFF];
    Board board;
    Timer timer;
    Move BestMove;
    Move[] killers = new Move[1024];

    // Thanks http://www.talkchess.com/forum3/viewtopic.php?f=2&t=68311&start=19
    //                       P    N    B    R     Q   K
    short[] PieceValues = {  82, 281, 365, 477, 1024, 0, // Middlegame
                             95, 220, 297, 512, 1146, 0 }; // Endgame
    int searchTime;
    int[,,] hhScore;
    int[][] PSQTable;
    bool AbortSearch => timer.MillisecondsElapsedThisTurn > searchTime;

    #endregion

    #region Constructor

    // Thanks Again, Tyrant https://github.com/Tyrant7/Easy-PST-Packer/tree/main
    public Bot_304()
    {
        PSQTable = PackedPestoTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select(square => (int)((sbyte)square * 1.461) + PieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }
    #endregion

    #region Think
    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;

        hhScore = new int[2, 7, 64];

        searchTime = timer.MillisecondsRemaining / 30;
        int depth = 2, score, alpha = -999999, beta = 999999;

        BestMove = board.GetLegalMoves()[0];
        while (true)
        {
            if (AbortSearch || depth > 32)
                break;
            score = PVS(0, depth, -999999, 999999);
            if (score <= alpha)
                alpha -= 1187;
            else if (score >= beta)
                beta += 1187;
            else
            {
                alpha = score - 35;
                beta = score + 35;
                depth++;
            }
        }
        return BestMove;
    }
    #endregion

    #region PVsearch Eval
    int PVS(int ply, int depth, int alpha, int beta, bool allowNMFP = true)
    {
        ulong posHashKey = board.ZobristKey;
        Move bestMove = default, move;
        int bestEval = -999999 - 1, alphaOrig = alpha, eval = Evaluate(), movesTried = 0, score = 0;
        bool inCheck = board.IsInCheck(), allowFP = false;

        if (AbortSearch || ply != 0 && board.IsRepeatedPosition())
            return 0;

        var ttEntry = TT[posHashKey % 0x4FFFFF];
        if (ply > 0 && ttEntry.Item1 == posHashKey && ttEntry.Item3 >= depth &&
            (ttEntry.Item2 == 2 || ttEntry.Item2 == 1 && ttEntry.Item4 <= alpha || ttEntry.Item2 == 0 && ttEntry.Item4 >= beta))
            return ttEntry.Item4;

        if (inCheck)
            depth++;

        int CompressedPVS(int alpha, int beta, bool NMFP, int R = 1) => score = -PVS(ply + 1, depth - R, alpha, beta, NMFP);

        bool qsearch = depth <= 0;
        if (qsearch)
        {
            bestEval = eval;
            if (bestEval >= beta)
                return bestEval;
            alpha = Math.Max(alpha, bestEval);
        }
        else if (beta - alpha == 1 && !inCheck)
        {
            if (eval - 205 * depth >= beta)
                return eval;

            if (allowNMFP && depth > 2)
            {
                board.ForceSkipTurn();
                CompressedPVS(-beta, 1 - beta, false, 3);
                board.UndoSkipTurn();
                if (score >= beta)
                    return score;
            }

            allowFP = depth <= 6 && eval + 59 * depth <= alpha;
        }

        var moves = board.GetLegalMoves(qsearch && !inCheck)
            .OrderByDescending(x => x == ttEntry.Item5 ? 999999 :
                                    x.IsPromotion ? 20000 :
                                    x.IsCapture ? 10000 * (int)x.CapturePieceType - (int)x.MovePieceType :
                                    x == killers[ply] ? 10000 :
                                    hhScore[ply & 1, (int)x.MovePieceType, x.TargetSquare.Index]).ToArray();

        for (int i = -1; ++i < moves.Length;)
        {
            move = moves[i];
            if (allowFP && !(movesTried == 0 || move.IsCapture || move.IsPromotion))
                continue;
            board.MakeMove(move);

            bool fullS = i == 0 || qsearch, allowNMP = movesTried++ > 5;

            CompressedPVS(fullS ? -beta : -alpha - 1, -alpha, allowNMP);
            if (!fullS && alpha < score && score < beta)
                CompressedPVS(-beta, -alpha, allowNMP);

            board.UndoMove(move);

            if (AbortSearch)
                return bestEval;

            if (score > bestEval)
            {
                bestEval = score;
                bestMove = move;
                if (ply == 0)
                    BestMove = move;
                alpha = Math.Max(alpha, score);
                if (alpha >= beta)
                {
                    if (!move.IsCapture)
                    {
                        hhScore[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                        killers[ply] = move;
                    }
                    break;
                }
            }
        }
        if (!qsearch && moves.Length == 0)
            return inCheck ? ply - 999999 : 0;
        TT[posHashKey % 0x4FFFFF] = new(posHashKey, bestEval >= beta ? 0 : bestEval <= alphaOrig ? 1 : 2, depth, bestEval, bestMove == default ? ttEntry.Item5 : bestMove);

        return bestEval;
    }

    int Evaluate()
    {
        int mg = 0, eg = 0, phase = 0;
        for (int square = 0; square < 64; ++square)
        {
            Piece piece = board.GetPiece(new Square(square));
            if (piece.IsNull)
                continue;
            int pieceIdx = (int)piece.PieceType - 1;

            mg += piece.IsWhite ? PSQTable[square ^ 56][pieceIdx] : -PSQTable[square][pieceIdx];
            eg += piece.IsWhite ? PSQTable[square ^ 56][pieceIdx + 6] : -PSQTable[square][pieceIdx + 6];
            phase += PieceValues[pieceIdx] / 200;
        }
        return (mg * phase + eg * (26 - phase)) / (board.IsWhiteToMove ? 26 : -26);
    }
    #endregion
}