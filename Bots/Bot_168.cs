namespace auto_Bot_168;
using ChessChallenge.API;
using System;


public class Bot_168 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] values = { 0, 100, 300, 300, 500, 900, 10000 };

    // Get Mirrored move.
    private Move getMirrorMove(Move move, Board board)
    {
        PieceType mirroredPieceType = move.MovePieceType;
        int mirroredOriginRank = 7 - move.StartSquare.Rank;
        int mirroredOriginFile = 7 - move.StartSquare.File;
        int mirroredTargetRank = 7 - move.TargetSquare.Rank;
        int mirroredTargetFile = 7 - move.TargetSquare.File;

        //Get all the Possible moves
        Move[] possibleMoves = board.GetLegalMoves();

        Move moveToMake = Move.NullMove;

        for (int moveIdx = 0; moveIdx < possibleMoves.Length && moveToMake == Move.NullMove; moveIdx++)
        {
            Move m = possibleMoves[moveIdx];
            if (m.MovePieceType == mirroredPieceType && m.TargetSquare.Rank == mirroredTargetRank && m.TargetSquare.File == mirroredTargetFile
                && m.StartSquare.Rank == mirroredOriginRank && m.StartSquare.File == mirroredOriginFile)
            {
                moveToMake = m;
            }
        }
        return moveToMake;
    }

    private Move getBestNonMirrorMove(Move[] possibleMoves, Board board)
    {
        Random rng = new();
        Move moveToPlay = possibleMoves[rng.Next(possibleMoves.Length)];

        int highestMoveValue = 0;

        foreach (Move move in possibleMoves)
        {
            int moveValue = 0;

            // Always play checkmate in one
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            if (isMate)
            {
                moveValue += 20000;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = values[(int)capturedPiece.PieceType];
            moveValue += capturedPieceValue;

            if (moveValue > highestMoveValue)
            {
                moveToPlay = move;
                moveValue = capturedPieceValue;
            }
        }
        return moveToPlay;
    }

    public Move Think(Board board, Timer timer)
    {
        Move moveToPlay = Move.NullMove;

        //Get all the Possible moves
        Move[] possibleMoves = board.GetLegalMoves();

        //First move no Mirror move if white
        if (board.GameMoveHistory.Length == 0)
        {
            moveToPlay = getBestNonMirrorMove(possibleMoves, board);
        }

        //Copy move if possible
        if (moveToPlay == Move.NullMove)
        {

            Move oponentMove = board.GameMoveHistory[board.GameMoveHistory.Length - 1];
            moveToPlay = getMirrorMove(oponentMove, board);
        }

        //If no Copy possible go for best non Mirror Move
        if (moveToPlay == Move.NullMove)
        {
            moveToPlay = getBestNonMirrorMove(possibleMoves, board);
        }

        return moveToPlay;
    }
}