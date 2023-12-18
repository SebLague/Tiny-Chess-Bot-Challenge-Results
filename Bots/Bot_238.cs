namespace auto_Bot_238;
using ChessChallenge.API;
using System;

public class Bot_238 : IChessBot
{
    bool PlayingAsWhite;

    Move chosenMove;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        PlayingAsWhite = board.IsWhiteToMove;

        minimax(board, 4, int.MinValue, int.MaxValue, PlayingAsWhite);

        foreach (var move in moves)
        {
            if (MateInOne(board, move))
            {
                chosenMove = move;
                break;
            }
        }

        return chosenMove;
    }
    int eval(Board board)
    {
        int material = 0;

        for (int i = 0; i < 64; i++)
        {
            Piece piece = board.GetPiece(new Square(i));
            int colorMultiplier = (piece.IsWhite) ? 1 : -1;

            if (piece.PieceType == PieceType.Pawn)
                material += 10 * colorMultiplier;
            else if (piece.PieceType == PieceType.Knight || piece.PieceType == PieceType.Bishop)
                material += 30 * colorMultiplier;
            else if (piece.PieceType == PieceType.Rook)
                material += 50 * colorMultiplier;
            else if (piece.PieceType == PieceType.Queen)
                material += 90 * colorMultiplier;
            else if (piece.PieceType == PieceType.King)
                material += 1000 * colorMultiplier;

            if (piece.IsPawn)
            {
                if (piece.Square.Rank >= 3 && piece.Square.Rank <= 4 && piece.Square.File >= 3 && piece.Square.File <= 4)
                    material += 2 * colorMultiplier;
            }
            if (piece.IsKnight)
            {
                if (piece.Square.Rank >= 2 && piece.Square.Rank <= 5 && piece.Square.File >= 2 && piece.Square.File <= 5)
                    material += 2 * colorMultiplier;
            }
        }

        return material;
    }
    int minimax(Board board, int depth, int alpha, int beta, bool isMaxing)
    {
        if (depth == 0 || board.GetLegalMoves().Length == 0)
            return eval(board);
        if (isMaxing)
        {
            int bestEval = int.MinValue;
            Move bestMove = board.GetLegalMoves()[0];

            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int evaluation = minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);

                if (evaluation > bestEval)
                {
                    bestEval = evaluation;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, bestEval);
                if (beta <= alpha)
                    break;
            }
            chosenMove = bestMove;
            return bestEval;
        }
        else if (!isMaxing)
        {
            int bestEval = int.MaxValue;
            Move bestMove = board.GetLegalMoves()[0];

            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int evaluation = minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);

                if (evaluation < bestEval)
                {
                    bestEval = evaluation;
                    bestMove = move;
                }
                beta = Math.Min(beta, bestEval);
                if (beta <= alpha)
                    break;
            }
            chosenMove = bestMove;
            return bestEval;
        }
        return eval(board);
    }
    bool MateInOne(Board board, Move move)
    {
        board.MakeMove(move);
        bool IsMate = board.IsInCheckmate();
        board.UndoMove(move);
        return IsMate;
    }
}