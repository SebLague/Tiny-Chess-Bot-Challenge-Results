namespace auto_Bot_237;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

//legal moves per turn starts at 20, peaks around 40, and is around 15 at the end of the game

public class Bot_237 : IChessBot
{
    // Piece values:        null,   pawn,   knight, bishop, rook,   queen,  king
    int[] pieceValues = { 0, 10, 30, 30, 50, 90, 91 };

    // https://lichess.org/opening
    Dictionary<ulong, string[]> openningMoves = new Dictionary<ulong, string[]>
    {
        [0xb792d82d18345f3a] = new string[] { "e2e4", "d2d4", "g1f3", "c2c4", "b2b3" },
        [0xd8985f343b028184] = new string[] { "c7c5", "e7e5", "e7e6", "c7c6", "d7d5", "d7d6" },
        [0xc13102b0dafb9dde] = new string[] { "g8f6", "d7d5", "e7e5", "f7f5", "g7g6", "d5d6" },
        [0x78375378303a90c4] = new string[] { "d7d5", "g8f6", "c7c5" },
        [0xddb8f230aeb78c24] = new string[] { "g8f6", "e7e5", "c7c5", "e7e6", "c7c6" }
    };

    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        if (openningMoves.ContainsKey(board.ZobristKey))
            return new Move((string)openningMoves[board.ZobristKey].GetValue((int)Math.Round(Math.Pow(rng.NextDouble(), 2) * (openningMoves[board.ZobristKey].Length - 1))), board);
        //return new Move((string)GetExpRandomItem(openningMoves[board.ZobristKey]), board);

        Move[] moveSet = board.GetLegalMoves();

        //take an immediate mate if available
        foreach (Move move in moveSet)
        {
            if (MoveIsCheckmate(board, move))
                return move;
        }

        //exclude immediate opponent mates, if possible
        Move[] filteredMoves = moveSet.Where(m => !HasImmediateMateResponse(board, m)).ToArray();
        if (filteredMoves.Length > 0)
            moveSet = filteredMoves;

        /*int baselineNumEvals = 3500 / moveSet.Length;
        return moveSet.OrderByDescending(m => AvgMCMoveScore(board, m, 50, (int)(baselineNumEvals * (1 + SimpleMoveScore(m) / 10)))).ToArray()[0];*/
        return moveSet.OrderByDescending(m => AvgMCMoveScore(board, m, 50, (int)((3500 / moveSet.Length) * (1 + SimpleMoveScore(m) / 10)))).ToArray()[0];
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool HasImmediateMateResponse(Board board, Move move)
    {
        board.MakeMove(move);

        //check all opponent responses for mate
        bool result = board.GetLegalMoves().Where(m => MoveIsCheckmate(board, m)).Count<Move>() > 0;

        board.UndoMove(move);

        return result;
    }

    /*object GetExpRandomItem(Array arr)
    {
        return arr.GetValue((int)Math.Round(Math.Pow(rng.NextDouble(), 2) * (arr.Length - 1)));
    }*/

    float AvgMCMoveScore(Board board, Move move, int depthLimit, int numToAvg)
    {
        float avg = 0;
        for (int i = 0; i < numToAvg; i++)
        {
            avg += MCMoveScore(board, move, depthLimit);
        }
        return avg /= numToAvg;
    }

    int MCMoveScore(Board board, Move move, int depthLimit)
    {
        int result = 0;
        Move nextMove = move;
        Stack<Move> movesTaken = new Stack<Move>();
        bool myMove = true;
        for (int i = 1; i <= depthLimit; i++)
        {
            movesTaken.Push(nextMove);
            board.MakeMove(nextMove);
            if (board.IsInCheckmate())
            {
                if (myMove)
                    result = depthLimit - i; //foundMate = true;
                else
                    result = i - depthLimit; //foundLoss = true;
                break;
            }
            if (board.IsDraw())
                break;

            nextMove = board.GetLegalMoves(false).OrderByDescending(m => SimpleMoveScore(m)).ToArray()[0];

            myMove = !myMove;
        }
        foreach (Move m in movesTaken)
        {
            board.UndoMove(m);
        }

        return result;
    }

    double SimpleMoveScore(Move move)
    {
        return pieceValues[(int)move.CapturePieceType] +
                pieceValues[(int)move.PromotionPieceType] / 10 +
                pieceValues[(int)move.MovePieceType] / 100 +
                (move.IsEnPassant ? 0.1 : 0) +
                rng.NextDouble() / 100;
    }
}