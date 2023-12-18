namespace auto_Bot_161;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_161 : IChessBot
{
    /* Starting numbers: 8 pawns, 2 knights, 2 bishops, 2 rooks, queen
     * => each side starts with 800 + 600 + 600 + 1000 + 900 = 3900 "value"
     * Thus material evaluation will always give a value between -3900 and 3900 for zero promotions, and a value between
     *  -10300 and 10300 if every single pawn is promoted to a queen with zero material on the other side.
     * Pawn piece value of 100 gives of a score of 100 to play around with for the positional side of the evaluation
     * before we start valueing it more than material advantage.
     */
    int[] pawnPusher = { 0, 1, 8, 8, 9, 16, 100, 0 };
    int[] pieceValues = { 100, 300, 300, 500, 900, 0 };
    int scoreMAX = 10000, scoreMIN = -10000;
    int evaluatedNodes, nodeLimit;
    public Move Think(Board board, Timer timer)
    {
        evaluatedNodes = 0;
        nodeLimit = 1000000;
        int myColor = board.IsWhiteToMove ? 1 : -1;
        // int pieceDifference = getPieceDifference(board, myColor);
        int score = 0, bestScore = scoreMIN;
        // Move[] orderedMoves = getOrderedMoves(board);
        List<Move> orderedMoves = getOrderedMoves(board).ToList();
        Move bestMove = orderedMoves[0];
        // Dictionary<Move, int> moveScores = new Dictionary<Move, int>();
        // Without programmed openings, just play e4 as white.
        if (board.PlyCount == 0)
        {
            Move e4 = new Move("d2d4", board);
            return e4;
        }
        // int thinkTime = 0;              // Default value
        int depth = 3;  // Default value
        if ((board.PlyCount > 80) && (timer.MillisecondsRemaining > 20000)) depth = 5;
        // while (depth <= depthLimit) {
        foreach (Move move in orderedMoves)
        {
            if (investigateMove(board, move, 2))
            {
                bestScore = score;
                bestMove = move;
                break;
            }

            board.MakeMove(move);
            score = -negamax(depth, board, -myColor, scoreMIN, scoreMAX);
            // moveScores[move] = score; // Store the score for this move.
            board.UndoMove(move);

            if (score >= bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        // orderedMoves.Sort((move1, move2) => moveScores[move2].CompareTo(moveScores[move1]));
        // depth++;
        // }
        DivertedConsole.Write("LouBot | eval: " + (float)(score / 100.00) + " Nodes: " + evaluatedNodes + " Depth: " + depth); // #DEBUG
        return bestMove;
    }

    int negamax(int depth, Board board, int colour, int alpha, int beta)
    {
        evaluatedNodes++;
        if (investigateState(board, 0)) return 0;
        if (investigateState(board, 2)) return colour * scoreMAX;
        if ((evaluatedNodes > nodeLimit) || (depth == 0))
        {
            return colour * eval(board);  // If the limit is reached, return the current score.
        }
        Move[] allMoves = getOrderedMoves(board);
        int score;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            score = -negamax(depth - 1, board, -colour, -beta, -alpha);
            board.UndoMove(move);
            if (score >= beta) return beta;  // Fail hard beta-cutoff
            if (score > alpha) alpha = score;
        }
        return alpha;
    }
    // int Quiesce(Board board, int colour, int alpha, int beta){
    //     int score;
    //     // retain aka standing pat
    //     int retain = colour * eval(board);
    //     if (retain >= beta) return beta;    // Fail hard
    //     if (alpha < retain) alpha = retain; // Fail soft

    //     // Get all legal moves at the beginning to use throughout the function.
    //     Move[] allLegalMoves = board.GetLegalMoves();
    //     Move[] allCaptures = board.GetLegalMoves(true);

    //     foreach (Move capture in allCaptures){
    //         board.MakeMove(capture);
    //         score = -Quiesce(board, -colour, -beta, -alpha);
    //         board.UndoMove(capture);

    //         if (score >= beta) return beta;
    //         if (score > alpha) alpha = score;
    //     }
    //     return alpha;
    // }

    int eval(Board board)
    {
        bool isWhite = board.IsWhiteToMove;
        int whiteScore = getColourScore(board, true);
        int blackScore = getColourScore(board, false);
        return (whiteScore - blackScore);
    }

    int getColourScore(Board board, bool colour)
    {
        int score = 0;
        for (int pieceType = 1; pieceType < 6; pieceType++)
        {
            PieceList pieces = board.GetPieceList((PieceType)pieceType, colour);
            score += (pieces.Count) * pieceValues[pieceType - 1];
            if (pieceType == 1)
            {
                pawnScore(pieces, ref score, colour);
            }
            else
            {
                scorePieces(ref score, pieces);
            }
        }
        return score;
    }

    void pawnScore(PieceList pawns, ref int score, bool white)
    {
        foreach (Piece pawn in pawns)
        {
            if (white)
                score += pawnPusher[pawn.Square.Rank];
            else
                score += pawnPusher[8 - pawn.Square.Rank];
        }
    }

    void scorePieces(ref int score, PieceList pieces)
    {
        foreach (Piece piece in pieces)
        {
            Square square = piece.Square;
            score += centerPos(square);
        }
    }

    int centerPos(Square square)
    {
        // Bonus is between 1 and 16
        int rankScore = (int)(3.5 - Math.Abs(square.Rank - 3.5)) + 1;
        int fileScore = (int)(3.5 - Math.Abs(square.File - 3.5)) + 1;
        return rankScore * fileScore;
    }

    Move[] getOrderedMoves(Board board)
    {
        List<Move> checks = new List<Move>();
        List<Move> captures;
        List<Move> others = new List<Move>();

        Move[] allMoves = board.GetLegalMoves();
        Move[] allCaptures = board.GetLegalMoves(true);

        foreach (Move move in allMoves)
        {
            if (investigateMove(board, move, 1))
                checks.Add(move);
            else if (move.IsCapture)
                continue;
            else
                others.Add(move);
        }

        captures = getOrderedCaptures(allCaptures);

        // Concatenate the lists in the desired order
        List<Move> orderedMoves = checks;
        orderedMoves.AddRange(captures);
        orderedMoves.AddRange(others);

        return orderedMoves.ToArray();
    }

    List<Move> getOrderedCaptures(Move[] captures)
    {
        List<Move> captureList = new List<Move>(captures);
        captureList.Sort((move1, move2) => getScoreChange(move2).CompareTo(getScoreChange(move1)));
        return captureList;
    }

    int getScoreChange(Move move)
    {
        return (pieceValues[(int)move.MovePieceType - 1] - pieceValues[(int)move.CapturePieceType - 1]);
    }

    bool investigateMove(Board board, Move move, int val)
    {
        board.MakeMove(move);
        bool result = investigateState(board, val);
        board.UndoMove(move);
        return result;
    }
    bool investigateState(Board board, int val)
    {
        Func<bool>[] conditions = { board.IsDraw, board.IsInCheck, board.IsInCheckmate };
        return conditions[val]();
    }

}