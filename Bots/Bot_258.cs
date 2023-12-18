namespace auto_Bot_258;
using ChessChallenge.API;
using System;

public class Bot_258 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int highestValueEval = -1000000;
        Move bestMove = allMoves[0];

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int search = -Search(board, 4, -1000000, 1000000);
            if (search >= highestValueEval)
            {
                highestValueEval = search;
                bestMove = move;
            }
            board.UndoMove(move);
        }
        return bestMove;
    }

    int Search(Board board, int ply, int alpha, int beta)
    {
        if (ply == 0)
        {
            return Evaluate(board);
        }

        Move[] allMoves = board.GetLegalMoves();

        if (board.IsInCheckmate())
            return -1000000;
        if (board.IsInStalemate())
        {
            if (Evaluate(board) > -5)
            {
                return -1000000;
            }
        }

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int search = -Search(board, ply - 1, -beta, -alpha);
            board.UndoMove(move);
            if (search >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, search);

        }
        return alpha;
    }

    int Evaluate(Board board)
    {
        int evaluation = 0;
        evaluation += board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).Count;
        evaluation += board.GetPieceList(PieceType.Knight, board.IsWhiteToMove).Count * 3;
        evaluation += board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove).Count * 3;
        evaluation += board.GetPieceList(PieceType.Rook, board.IsWhiteToMove).Count * 5;
        evaluation += board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).Count * 9;

        evaluation -= board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove).Count;
        evaluation -= board.GetPieceList(PieceType.Knight, !board.IsWhiteToMove).Count * 3;
        evaluation -= board.GetPieceList(PieceType.Bishop, !board.IsWhiteToMove).Count * 3;
        evaluation -= board.GetPieceList(PieceType.Rook, !board.IsWhiteToMove).Count * 5;
        evaluation -= board.GetPieceList(PieceType.Queen, !board.IsWhiteToMove).Count * 9;

        Piece opponentKing = board.GetPieceList(PieceType.King, !board.IsWhiteToMove).GetPiece(0);
        int opponentKingRank = opponentKing.Square.Rank;
        int opponentKingFile = opponentKing.Square.File;
        evaluation += Math.Max(3 - opponentKingRank, opponentKingRank - 4) + Math.Max(3 - opponentKingFile, opponentKingFile - 4);

        //Piece selfKing = board.GetPieceList(PieceType.King, board.IsWhiteToMove).GetPiece(0);
        //int selfKingRank = selfKing.Square.Rank;
        //int selfKingFile = selfKing.Square.File;
        //evaluation += 14 - Math.Abs(opponentKingRank + selfKingRank) - Math.Abs(opponentKingFile + selfKingFile);

        return evaluation;
    }

}