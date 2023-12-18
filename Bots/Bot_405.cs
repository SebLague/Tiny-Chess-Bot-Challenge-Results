namespace auto_Bot_405;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
//using System.Numerics;
//using System.Linq;


public class Bot_405 : IChessBot
{
    //                     .  P    K    B    R    Q    K
    int[] kPieceValues = { 0, 100, 300, 310, 500, 900, 10000 };
    int kMassiveNum = 99999999;

    public int mDepth;
    Move mBestMove;
    public bool cas = true;
    public Move mym = Move.NullMove;



    public List<ulong> zobristlist = new List<ulong>();

    public List<int> alphalist = new List<int>();

    public List<int> depthlist = new List<int>();



    public List<int> evallist = new List<int>();

    public List<string> fenlist = new List<string>();





    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();
        mDepth = 4;

        zobristlist.Clear();
        alphalist.Clear();
        depthlist.Clear();

        evallist.Clear();
        fenlist.Clear();




        EvaluateBoardNegaMax(board, mDepth, -kMassiveNum, kMassiveNum, board.IsWhiteToMove ? 1 : -1, mym);

        return mBestMove;
    }

    int EvaluateBoardNegaMax(Board board, int depth, int alpha, int beta, int color, Move mymove)
    {

        if (board.IsDraw())
            return 0;

        ulong zobrist = board.ZobristKey;
        int index = zobristlist.LastIndexOf(zobrist);

        bool ttpt = (index >= 0);


        if (ttpt)
        {
            if (depth <= depthlist[index])
                return alphalist[index];
        }




        //.Add()
        // if (AuthorList.Contains("Mahesh Chand"))
        //AuthorList.IndexOf("Nipun Tomar"); -1






        Move[] legalMoves = board.GetLegalMoves();

        if (depth == 0 || legalMoves.Length == 0)
        {
            // EVALUATE
            int sum = 0;

            if (board.IsInCheckmate())
                return -9999999;

            sum = Evaluate(board, color, legalMoves);


            // EVALUATE

            return sum;
        }

        // TREE SEARCH
        int recordEval = int.MinValue;
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            if (mDepth == depth)
                mymove = move;
            int evaluation = -EvaluateBoardNegaMax(board, depth - 1, -beta, -alpha, -color, mymove);
            board.UndoMove(move);

            if (move.IsCastles)
                evaluation += 100;

            if (recordEval < evaluation)
            {
                recordEval = evaluation;
                if (depth == mDepth)
                    mBestMove = move;
            }

            alpha = Math.Max(alpha, recordEval);
            if (alpha >= beta) break;
        }
        // TREE SEARCH
        if (ttpt)
        {
            depthlist[index] = depth;
            alphalist[index] = alpha;
        }
        else
        {
            zobristlist.Add(zobrist);
            depthlist.Add(depth);
            alphalist.Add(alpha);
        }
        return alpha;
    }




    int Evaluate(Board board, int color, Move[] moves)
    {
        /*string fenString=board.GetFenString();
        int index=fenlist.LastIndexOf(fenString);

        bool ttpt = (index >= 0);


        if (ttpt)
        {
            return evallist[index];
        }
        */
        int sum = 0;
        for (int i = 1; i < 7; i++)
        {
            foreach (Piece piece in board.GetPieceList((PieceType)i, true))
            {
                sum += PieceValue(board, piece, i, moves);
            }

            foreach (Piece piece in board.GetPieceList((PieceType)i, false))
            {
                sum -= PieceValue(board, piece, i, moves);
            }

        }
        sum += moves.Length * color;

        sum += board.HasKingsideCastleRight(true) ? 1 : -1;
        sum += board.HasQueensideCastleRight(true) ? 1 : -1;
        sum -= board.HasKingsideCastleRight(false) ? 1 : -1;
        sum -= board.HasQueensideCastleRight(false) ? 1 : -1;
        //evallist.Add(sum * color);
        //fenlist.Add(fenString);

        return sum * color;
    }

    int PieceValue(Board board, Piece piece, int PieceType, Move[] moves)
    {

        return kPieceValues[PieceType];
    }





}


