namespace auto_Bot_398;
// winner geen illegale moves


using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_398 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0)
        {
            return Move.NullMove; // Geen legale zetten beschikbaar
        }

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            if (move.IsCapture)
            {
                int capturedPieceValue = pieceValues[(int)move.CapturePieceType];
                if (capturedPieceValue > highestValueCapture)
                {
                    moveToPlay = move;
                    highestValueCapture = capturedPieceValue;
                }
            }
        }

        if (IsBlunder(board, moveToPlay))
        {
            Move safeMove = allMoves.FirstOrDefault(m => !IsBlunder(board, m));
            if (safeMove != null && safeMove != Move.NullMove)
            {
                moveToPlay = safeMove;
            }
        }

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool IsBlunder(Board board, Move move)
    {
        board.MakeMove(move);
        bool isBlunder = false;

        if (board.IsInCheck())
        {
            isBlunder = true;
        }
        else
        {
            foreach (Move potentialEnemyMove in board.GetLegalMoves())
            {
                if (potentialEnemyMove.IsCapture)
                {
                    PieceType capturedPieceType = potentialEnemyMove.CapturePieceType;
                    if (capturedPieceType == PieceType.Queen || capturedPieceType == PieceType.Rook)
                    {
                        isBlunder = true;
                        break;
                    }
                }
            }
        }

        board.UndoMove(move);
        return isBlunder;
    }
}