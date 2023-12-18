namespace auto_Bot_436;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
public class Bot_436 : IChessBot
{
    private const int MAX_DEPTH = 10;
    private Move bestMove;
    private Evaluator evaluator = new();
    private int depthReached = 0;
    private int currentDepth = 0;
    private Dictionary<ulong, BoardEvaluation> transpositionTable = new();
    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 1000)
        {
            //panic mode
            Move[] legalMoves = board.GetLegalMoves();
            return legalMoves[new Random().Next(legalMoves.Length)];
        }
        int depth = 0;
        depthReached = 0;
        int thinkingTime = Math.Min(timer.MillisecondsRemaining / 30, timer.GameStartTimeMilliseconds / 240);
        int evaluation = 0;
        transpositionTable = new();
        while (timer.MillisecondsElapsedThisTurn < thinkingTime)
        {
            currentDepth = 0;
            evaluator.NodesEvaluated = 0;
            depth++;
            int alpha = -Evaluator.WON_GAME, beta = Evaluator.WON_GAME;
            evaluation = NegaMaxWithAlphaBetaPruning(board, depth, alpha, beta, true, PieceType.None);
            if (evaluation == Evaluator.WON_GAME || depth + 1 >= MAX_DEPTH)
            {
                break;
            }
        }
        return bestMove;
    }

    // alpha is the minimum score that the maximizing player(white) is assured of 
    // and beta is the maximum score that the minimizing player(black) is assured of
    private int NegaMaxWithAlphaBetaPruning(Board board, int remainingDepth, int alpha, int beta, bool isInitialLevel, PieceType movedPiece)
    {
        currentDepth++;
        if (currentDepth > depthReached)
            depthReached = currentDepth;
        if (transpositionTable.ContainsKey(board.ZobristKey))
        {
            if (transpositionTable[board.ZobristKey].remainingDepth > remainingDepth)
            {
                currentDepth--;
                return transpositionTable[board.ZobristKey].evaluation;
            }
        }
        if (remainingDepth == 0)
        {
            currentDepth--;
            int evaluation = QuiescenceSearch(board, alpha, beta, movedPiece);
            return evaluation;
        }
        Move[] moves = board.GetLegalMoves();
        // Recursion cutoff -> Evaluate the current position
        if (moves.Length == 0 || board.IsDraw())
        {
            currentDepth--;
            return evaluator.EvaluateBoardPosition(board, movedPiece);
        }

        int currentEvaluation;
        // check through all possible moves
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            currentEvaluation = -NegaMaxWithAlphaBetaPruning(board, remainingDepth - 1, -beta, -alpha, false, move.MovePieceType);
            board.UndoMove(move);

            //save or update the value in our transposition table
            if (transpositionTable.ContainsKey(board.ZobristKey))
            {
                transpositionTable[board.ZobristKey].evaluation = currentEvaluation;
                transpositionTable[board.ZobristKey].remainingDepth = remainingDepth;
            }
            else
            {
                transpositionTable.Add(board.ZobristKey, new BoardEvaluation(currentEvaluation, remainingDepth));
            }

            if (currentEvaluation > beta)
            {
                // beta cutoff - a better move than the best possibility from this position 
                // was already found in a different branch so we can stop searching in this one
                currentDepth--;
                return beta;
            }
            if (currentEvaluation > alpha)
            {
                alpha = currentEvaluation;
                if (isInitialLevel) bestMove = move;
            }
        }
        currentDepth--;
        return alpha;
    }


    private int QuiescenceSearch(Board board, int alpha, int beta, PieceType movedPiece)
    {
        currentDepth++;
        if (currentDepth > depthReached)
        {
            depthReached = currentDepth;
        }
        if (transpositionTable.ContainsKey(board.ZobristKey))
        {
            currentDepth--;
            return transpositionTable[board.ZobristKey].evaluation;
        }
        Move[] capturingMoves = board.GetLegalMoves(true);
        int currentEvaluation = evaluator.EvaluateBoardPosition(board, movedPiece);

        if (currentDepth >= MAX_DEPTH)
        {
            currentDepth--;
            transpositionTable.Add(board.ZobristKey, new BoardEvaluation(currentEvaluation, 0));
            return currentEvaluation;
        }
        if (currentEvaluation > beta)
        {
            // beta cutoff - a better move than the best possibility from this position 
            // was already found in a different branch so we can stop searching in this one
            currentDepth--;
            transpositionTable.Add(board.ZobristKey, new BoardEvaluation(currentEvaluation, 0));
            return beta;
        }
        if (currentEvaluation > alpha) alpha = currentEvaluation;

        int baseEvaluation = currentEvaluation;
        foreach (Move move in capturingMoves)
        {
            //don't look at captures that are unlikely to improve the current best evaluation
            if (evaluator.GetPieceValue(move.CapturePieceType) + baseEvaluation <= alpha + 100)
            {
                continue;
            }
            board.MakeMove(move);
            currentEvaluation = -QuiescenceSearch(board, -beta, -alpha, move.MovePieceType);
            board.UndoMove(move);
            if (currentEvaluation > beta)
            {
                // beta cutoff - a better move than the best possibility from this position 
                // was already found in a different branch so we can stop searching in this one
                currentDepth--;
                transpositionTable.Add(board.ZobristKey, new BoardEvaluation(currentEvaluation, 0));
                return beta;
            }
            if (currentEvaluation > alpha)
            {
                alpha = currentEvaluation;
            }
        }
        currentDepth--;
        transpositionTable.Add(board.ZobristKey, new BoardEvaluation(alpha, 0));
        return alpha;
    }
}

public class Evaluator
{
    // piece values for none, pawn, knight, bishop, rook, queen
    private static int[] PieceValues = new int[] { 0, 100, 300, 350, 500, 900 };
    public int NodesEvaluated { get; set; } = 0;
    public const int WON_GAME = 10000000;

    public int EvaluateBoardPosition(Board board, PieceType movedPiece)
    {
        NodesEvaluated++;
        if (board.IsInCheckmate())
        {
            return -WON_GAME;
        }

        //drawn position will be evaluated as slightly worse than an equal position
        //this should prevent the bot from picking repeating moves over non-repeating ones
        if (board.IsDraw()) return -10;
        //try to slightly encourage checks
        int checkBonus = board.IsInCheck() ? -50 : 0;
        //try to discourage endless king moves
        int pieceBonus = movedPiece == PieceType.King ? 20 : 0;
        int who2Move = board.IsWhiteToMove ? 1 : -1;
        return (EvaluateMaterial(board) + EvaluateMobility(board) + checkBonus + pieceBonus) * who2Move;
    }

    private int EvaluateMobility(Board board)
    {
        int ownMoves = board.GetLegalMoves().Length;
        return ownMoves / 10;
    }

    private int EvaluateMaterial(Board board)
    {
        int whitePiecesValue = 0, blackPiecesValue = 0;
        // Sum up how much all white and black pieces are worth
        for (int i = 1; i < PieceValues.Length; i++)
        {
            whitePiecesValue += board.GetPieceList((PieceType)i, white: true).Count * PieceValues[i];
            blackPiecesValue += board.GetPieceList((PieceType)i, white: false).Count * PieceValues[i];
        }
        return whitePiecesValue - blackPiecesValue;
    }

    public int GetPieceValue(PieceType pieceType)
    {
        return PieceValues[(int)pieceType];
    }
}

public class BoardEvaluation
{
    public int evaluation { get; set; }
    public int remainingDepth { get; set; }

    public BoardEvaluation(int evaluation, int remainingDepth)
    {
        this.evaluation = evaluation;
        this.remainingDepth = remainingDepth;
    }
}