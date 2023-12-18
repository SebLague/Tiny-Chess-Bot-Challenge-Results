namespace auto_Bot_529;
using ChessChallenge.API;
using System;

public class Bot_529 : IChessBot
{
    readonly (ulong, Move, int, int, byte)[] tt = new (ulong, Move, int, int, byte)[1048576];

    public Move Think(Board board, Timer timer)
    {
        Move bestMoveRoot = default;
        var killers = new Move[128];
        var history = new int[4096];
        int iterDepth = 1;

        while (iterDepth < 64 && timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 30)
            Search(-30000, 30000, iterDepth++, 0);

        return bestMoveRoot;

        int Search(int alpha, int beta, int depth, int ply)
        {
            bool inCheck = board.IsInCheck();

            // Check extensions
            if (inCheck)
                depth++;

            bool qs = depth <= 0;
            ulong key = board.ZobristKey;
            var (ttKey, ttMove, ttDepth, score, ttFlag) = tt[key % 1048576];
            int bestScore = -30000, moveIdx = 0;

            // Check for draw by repetition
            if (ply > 0
                && board.IsRepeatedPosition())
                return 0;

            // Stand Pat
            if (qs
                && (bestScore = alpha = Math.Max(alpha, Evaluate())) >= beta)
                return alpha;

            // TT Cutoffs
            if (beta - alpha == 1
                && ttKey == key
                && ttDepth >= depth
                && (score >= beta ? ttFlag > 0 : ttFlag < 2))
                return score;

            // Reverse Futility Pruning
            if (!qs
                && !inCheck
                && depth <= 8
                && Evaluate() >= beta + 120 * depth)
                return beta;

            // Generate moves
            var moves = board.GetLegalMoves(qs);

            // Checkmate/Stalemate
            if (moves.Length == 0)
                return qs ? alpha : inCheck ? ply - 30_000 : 0;

            // Score moves
            var scores = new int[moves.Length];
            foreach (Move move in moves)
                scores[moveIdx++] = -(
                    move == ttMove
                        ? 900_000_000
                        : move.IsCapture
                            ? 100_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType
                            : move == killers[ply]
                                ? 80_000_000
                                : history[move.RawValue & 4095]
                );

            Array.Sort(scores, moves);

            ttMove = default;
            moveIdx = ttFlag = 0;

            foreach (Move move in moves)
            {
                if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 15)
                    return 30000;

                board.MakeMove(move);

                // Principal Variation Search + Late Move Reductions
                if (moveIdx++ == 0
                    || qs
                    || depth < 2
                    || move.IsCapture
                    || (score = -Search(-alpha - 1, -alpha, depth - 2 - moveIdx / 16, ply + 1)) > alpha)
                    score = -Search(-beta, -alpha, depth - 1, ply + 1);

                board.UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    ttMove = move;
                    if (score > alpha)
                    {
                        alpha = score;
                        ttFlag = 1;

                        if (ply == 0)
                            bestMoveRoot = move;

                        if (alpha >= beta)
                        {
                            // Quiet cutoffs update tables
                            if (!move.IsCapture)
                            {
                                killers[ply] = move;
                                history[move.RawValue & 4095] += depth;
                            }

                            ttFlag++;

                            break;
                        }
                    }
                }
            }

            tt[key % 1048576] = (key, ttMove, depth, bestScore, ttFlag);

