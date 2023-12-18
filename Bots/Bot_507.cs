namespace auto_Bot_507;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_507 : IChessBot
{
    Move moveToPlay;
    Board myBoard;
    int longCombo = 4;
    public Move Think(Board board, Timer timer)
    {
        myBoard = board;
        for (int i = 0; i < 256; i++)
            isolatedPawnsArray[i] = ((isolatedPawnsUlong[i / 32] >> 2 * (i % 32)) % 2 == 1 ? 2 : 0) +
                ((isolatedPawnsUlong[i / 32] >> 2 * (i % 32) + 1) % 2 == 1 ? 1 : 0);
        ThinkMove(1, 1, 6, timer.OpponentMillisecondsRemaining > timer.MillisecondsRemaining ? 11 : 12, -100000, 0, 0);
        return moveToPlay;
    }

    int ThinkMove(int depth, int realDepth, int captureOnlyDepth, int maxDepth, int alpha, int evaluationIfNullMove, int combo)
    {
        bool captureOnly = depth > captureOnlyDepth;
        bool isCheckPosition = myBoard.IsInCheck();
        bool captureOnlyAllMoves = captureOnly && combo < longCombo && !isCheckPosition;
        Move[] allMoves = myBoard.GetLegalMoves(captureOnlyAllMoves);

        int[] evaluationArray = new int[allMoves.Length];
        int newDepth = captureOnly ? depth + 1 : depth + 3;
        int initialCombo = combo;

        bool fewMoves = allMoves.Length < 5 && !captureOnlyAllMoves;
        int evaluation, mean = 0;
        int initialAlpha = -100000 + realDepth;
        int nextAlphaSort = initialAlpha;

        while (newDepth > depth)
        {
            int moveNumber = 0;
            nextAlphaSort = initialAlpha;
            bool breakLoop = false;
            bool bigDepthTry = newDepth == depth + 1;
            foreach (Move move in allMoves)
            {
                Piece captueredPiece = myBoard.GetPiece(move.TargetSquare);
                bool hasntCapturedPiece = captueredPiece.IsNull || captueredPiece.IsPawn;
                if ((captureOnly && initialCombo < longCombo && hasntCapturedPiece && !fewMoves) || breakLoop || (moveNumber > 3 + 3 * initialCombo && !captureOnly && bigDepthTry))
                {
                    evaluationArray[moveNumber] = 100000;
                    moveNumber++;
                    continue;
                }

                myBoard.MakeMove(move);
                evaluation = EvaluatePosition(!myBoard.IsWhiteToMove);
                combo = initialCombo;
                bool isCombo = myBoard.IsInCheck() || isCheckPosition || !hasntCapturedPiece || move.IsPromotion || (bigDepthTry && mean / allMoves.Length + evaluationArray[moveNumber] < -300);
                combo = isCombo ? combo + 1 : 0;
                bool goToNextLayer = isCombo || !captureOnly;
                evaluationArray[moveNumber] = newDepth < maxDepth && goToNextLayer ? ThinkMove(newDepth, realDepth + 1, captureOnlyDepth, maxDepth, bigDepthTry ? nextAlphaSort : initialAlpha, -evaluation, combo) : -evaluation;
                int newEvaluation = -evaluationArray[moveNumber];
                if (newEvaluation > nextAlphaSort)
                {
                    nextAlphaSort = newEvaluation;
                    if (realDepth == 1)
                        moveToPlay = move;
                }
                myBoard.UndoMove(move);

                if (!bigDepthTry && evaluationArray[moveNumber] < 10000)
                    mean += newEvaluation;

                if (newEvaluation >= -alpha && bigDepthTry)
                    breakLoop = true;

                moveNumber++;
            }
            if (newDepth == depth + 3)
                Array.Sort(evaluationArray, allMoves);
            newDepth -= 2;
        }
        nextAlphaSort = (captureOnly && !fewMoves) ? Math.Max(nextAlphaSort, evaluationIfNullMove) : (myBoard.IsDraw() ? 0 : nextAlphaSort);
        return nextAlphaSort;
    }

    int[] isolatedPawnsArray = new int[256];
    ulong[] isolatedPawnsUlong = new ulong[] { 587910611272599080ul, 587726523033562518ul, 12003886850071440790ul, 587726520384357928ul, 12003886850071440790ul, 12003972718136352637ul, 587910611272599080ul, 587726520384357928ul };
    ulong eightKnightMovesSquares = 66229406269440ul;
    ulong sixKnightMovesSquares = 16961350949551104ul;
    ulong fourKnightMovesSquares = 4342175383962075708ul;
    ulong threeKnightMovesSquares = 4792111478498951490ul;
    ulong rank7 = 71776119061217280ul;
    ulong rank6 = 280375465082880ul;
    ulong rank2 = 65280ul;
    ulong rank3 = 16711680ul;
    ulong maxUlong = ulong.MaxValue;

    int EvaluatePosition(bool fromWhitePOV)
    {
        ulong wKnightsBitboard = getBitboard(PieceType.Knight, true);
        ulong bKnightsBitboard = getBitboard(PieceType.Knight, false);
        ulong wBishopsBitboard = getBitboard(PieceType.Bishop, true);
        ulong bBishopsBitboard = getBitboard(PieceType.Bishop, false);
        ulong wRooksBitboard = getBitboard(PieceType.Rook, true);
        ulong bRooksBitboard = getBitboard(PieceType.Rook, false);
        ulong wQueensBitboard = getBitboard(PieceType.Queen, true);
        ulong bQueensBitboard = getBitboard(PieceType.Queen, false);
        ulong wPawnsBitboard = getBitboard(PieceType.Pawn, true);
        ulong bPawnsBitboard = getBitboard(PieceType.Pawn, false);

        ulong wDragPawnBitboard = wPawnsBitboard >> 8;
        ulong bDragPawnBitboard = bPawnsBitboard << 8;
        for (int i = 8; i < 65; i <<= 1)
        {
            wDragPawnBitboard |= wDragPawnBitboard >> i;
            bDragPawnBitboard |= bDragPawnBitboard << i;
        }
        int doublePawnsDifference = EvaluatePieceDifference(wDragPawnBitboard & wPawnsBitboard, bDragPawnBitboard & bPawnsBitboard, maxUlong);
        int isolatedPawnsDifference = isolatedPawnsArray[(int)(255ul & wDragPawnBitboard)] - isolatedPawnsArray[(int)((bDragPawnBitboard) >> 56)];
        int value =
            900 * EvaluatePieceDifference(wQueensBitboard, bQueensBitboard, maxUlong) +
            500 * EvaluatePieceDifference(wRooksBitboard, bRooksBitboard, maxUlong) +
            315 * EvaluatePieceDifference(wBishopsBitboard, bBishopsBitboard, maxUlong) +
            300 * EvaluatePieceDifference(wKnightsBitboard, bKnightsBitboard, maxUlong) +
            100 * EvaluatePieceDifference(wPawnsBitboard, bPawnsBitboard, maxUlong) +
            70 * (NumberOfBits(rank7 & wPawnsBitboard) - NumberOfBits(rank2 & bPawnsBitboard)) -
            35 * (doublePawnsDifference + isolatedPawnsDifference - NumberOfBits(rank6 & wPawnsBitboard) + NumberOfBits(rank3 & bPawnsBitboard)) +
            12 * EvaluatePieceDifference(wRooksBitboard | wQueensBitboard, bRooksBitboard | bQueensBitboard, ~(bDragPawnBitboard | wDragPawnBitboard)) +
            5 * (EvaluatePieceDifference(wDragPawnBitboard, bDragPawnBitboard, maxUlong) +
            EvaluatePiecePosition(wKnightsBitboard | wBishopsBitboard, bKnightsBitboard | bBishopsBitboard)) +
            EvaluatePiecePosition(wQueensBitboard | wRooksBitboard | wPawnsBitboard, bQueensBitboard | bRooksBitboard | bPawnsBitboard);
        int kingPos = EvaluatePiecePosition(getBitboard(PieceType.King, true), getBitboard(PieceType.King, false));
        value += NumberOfBits(myBoard.AllPiecesBitboard & ~wPawnsBitboard & ~bPawnsBitboard) < 8 ? kingPos : -4 * kingPos;
        value += myBoard.IsWhiteToMove ? 12 : -12;
        value = (int)(value * (2 - (NumberOfBits(myBoard.AllPiecesBitboard) + myBoard.FiftyMoveCounter) / 32f));
        return fromWhitePOV ? value : -value;
    }

    ulong getBitboard(PieceType pieceType, bool white) => myBoard.GetPieceBitboard(pieceType, white);
    int NumberOfBits(ulong number) => BitOperations.PopCount(number);
    int EvaluatePieceDifference(ulong wPieces, ulong bPieces, ulong restriction) => NumberOfBits(wPieces & restriction) - NumberOfBits(bPieces & restriction);
    int EvaluatePiecePosition(ulong wPieces, ulong bPieces) => 14 * EvaluatePieceDifference(wPieces, bPieces, eightKnightMovesSquares) +
        8 * EvaluatePieceDifference(wPieces, bPieces, sixKnightMovesSquares) + 3 * EvaluatePieceDifference(wPieces, bPieces, fourKnightMovesSquares) +
        EvaluatePieceDifference(wPieces, bPieces, threeKnightMovesSquares);
}
