namespace auto_Bot_280;
using ChessChallenge.API;
using System;
/*
This was my first time doing any development in C# and it has been a fun challenge!

I'm not evaluating the board in my search function, but only the moves - hence the "Delta" in the bot-name!
I do a simple evaluation of the board before the searching (so the bot will know if it should accept a draw or not) but then only evalute every move as it occur in search. I do this to try saving time and also to sort the moves before "making" the move when searching.

I've made a custom way to save position values for all the pieces packed in ulongs, I do this to save tokens. The array will be unpacked before the bot starts moving.

The bot uses iterative deepening, and if the same move is occurring on top (with at least a pawn value better evaluation than the next-best move) four times in a row, the bot considers the move "obvious" and returns it immediately to save time.

The bot also has a small opening book of four moves!
 */

public class Bot_280 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 88, 309, 331, 495, 981, 0 },
        positionValues = UnpackPositionValues( // Created by PackNumbers
            new ulong[] {
                0xCBC2DDC3EFEE7777, 0x777729C146C2C8A1,
                0x9BDDEEEEFFFF7777, 0x7777CAA984A534BB,

                0xDDCBEEE5DE20B000, 0x10205A11CCB2CCC3,
                0xCCB2A82131211200, 0x21004220C521CC62,

                0xEDA4DED3BCC110A2, 0x2221ACDACCCADCC6,
                0xCBA65A4935433322, 0x32329322CB42CC73,

                0xDC32DDDBEEDDECDD, 0xCA1144303321A331,
                0x9B8AA989ABBBCCBB, 0x5492443443436854,

                0x3822CDCC3D3CDDCB, 0x4211AC3244B43333,
                0xEDDCECC3EDD3DCCB, 0x11112111BC33DDDA,

                0x12212AC2322723C1, 0x73D603BA01221120,
                0xDDC4CDCACCB62510, 0x2210CA31CC92DCA2
            }
        );

    int timelimit, gameProgress;

    // Used to unpack the positional values of pieces
    static int[] UnpackPositionValues(ulong[] packedValue)
    {
        int[] unpackedNumbers = new int[1536];
        for (int n = 0; n < 24; n += 2)
            for (int a = 0; a < 2; a++)
                for (int c = 0, outcount = 32 * a; outcount < 32 * a + 32; outcount += 8, c += 4)
                    for (int i = 0; i < 4; i++)
                    {
                        int value = (byte)(packedValue[n + a] >> (c + i) * 4 & 0xF) - 7;
                        unpackedNumbers[32 * n + outcount + i] = unpackedNumbers[32 * n + outcount + 7 - i] = (int)(Math.Sign(value) * Math.Pow(2, Math.Abs(value) - 1));
                    }
        return unpackedNumbers;
    }


    public Move Think(Board board, Timer timer)
    {
        //DivertedConsole.Write("0x{0:X16}", board.ZobristKey); //#DEBUG

        timelimit = timer.MillisecondsRemaining / 25;
        Move[] allMoves = board.GetLegalMoves();

        // Forced move
        if (allMoves.Length == 1)
            return allMoves[0];

        // "Opening book"
        switch (board.ZobristKey)
        {
            case 0xB792D82D18345F3A: return allMoves[16]; // e4 as first move
            case 0xD8985F343B028184: return allMoves[14]; // c5 in response to e4
            case 0xC13102B0DAFB9DDE: return allMoves[15]; // d5 in response to d4
            case 0x78375378303A90C4: return allMoves[2]; // Nf6 in response to Nf3
        }

        PieceList[] allPieceLists = board.GetAllPieceLists();
        bool isWhiteToMove = board.IsWhiteToMove;

        // Count all pieces to get the progress from opening to end-game
        gameProgress = 0;
        foreach (PieceList pieceList in allPieceLists)
            foreach (Piece piece in pieceList)
                if (piece.PieceType != PieceType.Pawn)
                    gameProgress += pieceValues[(int)piece.PieceType];

        // Calculate the start evaluation, negative is good
        int negBaseEval = 0, sameMoveCount = 0;
        foreach (PieceList pieceList in allPieceLists)
            foreach (Piece piece in pieceList)
                negBaseEval += (piece.IsWhite == isWhiteToMove ? -1 : 1) * PiecePositionalValue(piece.PieceType, piece.Square.Index, piece.IsWhite); // value and positional value of piece


        int[] result = new int[allMoves.Length];
        Move moveToMake = Move.NullMove;

        // Start iterative deepening search
        for (int orgDepth = 0; ; orgDepth++)
        {

            int negAlpha = 999999, bestResult = 999999, i = 0;
            foreach (Move move in allMoves)
            {

                board.MakeMove(move);
                result[i] = Search(board, orgDepth, negBaseEval - EvaluateMove(move, isWhiteToMove), -999999, negAlpha, false, !isWhiteToMove);
                board.UndoMove(move);

                if (result[i] < bestResult)
                {
                    bestResult = result[i];
                    sameMoveCount += move.Equals(moveToMake) ? 1 : -sameMoveCount; // Token-saving: if(move.Equals(moveToMake)) sameMoveCount++; else sameMoveCount = 0;
                    moveToMake = move;
                }

                if (timer.MillisecondsElapsedThisTurn >= timelimit && i > 0)
                    return moveToMake;

                negAlpha = Math.Min(negAlpha, result[i++]);
            }
            // Sort the evaluation for next iteration, low is best
            Array.Sort(result, allMoves);

            // If a move seems obvious or we doesn't have time to search deeper, make it now
            if (sameMoveCount > 3 && result[1] - result[0] > 100 || 3 * timer.MillisecondsElapsedThisTurn >= timelimit)
                return moveToMake;
        }
    }

    // Main negamax search routine
    int Search(Board board, int depth, int value, int alpha, int beta, bool onlyCaptures, bool isWhite)
    {

        if (board.IsRepeatedPosition()) // The rest of draw positions are discovered after getting the moves
            return 0;

        Move[] allMoves = board.GetLegalMoves(onlyCaptures);

        if (allMoves.Length == 0)
            return board.IsInCheckmate() ? -888000 - depth :
                board.IsDraw() ? 0 : value;

        if (depth == 0 && board.IsInCheck())
            depth = 1; // Search deeper when in check

        int[] result = new int[allMoves.Length];
        int i = 0;

        // Evalutaion for each move, to make the best move first
        foreach (Move move in allMoves)
        {
            result[i++] = -value - EvaluateMove(move, isWhite) + (
                board.SquareIsAttackedByOpponent(move.TargetSquare) ?
                pieceValues[(int)move.MovePieceType] / 2 : 0
            );
            if (depth == 0 && move.IsCapture)
                onlyCaptures = true;
        }


        if (depth > 0 || onlyCaptures)
        {

            if (depth <= 0)
            {
                if (value > beta)
                    return value;
                alpha = Math.Max(alpha, value);
            }
            else
                value = -5555555;


            Array.Sort(result, allMoves); // Sort the moves with best (lowest) first 
            i = 0;

            foreach (Move move in allMoves)

                if (depth > 0 || move.IsCapture)
                {
                    result[i] -= board.SquareIsAttackedByOpponent(move.TargetSquare) ?
                        pieceValues[(int)move.MovePieceType] / 2 : 0;

                    board.MakeMove(move);
                    value = Math.Max(value, -Search(board, depth - 1, result[i++], -beta, -alpha, onlyCaptures, !isWhite));
                    board.UndoMove(move);

                    alpha = Math.Max(alpha, value);

                    if (alpha >= beta)
                        break;
                }
            //end foreach
        }
        return value;
    }

    // Evaluate the move
    int EvaluateMove(Move move, bool isWhite)
    {

        int eval = -PiecePositionalValue(move.MovePieceType, move.StartSquare.Index, isWhite); // remove value of moved piece startsquare

        if (move.IsPromotion)
            eval += PiecePositionalValue(move.PromotionPieceType, move.TargetSquare.Index, isWhite); // add value of promotion piece
        else
            eval += PiecePositionalValue(move.MovePieceType, move.TargetSquare.Index, isWhite); //  add value of moved piece targetsquare

        if (move.IsEnPassant)
            eval += PiecePositionalValue(move.CapturePieceType, 8 * move.StartSquare.Rank + move.TargetSquare.File, !isWhite); // add value of enpassant captured pawn
        else if (move.IsCapture)
            eval += PiecePositionalValue(move.CapturePieceType, move.TargetSquare.Index, !isWhite); // add value of captured piece
        else if (move.IsCastles)
            eval += PiecePositionalValue(PieceType.Rook, 8 * move.StartSquare.Rank + (move.TargetSquare.File == 2 ? 3 : 5), isWhite)  //  add value of rook endsquare
                  - PiecePositionalValue(PieceType.Rook, 8 * move.StartSquare.Rank + (move.TargetSquare.File == 2 ? 0 : 7), isWhite); //  remove value of rook startsquare          

        return eval;
    }

    // The value of a piece on a specific square depending on game progress
    int PiecePositionalValue(PieceType pieceType, int index, bool isWhite)
    {
        int modifiedIndex = 128 * ((int)pieceType - 1) + (isWhite ? 63 - index : index);
        return pieceValues[(int)pieceType] + (gameProgress * positionValues[modifiedIndex] + (6000 - gameProgress) * positionValues[modifiedIndex + 64]) / 6000;
    }
}

