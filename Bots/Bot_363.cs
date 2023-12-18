namespace auto_Bot_363;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_363 : IChessBot
{
    // the core idea for this one is that I don't want to do any actual tree search (besides iterating through the legal moves for this turn)
    // this was basically my baseline implementation that I was going to improve with other heuristics but they all performed worse
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> best = new();
        int eval = int.MinValue;
        // test each move we can make
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            // if its checkmate, just do it
            if (board.IsInCheckmate())
            {
                best.Clear();
                best.Add(move);
                break;
            }

            // find how many moves the opponent can make
            int opponentPossible = board.GetLegalMoves().Length;

            // find distinct capturable squares
            var opponentCaptures = board.GetLegalMoves(true).Select(m => m.TargetSquare).Distinct();

            // baseline eval for when opponent is in check, can't calc number of possible moves from our side
            int moveEval = 10 - opponentPossible;

            // see what moves we can make in the current board state (opponent doesn't get to move)
            if (board.TrySkipTurn())
            {
                // moves that we can make that aren't already defended
                int myPossible = board.GetLegalMoves().Where(m => !opponentCaptures.Contains(m.StartSquare)).Count();

                // eval is (number of moves we can make) - (number of moves opponent can make) on the current board
                moveEval = myPossible - opponentPossible;

                // eval scores are integers, so there's often multiple boards with the same eval
                // if this move is as good as previous moves, add it to the pool, if it's better, replace all previous
                if (moveEval >= eval)
                {
                    if (moveEval > eval)
                    {
                        best.Clear();
                        eval = moveEval;
                    }
                    best.Add(move);
                }
                board.UndoSkipTurn();
            }
            board.UndoMove(move);
        }

        // pick a random move out of the ones with the best score
        Random rnd = new();

        return best[rnd.Next(best.Count)];
    }
}