namespace auto_Bot_6;
using ChessChallenge.API;
using System.Linq;

public class Bot_6 : IChessBot
{
    int[] a = { 0, 100, 300, 300, 500, 900, 10000 };
    string[] b = { "e2e3", "e7e6", "f1c4", "f8c5", "d1f3", "d8f6" };
    public Move Think(Board board, Timer timer)
    {
        // it just chooses a move, quit simple really
        return (board.PlyCount < 6 && board.GetLegalMoves().Any(c => c.Equals(new Move(b[board.PlyCount], board)))) ? new Move(b[board.PlyCount], board) : board.GetLegalMoves()[board.GetLegalMoves().ToList().Select((d, e) => { board.MakeMove(d); int f = board.GetLegalMoves().Length == 0 ? 9999999 : board.GetLegalMoves().ToList().Select(g => { board.MakeMove(g); int h = board.GetLegalMoves().Length == 0 ? -9999999 : board.GetLegalMoves().ToList().Select(i => { board.MakeMove(i); int j = board.GetAllPieceLists().Sum(pl => (board.IsWhiteToMove ^ pl.IsWhitePieceList ? 1 : -1) * pl.Count * a[(int)pl.TypeOfPieceInList]); board.UndoMove(i); return j; }).Max(); board.UndoMove(g); return h; }).Min(); board.UndoMove(d); return new { k = f, l = e }; }).Aggregate((m, n) => (m.k > n.k) ? m : n).l];
    }
}