namespace auto_Bot_241;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_241 : IChessBot
{
    // Value of pieces according to alphaZero
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] piecesValues = { 0, 100, 305, 333, 563, 950, 10_000 };

    int depthOfSearch;
    Move[] legalMoves;
    Move moveToPlay;
    bool searchWasBroken = false;
    bool myBotIsWhite;
    Piece lastMovedPiece;

    //--- Values to tweek ---
    int kingSafetyZoneBonus = 5;
    int DrawacceptanceThreshold = 0;
    int PassedPawnBonus = 20;
    int DoubledPawnPenalty = 20;
    int CastlingBonus = 90;
    int MovingSamePiecePenalty = 90;
    int controledSquareBonus = 20;

    public Move Think(Board board, Timer timer)
    {

        myBotIsWhite = board.IsWhiteToMove;
        // Settup for the iterative deepening search
        legalMoves = OrderMoves(Shuffle(board.GetLegalMoves()));
        depthOfSearch = 2;
        searchWasBroken = false;

        // If we have enough time, we continue searching deeper and deeper
        while (CheckTime(timer))
        {
            AlphaBeta(board, depthOfSearch, int.MinValue, int.MaxValue, board.IsWhiteToMove, timer);
            depthOfSearch++;

            // We put the best move found at the beginning of the array so that it is searched first next time
            Move temp = legalMoves[0];
            legalMoves[0] = moveToPlay;
            for (int i = 1; i < legalMoves.Length - 1; i++)
            {
                (legalMoves[i], temp) = (temp, legalMoves[i]);
            }
        }

        return moveToPlay;
    }

    //--- Used for the search ----------------------------------------------------------------------------------------------------------

    // Check if we have enough time to continue searching --------------------------------------------------------------------------
    // Enough for 70 moves from the start, or 30 moves from now
    bool CheckTime(Timer timer)
    {
        return timer.MillisecondsElapsedThisTurn < Math.Min(timer.GameStartTimeMilliseconds / 70, timer.MillisecondsRemaining / 30);
    }
    //------------------------------------------------------------------------------------------------------------------------------

    // Shuffle an array of moves in order to randomize the plays -------------------------------------------------------------------
    Move[] Shuffle(Move[] moveArray) { return moveArray.OrderBy(x => Guid.NewGuid()).ToArray(); }
    //------------------------------------------------------------------------------------------------------------------------------

    // Give the "base value" of a move for sorting purposes. Lower will be placed first in the array -------------------------------
    int GetMoveBaseValue(Move move)
    {
        if (move.IsPromotion) return -10;
        if (move.IsCapture) return (piecesValues[(int)move.MovePieceType] - piecesValues[(int)move.CapturePieceType]) / 100;
        if (move.MovePieceType == PieceType.King && !move.IsCastles) return 11;
        else return 10;
    }
    //------------------------------------------------------------------------------------------------------------------------------

    // Order moves according to their base value (Promotion, Capture, Other, king and not castles) using bubble sort ---------------
    Move[] OrderMoves(Move[] moveArray)
    {
        Array.Sort(moveArray, (x, y) => GetMoveBaseValue(x).CompareTo(GetMoveBaseValue(y)));
        return moveArray;
    }
    //------------------------------------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------------------------------------

    //--- Used for the static eval -----------------------------------------------------------------------------------------------------

    // Check if we are is the endgame (less than 8 types of pieces/color on the board) ---------------------------------------------
    bool IsFinal(Board board) { return board.GetAllPieceLists().Length <= 8; }

    // Get the bitboard of all the attacked square of a given player (blocker are  : all piece or specified) -----------------------
    ulong GetAllAttacks(Board board, bool iswhite)
    {
        ulong attacks = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            if (list.IsWhitePieceList == iswhite)
            {
                foreach (Piece piece in list)
                {
                    attacks |= BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, iswhite);
                }
            }
        }
        return attacks;
    }

    // Get the score of a player (meaning the combined value of all their pieces) --------------------------------------------------
    int GetScore(Board board, bool isWhite)
    {
        int score = 0;
        PieceList[] allpieces = board.GetAllPieceLists();
        foreach (PieceList pieces in allpieces) if (pieces.IsWhitePieceList == isWhite)
            {
                foreach (Piece piece in pieces) score += piecesValues[(int)piece.PieceType];
            }
        return score;
    }

    // Calculate a bonus for the king safety (the more squares are under attack close to the king the most dangerous it is) --------
    int GetKingSafetyBonus(Board board, bool iswhite)
    {
        int bonus = 0;
        ulong KingZone = BitboardHelper.GetKingAttacks(board.GetKingSquare(iswhite));
        bonus -= BitboardHelper.GetNumberOfSetBits(KingZone & GetAllAttacks(board, !iswhite)) * kingSafetyZoneBonus;
        return bonus;
    }

    // Get the Bitboard of the file starting from a given square and going up or down (from white's perspective) ------------------
    ulong GetFileBitboardFrom(Square square, bool isGoingUp)
    {
        int offset = (isGoingUp ? Math.Min(7, square.Rank + 1) : 7 - Math.Max(0, square.Rank - 1)) * 8;
        ulong mask = 0x0101010101010101;
        mask <<= square.File;
        return isGoingUp ? mask << offset : mask >> offset;
    }

    // Get a bonus for the pawn structure (passed pawns are good and better if close to promotion, doubled pawns are bad) ---------
    int GetPawnStructureBonus(Board board, bool isWhite)
    {
        PieceList myPawns = board.GetPieceList(PieceType.Pawn, isWhite);
        int bonus = 0;
        foreach (Piece pawn in myPawns)
        {
            // Passed pawns are good
            ulong threeFilesInFront = GetFileBitboardFrom(pawn.Square, pawn.IsWhite); // Get the file of the pawn (starting from it)
            threeFilesInFront |= threeFilesInFront << 1 | threeFilesInFront >> 1; // Add the two adjacent files
                                                                                  // Doesn't quite work for the edge pawns, but it's short
            if ((threeFilesInFront & board.GetPieceBitboard(PieceType.Pawn, !isWhite)) == 0)
            {
                int distance = isWhite ? 7 - pawn.Square.Rank : pawn.Square.Rank;
                bonus += PassedPawnBonus * (6 - distance); // Closer to promotion is beter.
            }
            // Doubled pawns are bad
            if ((GetFileBitboardFrom(pawn.Square, isWhite) & board.GetPieceBitboard(PieceType.Pawn, isWhite)) != 0)
            {
                bonus -= DoubledPawnPenalty;
            }
        }
        return bonus;
    }

    // Get a bonus according to squares controled, the intent is to encourage the bot to control the center
    int GetCenterControlBonus(Board board, bool isWhite)
    {
        int bonus = 0;
        ulong control = GetAllAttacks(board, isWhite);
        ulong center = 0x0000001818000000;
        ulong outerCenterRing = 0x00003C3C3C3C0000UL & ~center;
        bonus += BitboardHelper.GetNumberOfSetBits(control & center) * controledSquareBonus; // Center
        bonus += BitboardHelper.GetNumberOfSetBits(control & outerCenterRing) * controledSquareBonus / 4; // Outer center
        return bonus;
    }

    //----------------------------------------------------------------------------------------------------------------------------------

    // StaticEval function that evaluates the position ---------------------------------------------------------------------------------
    int StaticEval(Board board)
    {
        // Count the value of all the pieces (sign depends on the color)
        int eval = GetScore(board, true) - GetScore(board, false);
        // Bonus for the pawn structure
        eval += GetPawnStructureBonus(board, true) - GetPawnStructureBonus(board, false);
        // Bonus for king safety
        eval += GetKingSafetyBonus(board, true) - GetKingSafetyBonus(board, false);
        if (!IsFinal(board))
        {
            // Bonus for controlling the center (could be painfull in the endgame)
            eval += GetCenterControlBonus(board, true) - GetCenterControlBonus(board, false);
        }
        return eval;
    }
    //----------------------------------------------------------------------------------------------------------------------------------

    // AlphaBeta search ----------------------------------------------------------------------------------------------------------------
    int AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer)
    {

        // If the game is over, return the score
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return ((board.PlyCount % 2 == 1) ? 1 : -1) * 100_000;

        // If we don't have enough time, we stop searching
        if (!CheckTime(timer))
        {
            searchWasBroken = depth > 0;
            return StaticEval(board);
        }

        // If we are at the end of the search, we return the static eval. We never stop on checks
        if (depth <= 0 && !board.IsInCheck()) return StaticEval(board);

        Move[] moves = depth == depthOfSearch ? legalMoves : OrderMoves(board.GetLegalMoves());

        int bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;
        Move bestMove = Move.NullMove;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = AlphaBeta(board, depth - 1, alpha, beta, !maximizingPlayer, timer);
            board.UndoMove(move);

            // We Give a bonus if the move is castles. This is to encourage castling
            if (move.IsCastles) eval += (maximizingPlayer ? 1 : -1) * CastlingBonus;
            // We Give a penalty for moving the same piece twice
            Piece movedPiece = board.GetPiece(move.StartSquare);
            if (maximizingPlayer == myBotIsWhite && movedPiece == lastMovedPiece && !IsFinal(board))
            {
                eval += (maximizingPlayer ? -1 : 1) * MovingSamePiecePenalty;
            }
            lastMovedPiece = movedPiece;

            if ((maximizingPlayer && eval > bestEval) || (!maximizingPlayer && eval < bestEval))
            {
                bestEval = eval;
                bestMove = move;
            }

            if (maximizingPlayer) alpha = Math.Max(alpha, eval);
            else beta = Math.Min(beta, eval);

            if (beta <= alpha) break;
        }

        if (depth == depthOfSearch && !searchWasBroken) moveToPlay = bestMove;

        return bestEval;
    }
    //----------------------------------------------------------------------------------------------------------------------------------
}