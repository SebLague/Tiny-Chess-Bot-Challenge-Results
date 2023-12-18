namespace auto_Bot_441;
using ChessChallenge.API;
using System;

public class Bot_441 : IChessBot
{
    // Values of pieces: pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 100, 320, 328, 500, 900, 0 };

    // Values for approximating game phase
    int[] gamephaseIncrement = { 0, 1, 1, 2, 4, 0 };

    // Encoded piece-square table
    // Values are encoded in 64-bit values
    // Each hex-digit represents a score for a piece
    // on a particular square, normalized between 0-15,
    // where 8 is neutral (0), F is a very positive score,
    // and 0 is a very negative score.
    ulong[] pieceSquareTableEncoded = {

        /* Middle Game */

        // Pawns
        0x9AA55AA988888888,
        0x888BB88897688679,
        0xAABDDBAA99ACCA99,
        0x88888888FFFFFFFF,
        // Knights
        0x1589985101333310,
        0x38ABBA8339AAAA93,
        0x38AAAA8339ABBA93,
        0x0133331015888851,
        // Bishops
        0x6988889656666665,
        0x68AAAA866AAAAAA6,
        0x689AA986699AA996,
        0x6566666656888888,
        // Rooks
        0x7888888788888888,
        0x7888888778888887,
        0x7888888778888887,
        0x999999999AAAAAA9,
        // Queens
        0x6898888656677665,
        0x8899998769999986,
        0x6899998678999987,
        0x5667766568888886,
        // Kings
        0xBB8888BBBDA88ADB,
        0x5331133565555556,
        0x3110011331100113,
        0x3110011331100113,

        /* End Game */

        // Pawns
        0x9999999988888888,
        0xAAAAAAAA99999999,
        0xDDDDDDDDBBBBBBBB,
        0x88888888FFFFFFFF,
        // Knights
        0x1589985101333310,
        0x38ABBA8339AAAA93,
        0x38AAAA8339ABBA93,
        0x0133331015888851,
        // Bishops
        0x6988889656666665,
        0x68AAAA866AAAAAA6,
        0x689AA986699AA996,
        0x6566666656888888,
        // Rooks
        0x7888888788888888,
        0x7888888778888887,
        0x7888888778888887,
        0x999999999AAAAAA9,
        // Queens
        0x6898888656677665,
        0x8899998769999986,
        0x6899998678999987,
        0x5667766568888886,
        // Kings
        0x6789987616777761,
        0x479AA97447899874,
        0x47899874479AA974,
        0x1345543145788754,
    };

    // Transposition table entry, fits in 16 bytes
    private struct TTableEntry
    {
        public ulong ZobristKey;
        public Move BestMove;
        public short Depth;
        public short Evaluation;
    }

    int[] pieceSquareTable = new int[1536];

    TTableEntry[] transpositionTable;

    int[] history;

    int searchDepth;
    Move bestMove;

    Board myBoard;
    Timer myTimer;
    int thinkTime;

    bool TimesUp => myTimer.MillisecondsElapsedThisTurn > thinkTime;

    ref TTableEntry TableEntry => ref transpositionTable[myBoard.ZobristKey & 0x7FFFFF];

    public Bot_441()
    {
        // Allocate transposition table (8,388,608 (0x800000) * 16 bytes = 134.2 MB)
        transpositionTable = new TTableEntry[0x800000];

        // Copy encoded PSTs into a new array for easier access
        for (int i = 0; i < 768; i++)
        {
            int value = (int)(pieceSquareTableEncoded[i / 16] >> i % 16 * 4 & 0xF) * 8 - 64 + pieceValues[i / 64 % 6];
            pieceSquareTable[i / 384 * 384 + i] = value;
            pieceSquareTable[i / 384 * 384 + (i ^ 56) + 384] = value;
        }
    }

    public int StaticEvaluation()
    {
        int midgameEvaluation = 0;
        int endgameEvaluation = 0;
        int gamePhase = 0;

        PieceList[] pieceLists = myBoard.GetAllPieceLists();

        for (int i = 0; i < 12; i++)
        {
            PieceList pieceList = pieceLists[i];

            int mgEval = 0;
            int egEval = 0;

            // Count PST-values
            for (int j = 0; j < pieceList.Count; j++)
            {
                int index = 64 * i + pieceList.GetPiece(j).Square.Index;
                mgEval += pieceSquareTable[index];
                egEval += pieceSquareTable[index + 768];
                gamePhase += gamephaseIncrement[i % 6];
            }

            // Negate eval if white piece XOR white to move
            if (i < 6 != myBoard.IsWhiteToMove)
            {
                mgEval = -mgEval;
                egEval = -egEval;
            }

            midgameEvaluation += mgEval;
            endgameEvaluation += egEval;
        }

        if (gamePhase > 24) gamePhase = 24; // In case of early promotion

        return (midgameEvaluation * gamePhase + endgameEvaluation * (24 - gamePhase)) / 24 * 4;
    }

