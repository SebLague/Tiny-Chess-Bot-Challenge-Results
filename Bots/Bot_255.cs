namespace auto_Bot_255;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_255 : IChessBot
{
    int NearKing(Piece piece, Board board, int multiplier, bool White)
    {
        // returns 0 if IsKing/IsRook and some value for piece being close to the king
        var king = board.GetKingSquare(White);
        return ((7 - Math.Abs(piece.Square.Rank - king.Rank)) * (7 - Math.Abs(piece.Square.File - king.File)) / 10) * multiplier;
    }
    int IsCenter(Piece piece, int width, int multiplier)
    {
        // returns multiplier if the piece is in the center (of given width) of the board and 0 if not
        if ((6 - width <= piece.Square.Rank) && (piece.Square.Rank <= width + 1) && (6 - width <= piece.Square.File) && (piece.Square.File <= width + 1)) return multiplier;
        return 0;
    }
    int AttackingPosibilities(Piece piece, Board board, int multiplier)
    {
        if (piece.IsKing || piece.IsPawn) return 0;
        if (piece.IsKnight) return BitboardHelper.GetKnightAttacks(piece.Square).ToString().Count(f => f == '1') * multiplier;
        return BitboardHelper.GetSliderAttacks(piece.PieceType, piece.Square, board).ToString().Count(f => f == '1') / 4 * multiplier;
    }
    int Evaluate(Board board, Move[] moves)
    {
        // Evaluate position on the board. If move is given, making a move and then evaluate a position
        // returns 10000 if checkmate and -10000 if draw
        // Extra: Rank of a pawn
        // Knight in the center 
        // Bishop in the center 
        // Queen in the center 
        // King not in the center 
        // Rook not in the center 
        // Near oponents king 
        // Pawn defended by a piece
        // Attacking posibilities 
        int eval = 0;
        bool m = moves is not null;
        if (m) board.MakeMove(moves[0]);
        if (board.IsInCheckmate() && m)
        {
            board.UndoMove(moves[0]);
            return 10000;
        }
        if (board.IsDraw() && m)
        {
            board.UndoMove(moves[0]);
            return -10000;
        }
        int pieces_count = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);
        foreach (var piecelist in board.GetAllPieceLists())
        {
            int _eval = 0;
            foreach (var piece in piecelist)
            {
                if (pieces_count > 12)
                {
                    if (piece.IsPawn) _eval += 100 + Math.Abs(piece.Square.Rank - (7 * Convert.ToInt32(!piece.IsWhite))) * 2 + IsCenter(piece, 4, 3) + NearKing(piece, board, 6, piece.IsWhite);
                    if (piece.IsBishop) _eval += 300 + IsCenter(piece, 4, 2);
                    if (piece.IsKnight) _eval += 300 + IsCenter(piece, 4, 4);
                    if (piece.IsQueen) _eval += 900 + IsCenter(piece, 5, 3);
                    if (piece.IsRook) _eval += 500 + IsCenter(piece, 4, -2);
                    if (piece.IsKing) _eval += IsCenter(piece, 4, -4);
                    _eval += NearKing(piece, board, 5, !piece.IsWhite) + AttackingPosibilities(piece, board, 4);
                    if (m && (moves[0].IsPromotion || moves[0].IsCastles || board.IsInCheck())) _eval += 15;
                }
                else
                {
                    if (piece.IsPawn) _eval += 110 + Math.Abs(piece.Square.Rank - (7 * Convert.ToInt32(!piece.IsWhite))) * 5 + NearKing(piece, board, 7, piece.IsWhite);
                    if (piece.IsBishop) _eval += 310 + IsCenter(piece, 4, 2);
                    if (piece.IsKnight) _eval += 300 + IsCenter(piece, 4, 5);
                    if (piece.IsQueen) _eval += 900 + IsCenter(piece, 5, 4);
                    if (piece.IsRook) _eval += 500 + IsCenter(piece, 4, -3);
                    if (piece.IsKing) _eval += IsCenter(piece, 4, 4);
                    _eval += NearKing(piece, board, 8, !piece.IsWhite) + AttackingPosibilities(piece, board, 5);
                    if (m && (moves[0].IsPromotion || board.IsInCheck())) _eval += 40;
                }
            }
            if (!piecelist.IsWhitePieceList) _eval *= -1;
            eval += _eval;
        }
        if (m) board.UndoMove(moves[0]);
        return eval;
    }

    Dictionary<Move, int> GetSafeMoves(Board board)
    {
        Dictionary<Move, int> safe_moves = new();
        var legal_moves = board.GetLegalMoves();

        foreach (var move in legal_moves)
        {
            int eval = Evaluate(board, new[] { move });
            if (eval == 10000) return new() { { move, 10000 } };
            if (eval == -10000)
            {
                safe_moves.Remove(move);
                continue;
            }
            if (move.CapturePieceType >= move.MovePieceType || board.SquareIsAttackedByOpponent(move.TargetSquare) == false) safe_moves[move] = eval;
        }
        if (safe_moves.Count != 0) return safe_moves;
        if (legal_moves.Length != 0) return new() { { legal_moves[0], 0 } };
        return null;
    }
    Dictionary<Move, int> OneMoveDeep(Board board, Dictionary<Move, int> safe_moves, bool White)
    {
        foreach (var move in safe_moves)
        {
            if (move.Value == 10000) return new() { { move.Key, 10000 } };

            board.MakeMove(move.Key);
            var opponents_moves = GetSafeMoves(board);
            board.UndoMove(move.Key);

            if (opponents_moves == null) return new() { { move.Key, 10000 } };
            if (opponents_moves.Values.First() == 10000 && safe_moves.Count > 1)
            {
                safe_moves.Remove(move.Key);
                continue;
            }

            opponents_moves = opponents_moves.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);

            if (White) safe_moves[move.Key] = opponents_moves.Values.First();
            else safe_moves[move.Key] = opponents_moves.Values.Last();

        }
        safe_moves = safe_moves.OrderBy(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        return safe_moves;
    }

    public Move Think(Board board, Timer timer)
    {
        bool White = board.IsWhiteToMove;
        var best_moves = OneMoveDeep(board, GetSafeMoves(board), White);
        if (White) return best_moves.Keys.Last();
        return best_moves.Keys.First();
    }
}