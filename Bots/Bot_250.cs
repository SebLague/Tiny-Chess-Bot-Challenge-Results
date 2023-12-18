namespace auto_Bot_250;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_250 : IChessBot
{
    /* TODO How to save tokens (only implement once necessary)
        - removing braces {} from if body and for loops saves 1 token
        - ...
    */

    // struct Transposition
    // {
    //     // http://mediocrechess.blogspot.com/2007/01/guide-transposition-tables.html

    //     public ulong ZobristKey; // 8 bytes
    //     // Consider changing to "short" to save 2 bytes. For that to work, would need to
    //     // redefine our pieceValues, minValue, maxValue and pieceQualityLookupTables.
    //     // Probably easiest to divide by 10 and set maxValue to 20000.
    //     public int Evaluation; // 4 bytes
    //     public byte Depth, NodeType; // 1+1 bytes
    //     public Move Move; // 4 bytes
    //     // byte age // 0-255, could store plycount, used to determine when to override a position

    //     // 18 bytes, change evaluation to 'short' so we only need 16 bytes in total!
    //     public Transposition(ulong _zobristKey, int _evaluation, byte _depth, byte _nodeType, Move _move)
    //     {
    //         ZobristKey = _zobristKey;
    //         Evaluation = _evaluation;
    //         Depth = _depth;
    //         NodeType = _nodeType; // 0 = not set, 1 = exact, 2 = alpha, 3 = beta
    //         Move = _move;
    //     }
    // }

    // debugging
    // string gameLogsFilePath = "src/My Bot/gameLogs.txt"; //#DEBUG
    // string consoleLogsFilePath = "src/My Bot/consoleLogs.txt"; //#DEBUG
    // int evaluatedPositions = 0; //#DEBUG
    // int evaluatedPositionsTotal = 0; //#DEBUG
    // bool clearLogFile = true; //#DEBUG


    Move bestMoveRoot;
    // private static ulong transpositionMask = 0x1fff; // 0x1fff = 8192. Increase size later
    // Transposition[] transpositionTable = new Transposition[transpositionMask + 1];

    // Piece values: pawn, knight, bishop, rook, queen, king
    int[][] pieceQualityLookupTables = new int[12][];
    int[] pieceMaterialValues = { 100, 310, 325, 475, 900, 10000 };
    int[] piecePhaseValues = { 0, 1, 1, 2, 4, 0 };
    int minValue = -1000000;
    int maxValue = 1000000;
    int searchDepth = 4; // min depth = 1

    public Bot_250()
    {
        pieceQualityLookupTables = GetPieceQualityLookupTables();
        // evaluatedPositionsTotal = 0; //#DEBUG
        // PrintReadableLookupTablesAsHex();
    }


    public Move Think(Board board, Timer timer)
    {
        // evaluatedPositions = 0; //#DEBUG
        // if (clearLogFile) //#DEBUG
        // { //#DEBUG
        // clearLogFile = false; //#DEBUG
        // FileWriter.ClearFile(gameLogsFilePath); //#DEBUG
        // FileWriter.ClearFile(consoleLogsFilePath); //#DEBUG
        // } //#DEBUG
        // FileWriter.AppendToFile(gameLogsFilePath, "[Turn " + (board.PlyCount + 1) + " - Starting Search]\n"); //#DEBUG


        bestMoveRoot = Move.NullMove;
        int evalScore = ComputeBestMove(board, minValue, maxValue, searchDepth, 0);


        // evaluatedPositionsTotal += evaluatedPositions; //#DEBUG
        // DivertedConsole.Write("[" + board.PlyCount.ToString().PadLeft(3, ' ') + "] " + "# evaluated positions: " + evaluatedPositions.ToString().PadLeft(7, ' ') + "  Score: " + evalScore.ToString().PadLeft(6, ' ') + "    Total evaluations: " + evaluatedPositionsTotal.ToString().PadLeft(10, ' ')); //#DEBUG
        // FileWriter.AppendToFile(gameLogsFilePath, "[Turn " + (board.PlyCount + 1) + " Played " + bestMoveRoot + "  " + evalScore + "]\n\n\n\n"); //#DEBUG
        // // WriteTranspositionTableToFile(consoleLogsFilePath, board); //#DEBUG

        return bestMoveRoot;
    }

    int ComputeBestMove(Board board, int alpha, int beta, int depthLeft, int plyFromRoot)
    {

        if (depthLeft == 0)
        {
            // evaluatedPositions++; //#DEBUG // only counts leaf notes as evaluated positions, which is fine
            return QuiescenceSearch(board, alpha, beta);
        }

        // Move localBestMove = Move.NullMove;
        // ulong zobristKey = board.ZobristKey;
        // int alphaCopy = alpha;

        // Access transposition table
        // ref Transposition ttEntry = ref transpositionTable[zobristKey & transpositionMask];
        // if (ttEntry.ZobristKey == zobristKey && ttEntry.Depth >= depthLeft &&
        //     (ttEntry.NodeType == 1 //exact score
        //     || ttEntry.NodeType == 2 && ttEntry.Evaluation <= alpha // alpha cutoff (good move)
        //     || ttEntry.NodeType == 3 && ttEntry.Evaluation >= beta) // beta cutoff (bad move)
        // )
        // {
        //     // I don't think we need to set move here because it seems impossible to get here with "plyCount==0"
        //     return ttEntry.Evaluation;
        // }

        // TODO transposition code should be before Move[] moves code
        // Span<Move> moves = stackalloc Move[256];
        // board.GetLegalMovesNonAlloc(ref moves);
        Move[] moves = OrderMovesByScore(board, board.GetLegalMoves());
        int evaluationScore, localBestScore = minValue;

        foreach (Move move in moves)
        {
            // if (depthLeft >= 1 && plyFromRoot > 0 && move == moves[0]) //#DEBUG
            // { //#DEBUG
            // FileWriter.AppendToFile(gameLogsFilePath, new string(' ', plyFromRoot * 4) + "d" + plyFromRoot + "\n"); //#DEBUG
            // } //#DEBUG

            board.MakeMove(move);
            evaluationScore = board.IsInCheckmate() ? (maxValue / 2) - plyFromRoot : board.IsDraw() ? 0 : -ComputeBestMove(board, -beta, -alpha, depthLeft - 1, plyFromRoot + 1);
            board.UndoMove(move);

            // FileWriter.AppendToFile(gameLogsFilePath, new string(' ', (plyFromRoot + 1) * 4) + "d" + (plyFromRoot + 1) + " " + move + "  " + evaluationScore + ((plyFromRoot == 0) ? "\n" : "") + "\n"); //#DEBUG

            if (evaluationScore > localBestScore)
            {
                localBestScore = evaluationScore;
                // localBestMove = move;
            }

            if (evaluationScore >= beta)
            {
                // Pruning happens here. Basically ignore paths that are worse than current best path (for either player).
                break;
            }

            if (evaluationScore > alpha)
            {
                alpha = localBestScore = evaluationScore;
                if (plyFromRoot == 0)
                {
                    bestMoveRoot = move;
                }
            }
        }

        //
        // byte nodeType = (byte)(localBestScore >= beta ? 3 : (localBestScore > alphaCopy ? 2 : 1));

        // // insert move into transposition table. Don't use "new" to avoid wasting memory.
        // ttEntry.ZobristKey = zobristKey;
        // ttEntry.Evaluation = localBestScore;
        // ttEntry.Depth = (byte)depthLeft;
        // ttEntry.NodeType = nodeType;
        // ttEntry.Move = localBestMove;

        return localBestScore;
    }


    Move[] OrderMovesByScore(Board board, Move[] moves)
    {
        // Principal Variation (https://www.chessprogramming.org/Principal_Variation)
        // hash move (transposition table) https://www.chessprogramming.org/Transposition_Table

        // Check for captures
        // Moving piece to square attacked by pawn
        // Check for pawn promotion


        /*
            for (int square = 0; square < 64; ++square)
            {
                for (int piece = 0; piece < 6; ++piece)
                {
                    contention[square] += GetPieceAttacks((PieceType)piece, new Square(square), board, isWhiteFriendly);
                    contention[square] -= GetPieceAttacks((PieceType)piece, new Square(square), board, !isWhiteFriendly);
                }
            }
        */


        return moves.OrderByDescending(move => move.CapturePieceType).ThenBy(move => move.MovePieceType).ToArray();
    }


    int QuiescenceSearch(Board board, int alpha, int beta)
    {
        int currentEval = Evaluate(board);

        if (currentEval >= beta)
            return beta;
        alpha = Math.Max(alpha, currentEval);

        Move[] captureMoves = OrderMovesByScore(board, board.GetLegalMoves(true));
        foreach (Move move in captureMoves)
        {
            board.MakeMove(move);
            currentEval = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (currentEval >= beta)
                return beta;
            alpha = Math.Max(alpha, currentEval);
        }

        return alpha;
    }


    /// <summary>
    /// Returns evaluation of current position. Note that a positive evaluation can be good for both
    /// White and Black in respect to their turn. That is why "board.IsWhiteToMove ? 1 : -1" is needed.
    /// Check NegaMax (https://www.chessprogramming.org/Negamax) for more details.
    /// </summary>
    int Evaluate(Board board)
    {
        // Approach
        // + Raw material value // Piece values: 100, 300, 325, 500, 900, 10000 (p, N, B, R, Q, K)
        // + Piece Quality // Use piece square tables
        // TODO King Safety // Check how many pieces attack king's surroundings, and what value those pieces have.
        //               // https://www.chessprogramming.org/King_Safety
        // ? Center control // Potentially unnecessary. Could get bonus points if superior control on each of the 4 squares.
        // ? Rook // Good in endgame. Need open files or 2nd/7th rank to be strong.
        //        // Used for pawn protection/advancement, cutting of kings.
        //        // https://chessfox.com/the-rooks-their-strengths-and-weaknesses/


        PieceList[] pieceLists = board.GetAllPieceLists();

        int gamePhase = 0; // max value = 4+4+8+8 = 24
        for (int pieceIndex = 0; pieceIndex < 12; pieceIndex++)
        {
            gamePhase += pieceLists[pieceIndex].Count * piecePhaseValues[pieceIndex % 6];
        }

        int materialScore = 0;
        int midGamePieceQualityScore = 0;
        int endGamePieceQualityScore = 0;
        for (int pieceTypeIndex = 0; pieceTypeIndex < 12; pieceTypeIndex++)
        {
            bool isWhitePiece = pieceTypeIndex < 6;

            // Evaluate raw material value
            materialScore += pieceLists[pieceTypeIndex].Count * pieceMaterialValues[pieceTypeIndex % 6] * (isWhitePiece ? 1 : -1);

            // Evaluate piece quality
            foreach (Piece piece in pieceLists[pieceTypeIndex])
            {
                midGamePieceQualityScore += ReadLookupTable(pieceQualityLookupTables[pieceTypeIndex % 6], piece.Square, isWhitePiece) * (isWhitePiece ? 1 : -1);
                endGamePieceQualityScore += ReadLookupTable(pieceQualityLookupTables[6 + pieceTypeIndex % 6], piece.Square, isWhitePiece) * (isWhitePiece ? 1 : -1);
            }
        }

        return (board.IsWhiteToMove ? 1 : -1) * (materialScore + (midGamePieceQualityScore * gamePhase + endGamePieceQualityScore * (24 - gamePhase)) / 24);
        // return (materialScore + midGamePieceQualityScore) * (board.IsWhiteToMove ? 1 : -1);
    }


    int ReadLookupTable(int[] table, Square square, bool isColorWhite)
    {
        return isColorWhite ? table[8 * (7 - square.Rank) + square.File] : table[square.Index];
    }


    /// <summary>
    /// Returns all piece quality lookup tables. Computed by using ulong bits. 8 bits form a number,
    /// where the most significant bit denotes positive (0) and negative (1) values. Possible values
    /// range from 0x00 to 0xFF, which translates to -128 to 127.
    /// <returns></returns>
    int[][] GetPieceQualityLookupTables()
    {
        int[][] lookupTables = new int[12][];
        ulong[] lookupTablesRaw = {
            // values from https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function

            // [ 0] mid-game Pawn      
            0x_00_00_00_00_00_00_00_00,
            0x_31_43_1e_2f_22_3f_11_fb,
            0x_fd_03_0d_0f_20_1c_0c_f6,
            0x_f9_06_03_0a_0b_06_08_f5,
            0x_f3_ff_fe_06_08_03_05_f4,
            0x_f3_fe_fe_fb_01_01_10_fa,
            0x_ef_00_f6_f5_f9_0c_13_f5,
            0x_00_00_00_00_00_00_00_00,

            // [ 1] mid-game Knight
            0x_ad_d4_ef_e8_1e_d0_f9_cb,
            0x_dc_ec_24_12_0b_1f_03_f8,
            0x_e9_1e_12_20_2a_40_24_16,
            0x_fc_08_09_1a_12_22_09_0b,
            0x_fa_02_08_06_0e_09_0a_fc,
            0x_f5_fc_06_05_09_08_0c_f8,
            0x_f2_e6_fa_ff_00_09_f9_f7,
            0x_cc_f6_e3_f0_f8_f2_f7_f5,

            // [ 2] mid-game Bishop
            0x_f2_02_d7_ee_f4_eb_03_fc,
            0x_f3_08_f7_fa_0f_1d_09_e9,
            0x_f8_12_15_14_11_19_12_ff,
            0x_fe_02_09_19_12_12_03_ff,
            0x_fd_06_06_0d_11_06_05_02,
            0x_00_07_07_07_07_0d_09_05,
            0x_02_07_08_00_03_0a_10_00,
            0x_f0_ff_f9_f6_fa_fa_ed_f6,

            // [ 3] mid-game Rook
            0x_10_15_10_19_1f_04_0f_15,
            0x_0d_10_1d_1f_28_21_0d_16,
            0x_fe_09_0d_12_08_16_1e_08,
            0x_f4_fb_03_0d_0c_11_fc_f6,
            0x_ee_f3_fa_00_04_fd_03_f5,
            0x_ea_f4_f8_f8_01_00_fe_f0,
            0x_ea_f8_f6_fc_00_05_fd_dd,
            0x_f7_fa_00_08_08_03_ee_f3,

            // [ 4] mid-game Queen
            0x_f2_00_0e_06_1d_16_15_16,
            0x_f4_ed_fe_00_f8_1c_0e_1b,
            0x_fa_f8_03_04_0e_1c_17_1c,
            0x_f3_f3_f8_f8_00_08_ff_00,
            0x_fc_f3_fc_fb_ff_fe_01_ff,
            0x_f9_01_fb_ff_fe_01_07_02,
            0x_ef_fc_05_01_04_07_ff_00,
            0x_00_f7_fc_05_f9_f4_f1_e7,

            // [ 5] mid-game King
            0x_bf_17_10_f1_c8_de_02_0d,
            0x_1d_ff_ec_f9_f8_fc_da_e3,
            0x_f7_18_02_f0_ec_06_16_ea,
            0x_ef_ec_f4_e5_e2_e7_f2_dc,
            0x_cf_ff_e5_d9_d2_d4_df_cd,
            0x_f2_f2_ea_d2_d4_e2_f1_e5,
            0x_01_07_f8_c0_d5_f0_09_08,
            0x_f1_24_0c_ca_08_e4_18_0e,

            // [ 6] end-game Pawn
            0x_00_00_00_00_00_00_00_00,
            0x_59_56_4f_43_49_42_52_5d,
            0x_2f_32_2a_21_1c_1a_29_2a,
            0x_10_0c_06_02_ff_02_08_08,
            0x_06_04_ff_fd_fd_fc_01_00,
            0x_02_03_fd_00_00_fe_00_fc,
            0x_06_04_04_05_06_00_01_fd,
            0x_00_00_00_00_00_00_00_00,

            // [ 7] end-game Knight
            0x_e3_ed_fa_f2_f1_f3_e1_cf,
            0x_f4_fc_f4_ff_fc_f4_f4_e6,
            0x_f4_f6_05_04_00_fc_f7_ec,
            0x_f8_01_0b_0b_0b_05_04_f7,
            0x_f7_fd_08_0c_08_08_02_f7,
            0x_f5_ff_00_07_05_ff_f6_f5,
            0x_eb_f6_fb_fe_ff_f6_f5_ea,
            0x_f2_e7_f5_f9_f5_f7_e7_e0,

            // [ 8] end-game Bishop
            0x_f9_f6_fb_fc_fd_fc_f8_f4,
            0x_fc_fe_03_fa_ff_fa_fe_f9,
            0x_01_fc_00_00_ff_03_00_02,
            0x_ff_04_06_04_07_05_01_01,
            0x_fd_01_06_09_03_05_ff_fc,
            0x_fa_ff_04_05_06_01_fd_f9,
            0x_f9_f7_fd_00_02_fc_f9_f3,
            0x_f5_fc_f5_fe_fc_f8_fe_f8,

            // [ 9] end-game Rook
            0x_06_05_09_07_06_06_04_02,
            0x_05_06_06_05_ff_01_04_01,
            0x_03_03_03_02_02_ff_fe_ff,
            0x_02_01_06_00_01_00_00_01,
            0x_01_02_04_02_fe_fd_fc_fb,
            0x_fe_00_fe_00_fd_fa_fc_f8,
            0x_fd_fd_00_01_fc_fc_fb_ff,
            0x_fc_01_01_00_fe_fa_02_f6,

            // [10] end-game Queen
            0x_fc_0b_0b_0d_0d_09_05_0a,
            0x_f8_0a_10_14_1d_0c_0f_00,
            0x_f6_03_04_18_17_11_09_04,
            0x_01_0b_0c_16_1c_14_1c_12,
            0x_f7_0e_09_17_0f_11_13_0b,
            0x_f8_f3_07_03_04_08_05_02,
            0x_f5_f5_f1_f8_f8_f5_ee_f0,
            0x_f0_f2_f5_eb_fe_f0_f6_ec,

            // [11] end-game King
            0x_db_ef_f7_f7_fb_07_02_f8,
            0x_fa_08_07_08_08_13_0b_05,
            0x_05_08_0b_07_0a_16_16_06,
            0x_fc_0b_0c_0d_0d_10_0d_01,
            0x_f7_fe_0a_0c_0d_0b_04_fb,
            0x_f7_ff_05_0a_0b_08_03_fc,
            0x_f3_fb_02_06_07_02_fe_f8,
            0x_e6_ef_f6_fb_f2_f9_f4_eb,
        };

        for (int lookupTableIndex = 0; lookupTableIndex < 12; lookupTableIndex++)
        {
            // Since lookupTablesRaw is a 1D array, slice correct index: lookupTablesRaw[0..8], [9..16], etc.
            lookupTables[lookupTableIndex] = MapUlongToLookupTable(lookupTablesRaw[(lookupTableIndex * 8)..(lookupTableIndex * 8 + 8)]);
        }

        return lookupTables;
    }

    int[] MapUlongToLookupTable(ulong[] squareTableRows)
    {
        int[] lookupTable = new int[64];
        int rowCounter = -1;

        foreach (ulong row in squareTableRows)
        {
            rowCounter++;
            for (int i = 0; i < 8; i++)
            {
                lookupTable[rowCounter * 8 + i] = GetNumberOfUlongBits(row, i * 2);
            }
        }
        return lookupTable;
    }

    /// <summary>
    /// By shifting bits, the number of the specified position is accessed and returned.
    /// </summary>
    int GetNumberOfUlongBits(ulong number, int position)
    {
        // Set all bits to the left to 0, then set all bits to the right to 0, 8 bits remain "untouched".
        number <<= position * 4;
        number >>= 14 * 4;
        return (sbyte)number;
    }

    // // void WriteTranspositionTableToFile(string filePath, Board board) //#DEBUG
    // // { //#DEBUG
    //     string dataToWrite = "Turn " + board.PlyCount + "\n"; //DEBUG
    // //     foreach (Transposition transposition in transpositionTable) //#DEBUG
    // //     { //#DEBUG
    //         if (transposition.NodeType == 0)
    // //         { //#DEBUG
    // //             continue; //#DEBUG
    // //         } //#DEBUG 
    // //         dataToWrite += "[" + (transposition.ZobristKey & transpositionMask).ToString().PadLeft(8, '.') + "] "; //#DEBUG
    // //         dataToWrite += "Zobrist key: " + transposition.ZobristKey.ToString().PadLeft(20, ' '); //#DEBUG
    // //         dataToWrite += "   Best " + transposition.Move + "   " + transposition.Evaluation.ToString().PadLeft(5, ' '); //#DEBUG
    // //         dataToWrite += "   d" + transposition.Depth.ToString().PadLeft(2, '0') + "   Node: " + transposition.NodeType + "\n"; //#DEBUG
    // //     } //#DEBUG
    //     dataToWrite += "\n\n\n\n\n";
    // //     FileWriter.AppendToFile(filePath, dataToWrite); //#DEBUG
    // // } //#DEBUG



    // void PrintReadableLookupTablesAsHex()
    // {
    //     sbyte[] mgPawn = {
    //           0,   0,   0,   0,   0,   0,   0,   0,
    //          49,  67,  30,  47,  34,  63,  17,  -5,
    //          -3,   3,  13,  15,  32,  28,  12, -10,
    //          -7,   6,   3,  10,  11,   6,   8, -11,
    //         -13,  -1,  -2,   6,   8,   3,   5, -12,
    //         -13,  -2,  -2,  -5,   1,   1,  16,  -6,
    //         -17,   0, -10, -11,  -7,  12,  19, -11,
    //           0,   0,   0,   0,   0,   0,   0,   0,
    //     };

    //     sbyte[] mgKnight = {
    //         -83, -44, -17, -24,  30, -48,  -7, -53,
    //         -36, -20,  36,  18,  11,  31,   3,  -8,
    //         -23,  30,  18,  32,  42,  64,  36,  22,
    //          -4,   8,   9,  26,  18,  34,   9,  11,
    //          -6,   2,   8,   6,  14,   9,  10,  -4,
    //         -11,  -4,   6,   5,   9,   8,  12,  -8,
    //         -14, -26,  -6,  -1,   0,   9,  -7,  -9,
    //         -52, -10, -29, -16,  -8, -14,  -9, -11,
    //     };

    //     sbyte[] mgBishop = {
    //         -14,   2, -41, -18, -12, -21,   3,  -4,
    //         -13,   8,  -9,  -6,  15,  29,   9, -23,
    //          -8,  18,  21,  20,  17,  25,  18,  -1,
    //          -2,   2,   9,  25,  18,  18,   3,  -1,
    //          -3,   6,   6,  13,  17,   6,   5,   2,
    //           0,   7,   7,   7,   7,  13,   9,   5,
    //           2,   7,   8,   0,   3,  10,  16,   0,
    //         -16,  -1,  -7, -10,  -6,  -6, -19, -10,
    //     };

    //     sbyte[] mgRook = {
    //          16,  21,  16,  25,  31,   4,  15,  21,
    //          13,  16,  29,  31,  40,  33,  13,  22,
    //          -2,   9,  13,  18,   8,  22,  30,   8,
    //         -12,  -5,   3,  13,  12,  17,  -4, -10,
    //         -18, -13,  -6,   0,   4,  -3,   3, -11,
    //         -22, -12,  -8,  -8,   1,   0,  -2, -16,
    //         -22,  -8, -10,  -4,   0,   5,  -3, -35,
    //          -9,  -6,   0,   8,   8,   3, -18, -13,
    //     };

    //     sbyte[] mgQueen = {
    //         -14,   0,  14,   6,  29,  22,  21,  22,
    //         -12, -19,  -2,   0,  -8,  28,  14,  27,
    //          -6,  -8,   3,   4,  14,  28,  23,  28,
    //         -13, -13,  -8,  -8,   0,   8,  -1,   0,
    //          -4, -13,  -4,  -5,  -1,  -2,   1,  -1,
    //          -7,   1,  -5,  -1,  -2,   1,   7,   2,
    //         -17,  -4,   5,   1,   4,   7,  -1,   0,
    //           0,  -9,  -4,   5,  -7, -12, -15, -25,
    //     };

    //     sbyte[] mgKing = {
    //         -65,  23,  16, -15, -56, -34,   2,  13,
    //         29,  -1, -20,  -7,  -8,  -4, -38, -29,
    //         -9,  24,   2, -16, -20,   6,  22, -22,
    //         -17, -20, -12, -27, -30, -25, -14, -36,
    //         -49,  -1, -27, -39, -46, -44, -33, -51,
    //         -14, -14, -22, -46, -44, -30, -15, -27,
    //         1,   7,  -8, -64, -43, -16,   9,   8,
    //         -15,  36,  12, -54,   8, -28,  24,  14,
    //     };




    //     sbyte[] egPawn = {
    //          0,   0,   0,   0,   0,   0,   0,   0,
    //         89,  86,  79,  67,  73,  66,  82,  93,
    //         47,  50,  42,  33,  28,  26,  41,  42,
    //         16,  12,   6,   2,  -1,   2,   8,   8,
    //          6,   4,  -1,  -3,  -3,  -4,   1,   0,
    //          2,   3,  -3,   0,   0,  -2,   0,  -4,
    //          6,   4,   4,   5,   6,   0,   1,  -3,
    //          0,   0,   0,   0,   0,   0,   0,   0,
    //     };

    //     sbyte[] egKnight = {
    //         -29, -19,  -6, -14, -15, -13, -31, -49,
    //         -12,  -4, -12,  -1,  -4, -12, -12, -26,
    //         -12, -10,   5,   4,   0,  -4,  -9, -20,
    //          -8,   1,  11,  11,  11,   5,   4,  -9,
    //          -9,  -3,   8,  12,   8,   8,   2,  -9,
    //         -11,  -1,   0,   7,   5,  -1, -10, -11,
    //         -21, -10,  -5,  -2,  -1, -10, -11, -22,
    //         -14, -25, -11,  -7, -11,  -9, -25, -32,
    //     };

    //     sbyte[] egBishop = {
    //          -7, -10,  -5,  -4,  -3,  -4,  -8, -12,
    //          -4,  -2,   3,  -6,  -1,  -6,  -2,  -7,
    //           1,  -4,   0,   0,  -1,   3,   0,   2,
    //          -1,   4,   6,   4,   7,   5,   1,   1,
    //          -3,   1,   6,   9,   3,   5,  -1,  -4,
    //          -6,  -1,   4,   5,   6,   1,  -3,  -7,
    //          -7,  -9,  -3,   0,   2,  -4,  -7, -13,
    //         -11,  -4, -11,  -2,  -4,  -8,  -2,  -8,
    //     };

    //     sbyte[] egRook = {
    //          6,   5,   9,   7,   6,   6,   4,   2,
    //          5,   6,   6,   5,  -1,   1,   4,   1,
    //          3,   3,   3,   2,   2,  -1,  -2,  -1,
    //          2,   1,   6,   0,   1,   0,   0,   1,
    //          1,   2,   4,   2,  -2,  -3,  -4,  -5,
    //         -2,   0,  -2,   0,  -3,  -6,  -4,  -8,
    //         -3,  -3,   0,   1,  -4,  -4,  -5,  -1,
    //         -4,   1,   1,   0,  -2,  -6,   2, -10,
    //     };

    //     sbyte[] egQueen = {
    //          -4,  11,  11,  13,  13,   9,   5,  10,
    //          -8,  10,  16,  20,  29,  12,  15,   0,
    //         -10,   3,   4,  24,  23,  17,   9,   4,
    //           1,  11,  12,  22,  28,  20,  28,  18,
    //          -9,  14,   9,  23,  15,  17,  19,  11,
    //          -8, -13,   7,   3,   4,   8,   5,   2,
    //         -11, -11, -15,  -8,  -8, -11, -18, -16,
    //         -16, -14, -11, -21,  -2, -16, -10, -20,
    //     };

    //     sbyte[] egKing = {
    //         -37, -17,  -9,  -9,  -5,   7,   2,  -8,
    //          -6,   8,   7,   8,   8,  19,  11,   5,
    //           5,   8,  11,   7,  10,  22,  22,   6,
    //          -4,  11,  12,  13,  13,  16,  13,   1,
    //          -9,  -2,  10,  12,  13,  11,   4,  -5,
    //          -9,  -1,   5,  10,  11,   8,   3,  -4,
    //         -13,  -5,   2,   6,   7,   2,  -2,  -8,
    //         -26, -17, -10,  -5, -14,  -7, -12, -21,
    //     };


    //     DivertedConsole.Write("// [ 0] mid-game Pawn\n" + ConvertIntToHex(mgPawn) + "\n");
    //     DivertedConsole.Write("// [ 1] mid-game Knight\n" + ConvertIntToHex(mgKnight) + "\n");
    //     DivertedConsole.Write("// [ 2] mid-game Bishop\n" + ConvertIntToHex(mgBishop) + "\n");
    //     DivertedConsole.Write("// [ 3] mid-game Rook\n" + ConvertIntToHex(mgRook) + "\n");
    //     DivertedConsole.Write("// [ 4] mid-game Queen\n" + ConvertIntToHex(mgQueen) + "\n");
    //     DivertedConsole.Write("// [ 5] mid-game King\n" + ConvertIntToHex(mgKing) + "\n");

    //     DivertedConsole.Write("// [ 6] end-game Pawn\n" + ConvertIntToHex(egPawn) + "\n");
    //     DivertedConsole.Write("// [ 7] end-game Knight\n" + ConvertIntToHex(egKnight) + "\n");
    //     DivertedConsole.Write("// [ 8] end-game Bishop\n" + ConvertIntToHex(egBishop) + "\n");
    //     DivertedConsole.Write("// [ 9] end-game Rook\n" + ConvertIntToHex(egRook) + "\n");
    //     DivertedConsole.Write("// [10] end-game Queen\n" + ConvertIntToHex(egQueen) + "\n");
    //     DivertedConsole.Write("// [11] end-game King\n" + ConvertIntToHex(egKing) + "\n");

    //     // use this code to divide or multiply all values of a piece square table
    //     int[] test = {
    // -74, -35, -18, -18, -11,  15,   4, -17,
    // -12,  17,  14,  17,  17,  38,  23,  11,
    //  10,  17,  23,  15,  20,  45,  44,  13,
    //  -8,  22,  24,  27,  26,  33,  26,   3,
    // -18,  -4,  21,  24,  27,  23,   9, -11,
    // -19,  -3,  11,  21,  23,  16,   7,  -9,
    // -27, -11,   4,  13,  14,   4,  -5, -17,
    // -53, -34, -21, -11, -28, -14, -24, -43
    //     };

    //     string asString = "";
    //     for (int j = 0; j < 64; j++)
    //     {
    //         asString += ((int)(test[j] / (float)2.0)).ToString().PadLeft(3, ' ') + ", "; // divide or multiply here
    //         asString += j % 8 == 7 ? "\n" : "";
    //     }
    //     DivertedConsole.Write("\n\n" + asString + "\n\n");
    // }

    // string ConvertIntToHex(sbyte[] values)
    // {
    //     string hexString = "0x";
    //     for (int index = 0; index < 64; index++)
    //     {
    //         hexString += "_" + values[index].ToString("x").PadLeft(2, '0');
    //         hexString += index % 8 == 7 && index != 63 ? ",\n0x" : "";
    //     }
    //     return hexString + ",";
    // }

}