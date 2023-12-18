namespace auto_Bot_44;
using ChessChallenge.API;
using System;

public class Bot_44 : IChessBot
{
    // Piece values: 0 null, 1 pawn, 2 knight, 3 bishop, 4 rook, 5 queen, 6 king
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 100000 };
    Move[] allMoves;

    public Move Think(Board board, Timer timer)
    {
        allMoves = board.GetLegalMoves();

        Random rnd = new Random();
        Move moveToPlay = allMoves[rnd.Next(allMoves.Length)];
        int highestValueMove = 0;

        if (timer.MillisecondsRemaining > 1000)
        {
            foreach (Move move in allMoves)
            {
                int currentMoveValue = MoveValue(board, move);
                if (currentMoveValue > highestValueMove)
                {
                    moveToPlay = move;
                    highestValueMove = currentMoveValue;
                }
            }
        }

        return moveToPlay;
    }

    int MoveValue(Board board, Move move)
    {
        bool moveAttacked = board.SquareIsAttackedByOpponent(move.TargetSquare);

        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        // en passant is forced
        if (isMate || move.IsEnPassant)
        {
            board.UndoMove(move);

            return 100000000;
        }

        if (board.IsInCheck() && !moveAttacked)
        {
            board.UndoMove(move);

            return 90;
        }

        if (board.IsDraw() && board.IsWhiteToMove != Winning(board))
        {
            board.UndoMove(move);

            return -90;
        }

        int numberOfLegalOpponentMoves = board.GetLegalMoves().Length;

        int numberOfLegalMoves;

        if (board.TrySkipTurn())
        {
            numberOfLegalMoves = board.GetLegalMoves().Length;

            board.UndoSkipTurn();
        }
        else
        {
            numberOfLegalMoves = 2 * numberOfLegalOpponentMoves;
        }

        int pieceValue = PieceValue(move.TargetSquare, board);
        board.UndoMove(move);

        return PieceValue(move.TargetSquare, board) - (moveAttacked ? pieceValue : 0) + numberOfLegalMoves - numberOfLegalOpponentMoves;
    }

    bool Winning(Board board)
    {
        int white = 0;
        int black = 0;
        foreach (var piece in board.GetAllPieceLists())
        {
            int pieceValue = pieceValues[(int)piece.TypeOfPieceInList] * piece.Count;

            if (piece.IsWhitePieceList)
            {
                white += pieceValue;
            }
            else
            {
                black += pieceValue;
            }
        }

        return white > black;
    }

    int PieceValue(Square square, Board board)
    {
        return pieceValues[(int)board.GetPiece(square).PieceType];
    }
}