namespace auto_Bot_221;
using ChessChallenge.API;
using System;

public class Bot_221 : IChessBot
{

    //Global variables
    private Board globalBoard;
    private Timer globalTimer;
    Move bestMove;
    Move currBestMove;
    private int infinity = 100000;
    private int millisecondsAvailable;

    //Transposition table
    record struct ttEntry(ulong key, int score, int depth, int flag, Move move);
    ttEntry[] transpositionTable;
    const ulong ttEntries = 0x8FFFFF;

    //Piece square table - thanks to https://github.com/JacquesRW for compressing algorithm
    readonly ulong[] pieceSquareTableCompressed = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    int[,,] pieceSquareTable;

    public Bot_221()
    {
        transpositionTable = new ttEntry[ttEntries];
        pieceSquareTable = new int[2, 6, 64];

        // Pre-extract all values from compressed pst
        for (int phase = 0; phase < 2; phase++)
            for (int pieceType = 0; pieceType < 6; pieceType++)
                for (int square = 0; square < 64; square++)
                {
                    // Get index in compressed pst
                    int index = 128 * pieceType + 64 * phase + square;
                    // Populate pst using decompression
                    pieceSquareTable[phase, pieceType, square] = (int)(((pieceSquareTableCompressed[index / 10] >> (6 * (index % 10))) & 63) - 20) * 8;
                }
    }
    public Move Think(Board board, Timer timer)
    {
        //Time saving first move
        //if (board.IsWhiteToMove && board.PlyCount == 0 && board.ZobristKey == 13227872743731781434) return new Move("e2e4", board);

        //Save global variables
        globalBoard = board;
        globalTimer = timer;
        millisecondsAvailable = timer.MillisecondsRemaining / 20;

        //Iterative deepening
        for (int currDepth = 0; currDepth < 100; currDepth++)
        {
            int evaluation = negamax(currDepth, 0, -infinity, infinity);

            if (timer.MillisecondsElapsedThisTurn > millisecondsAvailable) break;

            bestMove = currBestMove;

            //Evaluation is too high (i.e. checkmate found), there's no need to keep searching
            if (evaluation > infinity / 2) break;
        }

        return bestMove;
    }

    private int negamax(int depth, int ply, int alpha, int beta)
    {
        bool isRootNode = ply == 0 ? true : false;
        bool isQuiescenceSearch = depth < 0 ? true : false;
        int bestEvaluation = -infinity;

        //Punish draw by repetition
        if (!isRootNode && globalBoard.IsRepeatedPosition()) return -50;

        //Transposition table lookup
        ulong key = globalBoard.ZobristKey;
        ttEntry currEntry = transpositionTable[key % ttEntries];

        if (currEntry.key == key && !isRootNode && currEntry.depth >= depth)
        {
            if (currEntry.flag == 0 || (currEntry.flag == 1 && currEntry.score <= alpha) || (currEntry.flag == 2 && currEntry.score >= beta))
            {
                return currEntry.score;
            }
        }

        //Delta pruning
        if (isQuiescenceSearch)
        {
            bestEvaluation = evaluatePosition();

            if (bestEvaluation >= beta) return beta;
            if (bestEvaluation <= alpha - 1025) return alpha;
            alpha = Math.Max(alpha, bestEvaluation);
        }

        //Order moves
        Move[] moves = globalBoard.GetLegalMoves(isQuiescenceSearch);
        int[] moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == currEntry.move) moveScores[i] = 100;
            if (moves[i].IsCapture) moveScores[i] = 10 * ((int)moves[i].CapturePieceType - (int)moves[i].MovePieceType);
        }

        Array.Sort(moveScores, moves);
        Array.Reverse(moves);

        //Calculate best move
        Move bestMove = Move.NullMove;
        int initialAlpha = alpha;

        foreach (Move move in moves)
        {
            if (globalTimer.MillisecondsElapsedThisTurn > millisecondsAvailable) return 0;

            globalBoard.MakeMove(move);
            int evaluation;
            evaluation = -negamax(depth - 1, ply + 1, -beta, -alpha);
            globalBoard.UndoMove(move);

            //Alpha-beta pruning
            if (evaluation > bestEvaluation)
            {
                bestEvaluation = evaluation;
                bestMove = move;

                if (isRootNode) currBestMove = move;

                alpha = Math.Max(alpha, bestEvaluation);
                if (alpha >= beta) break;
            }
        }

        //Check for no availale moves
        if (moves.Length == 0 && !isQuiescenceSearch) return globalBoard.IsInCheck() ? -infinity : 0;

        //Transposition table update
        int flag = bestEvaluation >= beta ? 2 : bestEvaluation > initialAlpha ? 0 : 1;
        transpositionTable[key % ttEntries] = new ttEntry(key, bestEvaluation, depth, flag, bestMove);

        return bestEvaluation;
    }

    //Evaluation function
    private int[] middleGameValues = { 100, 310, 330, 500, 1000, 10000 };
    private int[] endGameValues = { 100, 310, 330, 500, 1000, 10000 };
    private int[] phaseWeight = { 0, 1, 1, 2, 4, 0 };
    private int evaluatePosition()
    {
        int phase = 0;
        int middleGameWeight = 0;
        int endGameWeight = 0;

        //Iterate through all pieces
        foreach (bool player in new[] { true, false })
        {
            for (int pieceType = 0; pieceType < 6; pieceType++)
            {
                ulong bitBoard = globalBoard.GetPieceBitboard((PieceType)pieceType + 1, player);

                //Extract piece values sum from the bitboard
                while (bitBoard != 0)
                {
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard) ^ (player ? 56 : 0);

                    middleGameWeight += middleGameValues[pieceType] + pieceSquareTable[0, pieceType, square];
                    endGameWeight += middleGameValues[pieceType] + pieceSquareTable[1, pieceType, square];

                    phase += phaseWeight[pieceType];
                }
            }

            middleGameWeight *= -1;
            endGameWeight *= -1;
        }

        phase = Math.Min(phase, 24);
        return ((middleGameWeight * phase) + (endGameWeight * (24 - phase))) / 24 * (globalBoard.IsWhiteToMove ? 1 : -1);
    }

}