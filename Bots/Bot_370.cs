namespace auto_Bot_370;
/*
    This is just TuroChamp with a few minor additions.

    In order to give poor grandpa here the hint of a chance amongst all the youngsters,
    I made sure he does not go on too many unnecessary tangents (i.e. added alpha-beta pruning),
    kept his stubborness in check (i.e. taught him about such a lowly concept as draws) and
    allowed him to take some notes to assist with his memory (i.e. added something like a tt,
    but only for move ordering).
    
    Due to my severely lacking C# skills, the code was turned into a minor abomination
    to fit within the token limit. I do beg your forgiveness ;_;
*/

using ChessChallenge.API;
using System;
using static ChessChallenge.API.BitboardHelper;
using static ChessChallenge.API.PieceType;

public class Bot_370 : IChessBot
{
    private struct score_type
    {
        public double material, position;

        public score_type(double mat, double pos)
        {
            material = mat;
            position = pos;
        }
    }

    private bool smallereq(score_type lhs, score_type rhs) =>
        lhs.material < rhs.material || lhs.material == rhs.material && lhs.position <= rhs.position;

    private readonly double[] piece_values = { 0, 1, 3, 3.5, 5, 10, 0.0001 }; // Technically the king had no value assigned in Turings paper, but I really do not like dividing by zero
    private readonly PieceType[] pieces = { Pawn, Knight, Bishop, Rook, Queen, King };

    private Board board;
    private bool is_white;
    private int time_for_move;
    private Timer timer;

    private Move[] order_tt = new Move[0x10000], root_moves;

    double rounded_sqrt(double v) =>
        Math.Truncate(Math.Sqrt(v) * 10 + 0.5) / 10;

    public Move Think(Board new_board, Timer new_timer)
    {
        board = new_board;
        is_white = board.IsWhiteToMove;
        timer = new_timer;
        time_for_move = timer.MillisecondsRemaining / 20;

        root_moves = board.GetLegalMoves();
        Move m = root_moves[0];
        for (int depth = 1; timer.MillisecondsElapsedThisTurn * 2 < time_for_move;)
            m = turochamp_ex(depth++) ?? m;

        return m;
    }

    private Move? turochamp_ex(int max_depth)
    {
        Move? best_move = null;
        score_type best_score = new score_type();

        foreach (Move m in root_moves)
        {
            board.MakeMove(m);
            score_type value = minimax_value(m, m.IsCastles, max_depth - 1, best_score, new score_type(9999999, 9999999));
            board.UndoMove(m);

            if (best_score.material < value.material || best_score.material == value.material && best_score.position < value.position)
            {
                best_score = value;
                best_move = m;
            }
            if (timer.MillisecondsElapsedThisTurn > time_for_move) return null;
        }

        return best_move;
    }

    private score_type minimax_value(Move last_move, bool has_castled, int remaining_depth, score_type alpha, score_type beta)
    {
        if (board.IsDraw()) return new score_type(1, 0);

        var is_my_turn = board.IsWhiteToMove == is_white;

        int moves_tried = 0;

        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves);

        var tt_idx = board.ZobristKey & 0xffff;
        moves.Sort((lhs, rhs) =>
        {
            if (lhs != rhs)
                if (lhs == order_tt[tt_idx]) return -1;
                else if (rhs == order_tt[tt_idx]) return 1;
            return rhs.CapturePieceType.CompareTo(lhs.CapturePieceType);
        });

