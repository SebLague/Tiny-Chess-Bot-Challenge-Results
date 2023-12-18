namespace auto_Bot_422;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_422 : IChessBot
{
    private static readonly int[] PieceValues = { 77, 302, 310, 434, 890, 0,
                                          109, 331, 335, 594, 1116, 0}, ms = new int[218]

    , UnpackedPestoTables = new[] {
            59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m,
            77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m,
            934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m,
            78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m,
            75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m,
            74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m,
            70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m,
            64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m,
       }.SelectMany(packedTable =>
       decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                   .Select((square, index) => (int)((sbyte)square * 1.461) + PieceValues[index % 12])
               .ToArray()
       ).ToArray();
    (ulong, int, int, Move, int)[] tt = new (ulong, int, int, Move, int)[0x400000];
    Move bmr;
    Move[] kl = new Move[2048];
    public Move Think(Board board, Timer timer)
    {
        var h = new int[2, 7, 64];

        int gaming = timer.MillisecondsRemaining / 13, i = 2, a = -999999, b = 999999, e;
        for (; ; )
        {
            e = search(a, b, i, 0, true);
            if (timer.MillisecondsElapsedThisTurn > gaming / 3)
                return bmr;
            if (e <= a)
                a -= 62;
            else if (e >= b)
                b += 62;
            else
            {
                a = e - 17;
                b = e + 17;
                i++;
            }
        }

        int search(int a, int b, int d, int ply, bool can_null)
        {
            bool c = board.IsInCheck(), p = false, nroot = ply++ > 0, np = b - a == 1;
            if (nroot && board.IsRepeatedPosition()) return 50;

            ulong z = board.ZobristKey;

            var (enk, end, ens, enm, enf) = tt[z & 0x3fffff];
            int e, best = -9999999, ttf = 2, n = 0, msc = 0;

            int s(int A, int R = 1, bool cn = true) => e = -search(-A, -a, d - R, ply, cn);
            if (c) d++;

            if (enk == z && nroot && end >= d && Math.Abs(ens) < 50000 && (enf == 1 || enf == 2 && ens <= a || enf == 3 && ens >= b)) return ens;
            bool q = d <= 0;

            if (q)
            {
                best = eval();
                if (best >= b) return best;
                a = best > a ? best : a;
            }
            else if (np && !c)
            {
                int see = eval();
                if (d <= 7 && see - 74 * d >= b)
                    return see;
                if (d >= 2 && see >= b && can_null)
                {
                    board.ForceSkipTurn();
                    s(b, 3 + d / 4 + Math.Min(6, (see - b) / 175), false);
                    board.UndoSkipTurn();
                    if (e >= b) return e;
                }
                p = d <= 8 && see + d * 141 <= a;
                if (d == 3 && see + 600 <= a) d--;
            }
            Span<Move> msp = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref msp, q && !c);
            foreach (Move mv in msp) ms[msc++] = -(mv == enm ? 9000000 : mv.IsCapture ? 1000000 * (int)mv.CapturePieceType - (int)mv.MovePieceType : kl[ply] == mv ? 900000 : h[ply & 1, (int)mv.MovePieceType, mv.TargetSquare.Index]);

            ms.AsSpan(0, msp.Length).Sort(msp);

            Move bm = enm;
            foreach (Move i in msp)
            {
                if (d > 2 && timer.MillisecondsElapsedThisTurn > gaming) return 75000; // 20000
                if (p && !(n == 0 || i.IsCapture || i.IsPromotion)) continue;
                board.MakeMove(i);
                if (n++ == 0 || q || (n < 6 || d < 2 || (s(a + 1, (np ? 2 : 1) + n / 13 + d / 9) > a)) && a < s(a + 1)) s(b);
                board.UndoMove(i);
                if (e > best)
                {
                    best = e;
                    if (e > a)
                    {
                        a = e;
                        bm = i;
                        ttf = 1;
                        if (!nroot) bmr = i;
                    }
                    if (a >= b)
                    {
                        if (!i.IsCapture)
                        {
                            h[ply & 1, (int)i.MovePieceType, i.TargetSquare.Index] += d * d;
                            kl[ply] = i;
                        }
                        ttf = 3;
                        break;
                    }
                }
            }
            if (best == -9999999) return c ? ply - 99999 : 0;
            tt[z & 0x3fffff] = (z, d, best, bm, ttf);
            return best;

        }
        int eval()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;


                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                        middlegame += UnpackedPestoTables[square * 16 + piece];
                        endgame += UnpackedPestoTables[square * 16 + piece + 6];


                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 23;
                            endgame += 62;
                        }


                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            middlegame -= 15;
                            endgame -= 15;
                        }
                    }
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)
                + 16;
        }
    }
}