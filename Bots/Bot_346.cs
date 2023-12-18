namespace auto_Bot_346;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_346 : IChessBot
{
    private class MoveScore
    {
        public Move Move;
        public float Score;

        public MoveScore(Move move, float score)
        {
            Move = move;
            Score = score;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        MoveScore bestMove = new MoveScore(Move.NullMove, float.MinValue);

        bool myColor = board.IsWhiteToMove;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate() || move.IsEnPassant)
            {
                board.UndoMove(move);
                return move;
            }

            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            float eval = Evaluate(board, 0, 3, myColor);

            if (eval > bestMove.Score)
            {
                bestMove = new MoveScore(move, eval);
            }

            board.UndoMove(move);
        }

        return bestMove.Move != Move.NullMove ? bestMove.Move : moves[0];
    }

    private float Evaluate(Board board, int depth, int maxDepth, bool myColor, float alpha = float.MinValue, float beta = float.MaxValue)
    {
        if (depth == maxDepth) return EvaluatePosition(board, myColor);

        float bestScore = depth % 2 == 1 ? float.MinValue : float.MaxValue;

        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return (depth % 2 == 1) ? float.MaxValue : float.MinValue;
            }

            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            float score = Evaluate(board, depth + 1, maxDepth, myColor, alpha, beta);

            if (depth % 2 == 1)
            {
                // my turn
                bestScore = Math.Max(score, bestScore);
                alpha = Math.Max(alpha, score);
            }
            else
            {
                bestScore = Math.Min(score, bestScore);
                beta = Math.Min(beta, score);
            }

            if (beta <= alpha)
            {
                board.UndoMove(move);
                break;
            }

            board.UndoMove(move);
        }

        return bestScore;
    }

    private ulong GetAttackedTiles(Board board, bool color, ulong pieces)
    {
        ulong bitboard = 0;

        int squareindex;
        while ((squareindex = BitboardHelper.ClearAndGetIndexOfLSB(ref (pieces))) != 64)
        {
            Square square = new Square(squareindex);
            PieceType piecetype = board.GetPiece(square).PieceType;
            bitboard |= BitboardHelper.GetPieceAttacks(piecetype, square, board, color);
        }

        return bitboard;
    }

    // evaluate from POV of myColor
    private float EvaluatePosition(Board board, bool myColor)
    {
        ulong myPieces = myColor ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        ulong enemyPieces = myColor ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;

        ulong myAttackedTiles = GetAttackedTiles(board, myColor, myPieces);
        ulong enemyAttackedTiles = GetAttackedTiles(board, !myColor, enemyPieces);

        int numberMyAttackedTiles = BitboardHelper.GetNumberOfSetBits(myAttackedTiles);
        int numberEnemyAttackedTiles = BitboardHelper.GetNumberOfSetBits(enemyAttackedTiles);

        ulong attackedEnemyPieces = myAttackedTiles & enemyPieces;
        ulong attackedMyPieces = enemyAttackedTiles & myPieces;

        int valueAttackedEnemyPieces = PieceSetValue(board, attackedEnemyPieces);
        int valueAttackedMyPieces = PieceSetValue(board, attackedMyPieces);

        int valueMyPieces = PieceSetValue(board, myPieces & ~enemyAttackedTiles) + valueAttackedMyPieces;
        int valueEnemyPieces = PieceSetValue(board, enemyPieces & ~myAttackedTiles) + valueAttackedEnemyPieces;

        int numberUnprotectedPieces = BitboardHelper.GetNumberOfSetBits(myPieces & ~myAttackedTiles);
        int numberUnprotectedEnemyPieces = BitboardHelper.GetNumberOfSetBits(enemyPieces & ~enemyAttackedTiles);

        int numberMyMoves = board.GetLegalMoves().Count();
        int numberEnemyMoves = board.GetLegalMoves(!myColor).Count();

        ulong myrank7 = (ulong)127 << (myColor ? 48 : 8);
        int pushedPawns = BitboardHelper.GetNumberOfSetBits(myPieces & myrank7);

        Square myKingPosition = board.GetKingSquare(myColor);
        int homeRank = myColor ? 0 : 7;
        int myKingDistance = Math.Abs(myKingPosition.Rank - homeRank);

        Square enemyKingPosition = board.GetKingSquare(!myColor);
        int enemyHomeRank = myColor ? 7 : 0;
        int enemyKingDistance = Math.Abs(enemyKingPosition.Rank - enemyHomeRank);


        //i made these numbers up there is 0 actual chess knowledge involved
        return 5 * (valueMyPieces - valueEnemyPieces) + valueAttackedEnemyPieces - valueAttackedMyPieces +
            numberUnprotectedEnemyPieces - numberUnprotectedPieces + numberMyMoves - numberEnemyMoves +
            pushedPawns + 2 * (numberMyAttackedTiles - numberEnemyAttackedTiles) + 5 * (myKingDistance - enemyKingDistance);

    }

    private int PieceSetValue(Board board, ulong bitboard)
    {
        int sum = 0;
        int squareindex;
        while ((squareindex = BitboardHelper.ClearAndGetIndexOfLSB(ref (bitboard))) != 64)
        {
            Square square = new Square(squareindex);
            sum += PieceValue(board.GetPiece(square).PieceType);
        }

        return sum;
    }

    private int PieceValue(PieceType type)
    {
        return type switch
        {
            PieceType.Pawn => 1,
            PieceType.Knight => 3,
            PieceType.Bishop => 3,
            PieceType.Rook => 5,
            PieceType.Queen => 9,
            PieceType.King => 100
        };
    }
}