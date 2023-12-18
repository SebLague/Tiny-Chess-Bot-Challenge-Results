namespace auto_Bot_242;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_242 : IChessBot
{

    /*
     *  SSSSSs   LL       OOOOO   TTTTTTTT  HH   HH    || 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF ||   VV   VV    11
     * SS        LL      OO   OO     TT     HH   HH    || FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF ||   VV   VV    11
     *  SSSSS    LL      OO   OO     TT     HHHHHHH    || FF.00.00 FF.00.00 FF.00.00 FF.00.00 FF.00.00 FF.00.00 FF.00.00 ||    VV VV     11
     *      SS   LL      OO   OO     TT     HH   HH    || FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF FF.FF.FF ||    VV VV     11
     * sSSSSS     LLLLL   OOOOO      TT     HH   HH    || 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF 00.00.FF ||     VVV   O  11
     */

    /* All hail Mediocrechess blog for guiding me */

    //Z_Key, Eval, Depth, Best, flag
    readonly (ulong, int, sbyte, Move, byte)[] TTable = new (ulong, int, sbyte, Move, byte)[k_TpMask + 1];
    /*
     *  0 Invalid => hasn't been evaluated.
     *  1 Low     => beta <= evaluation (fails high)
     *  2 Exact   => alpha < evaluation < beta
     *  3 Upper   => evaluation < alpha (fails low)
     */

    static ulong k_TpMask = 0x7FFFFF;
    Move[] killerMoves = new Move[2048];


    public Move Think(Board board, Timer timer)
    {
        int allocatedTime = timer.MillisecondsRemaining / 40, // Time to start search, time to finish is 3 times that
            depth, eval = 0, alpha = -9000, beta = 9000;

        var historyHeuristics = new int[2, 7, 64]; //TODO: Add Counter Move History

        for (depth = 2; allocatedTime > timer.MillisecondsElapsedThisTurn; ++depth)
        {
            eval = Search(alpha, beta, depth, 0, false);


            // Widden the window search if fell out the window (aka defenestration)
            if (eval <= alpha)
                alpha -= 100;
            if (eval >= beta)
                beta -= 100;
            // Move the window about a pawn
        }

        //DivertedConsole.Write($"Move Evaluation Depth: {depth} Eval: {eval} Move: {TTable[board.ZobristKey & 0x7FFFFF].Item4}");//Debug
        return TTable[board.ZobristKey & k_TpMask].Item4;


        int Search(int alpha, int beta, int depth, int ply, bool nullPrune)
        {
            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return -999999;

            bool
                 pvNode = beta - alpha > 1,
                 inCheck = board.IsInCheck(),
                 doEFPrune = false;

            if (inCheck) depth++;

            bool quiescence = depth < 0;

            var (tKey, tEval, tDepth, tBest, tFlag) = TTable[board.ZobristKey & k_TpMask];

            if //Reuse previous evaluations
                (
                 !pvNode // Outside PVS
                 && tKey == board.ZobristKey // Same position (in case of data colision)
                 && tDepth >= depth /*The evaluation went deeper (more depth left)*/ &&
                 //Fails high
                 (tFlag == 1 && tEval >= beta ||
                  //Exact
                  tFlag == 2 ||
                  //Fails low
                  tFlag == 3 && tEval <= alpha)
                )
                return tEval;

            int standingPat = Evaluate(),
                movesRated = 0,
                initialAlpha = alpha,
                bestEvaluation = -2147483648, //Min integer value 
                currentEvaluation,
                m = 0;

            Span<Move> moves = stackalloc Move[218];

            board.GetLegalMovesNonAlloc(ref moves, quiescence && !inCheck);

            if (moves.Length == 0) return standingPat; //Stabilized postion


            int nSearch(int nAlpha, int reduccions = 1, bool allowNull = true) => currentEvaluation = -Search(-nAlpha, -alpha, depth - reduccions, ply + 1, allowNull);

            if (quiescence)
            {
                if (standingPat >= beta) return beta;
                alpha = Math.Max(standingPat, alpha);
            }
            else if (!pvNode && !inCheck)
            {
                if (depth <= 7 && standingPat - 70 * depth >= beta) return standingPat;

                // Null Move Prunning
                // Ordinary recursive calls sends allowNull=true, while null-move searches sends allowNull=false.
                if (depth >= 2 && standingPat >= beta && nullPrune)
                {
                    board.ForceSkipTurn();

                    nSearch(beta, depth > 3 ? 3 : 2, false);

                    board.UndoSkipTurn();

                    // Failed high on the null move
                    if (currentEvaluation >= beta)
                        return currentEvaluation;
                }

                // Don't do EFP inside quiescence
                doEFPrune = depth <= 7 && standingPat + depth * 141 <= alpha;
            }

            // Order Moves
            foreach (Move move in moves) MovePriority[movesRated++] = -(
                    //Previously best evaluation
                    (tBest == move && tKey == board.ZobristKey) ? 9_000_000 :
                    //Captures ordered in MVVLVA order
                    move.IsCapture ? 1000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    //Check for a killer move
                    move == killerMoves[ply] ? 3
                    /*if none of the previous conditions passed, priority*/ : historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            MovePriority.AsSpan(0, moves.Length).Sort(moves);

            Move best = tBest;
            //Null window won't be used with this one to properly set alpha.

            foreach (Move move in moves)
            {
                if (m > 3 && timer.MillisecondsElapsedThisTurn > allocatedTime * 3)
                    return 999999;

                // RFP
                if (doEFPrune && !(move.IsCapture || move.IsPromotion || m == 0)) continue;

                board.MakeMove(move);


                /* PVS + LMR */

                /* TODO: Add these conditions
                 * Most programs do not reduce these types of moves:
                 *   Tactical Moves (captures and promotions) +
                 *   Moves while in check +
                 *   Moves which give check
                 *   Moves that cause a search extension
                 *   Anytime in a PV-Node in a PVS search +
                 *   Depth < 3 (sometimes depth < 2) +
                 */

                if (
                (
                 // This increases the count after the comparison
                 ++m < 7 /* 30/5 */
                 || quiescence
                 || inCheck || move.IsCapture
                 || depth < 2
                 /*       Reduced Null Window Search                  Null Window Search      */
                 || (nSearch(alpha + 1, pvNode ? 2 : m / 6) > alpha && nSearch(alpha + 1) > alpha)
                 ) && nSearch(beta) > bestEvaluation
                )
                {
                    best = move; bestEvaluation = currentEvaluation;
                    alpha = Math.Max(alpha, bestEvaluation);

                    //Fail hard (Refutes previous move)
                    if (/*allocatedTime < timer.MillisecondsElapsedThisTurn || */alpha >= beta)
                    {
                        if (ply < 2048) killerMoves[ply] = best;
                        historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                        board.UndoMove(move);
                        break;
                    }
                }

                board.UndoMove(move);

            }

            if (tDepth < depth)
                // Update table
                TTable[board.ZobristKey & k_TpMask] = (
                /*tKey  */ board.ZobristKey,
                /*tEval */ bestEvaluation,
                /*tDepth*/ (sbyte)depth,
                /*tBest */ best,
                /*Flag  */ (byte)(alpha >= beta /*Fails High*/ ? 1 : alpha < initialAlpha /*Fails low*/ ? 3 : /*Exact*/ 0));

            return bestEvaluation;
        }

        /*
         * Using Tyrant's evaluation because I ran out of time to fix mine. Ty btw. 
         */
        int Evaluate()
        {
            int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square/*, hangingPenalty = 0*/;
            var hangingPieces = new ulong[2];

            for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
                for (piece = 6; --piece >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;

                            // Gamephase, middlegame -> endgame
                            // Multiply, then shift, then mask out 4 bits for value (0-16)
                            gamephase += 0x00042110 >> piece * 4 & 0x0F)
                    {
                        // Material and square evaluation
                        square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;

                        //hangingPieces[sideToMove] |= BitboardHelper.GetPieceAttacks((PieceType) piece + 1, new(square), board, sideToMove > 0);

                        //square ^= 56 * sideToMove;
                        middlegame += UnpackedPestoTables[square * 16 + piece];
                        endgame += UnpackedPestoTables[square * 16 + piece + 6];

                        // Bishop pair bonus
                        /**/
                        if (piece == 2 && mask != 0)
                        {
                            middlegame += 23;
                            endgame += 62;
                        }

                        // Doubled pawns penalty (brought to my attention by Y3737)
                        if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                        {
                            middlegame -= 15;
                            endgame -= 15;
                        }
                        /**/

                        // Semi-open file bonus for rooks (+14.6 elo alone)
                        /*
                        if (piece == 3 && (0x101010101010101UL << (square & 7) & board.GetPieceBitboard(PieceType.Pawn, sideToMove > 0)) == 0)
                        {
                            middlegame += 13;
                            endgame += 10;
                        }
                        */

                        // Mobility bonus (+15 elo alone)
                        /*
                        if (piece >= 2 && piece <= 4)
                        {
                            int bonus = BitboardHelper.GetNumberOfSetBits(
                                BitboardHelper.GetPieceAttacks((PieceType)piece + 1, new Square(square ^ 56 * sideToMove), board, sideToMove > 0));
                            middlegame += bonus;
                            endgame += bonus * 2;
                        }
                        */
                    }

            /*
             * TODO: Make the opponent have less penalty to avoid making the bot avoiding free pieces
             * Too many tokens, exceeds limit
             */
            /*
            for (sideToMove = 2; --sideToMove >= 0; hangingPenalty = -hangingPenalty)
            for (piece = 6; --piece >= 0;)
                hangingPenalty = (1 << piece) * BitboardHelper.GetNumberOfSetBits(
                    ~hangingPieces[sideToMove] & hangingPieces[~sideToMove & 1] & board.GetPieceBitboard((PieceType) piece, sideToMove > 0));
            */
            return (middlegame * gamephase + endgame * (24 - gamephase)) / (board.IsWhiteToMove ? 24 : -24)

                /*
                ((middlegame * gamephase + endgame * (24 - gamephase)) / 24 
                    - 3 * hangingPenalty) 
                * (board.IsWhiteToMove ? 1 : -1)
                */

                + 16; // Tempo bonus to help with aspiration windows
        }
    }

    /*
     * Middlegame = (77, 302, 310, 434, 890, 0)
     * Endgame = (109, 331, 335, 594, 1116, 0)
     * 
     * # 11 bits by piece -> 66 bits ; king can be omitted, so really 55 bits
     * # Max value 2047
     * def compress(values):
     *      result = 0
     *      for i, value in enumerate(values):
     *          result += value << (i*11)
     *      return result
     *
     *      pass
     * # --------------------------------------------
     *
     * comMid = compress(Middlegame)
     * comEnd = compress(Endgame)
     * print(f"Middlegame: {comMid} ; {bin(comMid)}") # 15660774911995981 ; 0b110111101000110110010001001101100010010111000001001101
     * print(f"Endgame: {comEnd} ; {bin(comEnd)}")    # 19637983452485741 ; 0b1000101110001001010010001010011110010100101100001101101
     *
     * # --------------------------------------------
     */

    // ( index % 12 < 6) ? (M) >> (index % 12) * 11 : (E) >> (index % 12 - 6) ) & 2047
    // ( (M) >> index % 12 * 11 + (E) >> (index % 12 - 6) * 11 ) & 2047
    // ( 15660774911995981 >> index % 12 * 11 | 19637983452485741 >> (index % 12 - 6) * 11 ) & 2047
    // ( index % 12 < 6 ? 15660774911995981 : 19637983452485741 ) >> (index % 6 * 11) & 2047
    // Doesn't work time wasted

    int[] MovePriority = new int[218];
    static readonly int[] PieceValues = { 77, 302, 310, 434, 890, 0, // Middlegame
                                                 109, 331, 335, 594, 1116, 0, }, // Engame
    UnpackedPestoTables =
        new[] {
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
                    // No point in only taking 12 bytes. Since we never access the last 4 anyway, we can just leave them as garbage
                    .Select((square, index) => (int)((sbyte)square * 1.461) + PieceValues[index % 12])
                .ToArray()
        ).ToArray();

}
