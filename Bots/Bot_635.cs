namespace auto_Bot_635;
// NOTE:
// Credit to AugsEU for the Evaluation function, the Search algorithm logic and the transposition table implementation

using ChessChallenge.API;
using System;

public class Bot_635 : IChessBot
{

    int[] pieceValues = { 0, 100, 350, 360, 550, 950, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    int maxDepth = 50;
    Move bestmoveRoot = Move.NullMove;
    int[,] historyTable = new int[12, 64];


    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, bound;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
        {
            key = _key; move = _move; depth = _depth; score = _score; bound = _bound;
        }
    }

    const int entries = (1 << 20);
    TTEntry[] tt = new TTEntry[entries];

    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }


    // Negamax algorithm , with alpha beta pruning, to search game tree for best move
    int Search(Board board, Timer timer, int depth, int alpha, int beta, int ply)
    {
        ulong key = board.ZobristKey;
        bool qsearch = depth <= 0;
        bool notRoot = ply > 0;
        int best = -100000;


        if (notRoot && board.IsRepeatedPosition())
            return 0;

        TTEntry entry = tt[key % entries];

        // TT cutoffs
        if (notRoot && entry.key == key && entry.depth >= depth && (
            entry.bound == 3 // exact score
                || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
        )) return entry.score;


        int eval = Evaluate(board);

        if (qsearch)
        {
            best = eval;
            if (best >= beta) return best;
            alpha = Math.Max(alpha, best);
        }


        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, qsearch);

        if (!qsearch && moves.Length == 0)
        {
            return board.IsInCheck() ? -100000 : 0;
        }

        moves.Sort((move1, move2) =>
        {
            int maxOrMin = board.IsWhiteToMove ? 1 : -1;
            return moveScoreGuess(board, move2).CompareTo(moveScoreGuess(board, move1)) * maxOrMin;
        });

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;


        foreach (Move move in moves)
        {
            if (timer.MillisecondsRemaining < 10000)
                maxDepth = 10;

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                return 100000;

            board.MakeMove(move);
            int score = -Search(board, timer, depth - 1, -beta, -alpha, ply + 1);
            board.UndoMove(move);


            if (score > best)
            {
                best = score;
                bestMove = move;
                updateHistoryHeuristic(board, move, depth);

                if (ply == 0) bestmoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;

            }

            int bound = best >= beta ? 2 : best > origAlpha ? 3 : 1;

            // Push to Transposition Table
            tt[key % entries] = new TTEntry(key, bestMove, depth, best, bound);

        }
        return best;
    }


    // Method to get the value of a piece based on its type
    int GetPieceValue(PieceType pieceType)
    {
        return pieceValues[(int)pieceType];
    }


    // Method to determine the score of a move based on the piece type, whether it is a capture, and whether it puts the opponent in check
    int moveScoreGuess(Board board, Move move)
    {
        ulong attacksBitboard = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        bool checkMove = BitboardHelper.SquareIsSet(attacksBitboard, board.GetKingSquare(!board.IsWhiteToMove));
        bool squareAttacked = board.SquareIsAttackedByOpponent(move.TargetSquare);
        int maxOrMin = board.IsWhiteToMove ? 1 : -1;

        int scoreGuess = getHistoryHeuristic(board, move);


        if (move.IsCapture)
        {
            scoreGuess += GetPieceValue(move.CapturePieceType) - GetPieceValue(move.MovePieceType);
        }

        if (move.IsPromotion)
        {
            scoreGuess += GetPieceValue(move.PromotionPieceType) - GetPieceValue(move.MovePieceType);
        }

        if (squareAttacked)
        {
            scoreGuess -= 100;
        }

        if (checkMove)
        {
            scoreGuess += 50;
        }

        return scoreGuess * maxOrMin;
    }



    // Update the score of a move in the history table
    void updateHistoryHeuristic(Board board, Move move, int depth)
    {
        int pieceTypeIndex = (int)move.MovePieceType - 1 + (board.IsWhiteToMove ? 0 : 6);
        int targetSquareIndex = move.TargetSquare.Index;
        int maxOrMin = board.IsWhiteToMove ? 1 : -1;

        historyTable[pieceTypeIndex, targetSquareIndex] += depth * depth * maxOrMin;

    }

    // Get the score of a move in the history table
    int getHistoryHeuristic(Board board, Move move)
    {
        int pieceTypeIndex = (int)move.MovePieceType - 1 + (board.IsWhiteToMove ? 0 : 6);
        int targetSquareIndex = move.TargetSquare.Index;
        return historyTable[pieceTypeIndex, targetSquareIndex];
    }

    public int Evaluate(Board board)
    {
        int mg = 0, eg = 0, phase = 0;

        foreach (bool stm in new[] { true, false })
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += getPstVal(ind) + pieceValues[piece];
                    eg += getPstVal(ind + 64) + pieceValues[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }

        return (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    public Move Think(Board board, Timer timer)
    {
        bestmoveRoot = Move.NullMove;
        // Iterative deepening
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            int score = Search(board, timer, depth, -100000, 100000, 0);

            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }
        return bestmoveRoot.IsNull ? board.GetLegalMoves()[0] : bestmoveRoot;
    }
}

