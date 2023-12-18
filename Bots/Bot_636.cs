namespace auto_Bot_636;
using ChessChallenge.API;
using System;

public class Bot_636 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int[] middlegameValues = { 0, 78, 319, 327, 496, 984, 100000 };
    int[] endgameValues = { 0, 101, 330, 334, 499, 999, 100000 };
    int[] piecePhase = { 0, 0, 1, 1, 2, 4, 0 };
    int pieceCount = 32;

    const int minusInfinity = -1000000;

    Board board;
    int myColor;
    bool firstTime = true;
    Move bestMove;
    int maxDepth = 5;
    Random random = new Random();

    private int moveScore(Move a)
    {
        int kingMove = a.IsCastles ? 1 : (a.MovePieceType == PieceType.King ? -1 : 0);
        return kingMove + Convert.ToInt32(a.IsCapture);
    }

    private int CompareTo(Move a, Move b)
    {
        return moveScore(b) - moveScore(a);
    }

    private Move[] OrderMoves()
    {
        Move[] moves = board.GetLegalMoves();
        Array.Sort(moves, (a, b) => CompareTo(a, b));
        if (random.NextDouble() >= 0.9 && moves.Length > 1)
        {
            (moves[0], moves[1]) = (moves[1], moves[0]);
        }
        return moves;
    }

    private Move[] OrderCaptures()
    {
        Move[] moves = board.GetLegalMoves(true);
        Array.Sort(moves, (a, b) => (pieceValues[(int)b.CapturePieceType] - pieceValues[(int)b.MovePieceType]).CompareTo(pieceValues[(int)a.CapturePieceType] - pieceValues[(int)a.MovePieceType]));
        return moves;
    }

    private int EndgameEval()
    {
        PieceList[] allPieces = board.GetAllPieceLists();
        int score = 0;
        foreach (PieceList pieceList in allPieces)
        {
            PieceType pieceType = pieceList.TypeOfPieceInList;
            int sign = pieceList.IsWhitePieceList ? 1 : -1;
            score += pieceList.Count * endgameValues[(int)pieceType] * sign;
        }
        score += board.GetKingSquare(true).Rank * 10;
        score -= (7 - board.GetKingSquare(false).Rank) * 10;
        return score;
    }

    private int MiddlegameEval()
    {
        PieceList[] allPieces = board.GetAllPieceLists();
        int score = 0;
        foreach (PieceList pieceList in allPieces)
        {
            PieceType pieceType = pieceList.TypeOfPieceInList;
            int sign = pieceList.IsWhitePieceList ? 1 : -1;
            score += pieceList.Count * middlegameValues[(int)pieceType] * sign;
        }
        score -= board.GetKingSquare(true).Rank * 50;
        score += (7 - board.GetKingSquare(false).Rank) * 50;

        return score;
    }

    private int Evaluate(int turn)
    {
        int totalPhase = 24; // Esto se puede achicar a un numero
        float phase = totalPhase;

        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList pieceList in allPieces)
        {
            PieceType pieceType = pieceList.TypeOfPieceInList;
            int sign = pieceList.IsWhitePieceList ? 1 : -1;
            phase -= pieceList.Count * piecePhase[(int)pieceType];
        }
        phase = (phase * 256 + (totalPhase / 2)) / totalPhase;
        float eval = ((MiddlegameEval() * (256 - phase)) + (EndgameEval() * phase)) / 256;
        return (int)eval * turn;
    }

    private int Quiesce(int turn, int alpha, int beta)
    {
        int staticEval = Evaluate(turn);
        if (staticEval >= beta)
        {
            return beta;
        }
        alpha = Math.Max(alpha, staticEval);
        Move[] captures = OrderCaptures();
        foreach (Move capture in captures)
        {
            board.MakeMove(capture);
            int score = -Quiesce(turn * -1, -beta, -alpha);
            board.UndoMove(capture);
            if (score >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, score);
        }
        return alpha;
    }

    private int Search(int depth, int turn, bool start = true, int alpha = -100000000, int beta = 100000000)
    {
        if (board.IsDraw())
        {
            return depth; // In case of a draw, go for a longer one (to avoid silly repetitions in the opening).
        }
        Move[] moves = OrderMoves();
        if (moves.Length == 0)
        {
            if (board.IsInCheckmate())
            {
                return minusInfinity - depth; // If there are many mates, go for the fastest.
            }
            else
            {
                return 0;
            }
        }

        if (depth == 0)
        {
            return Quiesce(turn, alpha, beta);
        }

        int score = alpha - 1;
        foreach (Move move in moves)
        {

            board.MakeMove(move);
            score = Math.Max(score, -Search(depth - 1, turn * -1, false, -beta, -alpha));
            board.UndoMove(move);
            //alpha = Math.Max(alpha, score);
            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
                if (start)
                {
                    bestMove = move;
                }
            }
        }

        return alpha;
    }

    public Move Think(Board board, Timer timer)
    {

        if (firstTime)
        {
            // Initialize stuff for the first time
            firstTime = false;
            pieceCount = 32;
        }
        if (timer.MillisecondsRemaining > 20000 && pieceCount < 8)
        {
            maxDepth = 7;
        }
        else if (timer.MillisecondsRemaining > 20000 && pieceCount < 12)
        {
            maxDepth = 6;
        }
        else if (timer.MillisecondsRemaining < 1200)
        {
            maxDepth = 0;
        }
        else if (timer.MillisecondsRemaining < 2000)
        {
            maxDepth = 2;
        }
        else if (timer.MillisecondsRemaining < 5000)
        {
            maxDepth = 3;
        }
        else if (timer.MillisecondsRemaining < 10000)
        {
            maxDepth = 4;
        }
        else
        {
            maxDepth = 5;
        }
        myColor = board.IsWhiteToMove ? 1 : -1;
        this.board = board;
        if (board.PlyCount == 0)
        {
            bestMove = new Move("d2d4", board);
        }
        else if (board.PlyCount == 1)
        {
            bestMove = new Move("d7d5", board);
        }
        else
        {
            Search(maxDepth, myColor);
        }
        if (bestMove.IsCapture)
        {
            pieceCount--;
        }
        return bestMove;
    }

}