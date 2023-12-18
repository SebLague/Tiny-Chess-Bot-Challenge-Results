namespace auto_Bot_166;
/* WYRM */
// by Finian P.

//Made with help from JacquesRW's Example Bot

using ChessChallenge.API;
using System;

public class Bot_166 : IChessBot
{
    Board globalBoard;

    Timer globalTimer;
    int timeToThink;

    Move bestMoveRoot;

    static ulong ttSize = 2000000;
    Memory[] tt = new Memory[ttSize];

    int[] pieceValues = { 0, 80, 295, 300, 470, 950, 375 }; //0, 100, 325, 350, 500, 975

    ulong pawnAttackBBWhite, pawnAttackBBBlack = 0;

    int[,,] pstWhite = new int[7, 8, 8];
    int[,,] pstBlack = new int[7, 8, 8];

    int BoolToSign(bool b) => b ? 1 : -1;

    public Move Think(Board board, Timer timer)
    {
        globalBoard = board;

        globalTimer = timer;

        CreatePawnAttackBB(true);
        CreatePawnAttackBB(false);

        CreatePST(true);
        CreatePST(false);

        timeToThink = Math.Min(timer.MillisecondsRemaining / 30 + timer.IncrementMilliseconds,
            timer.MillisecondsRemaining / 2);

        for (int depth = 0; depth < 50; depth++)
        {
            int score = SearchEval(-30000, 30000, depth, 0);

            // DivertedConsole.Write("Depth " + depth + ", eval: " + score + ", " + bestMoveRoot + ", time: " + timer.MillisecondsElapsedThisTurn); //#DEBUG

            if (timer.MillisecondsElapsedThisTurn >= timeToThink) break;
        }

        return bestMoveRoot;
    }

    void CreatePawnAttackBB(bool isWhiteBB)
    {
        ulong pawnAttackBB = 0;
        PieceList pawns = globalBoard.GetPieceList(PieceType.Pawn, isWhiteBB);
        for (int i = 0; i < pawns.Count; i++)
            pawnAttackBB |= BitboardHelper.GetPawnAttacks(pawns.GetPiece(i).Square, isWhiteBB);
        if (isWhiteBB) pawnAttackBBWhite = pawnAttackBB;
        else pawnAttackBBBlack = pawnAttackBB;
    }

    void CreatePST(bool isWhitePST)
    {
        int[,,] pst = isWhitePST ? pstWhite : pstBlack;

        ulong friendlyPawnBB = globalBoard.GetPieceBitboard(PieceType.Pawn, isWhitePST);

        for (int pieceTypeInt = 1; pieceTypeInt <= 6; pieceTypeInt++)
        {
            for (int file = 0; file < 8; file++)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    int numAdvanced = isWhitePST ? rank : (7 - rank);

                    Square square = new(file, rank);

                    int points = pieceValues[pieceTypeInt];

                    if (pieceTypeInt == 1)
                        points += (int)Math.Pow(numAdvanced, 2.5 - Math.Abs(file - 3.5) / 3 + numAdvanced / 15)
                               // + BitboardHelper.GetNumberOfSetBits((ulong)(0b0000000100000001000000010000000100000001000000010000000100000001 << file) & friendlyPawnAttackBB) * 5
                               + (BitboardHelper.SquareIsSet(isWhitePST ? pawnAttackBBWhite : pawnAttackBBBlack, square) ? 5 : 0)
                               + BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPawnAttacks(square, isWhitePST) & friendlyPawnBB) * 3;
                    // + (BitboardHelper.SquareIsSet(isWhitePST ? pawnAttackBBBlack : pawnAttackBBWhite, square) ? 2 : 0);

                    else
                        points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(
                            (PieceType)pieceTypeInt,
                            square,
                            friendlyPawnBB | globalBoard.GetPieceBitboard(PieceType.Pawn, !isWhitePST),
                            isWhitePST
                            ) & (ulong.MaxValue ^ friendlyPawnBB)) * 1500 / pieceValues[pieceTypeInt];

                    if (pieceTypeInt == 6) points -= pst[4, file, Math.Clamp(rank + BoolToSign(isWhitePST), 0, 7)];

