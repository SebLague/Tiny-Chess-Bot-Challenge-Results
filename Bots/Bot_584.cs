namespace auto_Bot_584;
// link to the repository for fully commented my bot(Which will be open sourced after submissions)
// https://github.com/mcthouacbb/Chess-Challenge

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_584 : IChessBot
{
    private Move bestMoveRoot;

    private int nodes, phase, packedEval, sq, it, delta;

    int[] PSQT = new[] {
        162378573907163608468443463m, 12509171266076764537038065991m, 14089261551695647595105571143m, 22351037433687643931806486855m,
        19003484170494650816742838599m, 21187980179940948218151653703m, 19975300079112888281614799175m, 2687811070247606617347215687m,
        19870179131999826413948997223m, 25809768411681260029584968043m, 28245881552641757293457278305m, 24915375737088974826198425980m,
        28903659924196595598264655214m, 31094078408545548091836557405m, 31080713816033830771089640730m, 24864238667859563594204156690m,
        23260016648785637699477266200m, 27081506703938028863917941793m, 31034817044805132858166233405m, 32882065381430657238225992768m,
        33820243947236544512568209989m, 32959385480350578468483704410m, 32646113279690211858442504513m, 27013689959377190020747708962m,
        20802303329675993978549577738m, 28839341440119632895448981530m, 32248550104656124158272475931m, 35603413664164583855827061280m,
        35607092460794033969129849393m, 35017075472828730553059181614m, 32857867882404223461226910256m, 27562687882055139606891926554m,
        18331259009145206108329743360m, 26053981037194044763149767444m, 31294688650843406872773592593m, 35281829969640750762259972641m,
        35285428449506518144594411554m, 33140737414290710117469454369m, 29742395154414218187319347749m, 25997100445211660676862314251m,
        16810359492284245558761819904m, 22707542307684866459682345487m, 28216739956963853927472301070m, 30988773823366239658592439568m,
        30996051026723574284591367966m, 29140326373800249518908507161m, 24852181690656888486452266547m, 22046203362097688778252813078m,
        12256354782080149317318808576m, 18712018788167055605867549713m, 22713516193449595772052442122m, 25459048120630765548933812484m,
        26386289483102799332794043926m, 24240356575610778868202604331m, 19647543568190187961459541564m, 15012441751322451226075137041m,
        5429531826221284093706719559m, 8247537745937350384847180103m, 12552508570564778792259965255m, 18938017191840019227158860103m,
        11892444590785613558071447879m, 18349303023333149394129736007m, 11313293546591776380314865991m, 4196304726628143172615622983m
    }.SelectMany(decimal.GetBits).SelectMany(BitConverter.GetBytes).Chunk(2).Select(a => a[0] + a[1] * 65536).ToArray(),
        staticEvals = new int[256];

    (ulong, Move, short, byte, byte)[] ttEntries = new(ulong, Move, short, byte, byte)[8388608];

    public Move Think(Board board, Timer timer)
    {
        bool shouldStop = false;
        nodes = 0;
        var history = new int[2, 4096];
        var killerMoves = new Move[128];
        Array.Clear(ttEntries);


        for (int depth = 1, alpha = -64000, beta = 64000; ; delta *= 2)
        {
            it = Search(depth, alpha, beta, false, 0);

            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 50)
                return bestMoveRoot;
            if (it <= alpha)
                alpha -= delta;
            else if (it >= beta)
                beta += delta;
            else
            {
                delta = ++depth <= 6 ? 64000 : 15;
                alpha = it - delta;
                beta = it + delta;
            }
        }



        int Search(int depth, int alpha, int beta, bool doNull, int ply)
        {
            int LocalSearch(int localAlpha, int R = 1, bool localDoNull = true) => it = -Search(depth - R, -localAlpha, -alpha, localDoNull, ply + 1);

            bool inCheck = board.IsInCheck();

            if (ply > 0)
            {
                if (inCheck)
                    depth++;

                if (board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100)
                    return 0;
            }

            bool notPV = beta - alpha == 1,
                isQSearch = depth <= 0;

            ulong zkey = board.ZobristKey;
            var (ttKey, ttMove, ttScore, ttDepth, ttType) = ttEntries[zkey % 8388608];

            if (ttKey == zkey && notPV && ttDepth >= depth && (ttScore >= beta ? ttType > 1 : ttType < 3))
                return ttScore;


            phase = packedEval = it = 0;


            for (; it < 6; it++)
                for (int stm = 2; --stm >= 0; packedEval = -packedEval)
                    for (ulong pieceBB = board.GetPieceBitboard((PieceType)it + 1, stm == 1); pieceBB != 0;)
                    {
                        sq = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBB) ^ stm * 0b111000;
                        packedEval += PSQT[sq * 8 + it] + new[] { 5111853, 11075723, 14352633, 27459969, 49611616, 0 }[it];
                        phase += 17480 >> 3 * it & 7;

                        if (it == 2 && pieceBB != 0)
                            packedEval += 3407886;

                        if (it == 3 && (board.GetPieceBitboard(PieceType.Pawn, stm == 1) & 0x0101010101010101u << sq % 8) == 0)
                            packedEval += 655380;
                    }

            int staticEval = staticEvals[ply] = 8 + ((short)packedEval * phase + (packedEval + 0x8000 >> 16) * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24),
                bestScore = -32000,
                movesPlayed = 0,
                improving = Convert.ToInt32(!inCheck && ply > 1 && staticEval > staticEvals[ply - 2]);

            if (isQSearch)
            {
                bestScore = staticEval;
                if (staticEval >= beta)
                    return staticEval;
                if (staticEval > alpha)
                    alpha = staticEval;
            }
            else if (notPV && !inCheck)
            {
                if (depth <= 6 && staticEval - (depth - improving) * 80 >= beta)
                    return staticEval;

                if (doNull && depth >= 3 && phase > 2)
                {
                    board.ForceSkipTurn();
                    LocalSearch(beta, 2 + depth / 3, false);
                    board.UndoSkipTurn();
                    if (it >= beta)
                        return it;
                }
            }

            Span<Move> moves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref moves, isQSearch);

            Span<int> moveScores = stackalloc int[moves.Length];
            it = 0;
            foreach (Move move in moves)
                moveScores[it++] = -(
                    move == ttMove ? 2000000000 :
                    move.IsCapture || move.IsPromotion ?
                        200000000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    move == killerMoves[ply] ? 100000000 :
                    history[ply & 1, move.RawValue & 4095]
                );

            if (it == 0 && !isQSearch)
                return inCheck ? ply - 32000 : 0;

            moveScores.Sort(moves);

            ttType = 1;
            foreach (Move move in moves)
            {
                if (++nodes % 2048 == 0 && timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 20 || shouldStop)
                {
                    shouldStop = true;
                    return alpha;
                }

                bool isQuiet = !move.IsCapture && !move.IsPromotion;
                if (notPV && !inCheck && isQuiet && depth <= 5 && movesPlayed >= depth * (9 + 2 * improving) | staticEval + (depth + improving) * 130 - 50 <= alpha)
                    break;

                board.MakeMove(move);
                int reduction = movesPlayed >= 4 &&
                    depth >= 3 &&
                    isQuiet ? 2 + depth / 8 + movesPlayed / 19 : 1;


                if (movesPlayed++ == 0 || isQSearch || LocalSearch(alpha + 1, reduction) > alpha && reduction > 1 | !notPV)
                    LocalSearch(beta);


                board.UndoMove(move);

                if (it > bestScore)
                    bestScore = it;


                if (it > alpha)
                {
                    if (ply == 0)
                        bestMoveRoot = move;

                    alpha = it;
                    ttMove = move;
                    ttType = 2;
                }
                if (alpha >= beta)
                {
                    if (!isQSearch && isQuiet)
                    {
                        killerMoves[ply] = ttMove;
                        history[ply & 1, ttMove.RawValue & 4095] += depth * depth;
                    }
                    ttType++;
                    break;
                }
            }


            ttEntries[zkey % 8388608] = (zkey, ttMove, (short)bestScore, (byte)Math.Max(depth, 0), ttType);

            return bestScore;
        }
    }
}
