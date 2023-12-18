namespace auto_Bot_269;
//#define DEBUG_TIMER
//#define DEBUG_TREE_SEARCH




using ChessChallenge.API;
using System;
using System.Linq;





/// <summary>
/// Main bot class that does the thinking.
/// </summary>
public class Bot_269 : IChessBot
{
    #region rTypes

    /// <summary>
    /// Transposition table entry. Stores best move and evaluation for a board.
    /// </summary>
    struct TEntry
    {
        public ulong mKey;
        public Move mBestMove;
        public int mDepth, mEval, mEvalType;
        public TEntry(ulong key, Move move, int depth, int eval, int evalType)
        {
            mKey = key;
            mBestMove = move;
            mDepth = depth;
            mEval = eval;
            mEvalType = evalType;
        }
    }

    #endregion rTypes





    #region rConstants

    // Credit to Tyrant: https://github.com/Tyrant7/Easy-PST-Packer/tree/main
    decimal[] kPackedPTables =
    {
        63746705523041458768562654720m, 71818693703096985528394040064m, 75532537544690978830456252672m, 75536154932036771593352371712m, 76774085526445040292133284352m, 03110608541636285947269332480m, 00936945638387574698250991104m, 75531285965747665584902616832m,
        77047302762000299964198997571m, 03730792265775293618620982364m, 03121489077029470166123295018m, 03747712412930601838683035969m, 03763381335243474116535455791m, 08067176012614548496052660822m, 04977175895537975520060507415m, 02475894077091727551177487608m,
        02458978764687427073924784380m, 03718684080556872886692423941m, 04959037324412353051075877138m, 03135972447545098299460234261m, 04371494653131335197311645996m, 09624249097030609585804826662m, 09301461106541282841985626641m, 02793818196182115168911564530m,
        77683174186957799541255830262m, 04660418590176711545920359433m, 04971145620211324499469864196m, 05608211711321183125202150414m, 05617883191736004891949734160m, 07150801075091790966455611144m, 05619082524459738931006868492m, 00649197923531967450704711664m,
        75809334407291469990832437230m, 78322691297526401047122740223m, 04348529951871323093202439165m, 04990460191572192980035045640m, 05597312470813537077508379404m, 04980755617409140165251173636m, 01890741055734852330174483975m, 76772801025035254361275759599m,
        75502243563200070682362835182m, 78896921543467230670583692029m, 02489164206166677455700101373m, 04338830174078735659125311481m, 04960199192571758553533648130m, 03420013420025511569771334658m, 01557077491473974933188251927m, 77376040767919248347203368440m,
        73949978050619586491881614568m, 77043619187199676893167803647m, 01212557245150259869494540530m, 03081561358716686153294085872m, 03392217589357453836837847030m, 01219782446916489227407330320m, 78580145051212187267589731866m, 75798434925965430405537592305m,
        68369566912511282590874449920m, 72396532057599326246617936384m, 75186737388538008131054524416m, 77027917484951889231108827392m, 73655004947793353634062267392m, 76417372019396591550492896512m, 74568981255592060493492515584m, 70529879645288096380279255040m
    };



    //                     P    N    B    R    Q    K
    int[] kPieceValues = { 100, 300, 310, 500, 900,   0,
                           110, 270, 290, 550,1000,   0 };
    int[] kPiecePhase = { 0, 1, 1, 2, 4, 0 };

    int kMassiveNum = 99999999;
    const int kTTSize = 8333329;

    #endregion rConstants





    #region rDebug

#if DEBUG_TIMER
    int dNumMovesMade = 0;
    int dTotalMsElapsed = 0;
#endif
#if DEBUG_TREE_SEARCH
    int dNumPositionsEvaluated;
#endif

    #endregion rDebug





    #region rMembers

    Board mBoard;
    Move mBestMove;
    TEntry[] mTranspositionTable = new TEntry[kTTSize];
    int[][] mPTables = new int[64][];

    #endregion rMembers





    #region rInitialise

