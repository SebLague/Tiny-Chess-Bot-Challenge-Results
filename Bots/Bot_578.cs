namespace auto_Bot_578;
//using System.Threading;
using ChessChallenge.API;
using System;
// using ChessChallenge.Chess;


public class Bot_578 : IChessBot
{
    public bool IsWhite;


    float evauluate(Move[] moves, Board board, int moveNumber, int[] otherPiecesPrev, int[] myPiecesPrev, Move[] moves2, int moveNumber2)
    {
        int[] otherPieces = { 8, 2, 2, 2, 1, 1 };
        int[] myPieces = { 8, 2, 2, 2, 1, 1 };
        var pieceLists = board.GetAllPieceLists();

        float value = 0;

        int count = 0;
        if (IsWhite == true)
        {
            while (count < 6)
            {
                otherPieces[count] = pieceLists[count + 6].Count;
                myPieces[count] = pieceLists[count].Count;
                count++;
            }
        }
        else
        {
            while (count < 6)
            {
                otherPieces[count] = pieceLists[count].Count;
                myPieces[count] = pieceLists[count + 6].Count;
                count++;
            }
        }

        value += 1 * (otherPiecesPrev[0] - otherPieces[0]); //pawn
        value += 3 * ((otherPiecesPrev[1] - otherPieces[1]) + (otherPiecesPrev[2] - otherPieces[2]));  //kight and bishop
        value += 5 * (otherPiecesPrev[3] - otherPieces[3]);  //rook
        value += 100 * (otherPiecesPrev[4] - otherPieces[4]);  //queen

        value += 1 * (myPieces[0] - myPiecesPrev[0]); //pawn
        value += 3 * ((myPieces[1] - myPiecesPrev[1]) + (myPieces[2] - myPiecesPrev[2]));  //kight and bishop
        value += 5 * (myPieces[3] - myPiecesPrev[3]);  //rook
        value += 100 * (myPieces[4] - myPiecesPrev[4]);  //queen

        float material = 0;

        material += 1 * (myPieces[0] - otherPieces[0]); //pawn
        material += 3 * ((myPieces[1] - otherPieces[1]) + (myPieces[2] - otherPieces[2])); //knight and bishop
        material += 5 * (myPieces[3] - otherPieces[3]); //rook
        material += 9 * (myPieces[4] - otherPieces[4]); //queen



        if (board.IsInCheckmate() == true)
        {
            value = 1000;
        }
        else if (board.IsInStalemate() == true && material <= -5)
        {
            value = 500;
        }
        else if (board.IsInStalemate() == true && material >= 5)
        {
            value = -500;
        }
        else if (moves[moveNumber].MovePieceType == PieceType.Pawn && board.GameMoveHistory.Length > 40)
        {
            value += 0.05f;
        }
        else if (moves2[moveNumber2].MovePieceType == PieceType.Pawn && (board.GameMoveHistory.Length > 70 || board.GameMoveHistory.Length < 70))
        {
            value += -0.05f;
        }
        else if (moves[moveNumber].MovePieceType == PieceType.King && board.GameMoveHistory.Length < 40)
        {
            value += -0.1f;
        }
        else if (moves[moveNumber].MovePieceType == PieceType.Rook && board.GameMoveHistory.Length < 40)
        {
            value += -0.2f;
        }


        return value;

    }



    public Move Think(Board board, Timer timer)
    {
        IsWhite = board.IsWhiteToMove;
        Move[] moves = board.GetLegalMoves();
        Random rnd = new Random();
        int bestMove = rnd.Next(moves.Length);

        int moveNumber = 0;
        float bestScore = -1000;
        int count = 0;

        float score = 0;

        Move[] moves2 = board.GetLegalMoves();
        int moveNumber2 = 0;
        float[] worstScore = new float[1000];

        Move[] moves3 = board.GetLegalMoves();
        int moveNumber3 = 0;
        float[] bestScore3 = new float[1000];

        Move[] moves4 = board.GetLegalMoves();
        int moveNumber4 = 0;
        float[] worstScore4 = new float[1000];

        int[] otherPiecesPrev = { 8, 2, 2, 2, 1, 1 };
        int[] myPiecesPrev = { 8, 2, 2, 2, 1, 1 };
        var pieceLists = board.GetAllPieceLists();

        //openings
        if (board.GameMoveHistory.Length <= 1 && moves.Length == 20)
        {
            bestMove = 1;
        }

        else
        {

            if (IsWhite == true)//evaluate position
            {
                while (count < 6)
                {
                    otherPiecesPrev[count] = pieceLists[count + 6].Count;
                    myPiecesPrev[count] = pieceLists[count].Count;
                    count++;
                }
            }
            else
            {
                while (count < 6)
                {
                    otherPiecesPrev[count] = pieceLists[count].Count;
                    myPiecesPrev[count] = pieceLists[count + 6].Count;
                    count++;
                }
            }

            while (moveNumber < moves.Length)
            {
                board.MakeMove(moves[moveNumber]);
                moves2 = board.GetLegalMoves();
                moveNumber2 = 0;

                worstScore[moveNumber] = 5000;
                while (moveNumber2 < moves2.Length)
                {

                    board.MakeMove(moves2[moveNumber2]);

                    moves3 = board.GetLegalMoves();
                    moveNumber3 = 0;

                    if (timer.MillisecondsRemaining > 2000 && moves3.Length >= 1) //have time
                    {
                        bestScore3[moveNumber2] = -5000;
                        while (moveNumber3 < moves3.Length)
                        {
                            board.MakeMove(moves3[moveNumber3]);

                            moves4 = board.GetLegalMoves();
                            moveNumber4 = 0;

                            worstScore4[moveNumber3] = 5000;
                            while (moveNumber4 < moves4.Length)
                            {
                                board.MakeMove(moves4[moveNumber4]);

                                score = evauluate(moves, board, moveNumber, otherPiecesPrev, myPiecesPrev, moves2, moveNumber2);

                                if (score < worstScore4[moveNumber3])
                                {
                                    worstScore4[moveNumber3] = score;
                                }

                                board.UndoMove(moves4[moveNumber4]);
                                moveNumber4++;

                            }

                            if (worstScore4[moveNumber3] > bestScore3[moveNumber2])
                            {
                                bestScore3[moveNumber2] = worstScore4[moveNumber3];
                            }

                            board.UndoMove(moves3[moveNumber3]);
                            moveNumber3++;
                        }
                    }

                    else //low on time
                    {
                        bestScore3[moveNumber2] = evauluate(moves, board, moveNumber, otherPiecesPrev, myPiecesPrev, moves2, moveNumber2);
                    }

                    if (bestScore3[moveNumber2] < worstScore[moveNumber])
                    {
                        worstScore[moveNumber] = bestScore3[moveNumber2];
                    }

                    board.UndoMove(moves2[moveNumber2]);
                    moveNumber2++;
                }

                if (worstScore[moveNumber] > bestScore)
                {
                    bestMove = moveNumber;
                    bestScore = worstScore[moveNumber];
                }

                board.UndoMove(moves[moveNumber]);
                moveNumber++;
            }



            count = 0; //randomizer
            while (count < 50)
            {
                moveNumber = rnd.Next(moves.Length);

                if (worstScore[moveNumber] == bestScore)
                {
                    bestMove = moveNumber;
                    count++;
                }
            }
        }

        return moves[bestMove];
    }
}