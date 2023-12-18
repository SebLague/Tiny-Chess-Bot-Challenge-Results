namespace auto_Bot_202;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
public class Bot_202 : IChessBot
{
    Dictionary<ulong, int> transpositions = new(), quiesces = new();
    Queue<ulong> lastpositions = new(capacity: 1024);
    public Move Think(Board board, Timer timer)
    {
        Move move = PickMove(board, timer.MillisecondsRemaining >= 40000 ? 7 : timer.MillisecondsRemaining >= 20000 ? 4 : 6);
        board.MakeMove(move);
        lastpositions.Enqueue(board.ZobristKey);
        lastpositions.TrimExcess();
        return move;
    }
    /// <summary>
    /// Evaluates a current board based on mobility, material and other factors. Returns a score.
    /// </summary>
    int Eval(Board board)
    {
        int LegalMovesCount = board.GetLegalMoves().Length,
            mobilityValue = 4 * LegalMovesCount + 8 * board.GetLegalMoves(true).Length,
            pieceValue = 0;
        // S: I feel so bad about defining variables like this. 
        // J: I have no shame

        int[] materialValues = { 100, 320, 330, 500, 1000, board.PlyCount >= 25 ? 5000 : 10000 /*Give the King more value in lategame*/ };
        // Material values for P, N, B, R, Q, K
        // These values avoid exchanging minor pieces (N & B) for 3 minor pieces
        if (board.TrySkipTurn()) // Don't skip this if in check.
        {
            int skipLegalMovesCount = board.GetLegalMoves().Length, skipCaptureMovesCount = board.GetLegalMoves(true).Length;
            mobilityValue -= 4 * skipLegalMovesCount + 8 * skipCaptureMovesCount;

            mobilityValue += board.IsInCheckmate() ? 2147483647 :
                                board.IsInCheck() ? 110 :
                                skipLegalMovesCount == 0 ? 1000 :
                                0;

            board.UndoSkipTurn();
        }
        PieceList[] pieces = board.GetAllPieceLists();
        for (int i = 0; i < 6; i++)
        {
            // Calculate material value of current board        
            pieceValue += (pieces[i].Count - pieces[i + 6].Count) * materialValues[i];
            // Calculate positional value of pieces
            bool x = false;
            do
            {
                PieceType pieceType = (PieceType)i + 1;

                double posValue = 0;
                foreach (Piece p in board.GetPieceList(pieceType, x)) // x == false is black
                {
                    var (row, col) = CalcPosition(p.Square.Index, x);
                    posValue =
                        pieceType == PieceType.Knight ? RoundToNearest((MathF.Abs(row - 4.5f) + MathF.Abs(col - 4.5f)) / 2 * 23, 5) - 30 :
                        pieceType == PieceType.Bishop ? RoundToNearest((Math.Max(Math.Abs(row - 4.5), Math.Abs(col - 4.5)) * 10), 5) - 20 :
                        pieceType == PieceType.Rook ? -1 * ((col == 0 || col == 7) ? -5 : 0 + row == 2 ? 10 : 0) :
                        pieceType == PieceType.Queen ? RoundToNearest((MathF.Abs(row - 4.5f) + MathF.Abs(col - 4.5f)) / 4 * 10, 5) - 10 :
                        pieceType == PieceType.Pawn ? row == 2 ? 50 : row == 7 && (col == 4 || col == 5) ? -20 : RoundToNearest((MathF.Abs(row - 4.5f) + MathF.Abs(col - 4.5f)) / 2 * 23, 5) - 30 :
                    0;

                    pieceValue += (int)(posValue * 1);
                }
                x = !x;
            } while (x);
        }

        pieceValue -= board.IsInCheckmate() ? 2147483647 :
                        board.IsInCheck() ? 110 :
                        LegalMovesCount == 0 ? -1000 :
                        0;

        return (mobilityValue + pieceValue + (lastpositions.Contains(board.ZobristKey) ? mobilityValue / 2 : 0)) * (board.IsWhiteToMove ? 1 : -1);
    }
    /// <summary>
    /// Loops through legal moves and evaluates them with the AlphaBeta function.
    /// Always returns a valid move, but not neccesarily the best.
    /// </summary>
    Move PickMove(Board board, int depth)
    {
        int alpha = -2147483647;
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves.Length > 0 ? moves[0] : new();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int value = -AlphaBeta(-int.MaxValue, -alpha, depth - DepthCheck(board), board);
            board.UndoMove(move);
            if (value > alpha)
            {
                alpha = value;
                bestMove = move;
            }
        }
        return bestMove;
    }
    /// <summary>
    /// Recursively calls itself until it hits its maximum depth, then evaluates with the quiesce function.
    /// Also saves already evaluated values in the transpositions variable for later access.
    /// </summary>
    int AlphaBeta(int alpha, int beta, int depth, Board board)
    {
        if (depth <= 0) return Quiesce(alpha, beta, 4, board);
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            if (!transpositions.TryGetValue(board.ZobristKey, out int score))
            {
                score = -AlphaBeta(-beta, -alpha, depth - DepthCheck(board), board);
                transpositions[board.ZobristKey] = score;
            }
            board.UndoMove(move);
            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
    int Quiesce(int alpha, int beta, int depth, Board board)
    {
        int stand_pat = Eval(board);
        if (depth <= 0) return stand_pat;
        if (stand_pat >= beta || alpha < stand_pat) alpha = stand_pat;
        foreach (Move move in board.GetLegalMoves())
        {
            if (move.IsCapture || move.IsPromotion)
            {
                board.MakeMove(move);
                if (!quiesces.TryGetValue(board.ZobristKey, out int score))
                {
                    score = -Quiesce(-beta, -alpha, depth - DepthCheck(board), board);
                    quiesces[board.ZobristKey] = score;
                }
                board.UndoMove(move);
                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
        }
        return alpha;
    }
    int DepthCheck(Board board) => board.IsInCheck() ? 1 : 2;

    /// <summary>
    /// Converts the integer value gotten from a Square.Index to an X and Y value with starting point at the bottom left. 
    /// </summary>
    Tuple<int, int> CalcPosition(int index, bool white)
    {
        return Tuple.Create(Math.Abs(index % 8 - (white ? -63 : 0)) /*row*/, Math.Abs((int)Math.Floor(index / 8f) - (white ? -63 : 0)) /*col*/);
    }
    /// <summary>
    /// Rounds a number to the nearest full "value". 
    /// </summary>
    double RoundToNearest(double number, double value)
    {
        return Math.Round(number / value) * value;
    }
}