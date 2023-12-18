namespace auto_Bot_452;
using ChessChallenge.API;
using System;

//using System.Runtime.InteropServices; //#DEBUG

public class Bot_452 : IChessBot
{
    //private const int MAX_PLY = 64, TIME_SCALE = 30; //#DEBUG //30
    private int[,] HISTORY_MOVES = new int[13, 64]; //piece, square
    private Move[] KILLER_MOVE = new Move[64]; //move at PLY
    private int[] mScores = new int[256]; //prealloc move ordering array
    private int ITERATION, STOP_TIME;
    private Timer TIMER_CPY;
    private Board BOARD_CPY;

    private readonly (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[0x800000]; //key, move, draft, score, flag

    int[] MaterialValue = {0, 88, 309, 331, 495, 981, 0,  //index 0 is no-piece
                               0,   6,   7,  10,  18, 0}; //piece phase

    int[,] PIECE_SQUARE = new int[12, 64];

    public Move Think(Board board, Timer timer)
    {
        BOARD_CPY = board;
        Move[] mArray = BOARD_CPY.GetLegalMoves();
        Move bestMove = mArray[0];

        TIMER_CPY = timer;
        STOP_TIME = timer.MillisecondsRemaining / 30;

        if (mArray.Length == 1) return bestMove; //only 1 legal move

        ITERATION = 1;

        do
        {
            try
            {
                Negamax(ITERATION++, -200000, 200000, false, false, 0);
                bestMove = tt[BOARD_CPY.ZobristKey & 0x7FFFFF].Item2;
                //DivertedConsole.Write($"Depth {ITERATION-1} best move: {bestMove.StartSquare.Name}{bestMove.TargetSquare.Name} score: {tt[BOARD_CPY.ZobristKey & 0x7FFFFF].Item4}"); //#DEBUG
            }
            catch
            {
                break; //break loop
            }

        }
        while (ITERATION < 32); //MAX_PLY reached or not enough time to search again

        return bestMove;
    }

    private int Negamax(int depth, int alpha, int beta, bool doNull, bool isInCheck, int ply)
    {
        if (isInCheck) depth++; //check extension

        int hashFlag = 0, score, wtmOffset = BOARD_CPY.IsWhiteToMove ? 0 : 6, R = 0, moveCnt = 0; //upper, no score beat alpha
        bool inQuiescence = depth < 1, pvNode = beta - alpha > 1, opponentInCheck;

        if (ply == 64) return EvaluateBoard();

        if (TIMER_CPY.MillisecondsElapsedThisTurn > STOP_TIME) throw new Exception(); //jump to think without mucking up TT

        if (ply > 0 && BOARD_CPY.IsRepeatedPosition()) return -25; //ContemptFactor, much faster than full draw check.  I'd rather beat with fast searching than get more draws

        ulong key = BOARD_CPY.ZobristKey;
        ref var entry = ref tt[key & 0x7FFFFF];

        if (key == entry.Item1 && (entry.Item3 >= depth || depth <= 0)) // && Math.Abs(score = entry.Item4) < 90000)
        {
            score = entry.Item4;
            if (entry.Item5 == 0) //upper
                beta = Math.Min(beta, score);
            else if (entry.Item5 == 2) //lower
                alpha = Math.Max(alpha, score);
            else return score; //exact
            if (alpha >= beta) return beta;
        }

        if (!isInCheck)
            if (inQuiescence)
            {
                alpha = Math.Max(alpha, EvaluateBoard());
                if (alpha >= beta) return beta;
            }
            else if (depth >= 2 && !pvNode && doNull)
            {
                BOARD_CPY.ForceSkipTurn(); //force so we do not waste time checking if valid
                score = -Negamax(depth - (3 + depth / 4), -beta, -beta + 1, false, false, ply + 1); //disable NMP and set not in check
                BOARD_CPY.UndoSkipTurn();
                if (score >= beta) return beta;
            }

        //gen legal moves from this pos
        Span<Move> allMoves = stackalloc Move[256];
        BOARD_CPY.GetLegalMovesNonAlloc(ref allMoves, inQuiescence && !isInCheck);

        //see why the game ended
        if (allMoves.IsEmpty && (!inQuiescence || isInCheck)) return isInCheck ? -100000 + ply : -25; //CheckmateValue or ContemptFactor

        //score moves
        foreach (Move move in allMoves) //also use R as index variable
            mScores[R++] = -(move.Equals(entry.Item2) ? 100000000 :
                move.IsPromotion && (int)move.PromotionPieceType == 5 ? 90000000 :
                move.IsCapture ? 75000000 + 8 * (int)move.CapturePieceType - (int)move.MovePieceType :
                KILLER_MOVE[ply].Equals(move) ? 50000000 :
                HISTORY_MOVES[(int)move.MovePieceType + wtmOffset, move.TargetSquare.Index]);

        //sort moves
        mScores.AsSpan(0, allMoves.Length).Sort(allMoves);

        //Lambda for negamax
        int ngmx(int _alpha, int _R = 0) => score = -Negamax(depth - 1 - _R, _alpha, -alpha, true, opponentInCheck, ply + 1);

        Move bestMove = entry.Item2; //keep old best move if this is an All-node for extra move ordering fun

        foreach (Move move in allMoves)
        {
            R = 0;

            //make the next move
            BOARD_CPY.MakeMove(move);
            opponentInCheck = BOARD_CPY.IsInCheck();

            if (!(moveCnt == 0 || opponentInCheck || isInCheck || move.IsPromotion || pvNode)) R = 3; //LMR

            moveCnt++;

            //Null Window Reduction search
            _ = moveCnt == 0 && pvNode //searching a suspected pv-move in a pv-node 
                ? ngmx(-beta) //full search
                : ngmx(-alpha - 1, R) > alpha  //null window & reduced depth
                    ? (R > 0 ? ngmx(-alpha - 1) : score) > alpha //null window without reduction
                        ? ngmx(-beta) //full search
                        : 0
                    : 0;

            //take back move
            BOARD_CPY.UndoMove(move);

            //use move score
            if (score > alpha)
            {
                //update these early for tt write
                bestMove = move;
                alpha = score;

                if (score >= beta)
                {
                    if (!move.IsCapture)
                    {
                        KILLER_MOVE[ply] = move;
                        HISTORY_MOVES[(int)move.MovePieceType + wtmOffset, move.TargetSquare.Index] += depth * depth; //0 : 6 is color offset
                    }

                    hashFlag = 2; //lower
                    break;
                }

                hashFlag = 1; //exact

            }
        }

        entry = new(key, bestMove, depth, alpha, hashFlag);
        return alpha;
    }

    private int EvaluateBoard()
    {
        int scoreStart = 0, scoreEnd = 0, phase = 0, sideToMoveIdx, square, piece;
        ulong bb;

        for (sideToMoveIdx = 0; sideToMoveIdx < 2; sideToMoveIdx++, scoreStart = -scoreStart, scoreEnd = -scoreEnd)
            for (piece = 0; piece < 6; piece++)
                for (bb = BOARD_CPY.GetPieceBitboard((PieceType)piece + 1, sideToMoveIdx == 0); bb > 0;)
                {
                    //phase += PIECE_PHASE_LUT[piece];
                    phase += MaterialValue[piece + 7];
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref bb) ^ 56 * sideToMoveIdx;
                    scoreStart += PIECE_SQUARE[piece, square];
                    scoreEnd += PIECE_SQUARE[piece + 6, square];
                }

        return (scoreEnd * (128 - phase) + scoreStart * phase) / 128 * (BOARD_CPY.IsWhiteToMove ? 1 : -1);
    }

