namespace auto_Bot_487;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_487 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 0 };
    int moveCount = 0;

    public Move Think(Board board, Timer timer)
    {
        DivertedConsole.Write("New");
        List<Move> allMoves = OrderMoves(board, board.GetLegalMoves().ToList());

        // Pick a random move to play if nothing better is found
        Random rng = new();
        int bestEval = -100000;

        int searchDepth = (board.GetLegalMoves().Length < 20 && PiecesLeft(board) < 6) ? 6 : timer.MillisecondsRemaining > 5000 ? 4 : 3;

        List<Move> sameEvaluationMoves = new List<Move>();
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int moveEvaluation = -Search(board, searchDepth, -100000, 100000);
            if (moveEvaluation == bestEval)
                sameEvaluationMoves.Add(move);
            else if (moveEvaluation > bestEval)
            {
                bestEval = moveEvaluation;
                sameEvaluationMoves.Clear();
                sameEvaluationMoves.Add(move);
            }
            board.UndoMove(move);
            DivertedConsole.Write(move.ToString() + " " + moveEvaluation.ToString());
        }
        DivertedConsole.Write(moveCount);
        moveCount = 0;
        return sameEvaluationMoves[rng.Next(sameEvaluationMoves.Count - 1)];
        //return sameEvaluationMoves[0];
    }

    int Evaluate(Board board)
    {
        int whiteEvaluation = 0;
        int blackEvaluation = 0;
        PieceList[] peiceLists = board.GetAllPieceLists();
        foreach (PieceList pieceList in peiceLists)
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                {
                    whiteEvaluation += pieceValues[(int)piece.PieceType];
                }
                else
                {
                    blackEvaluation += pieceValues[(int)piece.PieceType];
                }
            }
        }

        int evaluation = whiteEvaluation - blackEvaluation;

        if (PiecesLeft(board) > 10)
        {
            evaluation += (int)(Math.Abs(board.GetKingSquare(true).File - 3.5) + Math.Abs(board.GetKingSquare(true).Rank - 3.5));
            evaluation -= (int)(Math.Abs(board.GetKingSquare(false).File - 3.5) + Math.Abs(board.GetKingSquare(false).Rank - 3.5));
        }
        else
        {
            evaluation -= (int)(Math.Abs(board.GetKingSquare(true).File - 3.5) + Math.Abs(board.GetKingSquare(true).Rank - 3.5));
            evaluation += (int)(Math.Abs(board.GetKingSquare(false).File - 3.5) + Math.Abs(board.GetKingSquare(false).Rank - 3.5));
        }

        int perspective = board.IsWhiteToMove ? 1 : -1;

        return evaluation * perspective;
    }

    int PiecesLeft(Board board)
    {
        int pieceCount = 0;
        PieceList[] peiceLists = board.GetAllPieceLists();
        foreach (PieceList pieceList in peiceLists)
        {
            foreach (Piece piece in pieceList)
            {
                pieceCount++;
            }
        }
        return pieceCount;
    }

    int Search(Board board, int depth, int alpha, int beta)
    {
        //DivertedConsole.Write(moveCount);
        if (board.IsInCheckmate())
            return -100000;
        if (board.IsDraw())
            return 0;
        if (depth == 1)
            return QuinscienceSearch(board, alpha, beta);

        List<Move> moves = OrderMoves(board, board.GetLegalMoves().ToList());

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            moveCount++;
            int evaluation = -Search(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);
            if (evaluation >= beta)
                return beta;
            alpha = Math.Max(alpha, evaluation);

        }
        return alpha;
    }

    List<Move> OrderMoves(Board board, List<Move> moves)
    {
        Dictionary<Move, int> moveEvaluations = new Dictionary<Move, int>();
        foreach (Move move in moves)
        {
            int moveScoreApprox = 0;
            PieceType movePieceType = board.GetPiece(move.StartSquare).PieceType;
            PieceType capturePieceType = board.GetPiece(move.TargetSquare).PieceType;
            board.MakeMove(move);
            if (board.IsInCheck())
                moveScoreApprox += 50;
            if (board.IsInCheckmate())
                moveScoreApprox += 100;
            board.UndoMove(move);
            // Better capture gives a better score
            if (capturePieceType != PieceType.None)
                moveScoreApprox += 10 * pieceValues[(int)capturePieceType] - pieceValues[(int)movePieceType];

            // Promotion are generally good moves
            if (move.IsPromotion)
                moveScoreApprox += pieceValues[(int)move.PromotionPieceType];

            if (board.SquareIsAttackedByOpponent(move.TargetSquare) && !move.IsCapture)
                moveScoreApprox -= pieceValues[(int)movePieceType] * 2;

            moveEvaluations[move] = moveScoreApprox;
        }
        List<KeyValuePair<Move, int>> sortedMoves = moveEvaluations.OrderByDescending(x => x.Value).ToList();
        moves.Clear();
        foreach (KeyValuePair<Move, int> move in sortedMoves)
        {
            moves.Add(move.Key);
        }
        return moves;
    }

    int QuinscienceSearch(Board board, int alpha, int beta)
    {
        int evaluation = Evaluate(board);
        if (evaluation >= beta)
            return beta;
        alpha = Math.Max(alpha, evaluation);

        List<Move> moves = board.GetLegalMoves(true).ToList();
        moves = OrderMoves(board, moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            moveCount++;
            evaluation = -QuinscienceSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta)
                return beta;
            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }
}