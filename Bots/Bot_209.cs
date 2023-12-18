namespace auto_Bot_209;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_209 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen
    int[] pieceValues = { 0, 100, 300, 300, 500, 900 };
    int[] centerPieceBonus = { 0, 20, 100, 100, 100, 100 };

    // MCTS data
    Dictionary<(ulong, bool), (int, int)> mctsNodes = new Dictionary<(ulong, bool), (int, int)>();

    public Move Think(Board board, Timer timer)
    {
        int mctsIter = 0;
        while (mctsIter < 9000 && timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 50)
        {
            MctsIteration(board, 20);
            mctsIter++;
        }
        Move[] moveOptions = board.GetLegalMoves();
        Move moveToPlay = moveOptions[0];
        int highestVisits = 0;
        foreach (Move move in moveOptions)
        {
            board.MakeMove(move);
            (ulong, bool) hash_key = (board.ZobristKey, board.IsRepeatedPosition());
            (int childVisits, int childSumValue) = mctsNodes.ContainsKey(hash_key) ? mctsNodes[hash_key] : (1, -10000);
            board.UndoMove(move);
            if (childVisits > highestVisits)
            {
                moveToPlay = move;
                highestVisits = childVisits;
            }
        }
        return moveToPlay;
    }

    // Recursive Mcts iteration
    int MctsIteration(Board board, int depth_remaining)
    {
        int value;
        (ulong, bool) hash_key = (board.ZobristKey, board.IsRepeatedPosition());
        (int visits, int sumValue) = mctsNodes.ContainsKey(hash_key) ? mctsNodes[hash_key] : (0, 0);

        if (board.IsInCheckmate())
        {
            value = -10000;
        }
        else if (board.IsDraw())
        {
            value = 0;
        }
        else if (depth_remaining == 0 || visits == 0)
        {
            value = 0;
            for (int ii = 1; ii < 6; ii++)
            {
                ulong myPieceBitboard = board.GetPieceBitboard((PieceType)ii, board.IsWhiteToMove);
                ulong opPieceBitboard = board.GetPieceBitboard((PieceType)ii, !board.IsWhiteToMove);

                value += BitboardHelper.GetNumberOfSetBits(myPieceBitboard) * pieceValues[ii];
                value -= BitboardHelper.GetNumberOfSetBits(opPieceBitboard) * pieceValues[ii];

                value += BitboardHelper.GetNumberOfSetBits(myPieceBitboard & 139081753165824) * centerPieceBonus[ii];
                value -= BitboardHelper.GetNumberOfSetBits(opPieceBitboard & 139081753165824) * centerPieceBonus[ii];
            }
            if (board.IsInCheck()) { value -= 200; }
        }
        else
        {
            Move[] moveOptions = board.GetLegalMoves();
            Move moveToExplore = moveOptions[0];
            double highestValue = -10000;
            foreach (Move move in moveOptions)
            {
                int capturedValue = pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];
                board.MakeMove(move);
                (ulong, bool) new_hash_key = (board.ZobristKey, board.IsRepeatedPosition());
                int checkBonus = board.IsInCheck() ? 200 : 0;
                (int childVisits, int childSumValue) =
                    mctsNodes.ContainsKey(new_hash_key) ?
                    mctsNodes[new_hash_key] :
                    (1, -sumValue / visits - capturedValue - checkBonus);
                board.UndoMove(move);
                double uctValue = 100 * Math.Sqrt(Math.Log(visits) / childVisits) - childSumValue / childVisits;
                if (uctValue > highestValue)
                {
                    moveToExplore = move;
                    highestValue = uctValue;
                }
            }
            board.MakeMove(moveToExplore);
            value = -MctsIteration(board, depth_remaining - 1);
            board.UndoMove(moveToExplore);
        }
        mctsNodes[hash_key] = (visits + 1, sumValue + value);
        return value;
    }
}