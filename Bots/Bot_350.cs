namespace auto_Bot_350;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_350 : IChessBot
{
    private Random rnd = new Random();

    private Dictionary<PieceType, List<Move>> pieceMoves = new Dictionary<PieceType, List<Move>>();

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move[] captureMoves = board.GetLegalMoves(true);
        if (captureMoves.Length > 0)
        {
            moves = captureMoves;
        }
        //find any moves where a pawn can take a significant piece
        List<Move> pawnTakes = new List<Move>();
        //empty the piecemoves dictionary for new moves
        pieceMoves = new Dictionary<PieceType, List<Move>>();
        //populate each type with at least a key and an empty list
        pieceMoves[PieceType.Pawn] = new List<Move>(); pieceMoves[PieceType.Knight] = new List<Move>(); pieceMoves[PieceType.Bishop] = new List<Move>();
        pieceMoves[PieceType.Rook] = new List<Move>(); pieceMoves[PieceType.Queen] = new List<Move>(); pieceMoves[PieceType.King] = new List<Move>();

        foreach (Move move in moves)
        {
            PieceType type = move.MovePieceType;
            PieceType capture = move.CapturePieceType;
            pieceMoves[type].Add(move);


            if (type == PieceType.Pawn && (capture == PieceType.Knight || capture == PieceType.Bishop || capture == PieceType.Rook || capture == PieceType.Queen))
            {
                pawnTakes.Add(move);
            }
        }

        if (pawnTakes.Count > 0)
        {
            return pawnTakes[rnd.Next(pawnTakes.Count)];
        }

        return GoToAnotherPiece();
    }

    private Move GoToAnotherPiece()
    {
        PieceType typeToCheck;
        //if no valid moves then go to a random move
        int val = rnd.Next(0, 13);
        switch (val)
        {
            case 11:
            case 12:
                typeToCheck = PieceType.Pawn;
                break;
            case 8:
            case 9:
            case 10:
                typeToCheck = PieceType.Knight;
                break;
            case 5:
            case 6:
            case 7:
                typeToCheck = PieceType.Bishop;
                break;
            case 3:
            case 4:
                typeToCheck = PieceType.Rook;
                break;
            case 1:
            case 2:
                typeToCheck = PieceType.Queen;
                break;
            case 0:
                typeToCheck = PieceType.King;
                break;
            default:
                typeToCheck = PieceType.None;
                break;
        }

        return TryMove(typeToCheck);
    }

    private Move TryMove(PieceType type)
    {
        List<Move> moves = pieceMoves[type];
        return pieceMoves[type].Count == 0 ? GoToAnotherPiece() : moves[rnd.Next(moves.Count)];
    }

}