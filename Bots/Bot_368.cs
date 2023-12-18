namespace auto_Bot_368;
using ChessChallenge.API;
using System;

public class Bot_368 : IChessBot
{
    private readonly int[] pieceVal = { 0, 100, 310, 330, 500, 1000, 10000 };
    private readonly int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    private readonly ulong[] psts =
    {
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
    private int GetPstVal(int psq) => (int)(((psts[psq / 10] >> (6 * (psq % 10))) & 63) - 20) * 8;
    private const int entries = 1 << 20;
    private readonly TTEntry[] tt = new TTEntry[entries];
    private readonly Move[,] killerMoves = new Move[50, 2]; // [depth, position(0, 1)]
    private readonly int[,,] history = new int[2, 64, 64]; // [color, startSquare, endSquare]

    public Move Think(Board board, Timer timer)
    {
        //int nodes = 0;
        Move bestMoveRoot = Move.NullMove;

        int Search(int alpha, int beta, int depth, bool root)
        {
            //nodes++;

            ulong key = board.ZobristKey;
            bool qSearch = depth <= 0, inCheck = board.IsInCheck(), nonPvNode = beta - alpha <= 1;
            int best = -30000, mg = 0, eg = 0, phase = 0, index, i;

            // Check for repetition (this is much more important than material and 50 move rule draws)
            if (!root && board.IsRepeatedPosition())
                return 0;

            // TT cutoffs
            TTEntry entry = tt[key % entries];
            if (!root && entry.key == key && entry.depth >= depth && (
                entry.bound == 3 // exact score
                    || entry.bound == 2 && entry.score >= beta // lower bound, fail high
                    || entry.bound == 1 && entry.score <= alpha // upper bound, fail low
            )) return entry.score;

            // Quiescence search is in the same function as negamax to save tokens
            if (qSearch)
            {
                foreach (bool stm in new[] { true, false })
                {
                    for (int piece = 0; ++piece <= 6;)
                    {
                        ulong mask = board.GetPieceBitboard((PieceType)piece, stm);
                        while (mask != 0)
                        {
                            phase += piecePhase[piece];
                            index = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (stm ? 56 : 0);
                            mg += GetPstVal(index) + pieceVal[piece];
                            eg += GetPstVal(index + 64) + pieceVal[piece];
                        }
                    }
                    mg = -mg;
                    eg = -eg;
                }

                best = (mg * phase + eg * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
                if (best >= beta) return best;
                alpha = Math.Max(alpha, best);
            }

            // Generate moves, only captures in qSearch
            var moves = board.GetLegalMoves(qSearch);
            var scores = new int[moves.Length];

            Move bestMove = Move.NullMove, move;
            int origAlpha = alpha, score;

            // Score moves
            for (i = 0; i < moves.Length; i++)
            {
                move = moves[i];

                // 1. PV Move
                // 2. Winning Captures
                // 3. Equal Captures
                // 4. Killer Moves
                // 6. Queen Promotion
                // 5. Losing Captures
                // 7. History Heuristic
                if (move == entry.move && key == entry.key) score = 10000;
                else if (move.IsCapture) score = (move.CapturePieceType > move.MovePieceType ? 9000 : move.CapturePieceType == move.MovePieceType ? 8000 : 5000) + 10 * (int)move.MovePieceType - (int)move.CapturePieceType;
                else if (!qSearch && move == killerMoves[depth, 0]) score = 7001;
                else if (!qSearch && move == killerMoves[depth, 1]) score = 7000;
                else if (move.PromotionPieceType == PieceType.Queen) score = 6000;
                else score = history[board.IsWhiteToMove ? 1 : 0, move.StartSquare.Index, move.TargetSquare.Index];

                scores[i] = score;
            }

            // Search moves
            for (i = 0; i < moves.Length; i++)
            {
                // Out of time
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                    return 30000;

                // Incrementally sort moves
                for (int j = i; ++j < moves.Length;)
                    if (scores[j] > scores[i])
                        (scores[i], scores[j], moves[i], moves[j]) = (scores[j], scores[i], moves[j], moves[i]);

                // Make move
                move = moves[i];
                board.MakeMove(move);

                // Calculate reduction depth reduction 
                int reduction = nonPvNode && !inCheck && !board.IsInCheck() && i > 3 && depth > 3 ? 2 : 1;

                // PV Search
                if (i == 0) score = -Search(-beta, -alpha, depth - reduction, false);
                else
                {
                    score = -Search(-alpha - 1, -alpha, depth - reduction, false);
                    if (alpha < score && score < beta) score = -Search(-beta, -alpha, depth - 1, false);
                }

                // Unmake move
                board.UndoMove(move);

                // New best move
                if (score > best)
                {
                    best = score;
                    bestMove = move;

                    if (root) bestMoveRoot = move;

                    // Improve alpha
                    alpha = Math.Max(alpha, score);

                    // Fail-high
                    if (alpha >= beta)
                    {
                        if (!qSearch && !move.IsCapture)
                        {
                            // Keep best two Killer Moves
                            killerMoves[depth, 1] = killerMoves[depth, 0];
                            killerMoves[depth, 0] = move;

                            // History Heuristic
                            history[board.IsWhiteToMove ? 1 : 0, move.StartSquare.Index, move.TargetSquare.Index] = depth ^ 2;
                        }
                        break;
                    }
                }
            }

            // (Check/Stale)mate
            if (!qSearch && moves.Length == 0) return inCheck ? board.PlyCount - 30000 : 0;

            // Push to TT
            tt[key % entries] = new TTEntry(key, bestMove, depth, best, best >= beta ? 2 : best > origAlpha ? 3 : 1);

            return best;
        }

        for (int depth = 0; depth < 49; Search(-30000, 30000, ++depth, true))
            //{
            // Out of time
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;

        //DivertedConsole.Write("{0}\t{1}\t{2:0,0}\t{3:0,0}", depth, bestMoveRoot, nodes, nodes / ((double)timer.MillisecondsElapsedThisTurn / 1000));
        //}

        return bestMoveRoot.IsNull ? board.GetLegalMoves()[0] : bestMoveRoot;
    }
}
