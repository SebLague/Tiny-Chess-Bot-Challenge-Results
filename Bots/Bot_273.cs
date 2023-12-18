namespace auto_Bot_273;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Bot_273 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int width = 100;
        int maxDepth = 0;

        bool isWhite = board.IsWhiteToMove;
        int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };
        Random randi = new Random();
        Move[] legalMoves = board.GetLegalMoves();
        float[] scores = new float[legalMoves.Length];

        int moveIdx = 0;
        foreach (Move currMove in legalMoves)
        {
            board.MakeMove(currMove);

            Move[] opMoves = board.GetLegalMoves();
            foreach (Move opMove in opMoves)
            {
                board.MakeMove(opMove);
                if (board.IsInCheckmate())
                {
                    scores[moveIdx] -= 1000 / (float)width / (float)opMoves.Length;
                }
                board.UndoMove(opMove);
            }
            opMoves = board.GetLegalMoves(true);
            if (opMoves.Length == 0)
            {
                opMoves = board.GetLegalMoves();
            }
            int opMovesLength = opMoves.Length;
            if (opMoves.Length > 0)
            {
                for (int w = 0; w < width; w++)
                {
                    foreach (Move currOpMove in opMoves)
                    {
                        List<Move> movesMade = new List<Move>();
                        board.MakeMove(currOpMove);
                        movesMade.Add(currOpMove);

                        int depth = 0;
                        while (depth <= maxDepth)
                        {
                            Move[] tempMoves = board.GetLegalMoves();
                            foreach (Move move in tempMoves)
                            {
                                board.MakeMove(move);
                                if (board.IsInCheckmate())
                                {
                                    scores[moveIdx] += 1000 / (float)width / (float)opMovesLength;
                                }
                                board.UndoMove(move);
                            }
                            tempMoves = board.GetLegalMoves(true);
                            if (tempMoves.Length == 0)
                            {
                                tempMoves = board.GetLegalMoves();
                            }
                            if (tempMoves.Length > 0)
                            {
                                int rIdx = randi.Next(tempMoves.Length);
                                board.MakeMove(tempMoves[rIdx]);
                                movesMade.Add(tempMoves[rIdx]);

                                tempMoves = board.GetLegalMoves();
                                foreach (Move move in tempMoves)
                                {
                                    board.MakeMove(move);
                                    if (board.IsInCheckmate())
                                    {
                                        scores[moveIdx] -= 1000 / (float)width / (float)opMovesLength;
                                    }
                                    board.UndoMove(move);
                                }
                                tempMoves = board.GetLegalMoves(true);
                                if (tempMoves.Length == 0)
                                {
                                    tempMoves = board.GetLegalMoves();
                                }
                                if (tempMoves.Length > 0)
                                {
                                    rIdx = randi.Next(tempMoves.Length);
                                    board.MakeMove(tempMoves[rIdx]);
                                    movesMade.Add(tempMoves[rIdx]);
                                    depth++;
                                }
                                else if (board.IsInCheckmate())
                                {
                                    scores[moveIdx] += 1000 / (float)width / (float)opMovesLength;
                                    depth++;
                                }
                                else
                                {
                                    depth++;
                                }
                            }
                            else if (board.IsInCheckmate())
                            {
                                scores[moveIdx] -= 1000 / (float)width / (float)opMovesLength;
                                depth++;
                            }
                            else
                            {
                                depth++;
                            }
                        }

                        for (int i = 1; i < 7; i++)
                        {
                            ulong pieces = board.GetPieceBitboard((PieceType)i, isWhite);
                            scores[moveIdx] += (float)(pieceValues[i] * BitboardHelper.GetNumberOfSetBits(pieces)) / (float)width / (float)opMovesLength;
                            pieces = board.GetPieceBitboard((PieceType)i, !isWhite);
                            scores[moveIdx] -= (float)(pieceValues[i] * BitboardHelper.GetNumberOfSetBits(pieces)) / (float)width / (float)opMovesLength;
                        }



                        for (int i = movesMade.Count - 1; i >= 0; i--)
                        {
                            board.UndoMove(movesMade[i]);
                        }
                    }
                }

            }
            else if (board.IsDraw())
            {
                scores[moveIdx] = 0;
            }
            else if (board.IsInCheckmate())
            {
                return currMove;
            }
            board.UndoMove(currMove);
            moveIdx++;
        }
        return legalMoves[Array.IndexOf(scores, scores.Max())];
    }
}