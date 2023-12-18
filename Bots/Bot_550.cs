namespace auto_Bot_550;
using ChessChallenge.API;
///using static ChessChallenge.Application.ConsoleHelper;
///using System;

public class Bot_550 : IChessBot
{


    /*
        HI SEBASTIAN!!!
        
        HERE I GIVE YOU A BRIEF HISTORY OF ME DEVELOPING THE BOT AND ITS MOST INTERESTING TECHNICAL FEATURES.
        WHY OCTOBOT? LIKE AN OCTOPUS, IT SEARCHES DYNAMICALLY THE TREESEARCH WITH ITS TENCTACLES, OF COURSE...
        THE ONLY REFERENCE I USED ARE YOUR CHESS VIDEOS, BUT FOR THE MOST PART I CAME UP TO IT BY MYSELF WITH A LOT OF HEADACHES.
        I PUT A FEW COMMENTS TO HELP YOU KNOW WHAT IS GOING ON, BECAUSE I HAD TO COMPACT THE CODE SO MUCH I ENDED UP USING MANY CODING TRICKS, MATH TRICKS AND OPTIMIZATIONS, SO THE READABILITY DROPPED DRAMMATICALLY IN SOME PLACES...
        IF NEEDED, THE COMMENTS WITH THE TRIPLE "/" CAN BE UNCOMMENTED TO GET A SMALL BUT MAYBE USEFUL OUTPUT ON THE CONSOLE (TELLS YOU HOW MANY EVALUATIONS HAS DONE, THE DEEPEST TREEBRANCH IT EXPLORED AND THE TOTAL TIME ELAPSED) 
        HAVE A NICE DAY AND GREETINGS FROM ITALY!!! SHISH!!!
        
        
        Andrea Colognese
        
        P.S. For any doubt, you may write to  (see in the description field my favourite email)
        
        
        HISTORY:
         - IN AUGUST, I DOWNLOAED ECLIPSE ON MY LAPTOP;
         - THEN I BECAME MAD FOR INSTALLING THE TOOLS NEEDED TO CODE IN C#;
         - OH BOY, WHAT A MESS (THE INFORMATION TO MAKE A GOOD C# SETUP IN ECLIPSE IS NOT EASY TO FIND!!!)
         - IS IT NORMAL THAT THE IDE DOESN'T HIGHLIGHT SYNTAX ERRORS?!?
         - WHY DO I HAVE TO LAUNCH THE PROJECT FROM THE COMMAND PROMPT? ARE WE STILL IN THE 80's?
         - I NEED SOME TIME TO RECOVER...
         - OK, IT'S SEPTEMBER: LET'S START THE DEVELOPMENT!!!
         - *tip tip tip tip tap tap tap....*
         - YOU LITTLE SILLY EVIL BOT OF MY BOOTS... 
         - OK, FINALLY SOMETHING THAT WORKS!!! LET'S IMPROVE IT...
         - OH, NOW IT TAKES A LONG TIME TO SEE IF THE TWEAKS ARE ACTUALLY GOOD. THANKFULLY I HAVE A LOOOOT OF FREETIM-[UNIVERSITY STARTS]
         - OK, STILL IMPROVING...
         - STILL IMPROVING BUT SLOWLY...
         - OK NOW ITS OCTOBER 1st AND ITS 8 pm: LETS TIDE UP THE CODE AND... HERE WE GO, I SENT IT!!!
         - THAT WAS AN EAAAASY RIDE... 
         
         
         
        MAIN FEATURES:
            
            * ALPHA-BETA PRUNING 
            * DYNAMIC MINIMAX TREESEARCH
            * CACHE (TRANSPOSITION TABLES)
            * RAW STRATEGY TO MAKE/AVOID DRAWS
            
             
         
        
    */
    public bool isWhitePlayer;

    public static int[] pieceValues = new int[] { 0, 50, 150, 350, 550, 1100, 100000 }, pieceAttackerValues = new int[] { 0, 30, 13, 11, 9, 2, 1 }, values = new int[65536], finalStrategyLookup = new int[]{   -3000,   -5000,  -10000, -1000000,
                                                                                                                                                                                                          1000000,       0,       1,        0,
                                                                                                                                                                                                            -3000,   -5000,       0,        0,
                                                                                                                                                                                                          1000000,       0,       0,        0
                                                                                                                                                                                                     };
    public int POS_INFINITE = 1000000000, NEG_INFINITE = -1000000000, amountOfPieces;
    ///public int counter, evalCounter, timeCounter = 0;
    public static ulong[] hashes = new ulong[65536];


    public Move Think(Board board, Timer timer)
    {


        amountOfPieces = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);

        isWhitePlayer = board.IsWhiteToMove;
        Move[] moves = getOrderedMoves(board);
        ///counter = 40;
        ///evalCounter = 0;


