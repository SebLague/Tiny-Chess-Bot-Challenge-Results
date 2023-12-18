namespace auto_Bot_513;
using ChessChallenge.API;
using System;

public class Bot_513 : IChessBot
{
    #region Global Variable Declaration

    //Declaring the value of the chess pieces
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    #endregion

    #region Think

    //Main Function
    public Move Think(Board board, Timer timer)
    {
        //Return the calculation of bestCapture
        return bestCapture(board);
    }

    #endregion

    #region Utilities

    //Spot a checkmate in one
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheck();
        board.UndoMove(move);
        return isMate;
    }

    //Check if a provided square is protected (Basically SquareIsAttackedByOpponent, but reverse)
    bool SquareIsProtected(Board board, Move move)
    {
        board.MakeMove(move);
        bool isProtected = board.SquareIsAttackedByOpponent(move.TargetSquare);
        board.UndoMove(move);
        return isProtected;
    }

    Move BeProtected(Board board, Move move, Move[] moves)
    {
        Move protectingMove = Move.NullMove;

        foreach (Move m in moves)
        {
            if (m == move)
            {
                continue;
            }

            if (board.SquareIsAttackedByOpponent(m.TargetSquare) && !SquareIsProtected(board, m) && pieceValues[(int)board.GetPiece(m.StartSquare).PieceType] > 500)
            {
                continue;
            }

            board.MakeMove(m);

            if (board.SquareIsAttackedByOpponent(move.StartSquare))
            {
                protectingMove = m;
            }

            board.UndoMove(m);
        }

        return protectingMove;
    }

    Move RemoveThreat(Board board, Move move)
    {
        Move[] moves = board.GetLegalMoves(true);
        return BeProtected(board, move, moves);
    }

    #endregion

    #region Calculation
    Move bestCapture(Board board)
    {
        #region Local Variable Declaration
        //Get all legal moves
        Move[] allMoves = board.GetLegalMoves();

        //Play a random move if all other conditions are failed to be met
        Move randomMove = allMoves[new Random().Next(allMoves.Length)];

        //Declaration of more variables
        Move moveToPlay = randomMove, threatenedMove = randomMove, captureMove = randomMove;
        int highestValueCapture = 0, highestValueThreatened = 0;
        #endregion

        //Loop through all the legal moves
        foreach (Move move in allMoves)
        {
            #region Local Variable Declaration

            //Declaration and setting of some variables
            Piece capturedPiece = board.GetPiece(move.TargetSquare), movingPiece = board.GetPiece(move.StartSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType], movingPieceValue = pieceValues[(int)movingPiece.PieceType];

            #endregion

            #region Move Value Calculations

            //Always play a checkmate in one if detected
            if (MoveIsCheckmate(board, move) || (MoveIsCheck(board, move) && SquareIsProtected(board, move) && movingPieceValue < 900))
            {
                moveToPlay = move;
                break;
            }

            //Check if the piece is threatened, and if
            //movingPieceValue + capturedPieceValue + (SquareIsProtected(board, move) ? (movingPieceValue / 2) : 0)
            //is greater than the current highest value, set it as the new highest and set the threatenedMove as the current move.
            Move reMove = RemoveThreat(board, move); //hehe removeMove = reMove
            int remove = reMove != Move.NullMove ? (movingPieceValue * 2 + pieceValues[(int)board.GetPiece(reMove.TargetSquare).PieceType]) : 0;
            int beProtected = (int)((BeProtected(board, move, allMoves) != Move.NullMove) ? (movingPieceValue * 1.5) : 0);
            int escape = (!board.SquareIsAttackedByOpponent(capturedPiece.Square) || SquareIsProtected(board, move) && movingPieceValue < 500) ? (movingPieceValue + capturedPieceValue + (SquareIsProtected(board, move) ? (movingPieceValue / 2) : 0)) : 0;
            int threatenedMovesValuesCompare = Math.Max(Math.Max(escape, beProtected), remove);

            if (board.SquareIsAttackedByOpponent(movingPiece.Square) && threatenedMovesValuesCompare > highestValueThreatened)
            {
                if (threatenedMovesValuesCompare == beProtected)
                {
                    threatenedMove = BeProtected(board, move, allMoves);
                }
                else if (threatenedMovesValuesCompare == escape)
                {
                    threatenedMove = move;
                }
                else if (threatenedMovesValuesCompare == remove)
                {
                    threatenedMove = reMove;
                }


                highestValueThreatened = threatenedMovesValuesCompare;
            }

            //Check if the piece is threatened, and if capturedPieceValue is greater than the current highest value and the moving piece's value
            //is less than the capturedPiece or  if nothing is attacking the target square, then, set capturedPieceValue as the new highest and
            //set the captureMove as the current move.
            if (capturedPieceValue > highestValueCapture)
            {
                if (capturedPieceValue >= movingPieceValue || !board.SquareIsAttackedByOpponent(capturedPiece.Square))
                {
                    captureMove = move;
                    highestValueCapture = capturedPieceValue;
                }
            }

            #endregion

            #region Values Comparison

            //Determine which move to play, by comparing their values
            moveToPlay = (highestValueCapture >= highestValueThreatened) ? captureMove : threatenedMove;

            #region No Choice

            if (movingPieceValue != 10000)
            {
                int highestDepth2Value = 0;

                if (highestValueCapture == 0 && highestValueThreatened == 0)
                {
                    board.MakeMove(move);

                    int depth2Value = bestCaptureDepth2(board);
                    if (depth2Value > highestDepth2Value)
                    {
                        moveToPlay = move;
                        highestDepth2Value = depth2Value;
                    }

                    board.UndoMove(move);
                }
            }

            #endregion
        }

        //Return the bestMove from the Calculations
        return moveToPlay;
    }

    int bestCaptureDepth2(Board board)
    {
        #region Local Variable Declaration
        //Get all legal moves
        Move[] allMoves = board.GetLegalMoves();

        int depth2Value = 0;
        int highestValueCapture = 0, highestValueThreatened = 0;
        #endregion

        //Loop through all the legal moves
        foreach (Move move in allMoves)
        {
            #region Local Variable Declaration

            //Declaration and setting of some variables
            Piece capturedPiece = board.GetPiece(move.TargetSquare), movingPiece = board.GetPiece(move.StartSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType], movingPieceValue = pieceValues[(int)movingPiece.PieceType];

            #endregion

            #region Move Value Calculations

            //Always play a checkmate in one if detected
            if (MoveIsCheckmate(board, move) || (MoveIsCheck(board, move) && SquareIsProtected(board, move) && movingPieceValue < 900))
            {
                depth2Value = 10000;
                break;
            }

            //Check if the piece is threatened, and if
            //movingPieceValue + capturedPieceValue + (SquareIsProtected(board, move) ? (movingPieceValue / 2) : 0)
            //is greater than the current highest value, set it as the new highest and set the threatenedMove as the current move.
            Move reMove = RemoveThreat(board, move); //hehe removeMove = reMove
            int remove = reMove != Move.NullMove ? (movingPieceValue * 2 + pieceValues[(int)board.GetPiece(reMove.TargetSquare).PieceType]) : 0;
            int beProtected = (int)((BeProtected(board, move, allMoves) != Move.NullMove) ? (movingPieceValue * 1.5) : 0);
            int escape = (!board.SquareIsAttackedByOpponent(capturedPiece.Square) || SquareIsProtected(board, move) && movingPieceValue < 500) ? (movingPieceValue + capturedPieceValue + (SquareIsProtected(board, move) ? (movingPieceValue / 2) : 0)) : 0;
            int threatenedMovesValuesCompare = Math.Max(Math.Max(escape, beProtected), remove);

            if (board.SquareIsAttackedByOpponent(movingPiece.Square) && threatenedMovesValuesCompare > highestValueThreatened)
            {
                highestValueThreatened = threatenedMovesValuesCompare;
            }

            //Check if the piece is threatened, and if capturedPieceValue is greater than the current highest value and the moving piece's value
            //is less than the capturedPiece or  if nothing is attacking the target square, then, set capturedPieceValue as the new highest and
            //set the captureMove as the current move.
            if (capturedPieceValue > highestValueCapture)
            {
                if (capturedPieceValue >= movingPieceValue || !board.SquareIsAttackedByOpponent(capturedPiece.Square))
                {
                    highestValueCapture = capturedPieceValue;
                }
            }

            depth2Value = Math.Max(highestValueCapture, highestValueThreatened);

            #endregion

            #region No Choice

            if (movingPieceValue != 10000)
            {
                if (highestValueCapture == 0 && highestValueThreatened == 0)
                {
                    if (!board.SquareIsAttackedByOpponent(capturedPiece.Square) || SquareIsProtected(board, move))
                    {
                        depth2Value = 10;
                    }

                    board.MakeMove(move);


                    if (board.GetLegalMoves(true).Length > 0)
                    {
                        depth2Value = 15;
                    }

                    Move[] moves2 = board.GetLegalMoves();

                    foreach (Move m2 in moves2)
                    {
                        if (MoveIsCheckmate(board, m2) || MoveIsCheck(board, m2))
                        {
                            depth2Value = 20;
                        }
                    }

                    board.UndoMove(move);
                }
            }

            #endregion
        }

        //Return the bestMove from the Calculations
        return depth2Value;
    }
    #endregion

    #endregion
}