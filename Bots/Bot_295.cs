namespace auto_Bot_295;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_295 : IChessBot
{
    // At some point, I had the absolutely absolutely *brilliant* idea to make 
    // a bot in only a single statement! Dumb idea! — but it taught me a lot!

    Func<Board, int, int, int, Func<bool>, int> search;

    // It ended up being two statements (more like 1.5 if you're kind)
    // because I forgot to realize that the method I chose (traditional Negamax
    // w/ Alpha-Beta pruning) needs recursion to work, and by that time, I didn't
    // have time to start over with a non-recursive method, so I improvised!

    public Move Think(Board board, Timer timer) =>
        Enumerable.Range(0, 100)
            .Aggregate(
                (
                    nightly: (depth: 0, score: int.MinValue + 1, move: Move.NullMove),
                    stable: (depth: -1, score: int.MinValue + 1, move: Move.NullMove)
                ),
                (previousEvaluation, currentDepth) =>
                    timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / (60 - Math.Min(59, board.PlyCount))
                        ? previousEvaluation                                // Don't start a new search if time is up
                        : Enumerable.Repeat(
                                board.GetLegalMoves()
                                    .Select(
                                        move =>
                                            (
                                                move,
                                                -board.MakeMove(move, board =>
                                                    (search = new(
                                                        (board, depth, alpha, beta, shouldCancel) =>
                                                            board.GetLegalMoves().Length == 0
                                                                ? board.IsDraw() ? 0 : -1000000
                                                                : Enumerable.Repeat(
                                                                    depth <= 0                      // Don't calculate Standpat/Evaluation unless we have to
                                                                        ?
                                                                        // EVALUATION //
                                                                        (board.GetAllPieceLists()
                                                                            .Sum(pieceList =>
                                                                                new[] { 0, 100, 320, 330, 500, 900, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                * pieceList.Count
                                                                                * (pieceList.IsWhitePieceList ? 1 : -1)
                                                                            )
                                                                        + Enumerable.Repeat(
                                                                            (
                                                                                board.GetAllPieceLists(),
                                                                                1 - Math.Min(
                                                                                    1,
                                                                                        board.GetAllPieceLists()
                                                                                            .Sum(
                                                                                                pieceList =>
                                                                                                    new[] { 0, 0, 10, 10, 20, 45, 0 }[(int)pieceList.TypeOfPieceInList]
                                                                                                    * pieceList.Count
                                                                                            )
                                                                                        * 256 / 125
                                                                                    )

                                                                            ),
                                                                            1
                                                                        )
                                                                            .Select(
                                                                                pieceLists_endgameT => pieceLists_endgameT.Item1
                                                                                    .Sum(pieceList => pieceList.Sum(
                                                                                        piece => new Func<int, int, int>[] {
                                                                                            (x, y) => 0,
                                                                                            (x, y) => ((3 * (y - 1) * (8 - Math.Abs((x * 2) - 7))) * (256 - pieceLists_endgameT.Item2) + (20 * (y - 1)) * pieceLists_endgameT.Item2) / 256,
                                                                                            (x, y) => 30 - 3 * (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                                            (x, y) => 10 - (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7)),
                                                                                            (x, y) => 0,
                                                                                            (x, y) => 0,
                                                                                            (x, y) => (((7 - y) * Math.Abs((x * 2) - 7) - 25) * (256 - pieceLists_endgameT.Item2) + (50 - 5 * (Math.Abs(x * 2 - 7) + Math.Abs(y * 2 - 7))) * pieceLists_endgameT.Item2) / 256,
                                                                                        }[(int)piece.PieceType](piece.IsWhite ? piece.Square.File : 7 - piece.Square.File, piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank)
                                                                                    ) * (pieceList.IsWhitePieceList ? 1 : -1))
                                                                            )
                                                                            .First()
                                                                        ) * (board.IsWhiteToMove ? 1 : -1)
                                                                        : int.MinValue,                     // Don't calculate standpat/evaluation unless we have to
                                                                    1
                                                                )
                                                                    // NEGAMAX + ALPHA-BETA PRUNING + QUIESCENCE //
                                                                    .Select(
                                                                        standPat => shouldCancel()
                                                                            ? standPat
                                                                            : board.GetLegalMoves(depth <= 0)
                                                                                // MOVE ORDERING //
                                                                                .OrderByDescending(
                                                                                    move =>
                                                                                        -Convert.ToInt32(board.SquareIsAttackedByOpponent(move.TargetSquare))
                                                                                        + (move.CapturePieceType - move.MovePieceType)
                                                                                        + (int)move.PromotionPieceType
                                                                                        + Convert.ToInt32(move.IsCastles)
                                                                                )
                                                                                .Aggregate(
                                                                                    Math.Max(alpha, standPat),
                                                                                    (alpha, move) =>
                                                                                        alpha >= beta   // If alpha, score, or stand pat >= beta, we prune
                                                                                            ? beta
                                                                                            : Math.Max(
                                                                                                alpha,
                                                                                                board.MakeMove(move, board => -search(board, depth - 1, -beta, -alpha, shouldCancel))
                                                                                            )

                                                                                )
                                                                    )
                                                                    .First()
                                                    ))(board, currentDepth, int.MinValue + 1, int.MaxValue, () => timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / (60 - Math.Min(59, board.PlyCount)))
                                                )
                                            )
                                    )
                                    .MaxBy(move_score => move_score.Item2),
                                1
                            )
                                .Select(
                                    move_score => (
                                        nightly: (
                                            depth: currentDepth,
                                            score: move_score.Item2,
                                            move: move_score.Item1
                                        ),
                                        stable: previousEvaluation.nightly      // We can now grantee "nightly" did not cancel partway through.
                                    )
                                )
                                .First()
            ).stable.move;

    // You know those ideas that seem really smart in your head, but once you 
    // actually do them, you realize how stupid they are? “What if I made a chess 
    // bot in just a single C# statement?” is one of those ideas.

    // However, the point of this challenge was to learn, and that I did!
    // I learned a lot from this — specifically about Linq, which I'd never used before!
}

// ChessChallenge.API made this entire challenge much, much easier!
// However, there's no functional way of making and unmaking moves on
// a board. Since it's necessary for making a bot in one line possible, I
// decided to not count it against the "one" in "one-statement chess bot".
public static class Extensions
{
    public static T MakeMove<T>(this Board board, Move move, Func<Board, T> func)
    {
        board.MakeMove(move);
        T value = func(board);
        board.UndoMove(move);
        return value;
    }
}