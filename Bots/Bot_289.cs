namespace auto_Bot_289;
using ChessChallenge.API;
using System;
using System.Linq;
//For attempted troubleshooting
//using System.Threading.Tasks;
//using System.Diagnostics;

//Bot 1001

//872/1024, despite having over a 100 more thing of space, I am out of ideas and don't think that 100 is enough from my failed additions
public class Bot_289 : IChessBot
{
    //using Bot 603
    private Random random = new Random();
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 100000 };
    //static Board board;

    public Move Think(Board board, Timer timer)
    {
        //NOte make it only promote to queen under all circumstances

        //Note, function will be moved into the main build for the final submission to save space and potentially add more
        Move[] allMoves = board.GetLegalMoves();
        //Randomising the move orders will still have an effect, it makes it more likely to pick a move that is in the center and better, need to be tested tho
        allMoves = RandomizeArray(allMoves);

        //Make negative incase all moves are come up as negative, so it can play best terrible move
        int score = -999999999;


        //I assigned a random move to it incase their is a problem and also, C# hate if move does not have an assigned value
        Move drawMove = allMoves[random.Next(0, allMoves.Length)];
        Move bestMove = drawMove;

        foreach (Move possibleMoves in allMoves)
        {
            if (MoveIsCheckmate(board, possibleMoves) || MoveIsForcedMate(board, possibleMoves))
            {

                //DivertedConsole.Write("Found checkmate");
                return possibleMoves;
            }
            //Note always ingoring mate seem to make bot worse
            if (WillGetMated(board, possibleMoves))
            {
                //This move lead to defeat should be ingored, if not checkmate
                //DivertedConsole.Write("Insta defeat move detected");
                continue;
            }
            board.MakeMove(possibleMoves);
            //I hate draw, this stops draw at all costs, it will make bot lose games, but it is better if it cause more wins
            if (board.IsDraw() || (0 == board.GetLegalMoves().Count()))
            {
                //DivertedConsole.Write("Do not repeat, stalement or draw");
                board.UndoMove(possibleMoves);
                drawMove = possibleMoves;//If there is no other good move, try priorties drawing move
                continue;
            }
            board.UndoMove(possibleMoves);


            int currentScore = (FutureAttackTotal(board, possibleMoves) /*+ MateAble(board, possibleMoves)*/ + MoveTakePower(board, possibleMoves) + WinEndGame(board, possibleMoves) - MaxDangerDetection(board, possibleMoves) - FutureDefenceTotal(board, possibleMoves));


            //Depending on result, might want to run more tests, 
            //DivertedConsole.Write("Move score is :");
            //DivertedConsole.Write(currentScore.ToString());
            if (currentScore > score)
            {
                //DivertedConsole.Write("Best move so far");
                //My bot is better but does not know how to mate endgame
                score = currentScore;
                bestMove = possibleMoves;
            }
            /*if (WillGetMated(board, possibleMoves))
            {
                return possibleMoves;
            }*/
        }
        if (score == -999999999)
        {
            return drawMove;
        }
        return bestMove;
    }

    private bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
    private bool MoveIsForcedMate(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] counterMoves = board.GetLegalMoves();
        bool allMovesToCheckMate = false;
        foreach (Move move1 in counterMoves)
        {
            board.MakeMove(move1);
            Move[] counterMoves2 = board.GetLegalMoves();
            bool thisEnemyMoveLoses = false;
            foreach (Move move2 in counterMoves2)
            {
                if (MoveIsCheckmate(board, move2))
                {

                    thisEnemyMoveLoses = true;
                    break;
                }
            }
            board.UndoMove(move1);
            if (thisEnemyMoveLoses)
            {
                continue;
            }
            else
            {
                allMovesToCheckMate = false;
                break;
            }

        }
        board.UndoMove(move);
        if (allMovesToCheckMate)
        {
            return true;
        }
        return false;
    }

    private bool WillGetMated(Board board, Move move)
    {
        //board.MakeMove(move);
        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        foreach (Move possibleMove in allMoves)
        {
            //DivertedConsole.Write("Checking possible moves");
            board.MakeMove(possibleMove);
            bool isMate = board.IsInCheckmate();
            //DivertedConsole.Write(isMate.ToString());
            board.UndoMove(possibleMove);
            if (isMate)
            {
                //Do not preform, mateable
                board.UndoMove(move);
                return true;
            }
        }
        //Safe move
        board.UndoMove(move);
        return false;
    }

    //In next few moves, what pieces may the bot win
    private int FutureAttackTotal(Board board, Move move)
    {
        int power = 0;
        int maxPower = 0;
        board.MakeMove(move);
        Move[] possibleMoves = board.GetLegalMoves();
        foreach (Move possibleMove in possibleMoves)
        {
            //The s at the end differ for the varibles
            board.MakeMove(possibleMove);
            Move[] possibleMoves2 = board.GetLegalMoves();
            foreach (Move possibleMove2 in possibleMoves2)
            {
                int potential = MoveTakePower(board, possibleMove2);
                //It is currently run on a per move, best move basis, might be interesting to make it take scores from all runs

                power += potential;
                if (potential > power)
                {
                    maxPower = potential;
                }

            }
            board.UndoMove(possibleMove);
        }
        board.UndoMove(move);
        //Needs more testing, can be mated more but will win more than other options
        //Note a work around if possibleMoves.lenght is 0, probably because of scenerous where already mate, but it move efficent later
        try
        {
            return ((maxPower / 50) + (power / (possibleMoves.Length * 10)));
        }
        catch
        {
            return (maxPower / 10);
        }//add return max power ans see if it improves
        //return (maxPower / 10);
        //return (power/(possibleMoves.Length*10));
    }

    //In the next few turns, what piece might bot lose
    private int FutureDefenceTotal(Board board, Move move)
    {
        int power = 0;
        int maxPower = 0;
        //int numOfMoves = 0;
        board.MakeMove(move);
        Move[] possibleMoves = board.GetLegalMoves();
        foreach (Move possibleMove in possibleMoves)
        {
            int potential = MoveTakePower(board, possibleMove);
            power += potential;
            if (potential > maxPower)
            {
                maxPower = potential;
            }
        }
        board.UndoMove(move);
        try
        {
            return (maxPower / 50 + power / (possibleMoves.Length * 10) / 2);
        }
        catch
        {
            //Move results in a draw and should not be played
            return (10000);
        }

    }

    //Randomise the array, create pesudo determinisitc games, espicailly with low searching
    public Move[] RandomizeArray(Move[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = random.Next(n + 1);
            Move value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
        return array;
    }


    public int MoveTakePower(Board board, Move move)
    {
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
        //DivertedConsole.Write(capturedPieceValue.ToString());
        return capturedPieceValue;
    }

    public int MaxDangerDetection(Board board, Move move)
    {
        int capturedPieceValue = 0;
        board.MakeMove(move);
        Move[] counterMoves = board.GetLegalMoves();
        foreach (Move ifMove in counterMoves)
        {
            Piece capturedPiece = board.GetPiece(ifMove.TargetSquare);
            int capturedPiecePotential = pieceValues[(int)capturedPiece.PieceType];
            //DivertedConsole.Write(capturedPieceValue.ToString());
            if (capturedPiecePotential > capturedPieceValue)
            {
                capturedPieceValue = capturedPiecePotential;
            }
        }
        board.UndoMove(move);
        //DivertedConsole.Write("Max danger detected");
        //DivertedConsole.Write(capturedPieceValue.ToString());
        return capturedPieceValue;
    }

    //More percicion for endgames
    public int WinEndGame(Board board, Move move)
    {
        if (board.GetAllPieceLists().Length > 8)
        {
            return 0;
        }
        int isMoveGood = 0;
        if (MoveIsCheckmate(board, move))
        {
            isMoveGood += 2700;
        }
        board.MakeMove(move);

        Move[] moves0 = board.GetLegalMoves();
        foreach (Move ifMove0 in moves0)
        {
            board.MakeMove(ifMove0);

            Move[] moves1 = board.GetLegalMoves();
            foreach (Move ifMove1 in moves1)
            {
                if (MoveIsCheckmate(board, ifMove1))
                {
                    isMoveGood += 900;
                    continue;
                }
                else
                {
                    board.MakeMove(ifMove1);
                    Move[] moves2 = board.GetLegalMoves();
                    foreach (Move ifMove2 in moves2)
                    {
                        board.MakeMove(ifMove2);
                        Move[] moves3 = board.GetLegalMoves();
                        foreach (Move ifMove3 in moves3)
                        {
                            if (MoveIsCheckmate(board, ifMove3))
                            {
                                isMoveGood += 30;
                            }
                            else
                            {
                                board.MakeMove(ifMove3);
                                Move[] moves4 = board.GetLegalMoves();

                                foreach (Move ifMove4 in moves4)
                                {
                                    board.MakeMove(ifMove4);
                                    Move[] moves5 = board.GetLegalMoves();

                                    foreach (Move ifMove5 in moves5)
                                    {

                                        if (MoveIsCheckmate(board, ifMove5))
                                        {
                                            isMoveGood += 1;
                                        }
                                        /*
                                        else
                                        {
                                            //If want to go more deep
                                            board.MakeMove(ifMove6);
                                            Move[] moves6 = board.GetLegalMoves();



                                            board.MakeMove(ifMove6);
                                        }*/

                                    }

                                    board.MakeMove(ifMove4);
                                }


                                board.MakeMove(ifMove3);
                            }
                        }
                        board.MakeMove(ifMove2);
                    }
                    board.UndoMove(ifMove1);
                }
            }
            board.UndoMove(ifMove0);
        }
        board.UndoMove(move);
        return isMoveGood;
    }
}

