namespace auto_Bot_121;
using ChessChallenge.API;
using System;

public class Bot_121 : IChessBot
{
    int[] value = { 1, 3, 3, 4, 9, 10 };

    public Move Think(Board board, Timer timer)
    {
        int highestValueCapture = 0;
        Move[] legalMoves = board.GetLegalMoves();



        Move bestMove = Move.NullMove;
        int bestEval = int.MinValue;
        int alpha = int.MinValue;
        int beta = int.MaxValue;

        foreach (Move legalMove in legalMoves)
        {
            board.MakeMove(legalMove);
            int eval = -AlphaBeta(board, alpha, beta, 3);
            if (eval >= bestEval)
            {
                bestEval = eval;
                bestMove = legalMove;

            }
            board.UndoMove(legalMove);

            if (MoveIsCheckmate(board, legalMove))
            {
                bestMove = legalMove;
                break;
            }

            Piece capturedPiece = board.GetPiece(legalMove.TargetSquare);
            int capturedPieceValue = value[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                bestMove = legalMove;
                highestValueCapture = capturedPieceValue;
            }

        }

        return bestMove;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    public int AlphaBeta(Board new_board, int alpha, int beta, int depth)
    {

        if (depth == 0)
        {
            return SearchAllCaptures(new_board, alpha, beta);
        }

        Move[] legalMoves = new_board.GetLegalMoves();

        if (legalMoves.Length == 0)
        {
            if (new_board.IsInCheckmate())
            {
                return int.MinValue;
            }
            return 0;
        }


        foreach (Move move in legalMoves)
        {
            new_board.MakeMove(move);
            int eval = -AlphaBeta(new_board, -beta, -alpha, depth - 1);

            new_board.UndoMove(move);

            if (eval >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, eval);

        }

        return alpha;
    }

    public int Evaluate_board(Board new_board)
    {
        PieceList[] pieceList = new_board.GetAllPieceLists();

        int white_pice_point = 0;
        int black_pice_point = 0;

        for (int i = 0; i < 6; i++) white_pice_point += pieceList[i].Count * value[i];
        for (int i = 6; i < 12; i++) black_pice_point += pieceList[i].Count * value[i - 6];

        int evaluation = white_pice_point - black_pice_point;
        evaluation += ForceKingToCornerInEndGameEval(new_board, 0.1f);

        int perspective = (new_board.IsWhiteToMove) ? 1 : -1;

        return evaluation * perspective;
    }

    public int SearchAllCaptures(Board new_board, int alpha, int beta)
    {
        int eval = Evaluate_board(new_board);
        if (eval >= beta) return beta;
        alpha = Math.Max(alpha, eval);

        Move[] CaptureMoveList = new_board.GetLegalMoves(capturesOnly: true);

        foreach (Move captureMove in CaptureMoveList)
        {
            new_board.MakeMove(captureMove);
            eval = -SearchAllCaptures(new_board, -beta, -alpha);
            new_board.UndoMove(captureMove);

            if (eval >= beta) return beta;
            alpha = Math.Max(alpha, eval);
        }

        return alpha;
    }

    public int ForceKingToCornerInEndGameEval(Board new_board, float endGameWeight)
    {
        int eval = 0;

        int friendlyking_file = new_board.GetKingSquare(new_board.IsWhiteToMove).File;
        int friendlyking_rank = new_board.GetKingSquare(new_board.IsWhiteToMove).Rank;

        int oponentKing_file = new_board.GetKingSquare(!new_board.IsWhiteToMove).File;
        int oponentKing_rank = new_board.GetKingSquare(!new_board.IsWhiteToMove).Rank;

        eval += Math.Max(3 - oponentKing_file, oponentKing_file - 4) + Math.Max(3 - oponentKing_rank, oponentKing_rank - 4) +
        14 - Math.Abs(friendlyking_file - oponentKing_file) + Math.Abs(friendlyking_rank - oponentKing_rank);

        return (int)(eval * 10 * endGameWeight);
    }
}

