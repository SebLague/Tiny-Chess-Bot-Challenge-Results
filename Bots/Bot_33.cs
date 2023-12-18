namespace auto_Bot_33;
using ChessChallenge.API;
using System;



public class Bot_33 : IChessBot
{
    static double Weight(double pos, int piece_type)
    {
        switch (piece_type)
        {
            case 0:
                return 0;
            case 1:
                return pos;
            case 2:
                return 3 * pos;
            case 3:
                return 3 * pos;
            case 4:
                return 5 * pos;
            case 5:
                return 9 * pos;
            case 6:
                return 10 * pos;


        }
        return 0;
    }

    static double VectorX(int index)
    {
        switch (index % 8)
        {
            case 0:
                return -3.5;
            case 1:
                return -2.5;
            case 2:
                return -1.5;
            case 3:
                return -0.5;
            case 4:
                return 0.5;
            case 5:
                return 1.5;
            case 6:
                return 2.5;
            case 7:
                return 3.5;
        }
        return 0;
    }

    static double VectorY(int index)
    {
        switch (Math.Ceiling((double)(index + 1) / 8))
        {
            case 1:
                return -3.5;
            case 2:
                return -2.5;
            case 3:
                return -1.5;
            case 4:
                return -0.5;
            case 5:
                return 0.5;
            case 6:
                return 1.5;
            case 7:
                return 2.5;
            case 8:
                return 3.5;
        }
        return 0;
    }

    static string indexToString(int index)
    {
        string l = "";
        switch (index % 8)
        {
            case 0:
                l = "a";
                break;
            case 1:
                l = "b";
                break;
            case 2:
                l = "c";
                break;
            case 3:
                l = "d";
                break;
            case 4:
                l = "e";
                break;
            case 5:
                l = "f";
                break;
            case 6:
                l = "g";
                break;
            case 7:
                l = "h";
                break;
        }
        return l + Math.Ceiling(((double)(index + 1) / 8)).ToString();
    }

    static double EvaluationX(Board board)
    {
        double totalWeightX = 0;
        for (int i = 0; i < 64; i++)
        {
            Square currSquare = new Square(indexToString(i));
            double weightX = Weight(VectorX(i), (int)board.GetPiece(currSquare).PieceType);
            totalWeightX += weightX;
        }
        return totalWeightX;
    }

    static double EvaluationY(Board board)
    {
        double totalWeightY = 0;
        for (int i = 0; i < 64; i++)
        {
            Square currSquare = new Square(indexToString(i));
            double weightY = Weight(VectorY(i), (int)board.GetPiece(currSquare).PieceType);
            totalWeightY += weightY;
        }
        return totalWeightY;
    }

    static double Reevaluation(Board board, Move move, double evalX, double evalY)
    {
        double newWeightX = evalX - Weight(VectorX(move.StartSquare.Index), (int)move.MovePieceType);
        double newWeightY = evalY - Weight(VectorY(move.StartSquare.Index), (int)move.MovePieceType);
        if (move.IsCapture & !move.IsEnPassant)

        {

            newWeightX = newWeightX - Weight(VectorX(move.TargetSquare.Index), (int)move.CapturePieceType);
            newWeightY = newWeightY - Weight(VectorY(move.TargetSquare.Index), (int)move.CapturePieceType);
        }
        else if (move.IsEnPassant & move.TargetSquare.Rank == 6)
        {

            newWeightX = newWeightX - Weight(VectorX(move.TargetSquare.Index - 8), 1);
            newWeightY = newWeightY - Weight(VectorY(move.TargetSquare.Index - 8), 1);
        }
        else if (move.IsEnPassant & move.TargetSquare.Rank == 2)
        {

            newWeightX = newWeightX - Weight(VectorX(move.TargetSquare.Index + 8), 1);
            newWeightY = newWeightY - Weight(VectorY(move.TargetSquare.Index + 8), 1);
        }
        if (!move.IsPromotion)
        {

            newWeightX += Weight(VectorX(move.TargetSquare.Index), (int)move.MovePieceType);
            newWeightY += Weight(VectorY(move.TargetSquare.Index), (int)move.MovePieceType);
        }
        else if (move.IsPromotion)
        {

            newWeightX += Weight(VectorX(move.TargetSquare.Index), (int)move.PromotionPieceType);
            newWeightY += Weight(VectorY(move.TargetSquare.Index), (int)move.PromotionPieceType);
        }
        if (move.IsCastles)
        {

            switch (move.TargetSquare.Index)
            {
                case 2:
                    newWeightX = newWeightX - Weight(VectorX(0), 4) + Weight(VectorX(3), 4);
                    newWeightY = newWeightY - Weight(VectorY(0), 4) + Weight(VectorY(3), 4);
                    break;
                case 6:
                    newWeightX = newWeightX - Weight(VectorX(7), 4) + Weight(VectorX(5), 4);
                    newWeightY = newWeightY - Weight(VectorY(7), 4) + Weight(VectorY(5), 4);
                    break;
                case 58:
                    newWeightX = newWeightX - Weight(VectorX(56), 4) + Weight(VectorX(59), 4);
                    newWeightY = newWeightY - Weight(VectorY(56), 4) + Weight(VectorY(59), 4);
                    break;
                case 62:
                    newWeightX = newWeightX - Weight(VectorX(63), 4) + Weight(VectorX(61), 4);
                    newWeightY = newWeightY - Weight(VectorY(63), 4) + Weight(VectorY(61), 4);
                    break;
            }
        }

        return Math.Sqrt((newWeightX * newWeightX) + (newWeightY * newWeightY));
    }

    public Move Think(Board board, Timer timer)
    {
        Random rnd = new Random();
        Move[] moves = board.GetLegalMoves();
        double currWeightX = EvaluationX(board);
        double currWeightY = EvaluationY(board);
        int[] bests = new int[1];

        double balance = 100000;
        int k = 0;
        for (int i = 0; i < moves.Length; i++)
        {
            double new_balance = Reevaluation(board, moves[i], currWeightX, currWeightY);
            if (new_balance < balance)
            {
                k = i;
                balance = new_balance;
                Array.Clear(bests);
                bests[0] = k;

            }
            else if (new_balance == balance)
            {
                for (int runs = 0; runs < bests.Length; runs++)
                {
                    bests[runs] = i;
                }
            }
        }
        int l = bests.Length;
        return moves[bests[rnd.Next(l)]];
    }
}