namespace auto_Bot_285;

using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_285 : IChessBot
{

    public static int[] pieceValues = { 0, 100, 310, 330, 500, 900 };

    int infinity = 999999999, negativeInfinity = -999999999;

    Board test = Board.CreateBoardFromFEN("rnbqkbnr / pppppppp / 8 / 8 / 8 / 8 / PPPPPPPP / RNBQKBNR");



    MoveComparer moveComparer = new MoveComparer();

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = Move.NullMove;
        int ThinkTime = timer.MillisecondsRemaining / 60;

        int score = 0;

        int depth = 0;
        for (int i = 0; i < 100; i++)
        {
            score = Negamax(i, board, ref bestMove, negativeInfinity, infinity);
            depth++;

            if (timer.MillisecondsElapsedThisTurn * 3 >= ThinkTime) break;

        }


        return bestMove;
    }

    int Negamax(int depth, Board board, ref Move bestMove, int alpha, int beta)
    {

        Move[] moves = board.GetLegalMoves();

        Array.Sort(moves, moveComparer);
        Array.Reverse(moves);
        Move m = Move.NullMove;

        int bestScore = negativeInfinity;

        if (moves.Length == 0)
        {
            if (board.IsInCheck()) return negativeInfinity;
            return 0;
        }
        if (moves.Contains(bestMove))
        {
            int swapIndex = Array.IndexOf(moves, bestMove);
            m = moves[0];
            moves[0] = bestMove;
            moves[swapIndex] = m;
        }
        else bestMove = moves[0];

        if (board.IsDraw()) return 0;

        if (depth == 0) return SearchCaptures(board, alpha, beta);


        foreach (Move move in moves)
        {

            board.MakeMove(move);
            int score = -Negamax(depth - 1, board, ref m, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
            {
                bestMove = move;
                return beta;
            }

            if (score > alpha)
            {
                bestMove = move;
                alpha = score;
            }
        }
        return alpha;
    }

    int SearchCaptures(Board board, int alpha, int beta)
    {

        int evaluation = Evaluate(board, board.IsWhiteToMove);
        if (evaluation >= beta) return beta;
        alpha = Math.Max(alpha, evaluation);

        Move[] moves = board.GetLegalMoves(true);

        Array.Sort(moves, moveComparer);
        Array.Reverse(moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            evaluation = -SearchCaptures(board, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta) return beta;
            alpha = Math.Max(alpha, evaluation);
        }
        return alpha;
    }


    int Evaluate(Board board, bool white)
    {

        int evaluation = 0;

        float endgameFactor = (32 - board.GetAllPieceLists().Length) / 32;

        for (int i = 1; i < 6; i++)
        {
            evaluation += board.GetPieceList((PieceType)i, true).Count * pieceValues[i];
            evaluation -= board.GetPieceList((PieceType)i, false).Count * pieceValues[i];
        }

        if (board.HasKingsideCastleRight(true)) evaluation += 100;
        if (board.HasKingsideCastleRight(false)) evaluation -= 100;
        if (board.HasQueensideCastleRight(true)) evaluation += 75;
        if (board.HasQueensideCastleRight(false)) evaluation -= 75;



        evaluation += KingPositionBonus(board, endgameFactor, 40);
        evaluation += DevelopPieceBonus(board);


        return white ? evaluation : -evaluation;
    }

    int KingPositionBonus(Board board, float endgameFactor, int affectiveness)
    {

        float evaluation = 0;

        int blackKingI = board.GetKingSquare(false).Rank;
        int blackKingJ = board.GetKingSquare(false).File;
        int whiteKingI = board.GetKingSquare(true).Rank;
        int whiteKingJ = board.GetKingSquare(true).File;


        evaluation += Math.Abs(blackKingI - 3.5f) * endgameFactor * affectiveness;
        evaluation += Math.Abs(blackKingJ - 3.5f) * endgameFactor * affectiveness;
        evaluation -= Math.Abs(whiteKingI - 3.5f) * endgameFactor * affectiveness;
        evaluation -= Math.Abs(whiteKingJ - 3.5f) * endgameFactor * affectiveness;

        return Convert.ToInt32(evaluation);
    }
    int DevelopPieceBonus(Board board)
    {
        int bonus = 0;

        for (int i = 2; i < 8; i++)
        {
            for (int j = 3; j < 7; j++)
            {
                if ((int)board.GetPiece(new Square(i, j)).PieceType > 1) bonus += 15;
            }
        }

        int centerSquaresAttacked = 0;
        for (int i = 4; i < 6; i++)
        {
            for (int j = 4; j < 6; j++)
            {
                if (board.SquareIsAttackedByOpponent(new Square(i, j))) centerSquaresAttacked++;
            }
        }
        bonus += (board.IsWhiteToMove ? -centerSquaresAttacked : centerSquaresAttacked) * 15;


        return bonus;
    }
}

class MoveComparer : IComparer<Move>
{

    public int Compare(Move m1, Move m2)
    {

        int x = GuessMoveEvaluation(m1);
        int y = GuessMoveEvaluation(m1);

        return x.CompareTo(y);

    }

    public int GuessMoveEvaluation(Move m1)
    {
        int guess = 0;
        if (m1.IsPromotion) guess += 10;
        if ((int)m1.MovePieceType < (int)m1.CapturePieceType) guess += 15;
        if (m1.MovePieceType == PieceType.Pawn) guess += 3;

        return guess;
    }
}