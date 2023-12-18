namespace auto_Bot_582;
using ChessChallenge.API;
using System;

public class Bot_582 : IChessBot
{
    int RepeatToggle = 1;
    PieceType PieceCheckRepeat1, PieceCheckRepeat2;
    Move BestMove, FinalBestMove = Move.NullMove;
    bool IsWhite;
    public Move Think(Board board, Timer timer)
    {
        IsWhite = board.IsWhiteToMove;
        BestMove = FinalBestMove = Move.NullMove;//reset bestmove//avoid illegal move

        FinalBestMove = BestLegaLmove(board, timer);
        /*
        int MoveAheadCounter = 0;//counter for current moves looking into the future
        if(timer.MillisecondsRemaining>5000){//below 5 sec remaining take 0.25 sec turns
          while(timer.MillisecondsElapsedThisTurn<1000){//turn takes less than 1 sec
            MoveAheadCounter++;
            SkipTurns(MoveAheadCounter);

            UndoSkipTurn(MoveAheadCounter);
            return FinalBestMove;
          }
        }else{
          while(timer.MillisecondsElapsedThisTurn<250){//turn takes less than 0.25 sec
            MoveAheadCounter++;
            SkipTurns(MoveAheadCounter);

            UndoSkipTurn(MoveAheadCounter);
            return FinalBestMove;
          }
        }
        */
        return FinalBestMove;
    }



    //------------------------------------------------------------------------------------

