namespace auto_Bot_362;
using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class Bot_362 : IChessBot
{

    double thinkingTime;
    Timer timer;
    int totalDepth;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        this.timer = timer;
        totalDepth = 0;

        Move bestMove = moves[0];
        Move bestMoveLast = moves[0];
        int depth = 0;
        thinkingTime = timer.MillisecondsRemaining / 120;
        Dictionary<Move, double> moveValuesOld = new Dictionary<Move, double>();
        foreach (Move move in moves)
        {
            double moveValue = -0.1 * (double)move.MovePieceType;
            if (move.IsPromotion)
            {
                moveValue += 5 * (double)move.PromotionPieceType;
            }
            if (move.IsCapture)
            {
                moveValue += 3 * (double)move.CapturePieceType;
            }
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveValue -= 4 * (double)move.MovePieceType;
            }
            moveValuesOld.Add(move, moveValue);
        }
        while (true)
        {
            double alpha = -100000;
            Dictionary<Move, double> moveValuesNew = new Dictionary<Move, double>();
            for (int i = 0; i < moves.Length; i++)
            {
                double highestSearch = -10000000;
                Move move = new Move();
                foreach (Move move1 in moves)
                {
                    if (moveValuesOld.ContainsKey(move1))
                    {
                        double searchValue = moveValuesOld[move1];
                        if (searchValue > highestSearch)
                        {
                            move = move1;
                            highestSearch = searchValue;
                        }
                    }
                }
                moveValuesOld.Remove(move);
                board.MakeMove(move);
                if (depth == 0 && board.IsInCheckmate())
                {
                    return move;
                }
                double value = -CalculateValue(board, depth, -100000, -alpha);
                moveValuesNew.Add(move, value);
                board.UndoMove(move);
                if (value > alpha)
                {
                    alpha = value;
                    bestMove = move;
                }
                if (timer.MillisecondsElapsedThisTurn > thinkingTime && depth > 0)
                {
                    return bestMoveLast;
                }
            }
            depth++;
            totalDepth = depth;
            moveValuesOld = moveValuesNew;
            bestMoveLast = bestMove;
        }
    }
    public double CalculateValue(Board board, int depth, double alpha, double beta)
    {
        if (board.IsDraw())
        {
            return 0;
        }
        double highest = -100000;
        Move[] moves;
        if (depth > 0)
        {
            moves = board.GetLegalMoves();
        }
        else
        {
            moves = board.GetLegalMoves(true);
        }
        if ((totalDepth > board.PlyCount / 10 && moves.Length == 0) || (totalDepth <= board.PlyCount / 10 && depth <= 0))
        {
            if (board.IsInCheckmate())
            {
                return 9000;
            }
            double output = CalculateValue(board, true) - CalculateValue(board, false);

            if (!board.IsWhiteToMove)
            {
                output = -output;
            }
            return output;
        }
        Dictionary<Move, double> moveValuesOld = new Dictionary<Move, double>();
        foreach (Move move in moves)
        {
            double moveValue = -0.1 * (double)move.MovePieceType;
            if (move.IsPromotion)
            {
                moveValue += 5 * (double)move.PromotionPieceType;
            }
            if (move.IsCapture)
            {
                moveValue += 3 * (double)move.CapturePieceType;
            }
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveValue -= 4 * (double)move.MovePieceType;
            }
            moveValuesOld.Add(move, moveValue);
        }
        for (int i = 0; i < moves.Length; i++)
        {
            double highestSearch = -10000000;
            Move move = new Move();
            foreach (Move move1 in moves)
            {
                if (moveValuesOld.ContainsKey(move1))
                {
                    double searchValue = moveValuesOld[move1];
                    if (searchValue > highestSearch)
                    {
                        move = move1;
                        highestSearch = searchValue;
                    }
                }
            }
            moveValuesOld.Remove(move);
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return 10000 + depth;
            }
            double value = -CalculateValue(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);
            if (value >= beta/* && depth > 0*/)
            {
                return value;
            }
            if (value > alpha)
            {
                alpha = value;
            }
            if (value > highest)
            {
                highest = value;
            }
            if (timer.MillisecondsRemaining < 10000 || timer.MillisecondsElapsedThisTurn > thinkingTime)
            {
                return highest;
            }

        }
        return highest;
    }
    public double CalculateValue(Board board, bool isWhite)
    {
        if (board.IsDraw())
        {
            return 0;
        }
        double output = 0;

        ulong pawns = board.GetPieceBitboard((PieceType)1, isWhite);
        output += 10 * BitboardHelper.GetNumberOfSetBits(pawns);
        ulong knights = board.GetPieceBitboard((PieceType)2, isWhite);
        output += 30 * BitboardHelper.GetNumberOfSetBits(knights);
        ulong bishops = board.GetPieceBitboard((PieceType)3, isWhite);
        output += 32 * BitboardHelper.GetNumberOfSetBits(bishops);
        ulong rooks = board.GetPieceBitboard((PieceType)4, isWhite);
        output += 50 * BitboardHelper.GetNumberOfSetBits(rooks);
        ulong queens = board.GetPieceBitboard((PieceType)5, isWhite);
        output += 90 * BitboardHelper.GetNumberOfSetBits(queens);
        ulong pieces = bishops | knights;
        output += BitboardHelper.GetNumberOfSetBits(pawns & pawns << 1);
        ulong defend = pieces | pawns;
        while (defend != 0)
        {
            if ((BitboardHelper.GetPawnAttacks(new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref defend)), !isWhite) & pawns) != 0)
            {
                output += 0.5;
            }
        }
        while (pawns != 0)
        {
            if (isWhite)
            {
                output += ((BitboardHelper.ClearAndGetIndexOfLSB(ref pawns)) / 8) / 20.0;
            }
            else
            {
                output += (8 - ((BitboardHelper.ClearAndGetIndexOfLSB(ref pawns)) / 8)) / 20.0;
            }
        }

        int loc = board.GetKingSquare(isWhite).Index;
        if (board.PlyCount > 40)
        {
            output -= Math.Abs(4 - (loc % 8));
            output -= Math.Abs(4 - (loc / 8));
        }
        else
        {
            output += Math.Abs(4 - (loc % 8));
            output += Math.Abs(4 - (loc / 8));
        }
        pieces |= queens;
        while (pieces != 0)
        {
            loc = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);
            output += Math.Abs(4 - (loc % 8)) / 3.0;
            output += Math.Abs(4 - (loc / 8)) / 3.0;
        }

        return output;
    }
}
