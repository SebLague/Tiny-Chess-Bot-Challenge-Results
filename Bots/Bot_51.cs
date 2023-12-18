namespace auto_Bot_51;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_51 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 200, 600, 750, 1000, 2000, 20000 };

    // Token shenanigans to cache my DST Table
    ulong[][] dstTable =
    {
        new ulong[] { 5555555555555555, 5555555555555555, 5555555555555555, 5555555555555555 },
        new ulong[] { 9999999977777777, 6666666655555555, 4444444433333333, 2222222211111111 },
        new ulong[] { 1222222123333332, 2344443223455432, 2345543223444432, 2333333212222221 },
        new ulong[] { 2222222223333332, 2344443223455432, 2345543223444432, 2333333222222222 },
        new ulong[] { 2222222233333333, 4444444455555555, 5555555544444444, 3333333322222222 },
        new ulong[] { 2334433234455443, 3455554345566554, 4556655434555543, 3445544323344332 },
        new ulong[] { 4444444444444444, 4444444444444444, 3333333322222222, 1111111100000000 }
    };

    // Individual weights for the sum of the DST Positions
    float[] dstMultipliers = new float[] { 1f, 1f, 0.4f, 0.5f, 0.5f, 0.5f, 0.5f };

    // Starting moves to not waist precious begining time
    int[] startingMoves = new int[] { 28, 36, 18, 42 };
    PieceType[] startingMovePieces = new PieceType[] { PieceType.Pawn, PieceType.Pawn, PieceType.Knight, PieceType.Knight };

    const int maxDepth = 16; // Starting search depth
    int currentDepth;
    bool _playingWhite;
    Dictionary<ulong, short> _transpositionTable = new Dictionary<ulong, short>();
    Dictionary<PieceType, float[]> _dstLookup = new Dictionary<PieceType, float[]>();
    ValueTuple<int, Move> bestMove;

    public Bot_51()
    {
        for (int i = 0; i < 7; i++)
        {
            var data = new List<float>();
            for (int j = 0; j < 4; j++) // Setup DST Lookup table data
            {
                data.AddRange(dstTable[i][j].ToString().Select(x => ((int)char.GetNumericValue(x) - 5) * dstMultipliers[i]));
            }
            _dstLookup[(PieceType)i] = data.ToArray();
        }
        GC.Collect();
    }

    public Move Think(Board board, Timer timer)
    {
        _playingWhite = board.IsWhiteToMove;

        // If first 4 moves, pick move from starting moves
        if (board.PlyCount < 4)
        {
            var moves = board.GetLegalMoves();
            return moves.First(x => x.MovePieceType == startingMovePieces[board.PlyCount] && x.TargetSquare.Index == startingMoves[board.PlyCount]);
        }
        bestMove = default;

        if (_transpositionTable.Count() > 1000000)
            _transpositionTable.Clear();
        // Lower search depth if low on time
        var depthToUse = timer.MillisecondsRemaining > 15000 ? maxDepth : 6;
        for (currentDepth = 5; currentDepth <= depthToUse; currentDepth++)
        {
            var moveData = AlphaBeta(board, currentDepth, int.MinValue, int.MaxValue, true, 0);
            bestMove = moveData;

            // If search is taking too long, break out at current depth
            if (timer.MillisecondsElapsedThisTurn >= currentDepth * 100 ||
                timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 10)
            {
                break;
            }
        }
        DivertedConsole.Write($"P: {bestMove.Item2} {currentDepth} {bestMove.Item1}");
        return bestMove.Item2;
    }

    private ValueTuple<int, Move> AlphaBeta(
        Board board,
        int depth,
        int alpha,
        int beta,
        bool isMaximizingPlayer,
        int currentIteration)
    {
        if (depth <= 0 && currentDepth < 7 && !board.IsInCheckmate() && board.IsInCheck() && currentIteration < currentDepth)
        {
            depth++; // If move is a check, increase the depth and continue searching
        }
        if (depth <= 0) // if at maximum depth or if the game is over
        {
            var val = EvaluateBoard(board, new Move[0], isMaximizingPlayer, depth);
            return new ValueTuple<int, Move>(val, Move.NullMove);
        }

        var moves = board.GetLegalMoves();
        if (moves.Length == 0)
        {
            return new ValueTuple<int, Move>(EvaluateBoard(board, moves, isMaximizingPlayer, depth), Move.NullMove);
        }

        // Simple move ordering - capture and promotion moves first.
        moves = moves.OrderByDescending(m => (currentIteration == 0 && m == bestMove.Item2) || m.IsCapture || m.IsPromotion).ToArray();

        if (isMaximizingPlayer)
        {
            int maxEval = int.MinValue;
            Move bestMove = Move.NullMove;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, false, currentIteration++).Item1;
                board.UndoMove(move);
                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break; // Beta cut-off
            }
            return new ValueTuple<int, Move>(maxEval, bestMove);
        }
        else
        {
            int minEval = int.MaxValue;
            Move bestMove = Move.NullMove;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval = AlphaBeta(board, depth - 1, alpha, beta, true, currentIteration++).Item1;
                board.UndoMove(move);
                if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                }
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break; // Alpha cut-off
            }
            return new ValueTuple<int, Move>(minEval, bestMove);
        }
    }

    private int EvaluateBoard(Board board, Move[] legalMoves, bool isMaximizingPlayer, int currentDepth)
    {
        if (_transpositionTable.ContainsKey(board.ZobristKey)) // Get cached move
        {
            return _transpositionTable[board.ZobristKey];
        }

        int score = 0;
        foreach (var piece in board.GetAllPieceLists())
        {
            if (piece.IsWhitePieceList == _playingWhite)
                score += SumPieces(piece);
            else
                score -= SumPieces(piece);
        }

        score += legalMoves.Sum(x => x.IsCapture ? 2 : 1);
        if (isMaximizingPlayer)
        {
            if (board.IsInCheckmate())
                score -= 1000000 * currentDepth;
            if (board.IsDraw())
                score -= 100000;
        }
        else
        {
            if (board.IsInCheck())
                score -= 100;

            if (board.IsInCheckmate()) // Multiply the score based on how few moves the checkmate requires
                score += 1000000 * currentDepth;
        }
        if (!board.IsInCheckmate())
            _transpositionTable[board.ZobristKey] = (short)score;
        return score;
    }

    // Sums all the pieces on the board, then sums the DST table values for each piece
    private int SumPieces(PieceList pieces) => pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList] +
            (int)pieces.Sum(piece => _dstLookup[pieces.TypeOfPieceInList][_playingWhite ? 63 - piece.Square.Index : piece.Square.Index]);

    ~Bot_51()
    {
        _dstLookup.Clear();
        _transpositionTable.Clear();
    }
}