                    pst[pieceTypeInt, file, rank] = points;
                }
            }
        }
    }

    struct Memory
    {
        public ulong key;
        public Move move;
        public int value, depth, type; /* -1 = Upper bound, 0 = exact, 1 = Lower bound */
        public Memory(ulong key_, int type_, int value_, int depth_, Move move_)
        {
            (key, type, value, depth, move) = (key_, type_, value_, depth_, move_);
        }
    }

    int SearchEval(int alpha, int beta, int depth, int plyFromRoot)
    {
        if (globalBoard.IsInCheckmate()) return -30000 + plyFromRoot;
        if (globalBoard.IsDraw()) return 0;

        ulong zobristKey = globalBoard.ZobristKey;

        Memory ttMemory = tt[zobristKey % ttSize];
        if (ttMemory.key == zobristKey && ttMemory.depth >= depth)
        {
            if (ttMemory.type == 0)
            {
                if (plyFromRoot == 0) bestMoveRoot = ttMemory.move;
                return ttMemory.value;
            }
            if (ttMemory.type == -1 && ttMemory.value <= alpha) return alpha;
            if (ttMemory.type == 1 && ttMemory.value >= beta) return beta;
        }

        bool isQ = depth <= 0;

        int memoryType = -1;

        if (isQ)
        {
            int eval = Evaluate();
            if (eval >= beta)
            {
                tt[zobristKey % ttSize] = new Memory(zobristKey, 1, beta, depth, Move.NullMove);
                return beta;
            }
            if (eval > alpha)
            {
                memoryType = 0;
                alpha = eval;
            }
        }

        Move[] possibleMoves = globalBoard.GetLegalMoves(isQ);

        int[] scores = new int[possibleMoves.Length];
        for (int i = 0; i < possibleMoves.Length; i++)
        {
            Move moveToScore = possibleMoves[i];
            if (moveToScore == ttMemory.move) scores[i] = 1000000;
            else if (moveToScore.IsCapture) scores[i] = pieceValues[(int)moveToScore.CapturePieceType] - pieceValues[(int)moveToScore.MovePieceType];
            else scores[i] = BitboardHelper.SquareIsSet(globalBoard.IsWhiteToMove ? pawnAttackBBBlack : pawnAttackBBWhite, moveToScore.TargetSquare) ? -20000 : 0;
        }

        Move recordMove = Move.NullMove;
        for (int i = 0; i < possibleMoves.Length; i++)
        {
            for (int j = i + 1; j < possibleMoves.Length; j++)
                if (scores[j] > scores[i])
                    (scores[i], scores[j], possibleMoves[i], possibleMoves[j]) = (scores[j], scores[i], possibleMoves[j], possibleMoves[i]);

            Move m = possibleMoves[i];

            globalBoard.MakeMove(m);
            int eval = -SearchEval(-beta, -alpha, depth - (globalBoard.IsInCheck() ? 0 : 1), plyFromRoot + 1);
            globalBoard.UndoMove(m);

            if (globalTimer.MillisecondsElapsedThisTurn >= timeToThink) return 0;

            if (eval >= beta)
            {
                tt[zobristKey % ttSize] = new Memory(zobristKey, 1, beta, depth, recordMove);
                return beta;
            }
            if (eval > alpha)
            {
                memoryType = 0;
                recordMove = m;
                if (plyFromRoot == 0) bestMoveRoot = m;
                alpha = eval;
            }
        }

        tt[zobristKey % ttSize] = new Memory(zobristKey, memoryType, alpha, isQ ? 0 : depth, recordMove);

        return alpha;
    }

    int Evaluate()
    {
        int eval = 0;

        foreach (PieceList pl in globalBoard.GetAllPieceLists())
        {
            int[,,] pst = pl.IsWhitePieceList ? pstWhite : pstBlack;

            for (int i = 0; i < pl.Count; i++)
            {
                Piece p = pl.GetPiece(i);
                eval += pst[(int)p.PieceType, p.Square.File, p.Square.Rank]
                     * BoolToSign(pl.IsWhitePieceList);
            }
        }

        return eval * BoolToSign(globalBoard.IsWhiteToMove);
    }

}
