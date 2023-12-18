namespace auto_Bot_579;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_579 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    List<int[]> pieceAdjustments;

    bool isWhite;
    Move moveToPlay;
    int depth;
    Board boardRef;
    Timer timerRef;
    int maxTime;

    //Chess Piece Square Tables Bit Keys
    //Instead of using a 2D array, we can use 2 ulongs to represent a mirrored table
    //(0000) (-50)
    //(0001) (-40)
    //(0010) (-30)
    //(0011) (-20)
    //(0100) (-10)
    //(0101) (-5)
    //(0110) (0)
    //(0111) (5)
    //(1000) (10)
    //(1001) (15)
    //(1010) (20)
    //(1011) (25)
    //(1100) (30)
    //(1101) (40)
    //(1110) (50)
    //Not using negatives here saves like 5 tokens (just subtract 50 when using)
    int[] adjustmentValues = { 0, 10, 20, 30, 40, 45, 50, 55, 60, 65, 70, 75, 80, 90, 100 };

    //Create Piece Square Adjustment Table on Initialization
    public Bot_579()
    {
        pieceAdjustments = new List<int[]>(){new int[]{0}, //Blank
        GetPieceAdjustments(new ulong[] {13292315514680272486, 7378647193648342630}),   //Pawn
        GetPieceAdjustments(new ulong[] {12205485488516178448, 2454591752300046690}),   //Knight
        GetPieceAdjustments(new ulong[] {9760575157703033923, 4918887868711340132}),    //Bishop
        GetPieceAdjustments(new ulong[] {7378416150784730726, 8531619129795634789}),    //Rook
        GetPieceAdjustments(new ulong[] {8603413936259486787, 6071810403824072550}),    //Queen
        GetPieceAdjustments(new ulong[] {77125320457715986, 7550960606429581859}),      //King
        GetPieceAdjustments(new ulong[] {15871470423304712720, 2459077696152591426})    //King Endgame
        };
    }

    int[] GetPieceAdjustments(ulong[] rows)
    {
        int[] adjustments = new int[32];
        for (int i = 0; i < rows.Length; i++)
            for (int j = 0; j < 16; j++)
                adjustments[i * 16 + j] = adjustmentValues[(int)(((1 << 4) - 1) & (rows[i] >> (4 * j)))] - 50;
        return adjustments;
    }

    public Move Think(Board board, Timer timer)
    {
        boardRef = board;
        timerRef = timer;
        isWhite = boardRef.IsWhiteToMove;

        //As there are less pieces search to higher depths
        int pieceCount = 0;
        Array.ForEach(boardRef.GetAllPieceLists(), list => pieceCount += list.Count);
        depth = pieceCount < 5 ? 7 : pieceCount < 10 ? 6 : 5;

        //With less time on the clock allow less time to search to avoid flagging
        maxTime = timerRef.MillisecondsRemaining < 10000 ? 750 : timerRef.MillisecondsRemaining < 25000 ? 1000 : 2000;
        if (timerRef.MillisecondsRemaining < 5000)
        {
            maxTime = 500;
            depth = 5;
        }

        Search(depth, -600000, 600000, isWhite ? 1 : -1);
        return moveToPlay;
    }

    //Search Using Negamax Algorithm
    int Search(int currentDepth, int alpha, int beta, int color)
    {
        //Any Searches after the max time are discarded (Most likely found the search early based on move sorting)
        if (timerRef.MillisecondsElapsedThisTurn > maxTime)
            return 500000;
        if (boardRef.IsInCheckmate() || boardRef.IsDraw())
            return CalculatePosition(color, currentDepth);
        if (currentDepth == 0)
            return QuiescenceSearch(alpha, beta, color, currentDepth);

        Move[] moves = GetSortedMoves(false);
        foreach (Move move in moves)
        {
            boardRef.MakeMove(move);
            int eval = -Search(currentDepth - 1, -beta, -alpha, -color);
            boardRef.UndoMove(move);

            if (eval >= beta)
                return beta;
            if (eval > alpha)
            {
                alpha = eval;
                if (currentDepth == depth)
                    moveToPlay = move;
            }
        }
        return alpha;
    }

    //Quiescence Search To Resolve Pieces Under Threat
    //Maybe look at adding checks
    int QuiescenceSearch(int alpha, int beta, int color, int currentDepth)
    {
        int eval = CalculatePosition(color, currentDepth);
        if (eval >= beta)
            return beta;
        alpha = Math.Max(alpha, eval);

        //Gets only the capture moves
        Move[] captureMoves = GetSortedMoves(true);
        foreach (Move capture in captureMoves)
        {
            boardRef.MakeMove(capture);
            eval = -QuiescenceSearch(-beta, -alpha, -color, currentDepth - 1);
            boardRef.UndoMove(capture);

            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);
        }

        return alpha;
    }

    Move[] GetSortedMoves(bool capturesOnly)
    {
        Move[] moves = boardRef.GetLegalMoves(capturesOnly);
        int[] scores = new int[moves.Length];
        int count = 0;

        //Score each move based on multiple factors
        foreach (Move move in moves)
        {
            int scoreGuess = 0;
            int movePieceType = (int)move.MovePieceType;

            if (move.IsCapture)
                scoreGuess = 10 * pieceValues[(int)move.CapturePieceType] - pieceValues[movePieceType];

            if (move.IsPromotion)
                scoreGuess += pieceValues[(int)move.PromotionPieceType] - pieceValues[movePieceType];

            scoreGuess += GetSquareValue(move.TargetSquare, boardRef.IsWhiteToMove, GetAdjustmentList(movePieceType));
            boardRef.MakeMove(move);
            if (boardRef.IsInCheck())
            {
                scoreGuess += 5000;
                if (boardRef.IsInCheckmate())
                    scoreGuess += 500000;
            }
            boardRef.UndoMove(move);
            scores[count] = scoreGuess;
            count++;
        }

        //Reorder list of moves based on the guess scores
        for (int i = 0; i < moves.Length; i++)
            for (int j = i + 1; j < moves.Length; j++)
                if (scores[i] < scores[j])
                {
                    int tempScore = scores[i];
                    scores[i] = scores[j];
                    scores[j] = tempScore;

                    Move tempMove = moves[i];
                    moves[i] = moves[j];
                    moves[j] = tempMove;
                }

        return moves;
    }

    //Score the position based on the values of pieces left
    int CalculatePosition(int color, int currentDepth)
    {
        //Adjust by depth value to favor a quicker mate
        if (boardRef.IsInCheckmate())
            return -500000 - currentDepth;

        if (boardRef.IsDraw())
            return 0;

        int score = 0;
        PieceList[] pieceLists = boardRef.GetAllPieceLists();
        foreach (PieceList pieceList in pieceLists)
        {
            int pieceValue = pieceValues[(int)pieceList.TypeOfPieceInList];
            int[] adjustmentArray = GetAdjustmentList((int)pieceList.TypeOfPieceInList);
            foreach (Piece piece in pieceList)
            {
                int value = GetSquareValue(piece.Square, piece.IsWhite, adjustmentArray) + pieceValue;
                if (!piece.IsWhite)
                    value *= -1;
                score += value;
            }
        }

        return score * color;
    }

    int[] GetAdjustmentList(int pieceType)
    {
        return pieceType == 6 && IsEndGame() ? pieceAdjustments[7] : pieceAdjustments[pieceType];
    }

    //Adjustment values for piece based on the square it is on
    int GetSquareValue(Square square, bool isWhite, int[] adjustmentArray)
    {
        int file = square.File;
        int rank = square.Rank;
        rank = isWhite ? 7 - rank : rank;
        if (file > 3)
            file = 7 - file;
        return adjustmentArray[rank * 4 + file];
    }

    //Endgame if No Queens or 1 Queen and 1 Minor Piece
    bool IsEndGame()
    {
        bool[] sides = { true, false };
        var GetPieces = boardRef.GetPieceList;
        foreach (bool side in sides)
        {
            int queenCount = GetPieces(PieceType.Queen, side).Count;
            int minorPieceCount = GetPieces(PieceType.Rook, side).Count + GetPieces(PieceType.Bishop, side).Count + GetPieces(PieceType.Knight, side).Count;
            if ((queenCount == 0 && minorPieceCount > 2) || (queenCount == 1 && minorPieceCount > 1))
                return false;
        }
        return true;
    }
}