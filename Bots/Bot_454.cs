namespace auto_Bot_454;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_454 : IChessBot
{

    //Hello Seb... don't look... this is a warning (ง'̀-'́)ง


    public int[] pieceScores = { 0, 5, 10, 15, 25, 50, 20 };
    //None = 0, Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6

    public int freePieceTakingScore = 20;
    //this is so the AI doesnt like to move the same type of piece a bunch of times in a row 
    //everytime the AI moves the same piece 2x in a row it gets a - debuff
    public int samePieceDebuff = 0;
    public int prevPieceID = 0;

    //the mad king will every 2nd turn want to move the king no matter what (toned down cuz the bot becomes a bit... bad...)
    public bool wantsToMoveKing = true;



    public Move Think(Board board, Timer timer)
    {
        wantsToMoveKing = !wantsToMoveKing;
        Random rnd = new Random();
        Move[] moves = board.GetLegalMoves();

        PieceList[] allPieces = board.GetAllPieceLists();

        //the key is the move and the int is the score of the move
        Dictionary<Move, int> moveCalculations = new Dictionary<Move, int>();



        //counts the amount of friendly and enemy units on screen
        int enemyCount = 0;
        int friendlyCount = 0;
        int enemyScore = 0;
        int friendlyScore = 0;
        int indx = 0;
        //probs better way but this is the AVA way (^u^)
        foreach (PieceList piece in allPieces)
        {
            //why am i using the index again????
            //update i forgot find out in the future
            if (indx < 5)
            {
                if (piece.IsWhitePieceList)
                {
                    enemyCount += piece.Count;
                    enemyScore += (pieceScores[(int)piece.TypeOfPieceInList] * piece.Count);
                }
                else
                {
                    friendlyCount += piece.Count;
                    friendlyScore += (pieceScores[(int)piece.TypeOfPieceInList] * piece.Count);
                }
            }
            else
            {
                if (piece.IsWhitePieceList)
                {
                    enemyCount += piece.Count;
                    enemyScore += (pieceScores[(int)piece.TypeOfPieceInList] * piece.Count);
                }
                else
                {
                    friendlyCount += piece.Count;
                    friendlyScore += (pieceScores[(int)piece.TypeOfPieceInList] * piece.Count);
                }
            }

            indx++;
        }

        //Seb... i just told you not to read my code... ಠ_ಠ

        //winning, losing, neutral
        string botState = "";
        if (Math.Abs(friendlyScore - enemyScore) < 65)
        {
            botState = "neutral";
        }
        else
        {
            if (enemyScore > friendlyScore)
            {
                botState = "losing";
            }
            else
            {
                botState = "winning";
            }
        }







        foreach (Move move in moves)
        {
            moveCalculations.Add(move, 0);

            //check if move is gonna result is a checkmate later on
            //also if bot is losing try to find a stalemate
            board.MakeMove(move);

            if (botState == "losing")
            {
                if (board.IsInStalemate())
                {
                    moveCalculations[move] += 120;
                }
                if (board.IsInsufficientMaterial())
                {
                    moveCalculations[move] += 120;
                }
                if (board.IsRepeatedPosition())
                {
                    moveCalculations[move] += 5;
                }
            }

            //Seb... ಠ╭╮ಠ its not pretty...


            Move[] enemyPossibleMovesAfterMove = board.GetLegalMoves();
            bool foundCheckMate = false;
            foreach (Move emove in enemyPossibleMovesAfterMove)
            {
                board.MakeMove(emove);
                if (board.IsInCheckmate())
                {
                    moveCalculations[move] -= 1000;
                    foundCheckMate = true;
                }




                //
                // A BIG WASTE OF TIME  (╯°□°）╯︵ ┻━┻
                // my attempt at looking farther into the future than 1 move
                //


                //if (enemyCount + friendlyCount < 10 || botState == "losing" || wantsToMoveKing)
                //{
                //    Move[] allPossiblePlayerMovesAfterEnemyMove = board.GetLegalMoves();

                //    foreach (Move pmove in allPossiblePlayerMovesAfterEnemyMove)
                //    {
                //        board.MakeMove(pmove);
                //        //checking if i can checkmate if can consider it a good move... mby... idk really its 23:24 
                //        if (board.IsInCheckmate())
                //        {
                //            moveCalculations[move] += 50;
                //        }
                //        //if its a capture check if its a big one like a pawn taking the queen or a rook
                //        if (pmove.IsCapture)
                //        {
                //            int pieceValueDif = pieceScores[(int)board.GetPiece(move.TargetSquare).PieceType] - pieceScores[(int)board.GetPiece(move.StartSquare).PieceType];
                //            if(pieceValueDif > 20)
                //            {
                //                moveCalculations[move] += 14;
                //            }
                //        }
                //        if (pmove.IsPromotion)
                //        {
                //            moveCalculations[move] += 20;
                //        }

                //        board.UndoMove(pmove);
                //    }
                //}


                board.UndoMove(emove);
                //if enemy will have option to checkmate bot then do -score and continue
                if (foundCheckMate)
                {
                    break;
                }
            }


            board.UndoMove(move);




            //that usless thing that makes the king BAD
            //gonna leave it in anyways so that the others have a fighting chance (⌐■_■)
            if (move.MovePieceType == PieceType.King && wantsToMoveKing)
            {
                moveCalculations[move] += 4;
            }

            //check if moving the same same piece as last turn. if yes add the debuff
            if ((int)move.MovePieceType == prevPieceID)
            {
                moveCalculations[move] -= samePieceDebuff;
            }



            //check if the current square is being attacked by an enemy if yes we are encouraged to move it
            //check if the square is being protected by one of our pieces
            if (board.SquareIsAttackedByOpponent(move.StartSquare))
            {
                board.ForceSkipTurn();
                if (board.SquareIsAttackedByOpponent(move.StartSquare))
                {
                    moveCalculations[move] += 6;
                }
                board.UndoSkipTurn();
            }


            //if the move captures something add +2 score to it and calculate the value of the capture
            if (move.IsCapture)
            {
                moveCalculations[move] += 2;

                int pieceValueDif = pieceScores[(int)board.GetPiece(move.TargetSquare).PieceType] - pieceScores[(int)board.GetPiece(move.StartSquare).PieceType];


                if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    moveCalculations[move] += pieceValueDif;
                    //!!!!!!!!!! should also check the the piece is then gonna be protected by friendly after move
                    //ran out of time gg AVA ur so bad...
                }
                //the piece is free for the taking
                else
                {
                    //pawns wanna be moved forward especially if no threat
                    if (enemyCount + friendlyCount < 14 && move.MovePieceType == PieceType.Pawn)
                    {
                        moveCalculations[move] += 10;
                    }
                    moveCalculations[move] += freePieceTakingScore;
                    //if its a free piece it doesnt matter the dif so just add the abs value
                    moveCalculations[move] += Math.Abs(pieceValueDif);
                    //check for promotion if yes add + score
                    if (move.IsPromotion)
                    {
                        //we only want the queen
                        if (move.PromotionPieceType != PieceType.Queen)
                        {
                            moveCalculations[move] -= 150;
                        }
                        else
                        {
                            moveCalculations[move] += 50;
                        }
                    }
                }
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    moveCalculations[move] += 1000;
                }
                board.UndoMove(move);
            }
            else
            {
                //check if the tile we are moving to is clear
                //being attacked
                if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    moveCalculations[move] += -30;
                    //if its being attacked check the value of the piece and remove that many points
                    moveCalculations[move] += -pieceScores[(int)board.GetPiece(move.StartSquare).PieceType];

                    //do move and check for check or checkmate
                    board.MakeMove(move);
                    if (board.IsInCheckmate())
                    {
                        moveCalculations[move] += 1000;
                    }
                    board.UndoMove(move);
                }
                else
                {
                    //pawns wanna be moved forward especially if no threat
                    if (enemyCount < 8 && move.MovePieceType == PieceType.Pawn)
                    {
                        moveCalculations[move] += 20;
                    }
                    else if (move.MovePieceType == PieceType.Pawn)
                    {
                        moveCalculations[move] += 8;
                    }
                    moveCalculations[move] += 4;
                    //do move and check for check or checkmate
                    board.MakeMove(move);
                    if (board.IsInCheck())
                    {
                        moveCalculations[move] += 10;
                    }
                    if (board.IsInCheckmate())
                    {
                        moveCalculations[move] += 1000;
                    }
                    board.UndoMove(move);

                    //check for promotion if yes add + score
                    if (move.IsPromotion)
                    {
                        //we only want the queen
                        if (move.PromotionPieceType != PieceType.Queen)
                        {
                            moveCalculations[move] -= 150;
                        }
                        else
                        {
                            moveCalculations[move] += 50;
                        }

                    }
                }
            }



        }






        //god wth was i doing here lmfao
        //Seb... what are you doing here... i told you it wasnt pretty... (ಥ﹏ಥ)

        int bestScore = moveCalculations.MaxBy(x => x.Value).Value;
        var movesWithBestScore = moveCalculations.Where(x => x.Value == bestScore).ToArray();


        Move bestMove = movesWithBestScore[rnd.Next(movesWithBestScore.Count())].Key;

        //if he moved the same type of piece be4 increase the debuff
        if ((int)bestMove.MovePieceType == prevPieceID)
        {
            samePieceDebuff += 3;
        }
        //else set it to 0
        else
        {
            samePieceDebuff = 0;
        }

        //msg to Seb. If you are reading this you ignored my request above not to read this ლ(ಠ益ಠლ) c c c... 


        prevPieceID = (int)bestMove.MovePieceType;
        return bestMove;
    }
}