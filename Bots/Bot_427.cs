namespace auto_Bot_427;
// LeMaire

using ChessChallenge.API;
using static System.Math;

public class Bot_427 : IChessBot
{
    private Move bestMoveFound;
    private int oriDepth;
    private bool isWhite;
    private double[] piecesValues = { 0, 71, 293, 300, 456, 905, 100000 };
    private double coefinale;
    private long[] piecegrid = {
    9565974397469357, //pawn (mdr)
    1233245635783589, //knight
    3444467545664588, //bishop (ok/3)
    4533464446445644, //rook ?
    3446466647885688  //queen
  };

    public Move Think(Board board, Timer timer)
    {
        evaluate(board, true);
        isWhite = board.IsWhiteToMove;
        bestMoveFound = Move.NullMove;
        oriDepth = (int)(3 + Pow(4 * (1.01f - coefinale), (timer.MillisecondsRemaining / timer.GameStartTimeMilliseconds)));
        if (oriDepth <= 2) oriDepth = 2;
        minimax(board, oriDepth, -9999999, 9999999);
        return bestMoveFound;
    }

    double minimax(Board board, int depth, double alpha, double beta)
    {

        if (board.IsDraw()) return 0;
        if (depth == 0) return searchCaptures(board, alpha, beta, oriDepth - depth);

        Move[] moves = board.GetLegalMoves();
        moves = order(board, moves);
        if (moves.Length == 0)
        {
            if (board.IsInCheckmate()) return (-50000 + oriDepth - depth);
            else return 0;
        }

        Move bestMoveInPosition = Move.NullMove;

        int i = 0;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            double evaluation = -minimax(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta) return beta;

            if (evaluation > alpha)
            {
                alpha = evaluation;

                bestMoveInPosition = move;
                if (depth == oriDepth) bestMoveFound = move;
            }
            i++;
        }

        return alpha;
    }

    double searchCaptures(Board board, double alpha, double beta, int pfr)
    {
        double evaluation = evaluate(board);
        if (evaluation >= beta) return beta;
        if (evaluation > alpha) alpha = evaluation;
        if (pfr >= 8) return evaluation;

        Move[] moves = board.GetLegalMoves(true);
        moves = order(board, moves);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            evaluation = -searchCaptures(board, -beta, -alpha, pfr + 1);
            board.UndoMove(move);

            if (evaluation >= beta) return beta;
            alpha = Max(alpha, evaluation);
        }

        return alpha;
    }

    int casindex(int x, int y)
    {
        return (int)(18 - 4 * Abs(x - 3.5f) - Abs(y - 3.5f));
    }

    double evaluate(Board board, bool calcCoef = false)
    {
        double whiteMaterial = 0, blackMaterial = 0, eval = 0;
        for (int i = 0; i < 5; i++)
        {
            whiteMaterial += board.GetAllPieceLists()[i].Count * piecesValues[i + 1];
            blackMaterial += board.GetAllPieceLists()[i + 6].Count * piecesValues[i + 1];
        }
        // Now let's detemrmine the material & positional score of each player
        int whitePoscore = 0, blackPoscore = 0, j = 0;
        for (int i = 0; i < 5; i++)
        {
            j = board.GetAllPieceLists()[i].Count;
            whiteMaterial += j * piecesValues[i + 1];
            for (int k = 0; k < j; k++)
            {
                whitePoscore += piecegrid[i].ToString()[casindex(board.GetAllPieceLists()[i].GetPiece(k).Square.File, board.GetAllPieceLists()[i].GetPiece(k).Square.Rank)] - '0';
            }
            j = board.GetAllPieceLists()[i + 6].Count;
            blackMaterial += j * piecesValues[i + 1];
            for (int k = 0; k < j; k++)
            {
                blackPoscore += piecegrid[i].ToString()[casindex(board.GetAllPieceLists()[i + 6].GetPiece(k).Square.File, board.GetAllPieceLists()[i + 6].GetPiece(k).Square.Rank)] - '0';
            }
        }

        if (calcCoef) coefinale = (blackMaterial + whiteMaterial) / 14284; // 7142

        eval = ((whiteMaterial - blackMaterial) / (whiteMaterial + blackMaterial) * 1000) + 3 * coefinale * (whitePoscore - blackPoscore);
        if (coefinale < 0.5) eval += getEndGameKingEval((board.IsWhiteToMove ? whiteMaterial : blackMaterial), (board.IsWhiteToMove ? blackMaterial : whiteMaterial), board.GetPiece(board.GetKingSquare(board.IsWhiteToMove)), board.GetPiece(board.GetKingSquare(!board.IsWhiteToMove))) / (coefinale + 0.5f);

        if (!board.IsWhiteToMove) eval *= -1;
        return eval;
    }

    double getEndGameKingEval(double mMaterial, double nMaterial, Piece mKing, Piece nKing)
    {
        if (mMaterial > nMaterial + 150) return (1 - coefinale) * ((4.701f * getDistanceFromCenter(nKing.Square.File, nKing.Square.Rank) + 1.599f * (14.02f - getMDistance(mKing, nKing))));
        return -getDistanceFromCenter(mKing.Square.File, mKing.Square.Rank) * (1 - coefinale);
    }

    double getDistanceFromCenter(int i, int j) => Abs(3 - (3.5f - Abs(i - 3.5f))) + Abs(3 - (3.5f - Abs(j - 3.5f)));

    double getMDistance(Piece wRoi, Piece bRoi) => Abs(bRoi.Square.File - wRoi.Square.File) + Abs(bRoi.Square.Rank - wRoi.Square.Rank);


    Move[] order(Board board, Move[] moves)
    {
        double[] guesses = new double[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {
            if (move.IsCapture) guesses[i] = piecesValues[(int)move.CapturePieceType] - piecesValues[(int)move.MovePieceType];
            i++;
        }

        return Sort(moves, guesses);
    }

    public Move[] Sort(Move[] moves, double[] scores)
    {
        for (int i = 0; i < moves.Length - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int s = j - 1;
                if (scores[s] < scores[j])
                {
                    (moves[j], moves[s]) = (moves[s], moves[j]);
                    (scores[j], scores[s]) = (scores[s], scores[j]);
                }
            }
        }
        return moves;
    }
}
