namespace auto_Bot_29;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_29 : IChessBot
{
    bool? isWhite;
    List<Move> pastMoves = new List<Move>();

    Dictionary<PieceType, int> pieceCaptureValues = new Dictionary<PieceType, int>()
    {
        { PieceType.Pawn, 100 },
        { PieceType.Knight, 300 },
        { PieceType.Bishop, 300 },
        { PieceType.Rook, 500 },
        { PieceType.Queen, 900 },
        { PieceType.King, 10000 }
    };

    Dictionary<PieceType, int[]> piecePlacementValues = new Dictionary<PieceType, int[]>()
    {
        {PieceType.Pawn, new int[64] {
            0, 0, 0, 0, 0, 0, 0, 0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
            5, 5, 10, 25, 25, 10, 5, 5,
            0, 0, 0, 20, 20, 0, 0, 0,
            -5,-5,-10,-20,-20,-10,-5,-5,
            -10,-10,-20,-30,-30,-20,-10,-10,
            -50,-50,-50,-50,-50,-50,-50,-50
        }},
        {PieceType.Knight, new int[64] {
            -50,-40,-30,-30,-30,-30,-40,-50,
            -40,-20,0 ,0 ,0 ,0 ,-20,-40,
            -30 ,0 ,10 ,15 ,15 ,10 ,0 ,-30,
            -30 ,5 ,15 ,20 ,20 ,15 ,5 ,-30,
            -30 ,0 ,15 ,20 ,20 ,15 ,0 ,-30,
            -30 ,5 ,10 ,15 ,15 ,10 ,5 ,-30,
            -40 ,-20 ,0 ,5 ,5 ,0 ,-20 ,-40,
            -50 ,-40 ,-30 ,-30 ,-30 ,-30 ,-40 ,-50
        }},
        {PieceType.Bishop, new int[64] {
            -20 ,-10 ,-10 ,-10 ,-10 ,-10 ,-10 ,-20 ,
            -10 ,0 ,0 ,0 ,0 ,0 ,0 ,-10 ,
            -10 ,0 ,5 ,10 ,10 ,5 ,0 ,-10 ,
            -10 ,5 ,5 ,10 ,10 ,5 ,5 ,-10 ,
            -10 ,0 ,10 ,10 ,10 ,10 ,0 ,-10 ,
            -10 ,10 ,10 ,10 ,10 ,10 ,10 ,-10 ,
            -10 ,5 ,0 ,0 ,0 ,0 ,5 ,-10 ,
            -20 ,-10 ,-40 ,-10 ,-10 ,-40 ,-10 ,-20
        }},
        {PieceType.Rook, new int[64] {
            -20 ,-15 ,-15 ,-15 ,-15 ,-15 ,-15 ,-20 ,
            -15, -3, -3, -3, -3, -3, -3, -15 ,
             -3, 3, 3, 3, 3, 3, 3, -3,
             -3, 3, 3, 3, 3, 3, 3, -3,
             -3, 3, 3, 3, 3, 3, 3, -3,
             -3, 3, 3, 3 ,3, 3, 3, -3,
             -15, -6, -6 ,-6, -6, -6, -6, -15,
             -20, -15, -15 ,-15, -15, -15, -15, -20
        }},
        {PieceType.Queen, new int[64] {
            -20 ,-10 ,-10 ,-5 ,-5 ,-10, -10 ,-20 ,
             -10 ,0 ,0 ,0 ,0 ,0 , 0 ,-10,
             -10 ,0 ,5 ,5 ,5 ,5 , 0 ,-10,
             -5 ,0 ,5 ,5 ,5 ,5 , 0 ,-5,
             0 ,0 ,5 ,5 ,5 ,5 , 0 ,-5,
             -10 ,5 ,5 ,5 ,5 , 5, 0, -10,
             -10 ,0 ,5 ,0 ,0 , 0, 0, -10,
             -20 ,-10 ,-10 ,-5, -5, -10,-10 ,-20
        }},
        {PieceType.King, new int[64] {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            10, 20, 20, -20, -20, 20, 20, 10,
            20,  30, 30, -30, -30, 30, 30, 20,
            20,  20, -10, -20, -20, -10, 20, 20
        }}
    };

    public class MoveValuation
    {
        public Move move;
        public double value;
    }

    public Move Think(Board board, Timer timer)
    {
        isWhite ??= board.IsWhiteToMove;

        var evaluatedMoves = GetEvaluatedMoves(board, timer);

        pastMoves.Add(evaluatedMoves.First().move);

        return pastMoves.Last();
    }

    public List<MoveValuation> GetEvaluatedMoves(Board board, Timer timer)
    {
        List<MoveValuation> evaluatedMoves = new() { new MoveValuation() { move = Move.NullMove, value = -1 } };

        while (timer.MillisecondsRemaining > 0)
        {
            var moves = board.GetLegalMoves();
            evaluatedMoves = moves.Select(move => new MoveValuation() { move = move, value = EvaluateMove(board, move) }).OrderByDescending(x => x.value).ToList();

            break;
        }
        return evaluatedMoves;
    }

    double EvaluateMove(Board board, Move move)
    {
        double value = 0;

        // neato moves
        if (move.IsEnPassant || move.IsCastles || move.IsPromotion)
            value += 1000;

        // position
        if (isWhite == true)
            value += piecePlacementValues[move.MovePieceType][move.TargetSquare.Index];
        else
            value += piecePlacementValues[move.MovePieceType].Reverse().ElementAt(move.TargetSquare.Index);

        // capturing
        if (move.IsCapture)
            value += pieceCaptureValues[move.CapturePieceType];

        // threaten king
        board.MakeMove(move);
        if (board.IsInCheck())
            value += 50;
        else if (board.IsInCheckmate())
            value += 10000;
        board.UndoMove(move);

        // don't expose
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            value -= pieceCaptureValues[move.MovePieceType];

        // don't repeat too much
        if (pastMoves.TakeLast(3).All(x => x.MovePieceType == move.MovePieceType))
            value -= 100;

        return value;
    }
}