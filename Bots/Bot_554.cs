namespace auto_Bot_554;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


// remove when not debugging
//using ChessChallenge.Application;
//using System.IO.Compression;

public class Bot_554 : IChessBot
{
    public static class Def // AKA - the magic-number zone
    {
        public static int[] pieceValues = { 0, 100, 900, 900, 2500, 8100, 80000 }; // these values are squared to exponentially prioritise higher value pieces
        public static int moveBudget = 20000;
        public static int moveLog = 0;
        public static int maxDepth = 6;
    }
    public struct MOVE // a Move with a score
    {
        float score;
        public Move move;
        public int interest;
        public int Budget;
        public MOVE(Move i, float j, int k)
        {
            move = i;
            score = j;
            interest = k;
            Budget = 0;

        }

        public void setScore(float i) => score = i;
        public float getScore() { return score; }
        public void setBudget(int totalInterest, int moveBudget)
        {
            float ratio = 0;
            if (totalInterest > 0) ratio = interest / (float)totalInterest;
            Budget = (int)(moveBudget * ratio);
            if (Budget < 3) Budget = 0;

        }

        public int getNumOfMoves(Board i)
        {
            return i.GetLegalMoves().Length;
        }
    }



    public class MinMaxFish
    {
        public int totalInterest = 0;
        MOVE[] getLegalMoves(Board board, float startingScore, int moveBudget)
        {
            var m = board.GetLegalMoves();
            var rng = new Random();
            m = m.OrderBy(e => rng.NextDouble()).ToArray();



            List<MOVE> moves = new List<MOVE>();
            foreach (Move i in m)
            {


                int localInterest = 1;
                float localScore = startingScore;

                if (i.MovePieceType != PieceType.Pawn && i.MovePieceType != PieceType.King) // encourage using pieces besides pawns/kings
                {
                    localInterest++;
                    localScore += Def.pieceValues[(int)i.MovePieceType] * 0.1f;
                }

                if (i.TargetSquare.File != 0 && i.TargetSquare.File != 7) // discourage moving pieces to edge tiles
                {
                    localScore += 5;
                }

                if (i.TargetSquare.Rank >= 2 && i.TargetSquare.Rank <= 6) // Encourage developing pieces
                {
                    localScore += 5;
                }

                if (board.IsInCheck()) // encourage moving the king while in check
                {
                    localScore -= 1000;
                    localInterest++;
                }

                if (i.IsCastles) // encourage castling
                {
                    localScore += 5000;
                    localInterest++;
                }
                if (i.IsPromotion) // encourage promotion!
                {
                    localScore += Def.pieceValues[(int)i.PromotionPieceType];
                    localInterest++;
                }

                if (i.IsCapture) // encourage capture
                {
                    localScore += Def.pieceValues[(int)i.CapturePieceType] + 10000;
                    localInterest += 2;
                }
                else
                {
                    localScore -= 5000;
                }

                if (board.SquareIsAttackedByOpponent(i.TargetSquare) || board.SquareIsAttackedByOpponent(i.StartSquare))
                {
                    localInterest++;
                    //localScore -= Def.pieceValues[(int)i.MovePieceType] * 0.5f;
                }

                totalInterest += localInterest;
                moves.Add(new MOVE(i, localScore, localInterest));
            }

            List<MOVE> budgetMoves = new List<MOVE>();

            foreach (MOVE i in moves)
            {
                i.setBudget(totalInterest, moveBudget);
                budgetMoves.Add(i);
            }
            return budgetMoves.ToArray();
        }
        public MOVE Think(Board board, int moveBudget, int depth)
        {
            Random rng = new Random();

            PieceList[] pieces = board.GetAllPieceLists();

            float whiteScore = 0;

            for (int i = 0; i <= 5; i++)
            {
                foreach (Piece piece in pieces[i])
                {
                    whiteScore += Def.pieceValues[i + 1];
                    if (board.SquareIsAttackedByOpponent(piece.Square) && board.IsWhiteToMove)
                    {
                        whiteScore -= Def.pieceValues[i + 1] / 10.0f;
                    }
                }

                foreach (Piece piece in pieces[i + 6])
                {
                    whiteScore -= Def.pieceValues[i + 1];
                    if (board.SquareIsAttackedByOpponent(piece.Square) && !board.IsWhiteToMove)
                    {
                        whiteScore += Def.pieceValues[i + 1] / 10.0f;
                    }
                }
            }
            float score = 0;
            if (board.IsWhiteToMove) score = whiteScore;
            else score = whiteScore * -1;

            MOVE[] moves = getLegalMoves(board, score, moveBudget);
            MOVE bestMove = moves[0];

            foreach (MOVE move in moves)
            {

                board.MakeMove(move.move);

                if (board.IsInCheckmate())
                {
                    move.setScore(int.MaxValue);
                    depth = 0;
                }
                if (board.IsInStalemate() || board.IsInsufficientMaterial() || board.IsFiftyMoveDraw() || board.IsRepeatedPosition())
                {
                    move.setScore(int.MinValue);
                    depth = 0;

                }

                if (move.Budget >= 1 && depth > 0)
                {

                    MinMaxFish i = new MinMaxFish();
                    MOVE a = i.Think(board, move.Budget, depth - 1);
                    move.setScore(a.getScore() * -1);
                }
                board.UndoMove(move.move);

                if (move.getScore() > bestMove.getScore())
                {
                    bestMove = move;
                }

                Def.moveLog += 1;

                //            if (depth ==  Def.maxDepth)
                //            {
                //                ConsoleHelper.Log(
                //  "Move: " + Def.moveLog + '|' + bestMove.move.MovePieceType.ToString() + " to " + bestMove.move.TargetSquare.ToString() +
                //  "\nInterest: " + bestMove.interest +
                //  ", Score:" + bestMove.getScore() +
                //  ", Budget:" + bestMove.Budget +
                //    ", Depth:" + (Def.maxDepth - depth)
                //, false, (ConsoleColor)depth);
                //            }


            }




            return bestMove;

        }


    }


    public Move Think(Board board, Timer timer)
    {



        MinMaxFish fisher = new MinMaxFish();

        Def.moveLog = 0;
        // float x = (timer.MillisecondsRemaining / 60000.0f);
        //  x = (-4 * (float)Math.Pow(x, 2)) + (4 * x);
        //   x = (x * 0.75f) + 0.25f;
        int timeBudget = Def.moveBudget;

        MOVE bestMove = fisher.Think(board, timeBudget, Def.maxDepth);
        return bestMove.move;

    }

}