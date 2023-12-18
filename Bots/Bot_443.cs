namespace auto_Bot_443;
using ChessChallenge.API;
using System;
// using System.Numerics;
using System.Collections.Generic;
using System.Linq;
using static ChessChallenge.API.PieceType;
using static System.BitConverter;
using static System.Math;

public class Bot_443 : IChessBot
{
    // ----------------
    // DATA CODE
    // ----------------

    // HexValue = TransformAndRightToLeft(DecValue + 100)
    private readonly int[][] POSITION_MAPS = {
        // DecValues: +00 +50 +10 +05 +00 +05 +05 +00
        // DecValues: +00 +50 +10 +05 +00 -05 +10 +00
        // DecValues: +00 +50 +20 +10 +00 -10 +10 +00
        // DecValues: +00 +50 +30 +25 +20 +00 -20 +00
        CreatePositionMap(0x64696964696E9664, 0x646E5F64696E9664, 0x646E5A646E789664, 0x645064787D829664),  // POSITION_MAP_PAWN_START

        // DecValues: -50 -40 -30 -30 -30 -30 -40 -50
        // DecValues: -40 -20 +00 +05 +00 +05 -20 -40
        // DecValues: -30 +00 +10 +15 +15 +10 +00 -30
        // DecValues: -30 +00 +15 +20 +20 +15 +05 -30
        CreatePositionMap(0x323C464646463C32, 0x323C464646463C32, 0x46646E73736E6446, 0x4669737878736446),  // POSITION_MAP_KNIGHT

        // DecValues: -20 -10 -10 -10 -10 -10 -10 -20
        // DecValues: -10 +00 +00 +05 +00 +10 +05 -10
        // DecValues: -10 +00 +05 +05 +10 +10 +00 -10
        // DecValues: -10 +00 +10 +10 +10 +10 +00 -10
        CreatePositionMap(0x505A5A5A5A5A5A50, 0x5A696E646964645A, 0x5A646E6E6969645A, 0x5A646E6E6E6E645A),  // POSITION_MAP_BISHOP

        // DecValues: +00 +05 -05 -05 -05 -05 -05 +00
        // DecValues: +00 +10 +00 +00 +00 +00 +00 +00
        // DecValues: +00 +10 +00 +00 +00 +00 +00 +00
        // DecValues: +00 +10 +00 +00 +00 +00 +00 +05
        CreatePositionMap(0x645F5F5F5F5F6964, 0x6464646464646E64, 0x6464646464646E64, 0x6964646464646E64),  // POSITION_MAP_ROOK

        // DecValues: -20 -10 -10 -05 +00 -10 -10 -20
        // DecValues: -10 +00 +00 +00 +00 +05 +00 -10
        // DecValues: -10 +00 +05 +05 +05 +05 +05 -10
        // DecValues: -05 +00 +05 +05 +05 +05 +00 -05
        CreatePositionMap(0x505A5A645F5A5A50, 0x5A6469646464645A, 0x5A6969696969645A, 0x5F6469696969645F),  // POSITION_MAP_QUEEN

        // DecValues: -80 -60 -40 -30 -20 -10 +20 +20
        // DecValues: -70 -60 -50 -40 -30 -20 +20 +30
        // DecValues: -70 -60 -50 -40 -30 -20 -05 +10
        // DecValues: -70 -60 -60 -50 -40 -20 -05 +00
        CreatePositionMap(0x78785A50463C2814, 0x827850463C32281E, 0x6E5F50463C32281E, 0x645F503C3228281E),  // POSITION_MAP_KING_START

        // DecValues: +00 +80 +50 +30 +20 +10 +10 +00
        // DecValues: +00 +80 +50 +30 +20 +10 +10 +00
        // DecValues: +00 +80 +50 +30 +20 +10 +10 +00
        // DecValues: +00 +80 +50 +30 +20 +10 +10 +00
        CreatePositionMap(0x646E6E788296B464, 0x646E6E788296B464, 0x646E6E788296B464, 0x646E6E788296B464),  // POSITION_MAP_PAWN_END

        // DecValues: -20 -05 -10 -15 -20 -25 -30 -50
        // DecValues: -10 +00 -05 -10 -15 -20 -25 -30
        // DecValues: -10 +05 +20 +35 +30 +20 +00 -30
        // DecValues: -10 +05 +30 +45 +40 +25 +00 -30
        CreatePositionMap(0x32464B50555A5F50, 0x464B50555A5F645A, 0x466478828778695A, 0x46647D8C9182695A)   // POSITION_MAP_KING_END
    };

