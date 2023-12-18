namespace auto_Bot_485;
using ChessChallenge.API;
using System;

public class Bot_485 : IChessBot
{
    Move BestMove;
    int[] PieceValues = { 100, 320, 330, 500, 900, 2000 };
    int[,] PieceSquareArray = {
    {0,0,0,0,0,0,0,0,50,50,50,50,50,50,50,50,10,10,20,30,30,20,10,10,5,5,10,25,25,10,5,5,0,0,0,20,20,0,0,0,5,-5,-10,0,0,-10,-5,5,5,10,10,-20,-20,10,10,5,0,0,0,0,0,0,0,0},
    {-50,-40,-30,-30,-30,-30,-40,-50,-40,-20,0,0,0,0,-20,-40,-30,0,10,15,15,10,0,-30,-30,5,15,20,20,15,5,-30,-30,0,15,20,20,15,0,-30,-30,5,10,15,15,10,5,-30,-40,-20,0,5,5,0,-20,-40,-50,-40,-30,-30,-30,-30,-40,-50,},
    {-20,-10,-10,-10,-10,-10,-10,-20,-10,0,0,0,0,0,0,-10,-10,0,5,10,10,5,0,-10,-10,5,5,10,10,5,5,-10,-10,0,10,10,10,10,0,-10,-10,10,10,10,10,10,10,-10,-10,5,0,0,0,0,5,-10,-20,-10,-10,-10,-10,-10,-10,-20,},
    {0,0,0,0,0,0,0,0,5,10,10,10,10,10,10,5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,-5,0,0,0,0,0,0,-5,0,0,0,5,5,0,0,0},
    {-20,-10,-10,-5,-5,-10,-10,-20,-10,0,0,0,0,0,0,-10,-10,0,5,5,5,5,0,-10,-5,0,5,5,5,5,0,-5,0,0,5,5,5,5,0,-5,-10,5,5,5,5,5,0,-10,-10,0,5,0,0,0,0,-10,-20,-10,-10,-5,-5,-10,-10,-20},
    {-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-30,-40,-40,-50,-50,-40,-40,-30,-20,-30,-30,-40,-40,-30,-30,-20,-10,-20,-20,-20,-20,-20,-20,-10,20,20,0,0,0,0,20,20,20,30,10,0,0,10,30,20}
    };
    public Move Think(Board board, Timer timer)
    {
        Search(board, 4, -1000000, 1000000, true);
        return BestMove;
    }
    /// <summary>
    /// Search for the best move usung alpha-beta pruning
    /// </summary>
    int Search(Board board, int depth, int alpha, int beta, bool isFirst)
    {
        // If no legal moves, it is either checkmate or stalemate 
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 0)
        {
            return (board.IsInCheck() ? 1000000 : 0);
        }
        if (board.IsDraw())
        {
            return 1000000;
        }
        // If depth is zero, evaluate position 
        if (depth == 0)
        {
            return Evaluate(board);
        }
        // Loop through all of the moves
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -Search(board, depth - 1, -beta, -alpha, false);
            board.UndoMove(move);
            if (eval >= beta)
            {
                // Move was too good, opponenet will ignore this
                // *snip*
                return beta;
            }
            if (eval > alpha)
            {
                if (isFirst) { BestMove = move; }
            }
            alpha = Math.Max(alpha, eval);
        }
        return alpha;
    }
    /// <summary>
    /// Evaluate the position
    /// </summary>
    int Evaluate(Board board)
    {
        int perspective = (board.IsWhiteToMove ? 1 : -1);
        int MaterialDifference = 0;
        int PieceScore = 0;
        int MopupScore;
        int Count = -2;
        foreach (PieceList list in board.GetAllPieceLists()) { Count += list.Count; }
        for (int i = 0; i < 6; i++)
        { MaterialDifference += PieceValues[i] * (board.GetAllPieceLists()[i].Count - board.GetAllPieceLists()[i].Count); }
        foreach (PieceList pieces in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieces)
            {
                if (!piece.IsNull)
                {
                    PieceScore += (piece.IsWhite ? 1 : -1) * PieceSquareArray[
                        (int)piece.PieceType - 1, (
                            piece.IsWhite ? piece.Square.Index : (piece.Square.File + 8 * (7 - piece.Square.Rank)))];
                }
            }
        }
        float dst = 1 / (1 +
            Math.Abs(board.GetKingSquare(board.IsWhiteToMove).File - board.GetKingSquare(!board.IsWhiteToMove).File) +
            Math.Abs(board.GetKingSquare(board.IsWhiteToMove).Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank)
            );
        dst *= dst <= 0.25 ? -1 : 1;
        int FileRankMin = Math.Min(board.GetKingSquare(!board.IsWhiteToMove).File, board.GetKingSquare(!board.IsWhiteToMove).Rank);
        MopupScore = (int)((16 - Count) * (10 * dst + Math.Min(7 - FileRankMin, FileRankMin)) / 2);

        return (int)(perspective * (0.6 * MaterialDifference + 0.2 * PieceScore + 0.1 * MopupScore));
    }
}