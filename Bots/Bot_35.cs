namespace auto_Bot_35;
using ChessChallenge.API;
using System;

public class Bot_35 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    public Move Think(Board board, Timer timer)
    {

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        Move suggestMove = Move.NullMove;
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {

            bool skipMove = false;
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }
            if (MoveIsDraw(board, move))
            {
                DivertedConsole.Write("Draw Possibility");
                continue;
            }
            else if (!MoveIsDraw(board, move))
            {
                board.MakeMove(move);
                Move[] tempMoves = board.GetLegalMoves();
                Move opMoveToPlay = tempMoves[rng.Next(tempMoves.Length)];

                foreach (Move tempMove in tempMoves)
                {
                    if (MoveIsCheckmate(board, tempMove))
                    {
                        skipMove = true;
                        DivertedConsole.Write("skip move mate");
                        break;
                    }
                    if (MoveIsDraw(board, tempMove))
                    {
                        DivertedConsole.Write("skip move draw");
                        skipMove = true;
                        break;
                    }
                }

                board.UndoMove(move);
                if (!skipMove)
                {
                    Piece capturedPiece = board.GetPiece(move.TargetSquare);
                    int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                    if (capturedPieceValue > highestValueCapture && !board.SquareIsAttackedByOpponent(capturedPiece.Square) && !skipMove)
                    {
                        highestValueCapture = capturedPieceValue;
                        moveToPlay = move;
                    }
                }
            }

        }


        return moveToPlay;


    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        if (isMate == true) { DivertedConsole.Write(move.ToString()); }
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }
}