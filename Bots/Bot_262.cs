namespace auto_Bot_262;
using ChessChallenge.API;
using System;

public class Bot_262 : IChessBot
{
    int[] VALS = { 0, 1, 3, 4, 5, 9, 0 },
          // from http://www.talkchess.com/forum3/viewtopic.php?f=2&t=68311&start=19
          MG_VALS = { 82, 337, 365, 477, 1025, 0 },
          EG_VALS = { 94, 281, 297, 512, 936, 0 },
          move_scores = new int[218];

    //const int AI_DEPTH = 4,
    //MATE_VALUE = 11000,
    //SEARCH_MIN = -11005, // -MATE_VALUE - AI_DEPTH - 1,
    //SEARCH_MAX = 11005, // -SEARCH_MIN,
    //MAX_SEARCH_EXTENSIONS = 8,

    //MIN_EG = 518,
    //MAX_MG = 6192,
    //PHASE_RANGE = 5674, // MAX_MG - MIN_EG,
    //EG_MATERIAL_START = 1656, // MG_VALS[3] * 2 + MG_VALS[2] + MG_VALS[1];
    //WIN_CAPT_BIAS = 400000,
    //PROMOTE_BIAS = 300000,
    //LOSE_CAPT_BIAS = 100000;

    public Move Think(Board board, Timer timer)
    {
        double best_score = double.MinValue; // -infinity
        Move[] allMoves = board.GetLegalMoves();
        Move best_move = Move.NullMove;
        foreach (Move move in allMoves)
        {
            double score;
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }
            else if (board.IsDraw()) score = -25;
            else score = -Minimax(board, (int)Math.Log(timer.GameStartTimeMilliseconds / 18, 7.5) - 1, -11005, 11005); // board, AI_DEPTH - 1, SEARCH_MIN, SEARCH_MAX
            board.UndoMove(move);

            if (score > best_score)
            {
                best_score = score;
                best_move = move;
            }
        }

        return best_move;
    }

    double Minimax(Board board, int depth, double alpha, double beta, int num_extensions = 0)
    {
        if (depth == 0) return QSearch(board, alpha, beta);
        if (board.IsInCheckmate()) return -11000 - depth;
        if (board.IsDraw()) return 25;

        double best_score = double.MinValue; // -infinity
        Move[] allMoves = board.GetLegalMoves();
        OrderMoves(board, allMoves);
        foreach (Move move in allMoves)
        {
            double score;
            board.MakeMove(move);
            int extension = num_extensions < 8 && board.IsInCheck() ? 1 : 0;
            score = -Minimax(board, depth - 1 + extension, -beta, -alpha, num_extensions + extension);
            board.UndoMove(move);

            if (score >= beta) return score;
            if (score > best_score)
            {
                best_score = score;
                if (score > alpha) alpha = score;
            }
        }

        return alpha;
    }

    double QSearch(Board board, double alpha, double beta)
    {
        double best_score;
        if (board.IsInCheckmate()) best_score = -11000;
        else if (board.IsDraw()) best_score = 25;
        else best_score = Evaluate(board) * (board.IsWhiteToMove ? 1 : -1);
        if (best_score >= beta) return best_score;
        if (best_score > alpha) alpha = best_score;

        Move[] allMoves = board.GetLegalMoves(true);
        OrderMoves(board, allMoves);
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            double score = -QSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (score >= beta) return score;
            if (score > best_score)
            {
                best_score = score;
                if (score > alpha) alpha = score;
            }
        }

        return alpha;
    }

    void OrderMoves(Board board, Move[] moves)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int score = 0,
                move_type = (int)move.MovePieceType,
                piece_val = VALS[move_type];
            bool is_capt = move.IsCapture,
                 attacked = board.SquareIsAttackedByOpponent(move.TargetSquare);

            if (is_capt)
            {
                int trade = VALS[(int)move.CapturePieceType] - piece_val;
                score += (!attacked || trade >= 0 ? 400000 : 100000) + trade; // WIN_CAPT_BIAS = 400000, LOSE_CAPT_BIAS = 100000
            }

            if (move_type == 1)
            {
                if (!is_capt && (int)move.PromotionPieceType == 5) score += 300000; // PROMOTE_BIAS
            }
            else if (move_type != 6 && attacked) score -= piece_val / 4;
            move_scores[i] = -score; // to sort in descending order
        }

        Array.Sort(move_scores, moves, 0, moves.Length);
    }

    double Evaluate(Board board)
    {
        PieceList[] all_pieces = board.GetAllPieceLists();
        int w_mat = 0,
            b_mat = 0;
        double mg_val = 0,
               eg_val = 0,
               factor_mg = 0,
               w_eg_weight = 0,
               b_eg_weight = 0;
        for (int i = 0; i < 12; i++)
        {
            PieceList plist = all_pieces[i];
            int count = plist.Count;

            double val2 = 0;
            for (int j = 0; j < count; j++)
            {
                if (i % 6 == 0)
                {
                    factor_mg += MG_VALS[i % 6];
                    if (i < 6) w_eg_weight += MG_VALS[i % 6];
                    else b_eg_weight += MG_VALS[i % 6];
                }
                if (i < 6) w_mat += MG_VALS[i % 6];
                else b_mat += MG_VALS[i % 6];
                // originally intended to use an approximation of piece square tables using math
                Square square = plist.GetPiece(j).Square;
                int rank = square.Rank,
                    file = square.File;
                val2 += (double)(i < 6 ? rank : 7 - rank) / 150
                      + (double)(i < 4 ? file : 7 - file) / 150;
            }
            mg_val += MG_VALS[i % 6] * (i < 6 ? 1 : -1) * (count + val2);
            eg_val += EG_VALS[i % 6] * (i < 6 ? 1 : -1) * (count + val2);
        }
        w_eg_weight = 1 - Math.Min(1, (w_mat - w_eg_weight) / 1656); // EG_MATERIAL_START = 1656
        b_eg_weight = 1 - Math.Min(1, (b_mat - b_eg_weight) / 1656);
        factor_mg = (Math.Min(Math.Max(factor_mg, 518), 6192) - 518) / 5674; // MIN_EG = 518, MAX_MG = 6192, PHASE_RANGE = 5674

        Square king_w = board.GetPieceList(PieceType.King, true).GetPiece(0).Square,
               king_b = board.GetPieceList(PieceType.King, false).GetPiece(0).Square;
        return mg_val * factor_mg + eg_val * (1 - factor_mg) + MopUpEval(king_w, king_b, w_mat, b_mat, b_eg_weight) - MopUpEval(king_b, king_w, b_mat, w_mat, w_eg_weight);
    }

    double MopUpEval(Square k1, Square k2, int mat1, int mat2, double eg_weight)
    {
        double score = 0;
        if (mat1 > mat2 + 200 && eg_weight >= 0)
        {
            int k1x = k1.File,
                k1y = k1.Rank,
                k2x = k2.File,
                k2y = k2.Rank;
            score += (Math.Abs(k2x - 3.5) + Math.Abs(k2y - 3.5)) * 4.7 // encourage pushing opponent king farther from center
                   + (14 - (Math.Abs(k1x - k2x) + Math.Abs(k1y - k2y))) * 1.6; // encourage moving king closer to opponent king
        }
        return score * eg_weight;
    }
}