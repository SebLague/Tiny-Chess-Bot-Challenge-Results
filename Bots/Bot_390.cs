namespace auto_Bot_390;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

internal class Bot_390 : IChessBot
{
    Move c_BestMove, // Best move in Search
         c_BestMoveThisIteration; // Best move found on this iteration (MinMax)

    int c_intBestEval,  // Best eval in Search
        c_intBestEvalThisIteration, // Best eval for this iteration (MinMax)
        c_intThisMoveTimeAllowed; // The time allowed to think for the current move  

    Timer c_Timer;
    Board c_Board;

    int[] c_PieceValues = { 100, 300, 300, 500, 900, 9000 };
    ulong[][] c_PieceMoveWeights;

    private struct TranspositionEntry
    {
        public int score,
                   depth,
                   nodeType;
        public Move move;
    }

    // transposition table to store the alpha beta values for each position
    Dictionary<ulong, TranspositionEntry> c_colTranspositionTable = new();

    public Bot_390()
    {

        #region "Weighted Move Bit boards"

        // Note can't use negative numbers for uLong values, so added 50 to them that gets subtracted when using
        c_PieceMoveWeights = new[]
        {
        // myPawns 
        new ulong[]{ 6144UL, 30UL,
            2359296UL, 40UL,
            4325376UL, 45UL,
            18374686483548733695UL, 50UL,
            837527109888UL, 55UL,
            214559386265088UL, 60UL,
            39582821253120UL, 70UL,
            103079215104UL, 75UL,
            26388279066624UL, 80UL,
            71776119061217280UL, 100UL},
        // Knights
        new []{ 9295429630892703873UL, 0UL,
            4792111478498951490UL, 10UL,
            4323598035499155516UL, 20UL,
            18577348462920192UL, 30UL,
            16961067477378048UL, 50UL,
            283472173056UL, 55UL,
            39582420959232UL, 60UL,
            26543503441920UL, 65UL,
            103481868288UL, 70UL },
        // Bishops
        new []{ 9295429630892703873UL, 30UL,
            9115709513998107006UL, 40UL,
            35538415940287488UL, 50UL,
            40020505281024UL, 55UL,
            26492373172224UL, 60UL },
        // Rooks 
        new []{ 18374825561424821991UL, 50UL,
            36310271995674648UL, 55UL,
            35465847065542656UL, 60UL,
            142393223512320UL, 45UL },
        // Queens 
        new []{ 9295429630892703873UL, 30UL,
            7386326700872794470UL, 40UL,
            1729382810977828888UL, 45UL,
            35538701555752448UL, 50UL,
            66229410471936UL, 55UL },
        // King Middle Game 
        new []{ 1736164147709607936UL, 0UL,
            7378697628168486912UL, 10UL,
            9331882295650418688UL, 20UL,
            2172518400UL, 30UL,
            8454144UL, 40UL,
            15384UL, 50UL,
            36UL, 70UL,
            50049UL, 80UL,
            66UL, 90UL }
        };
        #endregion
    }

    public Move Think(Board board, Timer timer)
    {
        c_Board = board;
        c_Timer = timer;

        // Iterative Deepening
        IterativeDeepening(); // move param depth 10 in to function to save a few tokens

        // Return best move
        return c_BestMove;
    }

    private void GetOrderdEvaluatedLegalMoves(ref Span<Move> moves, bool blnCapturesOnly)
    {

        c_Board.GetLegalMovesNonAlloc(ref moves, blnCapturesOnly);

        // Sort moves to possible best moves first
        moves.Sort((a, b) => AddWeightToMove(b, blnCapturesOnly) - AddWeightToMove(a, blnCapturesOnly));

    }

    private int AddWeightToMove(Move move, bool blnCapturesOnly)
    {

        int intScore = 0;
        c_intPieceType_SquareWeightByPiece = (int)move.MovePieceType - 1;

        if (move.IsCapture)
            // If a capture order capturing good pieces with bad pieces first ie (queen with a pawn)
            intScore += 10 * (c_PieceValues[(int)move.CapturePieceType - 1] - c_PieceValues[c_intPieceType_SquareWeightByPiece]);

        if (!blnCapturesOnly)
            // Add new square score, minus the old square score
            intScore += SquareWeightByPiece(move.TargetSquare.Index, c_Board.IsWhiteToMove) - SquareWeightByPiece(move.StartSquare.Index, c_Board.IsWhiteToMove);

        return intScore;

    }

