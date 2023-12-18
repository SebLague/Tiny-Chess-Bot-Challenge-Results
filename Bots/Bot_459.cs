namespace auto_Bot_459;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_459 : IChessBot
{
    Board mainBoard;
    //I am so sorry
    public Move Think(Board board, Timer timer)
    {
        mainBoard = board;
        Move[] moves = GetMoves();
        int depth = 2;
        int toatalPieceNum = 0;
        foreach (PieceList lists in mainBoard.GetAllPieceLists())
        {
            toatalPieceNum += lists.Count;
        }
        if (toatalPieceNum < 8)
        {
            if (timer.MillisecondsRemaining > 10000)
            {
                depth = 4;
            }
        }

        List<Move> bestMoves = new List<Move>();
        bestMoves.Add(moves[0]);

        int turnMod = 1;
        if (!mainBoard.IsWhiteToMove)
        {
            turnMod = -1;
        }
        float bestMoveVal = 999999 * -turnMod;
        foreach (Move move in moves)
        {
            mainBoard.MakeMove(move);
            float moveScore = GetValueForFutureMoves(depth, -999999, 999999);

            if (moveScore * turnMod >= bestMoveVal * turnMod)
            {
                if (moveScore * turnMod > bestMoveVal * turnMod)
                {
                    bestMoves = new List<Move>();
                    bestMoveVal = moveScore;
                }
                bestMoves.Add(move);
            }
            mainBoard.UndoMove(move);
        }
        return bestMoves[new Random().Next(bestMoves.Count)];
    }

    public Move[] GetMoves()
    {
        List<Move> queenCap = new List<Move>();
        List<Move> pawnCap = new List<Move>();
        List<Move> cap = new List<Move>();
        List<Move> allElse = new List<Move>();
        foreach (Move move in mainBoard.GetLegalMoves())
        {
            if (move.IsCapture)
            {
                if (move.CapturePieceType == PieceType.Queen)
                {
                    queenCap.Add(move);
                }
                else if (move.MovePieceType == PieceType.Pawn)
                {
                    pawnCap.Add(move);
                }
                else
                {
                    cap.Add(move);
                }
            }
            else
            {
                allElse.Add(move);
            }
        }
        cap.AddRange(allElse);
        pawnCap.AddRange(cap);
        queenCap.AddRange(pawnCap);
        return queenCap.ToArray();
    }

    float GetValueForFutureMoves(int depth, float alpha, float beta)
    {
        int turnMod = 1;
        if (mainBoard.IsWhiteToMove)
        {
            turnMod = -1;
        }
        float bestValue = 999999 * turnMod;
        Move[] moves = GetMoves();
        foreach (Move move in moves)
        {
            mainBoard.MakeMove(move);
            float curScore = GetPositionValue();
            float value;
            if ((Math.Abs(curScore) >= 999999) || (depth == 0))
            {
                value = curScore;
            }
            else
            {
                value = GetValueForFutureMoves(depth - 1, alpha, beta);
            }
            mainBoard.UndoMove(move);

            if (turnMod == -1)
            {
                if (value > alpha) { alpha = value; }
            }
            else
            {
                if (value < beta) { beta = value; }
            }

            if (value * turnMod < bestValue * turnMod)
            {
                bestValue = value;
            }
            if (beta <= alpha)
            {
                return bestValue;
            }
        }
        return bestValue;
    }

    float GetPositionValue()
    {
        if (mainBoard.IsInStalemate() || mainBoard.IsRepeatedPosition())
        {
            return 0;
        }
        else if (mainBoard.GetPieceList(PieceType.King, true).Count < 1)
        {
            return -999999;
        }
        else if (mainBoard.GetPieceList(PieceType.King, false).Count < 1)
        {
            return 999999;
        }
        else
        {
            return GetSideVal(true) - GetSideVal(false);
        }
    }
    float GetSideVal(bool white)
    {
        float boardBaseVal = mainBoard.GetPieceList(PieceType.Knight, white).Count * 300 //Knight Value
            + mainBoard.GetPieceList(PieceType.Bishop, white).Count * 300 //Bishop Value
            + mainBoard.GetPieceList(PieceType.Rook, white).Count * 500 //Rook Value
            + mainBoard.GetPieceList(PieceType.Queen, white).Count * 1000; //Queen Value
        foreach (Piece pawn in mainBoard.GetPieceList(PieceType.Pawn, white))
        {
            int adjVal;
            if (white) { adjVal = pawn.Square.Rank; }
            else { adjVal = 8 - pawn.Square.Rank; }
            boardBaseVal += 90 + adjVal * 1;//pawn value
        }

        int sightModifier = 0;
        PieceType[] piecesToCheck = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Queen };
        foreach (PieceType type in piecesToCheck)
        {
            foreach (Piece piece in mainBoard.GetPieceList(type, white))
            {
                string avalMoves = Convert.ToString((int)BitboardHelper.GetPieceAttacks(type, piece.Square, mainBoard, white), 2);
                foreach (char bit in avalMoves)
                {
                    if (bit == '1') { sightModifier++; }
                }
            }
        }

        return boardBaseVal + sightModifier * 5;
    }
}