        //IF THE TIME LEFT IS TOO LOW, RETURN THE FIRST LEGAL MOVE (NOT IDEAL BUT STILL BETTER THAN LOSING FOR TIMEOUT...) 
        Move bestMove = moves[0];
        int timeLeft = timer.MillisecondsRemaining;
        if (timeLeft < 1100)
        {
            return bestMove;
        }

        //SEARCH FOR A GOOD LEGAL MOVE
        int bestValue = NEG_INFINITE, currentValue = bestValue;
        Move[] newMoves;

        foreach (Move move in moves)
        {



            board.MakeMove(move);
            if (board.IsDraw())
            {

                //ELABORATE A STRATEGY (eh eh...)
                currentValue = finalStrategyLookup[isLosing(board, timer, false) + (isLosing(board, timer, true) * 4)];
                if (currentValue == 1)
                {
                    if (timeLeft < 5000)
                    {
                        board.UndoMove(move);
                        return move;
                    }
                    else
                    {
                        currentValue = POS_INFINITE - 1;
                    }
                }


            }
            else
            {
                newMoves = getOrderedMoves(board);
                if (newMoves.Length == 0)
                {       //CHECKMATE I WIN!!!!

                    ///timeCounter += timer.MillisecondsElapsedThisTurn;
                    ///Log("Bot, DEPTH: " + (40 - counter) + ", EVALS: " + evalCounter + ", TOT TIME: " + timeCounter, false, ConsoleColor.Green);
                    board.UndoMove(move);
                    return move;

                }
                else
                {
                    currentValue = searchMove(board, timer, newMoves, ((board.PlyCount % 8) < 2) ? 1300 : 500, 40, !isWhitePlayer, bestValue);//EvaluateBoard(board);//
                }
            }
            if (currentValue > bestValue)
            {
                bestMove = move;
                bestValue = currentValue;
            }
            board.UndoMove(move);
        }
        ///timeCounter += timer.MillisecondsElapsedThisTurn;
        ///Log("Bot, DEPTH: " + (40 - counter) + ", EVALS: " + evalCounter + ", TOT TIME: " + timeCounter, false, ConsoleColor.Green);
        return bestMove;
    }




    public int searchMove(Board board, Timer timer, Move[] moves, float depth, int secondDepth, bool turn, int alphaValue)
    {

        ///counter = secondDepth < counter ? secondDepth : counter;
        float quotient = depth / (float)moves.Length;
        bool diveDeeper = quotient > 1 && secondDepth > 0, isWhiteTurn = isWhitePlayer == turn;
        int currentValue, bestValue = isWhiteTurn ? NEG_INFINITE : POS_INFINITE;
        Move[] newMoves;



        foreach (Move move in moves)
        {

            board.MakeMove(move);

            newMoves = diveDeeper ? getOrderedMoves(board) : board.GetLegalMoves();




            if (newMoves.Length > 0)
            {



                currentValue = diveDeeper ? searchMove(board, timer, newMoves, quotient, secondDepth - 1, !turn, bestValue) : EvaluateBoard(board, true);



            }
            else
            {


                if (diveDeeper && isWhiteTurn && board.IsInStalemate())
                {
                    currentValue = 0;//STALEMATE, GOOD ONLY IF OTHER MOVES ARE BAD
                }
                else
                {

                    board.UndoMove(move);   //CHECKMATE I WIN!!!!        OR     CHECKMATE I LOSE  :(
                    return (isWhiteTurn ? POS_INFINITE : NEG_INFINITE);

                }




            }




            bestValue = (currentValue > bestValue && isWhiteTurn) || (currentValue < bestValue && !isWhiteTurn) ? currentValue : bestValue;

            board.UndoMove(move);
            if (bestValue < alphaValue && !isWhiteTurn)
            { //PRUNING?
                return bestValue;   //  *KATA-KLUNCH* (I have BIG scissors...)  
            }






        }


        return bestValue;

    }

    public Move[] getOrderedMoves(Board board)
    {
        Move[] moves = board.GetLegalMoves(), orderedMoves = new Move[moves.Length];
        int[] moveValues = new int[moves.Length];



        int actualValue, ausValue;
        Move move, ausMove;
        for (int i = 0; i < moves.Length; i++)
        {
            move = moves[i];
            actualValue = getMoveValue(board, move);

            for (int j = 0; j < moves.Length; j++)
            {

                if (orderedMoves[j].IsNull)
                {
                    //PLACE
                    orderedMoves[j] = move;
                    moveValues[j] = actualValue;
                    j = 100000;
                }
                else
                {
                    if (actualValue > moveValues[j])
                    {
                        //SWAP
                        ausMove = move;
                        ausValue = actualValue;

                        move = orderedMoves[j];
                        actualValue = moveValues[j];

                        orderedMoves[j] = ausMove;
                        moveValues[j] = ausValue;
                    }
                }
            }


        }

        return orderedMoves;
    }


    //THIS FUNCTION RETURNS A VALUE THAT SPECIFIES APPROXIMATELY HOW GOOD THE GIVEN MOVE IS (THIS IS USED TO ORDER MOVES FOR THE SEARCH)
    public int getMoveValue(Board board, Move move)
    {


        //USUALLY, A MOVE IS GOOD IF:

        //THE MOVING PIECE HAS LOW VALUE
        //IT CAPTURES A HIGH VALUE PIECE
        //IT IS A PROMOTION

        return (move.IsCapture ? (pieceValues[(int)move.CapturePieceType] / 10) : 0) + (amountOfPieces < 22 ? ((7 - (int)move.MovePieceType) * 30) : ((int)move.MovePieceType < 3 ? (int)move.MovePieceType * 100 : 0) - 50) + (move.IsPromotion ? pieceValues[(int)move.PromotionPieceType] : 0);




    }

    public int EvaluateBoard(Board board, bool alsoMoves)
    {

        ///evalCounter++;

        //CHECK IF THIS BOARD IS IN THE TRANSPOSITION TABLE / CACHE (ONLY 65536 EVALUATIONS ARE STORED, BUT IT WORKS FINE AND ACCESS TO THE ARRAY IS PRETTY FAST)
        ulong hash = board.ZobristKey;
        if (hashes[hash & 65535] == hash)
        {
            return values[hash & 65535] * (isWhitePlayer ? 1 : -1);
        }

        //EVALUATION (POINTS ARE POSITIVE IF THEY'RE FOR THE WHITE PLAYER, NEGATIVE IF THEY'RE FOR THE BLACK PLAYER)
        int whitePoints = 0;
        Piece piece;

        //COUNTING VALUE OF PIECES
        for (int i = 0; i < 64; i++)
        {

            piece = board.GetPiece(new Square(i));
            whitePoints += pieceValues[(int)piece.PieceType] * (piece.IsWhite ? 5 : -5);

        }
        if (alsoMoves)
        {
            //COUNT POSSIBLE MOVES AND EVALUATE THEM

            Move[] firstMoves = board.GetLegalMoves(), secondMoves;
            board.ForceSkipTurn();
            secondMoves = board.GetLegalMoves();
            board.UndoSkipTurn();

            whitePoints += getAllMovesValue(board.IsWhiteToMove ? firstMoves : secondMoves) - getAllMovesValue(board.IsWhiteToMove ? secondMoves : firstMoves);

        }


        values[hash & 65535] = whitePoints;
        hashes[hash & 65535] = hash;

        return isWhitePlayer ? whitePoints : -whitePoints;

    }



    //THIS FUNCTION RETURNS A VALUE THAT TELL HOW MUCH A PLAYER IS CLOSE TO DEAT- ehm ehm... I mean... DEFEAT.  
    // 0 = EVERYTHING SEEMS OK (THAT DOESN'T MEAN THE PLAYER IS WINNING, HE'S JUST NOT LOSING)
    // 1 = LOSING BY TIMEOUT
    // 2 = LOSING BY PIECES
    // 3 = LOSING BOTH BY TIMEOUT AND PIECES (WHAT A SHAME!!!)
    public int isLosing(Board board, Timer timer, bool isMe)
    {
        return (((isMe ? -1 : 1) * EvaluateBoard(board, false)) > 3500 ? 2 : 0) + ((timer.MillisecondsRemaining < 10000 && ((timer.OpponentMillisecondsRemaining - timer.MillisecondsRemaining) * (isMe ? 1 : -1)) > 3000) ? 1 : 0);//3500 = 700 * 5
    }



    //THIS FUNCTION RETURNS A VALUE THAT SPECIFIES EXACTLY HOW GOOD THE GIVEN MOVES ARE (THIS IS USED IN THE BOARD EVALUATION)

    //MOVES ARE GOOD IF:

    //THERE ARE CAPTURES AND THE MOVING PIECES ARE GOOD ATTACKERS (THEY HAVE LOW VALUE)
    //IT IS A PROMOTION
    //THERE ARE SQUARES THAT ARE ATTACKED(PROTECTED) BY MANY PIECES (see math magic)

    public int getAllMovesValue(Move[] whiteMoves)
    {
        int whitePoints = 0;
        int[] whiteProtectedSquares = new int[64];

        foreach (Move move in whiteMoves)
        {
            whiteProtectedSquares[move.TargetSquare.Index]++;                                                           //     ****************** math magic *************************
            whitePoints += (move.IsCapture ? pieceAttackerValues[(int)move.MovePieceType] : 0) + (move.IsPromotion ? 8 : 0) + (whiteProtectedSquares[move.TargetSquare.Index] * 2 - 1);
        }

        return whitePoints;
    }



}