namespace auto_Bot_551;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_551 : IChessBot
{

    // I can add stuff in the constructor here
    public Move bestMove;
    public int maxDepth = 100;
    public bool searchCancelled;

    public Move bestMoveThisIteration;
    public int bestEvalThisIteration;

    // stores <Zobrist hash - [eval, depth]>
    public Dictionary<ulong, int[]> transpositionTable = new Dictionary<ulong, int[]>();
    // TT size should be approx 128 Mb
    public int maxTTSize = 1024 * 1024 * 128 / (sizeof(ulong) + sizeof(int));


    public Move Think(Board board, Timer timer)
    {
        searchCancelled = false;
        startSearch(board, timer);

        return bestMove;
    }


    public void startSearch(Board board, Timer timer)
    {
        int bestEval = -100000;

        for (int searchDepth = 1; searchDepth < int.MaxValue; searchDepth++)
        {
            List<Move> moves = board.GetLegalMoves().ToList();
            Move bestMoveThisIteration = moves[0];
            int bestEvalThisIteration = bestEval;

            // test all legal moves to see which one is best

            for (int i = 0; i < moves.Count; i++)
            {

                board.MakeMove(moves[i]);

                // need to add - sign to make sure the eval remains accurate
                // since it is not the bot's turn

                int eval = -search(searchDepth, -10000, 10000, board, timer);

                if ((eval > bestEvalThisIteration) && !searchCancelled)
                {
                    bestEvalThisIteration = eval;
                    bestMoveThisIteration = moves[i];
                }
                board.UndoMove(moves[i]);
            }

            if (bestEvalThisIteration > bestEval)
            {
                bestMove = bestMoveThisIteration;
                bestEval = bestEvalThisIteration;
            }

            if (searchCancelled)
            {
                break;
            }
        }
        return;
    }


    // MiniMax and AlphaBeta pruning
    public int search(int depth, int alpha, int beta, Board board, Timer timer)
    {
        checkTime(timer);

        // very bad
        int negativeInfinity = -100000;

        // check TT first
        int[] result;
        int evaluation;

        if (transpositionTable.TryGetValue(board.ZobristKey, out result))
        {
            // check the depth of the TT result
            if (result[1] >= depth)
            {
                return result[0];
            }
        }

        if (searchCancelled) return 0;

        // evaluate the board when we get to the highest considered depth
        if (depth == 0)
        {
            // this is so that our evaluation is accurate even if captures are made
            return searchAllCaptures(alpha, beta, board);
        }

        // get all the possible moves at considered board state
        List<Move> moves = board.GetLegalMoves().ToList();
        // order the moves
        orderMoves(moves, board, bestMoveThisIteration);

        if (moves.Count == 0)
        {
            if (board.IsInCheck())
            {
                // this is Checkmate
                return negativeInfinity;
            }
            // stalemate
            return 0;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            evaluation = -search(depth - 1, -beta, -alpha, board, timer);
            board.UndoMove(move);


            if (evaluation >= beta)
            {
                // we update the TT here
                updateTT(beta, board, depth);
                // Move too good, opponent will avoid it
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);
        }

        // we update the TT here
        updateTT(alpha, board, depth);
        return alpha;
    }

    // just to check if there are any captures on the board
    public int searchAllCaptures(int alpha, int beta, Board board)
    {

        int evaluation = evaluate(board);
        if (evaluation >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, evaluation);

        List<Move> captureMoves = board.GetLegalMoves(true).ToList();
        orderMoves(captureMoves, board, bestMoveThisIteration);

        foreach (Move capture in captureMoves)
        {
            board.MakeMove(capture);
            evaluation = -searchAllCaptures(-beta, -alpha, board);
            board.UndoMove(capture);

            if (evaluation >= beta)
            {
                // Move too good, opponent will avoid it
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);
        }
        return alpha;
    }

    // reorder moves to make AlphaBeta pruning more efficient
    public void orderMoves(List<Move> moves, Board board, Move priorityMove)
    {
        List<int> movePriorities = new List<int>();

        foreach (Move move in moves)
        {
            // smaller priority is checked sooner
            int priority = 100000;

            if (move.IsCastles) priority -= 2000;
            if (move.IsPromotion) priority -= 1000;
            if (move.IsCapture) priority -= 500;

            board.MakeMove(move);

            if (board.IsInCheck()) priority -= 2000;
            if (board.IsInCheckmate()) priority -= 10000;

            board.UndoMove(move);

            // give the priority list the best priority
            if (move == priorityMove) priority -= 100000;

            movePriorities.Add(priority);
        }

        // order the lists together (this orders from low to high)
        var orderedZip = movePriorities.Zip(moves, (x, y) => new { x, y })
                              .OrderBy(pair => pair.x)
                              .ToList();

        movePriorities = orderedZip.Select(pair => pair.x).ToList();
        moves = orderedZip.Select(pair => pair.y).ToList();

        return;
    }

    public int evaluate(Board board)
    {
        int eval = 0;
        eval += evaluateMaterial(board);
        eval += developMinorPieces(board);

        return eval;
    }



    public int evaluateMaterial(Board board)
    {
        // Get the material difference
        // Probably very inefficient, bitboards may be better?

        int eval = 0;

        PieceList[] pl = board.GetAllPieceLists();

        // scores as white-black

        eval += (pl[0].Count - pl[6].Count) * 100;
        eval += (pl[1].Count - pl[7].Count) + (pl[2].Count - pl[8].Count) * 300;
        eval += (pl[3].Count - pl[9].Count) * 500;
        eval += (pl[4].Count - pl[10].Count) * 900;

        // how many moves each player has from this position
        int moves = board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        int opponentMoves = board.GetLegalMoves().Length;
        board.UndoSkipTurn();
        eval += moves - opponentMoves;

        // check who is to move to make sure the material count is
        // in the right direction
        if (!board.IsWhiteToMove) eval = -eval;

        return eval;
    }

    // gives a penalty if certain squares contain minor pieces, to encourage development
    // doesnt seem to work as well as intended
    public int developMinorPieces(Board board)
    {
        int eval = 0;
        int whiteToMove = 0;
        if (!board.IsWhiteToMove) whiteToMove = 56;

        Square[] minorsSquares = { new Square(whiteToMove + 1), new Square(whiteToMove + 2),
                new Square(whiteToMove + 5), new Square(whiteToMove + 6) };

        foreach (Square s in minorsSquares)
        {
            if (board.GetPiece(s).IsKnight) eval -= 50;
            if (board.GetPiece(s).IsBishop) eval -= 80;
        }
        return eval;
    }


    // update the TT
    public void updateTT(int eval, Board board, int depth)
    {
        ulong zobrist = board.ZobristKey;
        int[] entry = { eval, depth };
        int[] result;

        if (transpositionTable.TryGetValue(board.ZobristKey, out result))
        {
            // if the depth stored for this position is too low, update the TT
            if (result[1] < depth) { transpositionTable.Remove(zobrist); }
            else { return; }
        }

        if (transpositionTable.Count == maxTTSize)
        {
            transpositionTable.Remove(transpositionTable.ElementAt(0).Key);
        }
        transpositionTable.Add(zobrist, entry);
    }

    // controls how long the bot takes to think
    public void checkTime(Timer timer)
    {
        int n = 5000;
        if (timer.MillisecondsRemaining < 30000)
        {
            n = 200;
        }
        if (timer.MillisecondsElapsedThisTurn > n) { searchCancelled = true; }

        return;
    }

}