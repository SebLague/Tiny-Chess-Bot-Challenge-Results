namespace auto_Bot_356;
using ChessChallenge.API;

public class Bot_356 : IChessBot
{
    const float EVALUATION_PAWN = 1f;
    const float EVALUATION_KNIGHT = 3f;
    const float EVALUATION_BISHOP = 3f;
    const float EVALUATION_ROOK = 5f;
    const float EVALUATION_QUEEN = 9f;
    const float EVALUATION_CHECK = 2f;
    const float EVALUATION_MATE = 9001f;
    const float EVALUATION_DRAW = -1000f;
    const float EVALUATION_USELESS_MOVE = -1f;
    const float EVALUATION_REPEATED_POSITION = -5f;

    bool playingAsWhite = false;

    int depth = 4;

    public Move Think(Board board, Timer timer)
    {
        playingAsWhite = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();

        Move bestMove = FindBestMove(board, timer, moves, true, depth);

        if (timer.MillisecondsRemaining > 20000)
        {
            if (timer.MillisecondsElapsedThisTurn > 1200)
                depth = depth >= 6 ? depth -= 2 : 4;
            else if (timer.MillisecondsElapsedThisTurn < 100)
            {
                if (depth < 8)
                {
                    depth += 2;
                }
            }
        }
        else
        {
            depth = 4;
        }

        return bestMove;
    }

    Move FindBestMove(Board board, Timer timer, Move[] legalMoves, bool ourMove, int depth)
    {
        float evaluationBeforeMove = Evaluate(board);

        float[] evaluations = new float[legalMoves.Length];

        for (int i = 0; i < legalMoves.Length; ++i)
        {
            Move move = legalMoves[i];

            int depthForThisLine = timer.MillisecondsRemaining > 10000 ? depth : depth > 4 ? depth - 1 : 1;

            if (depthForThisLine > 1)
            {
                evaluations[i] = Evaluate(move, board);

                if (!EvaluationIsBetter(evaluations[i], evaluationBeforeMove, board.IsWhiteToMove))
                {
                    evaluations[i] += (playingAsWhite ? 1 : -1) * EVALUATION_USELESS_MOVE; // Penalize the useless move

                    // Only look one move ahead to see if the "useless" move was actually to dodge a capture
                    if (depthForThisLine > 2)
                        depthForThisLine = 2;
                }

                board.MakeMove(move);

                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return move;
                }
                else if (board.IsDraw())
                {
                    evaluations[i] = Evaluate(board);
                    board.UndoMove(move);
                }
                else
                {
                    Move bestCounterMove = FindBestMove(board, timer, board.GetLegalMoves(), !ourMove, depthForThisLine - 1);
                    board.MakeMove(bestCounterMove);
                    evaluations[i] = Evaluate(board);

                    board.UndoMove(bestCounterMove);
                    board.UndoMove(move);
                }
            }
            else
            {
                evaluations[i] = Evaluate(move, board);
            }
        }

        int bestMoveIndex = FindBestMoveIndex(evaluations, ourMove ? playingAsWhite : !playingAsWhite);

        Move bestMove = legalMoves[bestMoveIndex];

