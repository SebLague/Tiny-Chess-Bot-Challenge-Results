namespace auto_Bot_312;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_312 : IChessBot
{
    private const int MaxDepth = 4;
    private bool isWhite;
    Random rng = new();
    private List<Move> previousMoves = new List<Move>();
    private int[] pieceValues = { 0, 1, 3, 3, 5, 9 };
    private Dictionary<ulong, float> transpositionTable = new Dictionary<ulong, float>();
    private string[] ltrs = { "a", "b", "c", "d", "e", "f", "g", "h" };

    public Move Think(Board board, Timer timer)
    {
        isWhite = board.IsWhiteToMove;
        Move x = AlphaBeta(board, MaxDepth, float.NegativeInfinity, float.PositiveInfinity, true).Item1;
        return (Move)x;
    }

    private (Move, float) AlphaBeta(Board board, int depth, float alpha, float beta, bool maximizingPlayer)
    {

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return (new Move(), EvaluateBoard(board, null));
        }

        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = legalMoves[rng.Next(legalMoves.Length)];

        if (maximizingPlayer)
        {
            float maxEval = float.NegativeInfinity;

            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, false).Item2;

                if (eval > maxEval || (eval == maxEval && rng.Next(3) == 0))
                {
                    maxEval = eval;
                    bestMove = move;
                }
                if (board.IsInCheckmate())
                {
                    maxEval = float.PositiveInfinity;
                    bestMove = move;
                    eval = maxEval;
                }

                alpha = Math.Max(alpha, eval);

                board.UndoMove(move);
                if (beta <= alpha)
                    break;
            }


            return (bestMove, maxEval);
        }
        else
        {
            float minEval = float.PositiveInfinity;

            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float eval = AlphaBeta(board, depth - 1, alpha, beta, true).Item2;

                if (eval < minEval || (eval == minEval && rng.Next(3) == 0))
                {
                    minEval = eval;
                    bestMove = move;
                }
                if (board.IsInCheckmate())
                {
                    minEval = float.NegativeInfinity;
                    bestMove = move;
                    eval = minEval;
                }

                beta = Math.Min(beta, eval);
                board.UndoMove(move);
                if (beta <= alpha)
                    break;
            }



            return (bestMove, minEval);
        }
    }


    private float EvaluateBoard(Board board, Move? move)
    {
        int score = 0;
        for (int i = 1; i < 6; i++)
        {
            PieceList k = board.GetPieceList((PieceType)i, isWhite);
            score += pieceValues[i] * k.Count;
        }
        for (int i = 1; i < 6; i++)
        {
            PieceList k = board.GetPieceList((PieceType)i, !isWhite);
            score -= pieceValues[i] * k.Count;
        }
        //if (board.IsDraw()){return -10;}
        return (score + EvaluateKingSafety(board)) * 20 + distanceToKing(board, isWhite) + board.GetLegalMoves().Length;
    }

    private int distanceToKing(Board board, bool playingWhite)
    {
        Square kingSquare = board.GetKingSquare(!playingWhite);
        int kx = kingSquare.File;
        int ky = kingSquare.Rank;
        int distance = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (pieceList.IsWhitePieceList == playingWhite)
            {
                foreach (Piece piece in pieceList)
                {
                    Square pieceSquare = piece.Square;
                    int px = pieceSquare.File;
                    int py = pieceSquare.Rank;
                    distance += Math.Abs(px - kx) + Math.Abs(py - ky);
                }
            }
        }
        return distance;
    }

    private int EvaluateKingAttackedSquares(ulong playerKingAttacks, ulong opponentAttacks)
    {
        // Evaluate the number of squares attacked by the opponent's pieces around the player's king
        ulong attackedSquares = playerKingAttacks & opponentAttacks;
        int safetyScore = -BitboardHelper.GetNumberOfSetBits(attackedSquares) * 10;

        return safetyScore;
    }

    private int EvaluateKingSafety(Board board)
    {
        Square playerKingSquare = board.GetKingSquare(isWhite);
        ulong playerKingAttacks = BitboardHelper.GetKingAttacks(playerKingSquare);

        // Evaluate the number of squares attacked by the opponent's pieces around the player's king
        ulong opponentAttacks = isWhite ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
        int safetyScore = EvaluateKingAttackedSquares(playerKingAttacks, opponentAttacks);

        // Return a combination of the pawn shield and safety scores
        return safetyScore;
    }

}
