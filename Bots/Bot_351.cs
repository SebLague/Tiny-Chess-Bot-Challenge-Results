namespace auto_Bot_351;
using ChessChallenge.API;
using System;
using System.Linq;

using static ChessChallenge.API.BitboardHelper;

public class Bot_351 : IChessBot
{
    // ulong: 8, Move: 4, short: 2, sbyte: 1, byte: 1 bytes
    // should be 16 bytes, hard to tell with .NET
    record struct TTE(ulong key, Move move, short eval, sbyte depth, byte bound);

    // this should be 128MB, the other half is dedicated for the rest of the tables
    TTE[] tt = new TTE[0x800000];

    Move[] killers = new Move[256];

    static int[] basePST = {
        47,  86,
        201, 182,
        283, 270,
        406, 492,
        975, 893,
        -65, -74,
    };

    int[] PST = new[] {
        2518230907411499430950668323m,
        44104817447994687861312129059m,
        33475638175524089440063560551m,
        28491180915067266709640600893m,
        3770669036440616476500568085m,
        366328689812260096848959796m,
        4679808682653950830352859444m,
        27839605021991361467449887m,
        2547306241859477457130886400m,
        2518230907411499430831786569m,
        18935598730236055741918283811m,
        35231091217440381068596838m,
        23141685992117080226533427775m,
        33667893085428967300325133199m,
        31752826233196422755774391004m,
        25259804217024487879996176795m,
        36088190747705062122194751867m,
        35458259818283878227642443677m,
        24549927773187713400112246171m,
        17163481017160369788349143420m,
        25199215607510969972184794655m,
        5934692406808507467059769717m,
        7236720714431026198848476217m,
        4065727991516032445150732864m,
        10372703751632514300283133250m,
        11301224599790125318132472695m,
        9399498821264653291282180471m,
        5674812833859740508439193695m,
        9416513877218565455783989074m,
        8145838428349784077918540900m,
        5666255109913534143633235801m,
        3168699259294918239332402244m,
        10000387969999768201716638055m,
        9754980658601290049575328870m,
        8465027312011505315070218647m,
        5366494984994456643465583457m,
        6627436989171256437159761967m,
        7512398359518858054917624639m,
        6245386822270288817618423632m,
        1283937106910099571561860919m,
        3503520264149332800969313819m,
        5986710602927927917807667521m,
        20177132895669641023775969111m,
        19612655577073026960749117775m,
        21174813322821574115971504666m,
        28542985219163052468307773774m,
        20144548589149072937257163343m,
        24511444120778358311430013730m,
        23886306691005801935138199849m,
        15223068538434023927934046773m,
        6240574530722321355569902637m,
        3466024249856971086371228989m,
        3434738349104464876386912817m,
        17391872790520381943391983379m,
        28240801671042942327347691273m,
        26350205740019106645368526893m,
        36914994640912599998390686776m,
        31304388949624233158660290135m,
        21741586693177144644649640995m,
        19514873133928841407480094502m,
        27896421751388945212358473523m,
        26926774180886658226321772850m,
        12501601996796130962567157782m,
        9689778208629539986219939149m,
    }.SelectMany(v => decimal.GetBits(v).SelectMany(BitConverter.GetBytes).Take(12))
        .Select((pst, i) => basePST[i / 128 * 2 + i % 2] + pst).ToArray();

