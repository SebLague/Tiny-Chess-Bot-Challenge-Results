namespace auto_Bot_492;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_492 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = Move.NullMove;
        int bestEval = int.MinValue;
        List<Move> moves = OrderMovesByCapture(board.GetLegalMoves().ToList());
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = minimax(board, 4, int.MinValue, int.MaxValue);
            board.UndoMove(move);
            if (!board.IsWhiteToMove)
                eval *= -1;
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }
        return bestMove;
    }


    public int minimax(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0)
        {
            return GetEvaluation(board);
        }
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        if (board.IsInStalemate())
            return 0;
        if (board.IsDraw())
            return board.IsWhiteToMove ? -100000 : 100000;

        if (board.IsWhiteToMove)
        {
            foreach (Move move in OrderMovesByCapture(board.GetLegalMoves().ToList()))
            {
                board.MakeMove(move);
                int eval = minimax(board, depth - 1, alpha, beta);
                board.UndoMove(move);
                if (eval >= beta)
                    return beta;
                if (eval > alpha)
                    alpha = eval;
            }
            return alpha;
        }
        else
        {
            foreach (Move move in OrderMovesByCapture(board.GetLegalMoves().ToList()))
            {
                board.MakeMove(move);
                int eval = minimax(board, depth - 1, alpha, beta);
                board.UndoMove(move);
                if (eval <= alpha)
                    return alpha;
                if (eval < beta)
                    beta = eval;
            }
            return beta;
        }
    }

    public List<Move> OrderMovesByCapture(List<Move> moves)
    {
        moves = moves.OrderByDescending(m => pieceValues[(int)m.CapturePieceType]).ToList();
        moves = moves.OrderByDescending(m => m.IsCastles).ToList();
        moves = moves.OrderByDescending(m => m.IsPromotion ? 1 : 0).ToList();
        return moves;
    }

    public bool IsRepetition(Board board)
    {
        if (board.GameRepetitionHistory.Contains(board.ZobristKey))
        {
            int count = board.GameRepetitionHistory.Where<ulong>(x => x == board.ZobristKey).ToList().Count();
            if (count >= 2)
                return true;
        }
        return false;
    }

    public int GetEvaluation(Board board)
    {
        int eval = 0;
        int perspective = board.IsWhiteToMove ? 1 : -1;
        if (IsRepetition(board))
            return -1 * perspective;

        if (board.IsInCheckmate())
        {
            return perspective * int.MinValue;
        }
        if (board.IsInStalemate())
            return 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            int team = pieceList.IsWhitePieceList ? 1 : -1;
            foreach (Piece piece in pieceList)
            {
                eval += team * pieceValues[(int)piece.PieceType];
            }
        }
        if (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove))
            eval += 100 * perspective;

        if (board.IsInCheck())
            eval -= 100;

        eval += AttackSquaresNearKing(board);
        eval += GetPiecesOffBackRow(board);

        return eval;
    }

    public int GetPiecesOffBackRow(Board board)
    {
        if (board.GameMoveHistory.Length > 0)
        {
            Move lastMove = board.GameMoveHistory.Last();
            if (board.IsWhiteToMove)
            {
                if (lastMove.StartSquare.Rank == 8)
                    return -10;

            }
            else
            {
                if (lastMove.StartSquare.Rank == 0)
                    return 10;
            }
        }
        return 0;
    }

    public int AttackSquaresNearKing(Board board)
    {
        int blackBonus = 0;
        var whiteKingNearby = BitboardHelper.GetKingAttacks(board.GetKingSquare(true));
        if ((board.BlackPiecesBitboard & whiteKingNearby) > 0)
        {
            blackBonus = -50;
        }
        int whiteBonus = 0;
        var blackKingNearby = BitboardHelper.GetKingAttacks(board.GetKingSquare(false));
        if ((board.WhitePiecesBitboard & blackKingNearby) > 0)
        {
            whiteBonus = 50;
        }
        return blackBonus + whiteBonus;

    }
}