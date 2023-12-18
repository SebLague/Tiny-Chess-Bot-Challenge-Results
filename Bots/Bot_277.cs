namespace auto_Bot_277;
/* ZambroniBot - V.4.1.4: 8/19/23 */

// ZambroniBot is a simple negamax-based chess bot.
// It was created for the "Tiny Chess Bots" Competition by Sebastian Lague.
// Credits to JW for a base.

using ChessChallenge.API;
using System;

public class Bot_277 : IChessBot
{
    // Constants for better code readability
    private const int MaxDepth = 50;
    private const int MaxTableEntries = 1 << 20;

    // Initialize the best move root to null
    Move bestMoveRoot = Move.NullMove;

    // Piece values for evaluation
    int[] pieceValues = { 0, 100, 300, 350, 500, 1000, 10000 };

    // Phase values for evaluation
    int[] piecePhases = { 0, 0, 1, 1, 2, 4, 0 };

    // Piece-square tables for evaluation
    ulong[] pieceSquareTables = {
        657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588,
        421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453,
        347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460,
        257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824,
        365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
        347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716,
        366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844,
        329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224,
        366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612,
        401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902
    };


    // Structure to hold transposition table entry
    struct TranspositionTableEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TranspositionTableEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int tableEntries = (MaxTableEntries);
    TranspositionTableEntry[] transpositionTable = new TranspositionTableEntry[tableEntries];

    // Get piece-square table value for a given square index
    public int GetPieceSquareTableValue(int squareIndex)
    {
        return (int)(((pieceSquareTables[squareIndex / 10] >> (6 * (squareIndex % 10))) & 63) - 20) * 8;
    }

    // Evaluate the position using piece-square tables and piece values
    public int EvaluatePosition(Board board)
    {
        int middleGameScore = 0, endGameScore = 0, totalPhase = 0;
        bool isWhiteToMove = board.IsWhiteToMove;

        foreach (bool isWhite in new[] { true, false })
        {
            // Iterate through each piece type
            for (var pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int pieceIndex = (int)pieceType, squareIndex;
                ulong pieceMask = board.GetPieceBitboard(pieceType, isWhite);

                // Evaluate pieces on the board
                while (pieceMask != 0)
                {
                    totalPhase += piecePhases[pieceIndex];
                    squareIndex = 128 * (pieceIndex - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref pieceMask) ^ (isWhite ? 56 : 0);
                    middleGameScore += GetPieceSquareTableValue(squareIndex) + pieceValues[pieceIndex];
                    endGameScore += GetPieceSquareTableValue(squareIndex + 64) + pieceValues[pieceIndex];
                }
            }

            // Reverse material gain values for the opponent
            middleGameScore = -middleGameScore;
            endGameScore = -endGameScore;
        }

        // Calculate the weighted evaluation
        return (middleGameScore * totalPhase + endGameScore * (24 - totalPhase)) / 24 * (isWhiteToMove ? 1 : -1);
    }

    // Alpha-beta search function
    public int PerformSearch(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {

        ulong key = board.ZobristKey;
        int bestScore = -30000;
        bool inCheck = board.IsInCheck();
        bool isPV = beta - alpha > 1;
        bool isNotRoot = ply > 0;
        bool isQuiescenceSearch = depth <= 0;

        int eval = EvaluatePosition(board);

        // Check if the condition is met
        /*if (beta - alpha == 1 && !board.IsInCheck() && !isQuiescenceSearch && ply > 2 + depth * depth)
            return alpha; // Return alpha to break out of the search*/

        if (board.IsDraw()) return 0;

        // Extensions??
        if (board.IsInCheck() && !isQuiescenceSearch) depth++;

        // Check for repeated position to end the search
        if (!isNotRoot && board.IsRepeatedPosition()) return 0;

        TranspositionTableEntry entry = transpositionTable[key % tableEntries];

        // Use transposition table to retrieve stored information
        if (isNotRoot && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 || entry.bound == 2 && entry.score >= beta || entry.bound == 1 && entry.score <= alpha
        )) return entry.score;

        int evaluation = EvaluatePosition(board);

        // Perform quiescence search if depth is 0 or negative
        if (isQuiescenceSearch)
        {
            bestScore = evaluation;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }
        else if (!isPV && !board.IsInCheck())
        {
            //Reverse futility pruning
            if (eval - 100 * depth >= beta)
                return eval - 100 * depth;
        }

        // Generate legal moves and score captures
        Move[] legalMoves = board.GetLegalMoves(isQuiescenceSearch);
        int[] moveScores = new int[legalMoves.Length];

        // Assign scores to moves based on their type and value
        for (int i = 0; i < legalMoves.Length; i++)
        {
            Move move = legalMoves[i];
            if (move == entry.move) moveScores[i] = 1000000;
            // https://www.chessprogramming.org/MVV-LVA
            else if (move.IsCapture) moveScores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        Move bestMove = Move.NullMove;
        int originalAlpha = alpha;


        int legalMovesLength = legalMoves.Length; // Calculate the length once

        // Iterate through the moves and search deeper
        for (int i = 0; i < legalMovesLength; i++)
        {
            // Check if time is running out
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 30000;

            // Sort moves based on their scores
            for (int j = i + 1; j < legalMovesLength; j++)
            {
                if (moveScores[j] > moveScores[i])
                    (moveScores[i], moveScores[j], legalMoves[i], legalMoves[j]) = (moveScores[j], moveScores[i], legalMoves[j], legalMoves[i]);
            }

            Move move = legalMoves[i];
            board.MakeMove(move);
            int score = -PerformSearch(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // Update best move and alpha
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (ply == 0) bestMoveRoot = move;

                alpha = Math.Max(alpha, score);

                // Prune the search tree using alpha-beta
                if (alpha >= beta) break;
            }
        }

        // Handle terminal nodes and store information in transposition table
        if (!isQuiescenceSearch && legalMoves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        int bound = bestScore >= beta ? 2 : bestScore > originalAlpha ? 3 : 1;

        // Store the evaluated result in the transposition table
        transpositionTable[key % tableEntries] = new TranspositionTableEntry(key, bestMove, depth, bestScore, bound);
        return bestScore;
    }

    // Main thinking function that performs iterative deepening
    public Move Think(Board board, Timer timer)
    {
        bestMoveRoot = Move.NullMove;
        for (int depth = 1; depth <= MaxDepth; depth++)
        {
            int score = PerformSearch(board, timer, -30000, 30000, depth, 0);

            // Check if time is running out
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }
        return bestMoveRoot.IsNull ? board.GetLegalMoves()[0] : bestMoveRoot;
    }
}



