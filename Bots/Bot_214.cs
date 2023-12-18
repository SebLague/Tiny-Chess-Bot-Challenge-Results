namespace auto_Bot_214;
using ChessChallenge.API;
using System;


public class Bot_214 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 10, 30, 35, 50, 90, 100 };
    int maxDepth = 5;
    int highestValue;
    ulong[] pieceSquareValues = {
            0b0000000011111111100110011111111111111111111111111111111111111111,
            0b0000000000111100011111100111111001111110011111100011110000000000,
            0b0000000001111110011111100111111001111110011111100111111000000000,
            0b1111111111111111011111100111111001111110011111100111111011111111,
            0b0000000001111110011111101111111001111110011111100111111000000000,
            0b1111111111111111000000000000000000000000000000000000000000000000
         };

    public Move Think(Board board, Timer timer)
    {
        highestValue = (board.IsWhiteToMove) ? int.MinValue : int.MaxValue;
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = allMoves[0];
        int time_milli = timer.MillisecondsElapsedThisTurn;
        bool compareValues;

        // Apply Iterative Deepening with Alpha-Beta Pruning
        foreach (Move move in allMoves)
        {
            int resultValue = theAlgorithm(board, move, maxDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove);
            if (board.IsWhiteToMove)
            {
                compareValues = resultValue > highestValue;
            }
            else
            {
                compareValues = resultValue < highestValue;
            }

            if (compareValues)
            {
                bestMove = move;
                highestValue = resultValue;
            }
            DivertedConsole.Write(move.ToString() + resultValue);
            if (timer.MillisecondsElapsedThisTurn - time_milli >= 4000)
            {
                return bestMove;
            }
        }
        // DivertedConsole.Write("Best " + bestMove.ToString() + highestValue);
        return bestMove;
    }

    public int theAlgorithm(Board board, Move move, int depth, int alpha, int beta, bool iswhite)
    {
        if (depth == 0)
        {
            return EvaluateBoard(board, iswhite);
        }
        int eval = 0;
        int MinEval = int.MaxValue;
        int MaxEval = int.MinValue;
        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0)
        {
            board.UndoMove(move);
            return EvaluateBoard(board, iswhite);
        }
        foreach (Move rmove in allMoves)
        {
            eval = theAlgorithm(board, rmove, depth - 1, alpha, beta, !iswhite);
            if (iswhite)
            {
                MaxEval = Math.Max(MaxEval, eval);
                alpha = Math.Max(alpha, eval);
            }
            else
            {
                MinEval = Math.Min(MinEval, eval);
                beta = Math.Min(beta, eval);
            }
            if (beta <= alpha) { break; }

        }
        board.UndoMove(move);
        return (iswhite) ? MaxEval : MinEval;

    }

    public int EvaluateBoard(Board board, bool iswhite)
    {
        int score = 0;
        if (board.IsInCheckmate())
        {
            return iswhite ? int.MaxValue : int.MinValue;
        }

        if (board.IsInCheck())
        {
            score += (iswhite) ? 20 : -20;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        if (board.HasKingsideCastleRight(true) || board.HasQueensideCastleRight(true))
        {
            score += 20;
        }

        if (board.HasKingsideCastleRight(false) || board.HasQueensideCastleRight(false))
        {
            score -= 20;
        }

        //piece value & position
        int whiteScore = 0;
        int blackScore = 0;
        for (int index = 0; index < 64; index++)
        {
            Piece piece = board.GetPiece(new Square(index));
            int pieceType = (int)piece.PieceType - 1;
            if (pieceType > -1)
            {
                if (piece.IsWhite)
                {
                    whiteScore += pieceValues[pieceType + 1] + (int)((pieceSquareValues[pieceType] >> index) & 1) * 5;
                }
                else
                {
                    blackScore += pieceValues[pieceType + 1] + (int)((pieceSquareValues[pieceType] >> 63 - index) & 1) * 5;
                }
            }
        }
        score += whiteScore - blackScore;

        // center control
        int[] centerSquares = { 27, 28, 35, 36 };
        foreach (int square in centerSquares)
        {
            if (board.SquareIsAttackedByOpponent(new Square(square)))
            {
                if (iswhite)
                {
                    score -= 5;
                }
                else
                {
                    score += 5;
                }
            }
        }

        // king safety
        int[] dr = { -1, -1, -1, 0, 0, 1, 1, 1 };
        int[] dc = { -1, 0, 1, -1, 1, -1, 0, 1 };
        int wkingPos = board.GetKingSquare(true).Index;
        int bkingPos = board.GetKingSquare(false).Index;

        for (int i = 0; i < 8; i++)
        {
            int wRow = wkingPos / 8 + dr[i];
            int wCol = wkingPos % 8 + dc[i];
            int bRow = bkingPos / 8 + dr[i];
            int bCol = bkingPos % 8 + dc[i];
            if (wRow >= 0 && wRow < 8 && wCol >= 0 && wCol < 8)
            {
                score += 1;
            }
            if (bRow >= 0 && bRow < 8 && bCol >= 0 && bCol < 8)
            {
                score -= 1;
            }

        }

        return score;
    }

}
