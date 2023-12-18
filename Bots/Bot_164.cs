namespace auto_Bot_164;
using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

public class Bot_164 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king.
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 0 };

    Move[] next_move = new Move[32];
    bool endgame; // initialized to false by default.
    Board board;

    // calculate the value that can be lost in the next move, after a given move. This is just a heuristic.
    int FloatingValue(Move move) => board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)(move.IsPromotion ? move.PromotionPieceType : move.MovePieceType)] : 0;

    public Move Think(Board _board, Timer timer)
    {
        board = _board;
        // check for endgame condition, that there is less than 3 major pieces left.
        endgame |= board.GetAllPieceLists().Sum(list => list.Count(piece => piece.IsWhite != board.IsWhiteToMove && !piece.IsPawn)) <= 3;
        // move the queue of optimal moves by 2 forward.
        for (int i = 0; i < 30; i++)
            next_move[i] = next_move[i + 2];
        // iterative deepening minmax. (the persistent thing is the next_move array)
        int best = 0;
        for (int max_depth = 2; max_depth < 6 && Math.Abs(best) < 100000; max_depth++)
            best = miniMax(0, max_depth, -200000, 200000, 0);
        // return the best move from the next_move array.
        return next_move[0];
    }

    // alpha-beta pruning minmax
    int miniMax(int depth, int max_depth,
             int best, int beta, int floating_value)
    {
        // if draw or checkmate, return the appropriate score.
        if (board.IsDraw())
            return depth % 2 * 2 - 1;
        if (board.IsInCheckmate())
            return -100000;
        // look at the legal moves in this position.
        var moves = board.GetLegalMoves();
        // increase search depth if there is less than 4 options
        if (moves.Length < 4 && max_depth < 11)
            max_depth++;
        // evaluate the board
        if (depth >= max_depth || moves.Length == 0)
        {
            bool white = board.IsWhiteToMove;
            // get the piece value on the board.
            int total = board.GetAllPieceLists().Sum(
                list => list.Sum(
                    piece => (piece.IsWhite == white ? 1 : -1) * pieceValues[(int)piece.PieceType]));
            if (endgame)
            {
                int sign = Math.Sign(total);
                bool winner_white = total > 0 == white;
                Square x = board.GetKingSquare(true);
                Square y = board.GetKingSquare(false);
                // walk toward the losing kind or away from the winning king.
                total += (9 - (int)new Vector2(x.Rank - y.Rank, x.File - y.File).Length()) / 2 * sign;
                // prefer to push the opponents king into the corner with the same color as the bishop.
                // also prefer to walk my own kind (if I'm losing) into the center or the corner with the opposite color.
                if (winner_white) x = y;
                total += 2 * Math.Abs(x.Rank + ((board.GetPieceBitboard(PieceType.Bishop, winner_white) & 0x55AA55AA55AA55AAu) != 0 ? 7 - x.File : x.File) - 7) * sign;
            }

            // keep castlerights if possible. (pins the king in the early game)
            bool HasCastleRight(bool white) => board.HasKingsideCastleRight(white) || board.HasQueensideCastleRight(white);
            if (HasCastleRight(white))
                total++;
            if (HasCastleRight(!white))
                total--;

            // add current floating value, that could be lost, as we don't know how this game progresses.
            return total + floating_value;
        }
        // recursively check each of the moves. (minmax)
        // start with the most likely best move according to heuristics.
        foreach (Move move in moves.OrderByDescending(move =>
                // evaluate a move, that is not executed yet
                move == next_move[depth] ? 30 :
                pieceValues[(int)move.CapturePieceType]
                + pieceValues[(int)move.PromotionPieceType]
                - FloatingValue(move)
                + Random.Shared.Next(2) // randomize order
            ))
        {
            int floating = FloatingValue(move);
            board.MakeMove(move);
            int score = -miniMax(depth + 1, max_depth, -beta, -best, floating);
            board.UndoMove(move);
            if (score > best) // >= is really bad for some reason...
            {
                best = score;
                next_move[depth] = move;
                if (best >= beta || best >= 100000)
                    break;
            }
        }
        return best;
    }
}
