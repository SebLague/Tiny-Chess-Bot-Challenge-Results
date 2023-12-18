namespace auto_Bot_373;
using ChessChallenge.API;
using System;

public class Bot_373 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves(true);

        if (moves.Length != 0)
        {
            //Move[] groves = board.GetLegalMoves();
            int glass = 0;
            int goodness = 0;
            for (int i = 0; i < moves.Length; i++)
            {
                if (moves[i].CapturePieceType == PieceType.Queen)
                {
                    if (goodness < 6)
                    {
                        goodness = 6;
                        glass = i;
                    }

                }
                else if (moves[i].CapturePieceType == PieceType.Bishop)
                {
                    if (goodness < 5)
                    {
                        goodness = 5;
                        glass = i;
                    }

                }
                else if (moves[i].CapturePieceType == PieceType.Rook)
                {
                    if (goodness < 4)
                    {
                        goodness = 4;
                        glass = i;
                    }
                }
                else if (moves[i].CapturePieceType == PieceType.Knight)
                {
                    if (goodness < 3)
                    {
                        goodness = 3;
                        glass = i;
                    }
                }


            }
            return moves[glass];
        }
        else
        {


            //return moves[0];
            Move[] groves = board.GetLegalMoves();

            int furlong = 0;
            int furlongindex = (groves.Length - 1);
            var goodmoves = new int[63];
            int countertop = 0;
            for (int i = 0; i < groves.Length; i++)

            {

                if (Math.Abs(groves[i].TargetSquare.Rank - groves[i].StartSquare.Rank) >= furlong)
                {
                    furlong = Math.Abs(groves[i].TargetSquare.Rank - groves[i].StartSquare.Rank);
                    furlongindex = i;
                    /*DivertedConsole.Write(groves[i]);*/
                }
                if (board.IsWhiteToMove == false)
                {
                    if (-(groves[i].TargetSquare.Rank - groves[i].StartSquare.Rank) == 1)
                    {

                        goodmoves[countertop] = i;
                        countertop++;
                        /*DivertedConsole.Write(goodmoves[countertop-1]);*/
                    }
                }
                else
                {
                    if (-(groves[i].TargetSquare.Rank - groves[i].StartSquare.Rank) == -1)
                    {

                        goodmoves[countertop] = i;
                        countertop++;
                        /*DivertedConsole.Write(goodmoves[countertop-1]);*/
                    }
                }
            }
            int farback = 8;
            int farbackindex = 0;


            /*DivertedConsole.Write(goodmoves[countertop - 1]);*/
            Random random = new Random();
            farbackindex = random.Next(countertop);

            if (farbackindex >= groves.Length)
            {
                return groves[furlongindex];
            }
            return groves[goodmoves[farbackindex]];
        }
    }

}
