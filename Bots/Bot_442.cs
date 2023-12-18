namespace auto_Bot_442;
using ChessChallenge.API;
using System;

public class Bot_442 : IChessBot
{
    (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[1048576];

    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    int[] pieceValue = { 0, 100, 310, 330, 500, 1000, 10000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    ulong[] psqt = { 657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902 };

    public Move Think(Board board, Timer timer)
    {
        bool stopSearch = false;

        int[] history = new int[4096];
        Move[] killer = new Move[256];

        for (int depth = 1; !stopSearch && depth < 256; depth++)
            Search(-30000, 30000, depth, 0, true);

        var (_, ttMove, _, _, _) = tt[board.ZobristKey % 1048576];
        return ttMove;

        int Search(int alpha, int beta, int depth, int ply, bool doNull)
        {
            if (stopSearch
                || (depth > 2 && timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 50))
            {
                stopSearch = true;
                return 0;
            }

            // Check for checkmate
            if (board.IsInCheckmate())
                return -30000 + ply;

            // Check for draw
            if (board.IsDraw()
                || (ply > 0 && board.IsRepeatedPosition()))
                return 0;

            if (depth < 0)
                depth = 0;

            ulong key = board.ZobristKey;
            var (ttKey, ttMove, ttDepth, ttValue, ttFlag) = tt[key % 1048576];

            // TT Cutoffs
            if (beta - alpha == 1
                && ttKey == key
                && ttDepth >= depth
                && (ttValue >= beta ? ttFlag > 0 : ttFlag < 2))
                return ttValue;

            // Evaluate position
            int mg = 0, eg = 0, phase = 0;
            foreach (bool stm in new[] { true, false })
            {
                for (var p = PieceType.Pawn; p <= PieceType.King; p++)
                {
                    int piece = (int)p, i;
                    ulong mask = board.GetPieceBitboard(p, stm);
                    while (mask != 0)
                    {
                        phase += piecePhase[piece];
                        i = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                        mg += getPsqtValue(i) + pieceValue[piece];
                        eg += getPsqtValue(i + 64) + pieceValue[piece];
                    }
                }

                mg = -mg;
                eg = -eg;
            }
            int eval = (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);

            // Reverse Futility Pruning
            if ((depth == 0 || !board.IsInCheck())
                && depth < 9
                && eval >= beta + 120 * depth)
                return eval - 120 * depth;

            if (doNull
                && beta - alpha == 1
                && depth != 0
                && eval >= beta
                && ttValue >= eval
                && board.TrySkipTurn())
            {
                int nullValue = -Search(-beta, -beta + 1, depth - 3, ply + 1, false);
                board.UndoSkipTurn();
                if (nullValue >= beta)
                    return nullValue;
            }

            // Stand Pat
            if (depth == 0)
                ttValue = eval;
            else
                ttValue = -30000;

            if (ttValue > alpha)
                alpha = ttValue;

            // Generate moves
            var moves = board.GetLegalMoves(depth == 0);

            // Score moves
            int moveIdx = 0;
            var scores = new int[moves.Length];
            foreach (Move move in moves)
                scores[moveIdx++] = -(move == ttMove ? 400000 : move.IsCapture ? 200000 : move == killer[ply] ? 100000 : history[move.RawValue & 4095]);
            Array.Sort(scores, moves);

            ttFlag = 0;
            int moveCount = 0, value;
            foreach (Move move in moves)
            {
                board.MakeMove(move);

                // Principal Variation Search and Late Move Reduction
                if (moveCount++ == 0
                    || (value = -Search(-alpha - 1, -alpha, depth - ((moveCount > 3 && depth > 1 && !move.IsCapture) ? 2 : 1), ply + 1, true)) > alpha)
                    value = -Search(-beta, -alpha, depth - 1, ply + 1, true);

                board.UndoMove(move);

                if (stopSearch)
                    break;

                if (value > ttValue)
                {
                    ttValue = value;

                    if (value > alpha)
                    {
                        alpha = value;
                        ttMove = move;
                        ttFlag = 1;

                        if (alpha >= beta)
                        {
                            if (!move.IsCapture)
                            {
                                killer[ply] = move;
                                history[move.RawValue & 4095] += depth;
                            }

                            ttFlag++;
                            break;
                        }
                    }
                }
            }

            tt[key % 1048576] = (key, ttMove, depth, ttValue, ttFlag);

            return ttValue;
        }
    }

    int getPsqtValue(int i)
    {
        return (int)(((psqt[i / 10] >> (6 * (i % 10))) & 63) - 20) * 8;
    }
}