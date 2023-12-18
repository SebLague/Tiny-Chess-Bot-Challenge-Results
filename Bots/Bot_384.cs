namespace auto_Bot_384;
using ChessChallenge.API;
using System;

// A negamax bot (alpha beta pruning + quietSearch) with heuristical evaluation.
public class Bot_384 : IChessBot
{
    int depthNegamax = 3;
    int depthQuiescence = 3;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 0 };

    // Weights
    int materialDifferenceWeight = 10;
    int positionDifferenceWeight = 5;
    int checkWeight = 500;
    int bishopPairWeight = 1000;

    public Move Think(Board board, Timer timer)
    {
        if (board.PlyCount == 0 && board.IsWhiteToMove) return new Move("e2e4", board);
        if (board.PlyCount == 1 && !board.IsWhiteToMove) return new Move("e7e6", board);

        return GetBestMoveNegamax(board, depthNegamax);
    }

    Move GetBestMoveNegamax(Board position, int depth)
    {
        Move bestMove = Move.NullMove;
        int bestScore = int.MinValue;

        int materialDifference = MaterialDifference(position);

        foreach (Move legalMove in position.GetLegalMoves())
        {
            int score = -3000;

            position.MakeMove(legalMove);

            // If it's openning, try not to get out the queen
            if (position.PlyCount > 12 ||
                !legalMove.MovePieceType.Equals(PieceType.Queen))
                score = -GetScoreNegamax(position, depth - 1, int.MinValue, int.MaxValue);

            position.UndoMove(legalMove);

            // If we have better material, exchange pieces of same value
            if (legalMove.IsCapture &&
                pieceValues[(int)legalMove.MovePieceType] <= pieceValues[(int)position.GetPiece(legalMove.TargetSquare).PieceType] &&
                materialDifference >= 0) score += 1100;

            // If we can castle it's better
            if (legalMove.IsCastles) score += 500;

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = legalMove;
            }

        }
        return bestMove;
    }

    int GetScoreNegamax(Board position, int depth, int alpha, int beta)
    {
        if (position.GetLegalMoves().Length == 0 || position.IsDraw())
            return EvaluatePosition(position);

        else if (depth == 0)
            return QuiescenceSearch(position, depthQuiescence, -beta, -alpha);

        // Optimisation (looking capture first)
        Move[] legalMoves = position.GetLegalMoves();
        Array.Sort(legalMoves, (move1, move2) =>
        {
            int score1 = EvaluateMoveMetric(position, move1);
            int score2 = EvaluateMoveMetric(position, move2);
            return score2.CompareTo(score1);
        });

        foreach (Move legalMove in legalMoves)
        {
            position.MakeMove(legalMove);
            int newAlpha = (beta == int.MinValue) ? int.MaxValue : -beta;
            int newBeta = (alpha == int.MinValue) ? int.MaxValue : -alpha;
            int score = -GetScoreNegamax(position, depth - 1, newAlpha, newBeta);
            position.UndoMove(legalMove);

            alpha = Math.Max(alpha, score);

            if (alpha >= beta) break;
        }

        return alpha;
    }

    int QuiescenceSearch(Board position, int depth, int alpha, int beta)
    {
        int tempScore = EvaluatePosition(position);
        Move[] captureMoves = position.GetLegalMoves(capturesOnly: true);

        if (captureMoves.Length == 0 || position.IsDraw() || depth == 0)
            return tempScore;

        if (tempScore >= beta)
            return beta;

        alpha = Math.Max(tempScore, alpha);

        foreach (Move capture in captureMoves)
        {
            position.MakeMove(capture);
            int score = -QuiescenceSearch(position, depth - 1, -beta, -alpha);
            position.UndoMove(capture);

            alpha = Math.Max(alpha, score);
            if (alpha >= beta)
                return beta;
        }

        return alpha;
    }

    // EVALUATIONS

    int EvaluatePosition(Board board)
    {
        if (board.IsInCheckmate()) return -999999;

        int result = 0;
        result += MaterialDifference(board) * materialDifferenceWeight;
        result += PositionDifference(board) * positionDifferenceWeight;
        result += board.IsInCheck() ? -checkWeight : 0;

        result += BishopAccuracy(board, board.IsWhiteToMove);
        result -= BishopAccuracy(board, !board.IsWhiteToMove);

        // need to check if next is draw
        bool isDraw = board.IsRepeatedPosition() || board.IsInStalemate() || board.IsDraw();
        // If we are winning, try not to draw & if we are loosing, try to draw
        int drawDelta = 0;
        if (isDraw) drawDelta = (result >= 1000) ? -1500 : 1500;
        result += drawDelta;

        return result;
    }

    int MaterialDifference(Board board)
    {
        int material = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            int value = pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count;
            material += pieceList.IsWhitePieceList ? value : -value;
        }
        return board.IsWhiteToMove ? material : -material;
    }

    int PositionDifference(Board board)
    {
        int result = board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        result -= board.GetLegalMoves().Length;
        board.UndoSkipTurn();
        return result;
    }

    int BishopAccuracy(Board board, bool color)
    {
        return board.GetPieceList(PieceType.Bishop, color).Count == 2 ? bishopPairWeight : 0;
    }

    // HELPER

    int EvaluateMoveMetric(Board board, Move move)
    {
        int metric = 0;
        if (move.IsCapture) metric += 100;
        if (move.IsPromotion) metric += 50;
        if (move.IsCastles) metric += 30;
        return metric;
    }

}
