namespace auto_Bot_246;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

// https://github.com/aedans/Chess-Challenge
//
// James, a simple chess bot that heavily favors search enhancements over
// evaluation enhancements.
//
// The evaluation function only considers piece types and piece positions using
// the simplified evaluation function described on the chess programming wiki. 
//
// The search implements iterative deepening of an alpha/beta search using an 
// aspiration window extended with quiescence search and a transposition table. 
//
// Moves are evaluated in the order [previous best move, parent killer move, 
// promotion or capture, other move]. 
// 
// Time is allocated as a portion of the remaining time based on the number of 
// aspiration failures in a position.
//
// I wanted to do something a bit more unique, but ran into 3 major problems:
// (1) With no baseline to test against it was very difficult to tell how well
//     an experimental bot or experimental changes performed.
// (2) Since testing was limited to playing against previous versions of itself,
//     it was difficult to set up specific, repeatable tests.
// (3) I'm not very good at chess.
//
// Still, I'm pretty happy with the final result. This taught me a lot about 
// what to do (and a lot more about what not to do) so I might try implementing 
// some of the ideas I had in a more structured framework as a project in the 
// future. Hopefully James does well.
//
// - aedans
public class Bot_246 : IChessBot
{
    // Piece values, taken from the simplified evaluation function, stored as 
    // centipawns for each piece. King is omitted.
    // https://www.chessprogramming.org/Simplified_Evaluation_Function#Piece-Square_Tables
    readonly int[] pieceValues = { 100, 320, 330, 500, 900 };

    // A compressed way of representing the value of a piece on any square, using 
    // each nibble of a ulong to store the value as (value + 8) / 5, which can
    // then be retrieved by solving for value and using bitshifts to retrieve it.
    // Values are taken from the simplified evaluation function. King is omitted.
    readonly ulong[][] pieceEvalboards = new ulong[][]{
    new ulong[] { 0x888888889aa44aa9, 0x97688679888cc888, 0x99adda99aaceecaa, 0xffffffffffffffff, },
    new ulong[] { 0x0022220004899840, 0x29abba9228bccb82, 0x29bccb9228abba82, 0x0488884000222200, },
    new ulong[] { 0x4666666469888896, 0x6aaaaaa668aaaa86, 0x699aa996689aa986, 0x6888888646666664, },
    new ulong[] { 0x8889988878888887, 0x7888888778888887, 0x7888888778888887, 0x9aaaaaa988888888, },
    new ulong[] { 0x4667766468888986, 0x6899999678999988, 0x7899998768999986, 0x6888888646677664, },
  };

    // The transposition table, containing in order:
    // - The depth of the last evaluation
    // - The value when last evaluated
    // - The list of best moves when last evaluated
    // - The list of killer moves when last evaluated
    // - The cutoff of the last evaluation
    Dictionary<ulong, (int, int, List<Move>, List<Move>, int)> evaluations = new();

    // James thinks for 1/100th of the remaining time plus increment by default,
    // but thinks for 10% longer whenever an aspiration search fails. The 
    // transposition table persists between calls, so endgames can reach a higher
    // depth despite the reduced time.
    public Move Think(Board board, Timer timer)
    {
        var depth = 1;
        var bestEval = 0;
        Move bestMove = Move.NullMove;

        // Alpha and beta offset for implementing an aspiration window. The first
        // search has no aspiration window, so the offset is maximized.
        var alphaOffset = 99999;
        var betaOffset = 99999;

        // The inverse of time per move, 100 means 1/100th of remaining time.
        var timeAlloc = 100;
        var isTime = false;

        while (!isTime)
        {
            var alpha = bestEval - alphaOffset;
            var beta = bestEval + betaOffset;
            var eval = EvalMove(depth == 1 ? null : timer, board, depth, alpha, beta, new List<Move>(), timeAlloc, ref isTime, out Move move);

            // If we find a mate for either side, stop evaluating immediately. Since 
            // the offset is equivalent to a mate, it results in an infinite loop.
            if (Math.Abs(eval) == 99999)
            {
                bestEval = eval;
                bestMove = move;
                break;
            }

            // Expand the alpha or beta cutoffs if the aspiration search failed and 
            // increase the time to evaluate slighly.
            if (eval <= alpha)
            {
                alphaOffset *= 4;
                timeAlloc = (int)(timeAlloc * .9);
            }
            else if (eval >= beta)
            {
                betaOffset *= 4;
                timeAlloc = (int)(timeAlloc * .9);
            }
            else if (move != Move.NullMove)
            {
                bestEval = eval;
                bestMove = move;
                depth++;
                alphaOffset = 25;
                betaOffset = 25;
            }
        }

        DivertedConsole.Write("Depth: " + depth + " Eval: " + bestEval + " " + bestMove + " Time: " + timer.MillisecondsElapsedThisTurn);
        return bestMove;
    }

