namespace auto_Bot_461;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_461 : IChessBot
{
    public const string FILENAMES = "abcdefgh";

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    public readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    private Move GetRandomMove(List<Move> moves) => moves[new Random().Next(moves.Count)];

    private int InvertFile(int file) => 7 - file;
    private int InvertRank(int rank) => 9 - rank;

    public string InvertMove(string move)
    {
        int startFile = FILENAMES.IndexOf(move[0].ToString());
        int startRank = int.Parse(move[1].ToString());
        int endFile = FILENAMES.IndexOf(move[2].ToString());
        int endRank = int.Parse(move[3].ToString());

        return $"{FILENAMES[InvertFile(startFile)]}{InvertRank(startRank)}"
            + $"{FILENAMES[InvertFile(endFile)]}{InvertRank(endRank)}";
    }

    public string InvertMove(Move m) => InvertMove(m.ToString().Substring(7, 4));

    private Move GetCopyMove(Board board)
    {
        List<Move> moveList = new(board.GetLegalMoves());
        List<Move> moveHistory = new(board.GameMoveHistory);

        try
        {
            Move mostRecentMove = moveHistory[^1];
            Move invertedMove = new(InvertMove(mostRecentMove), board);

            if (moveList.Contains(invertedMove))
                return invertedMove;
            else return GetRandomMove(moveList);
        }
        catch
        {
            return GetRandomMove(moveList);
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a copy or random move if no move can be found
        Move moveToPlay = GetCopyMove(board);
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}
