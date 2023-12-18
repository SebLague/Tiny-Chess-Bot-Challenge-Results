namespace auto_Bot_186;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_186 : IChessBot
{
    // Compressed the entire square piece table to save a couple of tokens (saves 76 tokens including the decompression algorithm)
    private int[] squarePieceTable = new int[] {
        // Compressed pawn table, saves 11 tokens
        0,  8,
        50, 8,
        10, 2, 20, 1,  30, 2, 20, 1,  10, 2,
         5, 2, 10, 1,  25, 2, 10, 1,   5, 2,
         0, 3, 20, 2,   0, 3,
         5, 1, -5, 1, -10, 1,  0, 2, -10, 1, -5, 1, 5, 2,
               10, 2, -20, 2, 10, 2,   5, 1,
         0, 8, 
        
        // Compressed knight table, costs 21 tokens
       -50, 1, -40, 1, -30, 4, -40, 1, -50, 1,
       -40, 1, -20, 1,   0, 4, -20, 1, -40, 1,
       -30, 1,   0, 1,  10, 1,  15, 2,  10, 1, 0, 1, -30, 2,
                 5, 1,  15, 1,  20, 2,  15, 1, 5, 1, -30, 2,
                 0, 1,  15, 1,  20, 2,  15, 1, 0, 1, -30, 2,
                 5, 1,  10, 1,  15, 2,  10, 1, 5, 1, -30, 1,
       -40, 1, -20, 1,   0, 1,   5, 2,   0, 1, -20, 1, -40, 1,
       -50, 1, -40, 1, -30, 4, -40, 1, -50, 1, 
        
        // Compressed bishop table, saves 21 tokens
       -20, 1, -10, 6, -20, 1,
       -10, 1,   0, 6, -10, 2,
                 0, 1,   5, 1, 10, 2,   5, 1, 0, 1, -10, 2,
                 5, 2,  10, 2,  5, 2, -10, 2,
                 0, 1,  10, 4,  0, 1, -10, 2,
                10, 6, -10, 2,
                 5, 1,   0, 4,  5, 1, -10, 1,
       -20, 1, -10, 6, -20, 1,
        
        // Compressed rook table, saves 32 tokens
        0, 8,
        5, 1, 10, 6, 5, 1,
       -5, 1, 0, 6, -5, 2,
              0, 6, -5, 2,
              0, 6, -5, 2,
              0, 6, -5, 2,
              0, 6, -5, 1,
        0, 3, 5, 2,  0, 3, 
        
        // Compressed queen table, saves 4 tokens
       -20, 1, -10, 2,  -5, 2, -10, 2, -20, 1,
       -10, 1,   0, 6, -10, 2,
                 0, 1,   5, 4,   0, 1, -10, 1,
                -5, 1,   0, 1,   5, 4,   0, 1, -5, 1,
                 0, 2,   5, 4,   0, 1,  -5, 1,
       -10, 1,   5, 5,   0, 1, -10, 2,
                 0, 1,   5, 1,   0, 4, -10, 1,
       -20, 1, -10, 2,  -5, 2, -10, 2, -20, 1, 
        
       // Compressed king table, saves 19 tokens
        -30, 1, -40, 2, -50, 2, -40, 2, -30, 2,
                -40, 2, -50, 2, -40, 2, -30, 2,
                -40, 2, -50, 2, -40, 2, -30, 2,
                -40, 2, -50, 2, -40, 2, -30, 1,
        -20, 1, -30, 2, -40, 2, -30, 2, -20, 1,
        -10, 1, -20, 6, -10, 1,
         20, 2,   0, 4,  20, 3,
                 30, 1,  10, 1, 0, 2, 10, 1, 30, 1, 20, 1
    };

    public Bot_186() => squarePieceTable = squarePieceTable.SelectMany((n, i) =>
        i % 2 == 0 ?
            Enumerable.Repeat(n, squarePieceTable[i + 1]) :
            Enumerable.Empty<int>()
    ).ToArray();

    public Move Think(Board board, Timer timer) => MiniMax(board, 4, int.MinValue, int.MaxValue, board.IsWhiteToMove).Item2;

    private (int, Move) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (board.IsInCheckmate()) return (board.IsWhiteToMove ? int.MinValue : int.MaxValue, Move.NullMove);
        if (board.IsDraw()) return (0, Move.NullMove);
        if (depth == 0) return (board.GetAllPieceLists().Sum(list => list.Sum(GetPieceValue) * (list.IsWhitePieceList ? 1 : -1)), Move.NullMove);

        int bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;
        Move bestMove = board.GetLegalMoves()[0];

        foreach (Move move in board.GetLegalMoves().OrderBy(move => (
            IsCheckMove(move, board),
            move.IsCapture ? GetPieceValue(board.GetPiece(move.TargetSquare)) : 0)
        ).Reverse())
        {
            board.MakeMove(move);
            int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer).Item1;
            board.UndoMove(move);

            if (maximizingPlayer ? eval > bestEval : eval < bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }

            if (maximizingPlayer) alpha = Math.Max(alpha, eval);
            else beta = Math.Min(beta, eval);

            if (beta <= alpha) break;
        }

        return (bestEval, bestMove);
    }

    private bool IsCheckMove(Move move, Board board)
    {
        board.MakeMove(move);
        bool isCheckMove = board.IsInCheck();
        board.UndoMove(move);
        return isCheckMove;
    }

    private int GetPieceValue(Piece piece) => piece.PieceType == PieceType.None ? 0 : new int[] { 0, 100, 320, 330, 500, 900, 0 }[(int)piece.PieceType] + squarePieceTable[((int)piece.PieceType - 1) * 64 + piece.Square.Index];
}
