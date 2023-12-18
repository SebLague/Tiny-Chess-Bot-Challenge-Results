namespace auto_Bot_179;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_179 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        double alpha = double.MinValue;
        double beta = double.MaxValue;
        Move bestMove = Move.NullMove;
        var sortedMoves = new List<(Move move, double value)>();
        for (int depth = 1; depth <= 11; depth++)
        {
            Move move = Minimax(board, ref sortedMoves, false, timer, depth, board.IsWhiteToMove, alpha, beta).move;
            if (NeedToStop(timer))
            {
                if (move != Move.NullMove)
                {
                    bestMove = move;
                }
                break;
            }

            bestMove = move;
        }

        return bestMove;
    }

    (double value, Move move) Minimax(Board board, ref List<(Move move, double value)> sortedMoves, bool capturesOnly, Timer timer, int depth,
        bool isMaximizingPlayer, double alpha, double beta)
    {
        Move[] moves = sortedMoves.Any() ? sortedMoves.Select(x => x.move).ToArray() : board.GetLegalMoves(capturesOnly);
        var newSortedMoves = new List<(Move move, double value)>();
        bool allMovesEvaluated = true;

        Random rng = new();

        foreach (var move in moves)
        {
            double value = 0d;
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                newSortedMoves.Add((move, isMaximizingPlayer ? 250d : -250d));

                // Pretend we evaluated all moves. Never mind when we skip the other moves.
                // We found a checkmate: Leave allMovesEvaluated at true.
                board.UndoMove(move);
                break;
            }
            else
            {
                bool allDepthsEvaluated = true;
                if (!board.IsDraw())
                {
                    if (depth == 1)
                    {
                        value = EvaluatePosition(board) + rng.Next(100) * 0.0005d;

                        // Sort captures by value of moving piece minus value of captured piece.
                        var sortedCaptureMoves = board.GetLegalMoves(true)
                            .OrderBy(y => PieceValue(y.MovePieceType) - PieceValue(y.CapturePieceType))
                            .Take(3)
                            .Select(z => (z, 0d)).ToList();

                        if (sortedCaptureMoves.Any())
                        {
                            var valueIncludingCaptures = Minimax(board, ref sortedCaptureMoves, true, timer, 1, !isMaximizingPlayer, alpha, beta).value;

                            // We are 1 ply deep in the current method, so it's now the opponent to move:
                            value = isMaximizingPlayer ? (valueIncludingCaptures < value ? valueIncludingCaptures : value) :
                                (valueIncludingCaptures > value ? valueIncludingCaptures : value);
                        }
                    }
                    else
                    {
                        var sortedMovesNextDepth = new List<(Move move, double value)>();
                        for (int i = 1; i <= depth - 1; ++i)
                        {
                            value = Minimax(board, ref sortedMovesNextDepth, capturesOnly, timer, i, !isMaximizingPlayer, alpha, beta).value;
                            if (NeedToStop(timer))
                            {
                                allDepthsEvaluated = false;
                                break;
                            }
                        }
                    }
                }
                if (allDepthsEvaluated)
                {
                    newSortedMoves.Add((move, value));
                }
            }

            board.UndoMove(move);

            if (isMaximizingPlayer)
            {
                alpha = value > alpha ? value : alpha;
            }
            else
            {
                beta = value < beta ? value : beta;
            }

            if (beta <= alpha || NeedToStop(timer))
            {
                allMovesEvaluated = false;
                break;
            }
        }

        newSortedMoves = (isMaximizingPlayer ?
            newSortedMoves.OrderByDescending(x => x.value) :
            newSortedMoves.OrderBy(x => x.value)).ToList();

        // Report sorted moves back when all moves are evaluated.
        if (allMovesEvaluated)
        {
            sortedMoves = newSortedMoves;
        }

        if (newSortedMoves.Any())
        {
            return (newSortedMoves.First().value, newSortedMoves.First().move);
        }

        return (0d, Move.NullMove);
    }

    private static bool NeedToStop(Timer timer)
    {
        return timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining * 0.033 ||
            timer.MillisecondsElapsedThisTurn > timer.GameStartTimeMilliseconds * 0.025;
    }

    private double EvaluatePosition(Board board)
    {
        double value = 0d;
        var allPieces = board.GetAllPieceLists();

        for (int i = 0; i < allPieces.Length; i++)
        {
            foreach (var piece in allPieces[i])
            {
                value += PieceValue(piece.PieceType) * (piece.IsWhite ? 1d : -1d) +
                (piece.IsPawn ? (piece.IsWhite ? -1d : -6d) + piece.Square.Rank : 0d) * 0.01;
            }
        }

        return value;
    }

    private static double PieceValue(PieceType pieceType)
    {
        var pieceValueDictionary = new Dictionary<PieceType, double>
        {
            {PieceType.Pawn, 1d},
            {PieceType.Knight, 2.8d},
            {PieceType.Bishop, 3.2d},
            {PieceType.Rook, 4.8d},
            {PieceType.Queen, 9.5d},
            {PieceType.King, 0d}
        };

        return pieceValueDictionary[pieceType];
    }
}