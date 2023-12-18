namespace auto_Bot_433;
using ChessChallenge.API;
using System;
using System.Linq;

public class ScoredMove : IComparable
{
    public Move move;
    public int score;

    public int CompareTo(object obj)
    {
        if (obj == null) return 0;

        ScoredMove sm = obj as ScoredMove;

        if (sm.score == this.score)
            return 0;
        return (sm.score > this.score) ? -1 : 1;
    }

    public ScoredMove(Move move, int score = 0)
    {
        this.move = move;
        this.score = score;
    }

    public ScoredMove()
    {
        this.move = Move.NullMove;
        this.score = 0;
    }
}


public class Bot_433 : IChessBot
{

    int[,] square_weights = {
            {
                1000,         1000,         1000,         1000,         1000,         1000,         1000,         1000,
                1098,         1134,         1061,         1095,         1068,         1126,         1034,          989,
                 994,         1007,         1026,         1031,         1065,         1056,         1025,          980,
                 986,         1013,         1006,         1021,         1023,         1012,         1017,          977,
                 973,          998,          995,         1012,         1017,         1006,         1010,          975,
                 974,          996,          996,          990,         1003,         1003,         1033,          988,
                 965,          999,          980,          977,          985,         1024,         1038,          978,
                1000,         1000,         1000,         1000,         1000,         1000,         1000,         1000,

            },
            {
                 833,          911,          966,          951,         1061,          903,          985,          893,
                 927,          959,         1072,         1036,         1023,         1062,         1007,          983,
                 953,         1060,         1037,         1065,         1084,         1129,         1073,         1044,
                 991,         1017,         1019,         1053,         1037,         1069,         1018,         1022,
                 987,         1004,         1016,         1013,         1028,         1019,         1021,          992,
                 977,          991,         1012,         1010,         1019,         1017,         1025,          984,
                 971,          947,          988,          997,          999,         1018,          986,          981,
                 895,          979,          942,          967,          983,          972,          981,          977,

            },
            {
                 971,         1004,          918,          963,          975,          958,         1007,          992,
                 974,         1016,          982,          987,         1030,         1059,         1018,          953,
                 984,         1037,         1043,         1040,         1035,         1050,         1037,          998,
                 996,         1005,         1019,         1050,         1037,         1037,         1007,          998,
                 994,         1013,         1013,         1026,         1034,         1012,         1010,         1004,
                1000,         1015,         1015,         1015,         1014,         1027,         1018,         1010,
                1004,         1015,         1016,         1000,         1007,         1021,         1033,         1001,
                 967,          997,          986,          979,          987,          988,          961,          979,

            },
            {
                1032,         1042,         1032,         1051,         1063,         1009,         1031,         1043,
                1027,         1032,         1058,         1062,         1080,         1067,         1026,         1044,
                 995,         1019,         1026,         1036,         1017,         1045,         1061,         1016,
                 976,          989,         1007,         1026,         1024,         1035,          992,          980,
                 964,          974,          988,          999,         1009,          993,         1006,          977,
                 955,          975,          984,          983,         1003,         1000,          995,          967,
                 956,          984,          980,          991,          999,         1011,          994,          929,
                 981,          987,         1001,         1017,         1016,         1007,          963,          974,

            },
            {
                 972,         1000,         1029,         1012,         1059,         1044,         1043,         1045,
                 976,          961,          995,         1001,          984,         1057,         1028,         1054,
                 987,          983,         1007,         1008,         1029,         1056,         1047,         1057,
                 973,          973,          984,          984,          999,         1017,          998,         1001,
                 991,          974,          991,          990,          998,          996,         1003,          997,
                 986,         1002,          989,          998,          995,         1002,         1014,         1005,
                 965,          992,         1011,         1002,         1008,         1015,          997,         1001,
                 999,          982,          991,         1010,          985,          975,          969,          950,

            },
            {
                 935,         1023,         1016,          985,          944,          966,         1002,         1013,
                1029,          999,          980,          993,          992,          996,          962,          971,
                 991,         1024,         1002,          984,          980,         1006,         1022,          978,
                 983,          980,          988,          973,          970,          975,          986,          964,
                 951,          999,          973,          961,          954,          956,          967,          949,
                 986,          986,          978,          954,          956,          970,          985,          973,
                1001,         1007,          992,          936,          957,          984,         1009,         1008,
                 985,         1036,         1012,          946,         1008,          972,         1024,         1014,

            },
        };

    int[] piece_values = { 100, 300, 310, 500, 900, 1000000 };


    private int get_board_score(Board board)
    {
        int score = 0;
        PieceList[] pieces = board.GetAllPieceLists();

        for (int i = 0; i < 6; i++)
        {
            int v = this.piece_values[i];
            //white
            for (int j = 0; j < pieces[i].Count; j++)
                score += v + (this.square_weights[i, 63 - pieces[i].GetPiece(j).Square.Index] - 1000) / 5;
            //black
            for (int j = 0; j < pieces[i + 6].Count; j++)
                score -= v + (this.square_weights[i, pieces[i + 6].GetPiece(j).Square.Index] - 1000) / 5;
        }

        return score;
    }


    private int get_move_priority(Board board, Move move)
    {

        return
            (move.IsCapture ? (this.piece_values[(int)move.CapturePieceType - 1] * 2) : 0) +
            (move.MovePieceType == PieceType.Pawn ? 0 : 1);
    }

    private ScoredMove minmax(Board board, int depth, int deep_depth, int alpha = int.MinValue, int beta = int.MaxValue) // , IDictionary<string, float> scores
    {

        ScoredMove best_move = new();

        ScoredMove[] prioritizesd_moves = Array.Empty<ScoredMove>();

        foreach (Move move in board.GetLegalMoves())
            prioritizesd_moves = prioritizesd_moves.Append(new ScoredMove(move, get_move_priority(board, move))).ToArray();

        Array.Sort(prioritizesd_moves);
        Array.Reverse(prioritizesd_moves);

        for (int i = 0; i < (depth < 1 ? Math.Min(6, prioritizesd_moves.Length) : prioritizesd_moves.Length); i++)
        {
            ScoredMove pm = prioritizesd_moves[i];
            board.MakeMove(pm.move);
            int score;

            if (board.IsInCheckmate())
            {
                best_move.score = board.IsWhiteToMove ? -1000000 : 1000000;
                best_move.move = pm.move;
                board.UndoMove(pm.move);
                return best_move;
            }
            else if (board.IsDraw())
                score = 0;
            else if ((depth <= 0 && !(pm.move.IsCapture || board.IsInCheck() || pm.move.MovePieceType == PieceType.Pawn)) || (depth <= deep_depth))
                score = get_board_score(board);
            else
                score = minmax(board, depth - 1, deep_depth, alpha, beta).score;

            if (best_move.move == Move.NullMove || (board.IsWhiteToMove ? (score < best_move.score) : (score > best_move.score)))
            {
                best_move.score = score;
                best_move.move = pm.move;
            }
            board.UndoMove(pm.move);

            if (board.IsWhiteToMove)
                alpha = Math.Max(alpha, score);
            else
                beta = Math.Min(beta, score);
            if (beta <= alpha)
                break;
        }

        return best_move;
    }


    public Move Think(Board board, Timer timer)
    {
        ScoredMove a = minmax(board, Math.Min((int)Math.Log(timer.MillisecondsRemaining / 2000), 5), -4);
        return a.move;
    }
}