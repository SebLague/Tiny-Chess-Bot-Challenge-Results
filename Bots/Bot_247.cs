namespace auto_Bot_247;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_247 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 305, 500, 900, 10000 };
    float[] pawnPosValue = { 0f, 0f, 0f, 0f, 0.005f, 0.02f, 0.1f, 0f };
    float[] knightPosValue = { -0.03f, 0f, 0.03f, 0.05f, 0.05f, 0.03f, 0f, -0.03f };
    float[] bishopPosValue = { -0.02f, 0f, 0.02f, 0.04f, 0.04f, 0.02f, 0f, -0.02f };
    Dictionary<ulong, float> memory = new();

    public Move Think(Board board, Timer timer)
    {
        // Get all legal moves
        Move[] allMoves = board.GetLegalMoves();
        allMoves = allMoves.OrderByDescending(o => o.IsCapture).ToArray();

        // Play forced move
        if (allMoves.Length == 0) return allMoves[0];

        // Init best and second best move
        List<Move> bestMoves = new();
        float bestEval = board.IsWhiteToMove ? -100000 : 100000;
        Move secondBestMove = Move.NullMove;
        float secondBestEval = bestEval;

        int minimaxDepth = (timer.MillisecondsRemaining < 10000) ? 3 : 1;
        while (true)
        {
            bestMoves = new();
            bestEval = board.IsWhiteToMove ? -100000 : 100000;
            secondBestMove = Move.NullMove;
            secondBestEval = bestEval;
            foreach (Move move in allMoves)
            {
                // Run minimax algorithm
                board.MakeMove(move);
                float eval = minimax(board, minimaxDepth, -10000, 10000, board.IsWhiteToMove);
                board.UndoMove(move);
                if ((eval == 10000 && board.IsWhiteToMove) || (eval == -10000 && !board.IsWhiteToMove))
                {
                    return move;
                }
                if (eval == bestEval)
                    bestMoves.Add(move);
                else if ((eval < bestEval) ^ board.IsWhiteToMove)
                {
                    bestMoves = new List<Move>() { move };
                    bestEval = eval;
                }
                else if ((eval < secondBestEval) ^ board.IsWhiteToMove)
                {
                    secondBestMove = move;
                    secondBestEval = eval;
                }
            }
            // Increase depth if time is sufficient
            if (timer.MillisecondsRemaining > 10000 && timer.MillisecondsElapsedThisTurn < (0.0225 * timer.GameStartTimeMilliseconds + timer.IncrementMilliseconds) * 0.2f)
            {
                minimaxDepth++;
                DivertedConsole.Write("increase depth to " + minimaxDepth);
                continue;
            }
            break;
        }

        // Choose one move to play from best moves
        Random rng = new();
        Move moveToPlay = bestMoves[rng.Next(bestMoves.Count)];

        // Avoid repeating move in winning position
        if (board.IsWhiteToMove ^ (secondBestEval < 0))
        {
            board.MakeMove(moveToPlay);
            bool repeated = board.IsRepeatedPosition();
            board.UndoMove(moveToPlay);
            if (repeated && secondBestMove != Move.NullMove)
                return secondBestMove;
        }

        if (board.GameMoveHistory.Count() != 0)
        {
            Move lastMove = board.GameMoveHistory.Last();
            if (moveToPlay.IsCapture || moveToPlay.MovePieceType == PieceType.Pawn || lastMove.IsCapture || moveToPlay.MovePieceType == PieceType.Pawn || memory.Count() > 1000000)
                memory = new();
        }
        return moveToPlay;
    }

    // Evaluate position for one color of the position
    float evalColorPosition(Board board, int color, int[] pawnNum)
    {
        float meterial = 0;
        PieceList[] allPieceList = board.GetAllPieceLists();

        // Pawn
        foreach (Piece piece in allPieceList[6 * color])
        {
            meterial += pieceValues[1] * (1f + pawnPosValue[Math.Abs(piece.Square.Rank - 7 * color)]);
        }

        // Knight
        foreach (Piece piece in allPieceList[1 + 6 * color])
        {
            meterial += pieceValues[2] * (1f + knightPosValue[piece.Square.File] + knightPosValue[piece.Square.Rank]);
        }

        // Bishop
        foreach (Piece piece in allPieceList[2 + 6 * color])
        {
            meterial += pieceValues[3] * (1f + bishopPosValue[piece.Square.File] + bishopPosValue[piece.Square.Rank]);
        }

        // Rook
        foreach (Piece piece in allPieceList[3 + 6 * color])
        {
            meterial += pieceValues[4] * (1.02f - 0.01f * pawnNum[piece.Square.File]);
        }

        // Queen
        meterial += pieceValues[5] * allPieceList[4 + 6 * color].Count;

        // calculate space control
        int space = 0;
        for (int i = 0; i < 6; i++)
        {
            PieceList list = allPieceList[6 * color + i];
            foreach (Piece piece in list)
            {
                space += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(list.TypeOfPieceInList, piece.Square, board, list.IsWhitePieceList));
            }
        }

        return meterial + space * 0.06f;
    }

    // Evaluate the given position
    float evalPosition(Board board)
    {
        ulong memoryKey = board.ZobristKey;
        if (memory.ContainsKey(memoryKey))
            return memory[memoryKey];

        // Calculate how many pawn each file has
        int[] pawnNum = new int[8];
        PieceList[] allPieceList = board.GetAllPieceLists();
        foreach (Piece piece in allPieceList[0]) pawnNum[piece.Square.File]++;
        foreach (Piece piece in allPieceList[6]) pawnNum[piece.Square.File]++;

        float meterialWhite = evalColorPosition(board, 0, pawnNum);
        float meterialBlack = evalColorPosition(board, 1, pawnNum);

        /*
        if (verbose)
        {
            DivertedConsole.Write("current pos: " + (meterialWhite - meterialBlack).ToString());
            if (meterialWhite > meterialBlack)
            {
                DivertedConsole.Write("White is better");
            }
            else if (meterialWhite < meterialBlack)
            {
                DivertedConsole.Write("Black is better");
            }
            else
            {
                DivertedConsole.Write("Even");
            }
        }
        */
        memory[memoryKey] = meterialWhite - meterialBlack;
        return meterialWhite - meterialBlack;
    }

    float minimax(Board board, int depth, float alpha, float beta, bool maximizingPlayer)
    {
        if (board.IsInCheckmate())
            if (maximizingPlayer) return -10000;
            else return 10000;

        if (board.IsDraw())
            return 0;

        if (depth == 0)
            return evalPosition(board);

        Move[] moves = board.GetLegalMoves();
        if (maximizingPlayer)
        {
            float maxEval = -10000;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                float eval = minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha) break;
            }
            return maxEval;
        }
        else
        {
            float minEval = 10000;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                float eval = minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha) break;
            }
            return minEval;
        }
    }
}