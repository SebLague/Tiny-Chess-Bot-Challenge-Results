namespace auto_Bot_130;


using ChessChallenge.API;
using System;

public class Bot_130 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 1000, 3000, 3000, 5000, 9000, 100000 };
    Board board;
    Move lastMove = new Move();

    public Move Think(Board board, Timer timer)
    {
        Int32 SEARCH_DEPTH = 3;
        if (timer.MillisecondsRemaining < 1500)
        {
            SEARCH_DEPTH = 1;
        }
        if (timer.MillisecondsRemaining < 15000)
        {
            SEARCH_DEPTH = 2;
        }

        this.board = board;
        Move[] allMoves = this.board.GetLegalMoves();

        Move moveToPlay = allMoves[0];
        int bestValueMoveCurrently;
        if (board.IsWhiteToMove)
        {
            bestValueMoveCurrently = Int32.MinValue;
            foreach (Move move in allMoves)
            {

                // Find highest value move
                (int, bool) result = evalueteMove(move);
                if (result.Item2) return move;
                int moveValue = alphaBetaMin(result.Item1, SEARCH_DEPTH, move);
                if (moveValue > bestValueMoveCurrently)
                {
                    moveToPlay = move;
                    bestValueMoveCurrently = moveValue;
                }
            }
        }
        else
        {
            bestValueMoveCurrently = Int32.MaxValue;
            foreach (Move move in allMoves)
            {

                // Find highest value move
                (int, bool) result = evalueteMove(move);
                if (result.Item2) return move;
                int moveValue = alphaBetaMax(result.Item1, SEARCH_DEPTH, move);
                if (moveValue < bestValueMoveCurrently)
                {
                    moveToPlay = move;
                    bestValueMoveCurrently = moveValue;
                }
            }
        }
        lastMove = moveToPlay;
        return moveToPlay;
    }

    (int, bool) evalueteMove(Move move)
    {

        // Check if move is checkmate in one
        if (MoveIsCheckmate(move))
        {
            if (board.IsWhiteToMove) return (1000000, true);
            return (-1000000, true);
        }
        //Check if move is a Draw
        int improvement;
        board.MakeMove(move);
        bool isMate = board.IsDraw();
        board.UndoMove(move);
        if (isMate)
        {
            if (board.IsWhiteToMove)
            {
                improvement = -10000;
            }
            else
            {
                improvement = 500;
            }
        }
        // Find value of capture
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        improvement = pieceValues[(int)capturedPiece.PieceType];

        //reward forward movemement
        improvement += get_Rank_advance(move) * 2;

        //is move a check
        board.MakeMove(move);
        bool is_check = false;
        if (board.IsInCheck())
        {
            is_check = true;
        }
        board.UndoMove(move);

        //check for mobility of opposing King
        Square king_square = board.GetKingSquare(!board.IsWhiteToMove);
        improvement -= BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(king_square)) * 100;

        //check for King safety
        king_square = board.GetKingSquare(board.IsWhiteToMove);
        PieceList list = board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove);

        foreach (Piece piece in list)
        {
            if (piece.Square.File == king_square.File)
            {
                improvement += 500;
            }
            if (piece.Square.File == king_square.File - 1)
            {
                improvement += 300;
            }
            if (piece.Square.File == king_square.File + 1)
            {
                improvement += 300;
            }
        }

        if (board.SquareIsAttackedByOpponent(board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).GetPiece(0).Square))
        {
            if (move.MovePieceType != PieceType.Queen)
            {
                improvement -= 9000;
            }
        }

        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {

            improvement -= pieceValues[(int)move.MovePieceType] * 2;
        }
        else
        {
            if (is_check)
            {
                improvement += 200;
            }
        }
        if (SquareIsDefended(move) && move.MovePieceType != PieceType.Queen)
        {
            improvement += 200;
        }

        //evaluate movement of the piece
        improvement -= BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.StartSquare, board, board.IsWhiteToMove)) * 5;
        improvement += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.TargetSquare, board, board.IsWhiteToMove)) * 5;

        switch (move.MovePieceType)
        {
            case PieceType.Pawn:
                if (move.IsPromotion) improvement += (int)move.PromotionPieceType * 10;
                if (move.IsEnPassant) improvement += 100;
                if (move.IsCapture) improvement += 500;
                improvement += 20;
                break;
            case PieceType.King:
                if (move.IsCastles) improvement += 1000;
                improvement -= 500;
                break;
            case PieceType.Rook:
                if (1 < move.TargetSquare.File && move.TargetSquare.File < 6) improvement += 100;
                break;
            case PieceType.Bishop:
                if (1 < move.TargetSquare.File && move.TargetSquare.File < 6) improvement += 100;
                break;
            case PieceType.Knight:
                if (0 < move.TargetSquare.File && move.TargetSquare.File < 7) improvement += 100;
                if (move.TargetSquare.Rank == 0 || move.TargetSquare.Rank == 7)
                {
                    if (move.TargetSquare.File == 1 || move.TargetSquare.File == 6) improvement -= 1000;
                }
                break;

            default: break;

        }
        //punish reverting last move
        if (lastMove.StartSquare == move.TargetSquare)
        {
            improvement -= 1000;
        }
        //punish movement of same piece
        if (lastMove.TargetSquare == move.StartSquare)
        {
            improvement -= 1000;
        }

        if (board.IsWhiteToMove) return (improvement, false);
        return (-improvement, false);
    }

    int get_Rank_advance(Move move)
    {
        if (board.IsWhiteToMove)
        {
            return move.TargetSquare.Rank - move.StartSquare.Rank;
        }
        else
        {
            return move.StartSquare.Rank - move.TargetSquare.Rank;
        }
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool SquareIsDefended(Move move)
    {
        board.MakeMove(move);
        bool isdefended = board.SquareIsAttackedByOpponent(move.TargetSquare);
        board.UndoMove(move);
        return isdefended;
    }

    int alphaBetaMax(int initialScore, int depthleft, Move move)
    {
        if (depthleft == 0)
        {
            return initialScore;
        }

        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        int maxScore = Int32.MinValue;
        foreach (Move move_in_moves in allMoves)
        {

            int score = alphaBetaMin(initialScore + evalueteMove(move_in_moves).Item1, depthleft - 1, move_in_moves);
            if (score > maxScore)
            {
                maxScore = score;
            }
        }
        board.UndoMove(move);
        return initialScore + maxScore;
    }

    int alphaBetaMin(int initialScore, int depthleft, Move move)
    {
        if (depthleft == 0)
        {
            return initialScore;
        }

        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        int minScore = Int32.MaxValue;
        foreach (Move move_in_moves in allMoves)
        {

            int score = alphaBetaMax(initialScore + evalueteMove(move_in_moves).Item1, depthleft - 1, move_in_moves);
            if (score < minScore)
            {
                minScore = score;
            }
        }
        board.UndoMove(move);
        return initialScore + minScore;
    }

}