    int c_intPieceType_SquareWeightByPiece; // *** save tokens by setting this rather than passing it into SquareWeightByPiece


    // Get the square weight of a square for a specific piece and color (shared by Order moves and eval)
    private int SquareWeightByPiece(int SquareIndex, bool blnIsWhite)
    {

        ulong bit = 1UL << (blnIsWhite ? SquareIndex : 63 - SquareIndex);

        // Loop through bitBoards for pieceType to get its weight value
        for (int i = 0; i < c_PieceMoveWeights[c_intPieceType_SquareWeightByPiece].Length; i += 2)
        {
            if ((c_PieceMoveWeights[c_intPieceType_SquareWeightByPiece][i] & bit) == bit)
                return (int)c_PieceMoveWeights[c_intPieceType_SquareWeightByPiece][i + 1] - 50;
        }

        return 0;
    }

    private int GetEval()
    {

        int intEval = 0;

        // Checkmate Stalemate
        intEval += c_Board.IsInCheckmate() ? -5000 : c_Board.IsInStalemate() ? 5000 : 0;

        foreach (PieceList myPieceList in c_Board.GetAllPieceLists())
        {
            int intWhite = (myPieceList.IsWhitePieceList ? 1 : -1);
            c_intPieceType_SquareWeightByPiece = (int)myPieceList.TypeOfPieceInList - 1;

            // Material score
            intEval += intWhite * myPieceList.Count * c_PieceValues[c_intPieceType_SquareWeightByPiece];

            // piece square position weight
            foreach (Piece myPiece in myPieceList)
                intEval += intWhite * SquareWeightByPiece(myPiece.Square.Index, myPiece.IsWhite);

        }

        return c_Board.IsWhiteToMove ? intEval : -intEval;
    }

    private void IterativeDeepening()
    {

        // figure out best move order to start
        Span<Move> moves = stackalloc Move[256];
        GetOrderdEvaluatedLegalMoves(ref moves, false);

        for (int depth = 1; depth <= 25; depth++)
        {
            if (depth > 1)
            {
                // put best move found during last depth first at next depth
                moves[moves.IndexOf(c_BestMove)] = moves[0];
                moves[0] = c_BestMove;
            }

            if (SearchMoves(depth, moves))
            {
                // if (SearchMoves = true) time limit was exceeded or checkmate detected, go with best move.
                // checkmate: has already set c_BestBove
                // timeout: check if current best move before exiting early is better than last depths best move
                if (c_intBestEvalThisIteration > c_intBestEval)
                    c_BestMove = c_BestMoveThisIteration;
                return;
            }

            // Store found best move and eval
            c_BestMove = c_BestMoveThisIteration;
            c_intBestEval = c_intBestEvalThisIteration;

        }
    }

    // Start searching moves at using the current depth
    private bool SearchMoves(int intCurrentDepth, ReadOnlySpan<Move> moves)
    {

        int alpha = -50000,
            beta = 50000;
        c_intBestEvalThisIteration = -100000;

        // Set Time limit
        // NOTE: this is not a great way of estimating move time, but with limited tokens it will do for now
        c_intThisMoveTimeAllowed = c_Timer.MillisecondsRemaining / (80 - Math.Min(c_Board.PlyCount / 2, 39));

        foreach (Move move in moves)
        {
            c_Board.MakeMove(move);

            // If checkmate is found do it now
            if (c_Board.IsInCheckmate())
            {
                c_Board.UndoMove(move);
                c_BestMove = move;
                return true; // no need to search any further depths
            }

            // Do each move a recursively call min max to check the moves after it
            int intEval = -MinMax(-beta, -alpha, intCurrentDepth - 1); //, c_Board.ZobristKey);

            // Don't repeat positions (Stop repetition draw)
            if (c_Board.IsDraw())
                intEval = -50001;  // 1 is so can tell difference between base eval and draw

            c_Board.UndoMove(move);

            if (intEval == -2000000)
                return true; // true = time limit reached (stop iterative deepening code doing next depth)

            if (intEval > c_intBestEvalThisIteration)
            {
                c_intBestEvalThisIteration = intEval;
                c_BestMoveThisIteration = move;
            }

            alpha = Math.Max(alpha, intEval);
        }

        return false;
    }

