namespace auto_Bot_46;
using ChessChallenge.API;



struct TTEntry
{
    public ulong key;
    public Move move;
    public int depth, score, bound;

    public TTEntry(ulong _key, Move _move, int _depth, int _score, int _bound)
    {
        key = _key;
        move = _move;
        depth = _depth;
        score = _score;
        bound = _bound;
    }
}

class Bot_46 : IChessBot
{
    const int TranspositionTableSize = 1048576;
    const int MaxDepth = 32;

    private static readonly int[] pieceValues = { 0, 100, 310, 330, 500, 1000, 10000 };
    private static readonly int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    private static readonly ulong[] pieceSquareTables = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    private TTEntry[] transpositionTable = new TTEntry[TranspositionTableSize];
    private int nodesVisited;
    private Move bestMoveRoot;


    public Move Think(Board board, Timer timer)
    {
        nodesVisited = 0;
        Move bestMove = Move.NullMove;
        int minDepth = 1;
        if (timer.MillisecondsRemaining > 30000)
        {
            minDepth = 2;

        }
        for (int depth = minDepth; depth <= MaxDepth; depth++)
        {
            int score = Search(board, timer, -30000, 30000, depth, 0);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
            bestMove = bestMoveRoot;
        }
        return bestMove;
    }

    private int GetPieceSquareValue(int square)
    {
        return (int)(((pieceSquareTables[square / 10] >> (6 * (square % 10))) & 63) - 20) * 8;
    }

    private int Evaluate(Board board)
    {
        int mgScore = 0, egScore = 0, phase = 0;

        foreach (bool isWhite in new[] { true, false })
        {
            for (var pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int piece = (int)pieceType;
                ulong pieceMask = board.GetPieceBitboard(pieceType, isWhite);

                while (pieceMask != 0)
                {
                    phase += piecePhase[piece];
                    int squareIndex = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref pieceMask) ^ (isWhite ? 56 : 0);
                    mgScore += GetPieceSquareValue(squareIndex) + pieceValues[piece];
                    egScore += GetPieceSquareValue(squareIndex + 64) + pieceValues[piece];
                }
            }

            mgScore = -mgScore;
            egScore = -egScore;
        }

        int eval = (mgScore * phase + egScore * (24 - phase)) / 24;

        if (!board.IsWhiteToMove)
            eval *= -1;

        return eval;
    }

    private int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {
        nodesVisited++;
        ulong key = board.ZobristKey;
        bool qSearch = (depth <= 0);
        int bestScore = -30000;

        if (ply > 0)
        {
            for (int i = board.PlyCount - 2; i >= 0; i -= 2)
            {
                if (transpositionTable[i % TranspositionTableSize].key == key)
                    return 0;
            }
        }

        TTEntry entry = transpositionTable[key % TranspositionTableSize];

        if (ply > 0 && entry.key == key && entry.depth >= depth &&
            (entry.bound == 3 || (entry.bound == 2 && entry.score >= beta) || (entry.bound == 1 && entry.score <= alpha)))
            return entry.score;

        if (qSearch)
        {
            bestScore = Evaluate(board);
            if (bestScore >= beta) return bestScore;
            if (bestScore > alpha) alpha = bestScore;
        }

        Move[] moves = board.GetLegalMoves(qSearch);
        int[] scores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == entry.move) scores[i] = 1000000;
            else if (moves[i].IsCapture) scores[i] = 100 * moves[i].TargetSquare.Index - moves[i].StartSquare.Index;
        }

        Move bestMove = Move.NullMove;
        int origAlpha = alpha;

        for (int i = 0; i < moves.Length; i++)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 0;

            int ind = i;
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[ind]) ind = j;
            }
            (scores[i], scores[ind]) = (scores[ind], scores[i]);
            (moves[i], moves[ind]) = (moves[ind], moves[i]);

            Move move = moves[i];
            board.MakeMove(move);
            int score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (ply == 0)
                    bestMoveRoot = move;
                if (score > alpha)
                {
                    alpha = score;
                    if (alpha >= beta) break;
                }
            }
        }

        if (!qSearch && moves.Length == 0)
        {
            if (board.IsInCheck()) return -30000 + ply;
            else return 0;
        }

        transpositionTable[key % TranspositionTableSize] = new TTEntry(key, bestMove, depth, bestScore, bestScore >= beta ? 2 : bestScore > origAlpha ? 3 : 1);
        return bestScore;
    }

}