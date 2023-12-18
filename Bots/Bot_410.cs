namespace auto_Bot_410;
using ChessChallenge.API;
using System;
public class Bot_410 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king



    double[] pieceValues = { 100, 305, 333, 563, 950, 400, -100, -305, -333, -563, -950, -400 };
    double[] kingRow = { 0.95, 1, 0.9, 0.7, 0.7, 0.9, 1, 0.95 };
    double[] king = { 1.1, 0.8, 0.5, 0.4, 0.4, 0.5, 0.8, 1.1 };
    double[] pawnrow = { 0.91, 0.9, 0.95, 1, 1, 0.95, 0.9, 0.91 };
    double[] pawnrank = { 0.0, 1, 1.1, 1.2, 1.3, 1.4, 1.6, 3 };
    double[] night = { 0.65, 0.8, 0.9, 1.1, 1.1, 0.9, 0.8, 0.65 };
    double[] bishop = { 0.8, 0.95, 0.9, 1, 1, 0.9, 0.95, 0.8 };
    double[] rook = { 0.8, 0.8, 0.9, 1, 1, 0.9, 0.8, 0.8 };
    int depth = 5;

    public Move Think(Board board, Timer timer)
    {

        if (timer.MillisecondsRemaining < 3000)
        {
            depth = 3;
        }
        else if (timer.MillisecondsRemaining < 10000)
        {
            depth = 4;
        }
        Move myMove = board.GetLegalMoves()[0];
        double spieler = 1;
        if (!board.IsWhiteToMove)
        {
            spieler = -1;
        }






        minimax(spieler, depth, -900000, +900000);


        double minimax(double spieler, int tiefe, double alpha, double beta)
        {


            Move[] legal = board.GetLegalMoves();

            Array.Sort(legal, (y, x) => x.IsCapture.CompareTo(y.IsCapture));

            if (tiefe == 0)
            {
                return bewerten(spieler);
            }

            double maxwert = alpha;




            foreach (Move move in legal)
            {

                board.MakeMove(move);
                double wert = -minimax(-spieler, tiefe - 1, -beta, -maxwert);
                board.UndoMove(move);
                if (wert > maxwert)
                {
                    maxwert = wert;

                    if (tiefe == depth)
                    {

                        myMove = move;
                    }
                    if (maxwert >= beta)
                    {
                        break;
                    }
                }
            }
            return maxwert;

        }
        double bewerten(double spieler)
        {
            double bewertung = 0;
            if (board.IsInCheckmate())
            {
                return 100000 * spieler;
            }
            else if (board.IsDraw())
            {
                return 0;
            }
            else if (board.IsRepeatedPosition())
            {
                return 0;
            }
            PieceList[] array = board.GetAllPieceLists();
            for (int j = 0; j < array.Length; j++)
            {
                if (j == 0 || j == 6)
                {
                    if (spieler == 1)
                    {
                        if (!(pawnrank[0] == 0.0))
                        {
                            Array.Reverse(pawnrank);
                        }
                    }
                    else
                    {
                        if (pawnrank[0] == 0.0)
                        {
                            Array.Reverse(pawnrank);
                        }
                    }
                    foreach (Piece p in array[j])
                    {
                        bewertung += pieceValues[j] * pawnrow[p.Square.File] * spieler * pawnrank[p.Square.Rank];
                    }
                }
                else if (j == 5 || j == 11)
                {
                    bewertung += kingRow[array[j][0].Square.File] * spieler * pieceValues[j] * king[array[j][0].Square.Rank];
                }
                else if (j == 1 || j == 7)
                {
                    foreach (Piece p in array[j])
                    {
                        bewertung += pieceValues[j] * night[p.Square.File] * night[p.Square.Rank] * spieler;
                    }
                }
                else if (j == 2 || j == 8)
                {
                    foreach (Piece p in array[j])
                    {
                        bewertung += pieceValues[j] * bishop[p.Square.File] * bishop[p.Square.Rank] * spieler;
                    }
                }
                else if (j == 3 || j == 9)
                {
                    foreach (Piece p in array[j])
                    {
                        bewertung += pieceValues[j] * rook[p.Square.File] * spieler;
                    }
                }
                else
                {
                    PieceList pieces = array[j];
                    bewertung += (double)pieces.Count * pieceValues[j] * spieler;
                }
            }

            return bewertung;
        }

        return myMove;
    }

    // Test if this move gives checkmate


}