            return bestScore;
        }

        int Evaluate()
        {
            var accumulators = new int[2, 8];
            int mat = 0, bucket = -2;

            // Adds a feature (colour, piece, square) to given accumulator
            void updateAccumulator(int side, int feature)
            {
                for (int i = 0; i < 8;)
                    accumulators[side, i] += weights[feature * 8 + i++];
            }

            // Initialise with input biases
            updateAccumulator(0, 768);
            updateAccumulator(1, 768);

            for (int stm = 768; (stm -= 384) >= 0;)
            {
                for (var p = 0; p <= 5; p++)
                    for (ulong mask = board.GetPieceBitboard((PieceType)p + 1, stm > 0); mask != 0;)
                    {
                        mat += (int)(0x3847D12C4B064 >> 10 * p & 0x3FF);
                        bucket++;
                        int sq = BitboardHelper.ClearAndGetIndexOfLSB(ref mask);

                        // Add feature from each perspective
                        updateAccumulator(0, 384 - stm + p * 64 + sq);
                        updateAccumulator(1, stm + p * 64 + sq ^ 56);
                    }
                mat = -mat;
            }

            bucket /= 4;

            // Initialise with output bias
            int eval = 8 * raw[1672 + bucket];

            // Compute hidden -> output layer
            for (int i = 0; i < 16;)
                eval += Math.Clamp(accumulators[i / 8 ^ (board.IsWhiteToMove ? 0 : 1), i % 8], 0, 32) * raw[1544 + 16 * bucket + i++];

            // Scale + Material Factoriser
            return eval * 400 / 1024 + (board.IsWhiteToMove ? mat : -mat);
        }
    }

    readonly int[] weights = new int[6152];
    readonly int[] raw = new int[1680];

    public Bot_529()
    {
        int i;
        for (i = 0; i < 1680;)
        {
            var packed = decimal.GetBits(packedWeights[i / 16]);
            int num = i % 16 * 6;
            uint adj = (uint)packed[num / 32] >> num % 32 & 63;
            if (num == 30) adj += ((uint)packed[1] & 15) << 2;
            if (num == 60) adj += ((uint)packed[2] & 3) << 4;
            raw[i++] = (int)adj - 31;
        }

        for (i = 0; i < 6144;)
        {
            int sq = i / 8 % 64, pc = i / 512 * 128 + i % 8;
            weights[i++] = raw[pc + sq / 8 * 8] + raw[pc + 64 + sq % 8 * 8];
        }

        for (; i < 6152;)
            weights[i] = raw[i++ - 4608];
    }

    readonly decimal[] packedWeights = {
        37747653452566649643112200159m,40223533531119395795492272480m,36529664745230695484962498788m,38985286316542633163201559024m,
        38985295985106598109212715174m,38946912517662914348042283175m,36491272572183396258467653797m,32835797073131705103024765476m,
        32854243220785540026070853024m,29179712968580801034347984547m,32931911610134636654980605861m,30338489770686330769862403942m,
        34092490140321793794559567652m,30417350850180248481830385445m,30417653007847885573216118309m,30378079431469935389488559781m,
        32912569094329608617403729252m,30456036327645009463511197092m,30455729226267957424288278118m,27921211375468860013618767524m,
        31693674284166331378364175908m,30456041123744425432466171301m,30456343502809024535786526181m,30456031755176046322343319012m,
        26684509342306577591239571435m,25485557233320086643572602861m,25504904695049456221797611501m,25524847101149836653996144685m,
        27942698739172978852429690796m,26782125376487555259915692141m,24247607673262969198062455021m,25445667403639282418961205229m,
        16995979837228230366259918514m,13301502385994229316153059125m,13358925845899911503701943738m,8465797041554692508998518393m,
        15758946271974847145378399478m,15758341881806899673467795127m,14558482859454369201073764150m,16993863922977976472769742390m,
        37766386785038708462021624987m,38927267323141192925855746014m,38985286094209058421285452127m,31557610736976084312536504860m,
        35309547357410518802190149404m,38965962395254748578188482655m,40204195222396982536983414879m,42776463445949534612135737375m,
        47495259823828249531830368223m,41441818714203281256922095647m,41441814137266738764187301600m,38985286316542782417481627551m,
        45116348741624901621440963551m,46258183901752308854824244064m,42525918219260313199796425567m,41443329941643020071853228893m,
        45114841501130143488535566300m,47552338555051126503810431068m,45116041421604578846415407257m,38983765124381909900152408473m,
        42658894895806186162270631902m,46353377073965443271595276250m,46334648094814938202127091929m,41402240046829644290269002202m,
        38944767458615369558953174807m,43877496776303268980199421721m,40125293408959924711446177688m,35250899630839069592040388503m,
        38905784545795402365035497240m,41381967004565682317317269272m,40144631427073220979818649495m,38887343777314141395222419414m,
        52446041348229510066592966229m,50009149419048194965464307350m,50009149491663949555014440532m,47552602559049993130785773205m,
        50027887769251650175637530259m,51208101675590619910330791637m,52427308161960058093334582997m,51305722289067731609430677141m,
        68459529139765064009342462158m,68421136520449335297223624780m,62231436395502995394628348106m,56060483916731290586409781194m,
        58555697291085161888792881100m,64726361925672827280515057740m,65945275403978270817109858443m,64726380523468490112266594316m,
        40223230793709562254239112605m,41441814063605867518515903198m,39004615104276150816811890655m,32834285762283599487748392478m,
        40186373011307798078995175578m,41539434968771821023218235551m,39023665061583105897233508448m,37745547129360348775237154783m,
        18742957259898249745876947m,578060029656081477065113600m,1042084174529075921941626944m,1179301751213326758401520640m,
        4912752746535941460973053830m,12281746060469715462392899535m,19630475849706118524860098581m,30712637518828842268765701974m,
        47768146784521744342789384029m,
    };
}