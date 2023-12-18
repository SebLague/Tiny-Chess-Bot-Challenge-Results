namespace auto_Bot_139;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_139 : IChessBot
{
    private readonly Dictionary<ulong, (int score, int depth, Move bestMove)> _tTable = new();
    private readonly Dictionary<int, List<Move>> _killerMoves = new();

    private readonly int[] _pieceValues = { 0, 100, 320, 330, 500, 900, 100000 };

    private bool botCol;

    public Move Think(Board board, Timer timer)
    {
        _tTable.Clear();
        _killerMoves.Clear();

        botCol = board.IsWhiteToMove;

        if (board.AllPiecesBitboard == 18446462598732906495 || board.AllPiecesBitboard == 18446462599001337855)
        {
            return botCol ?
                new("e2e4", board) :
                new("d7d5", board);
        }

        int pieceCount = 0;
        foreach (var pl in board.GetAllPieceLists())
        {
            pieceCount += pl.Count;
        }

        // increase depth as pieceCount decreases
        return Minimax(board, Max(4, -pieceCount / 8 + 7), -2147483648, 2147483647, true).move;
    }

    private (int score, Move move) Minimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        ulong zobrist = board.ZobristKey;

        if (_tTable.TryGetValue(zobrist, out var entry) && entry.depth >= depth)
        {
            return (entry.score, entry.bestMove);
        }

        if (board.IsInCheckmate())
        {
            _tTable[zobrist] = (2147483647, depth, new());
            return (2147483647, new());
        }

        if (board.IsDraw())
        {
            _tTable[zobrist] = (0, depth, new());
            return (0, new());
        }

        if (depth <= 0)
        {
            if (board.IsInCheck())
            {
                depth++;
            }
            else
            {
                int score = EvaluateBoard(board);
                _tTable[zobrist] = (score, depth, new());
                return (score, new());
            }
        }

        // move ordering / evaluation
        Move[] moves = board.GetLegalMoves();
        List<Move> currKillerMoves = _killerMoves.ContainsKey(depth) ? _killerMoves[depth] : new();
        Dictionary<Move, int> moveScores = new(moves.Length);
        foreach (var move in moves)
        {
            int moveScore = 0;
            PieceType movePieceType = board.GetPiece(move.StartSquare).PieceType,
                capturePieceType = board.GetPiece(move.TargetSquare).PieceType;

            if (capturePieceType != PieceType.None)
            {
                moveScore = movePieceType == PieceType.King ?
                    _pieceValues[(int)movePieceType] :
                    10 * _pieceValues[(int)capturePieceType] - _pieceValues[(int)movePieceType];
            }

            if (move.IsPromotion)
            {
                moveScore += 25 * _pieceValues[(int)move.PromotionPieceType];
            }

            if (currKillerMoves.Contains(move))
            {
                moveScore += 1024;
            }

            moveScores[move] = moveScore;
        }
        Array.Sort(moves, (a, b) => moveScores[b] - moveScores[a]);

        // minimax
        Move bestMove = new();
        int bestScore = maximizingPlayer ? -2147483648 : 2147483647;
        foreach (var move in moves)
        {
            board.MakeMove(move);
            int score = Minimax(board, depth - 1, alpha, beta, !maximizingPlayer).score;
            board.UndoMove(move);

            if (maximizingPlayer ? score > bestScore : score < bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            if (maximizingPlayer)
            {
                alpha = Max(alpha, bestScore);
                if (beta <= alpha)
                {
                    UpdateKillerMoves(depth, move);
                    break;
                }
            }
            else
            {
                beta = Min(beta, bestScore);
                if (beta <= alpha)
                {
                    UpdateKillerMoves(depth, move);
                    break;
                }
            }
        }

        _tTable[zobrist] = (bestScore, depth, bestMove);
        return (bestScore, bestMove);
    }

    private int EvaluateBoard(Board board)
    {
        Square ekSquare = board.GetKingSquare(botCol);
        int ekRank = ekSquare.Rank, ekFile = ekSquare.File,
            totalScore = 0,
            ePieceCount = 0;

        foreach (var pl in board.GetAllPieceLists())
        {
            foreach (var p in pl)
            {
                Square square = p.Square;
                int pieceValue = 2 * _pieceValues[(int)p.PieceType],
                    r = square.Rank,
                    f = square.File,
                    rd = Abs(r - ekRank),
                    fd = Abs(f - ekFile),
                    piece = (int)p.PieceType,
                    centerScore = 5 * (10 - (Abs(f - 3) + Abs(r - 3))),
                    attackCount = BitboardHelper.GetNumberOfSetBits(piece switch
                    {
                        1 => BitboardHelper.GetPawnAttacks(square, botCol),
                        2 => BitboardHelper.GetKnightAttacks(square),
                        3 => BitboardHelper.GetSliderAttacks(PieceType.Rook, square, board),
                        4 => BitboardHelper.GetSliderAttacks(PieceType.Bishop, square, board),
                        5 => BitboardHelper.GetSliderAttacks(PieceType.Queen, square, board),
                        6 => BitboardHelper.GetKingAttacks(square),
                    } & board.AllPiecesBitboard),
                    attackScore = attackCount == 0 ? -1 : 3 * attackCount;


                bool isFriendly = p.IsWhite == botCol,
                     rowDistEqualsFileDist = rd == fd,
                     isSameRankOrFileWithEk = r == ekRank || f == ekFile;

                ePieceCount += isFriendly ? 0 : 1;

                totalScore += piece switch
                {
                    // reward for pushing up the board
                    1 => 5 * (10 - Abs(3 - (isFriendly ? r : 7 - r))),

                    // reward for being closer to the center
                    2 => 2 * centerScore,

                    // reward for pinning pieces/checking and for being closer to edges
                    3 => (isSameRankOrFileWithEk ? 5 : 0)
                        + 10 - Min(Min(f, 7 - f), Min(r, 7 - r)),

                    // reward for pinning pieces/checking and for being closer to the center
                    4 => (rowDistEqualsFileDist ? 5 : 0)
                        + centerScore,

                    // reward for pinning pieces/checking
                    5 => isSameRankOrFileWithEk || rowDistEqualsFileDist ? 5 : 0,

                    // reward for being in a 'good' position
                    6 => 10 - Max(Abs(4 - r), Abs(7 - r)) - attackScore,
                } + (isFriendly ? pieceValue : -pieceValue) + attackScore;
            }
        }

        // reward for having the enemy king closer to the corner as the game progresses
        if (ePieceCount < 6)
        {
            totalScore += 10 - Min(Min(ekRank, 7 - ekRank), Min(ekFile, 7 - ekFile));
        }

        // penalize for pieces still being in their original places
        //totalScore -= BitboardHelper.GetNumberOfSetBits(botStartBitboard & (!botCol ? board.WhitePiecesBitboard : board.BlackPiecesBitboard)) / (64 / ePieceCount);

        return totalScore;
    }

    private void UpdateKillerMoves(int depth, Move move)
    {
        if (!_killerMoves.ContainsKey(depth))
        {
            _killerMoves[depth] = new();
        }

        if (!_killerMoves[depth].Contains(move))
        {
            _killerMoves[depth].Insert(0, move);
            if (_killerMoves[depth].Count > 2)
            {
                _killerMoves[depth].RemoveAt(2);
            }
        }
    }

    // save some brain capacity
    private int Abs(int a) => Math.Abs(a);
    private int Min(int a, int b) => Math.Min(a, b);
    private int Max(int a, int b) => Math.Max(a, b);
}