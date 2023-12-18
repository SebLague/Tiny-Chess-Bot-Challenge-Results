namespace auto_Bot_236;
using ChessChallenge.API;

public class Bot_236 : IChessBot
{
    Board m_board;
    System.Random m_rng = new();

    //sicilian
    int[] m_black_debut = {0,-1,0,0,3517,0,0,2998,3,1869,2998,3,2117,3322,0,774,2942,6,0,
    2745,6,1739,1762,30,2117,3322,0,0,2745,6,1749,2942,0,0,-1,0,1350,
    2803,21,1153,2803,9,1609,1634,0,1739,1762,0,0,2803,27, 0, 2226, 36};

    int[] m_white_debut = { 0, -1, 0, 0, 1153, 0, 2942, 1739, 0, 0, 1153, 0, 2421, 2396, 0, 2291, 2268, 6, 0, 1153, 0, 0, 1804, 12 };

    int m_i_white_search_idx = 21;
    int m_i_black_search_idx = 51;
    int m_bIsWhite;
    int m_iThreatLevel = -3;
    bool m_bIsDebut;
    //int m_iDebutMoveID = 0;
    Move m_bestmove;


    public Move Think(Board board, Timer timer)
    {

        m_board = board;
        m_bIsWhite = m_board.IsWhiteToMove ? 1 : -1;

        if (m_board.PlyCount <= 1)
        {
            //string start_default_pos_fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
            int hash = 0;
            unchecked
            {
                for (int i = 0; i < board.GameStartFenString.Length; i++)
                {
                    hash = (int)board.GameStartFenString[i] + hash * 31;
                }
            }

            m_bIsDebut = hash == 1619322768 ? true : false;
        }


        //DivertedConsole.Write(m_bIsDebut ? "in debut\n" : "not debut\n");

        //m_bIsDebut = m_board.PlyCount < 8 ? true : false; 


        if (m_bIsDebut)
        {
            DebutMove();
            if (m_bIsDebut)
            {
                return m_bestmove;
            }
        }

        Move[] moves = m_board.GetLegalMoves();

        m_bestmove = moves[m_rng.Next(moves.Length)];
        int best_move_score = -65535;
        foreach (Move move in moves)
        {
            m_board.MakeMove(move);
            int score = alphaBetaMin(-65535, +65535, 3);
            m_board.UndoMove(move);
            if (score > best_move_score)
            {
                best_move_score = score;
                m_bestmove = move;
            }
        }

        return m_bestmove;
    }

    Move NumberToMove(int num)
    {
        int column_1 = num & 7;
        int row_1 = (num >> 3) & 7;
        int column_2 = (num >> 6) & 7;
        int row_2 = (num >> 9);

        char[] str_b = { 'a', '1', 'a', '1' };
        str_b[0] += (char)column_1;
        str_b[1] += (char)row_1;
        str_b[2] += (char)column_2;
        str_b[3] += (char)row_2;

        return new Move(new string(str_b), m_board);
    }

    int MoveToNumber(Move mv)
    {
        return mv.StartSquare.File + (mv.StartSquare.Rank << 3) +
        (mv.TargetSquare.File << 6) + (mv.TargetSquare.Rank << 9);
    }

    void DebutMove()
    {
        if (m_bIsWhite == 1)
        {
            // white debut
            DebutMove(ref m_white_debut, ref m_i_white_search_idx);
            return;
        }
        else
        {

            DebutMove(ref m_black_debut, ref m_i_black_search_idx);
            return;
        }
    }


    void DebutMove(ref int[] moves, ref int search_idx)
    {



        //static int search_idx = 36;
        int mv_i = 0;

        while (mv_i != -1)
        {
            int mv_w_i = 7777;
            if (m_board.GameMoveHistory.Length >= 1)
            {
                Move mv_w = m_board.GameMoveHistory[m_board.GameMoveHistory.Length - 1];
                mv_w_i = MoveToNumber(mv_w);
            }


            int mv_f = -1;
            while (mv_f != 0)
            {
                mv_f = moves[search_idx];
                if (mv_f == mv_w_i || mv_f == 0)
                {
                    mv_i = moves[search_idx + 1];
                    search_idx = moves[search_idx + 2];
                    break;
                }
                search_idx += 3;
            }
            //DivertedConsole.Write("white {0:D}, black {1:D}\n", mv_w_i, mv_i);

            if (mv_i == -1) break;

            if (mv_f == 0)
            {
                m_board.MakeMove(NumberToMove(mv_i));
                // is threat move?
                int score = alphaBetaMin(-65535, +65535, 3);
                //DivertedConsole.Write("threat score {0:D}\n", score);
                if (score < m_iThreatLevel)
                {
                    m_bIsDebut = false;
                    m_board.UndoMove(NumberToMove(mv_i));
                    return;
                }
                m_board.UndoMove(NumberToMove(mv_i));
            }

            m_bestmove = NumberToMove(mv_i);
            return;
        }

        // stop debut start evaluating
        m_bIsDebut = false;
        return;
    }

    int alphaBetaMax(int alpha, int beta, int depthleft)
    {
        if (depthleft == 0) return evaluate();
        Move[] moves = m_board.GetLegalMoves();
        foreach (Move move in moves)
        {
            m_board.MakeMove(move);
            int score = alphaBetaMin(alpha, beta, depthleft - 1);
            m_board.UndoMove(move);
            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;

        }
        return alpha;
    }

    int alphaBetaMin(int alpha, int beta, int depthleft)
    {
        if (depthleft == 0) return -evaluate();
        Move[] moves = m_board.GetLegalMoves();
        foreach (Move move in moves)
        {
            m_board.MakeMove(move);
            int score = alphaBetaMax(alpha, beta, depthleft - 1);
            m_board.UndoMove(move);
            if (score <= alpha)
                return alpha;
            if (score < beta)
                beta = score;

        }
        return beta;
    }

    int evaluate()
    {
        int score = 0;

        int Q_W = m_board.GetPieceList((PieceType)5, true).Count;
        int Q_B = m_board.GetPieceList((PieceType)5, false).Count;
        int R_W = m_board.GetPieceList((PieceType)4, true).Count;
        int R_B = m_board.GetPieceList((PieceType)4, false).Count;
        int B_W = m_board.GetPieceList((PieceType)3, true).Count;
        int B_B = m_board.GetPieceList((PieceType)3, false).Count;
        int N_W = m_board.GetPieceList((PieceType)2, true).Count;
        int N_B = m_board.GetPieceList((PieceType)2, false).Count;
        int P_W = m_board.GetPieceList((PieceType)1, true).Count;
        int P_B = m_board.GetPieceList((PieceType)1, false).Count;
        int K_W = m_board.GetPieceList((PieceType)6, true).Count;
        int K_B = m_board.GetPieceList((PieceType)6, false).Count;

        score = 200 * (K_W - K_B) +
                9 * (Q_W - Q_B) +
                5 * (R_W - R_B) +
                3 * (B_W - B_B + N_W - N_B) +
                1 * (P_W - P_B);

        return score * m_bIsWhite;
    }
}