    Move BestLegaLmove(Board board, Timer timer)
    {

        Move[] LegalMoves = board.GetLegalMoves();
        BestMove = Move.NullMove;
        int AvailableMoves = LegalMoves.Length;
        int BestMoveValue = 0;
        int[] AvailableMovesValue, SavePieceType, SavePieceSquare;
        AvailableMovesValue = new int[AvailableMoves];
        SavePieceType = new int[AvailableMoves];
        SavePieceSquare = new int[AvailableMoves];
        int[] PawnRankValue = { 125, 150, 300, 1000 };
        int PawnValue = 100;//seperate int to enable dynamic pawn value
        int[] PieceValue = { 0, PawnValue, 300, 300, 500, 1000, 2000 };
        //No Piece//pawn,knight,bishop,rook,queen,King
        int[] StartSquareAttacked = { 0, ((int)(PawnValue * 0.5)), 110, 110, 290, 800, 1800 };///edit modifiers later to determine best value
        int[] TargetSquareAtacked = { 0, ((int)(PawnValue * 1.2)), 360, 360, 650, 1000, 2000 };
        //dont need a no piece in these arrays but allows them to play nice with the other arrays
        if (AvailableMoves == 0) { return Move.NullMove; }//my turn king cant move//prevent ai from bricking


        //DivertedConsole.Write(AvailableMoves);


        for (int M = 0; M != AvailableMoves; M++)
        {//search best move loop
            Move move = LegalMoves[M];
            int PieceCaptured = (int)move.CapturePieceType;
            int PieceMoved = (int)move.MovePieceType;
            int moveValue = 0;
            //onsole.WriteLine(M + " " + move.StartSquare + " " + move.TargetSquare + " " + move.MovePieceType);


            if (move == Move.NullMove) { continue; }//redundent if legalMoves works right

            board.MakeMove(move);//Play move and calulate move value
                                 //DivertedConsole.Write("MoveCaptureBoolean: " + move.IsCapture);
            if (board.IsInCheckmate() == true)
            {//always play checkmate
                BestMove = move;
                board.UndoMove(move);
                return BestMove;
            }

            if (board.IsInCheck() == true)
            {//if move checks add value
                moveValue = moveValue + 50;
            }

            //sets enemyPawnPieceValue
            if (move.CapturePieceType == PieceType.Pawn)
            {//dynamic setting of Enemy pawn value
                if (IsWhite == true)
                {
                    for (int W = 3; W != 7; W++)
                    {                //W-3 to stay within array bounds
                        if (move.TargetSquare.Rank == W) { PawnValue = PawnRankValue[W - 3]; }
                    }
                }
                else
                {
                    for (int B = 4; B != 0; B--)
                    {                //4-B to stay within array bounds
                        if (move.TargetSquare.Rank == B) { PawnValue = PawnRankValue[4 - B]; }
                    }
                }
            }


            for (int P = 0; P != 7; P++)
            {//for loop less tokens than series of if statements
                if (PieceCaptured == P)
                {//1=pawn 2=knight 3=bishop 4=rook 5=queen 6=King 0=None
                    moveValue = moveValue + PieceValue[P];
                    //DivertedConsole.Write("PieceType# capture check: " + P);
                }
                if (board.FiftyMoveCounter > 75 && move.IsCapture == true) { moveValue = (moveValue + 125); }
                if (board.FiftyMoveCounter > 75 && P == 1) { moveValue = (moveValue + 125); }//adds  value to encourage captures and pawn move
            }
            //DivertedConsole.Write("moveValue: " + moveValue);


            board.UndoMove(move);//----------------------
            PawnValue = 100;//reset pawn value

            //sets MyPawnPieceValue
            if (move.MovePieceType == PieceType.Pawn)
            {//dynamic setting of pawn value//to be reset at end of move////////
                if (board.FiftyMoveCounter > 75) { moveValue = (moveValue + 125); }//adds slighty more than checks value to encourage pawn movement
                if (move.IsPromotion == true) { moveValue = (moveValue + 1000); }//high value on promotion not higher than pawn value so that promotion dousnt happen if promotion tile is being attacked by enemy
                if (IsWhite == true)
                {
                    for (int W = 3; W != 7; W++)
                    {                //W-3 to stay within array bounds
                        if (move.StartSquare.Rank == W) { PawnValue = PawnRankValue[W - 3]; }
                    }
                }
                else
                {
                    for (int B = 4; B != 0; B--)
                    {                //4-B to stay within array bounds
                        if (move.StartSquare.Rank == B) { PawnValue = PawnRankValue[4 - B]; }
                    }
                }
            }

            //starts at 1 to bypass null in arrray
            for (int Pa = 1; Pa != 7; Pa++)
            {//if move results in piece moving into enemy atacked square minus piece moved value from moveValue*modifier
                if (board.SquareIsAttackedByOpponent(move.StartSquare) == true)
                {//add value to any move with piece being attacked
                 //Pa+1 to bypass none in PieceType enum
                    if (PieceMoved == (Pa))
                    {
                        moveValue = (moveValue + StartSquareAttacked[Pa]);
                        //DivertedConsole.Write("Move: " + moveValue);
                        //DivertedConsole.Write("M " + M);
                        SavePieceType[M] = (Pa);//assigns piece being attacked and the Start square to array for SavePiece loop to use
                                                //DivertedConsole.Write("SavePieceType[M]" + SavePieceType[M]);
                        SavePieceSquare[M] = move.StartSquare.Index;
                        //DivertedConsole.Write("Startsquare atacked move value = " + moveValue);
                    }
                }
                board.MakeMove(move);//necessary so that squares behind sliding pieces being attacked by sliding pieces count as Attacked by enemy otherwise sliding pieces being attacked "blocks" enemy
                board.ForceSkipTurn();//skip back to my turn
                                      //this way of implementing wastes more time but saves tokens
                if (board.SquareIsAttackedByOpponent(move.TargetSquare) == true)
                {
                    if (PieceMoved == (Pa))
                    {
                        moveValue = (moveValue - TargetSquareAtacked[Pa]);
                        //DivertedConsole.Write("Targetsquare atacked move value = " + moveValue);
                    }
                }
                board.UndoSkipTurn();
                board.UndoMove(move);
            }

            if (move.MovePieceType == PieceCheckRepeat1 && move.MovePieceType == PieceCheckRepeat2)
            {
                moveValue = (moveValue - 125);
                //DivertedConsole.Write("piece check repeat");
            }
            //same piece is checking/chasing king subtract 125 from moveValue
            if (board.IsDraw() == true)
            {
                moveValue = (moveValue - 225);
                //DivertedConsole.Write("isdraw");
            }///curb repetition


            //DivertedConsole.Write("-----EndmoveValue: " + moveValue);
            AvailableMovesValue[M] = moveValue;//assign move value to array for further sorting
                                               //DivertedConsole.Write("-----EndmoveValueInAray: " + AvailableMovesValue[M]);
            PawnValue = 100;//reset pawn value
                            //DivertedConsole.Write("savepiecetype: " + SavePieceType[M]);//////////////////
        }

        //SavePiece loop----going through each move again to add to value if move saves another piece--helps in situations where bot would previously favour moving a high value piece over killing the offending piece with something else
        for (int M = 0; M != AvailableMoves; M++)
        {
            Move move = LegalMoves[M];
            //DivertedConsole.Write(move.IsCapture);
            if (move.IsCapture == true)
            {//probably wont save much time filtering but whatever
                board.MakeMove(move);//made after capture check so that its not making unnecassery moves
                board.ForceSkipTurn();//skip back to my turn
                for (int Mb = 0; Mb != AvailableMoves; Mb++)
                {//loop through SavePieceSquare
                    Square SpS = new Square(SavePieceSquare[Mb]);//creats square out of SavePieceSquare Index
                    if (move.StartSquare == SpS)
                    {//needed so that it dousnt "save" itself when capturing a piece
                     //DivertedConsole.Write(Mb);
                     //DivertedConsole.Write("ThisMove");
                     //DivertedConsole.Write("--------");
                        continue;
                    }
                    if (board.SquareIsAttackedByOpponent(SpS) == false && SavePieceType[Mb] >= 1)
                    {//if move results in square no longer being attacked move has saved the piece
                     //DivertedConsole.Write(Mb);
                     //DivertedConsole.Write(SavePieceType[Mb]);//should only be from 1-6
                     //DivertedConsole.Write("--------");
                        AvailableMovesValue[M] = (int)(AvailableMovesValue[M] + (PieceValue[SavePieceType[Mb]] * 0.8));//if piece saved adds that pieces value*0.75 to moves value///might change value later
                        break;//prevents it from taking into acount saving multiple pieces but with the way its set up with out this it can save the same piece more then once if allowed
                    }

                }
                board.UndoSkipTurn();
                board.UndoMove(move);
            }
        }


        //DivertedConsole.Write("all move calculated");
        ///DivertedConsole.Write(AvailableMovesValue.Length);
        for (int Q = 0; Q != AvailableMoves; Q++)
        {//find highest value move and assign to Bestmove
         //DivertedConsole.Write(Q + " " + AvailableMovesValue[Q]);
            if (AvailableMovesValue[Q] == BestMoveValue)
            {
                Random RNG = new Random();//if two move have same value rng pick one
                if ((RNG.Next(0, 2)) == 1) { BestMove = LegalMoves[Q]; BestMoveValue = AvailableMovesValue[Q]; }//if all moves come back zero(game start) move would be random
            }
            if (AvailableMovesValue[Q] > BestMoveValue) { BestMove = LegalMoves[Q]; BestMoveValue = AvailableMovesValue[Q]; }
            if (BestMove == Move.NullMove) { BestMove = LegalMoves[Q]; }//with low move count and BestMove initialized to nullmove can output nullmove
                                                                        //DivertedConsole.Write("-----EndmoveValueInAraySorting: "+ Q + "---" + AvailableMovesValue[Q]);
        }
        //repeat RepeatToggle//assign check repeat
        if (RepeatToggle == 1)
        {
            RepeatToggle++; if (board.IsInCheck() == true) { PieceCheckRepeat1 = BestMove.MovePieceType; }
        }
        else if (RepeatToggle == 2) { RepeatToggle--; if (board.IsInCheck() == true) { PieceCheckRepeat2 = BestMove.MovePieceType; } }


        //DivertedConsole.Write("check1: " + PieceCheckRepeat1);
        //DivertedConsole.Write("check2: " + PieceCheckRepeat2);
        //DivertedConsole.Write("IsWhite: " + board.IsWhiteToMove);
        //DivertedConsole.Write("Available Moves: " + AvailableMoves);
        //DivertedConsole.Write("Total ms: " + timer.MillisecondsRemaining);
        //DivertedConsole.Write("Turn Time: " + timer.MillisecondsElapsedThisTurn + "ms");
        //DivertedConsole.Write("Best Move: " + BestMove.StartSquare + " " + BestMove.TargetSquare + " " + BestMove.MovePieceType);
        //DivertedConsole.Write("Best Move Value: " + BestMoveValue);
        //DivertedConsole.Write("--------------");

        return BestMove;
    }

}
