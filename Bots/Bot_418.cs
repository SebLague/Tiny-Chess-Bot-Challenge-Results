namespace auto_Bot_418;
using ChessChallenge.API;
using System;


public class Bot_418 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();
        Random rand = new Random();
        Move bestMove = legalMoves[rand.Next(legalMoves.Length)];
        float bestMoveEval = 0;
        float[] pieceValues = { 0, 1, 3, 3.25f, 5, 9, 100 };
        float materialImbalance = getMaterialImbalance(board, board.GetAllPieceLists(), pieceValues);

        foreach (Move move in legalMoves)
        {
            float moveEval = 0;
            if (moveIsCheckmate(board, move))
            {
                return move;
            }
            if (moveIsStalemete(board, move))
            {
                if (board.IsWhiteToMove && materialImbalance < 0)
                {
                    moveEval += 10;
                }
                else if (!board.IsWhiteToMove && materialImbalance > 0)
                {
                    moveEval += 10;
                }
                else
                {
                    moveEval -= 10;
                }

            }
            if (move.IsCapture)
            {
                var CapturedPiece = move.CapturePieceType;
                moveEval += 5 + pieceValues[(int)CapturedPiece];
            }
            if (moveIsCheck(board, move))
            {
                moveEval += 1;
            }
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveEval -= 10;
            }

            switch (move.MovePieceType)
            {
                case PieceType.King:

                    if (move.IsCastles)
                    {
                        moveEval += 5 / board.PlyCount;
                    }
                    else if (board.PlyCount < 50 || board.IsInCheck())
                    {
                        moveEval -= 10;
                    }
                    else
                    {
                        moveEval -= 0.5f;
                    }
                    break;

                case PieceType.Queen:
                    if (board.PlyCount < 10)
                    {
                        moveEval -= 1;
                    }
                    break;

                case PieceType.Pawn:
                    if (move.IsPromotion)
                    {
                        moveEval += pieceValues[(int)move.PromotionPieceType];
                    }
                    else
                    {
                        moveEval += 0.015f * board.PlyCount;
                    }
                    if ((move.TargetSquare.File == 3 || move.TargetSquare.File == 4) && board.PlyCount < 8)
                    {
                        moveEval += 2;
                    }
                    if (Math.Abs(move.StartSquare.Rank - move.TargetSquare.Rank) == 2)
                    {
                        moveEval += 0.5f;
                    }
                    break;
            }

            if (moveEval > bestMoveEval)
            {
                bestMove = move;
                bestMoveEval = moveEval;
            }
        }
        return bestMove;
    }

    bool moveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
    bool moveIsStalemete(Board board, Move move)
    {
        board.MakeMove(move);
        bool isStalemate = board.IsDraw();
        board.UndoMove(move);
        return isStalemate;
    }
    bool moveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }
    float getMaterialImbalance(Board board, PieceList[] lists, float[] pieceValues)
    {
        float imbalance = 0;
        foreach (PieceList list in lists)
        {
            for (int i = 0; i < list.Count; i++)
            {

                float val = pieceValues[(int)list.GetPiece(i).PieceType];
                if (list.GetPiece(i).IsWhite)
                {
                    imbalance += val;
                }
                else
                {
                    imbalance -= val;
                }
            }
        }
        return imbalance;
    }
}