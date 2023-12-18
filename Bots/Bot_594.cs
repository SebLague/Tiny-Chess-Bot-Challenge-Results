namespace auto_Bot_594;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_594 : IChessBot
{

    int[] typeValues = { 0, 100, 320, 330, 500, 900, 10000 };

    int minValue = -int.MaxValue, maxValue = int.MaxValue;

    List<Move> sameValueMoves = new();
    Random random = new();

    Move bestMoveThisTurn, bestMoveCurrIteration;
    int bestEvaluationCurrIteration;
    bool searchTimeout;

    public Move Think(Board board, Timer timer)
    {
        bestMoveThisTurn = Move.NullMove;
        searchTimeout = false;

        Comparison<Move> orderingComparison =
            (x, y) => MoveOrderingEvaluation(x, board).CompareTo(MoveOrderingEvaluation(y, board));

        GC.Collect();
        GC.WaitForPendingFinalizers();


        Span<Move> legalMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref legalMoves);

        if (legalMoves.Length == 1)
            return legalMoves[0];

        var timeDifference = timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining;
        var timeToThink = timeDifference > 1 ? 300 + timeDifference / 2 : 300;

        for (var iterationDepth = 0; iterationDepth < maxValue; iterationDepth++)
        {
            bestMoveCurrIteration = Move.NullMove;
            bestEvaluationCurrIteration = minValue;


            sameValueMoves.Clear();


            legalMoves.Sort(orderingComparison);

            foreach (var legalMove in legalMoves)
            {
                if (timer.MillisecondsElapsedThisTurn > timeToThink)
                {
                    searchTimeout = true;
                    break;
                }

                board.MakeMove(legalMove);

                var moveEvaluation = -AlphaBetaNegamaxSearch(minValue, maxValue, iterationDepth, orderingComparison, board);
                if (moveEvaluation == maxValue)
                    return legalMove;

                if (moveEvaluation > bestEvaluationCurrIteration)
                {
                    bestEvaluationCurrIteration = moveEvaluation;
                    sameValueMoves.Clear();
                }

                if (moveEvaluation == bestEvaluationCurrIteration)
                    sameValueMoves.Add(legalMove);

                board.UndoMove(legalMove);
            }

            bestMoveCurrIteration = sameValueMoves.Count >= 1
                ? sameValueMoves[random.Next(sameValueMoves.Count)]
                : Move.NullMove;

            if (bestMoveCurrIteration != Move.NullMove)
                bestMoveThisTurn = bestMoveCurrIteration;

            if (searchTimeout)
                break;
        }

        return bestMoveThisTurn;
    }

    int AlphaBetaNegamaxSearch(int alpha, int beta, int depth, Comparison<Move> orderingComparison, Board board)
    {
        if (depth == 0)
            return QuiescenceSearch(alpha, beta, orderingComparison, board);

        Span<Move> depthMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref depthMoves);

        if (depthMoves.Length == 0)
            return board.IsInCheckmate() ? minValue : 0;

        if (board.GameRepetitionHistory.Contains(board.ZobristKey))
            return Math.Min(0, beta);

        depthMoves.Sort(orderingComparison);

        foreach (var move in depthMoves)
        {
            board.MakeMove(move);

            var searchScore = -AlphaBetaNegamaxSearch(-beta, -alpha, depth - 1, orderingComparison, board);

            board.UndoMove(move);

            if (searchScore >= beta)
                return beta;

            if (searchScore > alpha)
                alpha = searchScore;
        }

        return alpha;
    }

    int QuiescenceSearch(int alpha, int beta, Comparison<Move> orderingComparison, Board board)
    {
        var stubbornScore = StaticEvaluation(board);

        if (stubbornScore >= beta)
            return beta;

        var applyDeltaPruning = !IsEndgame(board);

        if (stubbornScore > alpha)
            alpha = stubbornScore;

        Span<Move> captureMoves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref captureMoves, true);

        if (captureMoves.Length == 0)
            return stubbornScore; // return STATIC EVALUATION

        captureMoves.Sort(orderingComparison);

        foreach (var captureMove in captureMoves)
        {
            if (applyDeltaPruning && stubbornScore + 200 + typeValues[(int)captureMove.CapturePieceType] < alpha)
                continue;

            board.MakeMove(captureMove);

            var quiesceScore = -QuiescenceSearch(-beta, -alpha, orderingComparison, board);

            board.UndoMove(captureMove);

            if (quiesceScore >= beta)
                return beta;

            if (quiesceScore > alpha)
                alpha = quiesceScore;
        }
        return alpha;
    }


    int MoveOrderingEvaluation(Move move, Board board)
    {
        if (move == bestMoveThisTurn)
            return minValue;

        var evalGuess = 0;

        if (move.IsCapture)
            evalGuess = 10 * typeValues[(int)move.CapturePieceType];

        if (move.IsPromotion)
            evalGuess += typeValues[(int)move.PromotionPieceType];

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            evalGuess -= typeValues[(int)move.MovePieceType];

        return -evalGuess;
    }

    int StaticEvaluation(Board board)
    {
        var evaluation = GetMaterial(0, board) - GetMaterial(6, board);

        if (IsEndgame(board))
        {
            if (evaluation != 0)
                evaluation += MopUpEvaluation(evaluation > 0, board);

            else if (board.TrySkipTurn())
            {
                Span<Move> nextPlayerMoves = stackalloc Move[256], currPlayerMoves = stackalloc Move[256];
                board.GetLegalMovesNonAlloc(ref nextPlayerMoves);
                board.UndoSkipTurn();
                board.GetLegalMovesNonAlloc(ref currPlayerMoves);
                var mobilityEvaluation = currPlayerMoves.Length - nextPlayerMoves.Length;
                // mobility evaluation
                evaluation += board.IsWhiteToMove ? mobilityEvaluation : -mobilityEvaluation;
            }
        }
        else
            evaluation += GetCenterControl(0, board) - GetCenterControl(6, board);

        return board.IsWhiteToMove ? evaluation : -evaluation;
    }

    int GetMaterial(int isWhiteOffset, Board board)
    {
        var pieceLists = board.GetAllPieceLists();
        int material = 0;

        for (var i = 0; i <= 4; i++)
            material += pieceLists[i + isWhiteOffset].Count * typeValues[i + 1];

        return material;
    }

    int GetCenterControl(int isWhiteOffset, Board board)
    {
        var piecesLists = board.GetAllPieceLists();
        int mobility = 0;

        for (var i = 0; i <= 2; i++)
        {
            foreach (var piece in piecesLists[i + isWhiteOffset])
            {
                mobility += 6 - CenterManhattanDistance(piece.Square);
            }
        }

        return mobility;
    }

    int MopUpEvaluation(bool whiteAdvantage, Board board)
    {
        var opponentKingSquare = board.GetKingSquare(!whiteAdvantage);

        var myKingSquare = board.GetKingSquare(whiteAdvantage);
        var trapEvaluation = CenterManhattanDistance(opponentKingSquare)
                                       + (14 - Math.Abs(myKingSquare.File - opponentKingSquare.File) - Math.Abs(myKingSquare.Rank - opponentKingSquare.Rank));

        return whiteAdvantage ? trapEvaluation : -trapEvaluation;
    }

    int CenterManhattanDistance(Square square)
    {
        return Math.Max(3 - square.File, square.File - 4)
               + Math.Max(3 - square.Rank, square.Rank - 4);
    }

    bool IsEndgame(Board board)
    {
        var pieces = board.GetAllPieceLists();
        int whiteMaterial = GetMaterial(0, board), blackMaterial = GetMaterial(6, board);
        return (whiteMaterial <= 1300 && blackMaterial <= 1300)
               || Math.Min(whiteMaterial, blackMaterial) == 0
               || pieces[0].Count + pieces[6].Count == 0;
    }
}