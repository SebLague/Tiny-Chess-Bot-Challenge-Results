namespace auto_Bot_378;
using ChessChallenge.API;
using System;

public class Bot_378 : IChessBot
{
    struct TTEntry
    {

        public ulong key;

        public int depth, score;

        public Move move;

        public int bound;

        public TTEntry(ulong _key, int _depth, int _score, Move _move, int _bound)
        {
            key = _key; depth = _depth; score = _score; move = _move; bound = _bound;
        }
    }
    const int infinity = 9999999;
    readonly int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };
    int fDepth;
    Move bestMove = Move.NullMove;
    const int numEntries = 1 << 20;
    TTEntry[] entries = new TTEntry[numEntries];

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        fDepth = 0;
        for (int depth = 1; depth < 50; depth++)
        {
            fDepth = depth;
            search(board, timer, fDepth, -infinity, infinity, board.IsWhiteToMove);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
            {
                break;
            }

        }
        DivertedConsole.Write(fDepth);
        return bestMove;
    }

    private int search(Board board, Timer timer, int depth, int alpha, int beta, bool color)
    {

        ulong key = board.ZobristKey;

        Move[] moves;

        int perspective = color ? 1 : -1;

        TTEntry tt = entries[key % numEntries];

        if (depth != fDepth && board.IsRepeatedPosition())
        {
            return 0;
        }

        if (tt.key == key && tt.depth >= depth && depth != fDepth && (
            tt.bound == 3
                    || tt.bound == 2 && tt.score >= beta
                    || tt.bound == 1 && tt.score <= alpha
        ))
        {

            return tt.score;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        if (depth == 0 || (moves = orderList(board, board.GetLegalMoves())).Length == 0)
        {
            if (board.IsInCheckmate()) return int.MinValue;

            int sum = 0;

            for (int i = 0; ++i < 7;)
            {
                sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceValues[i];
            }

            return sum * perspective;
        }



        int best = int.MinValue;
        int orginalAlpha = alpha;
        foreach (Move move in moves)
        {

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return 0;

            board.MakeMove(move);
            int eval = -search(board, timer, depth - 1, -beta, -alpha, !color);
            board.UndoMove(move);

            if (best < eval)
            {
                best = eval;

                if (depth == fDepth)
                {
                    bestMove = move;
                }
            }

            alpha = Math.Max(best, alpha);
            if (alpha >= beta) break;
        }
        int bound = best >= beta ? 2 : best > orginalAlpha ? 3 : 1;
        entries[key % numEntries] = new TTEntry(key, depth, best, bestMove, bound);
        return best;
    }

    // public int evalPos(Board board, bool color){
    //     int whiteEval = countMaterialForColor(board, true);
    //     int blackEval = countMaterialForColor(board, false);

    //     int evaluation = whiteEval - blackEval;

    //     int evalModifier = color ? 1 : -1;

    //     evaluation *= evalModifier;

    //     return evaluation;
    // }

    // public int countMaterialForColor(Board board, bool white){
    //     int material = 0;
    //     material += board.GetPieceList(PieceType.Pawn,white).Count * pieceValues[1];
    //     material += board.GetPieceList(PieceType.Knight,white).Count * pieceValues[2];
    //     material += board.GetPieceList(PieceType.Bishop,white).Count * pieceValues[3];
    //     material += board.GetPieceList(PieceType.Rook,white).Count * pieceValues[4];
    //     material += board.GetPieceList(PieceType.Queen,white).Count * pieceValues[5];
    //     return material;
    // }

    public Move[] orderList(Board board, Move[] moves)
    {
        int[] vals = new int[moves.Length];



        for (int i = 0; i < moves.Length; i++)
        {
            Move currMove = moves[i];
            int movePieceType = (int)currMove.MovePieceType;
            int capturePieceType = (int)currMove.CapturePieceType;
            int moveScore = 0;
            TTEntry tt = entries[board.ZobristKey % numEntries];

            if (tt.move == currMove && board.ZobristKey == tt.key)
            {
                moveScore += 100000;
            }


            if (capturePieceType != (int)PieceType.None)
            {
                //moveScore = 10 * pieceValues[capturePieceType] - pieceValues[movePieceType];
                moveScore = 5 * pieceValues[capturePieceType];
            }

            if (currMove.IsPromotion)
            {
                moveScore += pieceValues[(int)currMove.PromotionPieceType];

            }

            if (board.SquareIsAttackedByOpponent(currMove.TargetSquare))
            {
                moveScore -= pieceValues[movePieceType];
            }

            vals[i] = moveScore;
        }
        return sort(moves, vals);
    }

    public Move[] sort(Move[] moves, int[] vals)
    {

        //TODO: Merge sort
        Move[] nMoves = moves;
        for (int i = 0; i < vals.Length - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (vals[j] > vals[swapIndex])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (vals[j], vals[swapIndex]) = (vals[swapIndex], vals[j]);
                }
            }
        }

        return nMoves;
    }




}
