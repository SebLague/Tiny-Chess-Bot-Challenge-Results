namespace auto_Bot_548;
using ChessChallenge.API;
using System;
public class Bot_548 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    Move[] previousMoves = new Move[5];
    bool myColor;
    public Move Think(Board board, Timer timer)
    {
        myColor = board.IsWhiteToMove;
        Piece previousMovedPiece = board.GetPiece(previousMoves[4].TargetSquare);
        Move[] allMoves = board.GetLegalMoves();
        Random rnd = new();
        // Pick a random move to play if nothing better is found
        Move moveToPlay = allMoves[allMoves.Length - 1];
        int highestValueMove = -10000;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            // if (MoveIsCheckmate(board, move))
            // {
            //     moveToPlay = move;
            //     break;
            // }

            Piece currentPiece = board.GetPiece(move.StartSquare);

            int moveValue = 0;
            if (currentPiece.PieceType == previousMovedPiece.PieceType)
            {
                moveValue -= 150;
            }
            if (currentPiece.IsPawn)
            {
                moveValue += 110 + rnd.Next(-20, 20);
            }
            if (currentPiece.IsKing)
            {
                moveValue -= 50 + rnd.Next(-20, 20);
            }
            if (moveHasBeenPlayedBefore(board, move))
            {
                moveValue = -5000;
            }
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            moveValue += pieceValues[(int)capturedPiece.PieceType];
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {

                moveValue -= 2 * pieceValues[(int)currentPiece.PieceType];
            }
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                moveToPlay = move;
                break;
            }
            moveValue -= bestOppenentCapture(board, 2);

            if (board.IsInCheck())
            {
                moveValue += 1000;
            }

            board.UndoMove(move);
            if (moveValue > highestValueMove)
            {
                moveToPlay = move;
                highestValueMove = moveValue;
            }
        }
        updatePreviousMoveList(moveToPlay);
        return moveToPlay;

    }
    void updatePreviousMoveList(Move move)
    {
        for (int i = 0; i < 4; i++)
        {
            previousMoves[i] = previousMoves[i + 1];
        }
        previousMoves[4] = move;
    }
    int bestOppenentCapture(Board board, int depth)
    {


        Move[] allOppMoves;
        if (depth < 1 || (allOppMoves = board.GetLegalMoves()).Length == 0)
        {

            return TotalBoardValue(board);
        }
        int bestValueMove = 0;
        // Move bestOppMove =  allOppMoves[0];
        foreach (Move oppMove in allOppMoves)
        {
            int score = pieceValues[(int)board.GetPiece(oppMove.TargetSquare).PieceType];

            if (board.SquareIsAttackedByOpponent(oppMove.TargetSquare))
            {
                score -= pieceValues[(int)board.GetPiece(oppMove.StartSquare).PieceType];
                // moveValue -= pieceValues[(int)currentPiece.PieceType];
            }

            board.MakeMove(oppMove);
            if (board.IsInCheckmate())
            {
                score += 10000;
                depth = 0;
            }
            score -= bestOppenentCapture(board, depth - 1);

            if (board.IsInCheck())
            {
                score += 1000;
            }

            board.UndoMove(oppMove);
            if (score > bestValueMove)
            {
                bestValueMove = score;
                // bestOppMove = oppMove;
            }
        }

        return bestValueMove;


    }


    int TotalBoardValue(Board board)
    {
        int score = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        for (int i = 0; i < 6; i++)
        {
            score += pieceValues[i + 1] * (allPieces[i].Count - allPieces[6 + i].Count);
        }
        if (!board.IsWhiteToMove)
        {
            score = -score;
        }
        return score;
    }
    bool moveHasBeenPlayedBefore(Board board, Move move)
    {
        foreach (Move previousMove in previousMoves)
        {
            if (move == previousMove)
            {
                return true;
            }
        }
        return false;
    }
}
