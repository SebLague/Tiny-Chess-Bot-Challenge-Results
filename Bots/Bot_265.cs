namespace auto_Bot_265;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_265 : IChessBot
{
    private int lastVal;
    private bool isEndgame;
    private bool isWhite;
    private readonly Dictionary<PieceType, int> PieceValue;


    public Bot_265()
    {
        PieceValue = new Dictionary<PieceType, int> {
            {PieceType.Pawn, 100},
            {PieceType.Bishop, 320},
            {PieceType.Knight, 300},
            {PieceType.Rook, 480},
            {PieceType.Queen, 900},
            {PieceType.King, 0},
            {PieceType.None, 0}
        };
    }


    public Move Think(Board board, ChessChallenge.API.Timer timer)
    {
        isWhite = board.IsWhiteToMove;
        isEndgame = EvaluateTotalBoardMaterial(board) < 3000 ? true : false;
        Move bestMove = GetBestMove(board, CalculateDepth(timer), -1000000, 1000000);
        return bestMove;
    }


    private Move GetBestMove(Board board, int recursionDepth, int alpha, int beta)
    {
        Move[] moves = board.GetLegalMoves();

        //assign a estimated value to each move
        int[] vals = new int[moves.Length];
        for (int i = 0; i < vals.Length; i++)
        {
            vals[i] = EstimateMove(moves[i]);
        }

        //sort moves by estimated value with Quicksort
        QuickSortNow(moves, vals, 0, vals.Length - 1);

        //initialize variables
        int val;
        int maxVal = int.MinValue;
        Move bestMove = moves[0];

        //loop over moves
        foreach (Move move in moves)
        {
            board.MakeMove(move);


            //instantly return value if a checkmate is found
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                lastVal = 5000 + recursionDepth * 1000;
                return move;
            }

            //evaluate board and subtract the opponents best move afterwards
            val = EvaluateResultingChange(board, move, board.IsWhiteToMove);
            if (recursionDepth != 0 && !board.IsDraw())
            {
                GetBestMove(board, recursionDepth - 1, -beta, -alpha);
                val -= lastVal;
            }
            board.UndoMove(move);

            alpha = Math.Max(alpha, val);

            //check if the move is the best move until now
            if (val > maxVal)
            {
                bestMove = move;
                maxVal = val;
            }

            if (alpha > beta)
            {
                break;
            }
        }

        //return value and move
        lastVal = maxVal;
        return bestMove;
    }





    private int EvaluatePosition(Square sq, PieceType type, bool isWhite)
    {

        //pieces that should be in the middle of the board
        if (((int)type <= 3) || ((int)type == 6 && isEndgame))
        {
            if (sq.Rank >= 3 && sq.File >= 3 && sq.Rank <= 6 && sq.File <= 6)
            {
                return 10 * (int)type;
            }
        }

        //pawns
        if (type == PieceType.Pawn)
        {
            int bonus = isEndgame ? 3 : 1;
            return isWhite ? sq.File * 5 * bonus : (-sq.File + 8) * 5 * bonus;
        }
        return 0;
    }


    private int EvaluateResultingChange(Board board, Move move, bool isWhite)
    {
        int val = 0;

        if (board.IsDraw())
        {
            return -50;
        }
        if (board.IsInCheck())
        {
            if (board.FiftyMoveCounter < 30)
            {
                val += 40;
            }
        }
        if (move.IsCapture)
        {
            val += PieceValue[move.CapturePieceType];
            val += EvaluatePosition(move.TargetSquare, move.CapturePieceType, isWhite);
        }
        if (move.IsPromotion)
        {
            val += PieceValue[move.PromotionPieceType];
            //pawn "loss"
            val--;
        }
        if (move.IsCastles)
        {
            val += 50;
        }
        //evaluate piece positions
        val += EvaluatePosition(move.TargetSquare, move.MovePieceType, isWhite) - EvaluatePosition(move.StartSquare, move.MovePieceType, isWhite);
        return val;
    }

    private int EstimateMove(Move move)
    {
        return PieceValue[move.CapturePieceType];
    }


    /*
    < 5 seconds --> 2
    if > 20 and more than opponent --> 4
    else --> 3
    */
    private int CalculateDepth(ChessChallenge.API.Timer timer)
    {
        return 6;
        int t = timer.MillisecondsRemaining / 40;
        if (t > 5000)
        {
            return 5;
        }
        if (t > 1000)
        {
            return 4;
        }
        return 3;
    }


    public static void QuickSortNow(Move[] moves, int[] iInput, int start, int end)
    {
        if (start < end)
        {
            int pivot = Partition(moves, iInput, start, end);
            QuickSortNow(moves, iInput, start, pivot - 1);
            QuickSortNow(moves, iInput, pivot + 1, end);
        }
    }

    public static int Partition(Move[] moves, int[] iInput, int start, int end)
    {
        int pivot = iInput[end];
        int pIndex = start;

        for (int i = start; i < end; i++)
        {
            if (iInput[i] >= pivot)
            {
                int temp = iInput[i];
                Move tempM = moves[i];

                iInput[i] = iInput[pIndex];
                moves[i] = moves[pIndex];
                iInput[pIndex] = temp;
                moves[pIndex] = tempM;
                pIndex++;
            }
        }
        int anotherTemp = iInput[pIndex];
        Move anotherTempM = moves[pIndex];
        iInput[pIndex] = iInput[end];
        moves[pIndex] = moves[end];
        iInput[end] = anotherTemp;
        moves[end] = anotherTempM;
        return pIndex;
    }


    public int EvaluateTotalBoardMaterial(Board board)
    {
        int val = 0;
        PieceList[] pieces = board.GetAllPieceLists();
        foreach (PieceList list in pieces)
        {
            val += PieceValue[list[0].PieceType] * list.Count;
        }
        return val;
    }
}