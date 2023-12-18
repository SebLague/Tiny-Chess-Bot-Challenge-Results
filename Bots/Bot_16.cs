namespace auto_Bot_16;
using ChessChallenge.API;
using System.Linq;

public class Bot_16 : IChessBot
{
    int[] _pieceValues = { 0, 10, 30, 30, 50, 90, 900 };
    public int GetEvalScore(Board board, Move move)
    {
        board.MakeMove(move);
        // The score is just the sum of the values of all the bot's pieces,
        // minus the sum of the values of the bot's opponent
        int score = board.GetAllPieceLists().Sum(pieceList =>
            pieceList.Count * _pieceValues[(int)pieceList.TypeOfPieceInList] * (board.IsWhiteToMove == pieceList.IsWhitePieceList ? -1 : 1)
        );
        board.UndoMove(move);
        return score;
    }
    public Move Think(Board board, Timer timer)
    {
        int maxTurnLength = System.Math.Clamp(1000, timer.MillisecondsRemaining / 40, timer.MillisecondsRemaining / 10);

        Move[] moves = board.GetLegalMoves();
        return moves.OrderByDescending(move => GetEvalScore(board, move)).First();
    }
}