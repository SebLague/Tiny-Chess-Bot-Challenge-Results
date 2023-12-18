namespace auto_Bot_165;
using ChessChallenge.API;
using System;

public class Bot_165 : IChessBot
{
    private double[] pawnSquareValues { get; } = new double[64]
    {
        0, 0, 0, 0, 0, 0, 0, 0,
        5, 5, 5, 5, 5, 5, 5, 5,
        1, 1, 2, 3, 3, 2, 1, 1,
        .5, .5, 1, 2.5, 2.5, 1, 5, .5,
        0, 0, 0, 2, 2, 0, 0, 0,
        .5, -.5 , -1, 0, 0, -1, -.5, .5,
        .5, 1, 1, -2, -2, 1, 1, .5,
        0, 0, 0, 0, 0, 0, 0, 0
    };
    private double[] knightSquareValues { get; } = new double[64]
    {
        -5, -4, -3, -3, -3, -3, -4, -5,
        -4, -2, 0, 0, 0, 0, -2, -4,
        -3, 0, 1, 1.5, 1.5, 1, 0,-3,
        -3, .5, 1.5, 2, 2, 1.5, 5,-3,
        -3, 0, 1.5, 2, 2, 1.5, 0,-3,
        -3, .5, 1, 1.5, 1.5, 1, .5,-3,
        -4, -2, 0, .5, .5, 0, -2, -4,
        -5, -4, -3, -3, -3, -3, -4, -5
    };
    private double[] bishopSquareValues { get; } = new double[64]
    {
        -2, -1, -1, -1, -1, -1, -1, -2,
        -1, 0, 0, 0, 0, 0, 0, -1,
        -1, 0, .5, 1, 1, .5, 0, -1,
        -1, .5, .5, 1, 1, .5, .5, -1,
        -1, 0, 1, 1, 1, 1, 0, -1,
        -1, 1, 1, 1, 1, 1, 1, -1,
        -1, .5, 0, 0, 0, 0, .5, -1,
        -2, -1, -1, -1, -1, -1, -1, -2
    };
    private double[] kingSquareValues { get; } = new double[64]
    {
        -3, -4, -4, -5, -5, -4, -4, -3,
        -3, -4, -4, -5, -5, -4, -4, -3,
        -3, -4, -4, -5, -5, -4, -4, -3,
        -3, -4, -4, -5, -5, -4, -4, -3,
        -2, -3, -3, -4, -4, -3, -3, -2,
        -1, -2, -2, -2, -2, -2, -2, -1,
        2, 2, 0, 0, 0, 0, 2, 2,
        2, 3, 1, 0, 0, 1, 3, 2
    };

    private bool isWhite;

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        return moves[MinMax(board, 3, true, double.MinValue, double.MaxValue).Index];       //Depth set to 3
    }

    private MoveData MinMax(Board board, int depth, bool isYourMove, double alpha, double beta)     //Uses alpha beta pruning and recursion
    {
        Move[] moves = board.GetLegalMoves();

        if (isYourMove)     //Maximizing player
        {
            MoveData max;
            MoveData each;

            max = new MoveData(-1, double.MinValue);

            for (int i = 0; i < moves.Length; i++)
            {
                board.MakeMove(moves[i]);

                if (depth > 0 && board.GetLegalMoves().Length != 0)
                    each = MinMax(board, depth - 1, !isYourMove, alpha, beta);
                else
                    each = new MoveData(i, Evaluation(board));
                each.Index = i;

                board.UndoMove(moves[i]);

                if (each.Eval > max.Eval)
                    max = each;

                alpha = Math.Max(alpha, max.Eval);

                if (alpha >= beta)
                    break;
            }

            return max;
        }
        else    //Minimizing player
        {
            MoveData min;
            MoveData each;

            min = new MoveData(-1, double.MaxValue);

            for (int i = 0; i < moves.Length; i++)
            {
                board.MakeMove(moves[i]);

                if (depth > 0 && board.GetLegalMoves().Length != 0)
                    each = MinMax(board, depth - 1, !isYourMove, alpha, beta);
                else
                    each = new MoveData(i, Evaluation(board));
                each.Index = i;

                board.UndoMove(moves[i]);

                if (each.Eval < min.Eval)
                    min = each;

                beta = Math.Min(beta, min.Eval);

                if (alpha >= beta)
                    break;
            }

            return min;
        }
    }

    private double Evaluation(Board board)
    {
        if (board.IsInCheckmate())
            return 10000;

        double eval = 0;
        PieceList[] pieces = board.GetAllPieceLists();

        foreach (PieceList each in pieces)       //Piece evaluation
        {
            double pieceMultiplier = 0;
            switch ((int)each.TypeOfPieceInList)
            {
                case 1:
                    pieceMultiplier = 1;
                    break;
                case 2:
                    pieceMultiplier = 3.05;
                    break;
                case 3:
                    pieceMultiplier = 3.33;
                    break;
                case 4:
                    pieceMultiplier = 5.63;
                    break;
                case 5:
                    pieceMultiplier = 9.5;
                    break;
            }

            eval += (!(each.IsWhitePieceList ^ isWhite) ? 1 : -1) * pieceMultiplier * each.Count;
        }

        if (board.IsInCheck())
            eval += 0.5;
        if (board.IsDraw())
        {
            eval -= 10;
        }

        for (PieceType type = (PieceType)1; (int)type < 7; type++)      //Position evaluation
        {
            PieceList pieceTypeList = board.GetPieceList(type, isWhite);

            foreach (Piece each in pieceTypeList)
            {
                int square = each.Square.Index;

                switch ((int)type)
                {
                    case 1:
                        eval += 0.5 * pawnSquareValues[(isWhite ? 63 - square : square)];
                        break;
                    case 2:
                        eval += 0.5 * knightSquareValues[(isWhite ? 63 - square : square)];
                        break;
                    case 3:
                        eval += 0.5 * bishopSquareValues[(isWhite ? 63 - square : square)];
                        break;
                    case 6:
                        eval += 0.5 * kingSquareValues[(isWhite ? 63 - square : square)];
                        break;
                }
            }
        }

        return eval;
    }
}

public struct MoveData      //Helps minmax algorithm
{
    public int Index { get; set; }
    public double Eval { get; }

    public MoveData(int index, double eval)
    {
        Index = index;
        Eval = eval;
    }
}