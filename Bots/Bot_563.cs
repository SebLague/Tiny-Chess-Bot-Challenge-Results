namespace auto_Bot_563;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_563 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int depth = DecideDepth(timer);

        List<Move> moves = board.GetLegalMoves().OrderBy(GuessRankMove).ToList();
        DivertedConsole.Write(moves[0].MovePieceType);
        Move bestMove = moves[0];
        float bestEvaluation = board.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity;

        foreach (Move move in board.GetLegalMoves())
        {
            // my AI loves certain "stylish" moves, it will play them regardless of evaluation
            if (IsForced(board, move))
            {
                return move;
            }

            board.MakeMove(move);
            float moveEvaluation = Minimax(board, depth, float.NegativeInfinity, float.PositiveInfinity);
            board.UndoMove(move);

            if (board.IsWhiteToMove)
            {
                if (moveEvaluation > bestEvaluation)
                {
                    bestEvaluation = moveEvaluation;
                    bestMove = move;
                }
            }
            else
            {
                if (moveEvaluation < bestEvaluation)
                {
                    bestEvaluation = moveEvaluation;
                    bestMove = move;
                }
            }

        }

        return bestMove;
    }

    private int DecideDepth(Timer timer)
    {
        int depth = 4;
        if (timer.MillisecondsRemaining > 60001)
        {
            depth = 5;
        }
        else if (timer.MillisecondsRemaining < 10000)
        {
            depth = 3;
        }
        else if (timer.MillisecondsRemaining < 1000)
        {
            depth = 2;
        }
        return depth;
    }

    private bool IsForced(Board board, Move move)
    {
        // obviously enpassant is forced
        if (move.IsEnPassant)
        {
            return true;
        }

        // castling with check is stylish - therefore forced
        if (move.IsCastles)
        {
            if (IsCheck(board, move))
            {
                return true;
            }
        }

        // underpromotion with check... doesn't get cooler -- FORCED
        if (move.IsPromotion)
        {
            if (move.PromotionPieceType == PieceType.Knight || move.PromotionPieceType == PieceType.Bishop)
            {
                if (IsCheck(board, move))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsCheck(Board board, Move move)
    {
        bool toReturn = false;
        board.MakeMove(move);
        if (board.IsInCheck())
        {
            toReturn = true;
        }
        board.UndoMove(move);
        return toReturn;
    }

    private float Minimax(Board board, int depth, float alpha, float beta)
    {
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity;
        }
        if (depth == 0)
        {
            return Evaluate(board);
        }

        float best = board.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity;
        foreach (Move move in board.GetLegalMoves().OrderBy(GuessRankMove).ToList())
        {
            board.MakeMove(move);
            float moveEvaluation = Minimax(board, depth - 1, alpha, beta);
            board.UndoMove(move);

            best = board.IsWhiteToMove ? Math.Max(best, moveEvaluation) : Math.Min(best, moveEvaluation);

            if (board.IsWhiteToMove)
            {
                alpha = Math.Max(alpha, best);
                if (best >= beta)
                {
                    break;
                }
            }
            else
            {
                beta = Math.Min(beta, best);
                if (best <= alpha)
                {
                    break;
                }
            }
        }

        return best;
    }

    private static float Evaluate(Board board)
    {
        PieceList[] allPieces = board.GetAllPieceLists();

        float pawns = 1 * (allPieces[0].Count - allPieces[6].Count);
        float knights = 3 * (allPieces[1].Count - allPieces[7].Count);
        float bishops = 3 * (allPieces[2].Count - allPieces[8].Count);
        float rooks = 5 * (allPieces[3].Count - allPieces[9].Count);
        float queens = 9 * (allPieces[4].Count - allPieces[10].Count);


        float mobility = board.GetLegalMoves().Count();
        if (board.TrySkipTurn())
        {
            mobility -= board.GetLegalMoves().Count();
            board.UndoSkipTurn();
        }
        else
        {
            mobility = 0; // I DONT LIKE THIS
        }
        mobility = (float)(0.025 * (board.IsWhiteToMove ? mobility : -mobility));


        float evaluation = pawns + knights + bishops + rooks + queens + mobility;

        return evaluation;
    }

    private static float GuessRankMove(Move move)
    {
        float total = 0;

        if (move.IsCapture)
        {
            total += 1;
            float difference = PieceValue(move.CapturePieceType) - PieceValue(move.MovePieceType);
            if (difference > 0)
            {
                total += difference;
            }
        }

        return -total;
    }

    private static float PieceValue(PieceType piece)
    {
        if (piece is PieceType.Pawn)
        {
            return 1;
        }
        else if (piece is PieceType.Knight)
        {
            return 3;
        }
        else if (piece is PieceType.Bishop)
        {
            return 3;
        }
        else if (piece is PieceType.Rook)
        {
            return 5;
        }
        else if (piece is PieceType.Queen)
        {
            return 9;
        }
        else
        {
            return 0;
        }
    }
}