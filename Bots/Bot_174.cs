namespace auto_Bot_174;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;
using PieceList = ChessChallenge.API.PieceList;

public class Bot_174 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int moveNumber = 0;
    int maxDepth = 1;

    private const double DrawPenalty = -50000;
    private const double CheckmateReward = 50000;
    private const double FavorThreshold = 1000;

    public Move Think(Board board, Timer timer)
    {
        moveNumber++;

        if (moveNumber > 5)
            maxDepth = 3;
        if (moveNumber > 7)
            maxDepth = 4;
        if (CountPieces(board, board.IsWhiteToMove) < 5 || CountPieces(board, !board.IsWhiteToMove) < 5)
            maxDepth = 5;
        if (timer.MillisecondsRemaining < 10 * 1000)
            maxDepth = 3;

        Move moveToPlay = BestMove(board);
        return moveToPlay;
    }

    int CountPieces(Board board, bool whitePieces)
    {
        PieceList[] pieceLists = board.GetAllPieceLists();
        int whitePiecesCount = 0;
        int blackPiecesCount = 0;
        for (int i = 0; i < pieceLists.Length; i++)
        {
            whitePiecesCount += pieceLists[i].Count * (pieceLists[i].IsWhitePieceList ? 1 : 0);
            blackPiecesCount += pieceLists[i].Count * (pieceLists[i].IsWhitePieceList ? 0 : 1);
        }

        if (whitePieces)
            return whitePiecesCount;
        else
            return blackPiecesCount;
    }

    int CalculateEval(Board board)
    {
        PieceList[] pieceLists = board.GetAllPieceLists();
        int eval = 0;
        for (int i = 0; i < pieceLists.Length; i++)
        {
            eval += pieceLists[i].Count * pieceValues[i % 6 + 1] * (pieceLists[i].IsWhitePieceList ? 1 : -1);
        }
        return eval;
    }

    public Move BestMove(Board board)
    {
        List<Move> bestMoves = new List<Move>();
        double bestScore = double.NegativeInfinity;

        Move[] moves = board.GetLegalMoves().OrderByDescending(m => pieceValues[(int)m.MovePieceType] - m.MovePieceType == PieceType.King ? 100000 : 0).ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            double score = Minimize(board, maxDepth, double.NegativeInfinity, double.PositiveInfinity);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestMoves.Clear();
                bestMoves.Add(move);
                bestScore = score;
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        Move bestMove = Move.NullMove;
        Random rng = new Random();
        if (bestMoves.Count > 0)
        {
            bestMove = bestMoves[rng.Next(bestMoves.Count)];
        }
        else
        {
            bestMove = moves[rng.Next(moves.Length)];
        }

        int eval = CalculateEval(board);
        DivertedConsole.Write(moveNumber + " - " + maxDepth + " - " + eval + " - " + CountPieces(board, board.IsWhiteToMove) + "/" + CountPieces(board, !board.IsWhiteToMove) + " - " + bestScore + " - " + bestMove);
        return bestMove;
    }

    private double Maximize(Board board, int depth, double alpha, double beta)
    {
        if (depth == 0)
        {
            return board.IsWhiteToMove ? CalculateEval(board) : -CalculateEval(board)
                   - (board.IsInCheckmate() ? CheckmateReward : 0)
                   - (board.IsDraw() ? DrawPenalty : 0);
        }

        double score = double.NegativeInfinity;

        Move[] moves = board.GetLegalMoves()
                            .OrderByDescending(m => pieceValues[(int)m.MovePieceType] - m.MovePieceType == PieceType.King ? 100000 : 0)
                            .ThenByDescending(m => m.IsPromotion && m.PromotionPieceType == PieceType.Queen ? 1 : 0)
                            .ThenByDescending(m => m.IsCapture ? pieceValues[(int)board.GetPiece(m.TargetSquare).PieceType] : 0)
                            .ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = Math.Max(score, Minimize(board, depth - 1, alpha, beta));
            alpha = Math.Max(alpha, score);
            board.UndoMove(move);

            if (alpha >= beta)
            {
                break;
            }
        }

        return score;
    }

    private double Minimize(Board board, int depth, double alpha, double beta)
    {
        if (depth == 0)
        {
            return board.IsWhiteToMove ? -CalculateEval(board) : CalculateEval(board)
                   + (board.IsInCheckmate() ? CheckmateReward : 0)
                   + (board.IsDraw() ? DrawPenalty : 0);
        }

        double score = double.PositiveInfinity;

        Move[] moves = board.GetLegalMoves()
                            .OrderByDescending(m => pieceValues[(int)m.MovePieceType] - m.MovePieceType == PieceType.King ? 100000 : 0)
                            .ThenByDescending(m => m.IsPromotion && m.PromotionPieceType == PieceType.Queen ? 1 : 0)
                            .ThenByDescending(m => m.IsCapture ? pieceValues[(int)board.GetPiece(m.TargetSquare).PieceType] : 0)
                            .ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = Math.Min(score, Maximize(board, depth - 1, alpha, beta));
            beta = Math.Min(beta, score);
            board.UndoMove(move);

            if (beta <= alpha)
            {
                break;
            }
        }

        return score;
    }
}