        foreach (Move m in moves)
        {
            if (timer.MillisecondsElapsedThisTurn > time_for_move) return beta;

            bool considerable = remaining_depth > 0 || m.IsCapture &&
            (
                    m.CapturePieceType > m.MovePieceType ||
                    last_move.IsCapture && last_move.TargetSquare == m.TargetSquare ||
                    !board.SquareIsAttackedByOpponent(m.TargetSquare)
            );

            board.MakeMove(m);
            if (!considerable && !board.IsInCheckmate())
            {
                board.UndoMove(m);
                continue;
            }
            ++moves_tried;
            score_type value = minimax_value(m, is_my_turn && m.IsCastles || has_castled, remaining_depth - 1, alpha, beta);
            board.UndoMove(m);

            if (is_my_turn)
            {
                if (smallereq(alpha, value))
                {
                    order_tt[tt_idx] = m;
                    alpha = value;
                }
                if (smallereq(beta, value)) return beta;
            }
            else
            {
                if (smallereq(value, beta))
                {
                    order_tt[tt_idx] = m;
                    beta = value;
                }
                if (smallereq(value, alpha)) return alpha;
            }
        }

        if (moves_tried == moves.Length)
            return is_my_turn ? alpha : beta;

        var eval = evaluate(has_castled);
        return is_my_turn ?
            smallereq(eval, alpha) ? alpha : eval :
            smallereq(beta, eval) ? beta : eval;
    }

    private score_type evaluate(bool castled_bonus)
    {
        var is_my_turn = board.IsWhiteToMove == is_white;
        double position = 0, own_material = 0, opponent_material = 0;

        if (board.IsInCheckmate())
            if (is_my_turn)
                opponent_material += 1000;
            else
                own_material += 1000;

        Span<Move> moves = stackalloc Move[218];
        if (is_my_turn)
            board.GetLegalMovesNonAlloc(ref moves);
        else
        {
            bool in_check = board.IsInCheck();
            if (in_check)
                position += 1.5;

            board.ForceSkipTurn();
            board.GetLegalMovesNonAlloc(ref moves);
            if (!in_check)
                foreach (Move m in moves)
                {
                    board.MakeMove(m);
                    if (board.IsInCheckmate())
                    {
                        board.UndoMove(m);
                        position += 1;
                        break;
                    }
                    board.UndoMove(m);
                }
            board.UndoSkipTurn();
        }

        Span<int> move_counts = stackalloc int[64], defenders_count = stackalloc int[64], defending_piece_count = stackalloc int[64];
        foreach (Move m in moves)
            if (!m.IsCastles)
                move_counts[m.StartSquare.Index] += m.IsCapture ? 2 : 1;
            else
                castled_bonus = true;

        if (castled_bonus || board.HasKingsideCastleRight(is_white) || board.HasQueensideCastleRight(is_white))
            position += 1;

        foreach (PieceType type in pieces)
            foreach (Piece p in board.GetPieceList(type, is_white))
            {
                var attack = GetPieceAttacks(type, p.Square, board.AllPiecesBitboard, is_white);

                while (attack > 0)
                {
                    var sq = ClearAndGetIndexOfLSB(ref attack);
                    ++defenders_count[sq];
                    if (type != Pawn)
                        ++defending_piece_count[sq];
                }
            }

        foreach (PieceType type in pieces)
        {
            var value = piece_values[(int)type];
            opponent_material += value * board.GetPieceList(type, !is_white).Count;

            foreach (Piece p in board.GetPieceList(type, is_white))
            {
                own_material += value;
                var idx = p.Square.Index;
                if (type == Pawn)
                {
                    position += 0.2 * (is_white ? p.Square.Rank - 1 : 6 - p.Square.Rank);
                    if (defending_piece_count[idx] > 0)
                        position += 0.3;
                }
                else
                {
                    if (type < Queen)
                        position += defenders_count[idx] > 1 ? 1.5 : defenders_count[idx] > 0 ? 0.5 : 0;

                    position += rounded_sqrt(move_counts[idx]);
                }
            }
        }

        var pseudo_queen_targets = GetSliderAttacks(Queen, board.GetKingSquare(is_white), board);
        position -= rounded_sqrt(GetNumberOfSetBits(pseudo_queen_targets & (~board.AllPiecesBitboard)) + 2 * GetNumberOfSetBits(pseudo_queen_targets & (is_white ? board.BlackPiecesBitboard : board.WhitePiecesBitboard)));

        return new score_type(own_material / opponent_material, position);
    }
}
