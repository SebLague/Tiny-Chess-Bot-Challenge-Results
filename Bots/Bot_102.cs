namespace auto_Bot_102;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

/* =====================================================================================================================
    This bot is a single-player-bot. This means that it plays the game like the opponent won't respond. Like I am 
    playing chess against no-one. The only thing I am taking into account regarding my opponent is not allowing easy 
    captures or checkmates. Other than that, I am always looking to check. Whichever move gets me there fastest.

    The joke is that, even though I take very little regard towards how the opponent might respond, I still net wins.
===================================================================================================================== */
public class Bot_102 : IChessBot
{
    // Points to gain for causing a draw
    int drawValue = 500;
    // Points to gain for losing a piece
    int[] pieceLossValues = { 0, 1, 2, 2, 3, 4, 1000 };
    // Points to lose for capturing a piece
    int[] pieceGainValues = { 0, 2, 3, 3, 5, 7, 1000 };
    // Points to gain for repetition
    int repititionValue = 2;

    // Square I last moved a piece to
    Square lastMoveTo;
    // Random number generator
    Random rng = new Random();

    public Move Think(Board board, Timer timer)
    {
        // Start with getting all the legal moves I can do now (randomized, so that I have a random chance of finding a
        // good move sooner)
        Move[] moves = randomizeMoves(board.GetLegalMoves());

        // As a wise man once said: 'Always play checkmate in one'
        foreach (Move move in moves) if (MoveIsCheckmate(board, move)) return move;

        /* -------------------------------------------------------------------------------------------------------------
            If I can't checkmate, I'm going to evaluate all my moves. I will do this by scoring (evaluating) each move
            based on the following factors:
            - Fruitfulness: Within how many moves can this branch reach an opponent state of check?* (base value)
            - Draw: Does this move allow the opponent to draw the game? (add points)
            - Checkmate: Does this move allow the opponent to checkmate me after? (add points)
            - Loss: Does this move allow an opponent to immediately take a piece? (add points)
            - Gain: Does this move take an opponent piece? (subtract points)
            - Repetition: Does this move move the same piece twice in a row? (add points)
            The lowest scoring moves will be stored in a list.
            *I'm doing evaluations on a single-player basis, meaning I'm not letting the opponent respond in-between my 
            moves. This way of thinking creates a sort of rough plan, an idea of what I want to do (like 'move up a 
            rook'.)
        ------------------------------------------------------------------------------------------------------------- */

        // Guestimate a good search depth limit based on how many legal moves there are currently
        int depthLimit = (int)Math.Round(5.2 - moves.Length * 0.05);
        // Prepare a list for keeping track of all my best scoring moves along with an int for keeping track of how low 
        // it scored
        List<Move> bestMoves = new List<Move> { };
        int lowestDepth = int.MaxValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            // Fruitfulness
            int score = getLowestDepthToCheck(board, 1, lowestDepth <= depthLimit ? lowestDepth : depthLimit);
            // Opponent immediate responses
            foreach (Move m in board.GetLegalMoves())
            {
                board.MakeMove(m);
                // Draw
                if (board.IsDraw()) score += drawValue;
                // Checmate
                if (board.IsInCheckmate()) score += pieceLossValues[6];
                board.UndoMove(m);
            }
            // Loss
            foreach (Move m in board.GetLegalMoves(true)) score += pieceLossValues[(int)m.CapturePieceType];
            // Gain
            score -= pieceGainValues[(int)move.CapturePieceType];
            // Repetition
            if (move.StartSquare == lastMoveTo) score += repititionValue;
            board.UndoMove(move);

            // If the resulting score is lower than our previous lowest, forget everything and make this the one new 
            // best move. Otherwise, if the move is equally good as what we've seen before, add it to the list
            if (score < lowestDepth)
            {
                lowestDepth = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            // Check if the move is equally good as ones we've seen
            else if (score == lowestDepth) bestMoves.Add(move);
        }

        /* -------------------------------------------------------------------------------------------------------------
            Pick random among best moves. This creates 2-fold-randomization, which is unnecessary. In the future I could 
            implement a better strategy for selecting the bestest of best moves.
        ------------------------------------------------------------------------------------------------------------- */
        Move moveToPlay = bestMoves[rng.Next(bestMoves.Count)];
        lastMoveTo = moveToPlay.TargetSquare;
        return moveToPlay;
    }

    /* =================================================================================================================
        Keep skipping opponent turns and test mine. Returns the lowest amount of turns needed to check. If I couldn't 
        check before the depth limit, return the value for losing a king - 100 (this is a bad move, but better than
        losing a king.) When I cause a draw, return 500 (again, bad move, but beats losing.)
    ================================================================================================================= */
    int getLowestDepthToCheck(Board board, int depth, int depthLimit)
    {
        if (depth >= depthLimit) return pieceLossValues[6] - 100; // Base case: Depth limit was reached
        if (board.IsDraw()) return drawValue; // Base case: I've caused a draw
        if (!board.TrySkipTurn()) return depth; // Base case: Opponent is in check

        // Recursion
        int lowestDepth = int.MaxValue;
        Move[] moves = randomizeMoves(board.GetLegalMoves());

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int result = getLowestDepthToCheck(board, depth + 1, depthLimit);
            board.UndoMove(move);

            if (result < lowestDepth) lowestDepth = result;
        }

        board.UndoSkipTurn();

        return lowestDepth;
    }

    /* =================================================================================================================
        Randomize moves array
    ================================================================================================================= */
    Move[] randomizeMoves(Move[] moves)
    {
        for (int i = moves.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            Move move = moves[i];
            moves[i] = moves[j];
            moves[j] = move;
        }
        return moves;
    }

    /* =================================================================================================================
        Test if this move gives checkmate. Look familiar?
    ================================================================================================================= */
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}