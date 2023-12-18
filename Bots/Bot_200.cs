namespace auto_Bot_200;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_200 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int maxDepth = 10;

        bool isWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();

        Move chosenMove = moves[0];
        float chosenMoveVal = 0;

        foreach (Move move in moves)
        {
            List<Move> madeMoves = new List<Move>();
            float moveVal = 0;

            bool qSideCastleRight = board.HasQueensideCastleRight(isWhite);
            bool kSideCastleRight = board.HasKingsideCastleRight(isWhite);

            float startPosVal = PiecePosVal(move.MovePieceType, move.StartSquare, isWhite);
            moveVal += (PiecePosVal(move.IsPromotion ? move.PromotionPieceType : move.MovePieceType, move.TargetSquare, isWhite) - startPosVal) * 10;

            board.MakeMove(move);
            madeMoves.Add(move);
            if (board.IsInCheckmate()) return move;

            if (qSideCastleRight != board.HasQueensideCastleRight(isWhite)) moveVal -= 5000;
            if (kSideCastleRight != board.HasKingsideCastleRight(isWhite)) moveVal -= 5000;

            if (board.IsInCheck()) moveVal += 200;

            if (move.IsCapture) moveVal += 100 * (PieceValue(move.CapturePieceType) * PiecePosVal(move.CapturePieceType, move.TargetSquare, !isWhite));

            if (move.IsPromotion) moveVal += 40 * PieceValue(move.PromotionPieceType);

            if (board.SquareIsAttackedByOpponent(move.StartSquare)) moveVal += 60 + PieceValue(move.MovePieceType);

            if (move.IsCastles) moveVal += 500;

            for (int i = 0; i < maxDepth; i++)
            {
                if (board.IsDraw()) break;
                int availableMoves;
                float moveValue;
                madeMoves.Add(BestMove(board, out availableMoves, out moveValue));
                board.MakeMove(madeMoves[madeMoves.Count - 1]); //Opponent

                if (board.IsInCheckmate())
                {
                    moveVal -= 5000 / (i + 1);
                    break;
                }
                if (availableMoves == 1) moveVal += 50;
                moveVal -= moveValue * (maxDepth / (i + 1) / maxDepth);

                if (board.IsDraw())
                {
                    moveVal /= 3;
                    break;
                }
                madeMoves.Add(BestMove(board, out availableMoves, out moveValue));
                board.MakeMove(madeMoves[madeMoves.Count - 1]); //Bot

                if (board.IsInCheckmate())
                {
                    moveVal += 5000 / (i + 1);
                    break;
                }
                if (availableMoves == 1) moveVal -= 50;
                moveVal += moveValue * (maxDepth / (i + 1) / maxDepth);
                if (board.IsDraw())
                {
                    moveVal /= 3;
                    break;
                }
            }

            if (moveVal > chosenMoveVal)
            {
                chosenMoveVal = moveVal;
                chosenMove = move;
            }
            if (moveVal == chosenMoveVal)
            {
                Random rnd = new();
                if (rnd.Next(2) == 1)
                {
                    chosenMove = move;
                    chosenMoveVal = moveVal;
                }
            }

            madeMoves.Reverse();
            foreach (Move madeMove in madeMoves)
            {
                board.UndoMove(madeMove);
            }
        }

        return chosenMove;

        int PieceValue(PieceType piece)
        {
            int[] pieceValues = new int[6] { 30, 65, 70, 130, 250, 1000 }; //pawn,bishop,knight,rook,queen,king
            return pieceValues[(int)piece - 1];
        }

        float PiecePosVal(PieceType type, Square square, bool isWhite)
        {
            float val = 1;
            if (type == PieceType.Pawn)
            {
                val *= isWhite ? (square.File < 3 || square.File > 4 ? 2 / square.Rank : square.Rank) : (square.File < 3 || square.File > 4 ? square.Rank : 2 / square.Rank);
            }
            else if (type == PieceType.King)
            {
                val *= isWhite ? (square.Rank == 0 ? 8 : 7 / square.Rank) + ((square.File + 3.5f) * (square.File + 3.5f)) : square.Rank + (square.File + 3.5f) * (square.File + 3.5f);
            }

            return val;
        }

        Move BestMove(Board board, out int availableMoves, out float moveValue)
        {
            bool isWhite = board.IsWhiteToMove;

            Move[] moves = board.GetLegalMoves();
            availableMoves = moves.Length;

            Move chosenMove = moves[0];
            float chosenMoveVal = 0;
            foreach (Move move in moves)
            {
                float moveVal = 0;

                bool qSideCastleRight = board.HasQueensideCastleRight(isWhite);
                bool kSideCastleRight = board.HasKingsideCastleRight(isWhite);

                float startPosVal = PiecePosVal(move.MovePieceType, move.StartSquare, isWhite);
                moveVal += (PiecePosVal(move.IsPromotion ? move.PromotionPieceType : move.MovePieceType, move.TargetSquare, isWhite) - startPosVal) * 10;

                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    moveValue = 5000;
                    board.UndoMove(move);
                    return move;
                }
                if (board.IsInCheck()) moveVal += 200;

                if (qSideCastleRight != board.HasQueensideCastleRight(isWhite)) moveVal -= 5000;
                if (kSideCastleRight != board.HasKingsideCastleRight(isWhite)) moveVal -= 5000;
                board.UndoMove(move);

                if (move.IsCapture) moveVal += 100 * (PieceValue(move.CapturePieceType) * PiecePosVal(move.CapturePieceType, move.TargetSquare, !isWhite));

                if (move.IsPromotion) moveVal += 40 * PieceValue(move.PromotionPieceType);

                if (board.SquareIsAttackedByOpponent(move.StartSquare)) moveVal += 60 + PieceValue(move.MovePieceType);
                if (board.SquareIsAttackedByOpponent(move.TargetSquare)) moveVal -= 30 + PieceValue(move.MovePieceType);

                if (move.IsCastles) moveVal += 1000;

                if (moveVal > chosenMoveVal)
                {
                    chosenMove = move;
                    chosenMoveVal = moveVal;
                }
                if (moveVal == chosenMoveVal)
                {
                    Random rnd = new();
                    if (rnd.Next(2) == 1)
                    {
                        chosenMove = move;
                        chosenMoveVal = moveVal;
                    }
                }
            }

            moveValue = chosenMoveVal;
            return chosenMove;
        }
    }
}