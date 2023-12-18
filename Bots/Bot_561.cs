namespace auto_Bot_561;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_561 : IChessBot
{
    private int[] _values = { 0, 100, 250, 350, 500, 900, 1000 };

    Move RandMove(Move[] moves) => moves[Random.Shared.Next(moves.Length)];


    Move HighestValue(IEnumerable<Move> moves) => moves.Aggregate(
        new Move(),
        (agg, move) =>
            _values[(int)agg.CapturePieceType] > _values[(int)move.CapturePieceType]
            ? agg : move);

    IEnumerable<Move> MovesWithNoBadTrades(IEnumerable<Move> move, Board board) => move.Where(m => !MoveHasBadTrade(m, board));
    // Okay, let's work through this. The tuple here has a move and then whether or not that move leads to mate
    IEnumerable<(Move, bool)> MovesWithChecks(IEnumerable<Move> moves, Board board) =>
        moves
            // Turn moves into tuples with the move and whether or not it makes a check
            .Select(m => (m, MoveMakesCheck(board, m)))
            // Filter down to ones where we have checks
            .Where(t => t.Item2.Item1 || t.Item2.Item2)
            // Finally, return a tuple that has the move and whether or not the move is a mate
            .Select(t => (t.Item1, t.Item2.Item2));

    // First item is for if this is a check, second is if it's a mate
    (bool, bool) MoveMakesCheck(Board board, Move move)
    {
        board.MakeMove(move);
        var isCheck = CheckForCheck(board);
        board.UndoMove(move);

        return isCheck;
    }
    // Same here
    (bool, bool) CheckForCheck(Board board) => (board.IsInCheck(), board.IsInCheckmate());

    bool MoveHasBadTrade(Move move, Board board) => DoMoveGetMovesRevertMove(move, board).Any(m => _values[(int)m.CapturePieceType] > _values[(int)move.CapturePieceType]);

    bool WouldLosePieceNextTurn(Move move, Board board) => DoMoveGetMovesRevertMove(move, board, true)
        .Any(move1 => move1.TargetSquare == move.TargetSquare);

    Move[] DoMoveGetMovesRevertMove(Move move, Board board, bool capturesOnly = false)
    {
        board.MakeMove(move);
        var moves = board.GetLegalMoves(capturesOnly);
        board.UndoMove(move);

        return moves;
    }

    bool CheckMoveWithTest(Move move, Board board, Func<Board, Move, bool> tester)
    {
        board.MakeMove(move);
        var check = tester(board, move);
        board.UndoMove(move);

        return check;
    }

    bool MoveWouldLeadToMate(Move move, Board board)
    {
        board.MakeMove(move);
        var answer = board.GetLegalMoves(true).Any(m =>
        {
            board.MakeMove(m);
            var result = board.IsInCheckmate();
            board.UndoMove(m);
            return result;
        });
        board.UndoMove(move);
        return answer;
    }

    bool MoveWouldStaleMate(Move move, Board board) => CheckMoveWithTest(move, board, (b, _) => b.IsDraw() || b.IsRepeatedPosition());


    Move OneMoveSearch(Board board, Move[] moves, bool lateGame = false)
    {
        if (board.IsInCheck())
        {
            return RandMove(moves);
        }
        var noKingMoves =
            lateGame ? moves : moves.Where(m => m.MovePieceType != PieceType.King || m.IsCastles).ToArray();

        var noLossMoves =
            noKingMoves
                .Where(m => !WouldLosePieceNextTurn(m, board))
                .ToArray();

        var caps =
            MovesWithNoBadTrades(board.GetLegalMoves(true), board)
                .ToArray();

        var checks =
            MovesWithChecks(noKingMoves, board)
                .Where(t => !MoveHasBadTrade(t.Item1, board) && !WouldLosePieceNextTurn(t.Item1, board))
                .ToArray();

        var checksCollapsed =
            checks
                .Select(t => t.Item1)
                .ToArray();

        var checksWithCaps =
            checks
                .Select(t => t.Item1)
                .Where(t => caps.Contains(t))
                .ToArray();

        // We first want to check for mates
        var movesWithMate =
            checks.Where(m => m.Item2)
                .Select(m => m.Item1)
                .ToArray();

        return movesWithMate.Length > 0 ? movesWithMate[0] :
            checksWithCaps.Length > 0 ? HighestValue(checksWithCaps) :
            caps.Length > 0 ? HighestValue(caps) :
            checksCollapsed.Length > 0 ? RandMove(checksCollapsed) :
            noKingMoves.Length > 0 ? RandMove(noKingMoves) :
            noLossMoves.Length > 0 ? RandMove(noLossMoves) :
            RandMove(moves);
    }

    Move LowPieceCountMoves(Board board, Move[] moves)
    {
        var movesWithPawns = moves.Where(m => m.MovePieceType is PieceType.Pawn && !WouldLosePieceNextTurn(m, board)).ToArray();
        var pawnCaps = movesWithPawns.Where(m => m.IsCapture).ToArray();
        if (movesWithPawns.Length > 0)
        {
            return pawnCaps.Length > 0 ? HighestValue(pawnCaps) : OneMoveSearch(board, movesWithPawns, true);
        }

        return OneMoveSearch(board, moves, true);
    }

    private Move VeryEndGameMoves(Board board, Move[] moves)
    {
        var enemyKingSquare = board.GetKingSquare(!board.IsWhiteToMove);
        var myKingSquare = board.GetKingSquare(board.IsWhiteToMove);
        var kingMoves =
            moves
                .Where(m =>
                    m.MovePieceType == PieceType.King
                    ||
                     (myKingSquare.File < enemyKingSquare.File
                         ? m.TargetSquare.File > myKingSquare.File
                         : m.TargetSquare.File < myKingSquare.File) ||
                     (myKingSquare.Rank < enemyKingSquare.Rank
                         ? m.TargetSquare.Rank > myKingSquare.Rank
                         : m.TargetSquare.Rank < myKingSquare.Rank))
                .ToArray();

        return OneMoveSearch(board, kingMoves.Length > 0 ? kingMoves : moves);
    }

    public Move Think(Board board, Timer timer)
    {
        var moves = board.GetLegalMoves().ToArray();
        var movesNoMates = moves.Where(m => !MoveWouldStaleMate(m, board) && !MoveWouldLeadToMate(m, board)).ToArray();
        var allPieces = board.GetAllPieceLists().Aggregate(0, (i, list) => i + list.Count());
        var move = allPieces switch
        {
            > 10 => OneMoveSearch(board, movesNoMates.Length > 0 ? movesNoMates : moves),
            > 5 => LowPieceCountMoves(board, movesNoMates.Length > 0 ? movesNoMates : moves),
            _ => VeryEndGameMoves(board, movesNoMates.Length > 0 ? movesNoMates : moves)
        };

        return !move.IsPromotion ? move : new Move($"{move.StartSquare.Name}{move.TargetSquare.Name}q", board);
    }

}