namespace auto_Bot_329;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

class Bot_329 : IChessBot
{

    public Move Think(Board board, Timer timer)
    {
        long remainingTime = timer.MillisecondsRemaining;
        int depth = remainingTime < 5000 ? 3 : remainingTime < 10000 ? 4 : remainingTime < 20000 ? 5 : 4;
        return MiniMax(board, depth, double.MinValue, double.MaxValue, board.IsWhiteToMove).Item2;
    }

    static double[][] A(params double[] p) => Enumerable.Range(0, p.Length / 8).Select(i => p.Skip(i * 8).Take(8).ToArray()).ToArray();
    ///
    static Dictionary<int, double[][]> PieceSquareTable = new()
    {
        [2] = A(-5, -4, -3, -3, -3, -3, -4, -5, -4, -2, 0, 0, 0, 0, -2, -4, -3, 0, 1, 1.5, 1.5, 1, 0, -3, -3, 0.5, 1.5, 2, 2, 1.5, 0.5, -3, -3, 0, 1.5, 2, 2, 1.5, 0, -3, -3, 0.5, 1, 1.5, 1.5, 1, 0.5, -3, -4, -2, 0, 0.5, 0.5, 0, -2, -4, -5, -4, -3, -3, -3, -3, -4, -5),
        [3] = A(-2, -1, -1, -1, -1, -1, -1, -2, -1, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0.5, 1, 1, 0.5, 0, -1, -1, 0.5, 0.5, 1, 1, 0.5, 0.5, -1, -1, 0, 1, 1, 1, 1, 0, -1, -1, 1, 1, 1, 1, 1, 1, -1, -1, 0.5, 0, 0, 0, 0, 0.5, -1, -2, -1, -1, -1, -1, -1, -1, -2),
        [5] = A(-2, -1, -1, -0.5, -0.5, -1, -1, -2, -1, 0, 0, 0, 0, 0, 0, -1, -1, 0, 0.5, 0.5, 0.5, 0.5, 0, -1, -0.5, 0, 0.5, 0.5, 0.5, 0.5, 0, -0.5, 0, 0, 0.5, 0.5, 0.5, 0.5, 0, -0.5, -1, 0.5, 0.5, 0.5, 0.5, 0.5, 0, -1, -1, 0, 0.5, 0, 0, 0.5, 0, -1, -2, -1, -1, -0.5, 0, 0, 0, 0),
    };


    (double, Move) MiniMax(Board board, int depth, double alpha, double beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return (EvaluateBoard(board), new Move());
        }

        double bestEval = maximizingPlayer ? -99999 : 99999;
        Move bestMove = new Move();

        foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            var eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer);
            board.UndoMove(move);

            if ((maximizingPlayer && eval.Item1 > bestEval) || (!maximizingPlayer && eval.Item1 < bestEval))
            {
                bestEval = eval.Item1;
                bestMove = move;
            }

            if (maximizingPlayer)
            {
                alpha = Math.Max(alpha, eval.Item1);
                if (beta <= alpha) break;
            }
            else
            {
                beta = Math.Min(beta, eval.Item1);
                if (beta <= alpha) break;
            }
        }

        return (bestEval, bestMove);
    }



    double CheckKingSafety(Board board, bool isWhite)
    {
        var kingIndex = board.GetKingSquare(isWhite).Index;

        // Define castling positions based on the king's color
        int kingsideRookFile = isWhite ? 7 : 63;
        int queensideRookFile = isWhite ? 0 : 56;

        int[] offsets = { -9, -8, -7, -1, 1, 7, 8, 9 };
        double safetyScore = 0;

        for (int i = 0; i < offsets.Length; i++)
        {
            int checkPos = kingIndex + offsets[i];

            if (checkPos == kingsideRookFile || checkPos == queensideRookFile)
            {
                continue;
            }

            // Check if the square is within the board bounds and not attacked by the opponent
            if (!board.SquareIsAttackedByOpponent(new Square(checkPos)))
            {
                safetyScore += 1.1;
            }
        }

        return safetyScore;
    }


    double EvaluateBoard(Board board)
    {
        double[] PieceValues = { 0, 1, 3, 3.15, 5, 9 };
        double score = 0;
        double positionScore = 0;
        const double centerWeight = 0.1;

        for (int i = 1; i <= 5; i++)
        {
            PieceType pieceType = (PieceType)i;

            var whitePieces = board.GetPieceList(pieceType, true);
            var blackPieces = board.GetPieceList(pieceType, false);
            int whiteCount = whitePieces.Count;
            int blackCount = blackPieces.Count;
            double pieceValue = PieceValues[i];

            double centerSum = whitePieces
                .Concat(blackPieces)
                .Where(piece => i == 4 ? true : i == 1 ? pieceType == PieceType.Pawn : true)
                .Sum(piece => i == 4 ? piece.Square.File : i == 1 ? Math.Abs(piece.Square.File - 4) + Math.Abs(piece.Square.Rank - 4) : PieceSquareTable[i][piece.Square.File][piece.Square.Rank]);

            if (i == 4)
                positionScore += (whitePieces.Sum(piece => piece.Square.File) - blackPieces.Sum(piece => piece.Square.File)) * centerWeight;
            if (i == 1)
                positionScore += (blackPieces.Sum(piece => Math.Abs(piece.Square.File - 4) + Math.Abs(piece.Square.Rank - 4)) - whitePieces.Sum(piece => Math.Abs(piece.Square.File - 4) + Math.Abs(piece.Square.Rank - 4))) * centerWeight;
            score += pieceValue * (whiteCount - blackCount);
        }

        double whiteKingSafety = CheckKingSafety(board, true);
        double blackKingSafety = CheckKingSafety(board, false);


        return score + (positionScore * 1.8) + ((whiteKingSafety - blackKingSafety) * 1.5);
    }
}