    // This is the main search the one that checks every possible move
    private int MinMax(int alpha, int beta, int depth)
    {

        // So we know whether this is a best score node
        int alpha_orig = alpha;

        // Search the transposition Table
        bool blnNewEntry = true;
        if (c_colTranspositionTable.TryGetValue(c_Board.ZobristKey, out TranspositionEntry entry))
        {
            blnNewEntry = false;
            if (entry.depth >= depth)
            {
                //If the entry has a lower bound greater than or equal to beta, return it
                int nodetype = entry.nodeType,
                    score = entry.score;

                // If the entry has a lower bound greater than or equal to beta, return it
                if (nodetype == 0) // 0 = eNodeType.Exact)
                    return score;

                if (nodetype == 2 && score <= alpha) // 2 = eNodeType.UpperBound
                    return alpha;

                if (nodetype == 1 && score >= beta) // 1 = eNodeType.LowerBound
                    return beta;

            }
        }

        // End of search, Search captures
        if (depth == 0)
            return QuiescenceSearch(alpha, beta, 6);

        Span<Move> moves = stackalloc Move[256];
        GetOrderdEvaluatedLegalMoves(ref moves, false);

        Move BestMove = Move.NullMove;

        int intBestEval = -50000,
            EvalType = 2; // 2 = eNodeType.UpperBound

        foreach (Move move in moves)
        {
            c_Board.MakeMove(move);
            int intEval = -MinMax(-beta, -alpha, depth - 1);
            c_Board.UndoMove(move);

            // time out 
            if (c_Timer.MillisecondsElapsedThisTurn > c_intThisMoveTimeAllowed)
                return 2000000; // Number bigger than eval allows

            // Store move if better than best move 
            if (intEval > intBestEval)
            {
                intBestEval = intEval;
                BestMove = move;
            }

            // alpha beta pruning
            if (intEval > alpha)
            {
                alpha = intEval;

                if (alpha >= beta)
                    break;
            }
        }

        if (intBestEval >= beta) // failed high, lower bound
            EvalType = 1; // 1 = eNodeType.LowerBound 

        if (intBestEval <= alpha_orig)  // failed low, upper bound
            EvalType = 2; // 2 = eNodeType.UpperBound

        if (alpha_orig < intBestEval && intBestEval < beta)  // exact, principle variation node
            EvalType = 0; // 0 = eNodeType.Exact)

        // if no entry or this depth is less or equal to existing
        if (blnNewEntry || depth <= entry.depth)
            c_colTranspositionTable[c_Board.ZobristKey] = new TranspositionEntry { score = intBestEval, move = BestMove, nodeType = EvalType, depth = depth };


        return intBestEval;

    }

    // Quiescence search is the captures that can happen after the depth is reached, 
    // continuing for each sides captures to get a proper material evaluation
    private int QuiescenceSearch(int alpha, int beta, int limit)
    {

        int stand_pat = GetEval();

        if (stand_pat >= beta)
            return beta;

        if (alpha < stand_pat)
            alpha = stand_pat;

        if (limit == 0)
            return stand_pat;

        Span<Move> moves = stackalloc Move[256];
        GetOrderdEvaluatedLegalMoves(ref moves, true);

        foreach (Move move in moves)
        {
            c_Board.MakeMove(move);
            int intEval = -QuiescenceSearch(-beta, -alpha, limit - 1);
            c_Board.UndoMove(move);

            // alpha beta pruning
            if (intEval >= beta)
                return beta;

            if (intEval > alpha)
                alpha = intEval;
        }

        return alpha;

    }
}
