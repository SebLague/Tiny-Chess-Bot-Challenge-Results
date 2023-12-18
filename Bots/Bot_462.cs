namespace auto_Bot_462;
using ChessChallenge.API;
using System;

public class Bot_462 : IChessBot
{
    bool amIWhite = false;
    public Move Think(Board board, Timer timer)
    {
        amIWhite = board.IsWhiteToMove;
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new();
        Move bestMove = allMoves[rng.Next(allMoves.Length)];
        int depth = board.PlyCount <= 20 ? (board.PlyCount <= 40 ? 4 : 3) : 3; // Adjust the desired depth for your search

        // Create a timer to limit the search time
        float alpha = float.MinValue;
        float beta = float.MaxValue;

        foreach (Move move in allMoves)
        {
            Board cloneBoard = board; // Create a copy of the board to make the move
            cloneBoard.MakeMove(move);

            float score = AlphaBeta(cloneBoard, depth - 1, alpha, beta, false);
            if (score > alpha && score < 100000)
            {
                alpha = score;
                bestMove = move;
            }
            cloneBoard.UndoMove(move);
        }

        return bestMove;
    }

    float AlphaBeta(Board board, int depth, float alpha, float beta, bool isMaximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate())
        {
            return Evaluate(board);
        }

        float bestValue = isMaximizingPlayer ? float.MinValue : float.MaxValue;

        foreach (Move move in board.GetLegalMoves())
        {
            Board cloneBoard = board; // Create a copy of the board to make the move
            cloneBoard.MakeMove(move);

            float score = AlphaBeta(cloneBoard, depth - 1, alpha, beta, !isMaximizingPlayer);

            if (isMaximizingPlayer)
            {
                bestValue = MathF.Max(bestValue, score);
                alpha = MathF.Max(alpha, score);
            }
            else
            {
                bestValue = MathF.Min(bestValue, score);
                beta = MathF.Min(beta, score);
            }

            cloneBoard.UndoMove(move);

            if (beta <= alpha)
            {
                break; // Prune the search tree
            }
        }

        return bestValue;
    }

    private static readonly float[] values = { 0, 1, 3, 3.5f, 5, 9, 1 };
    private static readonly int[] squares = { 18, 19, 20, 21, 26, 27, 28, 29, 35, 36, 37, 38, 43, 44, 45, 46 };
    float Evaluate(Board board)
    {
        // Your evaluation function goes here
        // Return a score for the current state of the board
        float whiteScore = getScore(board, true);
        float blackScore = getScore(board, false);
        float extra = board.IsDraw() ? -100 : 0;
        extra += (amIWhite & board.IsInCheckmate() & !board.IsWhiteToMove) | (!amIWhite & board.IsInCheckmate() & board.IsWhiteToMove) ? 1000 : 0;
        extra += (!amIWhite & board.IsInCheckmate() & !board.IsWhiteToMove) | (amIWhite & board.IsInCheckmate() & board.IsWhiteToMove) ? -1000 : 0;
        extra += (amIWhite & board.IsInCheck() & !board.IsWhiteToMove) | (!amIWhite & board.IsInCheck() & board.IsWhiteToMove) ? 3f : 0;
        extra += (amIWhite & board.IsInCheck() & board.IsWhiteToMove) | (!amIWhite & board.IsInCheck() & !board.IsWhiteToMove) ? -3f : 0;
        extra += -board.PlyCount / 50.0f;
        ulong bitboard = amIWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        float sum = 0.0f;
        foreach (int index in squares)
        {
            sum += BitboardHelper.SquareIsSet(bitboard, new Square(index)) ? 1 : 0;
        }
        extra += amIWhite & (board.HasKingsideCastleRight(amIWhite) | board.HasQueensideCastleRight(amIWhite)) ? 1 : 0;
        return (amIWhite ? whiteScore - blackScore : blackScore - whiteScore) + extra + (board.PlyCount <= 15 ? (sum / squares.Length) : 0);
    }

    private static float getScore(Board board, bool white)
    {
        float score = 0;
        foreach (PieceType piece in Enum.GetValues(typeof(PieceType)))
        {
            score += values[(int)piece] * countPieces(board.GetPieceBitboard(piece, white));
        }
        return score;
    }

    private static int countPieces(ulong pieceBitboards)
    {
        int count = 0;
        while (pieceBitboards > 0)
        {
            if ((pieceBitboards & 1UL) == 1UL)
            {
                count++;
            }
            pieceBitboards >>= 1;
        }
        return count;
    }
}