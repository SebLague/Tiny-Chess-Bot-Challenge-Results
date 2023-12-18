namespace auto_Bot_73;
using ChessChallenge.API;
using System;

public class Bot_73 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    Move futuremove = Move.NullMove;
    public Move Think(Board board, Timer timer)
    {
        Move[] mymoves = board.GetLegalMoves();//get all moves
        Random rng = new();//random
        Move moveToPlay = mymoves[rng.Next(mymoves.Length)];//get a random move
        int hyc = 0;//hyc
        int lcc = cancaptured(board, moveToPlay);
        Piece c = board.GetPiece(moveToPlay.TargetSquare);
        int ca = pieceValues[(int)c.PieceType];
        hyc = ca;
        if (futuremove == Move.NullMove)
        {
            foreach (Move move in mymoves)
            {
                //DivertedConsole.Write("" + move.ToString());
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    moveToPlay = move;
                    break;
                }
                board.UndoMove(move);
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
                int cc = cancaptured(board, move);
                if ((capturedPieceValue > hyc & cc <= capturedPieceValue) | lcc > cc)
                {
                    moveToPlay = move;
                    hyc = capturedPieceValue;
                    lcc = cc;
                }
                board.MakeMove(move);
                if (!MoveIsCheckmate(board))
                {
                    foreach (Move move2 in board.GetLegalMoves())
                    {
                        board.MakeMove(move2);
                        if (board.IsInCheckmate())
                        {
                            moveToPlay = move;
                            futuremove = move2;
                            break;
                        }
                        board.UndoMove(move2);
                    }

                    board.UndoMove(move);
                }
                else
                {
                    board.UndoMove(move);
                    continue;
                }
            }
        }
        else { moveToPlay = futuremove; futuremove = Move.NullMove; }
        return moveToPlay;
    }
    int cancaptured(Board board, Move move)
    {
        board.MakeMove(move);
        int hec = 0;
        foreach (Move move1 in board.GetLegalMoves())
        {
            Piece capturedPiece = board.GetPiece(move1.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            if (capturedPieceValue > hec)
            {
                hec = capturedPieceValue;
            }
        }
        board.UndoMove(move);
        return hec;
    }
    bool MoveIsCheckmate(Board board)
    {//make it loop and check for mates
        bool ismate = false;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            bool isMate1 = board.IsInCheckmate();
            board.UndoMove(move);
            if (isMate1) { ismate = true; }
        }
        return ismate;
    }
}