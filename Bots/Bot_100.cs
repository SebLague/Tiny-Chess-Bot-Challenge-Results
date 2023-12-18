namespace auto_Bot_100;
using ChessChallenge.API;
using System;

public class Bot_100 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int[] materialValues = { 0, 100, 250, 500, 500, 10000, 30000 };

    public Move Think(Board board, Timer timer)
    {
        var isWhite = board.IsWhiteToMove;
        Move[] allMoves = board.GetLegalMoves();

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);

            if (isMate)
            {
                moveToPlay = move;
                break;
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType] + GetScore(move, board, 3, isWhite); ;

            if (capturedPieceValue <= highestValueCapture) continue;
            moveToPlay = move;
            highestValueCapture = capturedPieceValue;
        }

        return moveToPlay;
    }

    private int GetScore(Move move, Board board, int depth, bool isWhite)
    {
        if (depth <= 0) return CalculateMaterial(move, board, isWhite);

        int highestScore = 0;

        board.MakeMove(move);
        Move[] allMovesRec = board.GetLegalMoves();
        foreach (Move moveRec in allMovesRec)
        {
            highestScore = GetScore(moveRec, board, depth - 1, isWhite) / depth;
        }
        board.UndoMove(move);

        return CalculateMaterial(move, board, isWhite) + highestScore;
    }

    private int CalculateMaterial(Move move, Board board, bool isWhite)
    {
        int material = 0;
        int to_multply = 0;
        var pieceList = board.GetAllPieceLists();
        for (int i = 0; i < pieceList.Length; i++)
        {
            Piece p = pieceList[i].GetPiece(0);

            if (isWhite == p.IsWhite)
            {
                to_multply = 100;
            }
            else
            {
                to_multply = -100;
            }
            material += materialValues[(int)p.PieceType] * to_multply;
        }

        return material;
    }
}