    //float scaleFactor = 193f;
    ulong[] PST = new ulong[]
    {
        69236217153993984UL, 17297503790035045627UL, 17944897387977185514UL, 583816648739653612UL, 656997913731864581UL, 1161404374708526605UL, 17359381971960468749UL, 17292135939790599659UL,
        1304948881840874030UL, 2748932048090370577UL, 69788103035845367UL, 17725590812063630581UL, 2819169578488064UL, 2459272272412682024UL, 15555691437820928209UL, 15122780703108686554UL,
        16277111785883625185UL, 16350011601483130596UL, 650777890216808467UL, 579566920132464141UL, 15047622516513239790UL, 16495820071909254360UL, 17221205345340164851UL, 18088440752190652144UL,
    };


    public Bot_452()
    {
        /*
        long totalSize = 0;                                                                     //#DEBUG
        totalSize += PST.Length * sizeof(ulong);                                                //#DEBUG
        totalSize += MaterialValue.Length * sizeof(int);                                        //#DEBUG
        //totalSize += PIECE_PHASE_LUT.Length * sizeof(int);                                     //#DEBUG
        totalSize += KILLER_MOVE.Length * Marshal.SizeOf(typeof(Move));                         //#DEBUG
        totalSize += HISTORY_MOVES.GetLength(0) * HISTORY_MOVES.GetLength(1) * sizeof(int);     //#DEBUG
        totalSize += PIECE_SQUARE.GetLength(0) * PIECE_SQUARE.GetLength(1) * sizeof(int);       //#DEBUG
        // Calculate the size of one element in the tt array
        long ttElementSize = sizeof(ulong) + Marshal.SizeOf(typeof(Move)) + sizeof(int) * 3;    //#DEBUG
        // Multiply by the length of the tt array to get the total size
        totalSize += tt.Length * ttElementSize;                                                 //#DEBUG
        DivertedConsole.Write($"Total size of LUTs: {totalSize / 1000.0 / 1000.0} MB");             //#DEBUG
        */

        for (int piece = 0; piece < 12; piece++)
            for (int square = 0; square < 64; square++)
            {

                ITERATION = piece * 16 + square / 8;
                STOP_TIME = piece * 16 + square % 8 + 8;
                PIECE_SQUARE[piece, square ^ 56] = MaterialValue[piece % 6 + 1]
                    + (int)Math.Round(((sbyte)(PST[ITERATION / 8] >> ((ITERATION % 8) * 8) & 0xFF) + (sbyte)(PST[STOP_TIME / 8] >> ((STOP_TIME % 8) * 8) & 0xFF)) / (2f * sbyte.MaxValue) * 193f);
            }

    }

}