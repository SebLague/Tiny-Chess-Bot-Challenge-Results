namespace auto_Bot_230;
/*
 * Sebastian Lague's Tiny Chess Challenge
 * https://github.com/SebLague/Chess-Challenge
 * https://youtu.be/iScy18pVR58
 *
 * Submission by Lauri Räsänen
 * https://rasanen.dev/
 *
 * PVS with hand written eval.
 * Somewhat sketchy adaptive depth,
 * based on previous think time and EBF in alpha-beta.
 * Roughly 1650 elo according to Stockfish UCI_ELO.
 */

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_230 : IChessBot
{
    int[] pieceValues;
    int lastThinkTime;
    int maxDepth = 5;
    int lastEval;
    Move[] moveList;

    public Move Think(Board board, Timer timer)
    {
        if (lastThinkTime > 0)
        {
            if (lastThinkTime > timer.MillisecondsRemaining * 0.1f)
                maxDepth--;
            else
            {
                // b ^ ceil(n/2) + b ^ floor(n/2) when b = 40 => 1.95, 20.5 alternating
                var timeEstimate = lastThinkTime * (maxDepth % 2 == 0 ? 20.5f : 1.95f);
                if (timeEstimate < timer.MillisecondsRemaining * 0.05f && lastEval < 999)
                    maxDepth++;
            }
            // going below this is no bueno, fast anyways.
            // probably shouldn't happen unless running on a potato.
            if (maxDepth < 2) maxDepth = 2;
        }

        // Change piece values based on how far we are in the game
        var pieceValueMult = MathF.Min(1.0f, board.PlyCount / 60.0f);
        pieceValues = new int[]{
            88   + (int)(pieceValueMult * 24),  // pawn
            394  - (int)(pieceValueMult * 55),  // knight
            428  - (int)(pieceValueMult * 71),  // bishop
            572  + (int)(pieceValueMult * 42),  // rook
            1240 - (int)(pieceValueMult * 116), // queen
            0                                   // king
        };

        moveList = new Move[maxDepth];
        lastEval = Search(board, -10001, 10000, maxDepth);
        lastThinkTime = timer.MillisecondsElapsedThisTurn + 5; // fudge
        return moveList[maxDepth - 1];
    }

    private int Search(Board board, int alpha, int beta, int depth)
    {
        if (depth == 0)
        {
            // quiesce
            int eval = EvalBoard(board);

            if (eval >= beta) return beta;
            if (alpha < eval) alpha = eval;

            foreach (var capture in board.GetLegalMoves(true))
            {
                board.MakeMove(capture);
                eval = -Search(board, -beta, -alpha, 0);
                board.UndoMove(capture);
                if (eval >= beta) return beta;
                if (alpha < eval) alpha = eval;
            }

            return alpha;
        }

        var searchPv = true;

        foreach (var move in board.GetLegalMoves().OrderByDescending(m => (m.IsCapture ? 100 * (int)m.CapturePieceType - (int)m.MovePieceType : 0) + (m.IsPromotion ? 1 : 0)))
        {
            board.MakeMove(move);

            int score = 0;
            if (searchPv)
            {
                score = -Search(board, -beta, -alpha, depth - 1);
                moveList[depth - 1] = move;
            }
            else
            {
                score = -Search(board, -alpha - 1, -alpha, depth - 1);
                if (score > alpha)
                    score = -Search(board, -beta, -alpha, depth - 1);
            }

            board.UndoMove(move);

            if (score >= beta)
            {
                moveList[depth - 1] = move;
                return beta;
            }
            if (score > alpha)
            {
                moveList[depth - 1] = move;
                alpha = score;
                searchPv = false;
            }
        }

        return alpha;
    }

    private int EvalBoard(Board board)
    {
        if (board.IsInCheckmate())
            return -10000;

        if (board.IsDraw())
            return -50; // contempt

        // token optimization
        var whiteToMove = board.IsWhiteToMove ? 1 : -1;
        var pieces = board.GetAllPieceLists();
        var openingWeight = 3.0f / board.PlyCount;

        var GetPieceBitboard = board.GetPieceBitboard;
        var GetNumberOfSetBits = BitboardHelper.GetNumberOfSetBits;

        var whitePawnBB = GetPieceBitboard(PieceType.Pawn, true);
        var blackPawnBB = GetPieceBitboard(PieceType.Pawn, false);
        var whiteKnightBB = GetPieceBitboard(PieceType.Knight, true);
        var blackKnightBB = GetPieceBitboard(PieceType.Knight, false);
        var whiteBishopBB = GetPieceBitboard(PieceType.Bishop, true);
        var blackBishopBB = GetPieceBitboard(PieceType.Bishop, false);
        var whiteQueenBB = GetPieceBitboard(PieceType.Queen, true);
        var blackQueenBB = GetPieceBitboard(PieceType.Queen, false);
        var whitePiecesBB = board.WhitePiecesBitboard;
        var blackPiecesBB = board.BlackPiecesBitboard;

        // mobility
        // GetLegalMoves is very slow here.
        // Will never be cached since we call MakeMove right before eval.
        // Estimate legal moves by checked state and number of pieces.
        int eval = (
            (board.IsInCheck() ? 2 : GetNumberOfSetBits(board.IsWhiteToMove ? whitePiecesBB : blackPiecesBB))
        ) * 25 * whiteToMove;

        // material
        for (int i = 0; i < 5; i++)
            eval += (pieces[i].Count - pieces[i + 6].Count) * pieceValues[i];

        // pawns
        ulong pawnFiles = 0;
        var wpCount = pieces[0].Count;
        for (int i = 0; i < wpCount; i++)
        {
            var pawn = pieces[0][i];
            // advance & structure
            eval += 10 * (2 * pawn.Square.Rank + GetNumberOfSetBits(whitePawnBB & BitboardHelper.GetPawnAttacks(pawn.Square, true)));
            // doubling
            pawnFiles |= 1U << pawn.Square.File;
        }
        eval -= wpCount - GetNumberOfSetBits(pawnFiles);
        pawnFiles = 0;
        var bpCount = pieces[6].Count;
        for (int i = 0; i < bpCount; i++)
        {
            var pawn = pieces[6][i];
            eval -= 10 * (2 * (7 - pawn.Square.Rank) + GetNumberOfSetBits(blackPawnBB & BitboardHelper.GetPawnAttacks(pawn.Square, false)));
            pawnFiles |= 1U << pawn.Square.File;
        }
        eval += bpCount - GetNumberOfSetBits(pawnFiles);

        // center occupancy
        // extended 4x4 center
        eval += (20 + (int)(openingWeight * 50)) * (
            GetNumberOfSetBits(whitePawnBB & 0x00003C3C3C3C0000) -
            GetNumberOfSetBits(blackPawnBB & 0x00003C3C3C3C0000)
        );
        // 2x2 center
        eval += (10 + (int)(openingWeight * 50)) * (
            GetNumberOfSetBits(whitePawnBB & 0x0000001818000000) -
            GetNumberOfSetBits(blackPawnBB & 0x0000001818000000)
        );

        // king protecting pawns / king pawn shield
        eval += 20 * (
            GetNumberOfSetBits(whitePawnBB & BitboardHelper.GetKingAttacks(board.GetKingSquare(true))) -
            GetNumberOfSetBits(blackPawnBB & BitboardHelper.GetKingAttacks(board.GetKingSquare(false)))
        );

        // development
        eval += (20 + (int)(openingWeight * 4)) * (
            GetNumberOfSetBits((whiteKnightBB | whiteBishopBB | whiteQueenBB) & 0xFFFFFFFFFFFF00) -
            GetNumberOfSetBits((blackKnightBB | blackBishopBB | blackQueenBB) & 0x00FFFFFFFFFFFF)
        );

        // edges are generally bad for these pieces
        eval += 30 * (
            GetNumberOfSetBits((blackKnightBB | blackBishopBB | blackQueenBB) & 0xFF818181818181FF) -
            GetNumberOfSetBits((whiteKnightBB | whiteBishopBB | whiteQueenBB) & 0xFF818181818181FF)
        );

        // Rooks are generally good on low ranks
        eval += 5 * (
            GetNumberOfSetBits(GetPieceBitboard(PieceType.Rook, true) & 0x00000000FFFFFFFF) -
            GetNumberOfSetBits(GetPieceBitboard(PieceType.Rook, false) & 0xFFFFFFFF00000000)
        );

        // Having both bishops is good
        if (pieces[2].Count > 1) eval += 5;
        if (pieces[8].Count > 1) eval -= 5;

        // signed for the side playing
        return eval * whiteToMove;
    }
}