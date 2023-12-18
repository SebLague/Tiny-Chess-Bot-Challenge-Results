namespace auto_Bot_382;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_382 : IChessBot
{
    List<ulong> moveList = new List<ulong>();

    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }
    bool MoveIsCheckMate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheckmate();
        board.UndoMove(move);
        return isCheck;
    }
    public Move Think(Board board, Timer timer)
    {
        Random random = new Random();
        Move[] moves = board.GetLegalMoves().Where(x =>
        {
            board.MakeMove(x);
            bool ret = moveList.Count(y => y == board.ZobristKey) < 2;
            board.UndoMove(x);
            return ret;
        }).ToArray();
        Move[] takes = board.GetLegalMoves(true).Where(x =>
        {
            board.MakeMove(x);
            bool ret = moveList.Count(y => y == board.ZobristKey) < 2;
            board.UndoMove(x);
            return ret;
        }).ToArray();

        var filteredMoves = (from move in moves where !board.SquareIsAttackedByOpponent(move.TargetSquare) select move).ToArray();

        var hanging = (from move in takes where OkStep(board, move) select move).ToArray();


        var captures = (from move in takes
                        let Dif = (int)board.GetPiece(move.TargetSquare).PieceType - ((int)board.GetPiece(move.StartSquare).PieceType % 6)
                        where Dif >= 0
                        orderby Dif descending
                        select move).ToArray();

        var takeChecks = captures.Union(hanging).Where(x => MoveIsCheck(board, x))
            .OrderBy(x => (int)board.GetPiece(x.StartSquare).PieceType).ToArray();
        var moveChecks = filteredMoves.Where(x => MoveIsCheck(board, x))
            .OrderBy(x => (int)board.GetPiece(x.StartSquare).PieceType).ToArray();

        var pawnMoves = (from move in filteredMoves where move.MovePieceType == PieceType.Pawn && (move.IsPromotion && move.PromotionPieceType == PieceType.Rook) orderby board.IsWhiteToMove ? move.TargetSquare.Rank : (8 - move.TargetSquare.Rank) select move).ToArray();

        var ownHanging = from move in board.GetLegalMoves() where isHanging(board, move) orderby board.SquareIsAttackedByOpponent(move.TargetSquare) ? 1 : 0, (int)move.MovePieceType descending, move.MovePieceType - move.CapturePieceType descending select move;
        Move final;
        var checkMates = moveChecks.Union(takeChecks).Where(x => MoveIsCheckMate(board, x)).ToArray();
        if (checkMates.Length > 0)
        {
            final = checkMates[0];
        }
        else if (ownHanging.Count() > 0)
        {
            final = ownHanging.First();
        }
        else if (takeChecks.Length > 0)
        {
            final = takeChecks[0];
        }
        else if (moveChecks.Length > 0)
        {
            final = moveChecks[0];
        }
        else if (hanging.Length > 0)
        {
            final = hanging[0];
        }
        else if (captures.Length > 0)
        {
            final = captures[0];
        }
        else if (PieceCount(board) < 10 && pawnMoves.Length > 0)
        {
            final = pawnMoves[0];
        }
        else if (filteredMoves.Length > 0)
        {
            final = filteredMoves[random.Next(0, filteredMoves.Length)];
        }
        else
        {
            final = moves[random.Next(0, moves.Length)];
        }
        moveList.Add(board.ZobristKey);
        return final;
    }
    private float PieceCount(Board board)
    {
        return board.GetAllPieceLists().Sum(x => x.Count);
    }
    private bool OkStep(Board board, Move move)
    {
        return !board.SquareIsAttackedByOpponent(move.TargetSquare);
    }
    private bool isHanging(Board board, Move move)
    {
        return board.SquareIsAttackedByOpponent(move.StartSquare);
    }
}