namespace auto_Bot_282;
/****************************************************************************\
                            Hippo version 8.1
    Author: MonteDegro

    Credit to "Chess Programming" youtube channel, especially for the
    playlist "Bitboard Chess Engine in C" for great explanations and
    demonstrations of various heuristics and techniques.
\****************************************************************************/

using ChessChallenge.API;
using System;

public class Bot_282 : IChessBot
{
    // size = 24 bytes
    struct HashNode
    {
        public ulong HashKey;
        public int HashDepth, HashScore, HashFlag;
        public Move HashMove;

        public HashNode(ulong Key, int Depth, int Score, int Flag, Move MoveToStore)
        {
            HashKey = Key;
            HashDepth = Depth;
            HashScore = Score;
            HashFlag = Flag;
            HashMove = MoveToStore;
        }
    }

    HashNode[] HashTable = new HashNode[0x300000];

    static int MaxPly = 128, MaxExtensions = 8;
    static ulong HashEntries = 0x300000;

    int Ply, TimeLeft;

    int[] PieceValues = { 0, 100, 300, 320, 500, 900, 0 };

    int[,] HistoryBonus;

    int EvaluatePosition(Board board)
    {
        int Score = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                bool IsWhite = piece.IsWhite;

                Square EnemyKing = board.GetKingSquare(!IsWhite),
                       AllyKing = board.GetKingSquare(IsWhite),
                       PiecePos = piece.Square;

                ulong ControlBitboard = BitboardHelper.GetPieceAttacks(piece.PieceType,
                                                                       PiecePos,
                                                                       board,
                                                                       IsWhite);
                int Sign = piece.IsWhite ? 1 : -1,
                    Type = (int)piece.PieceType,
                    Mobility = BitboardHelper.GetNumberOfSetBits(ControlBitboard),
                    KingCtrl = BitboardHelper.GetNumberOfSetBits(ControlBitboard &
                                                                 BitboardHelper.GetKingAttacks(EnemyKing));

                Score += Sign * (PieceValues[Type] + 20 * Mobility + 10 * KingCtrl);

                if (piece.PieceType == PieceType.Pawn)
                {
                    if (IsWhite)
                        Score += 5 * PiecePos.Rank;
                    else
                        Score -= 40 - 5 * PiecePos.Rank;


                    Score += Sign * (BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(PiecePos) &
                                                                       board.GetPieceBitboard(PieceType.Pawn, IsWhite)) +
                                     BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPawnAttacks(PiecePos, IsWhite) &
                                                                       board.GetPieceBitboard(PieceType.Pawn, IsWhite))
                                     - Math.Abs(PiecePos.File - EnemyKing.File) - Math.Abs(PiecePos.Rank - EnemyKing.Rank));

                    if (Math.Abs(PiecePos.File - AllyKing.File) < 2 && PiecePos.Rank == AllyKing.Rank + Sign)
                        Score += Sign * 10;
                }
            }
        }

        return board.IsWhiteToMove ? 10 + Score : -10 - Score;
    }

    int EstimateMove(Board board, Move move)
    {
        ulong Key = board.ZobristKey;
        if (HashTable[Key % HashEntries].HashKey == Key)
        {
            if (HashTable[Key % HashEntries].HashMove == move)
                return 100000;
        }

        if (move.IsCapture)
            return 50000 + 100 * (int)move.CapturePieceType - (int)move.MovePieceType;

        if (move.IsPromotion)
            return 49999 + 100 * (int)move.PromotionPieceType;

        return HistoryBonus[(int)move.MovePieceType + (board.IsWhiteToMove ? 6 : 0), move.TargetSquare.Index];
    }

    Move[] GetSortedMoves(Board board, bool CapturesOnly)
    {
        Move[] Moves = board.GetLegalMoves(CapturesOnly);
        int[] Estimates = new int[Moves.Length];

        for (int Index = 0; Index < Moves.Length; Index++)
            Estimates[Index] = EstimateMove(board, Moves[Index]);

        Array.Sort(Estimates, Moves);
        Array.Reverse(Moves);

        return Moves;
    }

    int PVSearch(Board board, int Depth, int NumExtensions, int Alpha, int Beta)
    {
        if (Ply > 0 && board.IsRepeatedPosition())
            return 0;

        ulong Key = board.ZobristKey;
        HashNode HashEntry = HashTable[Key % HashEntries];
        if (HashEntry.HashKey == Key)
        {
            if (HashEntry.HashDepth >= Depth)
            {
                int HashScore = HashEntry.HashScore, HashFlag = HashEntry.HashFlag;

                if (HashFlag == 0)
                    return HashScore;

                if (HashFlag == 1 && HashScore <= Alpha)
                    return Alpha;

                if (HashFlag == -1 && HashScore >= Beta)
                    return Beta;
            }
        }

        int Score = EvaluatePosition(board), Flag = 1;

        bool IsCapturesOnly = Depth <= 0, IsPV = true;

        if (IsCapturesOnly)
        {
            Score = EvaluatePosition(board);
            if (Score >= Beta)
                return Beta;
            if (Score > Alpha)
                Alpha = Score;
        }
        else if (Ply > 0 && board.TrySkipTurn())
        {
            Ply++;
            Score = -PVSearch(board, Depth - 3, NumExtensions, -Beta, -Alpha);

            Ply--;
            board.UndoSkipTurn();

            if (Score >= Beta)
                return Beta;
        }

        Move[] Moves = GetSortedMoves(board, IsCapturesOnly);

        Move MoveToStore = new Move();

        foreach (Move move in Moves)
        {
            board.MakeMove(move);
            Ply++;

            int NextDepth = board.IsInCheck() && NumExtensions < MaxExtensions ? Depth : Depth - 1,
                NumNextExtensions = NextDepth == Depth ? NumExtensions + 1 : NumExtensions;

            if (IsPV)
                Score = -PVSearch(board, NextDepth, NumNextExtensions, -Beta, -Alpha);
            else
            {
                Score = -PVSearch(board, NextDepth, NumNextExtensions, -Alpha - 1, -Alpha);

                if (Score > Alpha && Score <= Beta)
                    Score = -PVSearch(board, NextDepth, NumNextExtensions, -Beta, -Alpha);
            }

            board.UndoMove(move);
            Ply--;

            if (Score > Alpha)
            {
                Alpha = Score;

                Flag = 0;

                MoveToStore = move;

                HistoryBonus[(int)move.MovePieceType + (board.IsWhiteToMove ? 6 : 0), move.TargetSquare.Index] += Ply * Ply;

                if (Alpha >= Beta)
                {
                    Flag = -1;

                    break;
                }
            }
        }

        if (!IsCapturesOnly && Moves.Length == 0)
            return board.IsInCheckmate() ? -30000 + Ply : 0;

        HashTable[Key % HashEntries] = new HashNode(Key, Depth, Alpha, Flag, MoveToStore);

        return Alpha;
    }

    public Move Think(Board board, Timer timer)
    {
        TimeLeft = timer.MillisecondsRemaining;
        int Depth = 1;

        Array.Clear(HashTable);

        HistoryBonus = new int[13, 64];

        Ply = 0;

        while (Depth < MaxPly)
        {
            if (timer.MillisecondsElapsedThisTurn > TimeLeft / 200)
                break;

            PVSearch(board, Depth++, 0, -30000, 30000);
        }

        return HashTable[board.ZobristKey % HashEntries].HashMove;
    }
}