    // NOTE: replaced const macros to reduce token count
    // private const int CHECKMATE_VALUE = 1000000000;
    // private const int MIN_WINNING_VALUE = 200;
    // private const int RATING_BIAS_CAPTURE_WIN = 8000000;
    // private const int RATING_BIAS_CAPTURE_LOSE = 2000000;
    // private const int RATING_BIAS_PROMOTE = 6000000;
    // private const int RATING_BIAS_CASTLE = 1000000;
    // private const int RATING_BIAS_ATTACKED = 50;
    // private const int KING_PUSH_TO_EDGE_FACTOR = 10;
    // private const int KING_MOVE_CLOSER_FACTOR = 4;
    // private const float MIN_THINK_TIME = 720.0F;
    // private const float ENDGAME_FACTOR_OFFSET = 1000.0F;
    // private readonly int[] PASSED_PAWN_VALUE = { 0, 120, 80, 50, 30, 15, 15 };
    private readonly PieceType[] EVAL_PIECE_TYPES = { Pawn, Knight, Bishop, Rook, Queen };

    private int GetPieceValue(PieceType pieceType) => pieceType switch
    {
        Pawn => 100,
        Knight => 300,
        Bishop => 320,
        Rook => 500,
        Queen => 900,
        King => 10000,
        _ => 0,
    };

    private float CalculateEndgameFactor(int piecesValueSum) => 1.0F - (piecesValueSum - 1000.0F) / 7880.0F;

    // ----------------
    // FUNCTION CODE
    // ----------------

    // private readonly ulong[] PASSED_PAWN_MASKS = new ulong[128];
    private readonly Dictionary<Move, int> MOVE_RATINGS_DICT = new();

    private Board board;
    private int minimaxDepth;
    private Move bestMoveOverall;
    private Move bestMoveInIteration;
    private bool isSearchCancelled;
    private float endgameFactor;

    Timer timer;
    int thinkTime;

    // NOTE: not enough tokens available, impact seems to be not that big
    // public Bot_443 ()
    // {
    //  // Init PASSED_PAWN_MASKS:
    //  for (int i = 0; i < 64; i++)
    //  {
    //      int rank = i >> 3, file = i & 0b000111;
    //      ulong adjacentFiles = 0x101010101010101ul << Max(0, file - 1) | 0x101010101010101ul << Min(7, file + 1);
    //      PASSED_PAWN_MASKS[i] = (0x101010101010101ul << file | adjacentFiles) & ~(ulong.MaxValue >> (64 - 8 * (rank + 1)));
    //      PASSED_PAWN_MASKS[i + 64] = (0x101010101010101ul << file | adjacentFiles) & (1ul << 8 * rank - 1);
    //  }
    // }

