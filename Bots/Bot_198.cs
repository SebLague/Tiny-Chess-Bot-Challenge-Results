namespace auto_Bot_198;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_198 : IChessBot
{
    int[] pieceValues = { 0, 10, 30, 30, 50, 90, 900 };
    bool playingAsWhite;
    bool inEndGame = false;

    Move chosenMove = new Move();

    int totalMovesChecked = 0;

    public Move Think(Board board, Timer timer)
    {
        playingAsWhite = (board.IsWhiteToMove) ? true : false;
        chosenMove = board.GetLegalMoves()[0];

        int piecesL = 0;
        for (int i = 0; i < 64; i++)
        {
            if (board.GetPiece(new Square(i)).PieceType != PieceType.None) piecesL++;
        }

        inEndGame = (BitOperations.PopCount(board.AllPiecesBitboard) < 15) ? true : false;

        MiniMax(board, 3, int.MinValue, int.MaxValue, playingAsWhite);
        DivertedConsole.Write("Total Moves Checked: " + totalMovesChecked);
        totalMovesChecked = 0;
        return chosenMove;
    }

    int Evaluate(Board board)
    {
        // Keep in mind white wants to maximize the eval and black wants to minimize it
        // White pieces add to eval and black pieces subtract from it
        int material = 0;

        for (int i = 0; i < 64; i++)
        {
            Piece piece = board.GetPiece(new Square(i));
            int pcolorMultiplier = (piece.IsWhite) ? 1 : -1;
            int value = pieceValues[(int)board.GetPiece(new Square(i)).PieceType];

            material += value * pcolorMultiplier;

            if (piece.IsKnight)
            {
                if (piece.Square.Rank >= 2 && piece.Square.Rank <= 5 && piece.Square.File >= 2 && piece.Square.File <= 5)
                    material += 1 * pcolorMultiplier;
            }

            if (piece.IsPawn)
            {
                if (inEndGame)
                {
                    if (piece.IsWhite) material += piece.Square.Rank - 3;
                    if (!piece.IsWhite) material += -(piece.Square.Rank + 3);
                }
                else if (piece.Square.Rank >= 2 && piece.Square.Rank <= 5 && piece.Square.File >= 2 && piece.Square.File <= 5)
                {
                    material += 1 * pcolorMultiplier;
                    if (piece.Square.Rank >= 3 && piece.Square.Rank <= 4 && piece.Square.File >= 3 && piece.Square.File <= 4)
                        material += 1 * pcolorMultiplier;
                }


            }

            if (piece.IsKing)
            {
                if (piece.IsWhite && piece.Square.Rank > 1) material += (inEndGame) ? 1 : -2;
                if (!piece.IsWhite && piece.Square.Rank < 6) material += (inEndGame) ? -1 : 2;

                if (inEndGame) material += -20 * pcolorMultiplier;
            }

            if (piece.IsRook)
            {
                if (piece.IsWhite && piece.Square.Rank == 0)
                {
                    if (piece.Square.File <= 1 || piece.Square.File >= 6) material -= 2;
                }
                if (!piece.IsWhite && piece.Square.Rank == 7)
                {
                    if (piece.Square.File <= 1 || piece.Square.File >= 6) material += 2;
                }

                if (piece.IsWhite && piece.Square.Rank >= 6) material += 2;
                if (!piece.IsWhite && piece.Square.Rank <= 1) material -= 2;
            }
        }

        return material;
    }

    int MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.GetLegalMoves().Length <= 0)
        {
            return Evaluate(board);
        }

        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            Move bestMove = new Move();

            foreach (Move move in board.GetLegalMoves())
            {
                totalMovesChecked++;


                board.MakeMove(move);
                int eval = MiniMax(board, depth - 1, alpha, beta, false);
                if (move.IsCastles) eval += 1;
                if (move.IsPromotion) eval += 5;
                if (board.IsDraw()) eval += (playingAsWhite) ? -1000 : 1000;
                if (board.IsInCheckmate()) eval += (playingAsWhite) ? -10000 : 10000;
                board.UndoMove(move);

                if (maxEval < eval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, eval);

                if (beta <= alpha)
                    break;
            }

            chosenMove = bestMove;
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            Move bestMove = new Move();

            foreach (Move move in board.GetLegalMoves())
            {
                totalMovesChecked++;

                board.MakeMove(move);
                int eval = MiniMax(board, depth - 1, alpha, beta, true);
                if (move.IsCastles) eval -= 1;
                if (move.IsPromotion) eval -= 5;
                if (board.IsDraw()) eval += (!playingAsWhite) ? -1000 : 1000;
                if (board.IsInCheckmate()) eval += (!playingAsWhite) ? -10000 : 10000;
                board.UndoMove(move);

                if (minEval > eval)
                {
                    minEval = eval;
                    bestMove = move;
                }

                beta = Math.Min(beta, eval);

                if (beta <= alpha)
                    break;
            }

            chosenMove = bestMove;
            return minEval;
        }
    }
}