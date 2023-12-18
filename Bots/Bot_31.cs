namespace auto_Bot_31;
using ChessChallenge.API;
using System.Linq;

public class Bot_31 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return GetBestMove(board);
    }

    Move GetBestMove(Board board)
    {
        var moves = board.GetLegalMoves();
        var bestMove = new Move();
        var bestMoveScore = -999999f;
        var alpha = -100000f;

        foreach (var move in moves)
        {
            board.MakeMove(move);

            var moveScore = -AlphaBeta(board, 3, -100000f, -alpha);
            if (moveScore > bestMoveScore)
            {
                bestMoveScore = moveScore;
                bestMove = move;
            }

            if (moveScore > alpha)
                alpha = moveScore;

            board.UndoMove(move);
        }

        return bestMove;
    }

    float AlphaBeta(Board board, int depth, float alpha, float beta)
    {
        var bestScore = -9999f;
        if (depth <= 0)
            return Quiesce(board, alpha, beta);

        foreach (var move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            var score = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return score;

            if (score > bestScore)
                bestScore = score;

            if (score > alpha)
                alpha = score;
        }

        return bestScore;
    }

    float Quiesce(Board board, float alpha, float beta)
    {
        var standingPat = EvaluateBoard(board);
        if (standingPat >= beta)
            return beta;

        if (alpha < standingPat)
            alpha = standingPat;

        foreach (var move in board.GetLegalMoves())
        {
            if (!move.IsCapture) continue;
            board.MakeMove(move);
            var score = -Quiesce(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    float EvaluateBoard(Board board)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -9999f : 9999f;
        if (board.IsDraw()) return 0;

        int[] pieceValues = { 100, 320, 330, 500, 900, 20000 };
        PieceType[] pieceTypes =
            {PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King};
        float[][] tables =
        {
            new[]
            {
                0f, 0, 0, 0, 0, 0, 0, 0,
                5, 10, 10, -20, -20, 10, 10, 5,
                5, -5, -10, 0, 0, -10, -5, 5,
                0, 0, 0, 20, 20, 0, 0, 0,
                5, 5, 10, 25, 25, 10, 5, 5,
                10, 10, 20, 30, 30, 20, 10, 10,
                50, 50, 50, 50, 50, 50, 50, 50,
                0, 0, 0, 0, 0, 0, 0, 0
            },
            new[]
            {
                -50f, -40, -30, -30, -30, -30, -40, -50,
                -40, -20, 0, 5, 5, 0, -20, -40,
                -30, 5, 10, 15, 15, 10, 5, -30,
                -30, 0, 15, 20, 20, 15, 0, -30,
                -30, 5, 15, 20, 20, 15, 5, -30,
                -30, 0, 10, 15, 15, 10, 0, -30,
                -40, -20, 0, 0, 0, 0, -20, -40,
                -50, -40, -30, -30, -30, -30, -40, -50
            },
            new[]
            {
                -20f, -10, -10, -10, -10, -10, -10, -20,
                -10, 5, 0, 0, 0, 0, 5, -10,
                -10, 10, 10, 10, 10, 10, 10, -10,
                -10, 0, 10, 10, 10, 10, 0, -10,
                -10, 5, 5, 10, 10, 5, 5, -10,
                -10, 0, 5, 10, 10, 5, 0, -10,
                -10, 0, 0, 0, 0, 0, 0, -10,
                -20, -10, -10, -10, -10, -10, -10, -20
            },
            new[]
            {
                0f, 0, 0, 5, 5, 0, 0, 0,
                -5, 0, 0, 0, 0, 0, 0, -5,
                -5, 0, 0, 0, 0, 0, 0, -5,
                -5, 0, 0, 0, 0, 0, 0, -5,
                -5, 0, 0, 0, 0, 0, 0, -5,
                -5, 0, 0, 0, 0, 0, 0, -5,
                5, 10, 10, 10, 10, 10, 10, 5,
                0, 0, 0, 0, 0, 0, 0, 0
            },
            new[]
            {
                -20f, -10, -10, -5, -5, -10, -10, -20,
                -10, 0, 0, 0, 0, 0, 0, -10,
                -10, 5, 5, 5, 5, 5, 0, -10,
                0, 0, 5, 5, 5, 5, 0, -5,
                -5, 0, 5, 5, 5, 5, 0, -5,
                -10, 0, 5, 5, 5, 5, 0, -10,
                -10, 0, 0, 0, 0, 0, 0, -10,
                -20, -10, -10, -5, -5, -10, -10, -20
            },
            new[]
            {
                20f, 30, 10, 0, 0, 10, 30, 20,
                20, 20, 0, 0, 0, 0, 20, 20,
                -10, -20, -20, -20, -20, -20, -20, -10,
                -20, -30, -30, -40, -40, -30, -30, -20,
                -30, -40, -40, -50, -50, -40, -40, -30,
                -30, -40, -40, -50, -50, -40, -40, -30,
                -30, -40, -40, -50, -50, -40, -40, -30,
                -30, -40, -40, -50, -50, -40, -40, -30
            }
        };

        float evaluation = 0;
        for (int i = 0; i < 6; i++)
        {
            var whitePieces = board.GetPieceList(pieceTypes[i], true);
            var blackPieces = board.GetPieceList(pieceTypes[i], false);
            evaluation += pieceValues[i] * (whitePieces.Count - blackPieces.Count)
                          + (whitePieces.Sum(p => tables[i][p.Square.Index])
                             - blackPieces.Sum(p => tables[i][(7 - p.Square.Rank) * 8 + p.Square.File]));
        }

        return board.IsWhiteToMove ? evaluation : -evaluation;
    }
}