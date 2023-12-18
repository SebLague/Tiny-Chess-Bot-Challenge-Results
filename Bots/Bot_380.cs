namespace auto_Bot_380;
using ChessChallenge.API;

public class Bot_380 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {


        /**
             HI SEBASTIAN, I REALLY HOPE YOU SEE THIS!
             I love your videos so much!
             They encouraged me to truly get back into programming,
             and are so damning soothing and relaxing to watch!
             - Sky Sherwood (if you for some reason show this in the video, blur my last name please)
        */


        // the variable to return
        Move finalMove = new Move();

        // A move to look forward a turn (I'm not dealing with 14243 moves ahead, it will fry my laptop)
        Move[] futureMoves;

        // A square to look forward a turn
        Square finalSquare = new Square();

        // settign up some arrays
        Move[] moves = board.GetLegalMoves(false);
        Move[] captureMoves = board.GetLegalMoves(true);

        // rng for goofy ahh moments
        System.Random rng = new System.Random();

        // Are we trying to capture?
        bool capture = false;

        // To keep track of what piece we are attacking with
        bool wasteOurKing = true;
        bool wasteOurQueen = true;
        bool wastePeice = true;

        int counter = board.PlyCount;





        /// are we feeling daring?
        short feelingDaringArentWe = (short)rng.Next(1000);






        /*
            Sorry I made you have to slog through this
            I'ms till very new to programming
         */


        // if we can capture a piece, try to capture it
        while (wasteOurKing == true && wasteOurQueen == true) // I want to try to avoid attacking with the king (or if its teh first few moves, the queen)
        {
            if (rng.Next(0, 10 + counter) % rng.Next(2, 3) == 0) // little complex randomness that should (hopefully) make the bot more defensive as time goes on
            {
                // attack mode
                if (captureMoves.Length > 0)
                {
                    while (true) // if the move we make will result in a piece being captured, don't make it
                    {
                        capture = true;
                        finalMove = captureMoves[rng.Next(captureMoves.Length)];
                        finalSquare = finalMove.TargetSquare;

                        board.MakeMove(finalMove);
                        futureMoves = board.GetLegalMoves(true);

                        for (int i = 0; i < futureMoves.Length - 1; i++)
                        {
                            if (futureMoves[i].TargetSquare == finalSquare)
                            {
                                switch (finalMove.MovePieceType)
                                {
                                    case PieceType.Pawn:
                                        wastePeice = false;
                                        break;
                                    case PieceType.Queen:
                                        wastePeice = true;
                                        break;
                                    case PieceType.King:
                                        wastePeice = true;
                                        break;
                                    case PieceType.Knight:
                                        if (rng.Next(0, 2) == 0)
                                        {
                                            wastePeice = true;
                                            break;
                                        }
                                        else
                                        {
                                            wastePeice = false;
                                            break;
                                        }
                                    case PieceType.Bishop:
                                        if (rng.Next(0, 2) == 0)
                                        {
                                            wastePeice = true;
                                            break;
                                        }
                                        else
                                        {
                                            wastePeice = false;
                                            break;
                                        }
                                    case PieceType.Rook:
                                        if (rng.Next(0, 2) == 0)
                                        {
                                            wastePeice = true;
                                            break;
                                        }
                                        else
                                        {
                                            wastePeice = false;
                                            break;
                                        }
                                }
                            }
                        }
                        board.UndoMove(finalMove);
                        if (wastePeice == false)
                        {
                            break;
                        }
                    }
                }
                else
                {
                    capture = false;
                    finalMove = moves[rng.Next(moves.Length)];
                }
            }
            else
            {
                // defend mode
                capture = false;
                finalMove = moves[rng.Next(moves.Length)];
            }


            if (finalMove.MovePieceType == PieceType.King)
            {
                if (rng.Next(0, 90) == 0) // small chance for moving the king
                {
                    wasteOurKing = false;
                    break;
                }
                else
                {
                    wasteOurKing = true;
                }
            }
            else
            {
                wasteOurKing = false; // in the end, we dont want to waste our king
            }

            // Avoid using the queen in the first few round as it is our most valuable piece
            // (other than the king)
            if (finalMove.MovePieceType == PieceType.Queen)
            {
                if (board.PlyCount > 9)
                {
                    wasteOurQueen = false;
                }
                else if (rng.Next(0, 10) == 0)
                {
                    wasteOurQueen = false;
                }
            }
        }

        // if we can promote a pawn, promote that lil guy!
        if (!capture)
        {
            if (finalMove.MovePieceType == PieceType.Pawn && !finalMove.IsPromotion)
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    if (finalMove.MovePieceType == PieceType.Pawn && finalMove.IsPromotion)
                    {
                        finalMove = moves[i];
                        break;
                    }
                }
            }
        }

        // if this is the first few moments of the game, make a standard move
        if (board.PlyCount < 2)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                if (feelingDaringArentWe % 7 == 0)
                {
                    if (moves[i].MovePieceType == PieceType.Knight)
                    {
                        finalMove = moves[i];
                    }
                }
                else if (moves[i].MovePieceType == PieceType.Pawn)
                {
                    finalMove = moves[i];
                }
            }
        }

        // If we're feeling daring, we'll make a random move
        if (feelingDaringArentWe % 15 == 0)
        {
            finalMove = moves[rng.Next(moves.Length)];
        }

        return finalMove;
    }
}