namespace auto_Bot_299;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// Submission by Markus Grand Petersen from Copenhagen

// The bot uses the Minimax algorithm with Alpha-Beta pruning to efficiently evaluate 
// child positions to a depth of 5 based on the heuristics in evaluateBoard().

// Implementation of the MiniMax with Alpha-Beta pruning from the Pseudocode at the link
// https://www.geeksforgeeks.org/minimax-algorithm-in-game-theory-set-4-alpha-beta-pruning/

public class Bot_299 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int depth = 5;
        //White is the maximizer (goal is a positive score)
        //Black is the minimizer (goal is a negative score)
        var result = Minimax(board, depth, board.IsWhiteToMove, float.NegativeInfinity, float.PositiveInfinity);
        //DivertedConsole.Write("Bot eval: "+ result.Item1);
        DivertedConsole.Write("Actual eval: " + evaluateBoard(board));
        return result.Item2;
    }

    // Dictionary to assign values to each piece type.
    private readonly Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int> {
        { PieceType.None, 0 },
        { PieceType.King, 900 },
        { PieceType.Queen, 90 },
        { PieceType.Rook, 50 },
        { PieceType.Bishop, 30 },
        { PieceType.Knight, 30 },
        { PieceType.Pawn, 10 }
    };

    /// <summary>
    /// Evaluates the board position based on heuristic
    /// </summary>
    public float evaluateBoard(Board board)
    {
        float eval = 0;

        foreach (PieceList pList in board.GetAllPieceLists())
        {
            foreach (Piece p in pList)
            {
                // Base value of piece heuristic
                float value = pieceValues[p.PieceType];

                //Material evaluation sum (white is positive, black is negative).
                eval += p.IsWhite ? value : -value;
            }
        }

        // Mobility bonus
        eval += board.IsWhiteToMove ? 0.2f * board.GetLegalMoves().Length : -0.2f * board.GetLegalMoves().Length;

        // Check bonus
        eval += board.IsInCheck() ? (board.IsWhiteToMove ? -5 : 5) : 0;


        if (board.IsInCheckmate())
        {
            eval = board.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity;
        }

        if (board.IsDraw())
        {
            eval = 0;
        }

        return eval;
    }

    /// <summary>
    /// Minimax function with Alpha-Beta pruning.
    /// This function runs recursively until it reaches the given depth or finds a terminal state (Checkmate or Draw).
    /// It returns the evaluation of the board and the best move to reach that evaluation.
    /// </summary>
    public (float, Move) Minimax(Board board, int depth, bool isMaximizing, float alpha, float beta)
    {
        if (board.IsInCheckmate() || board.IsDraw() || depth == 0)
        {
            return (evaluateBoard(board), new Move());
        }

        Move[] moves = board.GetLegalMoves();
        moves = moves.OrderByDescending(m => board.GetPiece(m.TargetSquare).PieceType != PieceType.None)
                 .ThenByDescending(m => pieceValues[board.GetPiece(m.TargetSquare).PieceType])
                 .ToArray();
        Move bestMove = new Move();


        if (isMaximizing)
        {
            float maxEval = float.NegativeInfinity;

            foreach (Move m in moves)
            {
                board.MakeMove(m);
                bool isMate = board.IsInCheckmate();
                if (isMate)
                {
                    bestMove = m;
                    board.UndoMove(m);
                    break;
                }
                (float eval, Move _) = Minimax(board, depth - 1, false, alpha, beta);


                // Checking if the evaluation of this move is better than the current maxEval
                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = m;
                }

                board.UndoMove(m);

                // Alpha-Beta pruning
                alpha = Math.Max(alpha, maxEval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return (maxEval, bestMove);
        }
        else
        {
            float minEval = float.PositiveInfinity;
            foreach (Move m in moves)
            {
                board.MakeMove(m);
                bool isMate = board.IsInCheckmate();
                if (isMate)
                {
                    bestMove = m;
                    board.UndoMove(m);
                    break;
                }
                (float eval, Move _) = Minimax(board, depth - 1, true, alpha, beta);

                // Checking if the evaluation of this move is better than the current minEval
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = m;
                }

                board.UndoMove(m);
                //Alpha-Beta pruning
                beta = Math.Min(beta, minEval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return (minEval, bestMove);
        }
    }
}
