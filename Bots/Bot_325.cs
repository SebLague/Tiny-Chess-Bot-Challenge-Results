namespace auto_Bot_325;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_325 : IChessBot
{
    //All non TT objects take less than 32kbytes.
    //(2^28 - 32k) /  Marshal.SizeOf<(ulong, Move, int int int)> = (2^28 - 32k) / 24 = ~0.7 * 2^24 Rounded down to power of 2 = 2^23
    //replacing int by byte will not work we still get .net padding up to 24bytes/entry. 
    static (ulong, Move, int, int, int)[] TT = new (ulong, Move, int, int, int)[0x800000];
    static Move[] killers = new Move[8088]; //killers + no allocation movelist

    Move rootMove;
    int gameplies, depth;

    public Move Think(Board board, ChessChallenge.API.Timer timer)
    {
        gameplies += 2;
        int alpha = -999999, beta = 999999, eval;
        var historyHeuristics = new int[2, 4096];
        depth = Math.Max(2, depth / 2);

        while ((eval = PVS(depth, alpha, beta, 0, true)) != 99999)
        {
            // Gradual widening
            if (eval <= alpha)
                alpha -= 62;
            else if (eval >= beta)
                beta += 62;
            else
            {
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }
        return rootMove;

        // Principal Variation Search + Quiesce search
        int PVS(int depth, int alpha, int beta, int plyFromRoot, bool allowNull)
        {
            //Around ply 10 - 30 we take extra time. Good with and without opening book.  
            // Return checkmate so that this move is ignored
            if (depth > 2 && timer.MillisecondsElapsedThisTurn > (timer.MillisecondsRemaining / 32) * (gameplies < 8 | gameplies >= 26 ? 1 : 1.5f))
                return 99999;


            // Declare some reused variables
            bool noCheck = !board.IsInCheck(),
                canFPrune = false,
                notRoot = plyFromRoot++ > 0,
                notPV = beta - alpha == 1;

            // Draw detection
            if (notRoot && board.IsRepeatedPosition())
                return 0;

            ulong zobristKey = board.ZobristKey;
            ref var TTEntry = ref TT[zobristKey & 0x7FFFFF];
            var (entryKey, bestMove, entryScore, entryDepth, entryFlag) = TTEntry;

            // Define best eval all the way up here to generate the standing pattern for QSearch
            int bestEval = -9999999,
                newTTFlag = 2,
                movesTried = 0,
                movesScored = 0,
                eval;

            // Local method to save tokens for similar calls to PVS (set eval inside search)
            int Search(int newAlpha, int R = 1, bool canNull = true) => eval = -PVS(depth - R, -newAlpha, -alpha, plyFromRoot, canNull);

            // Check extensions
            if (!noCheck)
                depth++;

            // Transposition table lookup -> Found a valid entry for this position
            // Looked up as REF for performance reasons
            if (TTEntry.Item1 == zobristKey && notRoot && entryDepth >= depth && Math.Abs(entryScore) < 50000
                && entryFlag != 3 | entryScore >= beta
                && entryFlag != 2 | entryScore <= alpha)
                return entryScore;

            // Declare QSearch status here to prevent dropping into QSearch while in check
            bool inQSearch = depth <= 0;
            if (inQSearch)
            {
                // Determine if quiescence search should be continued
                bestEval = Evaluate();
                if (bestEval >= beta)
                    return bestEval;
                alpha = Math.Max(alpha, bestEval);
            }
            // No pruning in QSearch
            // If this node is NOT part of the PV and we're not in check
            else if (notPV && noCheck)
            {
                // PRUNE: Reverse Futility
                int staticEval = Evaluate();

                // Give ourselves a margin of 74 centipawns times depth.
                // If we're up by more than that margin in material, there's no point in
                // searching any further since our position is so good
                if (depth <= 7 && staticEval - 74 * depth >= beta)
                    return staticEval;

                // PRUNE: NULL MOVE
                if (!inQSearch && staticEval >= beta && allowNull)
                {
                    board.ForceSkipTurn();
                    Search(beta, 3 + depth / 4 + Math.Min(6, (staticEval - beta) / 175), false);
                    board.UndoSkipTurn();

                    // Failed high on the null move
                    if (eval >= beta)
                        return eval;
                }

                // PRUNE: Extended futility. Can only prune when at lower depth and behind in evaluation by a large margin
                canFPrune = depth <= 8 && staticEval + depth * 141 <= alpha;
            }
            var moveSpan = killers.AsSpan(128 * plyFromRoot + 512);
            board.GetLegalMovesNonAlloc(ref moveSpan, inQSearch && noCheck);

            // Move Ordering
            foreach (Move move in moveSpan)
                MoveScores[movesScored++] = -(
                // Hash move
                move == bestMove ? 9_000_000 :
                // MVVLVA
                move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                // Killers
                move == killers[gameplies + plyFromRoot] ? 900_000 :
                // History
                historyHeuristics[plyFromRoot & 1, move.RawValue & 0b111111111111]);


            MoveScores.AsSpan(0, moveSpan.Length).Sort(moveSpan);

            foreach (Move move in moveSpan)
            {
                // Futility pruning
                if (canFPrune && !(movesTried == 0 || move.IsCapture || move.IsPromotion))
                    continue;

                //if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen) continue;

                board.MakeMove(move);

                // LMR + PVS
                // Do a full window search if haven't tried any moves or in QSearch
                if (movesTried++ == 0 || inQSearch ||

                    // Otherwise, skip reduced search if conditions are not met
                    (movesTried < 6 || depth < 2 ||

                        // If reduction is applicable do a reduced search with a null window
                        (Search(alpha + 1, 1 + movesTried / 13 + depth / 9 + (notPV ? 1 : 0)) > alpha)) &&

                        // If alpha was above threshold after reduced search, or didn't match reduction conditions,
                        // update eval with a search with a null window
                        alpha < Search(alpha + 1))

                    // We either raised alpha on the null window search, or haven't searched yet,
                    // -> research with no null window
                    Search(beta);

                board.UndoMove(move);

                if (eval > bestEval)
                {
                    bestEval = eval;
                    if (eval > alpha)
                    {
                        alpha = eval;
                        bestMove = move;
                        newTTFlag = 1;

                        // Update the root move
                        if (!notRoot)
                            rootMove = move;
                    }

                    // Cutoff
                    if (alpha >= beta)
                    {
                        // Update history tables
                        if (!move.IsCapture)
                        {
                            historyHeuristics[plyFromRoot & 1, move.RawValue & 0b111111111111] += depth * depth;
                            killers[gameplies + plyFromRoot] = move;
                        }
                        newTTFlag = 3;
                        break;
                    }
                }
            }

            // We had no legal moves
            if (bestEval == -9999999)
                return noCheck ? 0 : plyFromRoot - 99999;

            // Transposition table insertion
            TTEntry = (zobristKey, bestMove, bestEval, depth, newTTFlag);

            return bestEval;
        }

        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                    {

                        //{ 0, 1, 1, 2, 4, 0 }[piece]
                        gamephase += 0x00042110 >> piece * 4 & 0x0F;

                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

                        middlegame += PST[square * 16 + piece];
                        endgame += PST[square * 16 + piece + 6];

                        // Bishop pair bonus
                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 23;
                            endgame += 62;
                        }
                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            middlegame -= 25;
                            endgame -= 25;
                        }
                    }
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)
            // Tempo bonus to help with aspiration windows
                + 16;
        }
    }

    static int[] MoveScores = new int[218],

    //Retuned with 300 WAC positions
    PST = new[] {
        59445390123883219059782226176m, 70290677894405958874073512960m, 71539512396922371695602361600m, 78956267780589860938551981568m, 76477950923876370409996948736m, 78329987370818127525518498304m, 77060619909450810521835651072m, 61616583115249891914576543744m,
        76753580919193488454062043180m, 3437103645626119474067276085m, 5013542988189699209347735850m, 2865258195181361447053565244m, 5661503541441299719316970807m, 8414185094009835055136457260m, 8090174196009274972526347775m, 2486769613674808756792921836m,
        934584826409322862812005365m, 3735675340570651700594350849m, 8406969523092559353284994326m, 9647313136233310834915618841m, 9662991465804707416676190491m, 7785515519143610030641794092m, 8991995041815735990181178397m, 3721068986165620197362963709m,
        77383440993701244525986382830m, 5608301657434710294674213887m, 9021118025420956391840487680m, 11824603037136187117537274369m, 11837858590869387381917424400m, 11207998756503440353920626186m, 9028281854943685960159333898m, 4365435930136943526777264637m,
        75531342952572139448559204581m, 3129994171303795302534021625m, 7780746115255972865711344887m, 11205618721337766047278108163m, 11509049657471625577046086659m, 9025906578745831435612590333m, 6534267870125013366766637059m, 3115411244626559612837953008m,
        74280085839011331528989273317m, 324048954150361129609198582m, 4370323765064575483937883636m, 7150862595560822679991355382m, 7155688890998119161645501953m, 5600986637454890754120354040m, 1563108101768245091211217422m, 77683136352750705373032019449m,
        70256775951641872093279544293m, 76139418398446680433540787192m, 78919947765904233498722236146m, 2485617727604606326540337134m, 3105768375617668305352130555m, 1225874429600076427953045769m, 76411360668080757388661159957m, 72368736062636307117033191160m,
        64062225681559206515598345472m, 67469002455547505989692680960m, 70566298464266996568932736768m, 75814231575406875932276091648m, 70254367545452773838605905408m, 75823869869625855901850465536m, 69305327516206663745359178752m, 63733359968579361022158820866m
    }.SelectMany(packedTable => decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes).Select((square, index) => (int)((sbyte)square * 1.461) + new[] { 77, 302, 310, 434, 890, 0, 109, 331, 335, 594, 1116, 0 }[index % 12]).ToArray()).ToArray();

    //Above is packed by taking the value of a square like 620, subtracting the mean of the piecetype which will compress into a byte. 
    //24bytes fit into one decimal. (3 int + sign constructor)
    /*
    PST = {
        77, 140, 299, 469, 860, 42, 109, 267, 333, 620, 1145, -91, 79, 302, 310, 434, 77, 202, 277, 457, 866, 14, 110, 304, 339, 626, 1161, -41, 77, 302, 310, 434,
        77, 263, 290, 467, 898, 46, 109, 318, 337, 631, 1170, -36, 77, 302, 310, 434, 77, 288, 246, 472, 930, -49, 109, 314, 352, 629, 1162, -1, 77, 302, 310, 434,
        77, 332, 246, 492, 932, -4, 109, 311, 349, 620, 1158, -10, 77, 302, 310, 434, 77, 247, 274, 512, 934, 18, 109, 298, 339, 608, 1152, -5, 77, 302, 310, 434,
        77, 208, 298, 488, 945, 42, 109, 311, 334, 611, 1114, -7, 77, 302, 310, 434, 77, 209, 262, 504, 890, 105, 109, 231, 337, 608, 1151, -84, 77, 302, 310, 434,
        138, 279, 305, 444, 889, -64, 288, 304, 321, 620, 1118, -8, 80, 302, 310, 434, 154, 303, 334, 441, 872, -13, 281, 323, 336, 633, 1150, 16, 77, 302, 310, 434,
        138, 350, 321, 464, 876, -49, 278, 322, 342, 632, 1190, 23, 77, 302, 310, 434, 164, 345, 310, 486, 866, 20, 231, 330, 343, 626, 1212, 13, 77, 302, 310, 434,
        157, 346, 337, 470, 872, -8, 228, 315, 336, 628, 1225, 26, 77, 299, 310, 434, 141, 398, 346, 501, 920, 7, 237, 305, 334, 611, 1186, 39, 72, 305, 310, 434,
        76, 310, 328, 490, 900, -1, 284, 314, 342, 610, 1168, 36, 76, 302, 310, 437, 45, 322, 326, 515, 948, -27, 294, 286, 315, 591, 1129, 11, 81, 302, 310, 430,
        61, 306, 312, 423, 890, -77, 224, 315, 346, 621, 1124, 4, 77, 302, 310, 434, 78, 341, 342, 445, 889, 26, 230, 330, 342, 623, 1142, 20, 77, 302, 310, 434,
        109, 356, 351, 445, 892, -52, 198, 348, 351, 624, 1178, 39, 77, 302, 310, 434, 113, 372, 359, 451, 907, -58, 178, 348, 342, 621, 1180, 45, 77, 302, 310, 434,
        116, 408, 355, 479, 913, -42, 167, 330, 346, 608, 1203, 46, 77, 302, 310, 434, 142, 413, 383, 480, 954, 32, 154, 322, 348, 604, 1174, 42, 79, 305, 310, 434,
        119, 367, 362, 520, 958, 20, 198, 318, 340, 593, 1137, 43, 78, 302, 310, 434, 77, 331, 349, 495, 954, -33, 199, 307, 337, 593, 1123, 18, 77, 302, 310, 434,
        50, 306, 308, 405, 876, -61, 152, 327, 347, 624, 1134, -5, 77, 305, 310, 434, 75, 318, 320, 423, 879, -70, 142, 345, 361, 620, 1161, 26, 77, 302, 310, 434,
        77, 338, 346, 427, 886, -77, 128, 358, 353, 629, 1170, 42, 77, 302, 310, 434, 79, 363, 355, 434, 885, -117, 114, 361, 368, 624, 1194, 55, 75, 302, 310, 434,
        100, 341, 352, 436, 886, -116, 107, 363, 358, 611, 1209, 55, 77, 302, 312, 434, 91, 369, 347, 439, 898, -83, 109, 355, 356, 608, 1193, 52, 73, 302, 310, 434,
        97, 323, 324, 451, 897, -77, 126, 348, 353, 598, 1180, 43, 77, 302, 310, 434, 72, 338, 311, 452, 902, -109, 127, 315, 341, 595, 1156, 21, 77, 302, 315, 434,
        38, 291, 303, 389, 876, -55, 129, 330, 339, 617, 1137, -17, 77, 302, 310, 433, 67, 304, 317, 392, 877, -64, 126, 338, 355, 620, 1159, 12, 77, 302, 310, 434,
        64, 318, 323, 401, 876, -95, 108, 361, 364, 620, 1168, 37, 77, 302, 310, 434, 81, 322, 342, 414, 883, -128, 105, 358, 358, 621, 1193, 52, 77, 302, 310, 434,
        81, 331, 342, 412, 886, -124, 104, 364, 359, 617, 1186, 54, 75, 302, 310, 437, 73, 326, 324, 401, 883, -97, 105, 354, 358, 615, 1177, 42, 77, 302, 310, 438,
        83, 329, 318, 426, 894, -93, 114, 333, 351, 601, 1158, 30, 77, 302, 310, 434, 54, 294, 309, 414, 894, -122, 110, 324, 330, 596, 1146, 14, 77, 302, 314, 434,
        36, 273, 317, 382, 879, -23, 123, 311, 336, 611, 1120, -23, 77, 302, 314, 434, 63, 295, 323, 392, 886, -8, 123, 328, 348, 611, 1133, 1, 77, 302, 310, 434,
        63, 310, 323, 400, 879, -67, 108, 341, 353, 611, 1166, 21, 77, 302, 310, 434, 63, 312, 324, 401, 880, -80, 119, 354, 358, 615, 1155, 33, 77, 302, 307, 431,
        78, 318, 327, 405, 884, -75, 114, 354, 363, 611, 1161, 33, 77, 302, 310, 434, 66, 313, 321, 404, 889, -71, 109, 338, 355, 604, 1152, 26, 74, 302, 311, 434,
        98, 318, 323, 438, 901, -26, 113, 327, 344, 584, 1133, 7, 77, 302, 310, 434, 64, 286, 328, 417, 892, -43, 105, 317, 328, 586, 1118, -6, 77, 302, 306, 434,
        38, 260, 313, 378, 873, 59, 127, 305, 334, 608, 1120, -42, 77, 302, 310, 434, 66, 272, 321, 392, 882, 17, 127, 320, 331, 611, 1123, -14, 77, 302, 310, 434,
        57, 288, 328, 405, 890, 1, 116, 330, 328, 608, 1117, -1, 77, 302, 310, 434, 51, 302, 309, 404, 891, -37, 122, 335, 345, 614, 1127, 11, 77, 302, 312, 432,
        71, 301, 314, 403, 890, -36, 123, 332, 348, 605, 1129, 14, 76, 302, 310, 434, 90, 303, 330, 411, 898, -17, 113, 327, 336, 601, 1102, 4, 80, 301, 310, 434,
        110, 291, 337, 430, 909, 33, 111, 311, 336, 593, 1076, -14, 77, 301, 310, 434, 63, 288, 317, 395, 910, 43, 107, 313, 314, 598, 1054, -33, 77, 302, 310, 434,
        77, 216, 296, 398, 873, 49, 113, 302, 317, 604, 1115, -73, 77, 302, 310, 434, 77, 270, 314, 399, 864, 77, 109, 288, 334, 612, 1117, -56, 77, 297, 310, 434,
        77, 254, 302, 411, 872, 51, 109, 317, 312, 620, 1120, -37, 77, 302, 310, 434, 77, 272, 292, 416, 888, -54, 106, 320, 335, 618, 1105, -17, 77, 302, 310, 434,
        77, 275, 294, 420, 874, 14, 109, 320, 333, 611, 1117, -43, 77, 302, 314, 434, 75, 286, 293, 410, 861, -26, 109, 311, 331, 605, 1116, -20, 77, 302, 310, 434,
        79, 273, 317, 421, 879, 58, 109, 292, 318, 602, 1090, -48, 77, 297, 310, 430, 77, 240, 303, 404, 876, 53, 109, 289, 309, 592, 1089, -75, 77, 302, 310, 434
    };*/
}