    private static int[] CreatePositionMap(ulong file1, ulong file2, ulong file3, ulong file4)
    {
        var byteArray = GetBytes(file1).Concat(GetBytes(file2)).Concat(GetBytes(file3)).Concat(GetBytes(file4)).ToArray();
        var map = new int[64];
        // Shift, mirror and transpose:
        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 8; j++)
                map[j * 8 + i] = map[j * 8 + 7 - i] = byteArray[i * 8 + j] - 100;
        return map;
    }

    // TODO: implement:
    // - Advance pawns when winning
    // - Queen should not move at start that much
    // - Avoid repetitions at start
    // - Store killer moves
    private int CalculateMoveRating(Move move)
    {
        if (move == bestMoveOverall)
            return 1000000000;

        int moveRating = 0;
        bool isTargetSquareAttacked = board.SquareIsAttackedByOpponent(move.TargetSquare);
        if (move.IsCapture)
        {
            moveRating += GetPieceValue(move.CapturePieceType) - GetPieceValue(move.MovePieceType);
            moveRating += moveRating < 0 && isTargetSquareAttacked ? 2000000 : 8000000;
        }
        else
        {
            if (move.IsPromotion)
                moveRating += 6000000;
            if (move.IsCastles)
                moveRating += 1000000;
            if (isTargetSquareAttacked)
                moveRating -= 50;
            int mapIdx = (int)move.MovePieceType - 1;
            if (mapIdx != 0 && mapIdx != 5)
                moveRating += POSITION_MAPS[mapIdx][move.TargetSquare.Index] - POSITION_MAPS[mapIdx][move.StartSquare.Index];
        }

        return moveRating;
    }

    private Move[] SortMoves(Move[] moves)
    {
        MOVE_RATINGS_DICT.Clear();
        foreach (var move in moves)
            MOVE_RATINGS_DICT[move] = CalculateMoveRating(move);
        Array.Sort(moves, (moveA, moveB) => MOVE_RATINGS_DICT[moveB].CompareTo(MOVE_RATINGS_DICT[moveA]));
        return moves;
    }

    private int EvaluatePieces(bool forWhite)
    {
        int eval = 0;
        foreach (var pieceType in EVAL_PIECE_TYPES)
            eval += GetPieceValue(pieceType) * board.GetPieceList(pieceType, forWhite).Count;
        return eval;
    }

    private int EvaluatePositions(bool forWhite)
    {
        int eval = 0;
        for (int mapIdxStart = 0; mapIdxStart < 6; mapIdxStart++)
        {
            int mapIdxEnd = mapIdxStart == 0 ? 6 : mapIdxStart == 5 ? 7 : -1;
            foreach (var piece in board.GetPieceList((PieceType)(mapIdxStart + 1), forWhite))
            {
                int index = forWhite ? piece.Square.Index : new Square(piece.Square.File, 7 - piece.Square.Rank).Index;
                eval += mapIdxEnd < 0 ? POSITION_MAPS[mapIdxStart][index] : (int)((1F - endgameFactor) * POSITION_MAPS[mapIdxStart][index] + endgameFactor * POSITION_MAPS[mapIdxEnd][index]);
            }
        }
        return eval;
    }

    // TODO: implement:
    // - King should be behind pawns
    // - Pawns should advance but be covered
    // - Punish isolated pawns
    private int EvaluateBoard()
    {
        bool forWhite = board.IsWhiteToMove;
        int evalPiecesWhite = EvaluatePieces(true), evalPiecesBlack = EvaluatePieces(false);
        endgameFactor = CalculateEndgameFactor(evalPiecesWhite + evalPiecesBlack);

        // Evaluate piece values and positions:
        int eval = evalPiecesWhite - evalPiecesBlack + EvaluatePositions(true) - EvaluatePositions(false);
        eval *= forWhite ? 1 : -1;

        // Evaluate kings:
        if (eval > 200)
        {
            var opponentKingSquare = board.GetKingSquare(!forWhite);
            var ownKingSquare = board.GetKingSquare(forWhite);
            int evalKings = 10 * (Max(3 - opponentKingSquare.File, opponentKingSquare.File - 4) + Max(3 - opponentKingSquare.Rank, opponentKingSquare.Rank - 4)) + 4 * (14 - Abs(ownKingSquare.File - opponentKingSquare.File) - Abs(ownKingSquare.Rank - opponentKingSquare.Rank));
            eval += (int)(endgameFactor * evalKings);
        }

        // Evaluate pawns:
        // NOTE: not enough tokens available, impact seems to be not that big
        // int passedPawnMaskIdxOffset = forWhite ? 0 : 64;
        // foreach (var piece in board.GetPieceList(Pawn, forWhite))
        //  if ((board.GetPieceBitboard(Pawn, !forWhite) & PASSED_PAWN_MASKS[piece.Square.Index + passedPawnMaskIdxOffset]) == 0ul)
        //      eval += PASSED_PAWN_VALUE[forWhite ? 7 - piece.Square.Rank : piece.Square.Rank];

        return eval;
    }

    // TODO: implement:
    //  - Transpositions
    //  - Add a small amount of openings via ulong values in
    //  - Search extension on checkmate
    //  - Late move search reduction
    private int MiniMax(int depth, int alpha, int beta)
    {
        // Modified to remove illegal namespace -- seb
        isSearchCancelled = timer.MillisecondsElapsedThisTurn >= thinkTime;
        if (isSearchCancelled)
            return 0;

        // Quiescence search:
        if (depth <= 0)
        {
            int eval = EvaluateBoard();
            if (eval >= beta)
                return beta;
            alpha = Max(alpha, eval);
        }

        if (board.IsInCheckmate())
            return -1000000000;
        if (board.IsDraw())
            return 0;

        var moves = SortMoves(board.GetLegalMoves(depth <= 0));
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -MiniMax(depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (isSearchCancelled)
                return 0;

            if (eval > alpha)
            {
                alpha = eval;
                if (depth == minimaxDepth)
                    bestMoveInIteration = move;
                if (alpha >= beta)
                    break;
            }
        }
        return alpha;
    }

    private int CalculateThinkTime(Timer timer)
    {
        float thinkTime = Min(720.0F, timer.MillisecondsRemaining / 40.0F);
        if (timer.MillisecondsRemaining > timer.IncrementMilliseconds * 2)
            thinkTime += timer.IncrementMilliseconds * 0.8F;
        return (int)Ceiling(Max(thinkTime, Min(60.0F, timer.MillisecondsRemaining * 0.25F)));
    }

    public Move Think(Board currentBoard, Timer timer)
    {
        var nullMove = Move.NullMove;
        board = currentBoard;
        bestMoveOverall = nullMove;
        isSearchCancelled = false;

        thinkTime = CalculateThinkTime(timer);
        this.timer = timer;

        for (minimaxDepth = 0; minimaxDepth < 128; minimaxDepth++)
        {
            bestMoveInIteration = nullMove;
            var lastEval = MiniMax(minimaxDepth, -1000000000, 1000000000);
            if (bestMoveInIteration != nullMove)
                bestMoveOverall = bestMoveInIteration;
            if (isSearchCancelled)
                break;
        }
        if (bestMoveOverall == nullMove)
            bestMoveOverall = SortMoves(board.GetLegalMoves())[0];
        return bestMoveOverall;
    }
}