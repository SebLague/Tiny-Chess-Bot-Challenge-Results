namespace auto_Bot_256;
using ChessChallenge.API;
using System;
//using System.Numerics;
//using System.Collections.Generic;
//using System.Linq;

public class Bot_256 : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    //const int infinity = 1_000_000_000;

    struct TTEntry
    {
        public uint Key;
        public int Eval;
        public byte Depth;
        /// <summary> 0=exact, 1=lowerbound, 2=upperbound </summary>
        public byte Type;
    }

    //const int ttSize = 21_333_333; // 256_000_000 / sizeof(TTEntry)(12);
    TTEntry[] transpositionTable = new TTEntry[21_333_333];

    public Move Think(Board board, Timer timer)
    {
        Move move = Move.NullMove;

        for (int depth = 1; depth < 128; depth++)
        {
            move = Search(board, depth, move);

            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 120)
                break;
        }

        return move;
    }

    Move Search(Board board, int depth, Move prevBest)
    {
        bool white = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);
        Move bestMove = moves[0];
        int bestEval = -1_000_000_000;

        int alpha = -1_000_000_000;
        int beta = 1_000_000_000;

        if (!prevBest.IsNull)
            SearchMove(prevBest);

        foreach (Move m in moves)
            SearchMove(m);

        void SearchMove(Move m)
        {
            board.MakeMove(m);
            int eval = -Negamax(board, !white, depth - 1, -beta, -alpha);
            board.UndoMove(m);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = m;
            }

            if (eval > alpha)
                alpha = eval;
        }

        return bestMove;
    }

    // https://en.wikipedia.org/wiki/Negamax
    int Negamax(Board board, bool white, int depth, int alpha, int beta)
    {
        int alphaOg = alpha;

        ulong ttIndex = board.ZobristKey % 21_333_333;
        TTEntry ttEntry = transpositionTable[ttIndex];

        if (ttEntry.Key == (uint)board.ZobristKey &&
            ttEntry.Depth >= depth)
        {
            // moramo popravit rezultat ce je bil shranjen mat
            // shranjen je bil samo PlyCount od shranjevanja naprej zato dodamo trenuten PlyCount
            // https://github.com/maksimKorzh/chess_programming/blob/master/src/bbc/tt_search_mating_scores/TT_mate_scoring.txt
            if (Math.Abs(ttEntry.Eval) > 900_000_000)
                ttEntry.Eval -= board.PlyCount * Math.Sign(ttEntry.Eval);

            // EXACT
            if (ttEntry.Type == 0)
                return ttEntry.Eval;
            // LOWERBOUND
            else if (ttEntry.Type == 1)
                alpha = Math.Max(alpha, ttEntry.Eval);
            // UPPERBOUND
            else
                beta = Math.Min(beta, ttEntry.Eval);

            if (alpha >= beta)
                return ttEntry.Eval;
        }

        if (board.IsInCheckmate())
            // ce to negiras je mat = infinity - PlyCount, torej je bolje imeti cim manjsi PlyCount (cim hitrejsi mat)
            return -1_000_000_000 + board.PlyCount;

        if (board.IsDraw())
            return 0;

        if (depth <= 0)
            return Evaluate(board);

        Move[] moves = board.GetLegalMoves();
        OrderMoves(moves);
        int bestEval = -1_000_000_000;

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int extension = board.IsInCheck() ? 1 : 0;
            int eval = -Negamax(board, !white, depth - 1 + extension, -beta, -alpha);
            board.UndoMove(m);

            if (eval > bestEval)
                bestEval = eval;

            if (eval > alpha)
                alpha = eval;

            if (alpha >= beta)
                break;
        }

        ttEntry.Key = (uint)board.ZobristKey;
        ttEntry.Depth = (byte)depth;
        ttEntry.Eval = bestEval;

        // ce je eval mat, potem za shranjevanje v tt pristejemo trenuten
        // PlyCount tako da je v ttju shranjen PlyCount od shranjevanja naprej
        if (Math.Abs(ttEntry.Eval) > 900_000_000)
            ttEntry.Eval += board.PlyCount * Math.Sign(ttEntry.Eval);

        if (bestEval <= alphaOg)
            ttEntry.Type = 2; // UPPERBOUND
        else if (bestEval >= beta)
            ttEntry.Type = 1; // LOWERBOUND
        else
            ttEntry.Type = 0; // EXACT

        transpositionTable[ttIndex] = ttEntry;

        return bestEval;
    }

    // https://www.chessprogramming.org/Evaluation
    public int Evaluate(Board board)
    {
        int eval = 10;
        int side = board.IsWhiteToMove ? 1 : -1;

        int totalPieceEval = 0;

        for (PieceType i = PieceType.Pawn; i <= PieceType.King; i++)
        {
            int w = board.GetPieceList(i, true).Count * pieceValues[(int)i];
            eval += w * side;

            int b = board.GetPieceList(i, false).Count * pieceValues[(int)i];
            eval -= b * side;

            totalPieceEval += w + b;
        }

        // endgame bo 1 ob totalPieceEval 42250 in 0 ob 43250 vmes bo linearno
        float middlegameWeight = Math.Clamp((totalPieceEval - 42250) / 1000.0f, 0.0f, 1.0f);
        float endgameWeight = 1.0f - middlegameWeight;

        if (middlegameWeight > 0.0f)
            for (int i = 0; i < 2; i++)
            {
                ulong pawns = board.GetPieceBitboard(PieceType.Pawn, i == 0 ? board.IsWhiteToMove : !board.IsWhiteToMove);
                int add = (int)(20 * middlegameWeight) * (i == 0 ? 1 : -1);

                // dodatne tocke za kemete v sredini
                eval += BitboardHelper.GetNumberOfSetBits(pawns & 0b00000000_00000000_00000000_00011000_00011000_00000000_00000000_00000000) * add;

                // za podvojene kmete na istem filu
                add /= 2;
                for (int j = 0; j < 8; j++)
                    if (BitboardHelper.GetNumberOfSetBits(pawns & 0x8080808080808080 >> j) > 1)
                        eval -= add;
            }

        // v endgamu tocke za kmete ki so blizje promociji
        if (endgameWeight > 0.0f)
        {
            foreach (Piece piece in board.GetPieceList(PieceType.Pawn, true))
                eval += (int)((piece.Square.Rank - 3) * 5 * endgameWeight * side);

            foreach (Piece piece in board.GetPieceList(PieceType.Pawn, false))
                eval -= (int)((4 - piece.Square.Rank) * 5 * endgameWeight * side);
        }

        void AddKingDistToCenter(Square square, int add)
        {
            float distToCenter = Math.Abs(3.5f - square.Rank) + Math.Abs(3.5f - square.File);
            eval -= (int)(distToCenter * 5 * add * (endgameWeight * 2f - 1f));
        }
        // v endgamu kralja daj blizje sredini, v middlegamu pa stran od sredine
        AddKingDistToCenter(board.GetKingSquare(board.IsWhiteToMove), 1);
        AddKingDistToCenter(board.GetKingSquare(!board.IsWhiteToMove), -1);

        // minus za konje ki so na robu
        eval -= 15 * side * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Knight, true) & 0b11111111_10000001_10000001_10000001_10000001_10000001_10000001_11111111);
        eval += 15 * side * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Knight, false) & 0b11111111_10000001_10000001_10000001_10000001_10000001_10000001_11111111);

        // minus za laufarje ki so na zacetku
        eval -= 12 * side * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Bishop, true) & 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_11111111);
        eval += 12 * side * BitboardHelper.GetNumberOfSetBits(board.GetPieceBitboard(PieceType.Bishop, false) & 0b11111111_00000000_00000000_00000000_00000000_00000000_00000000_00000000);

        // minus za zgodnjo kraljico
        //if ((board.GetPieceBitboard(PieceType.Queen, true)  & 0b11111111_11111111_11111111_11111111_11111111_11111111_00000000_00000000) > 0) eval -= (int)(20 * side * middlegameWeight);
        //if ((board.GetPieceBitboard(PieceType.Queen, false) & 0b00000000_00000000_11111111_11111111_11111111_11111111_11111111_11111111) > 0) eval += (int)(20 * side * middlegameWeight);

        return eval;
    }

    // search je hitrejsi ce najprej pregledam poteze ki so bolj zanimive
    void OrderMoves(Move[] moves)
    {
        int j = 0;

        for (int i = 0; i < moves.Length; i++)
            if (moves[i].IsCapture || moves[i].IsPromotion)
            {
                (moves[i], moves[j]) = (moves[j], moves[i]);
                //Move m = moves[j];
                //moves[j] = moves[i];
                //moves[i] = m;
                j++;
            }
    }

    // https://github.com/SebLague/Chess-Coding-Adventure
}
