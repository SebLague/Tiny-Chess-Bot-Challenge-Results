namespace auto_Bot_2;
using ChessChallenge.API;
using System;
using System.Linq;
// I wanted to make a board room meeting where a bunch of fictional characters present strategies, but I couldn't include the conversations as this goes over the token limit
public class Bot_2 : IChessBot
{
    private Random rng = new Random();

    public Move Think(Board board, Timer timer)
    {
        var legalMoves = board.GetLegalMoves();
        Move jfkMove = JFKMove(board, legalMoves);
        Move sebastianMove = SebastianMove(board, legalMoves);
        Move oswaldMove = OswaldMove(board, legalMoves);
        Move megatronMove = MegatronMove(board, legalMoves);
        Move unMove = UnMove(board, legalMoves);
        Move hitlerMove = HitlerMove(board, legalMoves);

        // CEO makes the decision. The criteria for now is just the material balance.
        Move[] moves = new Move[] { jfkMove, sebastianMove, oswaldMove, megatronMove, unMove, hitlerMove };
        return moves.OrderByDescending(m => EvaluateMove(board, m))
                     .First();
    }

    private Move JFKMove(Board board, Move[] legalMoves)
    {
        // JFK prefers moving the King in the center of the board.
        var jfkMoves = legalMoves.Where(m => board.GetPiece(m.StartSquare).PieceType == PieceType.King
                                              && (m.TargetSquare.File == 'd' || m.TargetSquare.File == 'e')
                                              && (m.TargetSquare.Rank == 4 || m.TargetSquare.Rank == 5)).ToArray();
        if (jfkMoves.Any())
        {
            return jfkMoves[rng.Next(jfkMoves.Length)];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    private Move SebastianMove(Board board, Move[] legalMoves)
    {
        // Sebastian prefers to make the move that takes the most valuable piece.
        var sebMoves = legalMoves.Where(m => board.GetPiece(m.TargetSquare) != null)
                                  .OrderByDescending(m => (int)board.GetPiece(m.TargetSquare).PieceType).ToArray();
        if (sebMoves.Any())
        {
            return sebMoves[0];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    private Move OswaldMove(Board board, Move[] legalMoves)
    {
        // Oswald prefers to snipe with the Bishop
        var bishopMoves = legalMoves.Where(m => board.GetPiece(m.StartSquare).PieceType == PieceType.Bishop).ToArray();
        if (bishopMoves.Any())
        {
            return bishopMoves[rng.Next(bishopMoves.Length)];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    private Move MegatronMove(Board board, Move[] legalMoves)
    {
        // Bimbo megatron prefers the Queen.
        var queenMoves = legalMoves.Where(m => board.GetPiece(m.StartSquare).PieceType == PieceType.Queen).ToArray();
        if (queenMoves.Any())
        {
            return queenMoves[rng.Next(queenMoves.Length)];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    private Move UnMove(Board board, Move[] legalMoves)
    {
        // Kim Jong Un prefers the King.
        var kingMoves = legalMoves.Where(m => board.GetPiece(m.StartSquare).PieceType == PieceType.King).ToArray();
        if (kingMoves.Any())
        {
            return kingMoves[rng.Next(kingMoves.Length)];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    private Move HitlerMove(Board board, Move[] legalMoves)
    {
        // Hitler prefers moves that control the center of the board.
        // I really wanted to make him prefer to have the pieces form a swastika, but again, token limit
        var centerMoves = legalMoves.Where(m => (m.TargetSquare.File == 'd' || m.TargetSquare.File == 'e')
                                                && (m.TargetSquare.Rank == 4 || m.TargetSquare.Rank == 5)).ToArray();
        if (centerMoves.Any())
        {
            return centerMoves[rng.Next(centerMoves.Length)];
        }

        return legalMoves[rng.Next(legalMoves.Length)];
    }


    private int EvaluateMove(Board board, Move move)
    {
        var targetPiece = board.GetPiece(move.TargetSquare);
        int value = targetPiece != null ? (int)targetPiece.PieceType : 0;

        if (move.TargetSquare.File == 'd' || move.TargetSquare.File == 'e' && move.TargetSquare.Rank >= 4 && move.TargetSquare.Rank <= 5)
            value += 1;  // Bonus for controlling the center

        return value;
    }
}
