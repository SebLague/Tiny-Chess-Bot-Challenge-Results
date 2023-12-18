namespace auto_Bot_116;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_116 : IChessBot
{

    static ulong GetMyBitBoard(Board board, bool playingAsWhite)
    {
        if (playingAsWhite) { return board.WhitePiecesBitboard; }
        return board.BlackPiecesBitboard;
    }

    static ulong FlipVertical(ulong x)
    {
        const ulong k1 = 0x00FF00FF00FF00FF;
        const ulong k2 = 0x0000FFFF0000FFFF;
        x = ((x >> 8) & k1) | ((x & k1) << 8);
        x = ((x >> 16) & k2) | ((x & k2) << 16);
        x = (x >> 32) | (x << 32);
        return x;
    }

    static int NumberOfSetBits(ulong i)
    {
        i -= (i >> 1) & 0x5555555555555555UL;
        i = (i & 0x3333333333333333UL) + ((i >> 2) & 0x3333333333333333UL);
        return (int)(unchecked(((i + (i >> 4)) & 0xF0F0F0F0F0F0F0FUL) * 0x101010101010101UL) >> 56);
    }


    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };

    public Move Think(Board board, Timer timer)
    {
        double thinkingTime = 1000;
        bool playingAsWhite = board.IsWhiteToMove;
        ulong opponentBitBoardFlipped = FlipVertical(GetMyBitBoard(board, !playingAsWhite));
        Move[] attackingMoves = board.GetLegalMoves(true);
        Move[] moves = board.GetLegalMoves();
        Random rng = new();
        Move bestMove = moves[rng.Next(moves.Length)];

        int bestErrorTerm = NumberOfSetBits(~GetMyBitBoard(board, playingAsWhite) & opponentBitBoardFlipped);
        int bestCaptureValue = 0;
        int i = 0;
        while (i < moves.Length & timer.MillisecondsElapsedThisTurn < thinkingTime & bestErrorTerm != 0)
        {
            if (MoveIsCheckmate(board, moves[i])) { bestMove = moves[i]; break; } // Checkmate
            int capturedPieceValue = 0;
            if (attackingMoves.Contains(moves[i]))
            {
                capturedPieceValue = pieceValues[(int)moves[i].CapturePieceType];
            }
            board.MakeMove(moves[i]);
            int errorTerm = NumberOfSetBits(~GetMyBitBoard(board, playingAsWhite) & opponentBitBoardFlipped);
            if (errorTerm < bestErrorTerm | capturedPieceValue > bestCaptureValue) { bestMove = moves[i]; }
            board.UndoMove(moves[i]);
            i++;
        }
        return bestMove;
    }
}