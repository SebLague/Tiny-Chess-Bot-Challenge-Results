namespace auto_Bot_194;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_194 : IChessBot
{
    // eval = the static evaluation of a position based solely on number of pieces and piece positions
    // phase = value in [0,1] that tells us where between the middlegame and endgame we are
    // https://www.chessprogramming.org/Tapered_Eval
    // keepZob = variable ensuring we don't overwrite the best move at depth 0
    // timeLeft = pretty self-explanatory; multiplied by a number to save on tokens
    // quiEval = a variable that shouldn't be static but is here to stop the code from yelling at you
    static double eval, phase, keepZob, timeLeft, quiEval;

    // saves on tokens by only declaring a variable once and then using it for multiple purposes
    static int id;

    // the base values of pieces
    // Pawn = 0; Knight = 1; Bishop = 2; Rook = 3; Queen = 4; King = 5
    // first 6 values are for middlegame, last 6 are for endgame
    static ulong[] baseVals = { 108, 580, 772, 1245, 2533, 0, 206, 854, 915, 1380, 2682, 0 };

    // lookup table for the best moves at each position/Zobrist Key for more efficient searches
    // https://www.chessprogramming.org/Transposition_Table ("Always Replace")
    static Dictionary<ulong, Move> alreadyExplored = new();

    // lookup table for pawn structures
    // since pawns rarely move, it is inefficient to reevaluate a pawn structure every time we encounter it
    // https://www.chessprogramming.org/Pawn_Hash_Table
    static Dictionary<ulong, double> pawnFormations;

    // keeps track of the last move at each level that caused a beta-cutoff
    // https://www.chessprogramming.org/Killer_Move
    static Move[] killerMoves;

    // saves on some tokens
    static Move nil = Move.NullMove;

    public Move Think(Board board, Timer timer)
    {
        // only clear our memory of past positions if we're starting to take up too much memory
        // (for safety, this limit is less than 1/5th of the max)
        if (alreadyExplored.Count > 1000000) alreadyExplored = new Dictionary<ulong, Move>();

        pawnFormations = new Dictionary<ulong, double>();
        killerMoves = new Move[50];

        // doing this instead of a for loop saves on 2 tokens
        id = 50;
        while (id-- > 0) killerMoves[id] = nil;

        Move bestKeep = nil;
        keepZob = board.ZobristKey;
        timeLeft = timer.MillisecondsRemaining * 0.046666667;

        // again, this saves on 1 token as opposed to a for loop
        int depth = 0;

        // iteratively deepen the search to search less positions for each additional ply
        // https://www.chessprogramming.org/Iterative_Deepening
        while (depth++ < 100)
        {
            EvaluatePosition(board);
            negamax(board, 0, depth, -999999999999, 999999999999, board.IsWhiteToMove ? 1 : -1, false, timer);

            // if we've run out of time, our search isn't complete and likely returns a horrible move, so we want to not use this move
            // I might've tried saving this move if I had more tokens, but the optimized time cut-off means that we rarely need to forcibly break out through this
            if (timer.MillisecondsElapsedThisTurn > timeLeft) break;

            bestKeep = alreadyExplored[board.ZobristKey];

            // if we've used up more than 1/5th of our allotted time for this search, odds are we'll cross the limit before finishing next search
            // this is because each additional ply multiplies the total number of searched positions by more than 5 on average
            // thus, we should cut off the search early and save the extra time for later moves
            if (timer.MillisecondsElapsedThisTurn > timeLeft * 0.2) break;
        }

        return bestKeep;
    }

    // evaluates a position solely on piece counts and piece positions and determines the game phase
    // we only do this right before a search because this is a fairly expensive computation
    // in all other cases, we only adjust the eval variable by the pieces involved in a move
    public void EvaluatePosition(Board board)
    {
        eval = 0.0;
        double temp = 0.0;
        foreach (PieceList pieceList in board.GetAllPieceLists()) foreach (Piece p in pieceList)
            {
                eval += EvaluatePiece(p);
                if (!p.IsPawn) temp += baseVals[(int)p.PieceType - 1];
            }

        // bounds phase between 0 and 1
        phase = (Math.Max(3500, Math.Min(15258, temp)) - 3500) * 0.00008504847;
    }

    // this is how I do my move ordering
    // previous best move in position first, then this layer's killer move, then captures, then all other moves
    // there's no ordering of captures
    // https://www.chessprogramming.org/Move_Ordering
    public Move[] Concat(Move[] a1, Move[] a2, Move best, Move killer)
    {
        Move[] a3 = new Move[a1.Length + a2.Length + 2];

        a3[0] = best;
        a3[1] = killer;
        Array.Copy(a1, 0, a3, 2, a1.Length);
        Array.Copy(a2, 0, a3, a1.Length + 2, a2.Length);

        return a3;
    }

    // negamax function, which is like the minimax function but simplified to use less tokens
    // https://www.chessprogramming.org/Negamax

    // INPUTS:
    // board = the current board
    // depth = what ply we're looking at with this iteration
    // depthLimit = how deep we'll go before returning
    // alpha = our alpha value, aka the best score we have found for this player
    // beta = our beta value, aka the worst score we have found for this player
    // white = 1 if it's white's turn and -1 if it's black's turn
    // quiesce = boolean to determine whether we're in quiescence search or regular search (used to save on tokens rather than create a new function)
    // timer = the initial timer object passed along to break out in case we run out of time
    public double negamax(Board board, int depth, int depthLimit, double alpha, double beta, int white, bool quiesce, Timer timer)
    {
        // if we're in a draw or checkmate, break out immediately
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return depth - 999999999;

        // if we're in check, we want to explore this line deeper, so increase the depth limit
        // however... if our depth limit is already too deep, odds are we're following some repetition of lines
        // or the line isn't making much progress, so we won't increase the limit
        if (board.IsInCheck() && depthLimit < 15) depthLimit++;

        // quiescence search done to make sure we don't make silly last moves before stopping the search at a branch
        // it only considers captures, or all legal moves in the case of being in check
        // https://www.chessprogramming.org/Quiescence_Search
        if (quiesce)
        {
            // get the evaluations of both pawn structures efficiently
            ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
            if (!pawnFormations.ContainsKey(whitePawns)) pawnFormations[whitePawns] = EvaluatePawns(whitePawns);
            ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);
            if (!pawnFormations.ContainsKey(blackPawns)) pawnFormations[blackPawns] = EvaluatePawns(blackPawns);

            // evaluation of the position = static evaluation * whose turn it is (for negamax) + bonus for tempo
            quiEval = (eval + pawnFormations[whitePawns] - pawnFormations[blackPawns]) * white + 28;

            // if this position is TOO GOOD, our opponent would never go for it and we should return beta
            if (quiEval >= beta) return beta;

            // if this position is significantly worse than our best position found, we're unlikely to improve it significantly, so return alpha
            // (this is technically a kind of delta pruning? except we have a static value for our "significant value")
            // https://www.chessprogramming.org/Delta_Pruning
            if (quiEval < alpha - 700) return alpha;

            // if this evaluation is better than alpha, update alpha
            if (alpha < quiEval) alpha = quiEval;
        }

        // if we're at our depth limit, either start our quiescence search or return our static evaluation depending on what phase of the search we're in
        if (depth == depthLimit) return quiesce ? quiEval : negamax(board, depth, depthLimit + 2, alpha, beta, white, true, timer);

        // set a variable to all our legal moves (or all checks if we're in quiescence) to reference, specifically to make sure our killer moves aren't illegal
        Move[] legalMoves = board.GetLegalMoves(quiesce && !board.IsInCheck());

        // loop through every move
        foreach (Move m in Concat(board.GetLegalMoves(true), legalMoves, alreadyExplored.ContainsKey(board.ZobristKey) ? alreadyExplored[board.ZobristKey] : nil, killerMoves[depth]))
        {
            // if we've taken too long, escape without looking at any more moves
            if (timer.MillisecondsElapsedThisTurn > timeLeft) return alpha;

            // make sure that the move is legal and we haven't explored it already
            id = Array.IndexOf(legalMoves, m);
            if (id >= 0 && !m.IsNull)
            {
                // avoid exploring the same move twice
                legalMoves[id] = nil;

                // by only looking at the delta/change of a certain move in terms of its starting and target squares,
                // we can cut down on a lot of unnecessary computations for pieces that don't move
                double delta = EvaluatePiece(board.GetPiece(m.StartSquare));
                if (m.IsCapture)
                {
                    id = m.TargetSquare.Index;

                    // adjust for en passants because the target square isn't the same as the square being captured
                    if (m.IsEnPassant) id += white * -8;

                    delta += EvaluatePiece(board.GetPiece(new Square(id)));
                }
                board.MakeMove(m);
                delta -= EvaluatePiece(board.GetPiece(m.TargetSquare));
                eval -= delta;

                double score = -negamax(board, depth + 1, depthLimit, -beta, -alpha, -white, quiesce, timer);

                board.UndoMove(m);
                eval += delta;

                // this move is too good, our opponent won't go for it
                if (score >= beta)
                {
                    // this move caused a beta cutoff, therefore we want to make it our killer move for this depth
                    killerMoves[depth] = m;
                    return beta;
                }

                // this move is the best we've found yet, make it our new best move for this position
                if (score > alpha)
                {
                    alpha = score;

                    // don't overwrite the best move at depth zero in the rare case that zobrist keys overlap
                    if (depth == 0 || board.ZobristKey != keepZob) alreadyExplored[board.ZobristKey] = m;
                }
            }
        }

        return alpha;
    }

    public double EvaluatePiece(Piece piece)
    {
        // these scales allow us to assign a greater range of values to our piece square table
        // (instead of being limited to 0..15)
        double[] scales = { 6.03, 17.27, 6.13, 3.27, 1.27, 12.66, 4.45, 9.27, 4.93, 2.20, 6.60, 13.20 };

        // this is my piece square table that assigns the value of a piece being on a certain square at a certain phase in the game
        // each square is given 2 bits of data
        // the values are taken from a former version of Stockfish and changed by a common value then scaled and rounded to fit within 0..15
        // https://www.chessprogramming.org/Piece-Square_Tables
        ulong[,] packedTables = {
            {0x0358305008132370,0x02339AA00477FDB0}, // pawns middlegame
            {0x08BAA8727ADCCB92,0x8CFEECA7AEFFECB7}, // knights middlegame
            {0x1667886096ADACA8,0x699CD8C759AEFB95}, // bishops middlegame
            {0x493162304D958663,0x9EB88975CFDA8AB8}, // rooks middlegame
            {0x2014722629CF8980,0x5C9DBEA02AA8A9D7}, // queen middlegame
            {0x0112265D0011247F,0x0001146A00011248}, // king middlegame
            {0x0C8532000A843120,0x096432500FA54530}, // pawns endgame
            {0x035676401569B854,0x559CCA969CDFEEC9}, // knights endgame
            {0x2558884038DBAB95,0x4BC9CB847CDFFEC9}, // bishops endgame
            {0xE8943902686A6220,0xFF392551C4A39352}, // rooks endgame
            {0x04678531379AB973,0x58ADDA846ACFFCB7}, // queen endgame
            {0x1377874049DDCA83,0x59EFDDA66AEFDDA6}  // king endgame
        };

        int row = piece.Square.Rank;
        // if the piece is black, flip its rank to align it with the piece square table
        if (!piece.IsWhite) row = 7 - row;

        int col = piece.Square.File;
        // if the piece is past file d, flip it back to be within the first 4 files
        // this is done to cut the piece square table's size in half
        if (col > 3) col = 7 - col;

        id = (int)piece.PieceType - 1;

        // a bit of saving on tokens
        int rep1 = row * 4 + col % 2 * 32;

        double mg = baseVals[id] + (packedTables[id, col / 2] >> rep1) % 16 * scales[id];
        id += 6;
        double eg = baseVals[id] + (packedTables[id, col / 2] >> rep1) % 16 * scales[id];

        return (mg * phase + eg * (1 - phase)) * (piece.IsWhite ? 1 : -1);
    }

    // evalutates a pawn structure based solely on the amount of isolated and doubled pawns
    public double EvaluatePawns(ulong pawns)
    {
        double eval = 0.0, isolationState = 0.0;

        // single token saved here by avoiding a for loop
        // we'll also check i=-1 for the logic for isolated pawns to work
        int i = 8;
        while (i-- >= 0)
        {
            // this gives the amount of pawns on a file
            id = i < 0 ? 0 : BitboardHelper.GetNumberOfSetBits(pawns & (0x8080808080808080 >> i));

            // if there are more than one pawn on a file, we have doubled pawns
            // this is bad, so we want to apply a penalty
            eval -= (Math.Max(id, 1) - 1) * 11;

            // if there are pawns on this file and the last file had no pawns, keep track of the number of pawns on the file
            if (id > 0 && isolationState == 0.0) isolationState = id;
            // otherwise, if there are no pawns on the file...
            else if (id == 0)
            {
                // if there were pawns on the last file, apply a penalty based on how many pawns there were
                if (isolationState > 0.0) eval -= 10 * isolationState - 5;

                // and mark that there were no pawns on the file
                isolationState = 0.0;
            }
            // otherwise, mark that there were pawns on the file but they weren't isolated
            else isolationState = -1.0;
        }

        return eval;
    }
}