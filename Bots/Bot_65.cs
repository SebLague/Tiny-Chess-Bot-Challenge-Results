namespace auto_Bot_65;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_65 : IChessBot
{
    readonly IDictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>(){
        {PieceType.None,    00}, // none
        {PieceType.Pawn,    10}, // pawn
        {PieceType.Knight,  30}, // knight
        {PieceType.Bishop,  35}, // bishop
        {PieceType.Rook,    50}, // rook
        {PieceType.Queen,   90}, // queen
        {PieceType.King,    1000} // king
    };

    readonly List<Move> doneMoves = new List<Move>();

    public int Eval(Board board, Move currentmove, bool playerIsWhite, bool turnIsWhite)
    {
        if (board.IsInCheckmate()) return pieceValues[PieceType.King] * (turnIsWhite == playerIsWhite ? -1 : 1);
        if (board.IsDraw()) return -pieceValues[PieceType.King];

        int evalnum = 0;
        PieceList[] pieces = board.GetAllPieceLists();

        foreach (PieceList piecelist in pieces)
            foreach (Piece piece in piecelist)
                evalnum += pieceValues[piece.PieceType] * (playerIsWhite == piece.IsWhite ? 1 : -1);

        return evalnum - (turnIsWhite == playerIsWhite ? doneMoves.Count(move => move.Equals(currentmove)) * 15 : 0);
    }

    public (int, Move) Minimax(Board board, Move currentmove, int depth, int alpha, int beta, bool playerIsWhite, bool turnIsWhite)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) return (Eval(board, currentmove, playerIsWhite, turnIsWhite), Move.NullMove);

        Move[] moves = board.GetLegalMoves();

        if (playerIsWhite == turnIsWhite)
        {

            int besteval = int.MinValue;
            Move bestmove = moves[0];

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                turnIsWhite = board.IsWhiteToMove;

                (int evalnum, _) = Minimax(board, currentmove.IsNull ? move : currentmove, depth - 1, alpha, beta, playerIsWhite, turnIsWhite);
                if (evalnum > besteval)
                {
                    besteval = evalnum;
                    bestmove = move;
                }

                board.UndoMove(move);
                turnIsWhite = board.IsWhiteToMove;

                if (evalnum > beta) break;
                alpha = Math.Max(alpha, evalnum);
            }

            return (besteval, bestmove);

        }
        else
        {

            int worsteval = int.MaxValue;
            Move worstmove = moves[0];

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                turnIsWhite = board.IsWhiteToMove;

                (int evalnum, _) = Minimax(board, currentmove.IsNull ? move : currentmove, depth - 1, alpha, beta, playerIsWhite, turnIsWhite);
                if (evalnum < worsteval)
                {
                    worsteval = evalnum;
                    worstmove = move;
                }

                board.UndoMove(move);
                turnIsWhite = board.IsWhiteToMove;

                if (evalnum < alpha) break;
                beta = Math.Min(beta, evalnum);
            }

            return (worsteval, worstmove);
        }
    }

    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        int depth = 4;

        if (Eval(board, Move.NullMove, isWhite, isWhite) > 250) depth = 5;
        if (Eval(board, Move.NullMove, isWhite, isWhite) > 500) depth = 6;

        if (timer.MillisecondsRemaining < 30 * 1000) depth -= 1;
        if (timer.MillisecondsRemaining < 10 * 1000) depth = 3;

        (int besteval, Move bestmove) = Minimax(board, Move.NullMove, depth, int.MinValue, int.MaxValue, isWhite, isWhite);
        doneMoves.Add(bestmove);

        return bestmove.IsNull ? board.GetLegalMoves()[0] : bestmove;
    }
}