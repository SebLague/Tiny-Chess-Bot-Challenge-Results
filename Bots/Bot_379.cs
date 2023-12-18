namespace auto_Bot_379;
using ChessChallenge.API;
using System;

// BIG CHUMPER V6.0 - FINAL TWEAKS

public class Bot_379 : IChessBot
{
    // Piece values:   null,  P    N    B    R    Q      K
    int[] pieceValues = { 0, 100, 300, 325, 500, 900, 10_000 };

    // Global vars for easy/quick access
    int initialSearchDepth = 3;
    Move initialBestMove;
    Board board;
    Timer timer;
    bool turn;
    bool runningOutOfTime;
    bool secondEval;
    int move_timelimit;

    public Move Think(Board board, Timer timer)
    {
        // Initialize global vars
        this.board = board;
        this.timer = timer;
        this.turn = board.IsWhiteToMove;
        this.runningOutOfTime = false;
        this.secondEval = false;
        this.move_timelimit = timer.MillisecondsRemaining / 15;

        // Move search and evaluation
        int moveEval = evalMove(initialSearchDepth, -100_000_000, 100_000_000);

        // Redo search with less depth if original depth was too large
        if (runningOutOfTime)
        {
            secondEval = true;
            moveEval = evalMove(initialSearchDepth -= 1, -100_000_000, 100_000_000);
        }
        // Try to increase depth for next turn if played very quickly this time
        else if (timer.MillisecondsElapsedThisTurn < move_timelimit / 4 - 50)
        {
            // Only increase if losing, or in endgame, or has enough time (otherwise, keep low depth for speed)
            if (moveEval <= -50 || BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) <= 15 || timer.MillisecondsRemaining > timer.OpponentMillisecondsRemaining + 5_000 || timer.MillisecondsRemaining > timer.GameStartTimeMilliseconds / 2)
                initialSearchDepth++;
        }

