namespace auto_Bot_1;
using ChessChallenge.API;
using System;

public class Bot_1 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int moveCount = 0;
    int randomExitNum = 0;

    public Move Think(Board board, Timer timer)
    {
        Random exitRng = new();
        randomExitNum = exitRng.Next(0, 20);
        DivertedConsole.Write(randomExitNum);

        if (moveCount < 6 && randomExitNum > 15)
        {
            DivertedConsole.Write("Moving normaly");
            MoveNormaly(board, timer);
        }
        else if (moveCount >= 6 && randomExitNum < 15)
        {
            while (randomExitNum <= 15)
            {
                DivertedConsole.Write("IT HAS GONE SCHIZOPHRENIC");
                Move[] nextMove = board.GetLegalMoves();
                Random rng = new();

                moveCount++;
                return nextMove[rng.Next(nextMove.Length)];
            }
        }

        Move[] emptyMove = board.GetLegalMoves();
        Random rng2 = new();
        return emptyMove[rng2.Next(emptyMove.Length)];
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    Move MoveNormaly(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        moveCount++;
        return moveToPlay;
    }
}