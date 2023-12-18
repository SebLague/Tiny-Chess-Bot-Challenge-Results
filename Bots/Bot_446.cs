namespace auto_Bot_446;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_446 : IChessBot
{
    static int infinity = int.MaxValue;
    static int[] centerIndices = { 27, 28, 35, 36 };
    static int[] nearCenterIndices = { 18, 19, 20, 21, 26, 29, 34, 37, 42, 43, 44, 45 };

    bool stopThinking;
    DateTime startTime;
    int timeLimitMs;
    Move bestMove;


    public Move Think(Board board, Timer timer)
    {

        // Don't waste time
        Move[] legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 1)
        {
            return legalMoves[0];
        }

        // Decide how much time to allocate
        stopThinking = false;
        startTime = DateTime.Now;
        timeLimitMs = 500;
        int difference = timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining;
        if (difference > 0)
        {
            timeLimitMs = Math.Max((timer.MillisecondsRemaining > 10000 ? 1000 : 500), difference - 1000);
        }
        timeLimitMs = Math.Max(Math.Min(timeLimitMs, timer.MillisecondsRemaining - 1000), 0);


        // Start negamax search with iterative deepening and alpha beta pruning
        int alpha;
        int beta;
        Move[] sortedMoves = sortMoves(legalMoves, board);
        bestMove = sortedMoves[0];

        for (int maxDepth = 3; maxDepth <= (difference < 0 ? 5 : 20); maxDepth += 2)
        {
            alpha = -infinity;
            beta = infinity;
            int score = Negamax(board, 0, maxDepth, alpha, beta);
            if (stopThinking || score == infinity)
            {
                break;
            }
        }

        return bestMove;
    }

    // ##############################################
    // ################# SORT MOVES #################
    // ##############################################

    public Move[] sortMoves(Move[] moves, Board board)
    {
        Move[] sortedMoves = moves
            .Select(move => new { Move = move, Score = calculateMoveScore(move, board) })
            .OrderByDescending(item => item.Score)
            .Select(item => item.Move)
            .ToArray();
        return sortedMoves;
    }

    public double calculateMoveScore(Move move, Board board)
    {
        double moveScoreGuess = 0;

        // Always put best move first to benefit from iterative deepening
        if (move == bestMove)
        {
            moveScoreGuess = 1000000;
        }

        // Values Checks 
        board.MakeMove(move);
        if (board.IsInCheckmate())
        {
            moveScoreGuess += 1000000;
        }
        else if (board.IsInCheck())
        {
            moveScoreGuess += 1000;
        }
        board.UndoMove(move);

        // Values Captures of big pieces by little pieces
        if (move.CapturePieceType != PieceType.None)
        {
            moveScoreGuess += 10 * (GetPieceValue(move.CapturePieceType) - GetPieceValue(move.MovePieceType));
        }

        // Values Promotions
        if (move.IsPromotion)
        {
            moveScoreGuess += GetPieceValue(move.PromotionPieceType);
        }

        return moveScoreGuess;
    }


    // ################################################
    // #################### SEARCH ####################
    // ################################################

    public int Negamax(Board board, int depth, int maxDepth, int alpha, int beta)
    {
        // Evaluate the position if we are at the tip of the branch
        if (depth == maxDepth || board.IsDraw() || board.IsInCheckmate())
        {
            return SearchAllCaptures(board, alpha, beta);
        }

        // Search deeper in the move tree
        Move[] sortedMoves = sortMoves(board.GetLegalMoves(), board);
        foreach (Move move in sortedMoves)
        {
            // Stop if think time is up
            if ((DateTime.Now - startTime).TotalMilliseconds >= timeLimitMs)
            {
                stopThinking = true;
                return 0;
            }

            // Evaluate further move
            board.MakeMove(move);
            var evaluation = 0;
            checked { evaluation = -Negamax(board, depth + 1, maxDepth, -beta, -alpha); }
            board.UndoMove(move);

            // Pruning the branch
            if (evaluation >= beta)
            {
                if (depth == 0)
                {
                    bestMove = move;
                }
                return beta;
            }

            // Updating alpha
            if (evaluation > alpha)
            {
                alpha = evaluation;
                if (depth == 0)
                {
                    bestMove = move;
                }
            }
        }

        // Return best score
        return alpha;
    }

    public int SearchAllCaptures(Board board, int alpha, int beta)
    {
        int evaluation = EvaluatePosition(board);
        if (evaluation >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, evaluation);

        Move[] captureMoves = sortMoves(board.GetLegalMoves(true), board);

        foreach (Move captureMove in captureMoves)
        {
            board.MakeMove(captureMove);
            checked { evaluation = -SearchAllCaptures(board, -beta, -alpha); }
            board.UndoMove(captureMove);

            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }


    // ################################################
    // ################## EVALUATION ##################
    // ################################################

    private int EvaluatePosition(Board board)
    {
        // Ending position
        if (board.IsDraw())
        {
            return 0;
        }
        else if (board.IsInCheckmate())
        {
            return -infinity;
        }

        // Compute material balance
        int whiteMaterial = 0;
        int blackMaterial = 0;
        int perspective = (board.IsWhiteToMove) ? 1 : -1;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                {
                    whiteMaterial += GetPieceValue(piece.PieceType);
                }
                else
                {
                    blackMaterial += GetPieceValue(piece.PieceType);
                }
            }
        }

        // Compute piece activity and center control
        int whiteActivityAndCenterControl = 0;
        int blackActivityAndCenterControl = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                {
                    whiteActivityAndCenterControl += GetPieceActivityAndCenterControl(board, piece, !board.IsWhiteToMove);
                }
                else if (!piece.IsWhite)
                {
                    blackActivityAndCenterControl += GetPieceActivityAndCenterControl(board, piece, board.IsWhiteToMove);
                }
            }
        }

        // Development
        int development = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (board.IsWhiteToMove == piece.IsWhite && (piece.IsKnight || piece.IsBishop))
                {
                    if ((piece.Square.Rank != 0 && board.IsWhiteToMove) || (piece.Square.Rank != 7 && !board.IsWhiteToMove))
                    {
                        development += 10;
                    }
                }
            }
        }

        return perspective * (1000 * (whiteMaterial - blackMaterial) + 20 * (whiteActivityAndCenterControl - blackActivityAndCenterControl)) + 10 * development;
    }

    static private int GetPieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 1;
            case PieceType.Knight:
                return 3;
            case PieceType.Bishop:
                return 3;
            case PieceType.Rook:
                return 5;
            case PieceType.Queen:
                return 9;
            default:
                return 0;
        }
    }

    static private int GetPieceActivityAndCenterControl(Board board, Piece piece, bool skipTurn)
    {
        if (skipTurn)
        {
            board.ForceSkipTurn();
        }
        int activity = 0;
        int centerControl = 0;
        foreach (Move move in board.GetLegalMoves())
        {
            if (move.StartSquare == piece.Square)
            {
                if (piece.PieceType != PieceType.Pawn)
                {
                    activity += 1;
                }
                if ((piece.PieceType != PieceType.King && (piece.PieceType != PieceType.Pawn || move.IsCapture)))
                {
                    if (centerIndices.Contains(move.TargetSquare.Index))
                    {
                        centerControl += 10 / GetPieceValue(piece.PieceType);
                    }
                    else if (nearCenterIndices.Contains(move.TargetSquare.Index))
                    {
                        centerControl += 5 / GetPieceValue(piece.PieceType);
                    }
                }
            }
        }
        if (skipTurn)
        {
            board.UndoSkipTurn();
        }
        return activity + centerControl;
    }
}