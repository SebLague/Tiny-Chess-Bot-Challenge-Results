namespace auto_Bot_222;
using ChessChallenge.API;
using System;
using System.Linq;

// Token count 1005
// PecanPie v1.3

public class Bot_222 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 320, 325, 550, 975, 10000 };
    ulong[] packedPV = { 0x32643C3732373732, 0x32643C37322D3C32, 0x3264463C32283C32, 0x3264504D4B321932, 0x000A141414140A00, 0x0A1E323732371E0A, 0x14323C41413C321E, 0x1432414646413714, 0x1E2828282828281E, 0x28323237323C3728, 0x283237373C3C320A, 0x28323C3C3C3C3228, 0x32372D2D2D2D2D32, 0x323C323232323232, 0x323C323232323237, 0x323C323232323237, 0x1E28282D3228281E, 0x2832323232373228, 0x2832373737373728, 0x2D32373737373237, 0x141414141E284646, 0x0A0A0A0A141E4650, 0x0A0A0A0A141E323C, 0x000000000A1E3232, 0x0014141414141400, 0x0A1E282828281414, 0x1428465050463214, 0x1E32505A5A503214 };

    Board b;
    ulong zKey => b.ZobristKey;

    Timer t;
    int msToThink;

    ulong tt_size = 1048583;
    TT_Entry[] tt;

    int[,,] pieceSquareBonuses;

    bool cancelled => t.MillisecondsElapsedThisTurn > msToThink;

    Move searchBestMove;

    bool endgame;
    bool isSideEndgame(bool isWhite) => b.GetPieceBitboard(PieceType.Queen, isWhite) == 0 || (b.GetPieceBitboard(PieceType.Rook, isWhite) == 0 && BitboardHelper.GetNumberOfSetBits(b.GetPieceBitboard(PieceType.Bishop, isWhite) | b.GetPieceBitboard(PieceType.Knight, isWhite)) < 2);

    public Bot_222()
    {
        tt = new TT_Entry[tt_size];
        pieceSquareBonuses = new int[7, 8, 4];

        for (int i = 0; i < 224; i++) pieceSquareBonuses[i / 32, i % 8, i / 8 % 4] = (int)((packedPV[i / 8] >> (i % 8 * 8)) & 0x00000000000000FF) - 50;
    }

    int getPieceSquareBonus(int pieceType, int index, bool isWhite)
    {
        int rank = isWhite ? index / 8 : 7 - index / 8;
        int file = Math.Min(index % 8, 7 - index % 8);
        if (pieceType == 5) return pieceSquareBonuses[endgame ? 6 : 5, rank, file];
        return pieceSquareBonuses[pieceType, rank, file];
    }

    int score(bool isWhite)
    {
        int score = 0;

        var enemyKing = BitboardHelper.GetKingAttacks(b.GetKingSquare(!isWhite));
        var enemyPawns = b.GetPieceBitboard(PieceType.Pawn, !isWhite);

        for (int i = 1; i < 7; i++)
        {
            var pieces = b.GetPieceBitboard((PieceType)i, isWhite);

            // We don't like doubled pawns
            if (i == 1) for (int j = 0; j < 8; j++) score -= 10 * Math.Max(BitboardHelper.GetNumberOfSetBits(pieces & 0x0101010101010101ul << i) - 1, 0);

            ulong attacks = 0;

            ulong pieceIter = pieces;
            while (pieceIter != 0)
            {
                var index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceIter);

                // Add piece value and piece square bonus
                score += pieceValues[i] + getPieceSquareBonus(i - 1, index, isWhite);

                var pieceAttacks = BitboardHelper.GetPieceAttacks((PieceType)i, new Square(index), b, isWhite);
                attacks |= pieceAttacks;

                // Prefer piece mobility
                score += BitboardHelper.GetNumberOfSetBits(pieceAttacks);

                // We like attacking the enemy king
                score += 10 * BitboardHelper.GetNumberOfSetBits(pieceAttacks & enemyKing);

                if (i != 1) continue;

                // We like passed pawns
                pieceAttacks |= 1ul << index + (isWhite ? 8 : -8);
                bool isPassed = true;
                while (pieceAttacks != 0 && isPassed)
                {
                    isPassed = (pieceAttacks & enemyPawns) == 0;
                    pieceAttacks = isWhite ? pieceAttacks << 8 : pieceAttacks >> 8;
                }

                if (isPassed) score += 50;
            }

            if (i != 1) continue;

            // We like pawn chains
            score += 10 * BitboardHelper.GetNumberOfSetBits(pieces & attacks);
        }

        return score;
    }

    int evaluate(bool whiteToMove) => score(whiteToMove) - score(!whiteToMove);

    int moveOrder(Move move, Move storedBest)
    {
        if (move.Equals(storedBest)) return 100000;
        return pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
    }

    public Move Think(Board board, Timer timer)
    {
        msToThink = timer.IncrementMilliseconds + timer.MillisecondsRemaining / 40;

        b = board;
        t = timer;

        int depth = 2;
        Move bestMove = searchBestMove = Move.NullMove;

        while (!cancelled)
        {
            bestMove = searchBestMove;
            search(depth++, -100000, 100000, true);
        }

        // If we didn't come up with a best move then just take the first one we can get
        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }


    int search(int depth, int alpha, int beta, bool isTopLevel)
    {
        bool quiesce = depth <= 0;

        if (cancelled) return 0;

        // Encourage the engine to fight to the end by making early checkmates
        // have a better score than later checkmates.
        if (b.IsInCheckmate()) return b.PlyCount - 100000;

        // Check for draw by means other than stalemate. Stalemate will be checked when we generate moves.
        if (b.IsFiftyMoveDraw() || b.IsRepeatedPosition() || b.IsInsufficientMaterial()) return 0;

        // Adjust alpha and beta for the current ply of the game.
        beta = Math.Min(100000 - b.PlyCount, beta);
        alpha = Math.Max(b.PlyCount - 100000, alpha);

        endgame = isSideEndgame(true) && isSideEndgame(false);

        // If we are in quiescense then adjust alpha for the possibility of not making any captures.
        if (quiesce && !b.IsInCheck()) alpha = Math.Max(alpha, evaluate(b.IsWhiteToMove));

        Move bestMove = Move.NullMove;
        TT_Entry entry = tt[zKey % tt_size];
        if (entry.key == zKey)
        {
            bestMove = entry.bestMove;
            if (isTopLevel) searchBestMove = bestMove;

            if (quiesce || depth <= entry.depth)
            {
                // exact or upper bound
                if (entry.nodeType > 1) beta = Math.Min(entry.evaluation, beta);
                // exact or lower bound
                if (entry.nodeType < 3) alpha = Math.Max(entry.evaluation, alpha);
            }
        }

        // Early out if any of these conditions has caused the alpha/beta window to cut off.
        if (alpha >= beta) return alpha;

        var moves = b.GetLegalMoves(quiesce && !b.IsInCheck()).OrderByDescending(move => moveOrder(move, bestMove)).ToArray();

        // Check for stalemate
        if (moves.Length == 0) return quiesce ? alpha : 0;

        byte nodeType = 3; // Upper bound

        foreach (Move move in moves)
        {
            b.MakeMove(move);

            int move_score = -search(depth - 1, -alpha - 1, -alpha, false);
            if (move_score > alpha && move_score < beta) move_score = -search(depth - 1, -beta, -alpha, false);

            b.UndoMove(move);

            if (move_score > alpha)
            {
                bestMove = move;
                if (isTopLevel) searchBestMove = move;
                alpha = move_score;
                nodeType = 2; // Exact
            }

            if (alpha >= beta)
            {
                nodeType = 1; // Lower bound
                break;
            }
        }

        if (!cancelled && (entry.depth <= Math.Max(depth, 0))) tt[zKey % tt_size] = entry with { key = zKey, depth = depth, evaluation = alpha, nodeType = nodeType, bestMove = bestMove };

        return alpha;
    }

    struct TT_Entry
    {
        public ulong key;
        public int depth;
        public int evaluation;
        public Move bestMove;

        // 1 - Lower bound
        // 2 - Exact
        // 3 - Upper bound
        public byte nodeType;
    }
}
