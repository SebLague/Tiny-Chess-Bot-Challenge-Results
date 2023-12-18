namespace auto_Bot_634;
using ChessChallenge.API;
using System;

public class Bot_634 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
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

            if (move.IsEnPassant)
            {
                moveToPlay = move;
                break;
            }

            if (move.IsPromotion)
            {
                bool used = false;
                foreach (Move proMove in allMoves)
                {

                    if (proMove.IsPromotion)
                    {
                        if (MoveIsCheckmate(board, proMove) || (MoveIsCheck(board, proMove)) && allMoves.Length < 80)
                        {
                            used = true;
                            moveToPlay = proMove;
                            break; //ITS A CHECKMATE BRO!
                        }
                    }
                }

                if (!used)
                {
                    foreach (Move queenMove in allMoves)
                    {
                        if (queenMove.IsPromotion && CheckIfQueen(board, queenMove))
                        {
                            moveToPlay = queenMove;
                        }
                    }
                }
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }

            float randomOffset = new Random().NextSingle() * 500 - 250f;
            if (move.IsCastles && highestValueCapture < 300 + (int)randomOffset)
            {
                moveToPlay = move;
            }
        }
        if (allMoves.Length < 80)
        {
            for (int i = 0; i < allMoves.Length; i++)
            {
                if (CheckIfMate(board, allMoves[i], 0, 0))
                {
                    moveToPlay = allMoves[i];
                }
            }
        }

        int depth = 8;
        for (int i = 0; i < depth; i++)
        {
            if (CheckIfLosesQueen(board, moveToPlay) && allMoves.Length < 80)
            {
                moveToPlay = allMoves[rng.Next(allMoves.Length)];
            }
        }

        return moveToPlay;
    }

    bool CheckIfLosesQueen(Board board, Move move)
    {
        board.MakeMove(move);
        bool isLose = board.GetLegalMoves(true).Length > 0;
        board.UndoMove(move);
        return isLose;
    }

    bool CheckIfQueen(Board board, Move move)
    {
        board.MakeMove(move);
        bool isQueen = move.PromotionPieceType == PieceType.Queen;
        board.UndoMove(move);
        return isQueen;
    }

    bool CheckIfMate(Board board, Move move, int depth, int max)
    {


        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        if (!isMate)
        {
            foreach (Move newMove in board.GetLegalMoves())
            {
                if (MoveIsCheckmate(board, move))
                {
                    isMate = true;
                    board.UndoMove(newMove);
                    return isMate;
                }
            }
        }
        return isMate;
    }

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move, bool undo = true)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}