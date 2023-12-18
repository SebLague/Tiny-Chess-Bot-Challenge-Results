namespace auto_Bot_607;
using ChessChallenge.API;
using System;

public class Bot_607 : IChessBot
{
    private const int CHECKMATE_SCORE = 100_000;
    public Board board;
    const int DEPTH = 40;
    public static ulong ENTRIES = 1 << 22;
    TTEntry[] transpositionTable = new TTEntry[ENTRIES];
    int moveEstimate = 200;
    int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };
    Move bestRootMove = Move.NullMove;

    struct TTEntry
    {
        public ulong key;
        public ushort move;
        public int eval;
        public byte depth, flag;


        public TTEntry(ulong key, ushort move, int eval, byte depth, byte flag)
        {
            this.key = key;
            this.move = move;
            this.eval = eval;
            this.depth = depth;
            this.flag = flag;
        }
    }

    public Bot_607()
    {
        scorePool = new int[80][];
        for (int i = 0; i < scorePool.Length; i++)
        {
            scorePool[i] = new int[270];
        }
    }

    public Move Think(Board board, Timer timer)
    {
        int gameLength = board.GameMoveHistory.Length;
        int movesRemaining = moveEstimate - gameLength;
        if (movesRemaining <= 0)
        {
            moveEstimate += 50;
            movesRemaining = moveEstimate - gameLength;
        }
        double timeForMove = timer.MillisecondsRemaining / movesRemaining;
        this.board = board;
        for (int i = 3; i < DEPTH; i++)
        {
            alphaBeta(int.MinValue + 1, int.MaxValue, i, true);
            if (timer.MillisecondsElapsedThisTurn >= timeForMove)
            {
                break;
            }
        }
        return bestRootMove;
    }

    int[][] scorePool;

    private int alphaBeta(int alpha, int beta, int depth, bool isFirstCall)
    {
        if (!isFirstCall)
            if (board.IsDraw() || board.IsInCheckmate())
                return Evaluate(depth);

        Move[] moves = board.GetLegalMoves(depth <= 0);
        if (moves.Length == 0)
            return Evaluate(depth);
        Move bestMove = Move.NullMove;
        int bestScore = -300_000;

        if (depth <= 0)
        {
            bestScore = Evaluate(depth);
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }

        int[] scores = scorePool[depth + 40];

        TTEntry ttEntry = transpositionTable[board.ZobristKey % ENTRIES];
        ushort moveHash;
        if (ttEntry.key == board.ZobristKey)
            moveHash = ttEntry.move;
        else
            moveHash = 0;

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            scores[i] = (move.GetHashCode() == moveHash) ? 1_000_000 : (move.IsCapture ? 100 * (move.CapturePieceType - move.MovePieceType + 30) : (int)move.MovePieceType);
        }

        for (byte i = 0; i < moves.Length; i++)
        {
            for (int j = i + 1; j < moves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);
            }

            Move move = moves[i];
            board.MakeMove(move);
            int score = -alphaBeta(-beta, -alpha, depth - 1, false);
            board.UndoMove(move);

            if (score >= beta)
            {
                bestMove = move;
                bestScore = beta;
                break;
            }

            if (score > bestScore)
            {
                bestScore = score;
                alpha = Math.Max(score, alpha);
                bestMove = move;
                if (alpha == CHECKMATE_SCORE)
                {
                    break;
                }
            }
        }
        transpositionTable[board.ZobristKey % ENTRIES] = new TTEntry(board.ZobristKey, bestMove.RawValue, bestScore, (byte)depth, 0);
        if (isFirstCall)
        {
            bestRootMove = bestMove;
        }
        return bestScore;
    }

    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    }

    public int Evaluate(int depthLeft)
    {
        if (board.IsInCheckmate())
        {
            return (CHECKMATE_SCORE + depthLeft) * -1;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        int mg = 0, eg = 0, phase = 0;

        bool stm = true;
        do
        {
            for (var p = PieceType.Pawn; p <= PieceType.King; p++)
            {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, stm);
                while (mask != 0)
                {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                    mg += getPstVal(ind) + pieceVal[piece];
                    eg += getPstVal(ind + 64) + pieceVal[piece];
                }
            }

            mg = -mg;
            eg = -eg;
            stm = !stm;
        } while (!stm);

        return (board.IsWhiteToMove ? 1 : -1) * (mg * phase + eg * (24 - phase)) / 24;
    }
}