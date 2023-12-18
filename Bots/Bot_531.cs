namespace auto_Bot_531;
using ChessChallenge.API;
using System;

public class Bot_531 : IChessBot
{
    // Centi pawn values for: null, Pawn, Knight, Bishop, Rook, Queen, King
    int[] _centiPawnValues = { 0, 100, 300, 320, 500, 900, 0 }, _phasePieceValues = { 0, 0, 1, 1, 2, 4, 0 };
    Move _bestMove = Move.NullMove;

    private ulong[] _pieceSquareTables =
    {
        9913330531774723959, 8609676836631704936, 11078252110869744008, 8608480570021773311,
        250098419548360960, 1715269411402468225, 1710465645101833345, 4803766359914288,
        6374695211575366995, 6312245082029922709, 6307740382873098373, 3843071673468680053,
        7455559058829379447, 7455559058560874358, 7455559058560874358, 8608480568035350936,
        6302638648329659731, 7460362828890278021, 6307441315961931894, 3843370740631435125,
        13508397255544502747, 3535326889997710133, 1152921509170249729, 1152921509170249729,
        1258605535789388048, 1576256919301905233, 1566649386700635473, 5401900951762225
    };


    const int entries = 1 << 20;
    Transposition[] _tt = new Transposition[entries];

    struct Transposition
    {
        public ulong key;
        public Move move;
        public int depth, eval, bound;

        public Transposition(ulong _key, Move _move, int _depth, int _eval, int _bound)
        {
            key = _key;
            move = _move;
            depth = _depth;
            eval = _eval;
            bound = _bound;
        }
    }

    bool hasPassedTimeThreshold(Timer timer)
    {
        return timer.MillisecondsElapsedThisTurn >= Math.Min(timer.MillisecondsRemaining / 30, 8000);
    }

    // 0 = pawn; 1 = knight; 2 = bishop; 3 = rook; 4 = queen; 5 = king mid; 6 = king end
    int GetPieceSquareValue(int pieceTable, int square)
    {
        return ((int)((_pieceSquareTables[pieceTable * 4 + square / 16] >> square % 16 * 4) & 15) - 7) * 5;
    }

    public Move Think(Board board, Timer timer)
    {
        Move best = Move.NullMove;
        for (int depth = 1; depth < 50; depth++)
        {
            int currentEval = SearchPosition(board, depth, 0, -50000, 50000, timer);
            if (hasPassedTimeThreshold(timer))
                break;
            best = _bestMove;
            if (Math.Abs(currentEval) >= 50000 - 50)
                break;
        }
        return best;
    }

    int SearchPosition(Board board, int depth, int plyFromRoot, int alpha, int beta, Timer timer)
    {
        ulong key = board.ZobristKey;
        bool notRoot = plyFromRoot > 0;
        bool qsearch = depth <= 0;

        if (notRoot && board.IsRepeatedPosition())
            return 0;

        Transposition entry = _tt[key % entries];
        if (notRoot && entry.key == key && entry.depth >= depth && (entry.bound == 3 || (entry.bound == 2 && entry.eval >= beta) || (entry.bound == 1 && entry.eval <= alpha)))
            return entry.eval;
        Move[] legalMoves = board.GetLegalMoves(qsearch);
        if (!qsearch && legalMoves.Length == 0)
            return board.IsInCheck() ? -50000 + plyFromRoot : 0;

        if (qsearch)
        {
            int eval = EvaluatePosition(board);
            if (eval >= beta)
                return eval;
            alpha = Math.Max(alpha, eval);
        }

        int[] scores = new int[legalMoves.Length];
        Move bestMove = Move.NullMove;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            if (legalMoves[i] == entry.move) scores[i] = 1000000;
            else if (legalMoves[i].IsCapture) scores[i] = 100 * (int)board.GetPiece(legalMoves[i].TargetSquare).PieceType - (int)board.GetPiece(legalMoves[i].StartSquare).PieceType;
        }


        int origAlpha = alpha;
        for (int i = 0; i < legalMoves.Length; i++)
        {
            if (hasPassedTimeThreshold(timer)) return 50000;

            for (int j = i + 1; j < legalMoves.Length; j++)
            {
                if (scores[j] > scores[i])
                    (scores[i], scores[j], legalMoves[i], legalMoves[j]) = (scores[j], scores[i], legalMoves[j], legalMoves[i]);
            }

            Move move = legalMoves[i];

            board.MakeMove(move);
            int score = -SearchPosition(board, depth - 1, plyFromRoot + 1, -beta, -alpha, timer);
            board.UndoMove(move);
            if (score >= beta)
                return beta;
            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
                if (!notRoot)
                    _bestMove = move;
            }
        }

        _tt[key % entries] = new Transposition(key, bestMove, depth, alpha, alpha >= beta ? 2 : alpha > origAlpha ? 3 : 1);

        return alpha;
    }

    int EvaluatePosition(Board board)
    {
        int phase = 0, midEval = 0, endEval = 0;
        foreach (bool white in new[] { true, false })
        {
            for (int piece = 1; piece <= 6; piece++)
            {
                Square square;
                ulong bitBoard = board.GetPieceBitboard((PieceType)piece, white);
                while (bitBoard != 0)
                {
                    square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref bitBoard));
                    if (!white)
                    {
                        square = new Square((7 - square.Rank) * 8 + square.File);
                    }
                    phase += _phasePieceValues[piece];
                    var pieceSquareValue = GetPieceSquareValue(piece - 1, square.Index);
                    midEval += pieceSquareValue + _centiPawnValues[piece];
                    endEval += (piece == 6 ? GetPieceSquareValue(6, square.Index) : pieceSquareValue) +
                               _centiPawnValues[piece];
                }
            }
            midEval = -midEval;
            endEval = -endEval;
        }

        return (board.IsWhiteToMove ? 1 : -1) * (midEval * phase + endEval * (24 - phase)) / 24;
    }
}