    // Get the next most promising move
    public Move GetMove(ref Span<Move> moves, ref Span<int> moveScores, int startIndex, Move searchThisMoveFirst, int ply)
    {
        for (int i = startIndex; i < moves.Length; i++)
        {
            Move move = moves[i];
            int score = moveScores[i];

            // Assign a score to this move if one hasn't been assigned yet
            if (score == 0)
            {
                if (move == searchThisMoveFirst)
                    score = 100000000;
                else if (move.IsCapture)
                    score = 10000 * pieceValues[(int)move.CapturePieceType - 1] - 1000 * pieceValues[(int)move.MovePieceType - 1];
                else
                    score += history[move.RawValue & 0xFFF] + 1;
                moveScores[i] = score + 1;
            }

            if (score > moveScores[startIndex])
            {
                moves[i] = moves[startIndex];
                moves[startIndex] = move;
                moveScores[i] = moveScores[startIndex];
                moveScores[startIndex] = score;
            }
        }

        return moves[startIndex];
    }

    // Quiescence search over capture moves only
    public int SearchCaptures(int alpha, int beta)
    {
        int eval = StaticEvaluation();
        if (eval > beta)
            return beta;
        alpha = Math.Max(alpha, eval);

        // Allocate array of moves on the stack
        Span<Move> moves = stackalloc Move[256];
        // Allocate array of move scores on the stack
        Span<int> moveScores = stackalloc int[256];
        // Generate legal moves
        myBoard.GetLegalMovesNonAlloc(ref moves, true);

        // Search all moves
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = GetMove(ref moves, ref moveScores, i, Move.NullMove, 31);
            myBoard.MakeMove(move);
            eval = -SearchCaptures(-beta, -alpha);
            myBoard.UndoMove(move);

            if (TimesUp || eval >= beta) // Returning beta if time is up is weird, but luckily we don't care about the return-value in that case. Saves a few tokens at least
                return beta;
            alpha = Math.Max(alpha, eval);
        }

        return alpha;
    }

    public int Search(int depth, int alpha, int beta)
    {
        // I used to return early if time's up here, but to save a few tokens, I removed this early return,
        // letting the search continue exploring to the next leaf node before backing out. The extra time
        // wasted exploring this one branch should be very minimal anyway.
        //// if (TimesUp)
        ////    return;

        if (myBoard.IsInCheckmate())
            return -(20000 + depth * 4); // Checkmate
        else if (myBoard.IsDraw())
            return 0; // Stalemate or draw

        int eval = TableEntry.Evaluation;
        bool samePosition = TableEntry.ZobristKey == myBoard.ZobristKey;

        // Evaluation scores are stored in the transposition table in an IBV encoding (Integrated Bound and Value), where
        // * EXACT scores are represented by 4n
        // * LOWER bounds are represented by 4n + 1
        // * UPPER bounds are represented by 4n - 1
        // So we can efficiently store both the score and the node type flag in a single value.
        // StaticEvaluation() always returns EXACT values, and as such the returned score is always a multiple of 4
        if (depth != searchDepth && samePosition && TableEntry.Depth >= depth)
        {
            int flag = eval & 3; // Extract the node-type flag
            if (flag == 0b00) // EXACT
                return eval;
            else if (flag == 0b01 && eval >= beta) // LOWER
                return beta;
            else if (eval <= alpha) // UPPER
                return alpha;
        }

        if (depth == 0)
            // Search depth reached, start quiescence search
            return SearchCaptures(alpha, beta);

        // Retrieve the best move from the transposition table if possible
        Move currentBestMove = samePosition ? TableEntry.BestMove : Move.NullMove;

        // Allocate array of moves on the stack
        System.Span<Move> moves = stackalloc Move[256];
        // Allocate array of move scores on the stack
        System.Span<int> moveScores = stackalloc int[256];
        // Generate legal moves
        myBoard.GetLegalMovesNonAlloc(ref moves);

        //GetLegalMovesAndScores(ref moves, ref moveScores, false, currentBestMove, searchDepth - depth);

        // Search all moves
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = GetMove(ref moves, ref moveScores, i, currentBestMove, searchDepth - depth);
            myBoard.MakeMove(move);
            eval = -Search(depth - 1, -beta, -alpha | 1); // Here, -alpha is either EXACT or LOWER. So we OR with 1 to force it to LOWER.
            myBoard.UndoMove(move);

            if (TimesUp)
                return 0;

            if (eval >= beta - 1) // Here beta is always LOWER. Subtracting 1 turns it into EXACT.
            {
                currentBestMove = move;
                alpha = beta;
                if (!move.IsCapture)
                    history[move.RawValue & 0xFFF] += depth * depth;
                break;
            }

            if (eval > alpha)
            {
                alpha = eval;
                currentBestMove = move;
                if (depth == searchDepth)
                    bestMove = move;
            }
        }

        // Store evaluation in transposition table
        TableEntry.BestMove = currentBestMove;
        TableEntry.ZobristKey = myBoard.ZobristKey;
        TableEntry.Depth = (short)depth;
        TableEntry.Evaluation = (short)alpha;

        return alpha;
    }

    public Move Think(Board board, Timer timer)
    {
        myBoard = board;
        myTimer = timer;

        thinkTime = Math.Max(Math.Min(
            timer.MillisecondsRemaining / 40 + timer.IncrementMilliseconds / 2, // Spend 2.5% of remaining time per move, plus half of the increment
            timer.MillisecondsRemaining - 500),                                 // Don't spend more time than what is available (if the increment puts us above the total time left use time left - 500)
            80                                                                  // But spend at least 80 milliseconds per move
        );

        history = new int[4096];

        for (searchDepth = 1; searchDepth <= int.MaxValue; searchDepth++)
        {
            if (TimesUp)
                break;

            Search(searchDepth, -32765, 32765);
        }

        return bestMove;
    }
}