namespace auto_Bot_425;
using ChessChallenge.API;
using System;
using System.Linq;

/*  200 Tokens Monstrosity
 * 
 * Improved version of the winning bot the Nano chess community tournament, by ErwanF.
 * The whole bot is 200 tokens.
 * 
 * Features:
 * - alpha-beta search / negamax
 * - move ordering similar to MVV-LVA
 * - iterative deepening with time management
 * - quiescence search
 * - material evaluation (with accurate piece values, slightly inacurate for promotions but good enough)
 * - mobility evaluation (makes the bot play human-like by trying to control the board)
 * 
*/

public class Bot_425 : IChessBot
{
    Move bestRootMove;

    public Move Think(Board board, Timer timer)
    {
        int searchDepth = 0;

        // Defining the search function inside Think to use board and timer directly
        double Search(int depth, double alpha, double beta, double currentEval)
        {
            // Quiescence (no beta cutoff check here, it will be done latter)
            if (depth <= 0 && alpha < currentEval)
                alpha = currentEval;

            // Loop over all legal moves, sorted by highest capture value, then highest promotion value, then lowest piece moved value (similar to MVV-LVA move ordering).
            // At the root node, we start with the move found in the previous iteration.
            foreach (Move move in board.GetLegalMoves(depth <= 0)
                .OrderByDescending(move => (move == bestRootMove, move.CapturePieceType, move.PromotionPieceType - move.MovePieceType)))
            {
                // Beta cutoff check
                if (alpha >= beta)
                    break;

                board.MakeMove(move);

                // Recursive search call, updating at the same time the evaluation with material and mobility
                double score =
                    board.IsDraw() ? 0 : // Check for draw. No need to check for mate, it will be taken care of by the log returning -inf in the mobility
                    -Search(depth - 1, -beta, -alpha,
                        Math.Log(board.GetLegalMoves().Length)  // Mobility eval
                        - BitConverter.GetBytes(0b_11111111_11111110_11100001_01111101_01010000_01001011_00011001_00000000)[(int)move.CapturePieceType | (int)move.PromotionPieceType]  // Material eval
                        - currentEval
                    );

                // Update best score / move
                if (score > alpha)
                {
                    alpha = score;
                    if (depth == searchDepth)
                        bestRootMove = move;
                }

                // Exit search when the time is out (raises an exception when the value is negative)
                Convert.ToUInt32(timer.MillisecondsRemaining - 30 * timer.MillisecondsElapsedThisTurn);

                board.UndoMove(move);
            }

            return alpha;
        }

        // Iterative deepening: increase the depth until the time is out (the search will then raise an exception)
        try
        {
            for (; ; )
                Search(++searchDepth, -30000, 30000, 0);
        }
        catch { }

        return bestRootMove;
    }
}