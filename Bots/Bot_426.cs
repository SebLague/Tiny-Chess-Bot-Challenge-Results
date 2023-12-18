namespace auto_Bot_426;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_426 : IChessBot
{
    float evaluation = 0f;
    Random rnd = new Random();
    int timeLeftAtStart;

    int[] pieceValuesByType = { 0, 10, 30, 30, 50, 90, 1000 };

    public float GetEvaluation()
    {
        return evaluation;
    }

    public Move Think(Board board, Timer timer)
    {
        timeLeftAtStart = timer.MillisecondsRemaining;

        int depth = 3;

        if (GamePhase(board) > 0.05f && timeLeftAtStart > 20000)
            depth += 1;

        if (board.IsRepeatedPosition())
            depth += 1;
        DivertedConsole.Write(depth);

        (Move, float) bestMove = GetBestMove(board, float.MinValue, float.MaxValue, depth, timer);
        evaluation = bestMove.Item2;
        return bestMove.Item1;
    }

    ///<summary>
    ///Uses a Min Max algorithm with alpha beta pruning to calculate the best move at a given depth
    ///</summary>
    (Move, float) GetBestMove(Board board, float alpha, float beta, int depth, Timer timer)
    {
        List<Move> allMoves = board.GetLegalMoves().ToList();
        allMoves = allMoves.OrderBy(x => rnd.Next()).ToList();
        allMoves = allMoves.OrderByDescending(x => MovePriority(board, x)).ToList();

        float bestScore = board.IsWhiteToMove ? float.MinValue : float.MaxValue;
        Move bestMove = allMoves[0];
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            float score;
            if (depth == 0 || board.IsInsufficientMaterial() || board.IsInCheckmate() || board.GetLegalMoves().Length == 0)
                score = Evaulate(board);
            else
            {
                score = GetBestMove(board, alpha, beta, depth - 1, timer).Item2;
            }
            board.UndoMove(move);
            if ((board.IsWhiteToMove && score > bestScore) || (!board.IsWhiteToMove && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
            if (board.IsWhiteToMove && score > alpha) alpha = score;
            if (!board.IsWhiteToMove && score < beta) beta = score;
            if (beta < alpha) break;

            //break out of the loop and return the current best move if half the available time has passed
            if (timer.MillisecondsElapsedThisTurn > timeLeftAtStart / 3f)
            {
                //DivertedConsole.Write("stopped searching because of time");
                break;
            }
        }

        return (bestMove, bestScore);
    }

    float Evaulate(Board board)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? float.MinValue : float.MaxValue;
        if (board.IsInsufficientMaterial()) return 0;
        if (board.GetLegalMoves().Length == 0) return 0;
        if (board.IsRepeatedPosition()) return 0;// not sure if this is right because it's not counting the number of repetitions

        float result = 0;

        foreach (Piece piece in board.GetAllPieceLists().SelectMany(x => x))
        {
            float pieceValue = CalculatePieceValue(board, piece);
            if (piece.IsWhite) result += pieceValue;
            else result -= pieceValue;
        }

        return result;
    }

    int MovePriority(Board board, Move move)
    {
        int score = 0;

        board.MakeMove(move);
        if (board.IsInCheck()) score += 5;
        board.UndoMove(move);

        return score + (int)board.GetPiece(move.TargetSquare).PieceType;
    }

    /// <summary>Returns 0-1 depending on the number of pieces left on the board</summary>
    float GamePhase(Board board)
    {
        return Map(
            board.GetAllPieceLists().SelectMany(x => x).Count(),
            2, 32, 1, 0);
    }

    float Map(float value, float old_min, float old_max, float new_min, float new_max)
    {
        return new_min + (value - old_min) * (new_max - new_min) / (old_max - old_min);
    }

    float Lerp(float a, float b, float f)
    {
        return a + f * (b - a);
    }

    /// <summary>Converts an int from 0-7 to 0-3-0</summary>
    int Fold8(int x)
    {
        return (int)-Math.Abs(0.86f * x - 3) + 3;
    }

    float CalculatePieceValue(Board board, Piece piece)
    {
        int typeValue = pieceValuesByType[(int)piece.PieceType];

        ulong attackedSquares = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, piece.IsWhite);

        int attackedSquaresValue = 0;

        for (int file = 0; file < 8; file++)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                Square currentSquare = new Square(rank, file);

                if (BitboardHelper.SquareIsSet(attackedSquares, currentSquare))
                    attackedSquaresValue += SquareValue(board, currentSquare);
            }
        }

        return typeValue + attackedSquaresValue; // TODO: I also need to add Swap-off Value
    }

    int SquareValue(Board board, Square square)
    {
        Square enemyKingPosition = board.GetKingSquare(!board.IsWhiteToMove);

        if (Math.Abs(square.File - enemyKingPosition.File) <= 1 && Math.Abs(square.Rank - enemyKingPosition.Rank) <= 1)
            return 3;

        if (Fold8(square.File) == 3 && Fold8(square.Rank) == 3)
            return 2;

        return 1;
    }
}