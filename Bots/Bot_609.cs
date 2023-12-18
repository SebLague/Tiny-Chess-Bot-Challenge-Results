namespace auto_Bot_609;
using ChessChallenge.API;
using System.Collections.Generic;

public class Bot_609 : IChessBot
{
    const int inf = (int)1e9;
    const int checkmateVal = (int)1e8;

    Board board;
    Move bestMove = Move.NullMove;
    int bestEval = -inf;

    bool searching;

    Dictionary<PieceType, int> pieceValue = new Dictionary<PieceType, int>();

    public Bot_609()
    {
        pieceValue[PieceType.Pawn] = 100;
        pieceValue[PieceType.Knight] = 300;
        pieceValue[PieceType.Bishop] = 320;
        pieceValue[PieceType.Rook] = 500;
        pieceValue[PieceType.Queen] = 900;
        pieceValue[PieceType.King] = 9000;
    }

    public Move Think(Board _board, Timer timer)
    {
        board = _board;
        // searching = true;

        // int timeForMove = timer.MillisecondsRemaining/15;

        // void StopSearch(Object stateInfo) { searching = false; };
        // System.Threading.Timer invoker = new System.Threading.Timer(StopSearch, null, timeForMove, timeForMove);

        // int depth = 1, bestEvalID = -inf;
        // Move bestMoveID = Move.NullMove;
        // while (true)
        // {
        // Search(depth);
        // if (!searching) break;

        // if (bestEval > bestEvalID)
        // {
        // bestEvalID = bestEval;
        // bestMoveID = bestMove;
        // }

        // depth++;
        // }

        // bestEval = -inf;
        // bestMove = Move.NullMove;
        // return bestMoveID;

        searching = true;
        Search(3);
        return bestMove;
    }

    private int Evaluate()
    {
        int eval = 0;
        #region Material Evaluation

        eval += board.GetPieceList(PieceType.Pawn, true).Count * pieceValue[PieceType.Pawn];
        eval += board.GetPieceList(PieceType.Knight, true).Count * pieceValue[PieceType.Knight];
        eval += board.GetPieceList(PieceType.Bishop, true).Count * pieceValue[PieceType.Bishop];
        eval += board.GetPieceList(PieceType.Rook, true).Count * pieceValue[PieceType.Rook];
        eval += board.GetPieceList(PieceType.Queen, true).Count * pieceValue[PieceType.Queen];

        eval -= board.GetPieceList(PieceType.Pawn, false).Count * pieceValue[PieceType.Pawn];
        eval -= board.GetPieceList(PieceType.Knight, false).Count * pieceValue[PieceType.Knight];
        eval -= board.GetPieceList(PieceType.Bishop, false).Count * pieceValue[PieceType.Bishop];
        eval -= board.GetPieceList(PieceType.Rook, false).Count * pieceValue[PieceType.Rook];
        eval -= board.GetPieceList(PieceType.Queen, false).Count * pieceValue[PieceType.Queen];

        if (!board.IsWhiteToMove) eval *= -1;

        #endregion

        #region Moves Evaluation

        int moves = board.GetLegalMoves().Length;
        eval += (moves / (20 + moves)) * 100;

        #endregion

        return eval;
    }

    private int Extensions(Move playedMove, int numExtensions, int maxExtensions = 3)
    {
        int extension = 0;
        if (board.IsInCheck()) extension = 1;
        if (playedMove.MovePieceType == PieceType.Pawn && (playedMove.TargetSquare.Rank == 7 || playedMove.TargetSquare.Rank == 2)) extension = 1;

        if (numExtensions < maxExtensions) return extension;
        else return 0;
    }

    private int QuiescenceSearch(int alpha, int beta)
    {
        int eval = Evaluate();
        if (eval >= beta) return beta;
        if (alpha < eval) alpha = eval;

        // Move[] captureMoves = board.GetLegalMoves(true);
        Move[] captureMoves = OrderedLegalMoves(true);

        foreach (Move move in captureMoves)
        {
            board.MakeMove(move);
            int score = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove(move);

            if (score >= beta) return beta;
            if (score > alpha) alpha = score;
        }

        return alpha;
    }

    private int Search(int depth, int ply = 0, int alpha = -inf, int beta = inf, int extensions = 0)
    {
        if (depth == 0)
        {
            return QuiescenceSearch(alpha, beta);
        }

        // Move[] moves = board.GetLegalMoves();
        Move[] moves = OrderedLegalMoves();

        if (board.IsInCheckmate()) return -checkmateVal;
        if (board.IsDraw()) return 0;

        int bestEvalThisPly = -inf;
        Move bestMoveThisPly = Move.NullMove;

        foreach (Move move in moves)
        {
            if (!searching) return 0;

            int eval = 0;

            board.MakeMove(move);
            int extension = 0;
            extension = Extensions(move, extensions);
            eval = -Search(depth - 1 + extension, ply + 1, -beta, -alpha, extensions + extension);
            board.UndoMove(move);

            if (eval > bestEvalThisPly)
            {
                bestEvalThisPly = eval;
                bestMoveThisPly = move;
            }

            if (bestEvalThisPly > alpha) alpha = bestEvalThisPly;
            if (alpha >= beta) break;
        }

        if (ply == 0)
        {
            bestEval = bestEvalThisPly;
            bestMove = bestMoveThisPly;
        }

        return bestEvalThisPly;
    }

    private Move[] OrderedLegalMoves(bool capturesOnly = false)
    {
        Move[] legalMoves = board.GetLegalMoves(capturesOnly);

        int[] scores = new int[legalMoves.Length];

        for (int i = 0; i < legalMoves.Length; i++)
        {
            Move move = legalMoves[i];
            PieceType movePiece = board.GetPiece(move.StartSquare).PieceType;
            PieceType capturePiece = move.CapturePieceType;

            if (capturePiece != PieceType.None) scores[i] = 10 * (pieceValue[capturePiece] - pieceValue[movePiece]);

            if (move.IsPromotion) scores[i] += pieceValue[move.PromotionPieceType];
        }

        #region Sorting (Bubble Sort)

        for (int j = legalMoves.Length - 1; j > 0; j--)
        {
            for (int i = 0; i < j; i++)
            {
                if (scores[i] > scores[i + 1])
                {
                    (scores[i], scores[i + 1]) = (scores[i + 1], scores[i]);
                    (legalMoves[i], legalMoves[i + 1]) = (legalMoves[i + 1], legalMoves[i]);
                }
            }
        }

        #endregion

        return legalMoves;
    }
}