        return bestMove;
    }

    float Evaluate(Move move, Board board)
    {
        board.MakeMove(move);
        float evaluation = Evaluate(board);

        board.UndoMove(move);

        return evaluation;
    }

    float Evaluate(Board board)
    {
        float evaluation = 0f;

        float evaluationSign = (board.IsWhiteToMove ? -1 : 1);

        if (board.IsInCheckmate())
            evaluation += EVALUATION_MATE * evaluationSign;
        else if (board.IsRepeatedPosition())
            evaluation += EVALUATION_REPEATED_POSITION * evaluationSign;
        else if (board.IsInCheck())
            evaluation += EVALUATION_CHECK * evaluationSign;
        else if (board.IsDraw())
            evaluation += EVALUATION_DRAW * evaluationSign;

        PieceList whitePawns = board.GetPieceList(PieceType.Pawn, true);
        PieceList whiteKnights = board.GetPieceList(PieceType.Knight, true);
        PieceList whiteBishops = board.GetPieceList(PieceType.Bishop, true);
        PieceList whiteRooks = board.GetPieceList(PieceType.Rook, true);
        PieceList whiteQueens = board.GetPieceList(PieceType.Queen, true);
        PieceList blackPawns = board.GetPieceList(PieceType.Pawn, false);
        PieceList blackKnights = board.GetPieceList(PieceType.Knight, false);
        PieceList blackBishops = board.GetPieceList(PieceType.Bishop, false);
        PieceList blackRooks = board.GetPieceList(PieceType.Rook, false);
        PieceList blackQueens = board.GetPieceList(PieceType.Queen, false);

        foreach (Piece piece in whitePawns)
            evaluation += EVALUATION_PAWN + EvaluatePawnSquare(piece.Square, true);
        foreach (Piece piece in whiteKnights)
            evaluation += EVALUATION_KNIGHT + EvaluateKnightSquare(piece.Square, true);
        foreach (Piece piece in whiteBishops)
            evaluation += EVALUATION_BISHOP + EvaluateBishopSquare(piece.Square, true);
        foreach (Piece piece in whiteRooks)
            evaluation += EVALUATION_ROOK;
        foreach (Piece piece in whiteQueens)
            evaluation += EVALUATION_QUEEN;

        // King safety?

        foreach (Piece piece in blackPawns)
            evaluation += -EVALUATION_PAWN + EvaluatePawnSquare(piece.Square, false);
        foreach (Piece piece in blackKnights)
            evaluation += -EVALUATION_KNIGHT + EvaluateKnightSquare(piece.Square, false);
        foreach (Piece piece in blackBishops)
            evaluation += -EVALUATION_BISHOP + EvaluateBishopSquare(piece.Square, false);
        foreach (Piece piece in blackRooks)
            evaluation += -EVALUATION_ROOK;
        foreach (Piece piece in blackQueens)
            evaluation += -EVALUATION_QUEEN;

        // King safety?

        return evaluation;
    }

    int FindBestMoveIndex(float[] evaluations, bool playingAsWhite)
    {
        float largestPositiveEvaluation = evaluations[0];
        float largestNegativeEvaluation = evaluations[0];

        int largestPositiveEvaluationIndex = 0;
        int largestNegativeEvaluationIndex = 0;

        for (int i = 0; i < evaluations.Length; ++i)
        {
            if (evaluations[i] > largestPositiveEvaluation)
            {
                largestPositiveEvaluation = evaluations[i];
                largestPositiveEvaluationIndex = i;
            }

            if (evaluations[i] < largestNegativeEvaluation)
            {
                largestNegativeEvaluation = evaluations[i];
                largestNegativeEvaluationIndex = i;
            }
        }

        if (playingAsWhite)
            return largestPositiveEvaluationIndex;
        else
            return largestNegativeEvaluationIndex;
    }

    float EvaluatePawnSquare(Square square, bool white)
    {
        int relativeRank = white ? (square.Rank - 1) : (6 - square.Rank);

        float distanceFromCenter = square.File <= 3 ? (3.5f - square.File) : (square.File - 3.5f);

        float evaluation = (relativeRank) * (white ? 1 : -1) * 0.10f;

        evaluation = evaluation / distanceFromCenter;

        return evaluation;
    }

    float EvaluateKnightSquare(Square square, bool white)
    {
        float rankDistanceFromCenter = square.Rank <= 3 ? (3.5f - square.Rank) : (square.Rank - 3.5f);
        float fileDistanceFromCenter = square.File <= 3 ? (3.5f - square.File) : (square.File - 3.5f);
        float evaluation = (white ? 1 : -1) / (rankDistanceFromCenter + fileDistanceFromCenter);
        return evaluation;
    }

    float EvaluateBishopSquare(Square square, bool white)
    {
        int relativeRank = white ? (square.Rank) : (7 - square.Rank);

        float evaluation = relativeRank == 0 ? 0.0f : relativeRank < 4 ? 0.35f : 0;

        return evaluation * (white ? 1 : -1);
    }

    //string MoveToString(Move move)
    //{
    //    return $"{move.MovePieceType} from {move.StartSquare.Name} to {move.TargetSquare.Name}";
    //}

    bool EvaluationIsBetter(float evaluation, float baselineEvaluation, bool playingAsWhite)
    {
        if (playingAsWhite)
            return evaluation > baselineEvaluation;
        else
            return evaluation < baselineEvaluation;
    }
}