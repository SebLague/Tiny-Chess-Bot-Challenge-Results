namespace auto_Bot_40;
using ChessChallenge.API;
using System;

public class Bot_40 : IChessBot
{
    private int[] piece_value = { 0, 5, 10, 10, 10, 40, 20 };

    private bool IsMoveCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool value = board.IsInCheckmate();
        board.UndoMove(move);
        return value;
    }

    private int ThinkMove(Board board, Move move)
    {
        if (IsMoveCheckmate(board, move))
        {
            return 100000;
        }

        Piece piece = board.GetPiece(move.StartSquare);
        int value = piece_value[(uint)move.CapturePieceType];

        if (move.IsPromotion)
        {
            value += piece_value[(uint)move.PromotionPieceType];
        }

        if (board.TrySkipTurn())
        {
            if (Array.Exists(board.GetLegalMoves(), (x) => x.TargetSquare == move.StartSquare))
            {
                value += piece_value[(uint)piece.PieceType] * 2;
            }
            board.UndoSkipTurn();
        }

        board.MakeMove(move);
        if (Array.Exists(board.GetLegalMoves(), (x) => x.TargetSquare == move.TargetSquare))
        {
            value -= piece_value[(uint)piece.PieceType] * 2;
        }
        board.UndoMove(move);
        return value;
    }

    public Move Think(Board board, Timer timer)
    {
        int value = int.MinValue;
        Move move = new Move();

        foreach (Move current_move in board.GetLegalMoves())
        {
            int current_value = ThinkMove(board, current_move);
            if (current_value >= value)
            {
                value = current_value;
                move = current_move;
            }
        }

        DivertedConsole.Write(value);
        return move;
    }
}