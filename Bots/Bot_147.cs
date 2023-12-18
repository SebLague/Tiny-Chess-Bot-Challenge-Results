namespace auto_Bot_147;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_147 : IChessBot
{
    int[] pieceValues = { 0, 100, 325, 325, 550, 1000, 50000 };

    static int maxDepth = 5;
    Dictionary<ulong, Move> PVTable = new Dictionary<ulong, Move>();
    int initPly = 0;

    int[,] MvvLvaScores = new int[13, 13];
    int[,] searchHistory = new int[13, 64];
    Move[,] searchKillers = new Move[2, maxDepth + 1];

    public Bot_147()
    {
        for (int attacker = 1; attacker <= 6; attacker++)
            for (int victim = 1; victim <= 6; victim++)
                MvvLvaScores[victim, attacker] = victim * 100 + 6 - attacker;
    }

    public Move Think(Board board, Timer timer)
    {
        initPly = board.PlyCount;

        // ---------- ClearForSearch ---------- //
        for (int i = 0; i < 13; i++)
        {
            for (int j = 0; j < 64; j++)
            {
                searchHistory[i, j] = 0;
                searchKillers[i % 2, j % maxDepth] = Move.NullMove;
            }
        }

        PVTable.Clear();
        // ---------- ClearForSearch ---------- //

        Move bestMove = Move.NullMove;
        for (int depth = 1; depth <= maxDepth; depth++)
        {
            QuiescenceOrAlphaBeta(-99999999, 999999999, depth, board, timer, false);
            bestMove = PVTable.GetValueOrDefault(board.ZobristKey, Move.NullMove);
        }

        return bestMove.IsNull ? board.GetLegalMoves()[0] : bestMove;
    }

    int QuiescenceOrAlphaBeta(int alpha, int beta, int depth, Board board, Timer timer, bool isQuiescence)
    {
        int searchPly = board.PlyCount - initPly;

        if (board.IsDraw())
            return 999999999;

        int timeToPlay = (timer.MillisecondsRemaining <= 5000) ? 250 : 1000;
        if (!isQuiescence && (depth == 0 || timer.MillisecondsElapsedThisTurn >= timeToPlay))
            return QuiescenceOrAlphaBeta(alpha, beta, depth, board, timer, true);

        if (isQuiescence)
        {
            int boardScore = GetBoardScore(board);

            if (searchPly > maxDepth)
                return boardScore;

            if (boardScore >= beta)
                return beta;

            if (boardScore > alpha)
                alpha = boardScore;
        }

        Move[] moves = board.GetLegalMoves(isQuiescence);
        int oldAlpha = alpha;
        Move bestMove = Move.NullMove;
        Move PVMove = PVTable.GetValueOrDefault(board.ZobristKey, Move.NullMove);

        var moveListScores = new KeyValuePair<Move, int>[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int score = searchHistory[(int)move.MovePieceType, move.TargetSquare.Index];
            if (move.IsCapture)
                score = MvvLvaScores[(int)move.CapturePieceType, (int)move.MovePieceType] + 1000000;
            else if (searchKillers[0, searchPly] == move)
                score = 900000;
            else if (searchKillers[1, searchPly] == move)
                score = 800000;

            moveListScores[i] = new(move, score);
        }

        if (!isQuiescence)
        {
            if (PVMove != Move.NullMove)
            {
                for (int i = 0; i < moveListScores.Length; i++)
                {
                    if (moveListScores[i].Key == PVMove)
                    {
                        moveListScores[i] = new(PVMove, 2000000);
                        break;
                    }
                }
            }

            if (moves.Length == 0)
                return board.IsInCheck() ? -100000 - depth : 0;
        }

        for (int i = 0; i < moveListScores.Length; i++)
        {
            // ---------- PickNextMove ---------- //
            int bestScore = 0;
            int bestIndex = 0;

            for (int j = i; j < moveListScores.Length; j++)
            {
                if (moveListScores[j].Value > bestScore)
                {
                    bestScore = moveListScores[j].Value;
                    bestIndex = j;
                }
            }

            var temp = moveListScores[i];
            moveListScores[i] = moveListScores[bestIndex];
            moveListScores[bestIndex] = temp;
            // ---------- PickNextMove ---------- //

            Move move = moveListScores[i].Key;

            board.MakeMove(move);
            int score = -QuiescenceOrAlphaBeta(-beta, -alpha, depth - 1, board, timer, isQuiescence);
            board.UndoMove(move);

            if (score > alpha)
            {
                if (score >= beta)
                {
                    if (!isQuiescence && !move.IsCapture)
                    {
                        searchKillers[1, searchPly] = searchKillers[0, searchPly];
                        searchKillers[0, searchPly] = move;
                    }

                    return beta;
                }
                alpha = score;
                bestMove = move;

                if (!isQuiescence && !move.IsCapture)
                    searchHistory[(int)move.MovePieceType, move.TargetSquare.Index] += depth;
            }
        }

        if (alpha != oldAlpha)
            if (!PVTable.TryAdd(board.ZobristKey, bestMove))
                PVTable[board.ZobristKey] = bestMove;

        return alpha;
    }

    int GetBoardScore(Board board)
    {
        int score = 0;
        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                Square square = new(file, rank);
                Piece piece = board.GetPiece(square);
                if (piece != null)
                {
                    int pieceValue = pieceValues[(int)piece.PieceType] + PieceEvaluation(piece, square);
                    score += pieceValue * (piece.IsWhite ? 1 : -1);
                }
            }
        }

        return score * (board.IsWhiteToMove ? 1 : -1);
    }

    int PieceEvaluation(Piece piece, Square square)
    {
        int distanceFromCenterX = (int)Math.Abs(square.File - 3.5);
        int distanceFromCenterY = (int)Math.Abs(square.Rank - 3.5);
        int distanceFromBackrank = piece.IsWhite ? 7 - square.Rank : square.Rank;
        int distanceFromCenter = distanceFromCenterX + distanceFromCenterY;

        if (piece.IsPawn)
            return (distanceFromBackrank == 4 && distanceFromCenterX == 0) ? 60 : Math.Max(0, 20 - distanceFromBackrank * 5);

        if (piece.IsBishop || piece.IsKnight)
            return Math.Max(0, 20 - distanceFromCenter * 5);

        if (piece.IsRook)
            return distanceFromBackrank == 1 ? 25 : Math.Max(0, 10 - distanceFromCenter * 5);

        if (piece.IsQueen)
            return 0;

        if (piece.IsKing)
            return distanceFromBackrank == 7 ? 0 : -70;

        return 0;
    }
}