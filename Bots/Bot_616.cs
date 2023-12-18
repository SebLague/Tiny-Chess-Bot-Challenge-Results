namespace auto_Bot_616;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_616 : IChessBot
{
    // Piece values: pawn, knight, bishop, rook, queen, king
    // Piece values: pawn, knight, bishop, rook, queen, king
    int[] pieceVal = { 0, 88, 309, 331, 495, 981, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psts = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    int MaxDepth = 4;
    int MinDepth = 3;
    int MaxQui = 5;
    int TimeLimitInMilliseconds = 5000; // 5 secs

    // Token saving
    int maxValue = int.MaxValue;
    int minValue = int.MinValue + 1;

    public Move Think(Board board, Timer timer)
    {
        if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 7)
        {
            MaxDepth = 6;
        }
        if (timer.MillisecondsRemaining < 10000)
        {
            MaxDepth = 4;
        }

        return IDAS(board, timer).Item1;
    }


    // Iterative Deepening Aspiration Search
    public Tuple<Move, int, int> IDAS(Board board, Timer timer)
    {
        int threshold = 30; // 30 centipawns
        Tuple<Move, int> moveAndScore;

        int depth, depthSearched = 0;

        moveAndScore = RootAlphaBeta(board, MinDepth, minValue, maxValue);
        int best = moveAndScore.Item2;
        for (depth = MinDepth + 1; depth <= MaxDepth; depth++)
        {
            moveAndScore = RootAlphaBeta(board, depth, best - threshold, best + threshold);
            if (moveAndScore.Item2 <= (best - threshold)) // failed low
            {
                moveAndScore = RootAlphaBeta(board, depth, minValue, moveAndScore.Item2);
            }
            else if (moveAndScore.Item2 >= (best + threshold)) // failed high
            {
                moveAndScore = RootAlphaBeta(board, depth, moveAndScore.Item2, maxValue);
            }
            best = moveAndScore.Item2;
            depthSearched = depth;
            if (timer.MillisecondsElapsedThisTurn >= TimeLimitInMilliseconds) break;
        }

        return new Tuple<Move, int, int>(moveAndScore.Item1, depthSearched, best);
    }


    public Tuple<Move, int> RootAlphaBeta(Board board, int depth, int alpha, int beta)
    {
        Move[] moves = OrderedMoves(board);

        Move bestMove = moves[0];
        int maxScore = minValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int score = -AlphaBeta(board, -beta, -alpha, (byte)(depth - 1));
            if (score > maxScore)
            {
                maxScore = score;
                bestMove = move;
            }
            board.UndoMove(move);
        }

        return new Tuple<Move, int>(bestMove, maxScore);
    }

    public int AlphaBeta(Board board, int alpha, int beta, byte depthLeft)
    {
        if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsInCheckmate())
        {
            return minValue;
        }
        if (depthLeft == 0)
        {
            return Quiescence_Limited(board, alpha, beta, MaxQui);
        }

        Move[] moves = OrderedMoves(board);

        int bestScore = minValue + depthLeft + 1;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            int score = -AlphaBeta(board, -beta, -alpha, (byte)(depthLeft - 1));
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
            }
            if (bestScore > alpha)
            {
                alpha = bestScore;
            }
            if (bestScore >= beta)
            {
                break;
            }
        }

        return bestScore;
    }

    private int Quiescence_Limited(Board board, int alpha, int beta, int depthLeft)
    {
        if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsInCheckmate())
        {
            return minValue;
        }

        int standingPat = Evaluate(board);

        if (depthLeft == 0)
        {
            return standingPat;
        }

        if (standingPat >= beta)
        {
            return beta;
        }

        if (alpha < standingPat)
        {
            alpha = standingPat;
        }

        Move[] moves = OrderedMoves(board, true);
        int score;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Quiescence_Limited(board, -beta, -alpha, depthLeft - 1);
            board.UndoMove(move);

            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
            }
        }
        return alpha;
    }

    Move[] OrderedMoves(Board board, bool capturesOnly = false)
    {
        if (board.IsDraw() || board.IsInCheckmate())
        {
            return new Move[0];
        }
        Move[] captures = board.GetLegalMoves(true);
        captures.OrderByDescending(item => item.CapturePieceType - item.MovePieceType).ToArray();
        if (capturesOnly)
        {
            return captures;
        }

        Move[] nonCaptures = board.GetLegalMoves().Where(item => !item.IsCapture).ToArray();
        nonCaptures.OrderByDescending(item => item.IsPromotion || item.IsCastles).ToArray(); ;

        return captures.Concat(nonCaptures).ToArray();
    }

    // Evaluation function from https://github.com/jw1912/Chess-Challenge. My evaluation function also used compressed PESTO PSTs, but jw1912's code achomplished the same in much fewer tokens. Thanks!
    public int getPstVal(int psq)
    {
        return (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
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
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm == true ? 56 : 0);
                    mg += getPstVal(ind) + pieceVal[piece];
                    eg += getPstVal(ind + 64) + pieceVal[piece];
                }
            }
            mg = -mg;
            eg = -eg;
        }

        int eval = (mg * phase + eg * (24 - phase)) / 24;

        if (!board.IsWhiteToMove)
            eval *= -1;


        return eval;
    }

}