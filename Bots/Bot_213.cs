namespace auto_Bot_213;
using ChessChallenge.API;
using System;
using System.Linq;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;


public class Bot_213 : IChessBot
{

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();

        allMoves = allMoves.OrderBy(_ => rng.Next()).ToList().ToArray();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;
        int highestValueRescue = 0;
        int quality = -10;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                quality = 11;
            }


            if (quality < 10 && savesCheckMate(board, move))
            {
                moveToPlay = move;
                quality = 10;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);

            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            Piece movedPiece = board.GetPiece(move.StartSquare);

            int movedPieceValue = pieceValues[(int)movedPiece.PieceType];

            if (quality < 9 && capturedPieceValue > highestValueCapture && goodCapture(board, move) && capturedPieceValue >= highestValueRescue)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
                quality = 9;
            }

            if (quality < 7 && desirableFutureMovePossible(board, move) && !board.SquareIsAttackedByOpponent(move.TargetSquare) && isDefended(board, move)) //prioritise moves that allow for captures or checkmates
            {
                moveToPlay = move;
                quality = 7;
            }

            if (quality < 8 && isRescue(board, move) && movedPieceValue > highestValueRescue)
            {
                moveToPlay = move;
                quality = 8;
                highestValueRescue = movedPieceValue;
            }

            if (quality < 6 && desirableFutureMovePossible(board, move) && (!board.SquareIsAttackedByOpponent(move.TargetSquare)))
            {
                moveToPlay = move;
                quality = 6;
            }

            if (quality < 5 && !board.GetPiece(move.StartSquare).IsKing && !board.SquareIsAttackedByOpponent(move.TargetSquare) && isDefended(board, move) && moveForward(board, move))
            {
                moveToPlay = move;
                quality = 5;
            }

            if (quality < 4 && !board.SquareIsAttackedByOpponent(move.TargetSquare) && moveForward(board, move) && !board.GetPiece(move.StartSquare).IsKing)
            {
                moveToPlay = move;
                quality = 4;
            }


            //if (false && quality < 3 && !board.SquareIsAttackedByOpponent(move.TargetSquare) && isCheck(board, move))
            //{
            //    moveToPlay = move;
            //    quality = 3;
            //}


        }

        //if (quality < 0)
        //{
        //    DivertedConsole.Write("That was a random move " + moveToPlay.TargetSquare.Name);
        //}
        //else
        //{
        //    DivertedConsole.Write("Justification for " + moveToPlay.TargetSquare.Name + " is " + quality);
        //    if(quality == 6)
        //    {
        //        DivertedConsole.Write(board.SquareIsAttackedByOpponent(moveToPlay.TargetSquare).ToString() + " " + isDefended(board, moveToPlay).ToString());
        //    }

        //}
        return moveToPlay;
    }




    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool desirableFutureMovePossible(Board board, Move candidate)
    {
        board.MakeMove(candidate);
        board.ForceSkipTurn();
        bool result = false;

        Move[] legalMoves = board.GetLegalMoves();

        int highestValueCapture = 0;


        foreach (Move move in legalMoves)
        {
            if (MoveIsCheckmate(board, move))
            {

                result = true;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);

            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture && goodCapture(board, move)) //should probably change this to value taking piece with pawn over winning exchange
            {


                result = true;

            }
        }


        board.UndoSkipTurn();
        board.UndoMove(candidate);
        return result;
    }

    bool isRescue(Board board, Move candidate)
    {
        board.ForceSkipTurn();

        bool defended = board.SquareIsAttackedByOpponent(candidate.StartSquare);

        board.UndoSkipTurn();

        bool underAttack = board.SquareIsAttackedByOpponent(candidate.StartSquare);

        board.MakeMove(candidate);
        board.ForceSkipTurn();


        bool isSaved = !board.SquareIsAttackedByOpponent(candidate.TargetSquare);

        board.UndoSkipTurn();
        board.UndoMove(candidate);


        return underAttack && isSaved && (!defended || isAttackedByLowerValuePiece(board, candidate.StartSquare));
    }



    bool isDefended(Board board, Move candidate)
    {
        board.MakeMove(candidate);
        bool defended = board.SquareIsAttackedByOpponent(candidate.TargetSquare);
        board.UndoMove(candidate);
        return defended;
    }

    bool goodCapture(Board board, Move candidate)
    {
        Piece capturedPiece = board.GetPiece(candidate.TargetSquare);
        Piece movingPiece = board.GetPiece(candidate.StartSquare);

        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
        int movingPieceValue = pieceValues[(int)movingPiece.PieceType];

        return movingPieceValue < capturedPieceValue || !board.SquareIsAttackedByOpponent(candidate.TargetSquare); //should probably change this to value taking piece with pawn over winning exchange


    }

    bool moveForward(Board board, Move move)
    {
        if (board.IsWhiteToMove)
        {
            return move.TargetSquare.Rank - move.StartSquare.Rank > 0;

        }
        else
        {
            return move.TargetSquare.Rank - move.StartSquare.Rank < 0;

        }
    }

    bool isAttackedByLowerValuePiece(Board board, Square square)

    {
        board.ForceSkipTurn();
        Move[] moves = board.GetLegalMoves();


        Piece capturedPiece = board.GetPiece(square);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

        foreach (Move possible in moves)
        {
            if (possible.TargetSquare.Name.Equals(square.Name) && possible.IsCapture)
            {


                Piece capturingPiece = board.GetPiece(possible.StartSquare);

                int capturingPieceValue = pieceValues[(int)capturingPiece.PieceType];

                if (capturedPieceValue > capturingPieceValue)
                {
                    board.UndoSkipTurn();
                    return true;
                }
            }

        }
        board.UndoSkipTurn();

        return false;

    }

    bool isCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool result = board.IsInCheck();
        board.UndoMove(move);
        return result;
    }

    bool hasCheckMate(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return true;
            }
        }
        return false;
    }

    bool savesCheckMate(Board board, Move move)
    {
        board.ForceSkipTurn();
        bool checkMateOnBoard = hasCheckMate(board);
        board.UndoSkipTurn();

        board.MakeMove(move);
        bool stillCM = hasCheckMate(board);
        board.UndoMove(move);

        return checkMateOnBoard && !stillCM;

    }
}