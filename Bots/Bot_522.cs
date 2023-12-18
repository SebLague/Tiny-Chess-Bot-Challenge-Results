namespace auto_Bot_522;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


// AlphaBeta + Complex Eval + OrderMoves + Span memory alloc (in eval next captures and initial) + simple time management
public class Bot_522 : IChessBot
{
    int[] piecesValue = { 0, 10, 30, 30, 50, 90, 900 };
    bool amIWhite;

    private Dictionary<Move, List<int>> history = new();
    private Move lastMove = Move.NullMove;
    private int lastEval;

    public Move Think(Board board, Timer timer)
    {
        // Stopwatch stopwatch = new();
        // stopwatch.Start();

        //////////////////////////////////////////////////

        Span<Move> moves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref moves);

        amIWhite = board.IsWhiteToMove;

        var boardEval = BoardEval(board);

        if (lastMove != Move.NullMove)
            if (history.TryGetValue(lastMove, out var element))
            {
                element.Add(lastEval - boardEval);
            }
            else
            {
                history.Add(lastMove, new List<int> { lastEval - boardEval });
            }

        Move bestMove = moves[new Random().Next(moves.Length)];
        int bestScore = amIWhite ? int.MinValue : int.MaxValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            var eval = AlphaBeta(timer.MillisecondsRemaining > 3000 ? 3 : 2, !amIWhite, -1000, 1000, board);
            board.UndoMove(move);


            if (amIWhite)
            {
                if (eval < bestScore) continue;
                bestScore = eval;
                bestMove = move;
            }
            else
            {
                if (eval > bestScore) continue;
                bestScore = eval;
                bestMove = move;
            }
        }

        lastMove = bestMove;
        lastEval = boardEval;

        // stopwatch.Stop();

        history.TryGetValue(bestMove, out var stats);

        // DivertedConsole.Write(amIWhite ? "---White---" : "---Black---");
        // DivertedConsole.Write("Stats of best " + (stats?.Average() ?? bestScore));
        // DivertedConsole.Write("Best " + bestMove + " with score of " + bestScore);
        // DivertedConsole.Write("Elapsed time" + stopwatch.ElapsedMilliseconds);
        // DivertedConsole.Write("--------------------------------------------");

        return bestMove;
    }

    /// <summary>
    /// A move evaluation function using the AlphaBeta algorithm
    /// </summary>
    /// <param name="depth">Depth of the tree to explore</param>
    /// <param name="maximizingPlayer">If the evaluation is maximized in this recursion (changes in the next recursion)</param>
    /// <param name="alpha">Set initially to a very low value (ex: -inf ^_^)</param>
    /// <param name="beta">Set initially to a very high value (ex: inf ^_^)</param>
    /// <param name="studiedBoard">The board on which the move is played</param>
    /// <returns></returns>
    private int AlphaBeta(int depth, bool maximizingPlayer, int alpha, int beta,
        Board studiedBoard)
    {

        var moves = studiedBoard.GetLegalMoves();

        // Return final evaluation if this node is at the end of a branch or the max depth has been reached
        if (depth == 0 || moves.Length == 0)
        {
            return BoardEval(studiedBoard);
        }

        // Maximal evaluation
        if (maximizingPlayer)
        {
            var value = Int32.MinValue;
            foreach (var move in maximizingPlayer == amIWhite ? OrderMoves(history, moves, false) : moves)
            {
                studiedBoard.MakeMove(move);
                value = Math.Max(value,
                    AlphaBeta(depth - 1, !maximizingPlayer, alpha, beta, studiedBoard));
                studiedBoard.UndoMove(move);

                if (value > beta)
                {
                    break;
                }

                alpha = Math.Max(alpha, value);
            }

            return value;
        }
        // Minimize evaluation
        else
        {
            var value = Int32.MaxValue;
            foreach (var move in maximizingPlayer == amIWhite ? OrderMoves(history, moves, false) : moves)
            {
                studiedBoard.MakeMove(move);
                value = Math.Min(value,
                    AlphaBeta(depth - 1, !maximizingPlayer, alpha, beta, studiedBoard));
                studiedBoard.UndoMove(move);

                if (value < alpha)
                {
                    break;
                }

                beta = Math.Min(beta, value);
            }

            return value;
        }
    }

    /// <summary>
    /// Method to evaluate the status of the board (positive => white are winning || negative => black are winning).
    /// Checkmates count as a king (ex: white checkmated = board_evaluation - white_king).
    /// </summary>
    /// <param name="board">The board to evaluate</param>
    /// <param name="evalCheckMate">If the checkmates should be checked</param>
    /// <param name="evalNextCaptures">If the next captures should be checked</param>
    /// <returns></returns>
    private int BoardEval(Board board, bool evalCheckMate = true, bool evalNextCaptures = true)
    {
        int multiplier = amIWhite ? 1 : -1;

        var pieceLists = board.GetAllPieceLists();

        int total = 0;
        foreach (PieceList pieceList in pieceLists)
        {
            total += piecesValue[(int)pieceList.TypeOfPieceInList] * pieceList.Count *
                     (pieceList.IsWhitePieceList ? 1 : -1);
        }

        if (evalNextCaptures)
        {
            Span<Move> moves = stackalloc Move[128];
            board.GetLegalMovesNonAlloc(ref moves, true);

            foreach (var move in moves)
            {
                total += piecesValue[(int)move.CapturePieceType] * (board.GetPiece(move.StartSquare).IsWhite ? 1 : -1) /
                         2;
            }
        }

        if (evalCheckMate && board.IsInCheckmate())
        {
            total += board.IsWhiteToMove != amIWhite
                ? piecesValue[(int)PieceType.King] * multiplier
                : -piecesValue[(int)PieceType.King] * multiplier;
        }

        return total;
    }

    Move[] OrderMoves(Dictionary<Move, List<int>> history, Move[] moves, bool ascending = true)
    {
        var sorter = (Move move) => history.TryGetValue(move, out var stats) ? stats.Average() : ascending ? int.MaxValue : int.MinValue;

        if (history.Count > 0)
        {
            if (ascending)
                return moves.ToList()
                    .OrderBy(sorter)
                    .ToArray();
            return moves.ToList()
                .OrderByDescending(sorter)
                .ToArray();
        }

        return moves;
    }
}