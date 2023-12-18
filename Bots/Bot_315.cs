namespace auto_Bot_315;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_315 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int currentMove = 0;
    bool isWhite;


    public Move Think(Board board, Timer timer)
    {
        currentMove++;
        //DivertedConsole.Write(currentMove);
        Move[] allMoves = board.GetLegalMoves();
        Move[] captureMoves = board.GetLegalMoves(true);


        isWhite = board.IsWhiteToMove;

        //string.Equals(board.GameStartFenString, board.GetFenString())

        /*if (currentMove == 1) {
            if (isWhite) {
                return new Move("e2e3", board);
            } else {
                return new Move("e7e6", board);
            }
        }*/

        // Pick a random move to play if nothing better is found

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;
        int counter = 0;


        while (board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare) /*|| isGonnaDraw(board, moveToPlay)*/)
        {
            rng = new();
            moveToPlay = allMoves[rng.Next(allMoves.Length)];

            if (moveToPlay.IsPromotion)
            {
                moveToPlay = new Move(moveToPlay.StartSquare.Name + moveToPlay.TargetSquare.Name + "q", board);
            }
            if (board.IsInCheck() /*|| isGonnaDraw(board, moveToPlay)*/)
            {
                counter++;
            }
            if (counter == 10000)
            {
                break;
            }
        }

        /*
        foreach (Move move in allMoves) {
            if (MoveIsCheck(board, move) && !board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare)) {
                moveToPlay = move;
            }
        }
        */

        //int left = board.GetAllPieceLists().Count();
        int usLeft = board.GetPieceList(PieceType.Pawn, isWhite).Count() + board.GetPieceList(PieceType.Knight, isWhite).Count()
                + board.GetPieceList(PieceType.Bishop, isWhite).Count() + board.GetPieceList(PieceType.Rook, isWhite).Count()
                + board.GetPieceList(PieceType.Queen, isWhite).Count() + board.GetPieceList(PieceType.King, isWhite).Count();
        isWhite = !isWhite;
        int oppLeft = board.GetPieceList(PieceType.Pawn, isWhite).Count() + board.GetPieceList(PieceType.Knight, isWhite).Count()
                + board.GetPieceList(PieceType.Bishop, isWhite).Count() + board.GetPieceList(PieceType.Rook, isWhite).Count()
                + board.GetPieceList(PieceType.Queen, isWhite).Count() + board.GetPieceList(PieceType.King, isWhite).Count();
        isWhite = !isWhite;

        int think = 2;

        //if (left < 9) think = 4;
        //if (left < 6) think = 6;
        if (usLeft < 4 && oppLeft > 3) think = 0;
        if (usLeft < 2) think = 0;
        if (oppLeft == 1) think = 3;


        counter = 0;
        while (moveToPlay.MovePieceType == PieceType.King && usLeft > 12 && counter < 10000 && board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare))
        {
            counter++;
            rng = new();
            moveToPlay = allMoves[rng.Next(allMoves.Length)];
        }

        Move moveToTry = moveToPlay;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }



            if (deepThink(board, move, think))
            {
                moveToTry = move;
            }

            /*if (board.GetAllPieceLists().Count() < 100) {
                if (MoveIsCheck(board, move))
                {
                    board.MakeMove(move);
                    Move[] mateMoves = board.GetLegalMoves();
                    foreach (Move attempt in mateMoves) {
                        if (MoveIsCheckmate(board, attempt))
                        {
                            board.UndoMove(move);
                            moveToPlay = attempt;
                            break;
                        }
                    }
                    board.UndoMove(move);
                }
            }*/

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            int movingPieceValue = pieceValues[(int)move.MovePieceType];

            if (oppLeft < usLeft)
            {
                if (capturedPieceValue > highestValueCapture && capturedPieceValue >= movingPieceValue)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }
            else
            {
                if (capturedPieceValue > highestValueCapture && capturedPieceValue > movingPieceValue)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }
        }

        highestValueCapture = 0;
        foreach (Move move in captureMoves)
        {
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (!board.SquareIsAttackedByOpponent(move.TargetSquare) && capturedPieceValue > highestValueCapture)
            {
                highestValueCapture = capturedPieceValue;
                moveToPlay = move;
            }
        }



        if (!moveToPlay.IsCapture)
        {
            moveToPlay = moveToTry;
            moveToPlay = pushPawn(board, oppLeft, isWhite, moveToPlay, allMoves);
            if (usLeft < 30) moveToPlay = pieceRun(board, moveToPlay, allMoves, isWhite);

        }
        else
        {
            //DivertedConsole.Write("Capturing");
        }
        if (moveToPlay.IsPromotion)
        {
            moveToPlay = new Move(moveToPlay.StartSquare.Name + moveToPlay.TargetSquare.Name + "q", board);
        }




        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool deepThink(Board board, Move move, int numberPieces)
    {
        //DivertedConsole.Write(numberPieces);

        bool foundMate = false;
        if (numberPieces == 0)
        {
            return false;
        }

        board.MakeMove(move);
        foundMate = board.IsInCheckmate();

        if (foundMate)
        {
            board.UndoMove(move);
            return true;
        }
        else if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            board.UndoMove(move);
            return false;
        }
        else
        {
            Move[] possibleMoves = board.GetLegalMoves();

            foreach (Move make in possibleMoves)
            {
                if (deepThink(board, make, numberPieces - 1))
                {
                    board.UndoMove(move);
                    return true;
                }
            }
        }

        board.UndoMove(move);
        return false;

    }

    Move pushPawn(Board board, int oppLeft, bool isWhite, Move moveToPlay, Move[] legals)
    {
        if (oppLeft < 3)
        {
            foreach (Move move in legals)
            {
                if (move.MovePieceType == PieceType.Pawn)
                {
                    //DivertedConsole.Write("Pushing");
                    board.MakeMove(move);
                    if (!board.SquareIsAttackedByOpponent(move.StartSquare))
                    {
                        board.UndoMove(move);
                        return move;
                    }
                    board.UndoMove(move);
                }
            }

        }
        return moveToPlay;
    }

    Move pieceRun(Board board, Move moveToPlay, Move[] legals, bool isWhite)
    {
        Random rng = new();
        Move newMove;
        List<Piece> list = board.GetPieceList(PieceType.Pawn, isWhite).Concat(board.GetPieceList(PieceType.Knight, isWhite)).Concat(
                    board.GetPieceList(PieceType.Bishop, isWhite)).Concat(board.GetPieceList(PieceType.Rook, isWhite)).Concat(
                     board.GetPieceList(PieceType.Queen, isWhite)).Concat(board.GetPieceList(PieceType.King, isWhite)).ToList();

        int count = 0;
        foreach (Piece piece in list)
        {

            while (board.SquareIsAttackedByOpponent(piece.Square) && count < 10000 && piece.PieceType != PieceType.Pawn)
            {
                newMove = legals[rng.Next(legals.Length)];
                if (!board.SquareIsAttackedByOpponent(newMove.TargetSquare) && newMove.MovePieceType == piece.PieceType)
                {
                    return newMove;
                }
                count++;
            }
        }
        return moveToPlay;
    }
}