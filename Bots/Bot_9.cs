namespace auto_Bot_9;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class Bot_9 : IChessBot
{

    private int myMoveCount = 0;
    private readonly List<string> _openingMoves = new List<string> { "e2e3", "d1f3", "f1c4" };
    public Move Think(Board board, Timer timer)
    {
        var moveMap = new MoveMap(board);
        if (myMoveCount < _openingMoves.Count)
        {
            var openingMove = new Move(_openingMoves[myMoveCount], board);
            openingMove = moveMap.AllMoves.FirstOrDefault(m => m == openingMove);
            if (!openingMove.IsNull)
            {
                return openingMove;
            }
        }
        myMoveCount = myMoveCount + 1;
        var boardState = new BoardState(moveMap);
        return boardState.GetNextMove(board);
    }

    internal class MoveMap
    {

        internal List<Move> CaptureMoves;
        internal List<Move> NonCaptureMove;
        internal List<Move> AllMoves;

        internal MoveMap(Board board)
        {
            CaptureMoves = board.GetLegalMoves(true).ToList();
            AllMoves = board.GetLegalMoves().ToList();
            NonCaptureMove = AllMoves.Except(CaptureMoves).OrderBy(x => Guid.NewGuid()).ToList();
        }
    }

    internal class BoardState
    {

        private MoveMap _moveMap;

        internal BoardState(MoveMap moveMap)
        {
            _moveMap = moveMap;
        }

        internal Move GetNextMove(Board board)
        {
            var captureMovesScored = CaptureMoveScore.ScoreAndSortCaptureMoves(_moveMap.CaptureMoves, board);
            var nextJustDoItCaptureMove = captureMovesScored.FirstOrDefault(m => m.Item2.JustDoIt);
            if (nextJustDoItCaptureMove != null)
            {
                return nextJustDoItCaptureMove.Item1;
            }
            var scoredNonCaptureMoves =
                _moveMap
                .NonCaptureMove
                .Select(m => new Tuple<Move, JustMoveScore>(m, new JustMoveScore(m, board, captureMovesScored.Count)))
                .OrderBy(m => m.Item2.CausesStalemate ? 1 : 0)
                .ThenBy(m => m.Item2.EnemyCaptureCount)
                .ThenBy(m => m.Item2.CausesCheck ? 0 : 1)
                .ThenByDescending(m => m.Item2.PossibleCheckmateCount)
                .ThenByDescending(m => m.Item2.PossibleCheckCount)
                .ThenByDescending(m => m.Item2.PossibleCaptureCount)
                .ThenBy(m => m.Item1.MovePieceType == PieceType.King ? 1 : 0)
                .ToList();
            var causesCheckmateNonCaptureMove = scoredNonCaptureMoves.FirstOrDefault(m => m.Item2.CausesCheckMate);
            if (causesCheckmateNonCaptureMove != null)
            {
                return causesCheckmateNonCaptureMove.Item1;
            }
            var nextJustDoItNonCaptureMove = scoredNonCaptureMoves.FirstOrDefault(m => m.Item2.JustDoIt);
            if (nextJustDoItNonCaptureMove != null)
            {
                return nextJustDoItNonCaptureMove.Item1;
            }
            if (captureMovesScored.Any())
            {
                return captureMovesScored[0].Item1;
            }
            return scoredNonCaptureMoves.First().Item1;
        }

        internal class MoveScore
        {
            internal bool CausesCheckMate = false;
            internal bool CausesCheck = false;
            internal bool CausesStalemate = false;
            internal bool JustDoIt = false;

            internal MoveMap InitMove(Move m, Board board)
            {
                board.MakeMove(m);

                var enemyMoveMap = new MoveMap(board);

                CausesCheckMate = board.IsInCheckmate();
                CausesCheck = board.IsInCheck();
                CausesStalemate = !CausesCheck && enemyMoveMap.AllMoves.Count == 0;

                return enemyMoveMap;
            }
        }

        internal class CaptureMoveScore : MoveScore
        {

            internal int ValueDifference;

            internal CaptureMoveScore(Move m, Board board)
            {
                var enemyMoveMap = InitMove(m, board);

                board.UndoMove(m);

                ValueDifference = m.CapturePieceType - m.MovePieceType; // 5 to -5
                JustDoIt = !CausesStalemate && (CausesCheckMate || ValueDifference > -1);
            }

            public static List<Tuple<Move, CaptureMoveScore>> ScoreAndSortCaptureMoves(List<Move> captureMoves, Board board)
            {
                return captureMoves.Select(m => new Tuple<Move, CaptureMoveScore>(m, new CaptureMoveScore(m, board)))
                    .OrderBy(m => m.Item2.CausesCheckMate ? 0 : 1)
                    .ThenBy(m => m.Item2.CausesStalemate ? 1 : 0)
                    .ThenBy(m => m.Item2.CausesCheck ? 0 : 1)
                    .ThenByDescending(m => m.Item2.ValueDifference)
                    .ThenByDescending(m => (int)m.Item1.MovePieceType)
                    .ToList();
            }
        }

        internal class JustMoveScore : MoveScore
        {
            internal int PossibleCheckmateCount = 0;
            internal int PossibleCheckCount = 0;
            internal int PossibleCaptureCount = 0;
            internal int EnemyCaptureCount = 0;

            internal JustMoveScore(Move m, Board board, int currentPossibleCaptureCount)
            {
                var enemyMoveMap = InitMove(m, board);

                var enemyCaptureMovesScored = CaptureMoveScore.ScoreAndSortCaptureMoves(enemyMoveMap.CaptureMoves, board);
                EnemyCaptureCount = enemyCaptureMovesScored.Count();
                if (enemyMoveMap.AllMoves.Count == 0)
                {
                    board.UndoMove(m);
                    return;
                }
                var enemyMove = enemyCaptureMovesScored.Any() ? enemyCaptureMovesScored[0].Item1 : enemyMoveMap.NonCaptureMove.FirstOrDefault();
                board.MakeMove(enemyMove);

                var captureMoves = board.GetLegalMoves(true).ToList();
                var checkMoves = captureMoves.Where(m => m.CapturePieceType == PieceType.King);
                foreach (var checkMove in checkMoves)
                {
                    board.MakeMove(checkMove);
                    if (board.IsInCheckmate())
                    {
                        PossibleCheckmateCount++;
                    }
                    if (board.IsInCheck())
                    {
                        PossibleCheckCount++;
                    }
                    board.UndoMove(checkMove);
                }

                PossibleCaptureCount = captureMoves.Count;
                JustDoIt = PossibleCaptureCount > currentPossibleCaptureCount || PossibleCheckCount > 0;

                board.UndoMove(enemyMove);
                board.UndoMove(m);
            }
        }
    }
}