        // Return best move in our current position (which was set globally during eval)
        return initialBestMove;
    }

    // Used for ordering move list before searching (maximizing pruning)
    int compareMoves(Move m1, Move m2) { return expectedValue(m2) - expectedValue(m1); }

    // Manhattan distance (easier to compute and more suited for nature of the game)
    int distance(Square s1, Square s2) { return Math.Abs(s1.Rank - s2.Rank) + Math.Abs(s1.File - s2.File); }

    // Basic heuristics for deciding expected value of a move before playing it
    int expectedValue(Move move)
    {
        int eval = 0;

        // Capture move bonus + Weak piece move bonus
        eval += pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] / 40;

        // Begin mate sequence: Opponent only has king left
        if (BitboardHelper.GetNumberOfSetBits(board.IsWhiteToMove ? board.BlackPiecesBitboard : board.WhitePiecesBitboard) == 1)
        {
            // Bonus for moving king towards opponent king
            if (move.MovePieceType == PieceType.King)
                eval += 500 * (distance(move.StartSquare, board.GetKingSquare(!board.IsWhiteToMove)) - distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)));
            // Bonus for moving rook or queen
            if (move.MovePieceType == PieceType.Queen || move.MovePieceType == PieceType.Rook)
                eval += pieceValues[(int)move.MovePieceType] / 40;
        }

        // Return early to avoid bonus for moving king "away from threat" in check. Blocking with weaker piece may be better.
        if (board.IsInCheck())
            return eval;

        // Get all squares attacked by piece being moved in start and target positions
        ulong attackedSquaresStart = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove);
        ulong attackedSquaresTarget = BitboardHelper.GetPieceAttacks(move.MovePieceType, move.TargetSquare, board, board.IsWhiteToMove);

        // Reward placing pieces where they attack more squares on the board
        eval += (int)(1.5 * (BitboardHelper.GetNumberOfSetBits(attackedSquaresTarget) - BitboardHelper.GetNumberOfSetBits(attackedSquaresStart)));

        // Bonus for attacking enemy king
        if (BitboardHelper.SquareIsSet(attackedSquaresTarget, board.GetKingSquare(!board.IsWhiteToMove)))
            eval += 25;

        // Penalize moving onto attacked square
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            eval -= pieceValues[(int)move.MovePieceType];
        // Bonus for moving away from threat
        else if (board.SquareIsAttackedByOpponent(move.StartSquare))
            eval += pieceValues[(int)move.MovePieceType] / 5;

        // En Passant or Castling is almost always a good option if it can be played
        if (move.IsEnPassant || move.IsCastles)
            eval += 400;

        // Bonus value of promotion piece
        if (move.IsPromotion)
            eval += pieceValues[(int)move.PromotionPieceType] - 100;

        // Pawn near end of board bonus
        if (board.IsWhiteToMove && move.MovePieceType == PieceType.Pawn && move.TargetSquare.Rank >= 4)
            eval += 100 * (move.TargetSquare.Rank - 3);
        else if (!board.IsWhiteToMove && move.MovePieceType == PieceType.Pawn && move.TargetSquare.Rank <= 3)
            eval += 100 * (4 - move.TargetSquare.Rank);

        return eval;
    }

    // Alpha-Beta negamax move depth search
    int evalMove(int depth, int alpha_BestEval, int beta_OpponentBestEval)
    {
        // If taking too long to think, cancel search (redo with less depth)
        if (!secondEval && (runningOutOfTime || timer.MillisecondsElapsedThisTurn > move_timelimit))
        {
            runningOutOfTime = true;
            return 0;
        }

        // Checkmate in fewest moves possible (or avoid being mated for greatest number of moves possible)
        if (board.IsInCheckmate())
            return -10_000_000 - depth;

        // Slight penalty for drawing, go big or go home!
        if (board.IsDraw())
            return turn == board.IsWhiteToMove ? -150 : 150;

        // Gets all legal moves in current position (captures only for quiescense search at end of regular search depth)
        Span<Move> allMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref allMoves, capturesOnly: depth <= 0);

        // End search (ensure max total depth searched ends at an even number, to always consider opponent counter moves)
        if (depth <= -4 - initialSearchDepth % 2)
            return evalPosition();

        // Sort to search best moves first (for maximizing alpha-beta pruning)
        allMoves.Sort(compareMoves);

        // Evaluate possible moves recursively
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int moveEval = -evalMove(depth - 1, -beta_OpponentBestEval, -alpha_BestEval);
            board.UndoMove(move);

            // Alpha-beta cutoff (if an available move is too good, stop searching)
            // [A good opponent wouldn't put me in that position, and a bad one could be refuted by the move found]
            if (moveEval >= beta_OpponentBestEval)
                return moveEval;

            // Get move with maximum expected win evaluation value for me, assuming opponent plays best moves too
            if (moveEval > alpha_BestEval)
            {
                alpha_BestEval = moveEval;

                // Store the best move found for my actual turn at the board
                if (depth == initialSearchDepth)
                    initialBestMove = move;
            }
        }

        // End quiescense search early if no good capture moves available (don't just force bad captures)
        int boardEval;
        if (depth <= 0 && ((boardEval = evalPosition()) > alpha_BestEval))
            return boardEval;

        // Return best evaluation I can guarantee
        return alpha_BestEval;
    }

    // At end of move search depth, decide whether the current board state is favorable for me
    int evalPosition()
    {
        int evaluation = 0;

        // Total Material Count
        for (int pieceType = 1; pieceType < 6; pieceType++)
        {
            evaluation += pieceValues[pieceType] * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard((PieceType)pieceType, true));
            evaluation -= pieceValues[pieceType] * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard((PieceType)pieceType, false));
        }

        // White pawn near end of board bonus (5th, 6th, or 7th rank)
        ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true) >> 32;
        if (whitePawns != 0)
            for (int rank = 5; rank <= 7; rank++)
            {
                evaluation += 75 * (rank - 4) * BitboardHelper.GetNumberOfSetBits(whitePawns & 0xffUL);
                whitePawns >>= 16;
            }

        // Black pawn near end of board bonus (4th, 3rd, or 2nd rank)
        ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false) << 32;
        if (blackPawns != 0)
            for (int rank = 4; rank >= 2; rank--)
            {
                evaluation -= 75 * (5 - rank) * BitboardHelper.GetNumberOfSetBits(blackPawns & 0xff00000000000000UL);
                blackPawns <<= 16;
            }

        // Negamax eval should return score from perspective of current player (at leaf node) to move
        evaluation = board.IsWhiteToMove ? evaluation : -evaluation;

        // Bonus for pushing opponent king towards corner of board in endgame
        if (BitboardHelper.GetNumberOfSetBits(board.IsWhiteToMove ? board.BlackPiecesBitboard : board.WhitePiecesBitboard) == 1)
        {
            Square opponentKing = board.GetKingSquare(!board.IsWhiteToMove);
            evaluation += (int)Math.Round(Math.Abs(3.5 - opponentKing.File) + Math.Abs(3.5 - opponentKing.Rank)) * 10;
        }

        // Penalty for being in check (on my turn)
        if (board.IsInCheck())
            evaluation -= 50;

        return evaluation;
    }
}
