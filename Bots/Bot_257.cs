namespace auto_Bot_257;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_257 : IChessBot
{

    Dictionary<ulong, int> position_table = new Dictionary<ulong, int>();
    int[] pieceValues = { 60, 40, 27, 27, 10 };

    int time_limit = -1;
    int time_buffer = -1;
    int expected_moves = 40;
    int buffer_factor = 30;
    public Move Think(Board board, Timer timer)
    {
        if (time_limit == -1)
        {
            time_limit = timer.MillisecondsRemaining;
            time_buffer = (int)(time_limit / buffer_factor);
        }
        if (timer.MillisecondsRemaining < time_buffer)
        {
            time_limit = timer.MillisecondsRemaining;
            expected_moves = 40;
            time_buffer = 0;
        }
        int time_for_move = (time_limit - time_buffer) / expected_moves;
        int j = 0;
        if (timer.MillisecondsRemaining < 2000)
        {
            j = 2;
        }
        int high_score = -1000;
        int move_i = 0;
        Move[] moves = board.GetLegalMoves();
        int c = moves.Length;
        int score = 0;

        List<int> new_scores = new List<int>();
        int bm = 0;
        for (int i = 0; i < c; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsInCheckmate())
            {
                return moves[i];
            }
            board.UndoMove(moves[i]);
            //score = NegaMax(board, timer, j, moves[i], int.MaxValue);
            score = -AlphaBeta(board, int.MinValue, int.MaxValue, j, moves[i]);
            new_scores.Add(score);
            if (score > high_score)
            {
                high_score = score;
                move_i = i;
            }

            board.UndoMove(moves[i]);
        }
        bm = move_i;
        //DivertedConsole.Write("\n");
        while (timer.MillisecondsElapsedThisTurn < time_for_move && true)
        {
            List<int> scores = Kloniraj(new_scores);
            new_scores.Clear();

            //new_scores.Clear();
            j++;
            //DivertedConsole.Write(j);
            high_score = int.MinValue;
            int k = 0;
            //DivertedConsole.Write(j);

            while (k < moves.Length)
            {
                if (timer.MillisecondsRemaining < 2000 || timer.MillisecondsElapsedThisTurn > time_for_move)
                {
                    //DivertedConsole.Write("loop finished once");

                    return moves[bm];
                }
                int i = scores.FindIndex(row => row == scores.Max());

                score = -AlphaBeta(board, int.MinValue, int.MaxValue, j, moves[i]);
                if (board.IsInCheckmate())
                {
                    return moves[i];
                }
                if (score > high_score)
                {
                    high_score = score;
                    move_i = i;
                }

                board.UndoMove(moves[i]);
                new_scores.Add(score);
                scores[i] = int.MinValue;
                k++;
            }
            bm = move_i;

        }
        return moves[bm];
    }

    private List<int> Kloniraj(List<int> ls)
    {
        string s = "";
        foreach (int i in ls)
        {
            s += i.ToString();
            s += ";";
        }
        List<int> l = new List<int>();
        string sub = "";
        foreach (char c in s)
        {
            if (c == ';')
            {
                l.Add(int.Parse(sub));
                sub = "";
                continue;
            }
            sub += c;
        }

        return l;
    }

    private int Quiesce(Board board, Move move, int alpha, int beta)
    {
        int stand_pat = -Eval(board, move);

        if (stand_pat >= beta)
        {
            return beta;
        }
        if (stand_pat > alpha)
        {
            alpha = stand_pat;

        }

        Move[] moves = board.GetLegalMoves(true);
        int score;
        foreach (Move mv in moves)
        {
            score = -Quiesce(board, mv, -beta, -alpha);
            board.UndoMove(mv);

            if (score >= beta)
            {

                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
            }
        }

        return alpha;
    }
    private int Eval(Board board, Move move)
    {
        int p1 = GetColorValue(board);
        board.MakeMove(move);
        Move[] moves = board.GetLegalMoves();

        int p2 = GetColorValue(board);

        if (board.TrySkipTurn())
        {
            p1 = GetColorValue(board);
            board.UndoSkipTurn();
        }
        else if (board.IsInCheckmate())
        {
            return -int.MaxValue;
        }
        else if (board.IsDraw())
        {
            return 0;
        }

        return p1 - p2;
    }

    /*
     alpha beta(depthleft=?, alpha=MinValue, beta=MaxValue)
            if depthleft == 0:
                return -Eval()

            depthleft--

            score

            foreach move:
                score = -alphabeta(depthleft, -beta, -alpha)
                alpha = max(score, alpha)

                if score >= beta:
                    break
            return alpha
     */

    private int AlphaBeta(Board board, int alpha, int beta, int depthleft, Move m)
    {
        if (depthleft == 0)
        {
            return Quiesce(board, m, alpha, beta);
            return -Eval(board, m);
        }
        depthleft--;
        int score;
        board.MakeMove(m);

        foreach (Move mv in board.GetLegalMoves())
        {
            score = -AlphaBeta(board, int.MinValue, -alpha, depthleft, mv);
            board.UndoMove(mv);
            if (score > alpha)
            {
                alpha = score;
            }
            if (score >= beta)
            {
                return alpha;
            }
        }

        return alpha;
    }

    private int GetColorValue(Board board)
    {
        return board.GetLegalMoves().Length + board.GetLegalMoves(true).Length
        + board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).Count * pieceValues[0]
        + board.GetPieceList(PieceType.Rook, board.IsWhiteToMove).Count * pieceValues[1]
        + board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove).Count * pieceValues[2]
        + board.GetPieceList(PieceType.Knight, board.IsWhiteToMove).Count * pieceValues[3]
        + board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).Count * pieceValues[4];
    }
}