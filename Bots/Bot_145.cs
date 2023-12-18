namespace auto_Bot_145;
using ChessChallenge.API;
using System;

public class Bot_145 : IChessBot
{

    // none, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 1000000 };
    int[] piecePhases = { 0, 0, 1, 1, 2, 4, 0 };
    // 0-7: pawn, 8-15: knight, 16-23: bishop, 24-31: rook, 32-39: queen, 40-47: king mg, 48-55: king eg
    ulong[] psts = { 0b0011001000110010001100100011001000110010001100100011001000110010, 0b0011011100111100001111000001111000011110001111000011110000110111, 0b0011011100101101001010000011001000110010001010000010110100110111, 0b0011001000110010001100100100011001000110001100100011001000110010, 0b0011011100110111001111000100101101001011001111000011011100110111, 0b0011110000111100010001100101000001010000010001100011110000111100, 0b0110010001100100011001000110010001100100011001000110010001100100, 0b0011001000110010001100100011001000110010001100100011001000110010, 0b0000000000001010000101000001010000010100000101000000101000000000, 0b0000101000011110001100100011011100110111001100100001111000001010, 0b0001010000110111001111000100000101000001001111000011011100010100, 0b0001010000110010010000010100011001000110010000010011001000010100, 0b0001010000110111010000010100011001000110010000010011011100010100, 0b0001010000110010001111000100000101000001001111000011001000010100, 0b0000101000011110001100100011001000110010001100100001111000001010, 0b0000000000001010000101000001010000010100000101000000101000000000, 0b0001111000101000001010000010100000101000001010000010100000011110, 0b0010100000110111001100100011001000110010001100100011011100101000, 0b0010100000111100001111000011110000111100001111000011110000101000, 0b0010100000110010001111000011110000111100001111000011001000101000, 0b0010100000110111001101110011110000111100001101110011011100101000, 0b0010100000110010001101110011110000111100001101110011001000101000, 0b0010100000110010001100100011001000110010001100100011001000101000, 0b0001111000101000001010000010100000101000001010000010100000011110, 0b0011001000110010001100100011011100110111001100100011001000110010, 0b0010110100110010001100100011001000110010001100100011001000101101, 0b0010110100110010001100100011001000110010001100100011001000101101, 0b0010110100110010001100100011001000110010001100100011001000101101, 0b0010110100110010001100100011001000110010001100100011001000101101, 0b0010110100110010001100100011001000110010001100100011001000101101, 0b0011011100111100001111000011110000111100001111000011110000110111, 0b0011001000110010001100100011001000110010001100100011001000110010, 0b0001111000101000001010000010110100101101001010000010100000011110, 0b0010100000110010001101110011001000110010001100100011001000101000, 0b0010100000110111001101110011011100110111001101110011001000101000, 0b0011001000110010001101110011011100110111001101110011001000101101, 0b0010110100110010001101110011011100110111001101110011001000101101, 0b0010100000110010001101110011011100110111001101110011001000101000, 0b0010100000110010001100100011001000110010001100100011001000101000, 0b0001111000101000001010000010110100101101001010000010100000011110, 0b0100011001010000001111000011001000110010001111000101000001000110, 0b0100011001000110001100100011001000110010001100100100011001000110, 0b0010100000011110000111100001111000011110000111100001111000101000, 0b0001111000010100000101000000101000001010000101000001010000011110, 0b0001010000001010000010100000000000000000000010100000101000010100, 0b0001010000001010000010100000000000000000000010100000101000010100, 0b0001010000001010000010100000000000000000000010100000101000010100, 0b0001010000001010000010100000000000000000000010100000101000010100, 0b0000000000010100000101000001010000010100000101000001010000000000, 0b0001010000010100001100100011001000110010001100100001010000010100, 0b0001010000101000010001100101000001010000010001100010100000010100, 0b0001010000101000010100000101101001011010010100000010100000010100, 0b0001010000101000010100000101101001011010010100000010100000010100, 0b0001010000101000010001100101000001010000010001100010100000010100, 0b0001010000011110001010000011001000110010001010000001111000010100, 0b0000000000001010000101000001111000011110000101000000101000000000 };

    Move[] killerMoves = new Move[32];

    struct TTEntry
    {
        public ulong key;
        public Move move;

        // bound: 3-exact (new best move), 2-lowerbound (eval is beta or higher), 1-upperbound (eval is less than alpha)
        // public int depth, score , bound;
        public TTEntry(ulong key, Move move /*,  int depth, int score , int bound*/)
        {
            this.key = key;
            this.move = move;
            // this.score = score;
            // this.bound = bound;
        }
    }

    const int size = 1 << 20;
    TTEntry[] tt = new TTEntry[size];
    // int numCutoffs;

    static Move bestMove = Move.NullMove;
    bool abort;
    // Move preAbortBestMove = Move.NullMove;

    // multiplying -1 to int.MinValue will cause an overflow, so use infinity for max number
    static int infinity = 9999999;

    int posCount = 0;

