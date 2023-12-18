namespace auto_Bot_149;
using ChessChallenge.API;
using System;
using System.Linq;

// Hammerhead_V5, by mphart
// The chosen one. He was brought to life and trained in the depths of the underworld,
// known to us as the deep sea, trained by underwater dragons and powerful beings from the
// bottom of the world, all for the purpose of rising up to the challenge, the ultimate glory
// of being the best, the most intelligent chess bot to ever live, the one to rule them all.
public class Bot_149 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        // pick a random initial move
        Random random = new Random();
        Move[] allMoves = board.GetLegalMoves().OrderBy(x => random.Next()).ToArray();
        Move moveToPlay = allMoves[0];
        // hurry up if less than 5 sec remaining
        // if (timer.MillisecondsRemaining < 5000) depth = 2;
        int depth = 2;
        int maxVal = -2000000000;
        int minVal = 2000000000;
        foreach (Move m in allMoves)
        {
            board.MakeMove(m);
            if (board.IsWhiteToMove)
            {
                int eval = MiniMax(board, depth, int.MinValue, int.MaxValue, true);
                if (eval < minVal)
                {
                    minVal = eval;
                    moveToPlay = m;
                }
            }
            else
            {
                int eval = MiniMax(board, depth, int.MinValue, int.MaxValue, false);
                if (eval > maxVal)
                {
                    maxVal = eval;
                    moveToPlay = m;
                }
            }
            board.UndoMove(m);
        }
        return moveToPlay;
    }

    public int MiniMax(Board b, int depth, int alpha, int beta, bool maximizer)
    {
        //end of search
        if (depth == 0 || b.IsInCheckmate() || b.IsDraw()) return evaluate(b);
        Move[] moves = b.GetLegalMoves();
        //maximizer
        if (maximizer)
        {
            int maxEval = int.MinValue;
            foreach (Move m in moves)
            {
                b.MakeMove(m);
                int val = MiniMax(b, depth - 1, alpha, beta, false);
                if (val > maxEval) maxEval = val;
                if (val > alpha) alpha = val;
                b.UndoMove(m);
                if (beta < alpha) break;
            }
            return maxEval;
        }
        //minimizer
        else
        {
            int minEval = int.MaxValue;
            foreach (Move m in moves)
            {
                b.MakeMove(m);
                int val = MiniMax(b, depth - 1, alpha, beta, true);
                if (val < minEval) minEval = val;
                if (val < beta) beta = val;
                b.UndoMove(m);
                if (beta < alpha) break;
            }
            return minEval;
        }
    }

    public int evaluate(Board b)
    {
        if (b.IsDraw()) return 0;
        // add up all piece values
        int wpts = 0;
        int bpts = 0;
        for (int index = 0; index <= 63; index++)
        {
            Piece p = b.GetPiece(new Square(index));
            if (p.IsWhite) wpts += pieceValues[(int)p.PieceType];
            else bpts += pieceValues[(int)p.PieceType];
        }
        int pts = wpts - bpts;
        // todo make more elaborate and efficient
        // if just opponent king left, move opp king to edge, kings closer together
        if (bpts <= 12000 || wpts <= 12000)
        {
            int i = 1;
            bool t = true;
            if (wpts == 10000)
            {
                i = -1;
                t = false;
            }
            Square oppK = b.GetKingSquare(!t);
            Square friK = b.GetKingSquare(t);
            int distEdgeFile = Math.Min(oppK.File, 7 - oppK.File);
            int distEdgeRank = Math.Min(oppK.Rank, 7 - oppK.Rank);
            pts += i * 50 * (3 - distEdgeFile) * (3 - distEdgeFile) + 50 * (3 - distEdgeRank) * (3 - distEdgeRank);
            pts += i * 40 * Math.Abs(7 - oppK.Rank - friK.Rank) * Math.Abs(7 - oppK.File - friK.File);
        }
        // check for checks, wins, draws
        if (b.IsWhiteToMove)
        {
            if (b.IsInCheckmate()) return -2000000000;
            if (b.IsInCheck()) pts -= 50;
        }
        else
        {
            if (b.IsInCheckmate()) return 2000000000;
            if (b.IsInCheck()) pts += 50;
        }
        return pts;
    }
}