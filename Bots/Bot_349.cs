namespace auto_Bot_349;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_349 : IChessBot
{
    Move botMove = new();
    string pastBoard = "";
    readonly List<string> pastBoards = new();
    bool eWillCheckmate = false;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private readonly int[] pieceValues = { 0, 100, 400, 400, 600, 1200, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move[] opponentMoves;
        bool isWhite = board.IsWhiteToMove;

        int index = board.GetFenString().Normalize().IndexOf(" ");

        int highestValueCapture = 0;
        int myHighestValuePiece = 10001;
        botMove = moves[new Random().Next(moves.Length)];

        if (timer.MillisecondsRemaining < 50)
        {
            Random random = new();
            return botMove = moves[random.Next(moves.Length)];
        }

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                botMove = move;
                break;
            }

            if (MoveIsSaveCheck(board, move) && !MoveIsCheckmate(board, move))
            {
                botMove = move;
                break;
            }

            Piece myPiece = board.GetPiece(move.StartSquare);
            int myPieceValue = pieceValues[(int)myPiece.PieceType];


            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            foreach (Move i in moves)
            {
                int enemyValue = 0;
                opponentMoves = GetEnemyMoves(board);
                foreach (Move j in opponentMoves)
                {
                    if (j.TargetSquare == i.StartSquare)
                    {
                        enemyValue = pieceValues[(int)board.GetPiece(i.StartSquare).PieceType];
                    }
                }

                if (board.SquareIsAttackedByOpponent(i.StartSquare) && !board.SquareIsAttackedByOpponent(i.TargetSquare) &&
                    pieceValues[(int)board.GetPiece(i.StartSquare).PieceType] >= 200 && (!isProtected(board, i.StartSquare) || enemyValue < pieceValues[(int)board.GetPiece(i.StartSquare).PieceType]))
                {
                    return botMove = i;
                }

            }

            if (capturedPieceValue > highestValueCapture && capturedPieceValue >= myPieceValue && myPieceValue < myHighestValuePiece)
            {
                highestValueCapture = capturedPieceValue;
                myHighestValuePiece = myPieceValue;
                return botMove = move;
            }

            board.MakeMove(move);
            if ((move.IsEnPassant && move.PromotionPieceType == PieceType.Queen) || move.IsCastles) { board.UndoMove(move); return botMove = move; }
            board.UndoMove(move);

        }

        if (highestValueCapture == 0 && board.SquareIsAttackedByOpponent(botMove.TargetSquare))
        {
            foreach (Move move in moves)
            {
                if (board.GetKingSquare(white: isWhite) != move.StartSquare)
                {
                    botMove = move;
                    break;
                }
                else if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    botMove = move;
                    break;
                }
            }
        }

        if (EnemyWillCheckmate(board, botMove))
        {
            foreach (Move move in moves)
            {
                eWillCheckmate = false;
                board.MakeMove(move);

                opponentMoves = GetEnemyMoves(board);
                foreach (Move i in opponentMoves)
                {
                    board.MakeMove(i);
                    if (board.IsInCheckmate())
                    {
                        board.UndoMove(i);
                        eWillCheckmate = true;
                        break;
                    }
                    board.UndoMove(i);
                }
                if (!eWillCheckmate)
                {
                    eWillCheckmate = false;
                    board.UndoMove(move);
                    return botMove = move;
                }

                board.UndoMove(move);

            }
        }

        if (pastBoards.Contains(board.GetFenString().Normalize().Remove(index + 1)))
        {
            foreach (Move move in moves)
            {
                if (!board.SquareIsAttackedByOpponent(move.TargetSquare) && !pastBoards.Contains(board.GetFenString().Normalize().Remove(index + 1)))
                {
                    return botMove = move;
                }
                else if (!pastBoards.Contains(board.GetFenString().Normalize().Remove(index + 1)))
                {
                    return botMove = move;
                }
                else
                {
                    return botMove = move;
                }
            }
        }
        pastBoard = board.GetFenString().Normalize().Remove(index + 1);
        if (!pastBoards.Contains(pastBoard))
        {
            pastBoards.Add(pastBoard);
        }

        // Final move
        return botMove;

        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }


        bool MoveIsSaveCheck(Board board, Move move)
        {
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                return false;
            }
            board.MakeMove(move);
            bool isSaveCheck = board.IsInCheck();
            board.UndoMove(move);
            return isSaveCheck;
        }


        bool EnemyWillCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            Move[] opponentMoves;

            opponentMoves = GetEnemyMoves(board);
            foreach (Move i in opponentMoves)
            {
                board.MakeMove(i);
                if (board.IsInCheckmate())
                {
                    board.UndoMove(i);
                    board.UndoMove(move);
                    return true;
                }
                board.UndoMove(i);
            }

            board.UndoMove(move);
            return false;

        }

        bool isProtected(Board board, Square square)
        {
            if (board.TrySkipTurn())
            {
                if (board.SquareIsAttackedByOpponent(square))
                {
                    board.UndoSkipTurn();
                    return true;
                }
                board.UndoSkipTurn();
                return false;
            }
            return false;
        }
    }

    public Move[] GetEnemyMoves(Board board)
    {
        board.MakeMove(Move.NullMove);
        Move[] enemyMoves = board.GetLegalMoves();
        board.UndoMove(Move.NullMove);
        return enemyMoves;
    }

}