    public Move Think(Board board, Timer timer)
    {
        posCount = 0;
        bestMove = Move.NullMove;
        abort = false;
        // numCutoffs = 0;
        //
        // DivertedConsole.Write("Best move: " + bestMove.ToString());
        //
        // DivertedConsole.Write(timer.MillisecondsElapsedThisTurn);
        // DivertedConsole.Write("posCount: " + posCount);

        int depth;
        for (depth = 1; depth <= 30; depth++)
        {
            Search(board, timer, depth, -infinity, infinity);

            if (abort)
                break;

        }
        // DivertedConsole.Write("depth: " + depth);
        // DivertedConsole.Write("cutoffs: " + numCutoffs);

        // return abort ? preAbortBestMove : bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }

    int getPstValue(int pieceType, int idx)
    {
        return (int)(psts[((pieceType - 1) * 8 + (idx / 8))] >> (64 - (((idx % 8) + 1) * 8)) & 127) - 50;
    }


    int Evaluate(Board board)
    {

        int mg = 0, eg = 0, phase = 0;
        foreach (bool isWhite in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p;
                ulong bb = board.GetPieceBitboard(p, isWhite);
                while (bb != 0)
                {
                    phase += piecePhases[piece];
                    int idx = (isWhite ? BitboardHelper.ClearAndGetIndexOfLSB(ref bb) : 63 - BitboardHelper.ClearAndGetIndexOfLSB(ref bb));
                    mg += pieceValues[piece] + getPstValue(piece, idx);
                    eg += pieceValues[piece] + getPstValue(p == PieceType.King ? 7 : piece, idx);
                }

            }
            mg = -mg;
            eg = -eg;

        }

        return (mg * (Math.Max(phase, 24)) + eg * (24 - phase)) * (board.IsWhiteToMove ? 1 : -1);
    }

    int Search(Board board, Timer timer, int depth, int alpha, int beta, int movesFromRoot = 0, int numExtensions = 0)
    {
        posCount += 1;
        ulong key = board.ZobristKey;
        bool root = movesFromRoot == 0;
        bool qsearch = depth <= 0;
        int bestMoveScore = -infinity;

        if (!root && board.IsRepeatedPosition())
            return 0;


        TTEntry entry = tt[key % size];


        // if (!root && entry.key == key && entry.depth >= depth && (entry.bound == 3 || entry.bound == 2 && entry.score >= beta || entry.bound == 1 && entry.score <= alpha))
        // {
        //     numCutoffs++;
        //     return entry.score;
        // }

        // if (!root && key == entry.key && entry.depth >= depth && (entry.bound == 3 || entry.bound == 2 && entry.score >= beta || entry.bound == 3 && entry.score <= alpha))
        //     return entry.score;


        if (qsearch)
        {

            int eval = Evaluate(board);
            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);
        }

        Move[] moves = board.GetLegalMoves(qsearch);



        int[] moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            int moveScoreGuess = 0;
            Move move = moves[i];
            if (root && move == bestMove)
                moveScoreGuess += 1000000;
            if (entry.key == key && entry.move == move)
                moveScoreGuess += 100000;
            if (movesFromRoot < 32 && killerMoves[movesFromRoot] == move)
                moveScoreGuess += 10000;
            if (move.IsCapture)
                moveScoreGuess += 10 * pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
            if (move.IsPromotion)
                moveScoreGuess += pieceValues[(int)move.PromotionPieceType];


            moveScores[i] = moveScoreGuess;
        }

        Sort(moves, moveScores);

        Move currentBestMove = Move.NullMove;
        // int bound = 1;
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            board.MakeMove(move);
            int extension = numExtensions < 16 && board.IsInCheck() ? 1 : (move.MovePieceType == PieceType.Pawn && (move.TargetSquare.Rank == 1 || move.TargetSquare.Rank == 6)) ? 1 : 0;
            bool lmr = depth > 2 && i > 3 && !move.IsPromotion && extension == 0;
            int newDepth = lmr ? depth - 2 : depth - 1 + extension;
            int score = -Search(board, timer, newDepth, -beta, -alpha, movesFromRoot + 1, numExtensions + extension);
            if (lmr && score >= beta)
                score = -Search(board, timer, depth - 1 + extension, -beta, -alpha, movesFromRoot + 1, numExtensions + extension);

            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 30 || abort)
            {
                abort = true;
                break;
            }

            board.UndoMove(move);
            if (score > bestMoveScore)
            {
                currentBestMove = move;
                bestMoveScore = score;
                if (score >= beta)
                {
                    // move too good
                    alpha = beta;
                    // bound = 2;
                    if (!move.IsCapture && !move.IsPromotion && movesFromRoot < 32)
                        killerMoves[movesFromRoot] = move;
                    break;
                }
                if (score > alpha)
                {
                    // bound = 3;
                    alpha = score;
                    if (movesFromRoot == 0)
                        bestMove = move;
                }
            }
        }
        tt[key % size] = new TTEntry(key, currentBestMove);

        if (!qsearch && moves.Length == 0)
            return board.IsInCheck() ? -infinity + movesFromRoot : 0;
        // tt[key % size] = new TTEntry(key, currentBestMove, depth, alpha, bound);
        return alpha;

    }

    void Sort(Move[] moves, int[] moveScores)
    {
        for (int i = 0; i < moves.Length - 1; i++)
        {

            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
    }

}
