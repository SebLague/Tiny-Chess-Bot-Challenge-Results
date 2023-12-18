namespace auto_Bot_313;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_313 : IChessBot
{
    public int[] king_eval = new int[64]
    {
         20, 30, 10,  0,  0, 10, 30, 20,
         20, 20,  0,  0,  0,  0, 20, 20,
        -10,-20,-20,-20,-20,-20,-20,-10,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
    };

    public int[] bishop_eval = new int[64]
    {
        -20,-10,-10,-10,-10,-10,-10,-20,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -20,-10,-10,-10,-10,-10,-10,-20,
    };

    public int[] king_eval_endgame = new int[64]
    {
        -50,-30,-30,-30,-30,-30,-30,-50,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -50,-40,-30,-20,-20,-30,-40,-50,
    };


    public Move Think(Board board, Timer timer)
    {
        Move fmove = Get_Best(board);
        return fmove;
    }

    public Move Get_Best(Board board)
    {
        double score = double.NegativeInfinity;
        Move fmove = board.GetLegalMoves()[0];
        foreach (Move move in Sort(board).ToList())
        {
            board.MakeMove(move);
            double eval = -Search(board, 4, double.NegativeInfinity, double.PositiveInfinity);
            if ((bool)board.IsInCheckmate() == true)
            {
                eval = double.PositiveInfinity;
            }
            if (eval > score)
            {
                fmove = move;
                score = eval;
            }
            board.UndoMove(move);
        }
        return fmove;
    }

    public double Search(Board board, int dim, double alpha, double beta)
    {
        if (dim == 0)
        {
            return Eval(board);
        }

        foreach (Move move in Sort(board))
        {
            board.MakeMove(move);
            double nscore = -Search(board, dim - 1, -beta, -alpha);
            board.UndoMove(move);
            if (nscore >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, nscore);
        }
        return alpha;
    }


    public double Eval(Board board)
    {
        if (board.IsWhiteToMove == true)
        {
            return GetValue(board, true) - GetValue(board, false);
        }
        else
        {
            return GetValue(board, false) - GetValue(board, true);
        }
    }

    public double GetValue(Board board, bool turn)
    {
        int Pawns = board.GetPieceList(PieceType.Pawn, turn).Count;
        int Bishops = board.GetPieceList(PieceType.Bishop, turn).Count;
        int Knights = board.GetPieceList(PieceType.Knight, turn).Count;
        int Rooks = board.GetPieceList(PieceType.Rook, turn).Count;
        int Queen = board.GetPieceList(PieceType.Queen, turn).Count;
        int King = board.GetPieceList(PieceType.King, turn).Count;
        int KingSafety = pos_eval(board, turn);
        return 100 * Pawns + 320 * Bishops + 330 * Knights + 500 * Rooks + 900 * Queen + 20000 * King + KingSafety;

    }

    public List<Move> Sort(Board board)
    {
        List<Move> legals = board.GetLegalMoves().ToList();
        List<Move> nlist = new List<Move>();
        foreach (Move move in legals.ToList())
        {
            if (move.IsCapture || move.IsPromotion || move.IsCastles)
            {
                nlist.Add(move);
                legals.Remove(move);
            }
        }
        foreach (Move omove in legals.ToList())
        {
            nlist.Add(omove);
            legals.Remove(omove);
        }
        return nlist;
    }

    public int pos_eval(Board board, bool turn)
    {
        Square KSquare = board.GetKingSquare(turn);
        PieceList Bishops = board.GetPieceList(PieceType.Bishop, turn);
        int b_eval = 0;

        foreach (Piece bishop in Bishops)
        {
            Square BSquare = bishop.Square;
            b_eval += bishop_eval[turn ? BSquare.Index : BSquare.Index - BSquare.Rank * 8];
        }

        int ksq = turn ? KSquare.Index : KSquare.Index - KSquare.Rank * 8;
        int king_ev = board.GetPieceList(PieceType.Queen, true).Count != 0 && board.GetPieceList(PieceType.Queen, false).Count != 0 ? king_eval[ksq] : king_eval_endgame[ksq];
        return 3 * (king_ev + b_eval);
    }
}