/*
// Create the packed board, use values from -64 up to 128
//
int[][] allBoards = {
new int[] { // pawn start
      0,   0,   0,   0,
     64,  64, 128,  64,
     -8,  16,  32,  32,
    -16,  16,   8,  16,
    -32,   4,   1,  16,
    -16,  16,  -1,  -4,
    -32,  16,   2, -16,
      0,   0,   0,   0
},
new int[] { // pawn end
      0,   0,   0,   0,
    128, 128, 128, 128,
     64,  64,  64,  64,
     32,  32,   8,   2,
      8,   8,  -4,  -8,
     -2,   4,  -4,   1,
      2,   4,   4,  16,
      0,   0,   0,   0
},
new int[] { // knight start
    -64, -64, -64,   8,
    -64, -16,  64,  32,
     -2,  64,  64,  64,
      8,  16,  32,  32,
     -8,  16,  16,  16,
    -16,   8,  16,  16,
    -32, -32,   4,  -2,
    -64, -16, -64, -32
},
new int[] { // knight end
    -64, -64, -16, -32,
    -32, -16, -32,  -8,
    -32, -16,   1,   4,
    -16,   8,  16,  16,
    -16,  -1,  16,  16,
    -32, -16,  -2,  16,
    -64, -16, -16,  -4,
    -64, -64, -32, -16
},
new int[] { // bishop start
    -16,   4, -64, -32,
    -32,  16,  16,   8,
     -8,  32,  64,  32,
     -4,   4,  32,  64,
     -1,  16,  16,  32,
      4,  16,  16,  16,
      4,  32,  16,   4,
    -32, -16, -16, -16
},
new int[] { // bishop end
    -16, -16,  -8,  -8,
     -8,  -4,  -2,  -8,
      2,  -4,   4,  -2,
     -1,   4,   8,  16,
     -8,   0,  16,  16,
    -16,  -4,   8,  16,
    -16, -16,  -8,   2,
    -16,  -8, -16,  -8
},
new int[] {// rock start
     32,  32,  16,  64,
     32,  32,  64,  64,
      8,  32,  32,  32,
    -16,  -8,  16,  32,
    -32,  -8,  -8,   4,
    -32, -16,  -8,  -8,
    -64,  -8,  -4,  -4,
    -32, -32,   4,  16,
},
new int[] { // rock end
      8,   8,  16,  16,
      8,   8,   8,   4,
      2,   1,   2,   4,
      4,   1,   8,   2,
     -4,  -2,   1,  -1,
     -8,  -4,  -8,  -4,
     -4,  -8,  -4,  -4,
    -16,   2,  -4,  -2
},
new int[] { // queen start
      8,  16,  32,  32,
     16,  -8,  32,  -8,
     16,  16,  32,  16,
    -16, -16,   1,  -8,
     -8,  -8,  -8,  -8,
     -4,   8,  -4,  -4,
    -16,  -8,  16,   4,
    -32, -32, -16,  -4,
},
new int[] { // queen end
      8,  16,  16,  32,
     -8,  32,  32,  64,
     -8,  16,  16,  64,
     16,  32,  32,  64,
      4,  32,  32,  32,
     -8,  -8,  16,   8,
    -32, -32, -32, -16,
    -32, -32, -32, -32
},
new int[] { // king start
    -32,  16,  -8, -16,
      0, -16, -16,  -8,
    -16,  16,   4, -16,
    -32, -16, -16, -32,
    -64, -16, -32, -32,
    -16, -16, -32, -64,
      4,   8,  -8, -64,
     -1,  32,  -8,   0
},
new int[] { // king end
    -64, -32,  -2, -16,
     -1,   8,  16,  16,
      4,  16,  32,  16,
     -4,  16,  32,  32,
    -16,   4,  16,  32,
    -16,   2,  16,  16,
    -32,  -8,   4,  16,
    -64, -32, -16, -16,
}
};
int count = 0;
foreach ( int[] orgboard in allBoards ) { 
    byte[][] numboard = {
        new byte[16],
        new byte[16]
    };

    int adjuster = 7;

    for ( int c = 0; c < 2; c++ ) {
        for ( int j = 0; j < 16; j++ ) {
            int num = orgboard[j + 16*c];
            if ( num == 0 ) {
                numboard[c][j] = (byte)(num + adjuster);
            } else {
                numboard[c][j] = (byte)(Math.Sign(num) * Math.Log(Math.Abs(num)) / Math.Log(2) + Math.Sign(num) + adjuster);
            }
        }
    }

    ulong[] packed = {
        PackNumbers(numboard[0]),
        PackNumbers(numboard[1])
    };

    DivertedConsole.Write("0x{0:X16}, ", packed[0]);
    DivertedConsole.Write("0x{0:X16},", packed[1]);
    if ( count++ % 2 == 1 ) DivertedConsole.Write("");
}



static ulong PackNumbers(byte[] numbers) {
    if ( numbers.Length != 16 )
        throw new ArgumentException("The array must contain exactly 16 numbers.");

    ulong packedValue = 0;
    for ( int i = 0; i < 16; i++ ) {
        packedValue |= (ulong)numbers[i] << (i * 4);
    }
    return packedValue;
}

*/