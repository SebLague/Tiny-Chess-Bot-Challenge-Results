namespace auto_Bot_191;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_191 : IChessBot
{

    public int[] bestPositions = {
        //pawn knight bishop rook queen
            0  ,0  ,0  ,0  ,0  ,0  ,0  ,0,
            5  ,10 ,10 ,-20,-20,10 ,10 ,5,
            5  ,-5 ,-10, 0, 0  ,-10 ,-5 ,5,
            0  ,0  ,0  ,20 ,20 ,0  ,0  ,0,
            5  ,5  ,10 ,25 ,25 ,10 ,5  ,5,
            10 ,10 ,20 ,30 ,30 ,20 ,10 ,10,
            50 ,50 ,50 ,50 ,50 ,50 ,50 ,50,
            100  ,100 ,100  ,100  ,100 ,100  ,100  ,100,
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,  0,  5,  5,  0,-20,-40,
            -30,  5, 10, 15, 15, 10,  5,-30,
            -30,  0, 15, 20, 20, 15,  0,-30,
            -30,  5, 15, 20, 20, 15,  5,-30,
            -30,  0, 10, 15, 15, 10,  0,-30,
            -40,-20,  0,  0,  0,  0,-20,-40,
            -50,-40,-30,-30,-30,-30,-40,-50,
            -20,-10,-10,-10,-10,-10,-10,-20,
            -10,  5,  0,  0,  0,  0,  5,-10,
            -10, 10, 10, 10, 10, 10, 10,-10,
            -10,  0, 10, 10, 10, 10,  0,-10,
            -10,  5,  5, 10, 10,  5,  5,-10,
            -10,  0,  5, 10, 10,  5,  0,-10,
            -10,  0,  0,  0,  0,  0,  0,-10,
            -20,-10,-10,-10,-10,-10,-10,-20,
        };
    public Move Think(Board board, Timer timer) => new Move(GetBestMove(board), board);

    public double PositionalEval(Board board, Move mov)
    {
        int[] posindex = { 0, 0, 64, 128, 192, 256 };
        int rpos = 0;
        int[] pb = Enumerable.Reverse(bestPositions[0..64]).ToArray();
        int[] kb = Enumerable.Reverse(bestPositions[64..128]).ToArray();
        int[] bb = Enumerable.Reverse(bestPositions[128..192]).ToArray();
        var bestPositionsBlack = new int[bestPositions.Length];
        pb.CopyTo(bestPositionsBlack, 0);
        kb.CopyTo(bestPositionsBlack, 64);
        bb.CopyTo(bestPositionsBlack, 128);
        if (mov.MovePieceType == PieceType.King || mov.MovePieceType == PieceType.Queen || mov.MovePieceType == PieceType.Rook)
        {
            return 0;
        }
        if (!mov.IsCapture)
        {
            rpos = board.IsWhiteToMove == true ? -1 * rpos : mov.TargetSquare.Rank - mov.StartSquare.Rank;
        }
        int posval = new Square(mov.StartSquare.Name).Index + posindex[(int)mov.MovePieceType];
        int arpos = new Square(mov.TargetSquare.Name).Index + posindex[(int)mov.MovePieceType];

        return board.IsWhiteToMove == false ? (double)(bestPositions[arpos] - bestPositions[posval]) / 40 + rpos / 3 : (double)(bestPositionsBlack[arpos] - bestPositionsBlack[posval]) / 40 + rpos / 3;

    }
    public double PieceEval(Board board)
    {
        double[] values = { 0, 1, 3.32, 3.33, 6.1, 10, 0 };

        double evalW = 0;
        double evalB = 0;
        foreach (PieceType m in Enum.GetValues<PieceType>())
        {
            if (m != PieceType.None)
            {
                evalW += board.GetPieceList(m, true).Count * values[(int)m];
                evalB += board.GetPieceList(m, false).Count * values[(int)m];
            }
        }
        if (!board.IsWhiteToMove)
        {
            return evalW - evalB;
        }
        else
        {
            return evalB - evalW;
        }
    }
    public string GetBestMove(Board board, int depth = 2)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> bestmoves = new List<Move>() { moves[0] };
        double bval = -999;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (!board.IsDraw())
            {
                if (depth != 0)
                {
                    if (!board.IsInCheckmate())
                    {
                        double evalextra = PositionalEval(board, move) + PieceEval(board) - Double.Parse(GetBestMove(board, depth - 1));
                        if (move.MovePieceType == PieceType.Pawn)
                        {
                            if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 6)
                            {
                                evalextra += 1;
                            }
                            if (board.IsWhiteToMove == true && PieceEval(board) < 0)
                            {
                                evalextra += 1;
                            }
                            else if (board.IsWhiteToMove == false && PieceEval(board) > 0)
                            {
                                evalextra += 1;
                            }
                        }
                        if (move.IsPromotion)
                        {
                            evalextra += 9;
                        }
                        if (move.IsCastles)
                        {
                            evalextra += 0.5;
                        }
                        if (board.IsInCheck())
                        {
                            evalextra += 0.733;
                        }
                        if (evalextra > bval)
                        {
                            bval = evalextra;
                            bestmoves.Clear();
                            bestmoves.Add(move);
                        }
                        else if (evalextra == bval)
                        {
                            bestmoves.Add(move);
                        }

                    }
                    else
                    {
                        bval = 99999;
                        bestmoves.Clear();
                        bestmoves.Add(move);
                    }
                }
                else
                {

                    double deval = PieceEval(board) + PositionalEval(board, move);
                    if (move.IsPromotion)
                    {
                        deval += 9;
                    }
                    if (board.IsInCheck())
                    {
                        deval += 0.733;
                    }
                    if (board.IsInCheckmate())
                    {
                        deval += 999;
                    }
                    if (deval > bval)
                    {
                        bval = deval;
                        bestmoves.Clear();
                        bestmoves.Add(move);
                    }
                    else if (deval == bval)
                    {
                        bestmoves.Add(move);
                    }
                }
            }
            board.UndoMove(move);
        }

        bestmoves.RemoveAll(i => i == Move.NullMove);
        Random rng = new();
        Move moveToPlay = bestmoves[rng.Next(bestmoves.Count)];
        string bMove = moveToPlay.StartSquare.Name.ToString() + moveToPlay.TargetSquare.Name.ToString();
        bMove += moveToPlay.IsPromotion == true ? "q" : "";
        return depth != 2 ? bval.ToString() : bMove;
    }
}