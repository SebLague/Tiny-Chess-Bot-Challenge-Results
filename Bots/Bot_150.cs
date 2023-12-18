namespace auto_Bot_150;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_150 : IChessBot
{
    Board board;
    Timer timer;
    int maxDepth;
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };
    Move bestMove, lastMove;
    ulong[] prevPositions = new ulong[20];
    static Random rng = new Random();

    Dictionary<ulong, string>[] tinyBook = new Dictionary<ulong, string>[2]
    {
    new Dictionary<ulong, string>()
    {
    // WHITE
    // Ruy Lopez
    { 13227872743731781434, "e2e4" },
    { 17664629573069429839, "g1f3" },
    {  4392852280258111003, "f1b5" },
    // Berlin
    { 16216659299759980788, "d2d3" },
    // Exchange Variation
    {  9867298184502681243, "b5c6" },
    { 8940286089220283244,  "b5c6" },
    // BLACK
    // Russian Defence
    { 15607329186585411972, "e7e5" },
    {  9882551119019387150, "g8f6" },
    // Bc4
    {  2489898084368615438, "f6e4" },
    {  1848322555222883841, "d7d5" },
    { 13491258628483322835, "d8g5" },
    {  7682292172564989177, "d8g5" },
    // Nc4 - Four Knights
    {  7327528534536375422, "b8c6"},
    {  6170347653963943965, "f6e4" },
    // Nxe5 - Stafford Gambit
    { 11416031868353686291, "b8c6" },
    {  6355776680573568906, "d7c6" },
    // 5. d3
    {  9588357922653014998, "f8c5" },
    {  8003521074529437161, "h7h5" }
    },
    new Dictionary<ulong, string>()
    {
    // WHITE
    // London System
    { 13227872743731781434, "d2d4" },
    {  2293119182805454427, "c1f4" },
    {  9628964799347499318, "g1f3" },
    { 10729463467987483981, "c1f4" },
    { 12722993928303421391, "e2e3" },
    // BLACK
    // Sicilian Defense
    { 15607329186585411972, "c7c5" },
    {  9341647070118419100, "d7d6" },
    {  8330934554539731772, "c5d4" },
    { 16302338549478773485, "g8f6" },
    {  1056315154796951453, "g7g6" },
    { 17125461137602285889, "f8g7" }
    }
    };
    int opening = rng.Next(2);

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        maxDepth = 4 + (32 - countBits(board.AllPiecesBitboard)) / 8;

        if (bookMove() == null)
            search(0, -1000, 1000);

        for (int i = 19; i > 1; i--)
            prevPositions[i] = prevPositions[i - 1];
        prevPositions[1] = board.ZobristKey;
        board.MakeMove(bestMove);
        prevPositions[0] = board.ZobristKey;
        board.UndoMove(bestMove);

        return lastMove = bestMove;
    }

    Move? bookMove()
    {
        var book = tinyBook[opening];
        string? move;
        return book.TryGetValue(board.ZobristKey, out move) ? bestMove = new Move(move, board) : null;
    }

    int search(int depth, int alpha, int beta)
    {
        if (timer.MillisecondsRemaining < 30000)
            maxDepth = Math.Min(maxDepth, timer.MillisecondsRemaining < 20000 ?
                                            timer.MillisecondsRemaining < 10000 ? 4 : 5 : 6);

        Move[] moves;
        if (depth >= maxDepth || (moves = getMoves()).Length == 0)
            return evalBoard();

        if (moves.Length == 1)
            return setBestMove(1000, depth, moves[0]);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = -search(depth + 1, -beta, -alpha);
            board.UndoMove(move);
            if (score >= beta)
                return setBestMove(beta, depth, move);
            if (score > alpha)
                alpha = setBestMove(score, depth, move);
        }

        return alpha;
    }

    Move[] getMoves()
    {
        var moves = board.GetLegalMoves();
        var orderedMoves = new SortedDictionary<int, Move>();
        bool checkmate, edgeKnight, repetition = false;
        int i = 0, j, r, f;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            checkmate = board.IsInCheckmate();
            edgeKnight = move.MovePieceType == PieceType.Knight &&
                ((r = move.TargetSquare.Rank) == 0 || r == 7 || (f = move.TargetSquare.File) == 0 || f == 7);
            for (j = 0; j < 20 && !(repetition = board.ZobristKey == prevPositions[j]); j++) ;
            board.UndoMove(move);
            if (checkmate)
                return new Move[1] { move };
            orderedMoves.Add(((move.IsCapture ? 10 + // Look at captures first, preferring lower ranked pieces taking higher ranked pieces
                (pieceValues[(int)move.MovePieceType] - pieceValues[(int)move.CapturePieceType]) : 0) +
                // (rng.Next(moves.Length) == 0 ? 1 : 0) + // Randomly promote this move - seems to help with avoiding repetition / 50 move rule
                //                                         // (but can result in blunders)
                (move.IsPromotion ? pieceValues[(int)move.PromotionPieceType] : 0) + // Prefer promoting to queen
                (rng.Next(8) == 0 && move.MovePieceType == PieceType.Pawn ? 1 : 0) + // Have 1:8 chance of preferring to push a pawn
                (move.IsCastles ? Math.Max((20 - board.PlyCount) / 2, 1) : 0) - // Prefer castling early
                (repetition ? 10 : 0) - // Avoid repetition
                (move.StartSquare == lastMove.TargetSquare ? 1 : 0) - // Avoid moving the same piece twice
                (edgeKnight ? 1 : 0) - // Avoid placing knights on the edge of the board
                (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0)) * 1000 + i++, move);
        }
        return orderedMoves.Values.Reverse().ToArray<Move>();
    }

    int setBestMove(int score, int depth, Move move)
    {
        if (depth == 0)
            bestMove = move;
        return score;
    }

    int evalBoard()
    {
        return board.IsInCheckmate() ? -1000 :
                board.IsDraw() ? 0 :
                (evalPieces(true) - evalPieces(false)) * (board.IsWhiteToMove ? 1 : -1);
    }

    int evalPieces(bool white)
    {
        int bits, score = 0;
        for (int i = 1; i < 6; i++)
            score += (bits = countBits(board.GetPieceBitboard((PieceType)i, white))) * pieceValues[i] +
                        (i == 3 && bits == 2 ? 1 : 0); // A bishop pair scores an extra point
        return score;
    }

    int countBits(ulong bb)
    {
        int bits;
        for (bits = 0; bb > 0; bits++, bb &= bb - 1) ;
        return bits;
    }
}