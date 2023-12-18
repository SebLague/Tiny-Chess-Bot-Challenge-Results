namespace auto_Bot_470;
using ChessChallenge.API;
using System;

public class Bot_470 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        if (allMoves.Length == 0)
        {
            return Move.NullMove;
        }

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            if (move == Move.NullMove)
            {
                continue;
            }

            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    int Minimax(Board board, int depth, bool isMaximizing)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board);
        }

        Move[] legalMoves = board.GetLegalMoves();
        int bestScore = isMaximizing ? int.MinValue : int.MaxValue;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            int score = Minimax(board, depth - 1, !isMaximizing);
            board.UndoMove(move);

            if (isMaximizing)
            {
                bestScore = Math.Max(bestScore, score);
            }
            else
            {
                bestScore = Math.Min(bestScore, score);
            }
        }

        return bestScore;
    }

    int EvaluateBoard(Board board)
    {
        int score = 0;

        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            if (pieceType == PieceType.None)
                continue;

            PieceList whitePieces = board.GetPieceList(pieceType, true);
            PieceList blackPieces = board.GetPieceList(pieceType, false);

            int pieceValue = pieceValues[(int)pieceType];
            score += (whitePieces.Count - blackPieces.Count) * pieceValue;
        }

        int whitePawnValue = pieceValues[(int)PieceType.Pawn];
        int blackPawnValue = -pieceValues[(int)PieceType.Pawn];

        foreach (Square square in Enum.GetValues(typeof(Square)))
        {
            Piece piece = board.GetPiece(square);

            if (piece.PieceType == PieceType.Pawn)
            {
                if (piece.IsWhite)
                {
                    score += whitePawnValue;

                    if (square.Rank == 3 || square.Rank == 4)
                        score += 10;
                }
                else
                {
                    score += blackPawnValue;

                    if (square.Rank == 3 || square.Rank == 4)
                        score -= 10;
                }
            }
        }

        int whiteMobility = board.GetLegalMoves(false).Length;
        int blackMobility = board.GetLegalMoves(true).Length;
        score += (whiteMobility - blackMobility);

        Square whiteKingSquare = board.GetKingSquare(true);
        Square blackKingSquare = board.GetKingSquare(false);
        if (whiteKingSquare.File != 4) score--;
        if (blackKingSquare.File != 4) score++;

        return score;
    }


    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}