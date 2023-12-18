namespace auto_Bot_85;
using ChessChallenge.API;

public class Bot_85 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Square king_square = board.GetKingSquare(board.IsWhiteToMove);

        Move move = Move.NullMove;
        int greatest_change = int.MinValue;
        for (int i = 0; i < moves.Length; i++)
        {
            int distance_change = distance_from_king(king_square, moves[i].TargetSquare) - distance_from_king(king_square, moves[i].StartSquare);
            if (distance_change > greatest_change)
            {
                move = moves[i];
                greatest_change = distance_change;
            }
        }
        return move;
    }
    private int distance_from_king(Square king_square, Square piece_square)
    {
        int rank_distance = king_square.Rank - piece_square.Rank;
        int file_distance = king_square.File - piece_square.File;
        return (rank_distance) * (rank_distance) + (file_distance) * (file_distance);
    }
}