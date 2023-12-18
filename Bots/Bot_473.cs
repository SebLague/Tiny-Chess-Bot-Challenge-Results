namespace auto_Bot_473;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_473 : IChessBot
{
    readonly Move EMPTY_MOVE = new Move();

    // Indexes:           0, 1,   2,   3,   4,   5,  6
    // Values:         null, P,   N,   B,   R,   Q,  K
    int[] pieceValues = { 0, 80, 320, 330, 500, 900, 20000 };

    // (No King value, as Kings are used in gamephase evaluation)
    // Values:            null, P, N, B, R, Q   
    int[] gamephaseValues = { 0, 0, 1, 1, 2, 4 };

    // Store the psv's. Order is white mg values, white eg values, black mg values, black eg values, 
    // 12 (6 mg 6 eg) boards * 64 squares * 2 sides gives size = 1536.
    int[] pieceSquareValues = new int[1536];

    // 2 killer moves per ply * 24 maximum depth (0 - 23)
    Move[] killers = new Move[48];

    // lookahead, eval, bestMove, flag
    Dictionary<ulong, (int, int, Move, int)> transpositionTable;


    // Store some values globally to shave off few tokens.
    Board board;

    (int, int, Move, int) cachedValue;

    Move killer1;
    Move killer2;

    int avgMoveTimeMs = 20000;
    int searchDepth = 6;

    bool weAreWhite;
    bool timeout;

    int lastBestScore;

    public Bot_473()
    {
        // piece square tables inspired by https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
        ulong[] psvTables = {
            71525430544433664, 18434891338227770879, 18386062569588424191, 466047841934336, 21926708505550336, 18411470105013534207, 41359952817439232, 13891623449444352,
            591237209462407168, 2453059799198187357, 1760584642248727586, 1353764842849117962, 4682106015587917863, 153283331123824195, 8416946930880925306, 6830750066361162009,
            3516358578930176, 14695951350181658109, 6215813987183885634, 3852693352032396298, 6882751812330163219, 10473046375411954495, 13744472813043927229, 11102897478715068379,
            18121664682044424196, 325079391656705785, 909282063483178235, 1201314701337883418, 5017767524499101446, 17721358448793110523, 6608716018545794562, 2271167041494523190,
            3388693737294004240, 15040035937906065390, 5787178362411106193, 2120570028677547113, 10008464849680033844, 5403357707850512650, 730113151397147893, 3131386909367576494,
            36028797018964032, 8610319534943365039, 16953350001178237755, 8159074341780860067, 5195684147152644958, 4187064566753441690, 14286446674681544721, 15119006801280092800,

            72057044282114048, 18446463148488654847, 18374954210280799999, 224728193534208, 72039264676049152, 18374752189957371903, 56109509775872, 22213492640000,
            0, 4395230661825232572, 13984021532339112259, 6938155565081359702, 15343400259775214376, 7656876133266821817, 16193868928009222286, 3435155542802436848,
            0, 18446744073709551615, 30047451590957056, 13732953182257800799, 9056815080098901498, 15674182944983327414, 14277989785595597915, 7938738739345307643,
            0, 18446744073709551615, 18446741874431504754, 13573851613611675277, 4869229576937459338, 7104149538581770348, 8112639117880253692, 10519836394161442748,
            16356469695840256, 18430387604013711342, 9170846030491484177, 18288162038601750555, 18112458930802902137, 2178959015794375980, 18341559022168219817, 11282712898124465305,
            1132514156478464, 9222239522698297214, 467241295161474177, 13689394732245801749, 9982132891846174395, 25263199937102502, 13053595952196815844, 5574177697660916657,
        };

        for (int psvLongOffset = 0; psvLongOffset < 12; psvLongOffset++)
            for (int i = 0; i < 64; i++)
            {
                var psvLongsSubArray = new ulong[8];
                Array.Copy(psvTables, psvLongOffset * 8, psvLongsSubArray, 0, 8);
                int index = 768 + psvLongOffset * 64 + i;
                foreach (ulong psvLong in psvLongsSubArray)
                {
                    pieceSquareValues[index] <<= 1;
                    pieceSquareValues[index] |= BitboardHelper.SquareIsSet(psvLong, new Square(63 - i)) ? 1 : 0;
                }
                pieceSquareValues[index] -= 100;
                pieceSquareValues[psvLongOffset * 64 + i + 56 - 16 * (i / 8)] = pieceSquareValues[index];

            }
        transpositionTable = new Dictionary<ulong, (int, int, Move, int)>();
    }

    public Move Think(Board boardParam, Timer timer)
    {
        if (transpositionTable.Count > 2_000_000)
            transpositionTable.Clear();

        timeout = false;
        board = boardParam;
        weAreWhite = board.IsWhiteToMove;

        //dontEvaluateQuietPawnCaptures = true;
        if (boardParam.PlyCount > 20)
        {
            pieceValues[1] = 100;
            pieceSquareValues[259] = 10;
            pieceSquareValues[1083] = 102;
        }

        Move bestMove = EMPTY_MOVE;
        int depth = 0;
        while (depth < searchDepth)
        {
            // searchDepth, alpha, beta, color, ourMove, timer, isQuiet
            var bestMoveAndEval = NegaMax(depth++, -50000, 50000, weAreWhite ? 1 : -1, false, timer, false);
            if (timeout)
            {
                searchDepth--;
                break;
            }
            (bestMove, lastBestScore) = bestMoveAndEval;
        }

        avgMoveTimeMs = (avgMoveTimeMs + timer.MillisecondsElapsedThisTurn) / 2;
        if (avgMoveTimeMs < timer.MillisecondsRemaining * .007 && searchDepth < 24)
            searchDepth++;

        return bestMove;
    }

    private int BoardValue()
    {
        int material = 0,
            mg = 0,
            eg = 0,
            gamephase = 0,
            isWhiteAsInt = 0;
        while (isWhiteAsInt < 2)
        {
            int scoreSide = isWhiteAsInt * -2 + 1;
            int pieceIndex = 0;
            while (++pieceIndex < 7)
            {
                ulong bitboard = board.GetPieceBitboard((PieceType)pieceIndex, isWhiteAsInt == 0);
                while (bitboard > 0)
                {
                    if (pieceIndex != 6)
                    {
                        material += pieceValues[pieceIndex] * scoreSide;
                        gamephase += gamephaseValues[pieceIndex];
                    }
                    int pieceSquareIndex = 768 * isWhiteAsInt + (pieceIndex - 1) * 64 + BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
                    mg += pieceSquareValues[pieceSquareIndex] * scoreSide;
                    eg += pieceSquareValues[pieceSquareIndex + 384] * scoreSide;
                }
            }
            isWhiteAsInt++;
        }
        gamephase = Math.Min(24, gamephase);
        return material + ((mg * gamephase) + eg * (24 - gamephase)) / 24;
    }

    private int MoveOrderEvaulator(Move move)
    {
        return transpositionTable.TryGetValue(board.ZobristKey, out cachedValue) && move.Equals(cachedValue.Item3)
            ? 10000
            : move.IsCapture
                ? ((int)move.MovePieceType + (int)move.CapturePieceType) * 100
                : killer1.Equals(move)
                    ? 50
                    : killer2.Equals(move)
                        ? 40
                        : (int)move.MovePieceType;
    }

    private (Move, int) NegaMax(int lookAhead, int alpha, int beta, int color, bool ourMove, Timer timer, bool isQuietSearch)
    {
        // Simple timeout preventer, we stop processing further evaluations if more than a desired amount of time has passed
        // We also hijack this to stop processing if we are not losing badly, and are considering a repeated position
        timeout = timer.MillisecondsElapsedThisTurn * 10 > timer.MillisecondsRemaining;
        if (timeout || (ourMove && board.IsRepeatedPosition() && lastBestScore < -100))
            return (EMPTY_MOVE, 20000);

        if (transpositionTable.TryGetValue(board.ZobristKey, out cachedValue)
            && cachedValue.Item1 >= lookAhead
            && cachedValue.Item4 == 0)
            return (cachedValue.Item3, cachedValue.Item2);

        Move bestMove = EMPTY_MOVE;
        Span<Move> moves = stackalloc Move[220];
        board.GetLegalMovesNonAlloc(ref moves, isQuietSearch);

        int bestScore = isQuietSearch
                ? color * BoardValue()
                : moves.IsEmpty
                    ? ourMove
                        ? board.IsInCheckmate() ? -30200 - lookAhead : 30001
                        : -30100 - lookAhead
                    : -99999,
            flag = 0,
            KillerIndex = lookAhead * 2,
            moveIndex = 0;

        // the killer moves must be set correctly before sort is called
        killer1 = killers[KillerIndex];
        killer2 = killers[KillerIndex + 1];
        moves.Sort((A, B) => MoveOrderEvaulator(B) - MoveOrderEvaulator(A));

        while (moveIndex < moves.Length)
        {
            Move evaluatedMove = moves[moveIndex++];

            board.MakeMove(evaluatedMove);

            int eval = lookAhead <= 0
                    ? isQuietSearch
                        ? color * BoardValue()
                        : -NegaMax(3 + (searchDepth - 6) / 2, -beta, -alpha, -color, !ourMove, timer, true).Item2
                    : -NegaMax(lookAhead - 1, -beta, -alpha, -color, !ourMove, timer, isQuietSearch).Item2;
            if (eval > bestScore)
            {
                bestScore = eval;
                bestMove = evaluatedMove;
            }
            alpha = Math.Max(alpha, bestScore);

            board.UndoMove(evaluatedMove);

            if (alpha >= beta)
            {
                if (!evaluatedMove.IsCapture &&
                    !(killer1.Equals(evaluatedMove) || killer2.Equals(evaluatedMove)))
                {
                    killers[KillerIndex + 1] = killer1;
                    killers[KillerIndex] = evaluatedMove;
                }
                flag = 1;
                break;
            }
        }

        if (!isQuietSearch)
            transpositionTable[board.ZobristKey] = (lookAhead, bestScore, bestMove, flag);

        return (bestMove, bestScore);
    }
}