namespace auto_Bot_197;
using ChessChallenge.API;
using System;

public class Bot_197 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int bestMove = -1000000000;


        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];

        foreach (Move move in allMoves)
        {

            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
            board.MakeMove(move);
            int val = -Search(board, 3, -1000000000, 1000000000);
            if (val > bestMove)
            {
                moveToPlay = move;
                bestMove = val;
            }
            board.UndoMove(move);

        }
        return moveToPlay;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    int Evaluate(Board board)
    {
        PieceList[] Pieces = board.GetAllPieceLists();
        int whiteMaterial = 0;
        int blackMaterial = 0;

        foreach (PieceList piece in Pieces)
        {
            if (piece.IsWhitePieceList)
            {
                whiteMaterial += pieceValues[(int)piece.TypeOfPieceInList] * piece.Count;
            }
            else
            {
                blackMaterial += pieceValues[(int)piece.TypeOfPieceInList] * piece.Count;
            }

        }

        int material = (whiteMaterial - blackMaterial);
        int perspective = (board.IsWhiteToMove) ? 1 : -1;
        material = material * perspective;

        return material;
    }

    int Search(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            return Evaluate(board);
        }
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0)
        {
            if (board.IsInCheck())
            {
                return -1000000000;
            }
            return 0;
        }

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int evaluation = -Search(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);
            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);
        }
        return alpha;
    }
}