    public Move Think(Board board, Timer timer)
    {

        int PVSearch(int alpha, int beta, int depth, int ply, ref Move best)
        {
            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 32) throw new TimeoutException();
            if (board.IsRepeatedPosition()) return 0;

            ++ply;

            bool qsearch = depth <= 0, notcheck = !board.IsInCheck(), prune = alpha + 1 == beta && notcheck;

            var key = board.ZobristKey;
            ref var tte = ref tt[key & 0x7FFFFF];
            var ttc = tte;

            int eval = 0, value = ttc.eval;

            if (ply > 0 && ttc.key == key && ttc.depth >= depth && ttc.bound switch
            {
                0 => value >= beta,
                1 => value <= alpha,
                _ => true,
            })
            {
                best = ttc.move;
                return value;
            }

            if (qsearch || prune)
            {
                var pawn = PieceType.Pawn;
                Span<ulong> bitboards = stackalloc ulong[14];
                for (value = 0; value < 12; ++value) bitboards[value] =
                    board.GetPieceBitboard(pawn + value / 2, value % 2 > 0);

                int mg = 0, eg = 0, phase = 0, white, piece;

                for (white = 0; white < 2; ++white, mg = -mg, eg = -eg)
                    for (piece = 0; piece < 6; ++piece)
                        for (var bitboard = bitboards[piece * 2 + white]; bitboard > 0;)
                        {
                            value = ClearAndGetIndexOfLSB(ref bitboard);

                            ulong
                                file = 0x101010101010101u << value % 8,
                                attack = GetPieceAttacks(
                                    pawn + piece,
                                    new Square(value),
                                    board, white > 0);

                            bitboards[12 + white] |= attack;

                            var penalty = GetNumberOfSetBits(file & piece switch
                            {
                                0 => bitboard,
                                3 => bitboards[white],
                                _ => 0
                            }) * 16 - GetNumberOfSetBits(attack);

                            value = 2 * (piece * 64 + value ^ white * 56);
                            mg += PST[value] - penalty;
                            eg += PST[value + 1] - penalty;

                            phase += 0x042110 >> piece * 4 & 0xF;
                        }

                for (white = 0; white < 2; ++white, mg = -mg - value, eg = -eg - value)
                    value = 8 * GetNumberOfSetBits(
                        GetPieceAttacks(pawn + 5,
                            board.GetKingSquare(white == 0),
                            0, white == 0) & bitboards[12 + white]);

                if (phase > 24) phase = 24;
                eval = (mg * phase + eg * (24 - phase)) / 24;
                if (board.IsWhiteToMove) eval = -eval;
            }

            var tmpBest = Move.NullMove;

            if (qsearch)
            {
                if (eval >= beta) return beta;
                if (eval > alpha) alpha = eval;
            }
            else if (prune)
            {
                if (eval >= beta + depth * 64) return eval;

                if (depth >= 3 && eval >= beta)
                {
                    board.ForceSkipTurn();
                    value = -PVSearch(-beta, -beta + 1, depth - 3, ply, ref tmpBest);
                    board.UndoSkipTurn();
                    if (value >= beta) return beta;
                }
            }

            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, qsearch);

            if (!qsearch && moves.IsEmpty) return notcheck ? 0 : -0x7FFF + board.PlyCount;

            Span<int> moveOrder = stackalloc int[moves.Length];

            var index = 0;
            foreach (var move in moves)
                moveOrder[index++]
                    = move == best ? -1000
                    : move == ttc.move ? -100
                    : move == killers[ply] ? -10
                    : move.MovePieceType - move.CapturePieceType;

            moveOrder.Sort(moves);

            byte bound = 1, explored = 0;

            foreach (var move in moves)
            {
                board.MakeMove(move);

                var reduction
                    = move.IsPromotion || board.IsInCheck() ? 0
                    : !move.IsCapture && prune && explored > 4 ? 2 + explored / 8
                    : 1;

                if (!qsearch && explored++ > 0)
                {
                    value = -PVSearch(-alpha - 1, -alpha, depth - reduction, ply, ref tmpBest);
                    if (value <= alpha || value >= beta) goto skipfullsearch;
                }

            retryfullsearch:
                value = -PVSearch(-beta, -alpha, depth - reduction, ply, ref tmpBest);
            skipfullsearch:

                if (value > alpha)
                {
                    if (reduction > 1)
                    {
                        reduction = 1;
                        goto retryfullsearch;
                    }

                    bound = 2;
                    best = move;
                    alpha = value;
                }

                board.UndoMove(move);

                if (alpha >= beta)
                {
                    if (!move.IsCapture) killers[ply] = move;
                    bound = 0;
                    break;
                }
            }

            tte = new TTE(key, best, (short)alpha, (sbyte)depth, bound);
            return alpha;
        }

        var move = Move.NullMove;

        try
        {
            for (var depth = 1; depth <= 42; ++depth)
                if (PVSearch(-0x7FFF, 0x7FFF, depth, -1, ref move) > 0x6FFF)
                    break;
        }
        catch { }

        return move;
    }
}