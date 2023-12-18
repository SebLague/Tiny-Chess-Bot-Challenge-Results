namespace auto_Bot_240;
using ChessChallenge.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class Bot_240 : IChessBot
{

    bool ImWhite = true;
    Dictionary<ulong, Tuple<int, int, int>> ttDict = new Dictionary<ulong, Tuple<int, int, int>>();

    public Move Think(Board board, Timer timer)
    {

        int origTime = timer.MillisecondsRemaining;

        ImWhite = board.IsWhiteToMove;

        int BestScore = -100000;
        Move BestMove = Move.NullMove;

        for (int depth = 4; true; depth++)
        {
            int StartTime = timer.MillisecondsElapsedThisTurn;

            Move[] moves = board.GetLegalMoves();
            moves = OrderMoves(moves, ttDict, board);

            int alpha = -100000;
            int beta = 100000;
            foreach (Move move in moves)
            {

                board.MakeMove(move);
                int Score = -Search(depth - 1, board, alpha, beta); //This is still not working
                board.UndoMove(move);

                if (Score > BestScore)
                {
                    BestScore = Score;
                    BestMove = move;
                }
            }

            int TimeLimit = 200; // beggining thinking time
            int TimeLeft = timer.MillisecondsRemaining;
            if (TimeLeft < origTime - 10000)
            {
                TimeLimit = 500; // can think for longer once openning finished
            }
            if (TimeLeft < 10000)
            {
                TimeLimit = 150; // Need to think a bit faster at 10 seconds
            }
            if (TimeLeft < 4000)
            {
                TimeLimit = 0; // last 4 seconds need to speed up
            }

            if (timer.MillisecondsElapsedThisTurn - StartTime > TimeLimit)
            {
                //int distW = distanceToKing(board, true);
                //int distB = distanceToKing(board, false);

                //string text = $"Depth: {depth}    distW:   {distW}    distB:   {distB}    Time Speed:  {timer.MillisecondsElapsedThisTurn - StartTime}   FEN:   {board.GetFenString()}";
                //DivertedConsole.Write(text.ToString());
                break;
            }
        }

        return BestMove;
    }




    Move[] OrderMoves(Move[] moves, Dictionary<ulong, Tuple<int, int, int>> dict, Board board)
    {

        List<Move> MoveOrder = moves.ToList();

        foreach (Move move in moves)
        {
            int MoveScore = -1;

            if (ttDict.ContainsKey(board.ZobristKey) == true)
            {
                var tteval = ttDict[board.ZobristKey];
                int pttscore = tteval.Item1; //Score from transposition table

                if (pttscore > 0)
                {
                    MoveScore += 500; // buff this move because the transposition table liked it before
                }
            }

            if (move.CapturePieceType > 0)
            {
                MoveScore = (GetPieceValue((int)move.MovePieceType) - GetPieceValue((int)move.CapturePieceType));
            }

            if (MoveScore >= 0)
            {
                MoveOrder.Remove(move);
                MoveOrder.Insert(0, move);
            }
        }

        return MoveOrder.ToArray();
    }

    static int GetPieceValue(int PieceIndex)
    {
        if (PieceIndex > 1 & PieceIndex < 4)
        { // knight and bishop value
            return 300;
        }
        if (PieceIndex == 4)
        { // Rook
            return 500;
        }
        if (PieceIndex == 5)
        { //Queen Value
            return 1000;
        }

        return 100; //pawn value
    }


    int Search(int FuncDepth, Board board, int alpha, int beta)
    {
        int alphaOrig = alpha;

        if (ttDict.ContainsKey(board.ZobristKey) == true)
        {
            var tteval = ttDict[board.ZobristKey];
            int pttscore = tteval.Item1;
            int pttdepth = tteval.Item2;
            int pttflag = tteval.Item3;


            if (pttdepth >= FuncDepth)
            {
                if (pttflag == 2)
                {  //LOWERBOUND
                    if (alpha < pttscore)
                    {
                        alpha = pttscore;
                    }
                }
                else if (pttflag == 1)
                {  //UPPERBOUND
                    if (beta > pttscore)
                    {
                        beta = pttscore;
                    }
                }
                else
                { //EXACT
                    return pttscore;
                }

                if (alpha >= beta)
                {
                    return pttscore;
                }
            }
            else
            {
                ttDict.Remove(board.ZobristKey);
            }
        }

        if (FuncDepth == 0)
        {
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0)
        { // all this is checkmate checker
            if (board.IsInCheckmate() == true)
            {
                if (board.IsWhiteToMove == false)
                {
                    if (ImWhite == true)
                    {
                        return -1000000;
                    }
                    else
                    {
                        return 1000000;
                    }
                }
                else
                {
                    if (ImWhite == true)
                    {
                        return 1000000;
                    }
                    else
                    {
                        return -1000000;
                    }
                }
            }
            else
            {
                return 0;
            }
        }

        moves = OrderMoves(moves, ttDict, board);

        int Score = -1000000;
        foreach (Move move in moves)
        {

            board.MakeMove(move);
            Score = -Search(FuncDepth - 1, board, -beta, -alpha);
            board.UndoMove(move);

            if (Score > alpha)
            {
                alpha = Score;
            }
            if (Score >= beta)
            {
                return beta;
            }
        }

        // this stuff is adding to the dict
        int ttscore = Score;
        int ttflag = 3; // EXACT
        if (Score <= alphaOrig)
        {
            ttflag = 1; //UPPERBOUND
        }
        else if (Score >= beta)
        {
            ttflag = 2; // LOWERBOUND
        }
        int ttdepth = FuncDepth;

        Tuple<int, int, int> tup = new Tuple<int, int, int>(Score, ttdepth, ttflag);

        try
        {
            ttDict.Add(board.ZobristKey, tup);
        }
        catch { }

        return alpha;
    }



    static int Evaluate(Board board)
    {

        PieceList[] p = board.GetAllPieceLists(); //Gets an array of all the piece lists

        int WhiteScore = 0;
        int BlackScore = 0;
        for (int i = 0; i < 5; i++)
        {
            WhiteScore += p[i].Count * GetPieceValue(i + 1);
            BlackScore += p[i + 6].Count * GetPieceValue(i + 1);
        }


        int totalMoves = board.GameMoveHistory.Length;
        if (totalMoves < 6)
        {
            Random r = new Random();
            WhiteScore += r.Next(-200, 200); //Adds a bit of randomness at beginning
        }

        if (board.IsWhiteToMove == true)
        {
            return WhiteScore - BlackScore + distanceToKing(board, true);
        }
        else
        {
            return BlackScore - WhiteScore + distanceToKing(board, false);
        }
    }




    static int distanceToKing(Board board, bool isWhite)
    { //Gives a score which is a sum of all pieces distance from enemy king

        ulong kingBit = board.GetPieceBitboard(PieceType.King, !isWhite);

        ulong piecesBit = board.BlackPiecesBitboard;
        if (isWhite == true)
        {
            piecesBit = board.WhitePiecesBitboard;
        }

        byte[] kingByteArray = BitConverter.GetBytes(kingBit);
        BitArray kingBitArray = new BitArray(kingByteArray);
        byte[] pieceByteArray = BitConverter.GetBytes(piecesBit);
        BitArray PiecesBitArray = new BitArray(pieceByteArray);

        List<int> pieceLocX = new List<int>();
        List<int> pieceLocY = new List<int>(); //list to locate the pieces
        int distance = 0;

        int kingX = 0;
        int kingY = 0;
        for (int i = 0; i < 64; i += 8)
        {
            for (int j = 0; j < 8; j++)
            {
                if (kingBitArray[i + j])
                {
                    kingX = j;
                    kingY = i / 8;
                }
                if (PiecesBitArray[i + j])
                {
                    pieceLocX.Add(j);
                    pieceLocY.Add(i / 8);
                }
            }
        }

        for (int p = 0; p < pieceLocX.Count; p++)
        {
            distance += Math.Abs(pieceLocX[p] - kingX) + Math.Abs(pieceLocY[p] - kingY); //abs distance from each piece
        }

        return distance * 2; //times 2 to give it a bit more weight
    }
}