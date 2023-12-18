namespace auto_Bot_577;
using ChessChallenge.API;
using System;

public class Bot_577 : IChessBot
{
    int gameState = 0;

    int[] pieceValues = { 0, 100, 310, 320, 500, 900, 0 };

    int[] posIndexes = new int[] { 0, 1, 2, 3, 3, 2, 1, 0 };

    int[,] positionalValue = new int[,] {
        {-50, -40,-30,  -30},
        {-40, -20,  0,   0} ,
        {-30,  0,  10 ,  15 } ,
        {-30,  5,  15,   20} };


    public Move Think(Board board, Timer timer)
    {
        int depth;
        if (timer.MillisecondsRemaining > 100000)
            depth = 5;
        else if (timer.MillisecondsRemaining > 30000)
            depth = 4;
        else
            depth = 3;
        if (gameState <= 0)
            depth = 3;
        return GetBestMove(board, depth, board.IsWhiteToMove);
    }

    Move GetBestMove(Board board, int depth, bool isWhite)
    {
        var moves = board.GetLegalMoves();
        Move bestMove = moves[0];
        int materialBalance = GetRawMaterialBalance(board);

        int m = 1;
        if (isWhite)
            m = -1;

        int bestValue = 100000 * m;

        foreach (var move in moves)
        {
            int additional = 0;
            if (move.IsCastles)
                additional += 25;
            if (move.MovePieceType == PieceType.Rook && gameState <= 1)
                additional -= 40;

            int materialChange = GetMaterialChange(board, move);
            board.MakeMove(move);
            int value = Minimax(board, depth - 1, materialBalance + materialChange, !isWhite);
            if (isWhite && value > bestValue || !isWhite && value < bestValue)
            {
                bestMove = move;
                bestValue = value - m * additional;
            }
            board.UndoMove(move);
        }

        return bestMove;
    }

    int GetMaterialChange(Board board, Move move)
    {
        if (move.IsCapture)
        {
            Piece piece = board.GetPiece(move.TargetSquare);

            if (piece.IsWhite)
                return -pieceValues[(int)piece.PieceType];
            else
                return pieceValues[(int)piece.PieceType];
        }
        return 0;
    }

    int Minimax(Board board, int depth, int materialBalance, bool maximize = true, int alpha = int.MinValue, int beta = int.MaxValue)
    {
        var legalMoves = board.GetLegalMoves();

        if (depth <= 0 || legalMoves.Length == 0)
            return EvaluatePosition(board, materialBalance);

        int bestValue = maximize ? int.MinValue : int.MaxValue;

        foreach (Move move in legalMoves)
        {
            int materialChange = GetMaterialChange(board, move);
            board.MakeMove(move);
            float evaluation = Minimax(board, depth - 1, materialBalance + materialChange, !maximize, alpha, beta);
            if (maximize)
            {
                bestValue = (int)Math.Max(bestValue, evaluation);
                alpha = (int)Math.Max(alpha, evaluation);
            }
            else
            {
                bestValue = (int)Math.Min(bestValue, evaluation);
                beta = (int)Math.Min(beta, evaluation);
            }

            board.UndoMove(move);

            if (beta <= alpha)
                break;
        }
        return bestValue;
    }

    int EvaluatePosition(Board board, int materialBalance)
    {
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                return -10000;
            return 10000;
        }
        if (board.IsDraw())
            return 0;

        for (int Rank = 0; Rank < 8; Rank++)
        {
            for (int File = 0; File < 8; File++)
            {
                var piece = board.GetPiece(new Square(File, Rank));

                int m = -1;
                if (piece.IsWhite)
                    m = 1;

                if (gameState >= 2 && piece.IsPawn)
                {
                    if (piece.IsWhite)
                        materialBalance += 4 * (Rank + 1) * (Rank + 1);
                    else
                        materialBalance -= 4 * (7 - Rank) * (7 - Rank);
                }
                else
                {
                    materialBalance += m * positionalValue[posIndexes[File], posIndexes[Rank]];
                }
            }
        }

        return materialBalance;
    }

    int GetRawMaterialBalance(Board board)
    {
        int whiteMaterial = 0;
        int blackMaterial = 0;

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Piece piece = board.GetPiece(new Square(i, j));
                if (piece.IsWhite)
                    whiteMaterial += pieceValues[(int)piece.PieceType];
                else
                    blackMaterial += pieceValues[(int)piece.PieceType];
            }
        }
        if (whiteMaterial + blackMaterial <= 4000 || board.PlyCount >= 20)
            gameState = 2;
        else if (whiteMaterial + blackMaterial <= 7200 || board.PlyCount >= 8)
            gameState = 1;
        else
            gameState = 0;

        return whiteMaterial - blackMaterial;
    }
}