    // A combination of a top level evaluation function that returns the best 
    // move, an internal evaluation function that returns the score, and a 
    // quiescence search that tries to only evaluate quiet positions.
    public int EvalMove(Timer? timer, Board board, int depth, int alpha, int beta, List<Move> parentKillers, int timeAlloc, ref bool isTime, out Move bestMove)
    {
        bestMove = Move.NullMove;

        if (board.IsInCheckmate())
        {
            return -99999;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        // Using GetLegalMovesNonAlloc results in between 30%-50% more positions
        // evaluated, but costs a lot of brain space since we can't use Linq. Worth.
        var legalMoves = new Span<Move>(new Move[256]);
        board.GetLegalMovesNonAlloc(ref legalMoves);

        // If there are any non-pawn captures, we enter a quiescence search with 
        // depth 2 where we only look at captures and checks.
        var hasCaptures = false;
        foreach (var move in legalMoves)
        {
            if (move.IsCapture && move.CapturePieceType != PieceType.Pawn)
            {
                hasCaptures = true;
                break;
            }
        }

        // We also extend the search in check. Otherwise, we take the difference of
        // the piece evaluations bounded by alpha/beta.
        if ((depth <= 0 && !hasCaptures && !board.IsInCheck()) || depth <= -2)
        {
            return Math.Max(alpha, Math.Min(beta, PieceEvals(board, board.IsWhiteToMove) - PieceEvals(board, !board.IsWhiteToMove)));
        }

        var allMoves = new List<Move>();
        var childKillers = new List<Move>();

        if (evaluations.ContainsKey(board.ZobristKey))
        {
            var (evalDepth, eval, moves, killers, evalFlag) = evaluations[board.ZobristKey];

            // If the last evaluation was at an equal or greater depth to this one,
            // re-use its evaluation. If that evaluation failed due to an alpha or
            // beta cutoff, adjust alpha or beta accordingly.
            if (evalDepth >= depth && moves.Count > 0)
            {
                if (evalFlag == -1)
                {
                    alpha = Math.Max(alpha, eval);
                }

                if (evalFlag == 1)
                {
                    beta = Math.Max(beta, eval);
                }

                // We can return the evaluation immediately if the last evaluation 
                // wasn't cut off or there are no evaluations between alpha/beta.
                if (evalFlag == 0 || alpha >= beta)
                {
                    bestMove = moves.First();
                    return eval;
                }
            }

            childKillers.AddRange(killers);

            // Prioritize looking at the best moves in the last evaluation of the
            // position if the search isn't a quiescence search.
            if (depth > 0)
            {
                foreach (var move in moves)
                {
                    if (legalMoves.Contains(move))
                    {
                        allMoves.Add(move);
                    }
                }
            }
        }

        // Prioritize looking at moves that killed the evaluation in the parent 
        // node if the search isn't in a quiescence search.
        if (depth > 0)
        {
            foreach (var move in parentKillers)
            {
                if (legalMoves.Contains(move))
                {
                    allMoves.Add(move);
                }
            }
        }

        // Prioritize looking at moves that promote or capture.
        foreach (var move in legalMoves)
        {
            if (move.IsPromotion || move.IsCapture)
            {
                allMoves.Add(move);
            }
        }

        // Add all other moves if this isn't a quiescence search or if there are no
        // legal non-promotion non-capture moves, e.g. in check.
        if (depth > 0 || allMoves.Count == 0)
        {
            foreach (var move in legalMoves)
            {
                allMoves.Add(move);
            }
        }

        bestMove = Move.NullMove;
        isTime = timer != null && timer.MillisecondsElapsedThisTurn > (timer.MillisecondsRemaining / (timeAlloc + 3)) + timer.IncrementMilliseconds;

        var analyzedMoves = new HashSet<Move>();
        var bestMoves = new List<Move>() { };
        var flag = 1;
        foreach (var move in allMoves)
        {
            // Return immediately if time is up.
            if (isTime)
            {
                bestMove = Move.NullMove;
                return 0;
            }

            // Cache analyzed moves so that we don't evaluate duplicate moves added
            // earlier in the function. This is faster and more brain-efficient than
            // finding and reordering the array.
            if (analyzedMoves.Contains(move))
            {
                continue;
            }
            else
            {
                analyzedMoves.Add(move);
            }

            board.MakeMove(move);

            var eval = -EvalMove(timer, board, depth - 1, -beta, -alpha, childKillers, timeAlloc, ref isTime, out Move _);

            board.UndoMove(move);

            // Check if this is a best move, then add it to the list of best moves.
            if (eval > alpha)
            {
                alpha = eval;
                flag = 0;
                bestMove = move;
                bestMoves.Insert(0, move);

                // Check if this move is a refutation to the last move, then add it to
                // the list of killer moves of the last move and stop searching.
                if (eval >= beta)
                {
                    parentKillers.Add(move);
                    alpha = beta;
                    flag = -1;
                    break;
                }
            }
        }

        // If we weren't able to find a move in alpha/beta and there are legal 
        // moves, use our best guess.
        if (bestMove.IsNull && allMoves.Count > 0)
        {
            bestMove = allMoves[0];
            bestMoves.Add(allMoves[0]);
        }

        evaluations[board.ZobristKey] = (depth, alpha, bestMoves, childKillers, flag);

        return alpha;
    }

    // Sums the value for each piece.
    public int PieceEvals(Board board, bool white)
    {
        return new PieceType[] { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen }
          .Sum(type => board.GetPieceList(type, white).Sum(piece => GetPieceEval(piece)));
    }

    // Gets the value of a piece by taking the constant piece value and adjusting 
    // depending on the position using the evaluation board.
    public int GetPieceEval(Piece piece)
    {
        var index = piece.IsWhite ? piece.Square.Index : 63 - piece.Square.Index;
        var offset = 60 - index % 16 * 4;
        var value = 5 * ((int)((pieceEvalboards[(int)piece.PieceType - 1][index / 16] & (0xful << offset)) >> offset) - 8);
        return pieceValues[(int)piece.PieceType - 1] + value;
    }
}