    /// <summary>
    /// Create bot
    /// </summary>
    public Bot_269()
    {
        mPTables = kPackedPTables.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3)
                .SelectMany(c => BitConverter.GetBytes(c)
                    .Select((byte square) => (int)((sbyte)square * 1.461) + kPieceValues[pieceType++]))
                .ToArray();
        }).ToArray();
    }

    #endregion rInitialise





    #region rThinking

    /// <summary>
    /// Top level thinking function.
    /// </summary>
    public Move Think(Board board, Timer timer)
    {
        mBoard = board;

#if DEBUG_TREE_SEARCH
        dNumPositionsEvaluated = 0;
#endif
        int msRemain = timer.MillisecondsRemaining;
        mBestMove = mBoard.GetLegalMoves()[0];
        if (msRemain < 200)
            return mBestMove;

        int depth = 1;
        while (timer.MillisecondsElapsedThisTurn < (msRemain / 200))
            if (EvaluateBoardNegaMax(++depth, -kMassiveNum, kMassiveNum, true) > 999999) break;

#if DEBUG_TIMER
        dNumMovesMade++;
        dTotalMsElapsed += timer.MillisecondsElapsedThisTurn;
        DivertedConsole.Write("My bot time average: {0}", (float)dTotalMsElapsed / dNumMovesMade);
#endif
#if DEBUG_TREE_SEARCH
        int msElapsed = timer.MillisecondsElapsedThisTurn;
        DivertedConsole.Write("Num positions evaluated {0} in {1}ms | Depth {2}", dNumPositionsEvaluated, msElapsed, mDepth);
#endif
        return mBestMove;
    }





    /// <summary>
    /// Recursive search of given board position.
    /// </summary>
    int EvaluateBoardNegaMax(int depth, int alpha, int beta, bool top, int totalExtension = 0)
    {
        ulong boardKey = mBoard.ZobristKey;
        Move[] legalMoves = mBoard.GetLegalMoves();
        float alphaOrig = alpha;
        Move move, bestMove = Move.NullMove;
        int recordEval = int.MinValue;

        // Check for definite evaluations.
        if (mBoard.IsRepeatedPosition() || mBoard.IsInsufficientMaterial() || mBoard.FiftyMoveCounter >= 100)
            return 0;

        if (legalMoves.Length == 0)
            return mBoard.IsInCheck() ? -depth - 9999999 : 0;

        // Search transposition table for this board.
        TEntry entry = mTranspositionTable[boardKey % kTTSize];
        if (entry.mKey == boardKey && entry.mDepth >= depth)
        {
            if (entry.mEvalType == 0) return entry.mEval; // Exact
            else if (entry.mEvalType == 1) alpha = Math.Max(alpha, entry.mEval); // Lower bound
            else if (entry.mEvalType == 2) beta = Math.Min(beta, entry.mEval); // Upper bound
            if (alpha >= beta) return entry.mEval;
        }

        // Heuristic evaluation
        if (depth <= 0)
        {
#if DEBUG_TREE_SEARCH
            dNumPositionsEvaluated++;
#endif
            recordEval = (mBoard.IsWhiteToMove ? 1 : -1) * (EvalBoard());
            if (recordEval >= beta || depth <= -4) return recordEval;
            alpha = Math.Max(alpha, recordEval);
        }

        // Sort Moves
        int[] moveScores = new int[legalMoves.Length];
        for (int i = 0; i < legalMoves.Length; ++i)
        {
            move = legalMoves[i];
            moveScores[i] = -(move == entry.mBestMove ? 1000000 :
                                       move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType :
                                     move.IsPromotion ? (int)move.PromotionPieceType :
                                                        -(int)move.MovePieceType);
        }
        Array.Sort(moveScores, legalMoves);

        // Tree search
        for (int i = 0; i < legalMoves.Length; ++i)
        {
            move = legalMoves[i];
            if (depth <= 0 && !move.IsCapture) continue; // Only search captures in qsearch
            mBoard.MakeMove(move);
            int extension = totalExtension < 6 && mBoard.IsInCheck() ? 1 : 0;
            int evaluation = -EvaluateBoardNegaMax(depth - 1 + extension, -beta, -alpha, false, totalExtension + extension);
            mBoard.UndoMove(move);

            if (recordEval < evaluation)
            {
                recordEval = evaluation;
                bestMove = move;
                if (top)
                    mBestMove = move;
            }
            alpha = Math.Max(alpha, recordEval);
            if (alpha >= beta)
                break;
        }

        // Store in transposition table
        mTranspositionTable[boardKey % kTTSize] = new TEntry(boardKey, bestMove, depth, recordEval,
            recordEval <= alphaOrig ? 2 :
            recordEval >= beta ? 1 :
            0);

        return recordEval;
    }

    /// <summary>
    /// Evaluate the board for a given color.
    /// </summary>
    int EvalBoard()
    {
        int square = 0, mg = 0, eg = 0, phase = 0;

        int[,] pawns = new int[2, 8];
        int[] bishops = new int[2];

        for (; square < 64; ++square)
        {
            Piece piece = mBoard.GetPiece(new Square(square));
            if (piece.IsNull) continue;

            int pieceIdx = (int)piece.PieceType - 1;
            int colIdx = piece.IsWhite ? 0 : 1;

            switch (piece.PieceType)
            {
                case PieceType.Pawn:
                    ++pawns[colIdx, square % 8];
                    break;
                case PieceType.Bishop:
                    bishops[colIdx]++;
                    break;
            }

            mg += piece.IsWhite ? mPTables[square ^ 56][pieceIdx] : -mPTables[square][pieceIdx];
            eg += piece.IsWhite ? mPTables[square ^ 56][pieceIdx + 6] : -mPTables[square][pieceIdx + 6];
            phase += kPiecePhase[pieceIdx];
        }

        phase = Math.Min(phase, 24);
        phase = (mg * phase + eg * (24 - phase)) / 24;
        phase += (bishops[0] * bishops[0] - bishops[1] * bishops[1]) * 50;

        for (square = 0; square < 8; ++square)
        {
            mg = pawns[0, square];
            eg = pawns[1, square];
            phase += (mg - eg) * (1 - mg - eg);
        }

        return phase + ((int)mBoard.ZobristKey % 8);
    }

    #endregion rThinking
}