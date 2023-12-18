namespace auto_Bot_608;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_608 : IChessBot
{
    private Timer timer;
    private float millisecondsPerMove;
    private Dictionary<int, TranspositionEntry> transpositionTable = new();
    private ulong tableSize = 5000000;

    public static readonly TranspositionEntry NullEntry = new TranspositionEntry(0, Move.NullMove, 0, 0, 0);
    static int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public struct TranspositionEntry
    {
        public ulong zobrist;
        public Move bestMove;
        public int depth;
        public int score;
        public int moveNum;
        public TranspositionEntry(ulong zobrist, Move bestMove, int depth, int score, int moveNum)
        {
            this.zobrist = zobrist;
            this.bestMove = bestMove;
            this.depth = depth;
            this.score = score;
            this.moveNum = moveNum;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 2000)
        {
            millisecondsPerMove = timer.MillisecondsRemaining / 15;
        }
        else
        {
            millisecondsPerMove = timer.GameStartTimeMilliseconds / 200;
        }

        this.timer = timer;
        int depth = 1;
        int eval;
        Move bestMove = Move.NullMove;
        while (true)
        {
            eval = AlphaBeta(board, -1000000, 1000000, depth);
            if (timer.MillisecondsElapsedThisTurn > millisecondsPerMove)
            {
                break;
            }
            depth++;
            bestMove = GetTransposition(board.ZobristKey).bestMove;
        }

        return bestMove;
    }

    int AlphaBeta(Board board, int alpha, int beta, int depth)
    {
        if (timer.MillisecondsElapsedThisTurn > millisecondsPerMove)
        {
            return 0;
        }
        if (board.IsInCheckmate())
        {
            return (int)(200000 * ((depth % 2) - 0.5));
        }
        else if (board.IsInStalemate())
        {
            return (int)(-200000 * ((depth % 2) - 0.5));
        }
        else if (board.IsDraw())
        {
            return -1;
        }

        TranspositionEntry entry = GetTransposition(board.ZobristKey);
        if (entry.bestMove != Move.NullMove && entry.depth - (board.GameMoveHistory.Length - entry.moveNum) == depth)
        {
            return entry.score;
        }

        if (depth == 0)
        {
            return Quiesce(board, alpha, beta, depth, false);
        }

        Move[] moves = board.GetLegalMoves();
        moves = OrderedMoves(moves, board);
        Move bestMove = Move.NullMove;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int eval = -AlphaBeta(board, -beta, -alpha, depth - 1);
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > millisecondsPerMove)
            {
                return 0;
            }

            if (eval >= beta)
            {
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
                bestMove = move;
            }
        }
        transpositionTable[(int)(board.ZobristKey % tableSize)] = new TranspositionEntry(board.ZobristKey, bestMove, depth, alpha, board.GameMoveHistory.Length);

        return alpha;
    }

    int Quiesce(Board board, int alpha, int beta, int depth, bool isPromotion)
    {
        if (board.IsInCheckmate())
        {
            return (int)(200000 * ((depth % 2) - 0.5));
        }
        else if (board.IsInStalemate())
        {
            return (int)(-200000 * ((depth % 2) - 0.5));
        }
        else if (board.IsDraw())
        {
            return -1;
        }

        int staticEval = Evaluate(board);

        if (staticEval >= beta)
        {
            return beta;
        }

        int maxDelta = pieceValues[5] + (isPromotion ? pieceValues[5] - pieceValues[1] : 0);
        if (staticEval < alpha - maxDelta)
        {
            return alpha;
        }

        if (staticEval > alpha)
        {
            alpha = staticEval;
        }

        Move[] moves = board.GetLegalMoves();
        moves = ForcingMoves(moves, board);
        Move bestMove = Move.NullMove;

        foreach (Move move in moves)
        {
            if ((int)move.CapturePieceType + 200 < alpha)
            {
                return alpha;
            }

            board.MakeMove(move);
            int eval = -Quiesce(board, -beta, -alpha, depth - 1, move.IsPromotion);
            board.UndoMove(move);

            if (eval >= beta)
            {
                return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
                bestMove = move;
            }
        }
        return alpha;
    }



    Move[] OrderedMoves(Move[] moves, Board board)
    {
        Move ttBestMove = GetTransposition(board.ZobristKey).bestMove;
        IEnumerable<Move> orderedMoves = from move in moves
                                         orderby move == ttBestMove,
                                                 move.IsPromotion,
                                                 MoveIsCheck(move, board),
                                                 move.CapturePieceType - move.MovePieceType descending
                                         select move;

        return orderedMoves.ToArray();

    }

    Move[] ForcingMoves(Move[] moves, Board board)
    {
        IEnumerable<Move> forcingMoves = from move in moves
                                         where (move.IsCapture /* || MoveIsCheck(move, board) */)
                                         orderby (move.IsCapture ? (move.CapturePieceType - move.MovePieceType) : (int)move.MovePieceType) descending
                                         select move;
        return forcingMoves.ToArray();
    }

    int Evaluate(Board board)
    {
        int pieceSum = 0;
        int pieceValueMultiplier = 1;
        for (int color = 1; color >= 0; color--)
        {

            // Pieces
            for (int pieceType = 1; pieceType < pieceValues.Length - 1; pieceType++)
            {
                pieceSum += (board.GetPieceList((PieceType)pieceType, Convert.ToBoolean(color)).Count * pieceValues[pieceType] * pieceValueMultiplier);
            }

            pieceValueMultiplier = -1;
        }

        return pieceSum * (board.IsWhiteToMove ? 1 : -1);
    }

    bool MoveIsCheck(Move move, Board board)
    {
        board.MakeMove(move);
        bool check = board.IsInCheck();
        board.UndoMove(move);
        return check;
    }

    TranspositionEntry GetTransposition(ulong zobrist)
    {
        transpositionTable.TryGetValue((int)(zobrist % tableSize), out TranspositionEntry entry);
        if (entry.zobrist == zobrist)
        {
            return entry;
        }
        return NullEntry;

    }





}