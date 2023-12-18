namespace auto_Bot_205;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_205 : IChessBot
{
    Dictionary<ulong, PositionEntry> transpositionTable = new Dictionary<ulong, PositionEntry>();

    struct PositionEntry
    {
        public int Depth;
        public int Score;
        public Move BestMove;

        public PositionEntry(int depth, int score, Move bestMove)
        {
            Depth = depth;
            Score = score;
            BestMove = bestMove;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        int maxDepth = 5;
        int currentDepth = 1;

        // iterative deepening -> calculating / rating the moves
        while (currentDepth <= maxDepth)
        {
            AlphaBeta(board, currentDepth, int.MinValue + 1, int.MaxValue);
            currentDepth++;

            // time very low
            if (timer.MillisecondsRemaining < 10_000)
                maxDepth = 3;
        }

        // retrieval of best move
        return transpositionTable[board.ZobristKey].BestMove;
    }

    /* alpha is the minimum score you are guaranteed, beta is the minimum score the enemy is guaranteed */
    int AlphaBeta(Board board, int depth, int alpha, int beta)
    {
        ulong zobristKey = board.ZobristKey;
        int bestGuaranteedScore = int.MinValue;
        Move bestMove = Move.NullMove;

        if (transpositionTable.ContainsKey(zobristKey) && transpositionTable[zobristKey].Depth >= depth)
        {
            // position already exists and to be searched depth is deeper than stored depth
            PositionEntry entry = transpositionTable[zobristKey];
            return transpositionTable[zobristKey].Score;
        }

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            if (board.IsInCheckmate() || board.IsDraw())
                depth = 10000; // -> position gets saved

            // leaf node evaluation
            bestGuaranteedScore = board.IsWhiteToMove ? Evaluate(board) : -Evaluate(board); // positive = good for player at the moment
        }
        else
        {
            // check for interestingness (check and captures) -> increase depth -> doesnt increase higher up stored depth though...
            if (board.IsInCheck())
                depth++;

            // move ordering -> increase pruning
            Move[] legalMoves = board.GetLegalMoves();

            List<Move> sortedLegalMoves = legalMoves.Where(move => move.IsPromotion).ToList();
            List<Move> captureMoves = legalMoves.Where(move => move.IsCapture).ToList();
            List<Move> nonCaptureMoves = legalMoves.Where(move => !move.IsCapture).ToList();

            sortedLegalMoves.AddRange(captureMoves);
            sortedLegalMoves.AddRange(nonCaptureMoves);

            // recursion over moves
            foreach (Move move in sortedLegalMoves)
            {
                board.MakeMove(move);
                int worstPossibleScore = -AlphaBeta(board, depth - 1, -beta, -alpha); // worst possible of the positions available when you make the best move
                board.UndoMove(move);

                // check for best move yet
                if (worstPossibleScore > bestGuaranteedScore)
                {
                    bestGuaranteedScore = worstPossibleScore;
                    bestMove = move;
                    if (worstPossibleScore > alpha)
                    {
                        alpha = worstPossibleScore;

                        // player before wont make the move to this position
                        if (alpha >= beta)
                            break;
                    }
                }
            }
        }

        transpositionTable[zobristKey] = new PositionEntry(depth, bestGuaranteedScore, bestMove);

        return bestGuaranteedScore;
    }

    int Evaluate(Board board)
    {
        if (board.IsInCheckmate())
            return board.IsWhiteToMove ? -10000 : 10000;

        if (board.IsDraw())
            return 0;

        bool isEndgame = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 14 || BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Queen, true) | board.GetPieceBitboard(PieceType.Queen, false)) < 2;
        int score = 0;


        // Material balance
        int[] pieceValue = { 0, 100, 320, 330, 500, 900 };
        for (int i = 1; i < 6; i++)
        {
            PieceType pieceType = (PieceType)i;
            score += pieceValue[i] * (board.GetPieceList(pieceType, true).Count - board.GetPieceList(pieceType, false).Count);
        }

        // Checks
        if (board.IsInCheck())
            score += board.IsWhiteToMove ? -10 : 10;

        // you are to move
        score += board.IsWhiteToMove ? 15 : -15;


        if (isEndgame)
        {
            // Rook and Queen positions + pawn promotion
            score += EvaluateEndGamePosition(board, true) - EvaluateEndGamePosition(board, false);

            // Opposition (good only if you are better)
            int kingPositionDifference = Math.Abs(board.GetKingSquare(true).Index - board.GetKingSquare(false).Index);
            if (kingPositionDifference == 2 || kingPositionDifference == 16)
                score += score > 0 ? 30 : -30;
        }
        else
        {
            // Central control
            score += EvaluatePiecePositions(board, true) - EvaluatePiecePositions(board, false);

            // King safety
            score += EvaluateKingSafety(board, true) - EvaluateKingSafety(board, false);
        }

        return score;
    }

    int EvaluatePiecePositions(Board board, bool isWhite)
    {
        int[] centralControlPieces = { 1, 2 };

        int score = 0;

        // central control
        foreach (int index in centralControlPieces)
        {
            ulong pieces = board.GetPieceBitboard((PieceType)index, isWhite);
            score += 90 * BitboardHelper.GetNumberOfSetBits(0x0000001818000000UL & pieces); // inner center
            score += 45 * BitboardHelper.GetNumberOfSetBits(0x00003C24243C0000UL & pieces); // outer center
        }

        // bishop position
        ulong bishops = board.GetPieceBitboard(PieceType.Bishop, isWhite);
        score += 45 * BitboardHelper.GetNumberOfSetBits(0x0000666666660000UL & bishops);

        return score;
    }

    int EvaluateEndGamePosition(Board board, bool isWhite)
    {
        int[] openFilesPieces = { 3, 4, 5 };
        int[] matingPieces = { 4, 5 };
        double[] openFilesPiecesValue = { 2, 2, 0.8 };

        int opkingIndexShifted = board.GetKingSquare(!isWhite).Index - 35;

        ulong allPieces = board.AllPiecesBitboard;
        ulong opkingInnerMask = opkingIndexShifted > 0 ? 0x00001C141C000000UL << opkingIndexShifted : 0x00001C141C000000UL >> -opkingIndexShifted;
        ulong opkingOuterMask = opkingIndexShifted > 0 ? 0x003E2222223E0000UL << opkingIndexShifted : 0x003E2222223E0000UL >> -opkingIndexShifted;

        ulong pawns = board.GetPieceBitboard(PieceType.Pawn, isWhite);

        int score = 0;


        // open files
        foreach (int index in openFilesPieces)
        {
            PieceType pieceType = (PieceType)index;
            foreach (Piece piece in board.GetPieceList(pieceType, isWhite))
            {
                ulong pieceMoves = BitboardHelper.GetPieceAttacks(pieceType, piece.Square, allPieces, isWhite);
                score += (int)(openFilesPiecesValue[(int)pieceType - 3] * BitboardHelper.GetNumberOfSetBits(pieceMoves));
            }
        }

        // mating -> get close to king
        foreach (int index in matingPieces)
        {
            ulong pieces = board.GetPieceBitboard((PieceType)index, isWhite);
            score += 40 * BitboardHelper.GetNumberOfSetBits(opkingInnerMask & pieces); // first ring around opking
            score += 20 * BitboardHelper.GetNumberOfSetBits(opkingOuterMask & pieces); // second ring around opking
        }

        // king activity
        score += 20 * BitboardHelper.GetNumberOfSetBits(0x00003C3C3C3C0000UL & (1UL << board.GetKingSquare(isWhite).Index)); // middle 4x4

        // corneredness of opponents king -> help with mating in endgame
        score += 90 * BitboardHelper.GetNumberOfSetBits((1UL << opkingIndexShifted + 35) & 0xC3810000000081C3UL);

        // pawn promotions
        score += 130 * BitboardHelper.GetNumberOfSetBits(pawns & (isWhite ? 0x00FF000000000000UL : 0x000000000000FF00UL)) // seventh rank
            + 60 * BitboardHelper.GetNumberOfSetBits(pawns & (opkingIndexShifted > 0 ? 0x003E2222223E0000UL << opkingIndexShifted : 0x003E2222223E0000UL >> -opkingIndexShifted)); // sixth rank

        return score;
    }

    int EvaluateKingSafety(Board board, bool isWhite)
    {
        ulong kingPosition = 1UL << board.GetKingSquare(isWhite).Index;

        ulong pieces = isWhite ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        ulong surroundedKingMask = BitboardHelper.GetKingAttacks(board.GetKingSquare(isWhite));

        return ((kingPosition & 0xC3000000000000C3UL) == 0 ? 0 : 200) + 6 * BitboardHelper.GetNumberOfSetBits(pieces & surroundedKingMask);
    }

}
