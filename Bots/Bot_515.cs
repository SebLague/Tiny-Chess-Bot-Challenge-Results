namespace auto_Bot_515;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_515 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        var count = board.PlyCount / 2;

        if (count < 4)
        {

            Move[] predefinedMovesForBlack =
            {
            new Move("e7e5", board),
            new Move("f8c5", board),
            new Move("d8h4", board),
            new Move("h4f2", board)
        };

            Move[] predefinedMovesForWhite =
            {
            new Move("e2e4", board),
            new Move("f1c4", board),
            new Move("d1h5", board),
            new Move("h5f7", board)
        };


            var setMoves = board.IsWhiteToMove ? predefinedMovesForWhite : predefinedMovesForBlack;

            if (board.GetLegalMoves().Contains(setMoves[count]))
            {
                return setMoves[count];
            }
        }


        var legalMoves = board.GetLegalMoves();

        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            if (board.IsInCheckmate()) return legalMoves[i];
            board.UndoMove(legalMoves[i]);
        }

        var attacks = board.GetLegalMoves(true);
        if (attacks.Any()) { return attacks[0]; }

        int repeat = 8;
        var pawnMove = new Move();
        while (repeat-- > 0 && pawnMove.IsNull)
        {
            pawnMove = movePawnAhead(board);
        }


        if (!pawnMove.IsNull) return pawnMove;


        Move[] moves = board.GetLegalMoves();
        return moves[0];
    }

    Move movePawnAhead(Board board)
    {
        var pawns = board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove);
        if (pawns.Count == 0) { return new Move(); }


        Random rnd = new Random();

        var pawn = pawns[rnd.Next(pawns.Count)];
        var square = pawn.Square;

        var moves = board.GetLegalMoves();
        var move = moves.Where(x => x.StartSquare == square);


        if (move.Count() == 1) { return move.FirstOrDefault(); }

        var queen = move.FirstOrDefault(x => x.PromotionPieceType == PieceType.Queen);

        if (!queen.IsNull) { return queen; }

        return move.FirstOrDefault();
    }
}