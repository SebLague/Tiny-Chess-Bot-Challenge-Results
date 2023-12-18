namespace auto_Bot_600;
using ChessChallenge.API;
using System;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class Bot_600 : IChessBot
{
    const int MAX_DEPTH = 7;
    Move BestMove;
    int MyBotColor;
    int OpponentColor;

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        MyBotColor = board.IsWhiteToMove ? 0 : 8; // modified to remove illegal namespace -- seb
        OpponentColor = MyBotColor == 0 ? 8 : 0;
        Minimax(board, MAX_DEPTH, true, long.MinValue, long.MaxValue);
        return BestMove;
    }

    private ulong GetColoredBitboard(int color, Board board)
    {
        return color == 0 ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
    }

    private long EvaluateBoard(Board board, Boolean maximizingPlayer)
    {
        long score = 0;

        ulong myBitboard = GetColoredBitboard(MyBotColor, board);
        ulong opponentBitboard = GetColoredBitboard(OpponentColor, board);

        score += EvaluatePieces(board, myBitboard);
        score -= EvaluatePieces(board, opponentBitboard);

        score += EvaluateCenterControl(board, true);
        score -= EvaluateCenterControl(board, false);

        if (maximizingPlayer && board.IsInCheck())
            score += 100000;
        else
            score -= 100000;

        if (!maximizingPlayer)
            score = -score;

        return score;
    }

    private long EvaluatePieces(Board board, ulong bitboard)
    {
        long score = 0;
        while (bitboard != 0)
        {
            int bitIndex = System.Numerics.BitOperations.TrailingZeroCount(bitboard);
            Square square = new Square(bitIndex);
            Piece piece = board.GetPiece(square);
            score += pieceValues[(int)piece.PieceType];
            bitboard ^= (1UL << bitIndex);
        }
        return score;
    }

    private long EvaluateCenterControl(Board board, bool maximizingPlayer)
    {
        long score = 0;
        Square[] centerSquares = { new Square("d4"), new Square("e4"), new Square("d5"), new Square("e5") };

        foreach (var square in centerSquares)
        {
            Piece piece = board.GetPiece(square);
            if (piece.IsNull) continue;
            if (piece.IsPawn)
            {
                score += maximizingPlayer ? 1000 : -1000;
            }
            else if (piece.IsQueen)
            {
                score += maximizingPlayer ? 100000 : -100000;
            }
        }
        return score;
    }

    private long Minimax(Board board, int depth, Boolean maximizingPlayer, long alpha, long beta)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            if (maximizingPlayer && board.IsInCheckmate())
                return 10000000;
            else if (!maximizingPlayer && board.IsInCheckmate())
                return -10000000;
            else if (board.IsDraw())
                return -1000000;
            return EvaluateBoard(board, maximizingPlayer);
        }
        if (maximizingPlayer)
        {
            var maxEval = long.MinValue;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                var eval = Minimax(board, depth - 1, false, alpha, beta);
                board.UndoMove(move);
                if (eval > maxEval && depth == MAX_DEPTH)
                    // eval is the new best move
                    BestMove = move;
                else
                    // This is where I get the biggest score
                    maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            // Minimizing player
            var minEval = long.MaxValue;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                var eval = Minimax(board, depth - 1, true, alpha, beta);
                board.UndoMove(move);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }
}