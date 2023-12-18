namespace auto_Bot_118;
using ChessChallenge.API;
using System;

public class Bot_118 : IChessBot
{
    /* Hi, This is BotThatLikesToPlayChess™, a bot that likes to play chess.
     * He searches all moves with a depth of x where  (x >= 5  &&  (x % 2) == 1),
     * sure it may say 4 but then theres the first scan, i've compressed the code way too far 2 have less tokenzzz,
     * i also plan on adding move ordering 4 speed, but thatz kinda for later.
     * 
     * Some Settings */

    readonly int[] pieceVal = { 100, 300, 300, 500, 900, 10000, -100, -300, -300, -500, -900, -10000 }; // Rankings

    bool Abort;
    readonly byte MaxSDepth = 13;
    readonly int ThinkTime = 1000;
    int iid;
    int OpeningScan = 3;
    int DrawScore = 3000;
    int mCount = 0;
    Timer Timex;


    EntiryX[] T_Table = new EntiryX[2_550_000];

    struct EntiryX
    {
        public ulong index;
        public int eval;
        public byte depth = 0;

        public EntiryX(ulong key, int value, byte depth)
        {
            this.index = key;
            this.eval = value;
            this.depth = depth; // depth is how many ply were searched ahead from this position
        }
    };


    void StoreEval(ulong zKey, int eval, byte depth)
    {
        T_Table[zKey % (ulong)T_Table.Length] = new EntiryX(zKey, eval, depth); //right of mod has 2 == EntiryX size
    }

    public int LookupEvaluation(ulong zKey, int depth)
    {
        EntiryX entry = T_Table[zKey % (ulong)T_Table.Length];

        if (entry.index == zKey && entry.depth >= depth) return entry.eval;
        return -20_000;
    }




    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 1) return allMoves[0];

        // Pick a random move to play if nothing better is found
        Timex = timer;
        Random rng = new();

        bool biw2m = board.IsWhiteToMove;
        Abort = false;

        int defIter = biw2m ? -2147483648 : 2147483647;
        Move defMove = allMoves[rng.Next(allMoves.Length)];

        int bestEval = defIter; // Avoid Blundering
        Move bestMove = defMove; // Avoid _*ILLEGAL*_ Moves (to avoid going to jail)


        int eval;
        int Alpha;
        int Beta;
        int bestEvalThisIter = bestEval;
        Move bestMoveThisIter = bestMove;

        for (iid = 4; iid < MaxSDepth && !Abort; iid += 2)
        {
            Alpha = -2147483648;
            Beta = 2147483647;
            foreach (Move move in allMoves)
            {
                eval = (MoveIsMate(board, move, false) ? (biw2m ? -DrawScore : DrawScore) : (MoveIsMate(board, move) ? (biw2m ? 2147483647 : -2147483648) : GetEvalMultiDepth(board, move, iid, Alpha, Beta)));


                if (biw2m ? eval > bestEvalThisIter : eval < bestEvalThisIter)
                {
                    bestEvalThisIter = eval;
                    bestMoveThisIter = move;
                }

                if (biw2m ? eval > bestEval : eval < bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }

                if (Abort && iid > 4) { ++mCount; return bestMove; };
                if (biw2m ? eval > 2147480000 : eval < -2147480000) { ++mCount; return bestMove; }

                if (biw2m) Alpha = Math.Max(Alpha, bestEval); else Beta = Math.Min(Beta, bestEval);
                if (Beta <= Alpha) break;

            }
            if (mCount < OpeningScan) { ++mCount; return bestMove; };
            bestEval = bestEvalThisIter;
            bestMove = bestMoveThisIter;

            bestEvalThisIter = defIter;
            bestMoveThisIter = defMove;
            if (Abort && iid > 4) break;
        }
        ++mCount;
        return bestMove;
    }



    int SMove(Board board, int Depth, int Alpha, int Beta)
    {
        int LbestEval;
        int x;
        int lEval = LookupEvaluation(board.ZobristKey, Depth);
        if (lEval != -20_000) return lEval;
        if (Depth == 0) return GetEval(board);
        if (board.IsWhiteToMove)
        {

            LbestEval = -2147483648;
            foreach (Move move in board.GetLegalMoves())
            {
                if (MoveIsMate(board, move)) return 2147483647;

                if (Abort && iid > 4) return 0; // maybe you shoud think about All moves onlow depth

                x = MoveIsMate(board, move, false) ? -DrawScore : GetEvalMultiDepth(board, move, Depth - 1, Alpha, Beta);

                LbestEval = Math.Max(LbestEval, x);
                Alpha = Math.Max(Alpha, x);

                if (Timex.MillisecondsElapsedThisTurn > ThinkTime) Abort = true;
                if (Beta <= Alpha) return LbestEval;
            }

        }
        else
        {
            LbestEval = 2147483647;
            foreach (Move move in board.GetLegalMoves())
            {
                if (MoveIsMate(board, move)) return -2147483648;
                if (Abort && iid > 4) return 0; // maybe you shoud think about All moves 2 make surre

                x = MoveIsMate(board, move, false) ? DrawScore : GetEvalMultiDepth(board, move, Depth - 1, Alpha, Beta);

                LbestEval = Math.Min(LbestEval, x);
                Beta = Math.Min(Beta, x);

                if (Timex.MillisecondsElapsedThisTurn > ThinkTime) Abort = true;
                if (Beta <= Alpha) return LbestEval;
            }
        }

        if (Timex.MillisecondsElapsedThisTurn > ThinkTime) Abort = true;
        StoreEval(board.ZobristKey, LbestEval, (byte)Depth);
        return LbestEval;
    }




    int GetEval(Board board)
    {
        int CurrSum = 0;
        int Loops = 0;

        foreach (PieceList pl in board.GetAllPieceLists())
        {
            CurrSum += pieceVal[Loops] * pl.Count;
            Loops++;
        }

        return CurrSum;
    }
    int GetEvalMultiDepth(Board board, Move move, int DepthX, int Alpha, int Beta)
    {
        board.MakeMove(move);
        int eval = SMove(board, DepthX, Alpha, Beta);
        board.UndoMove(move);
        return eval;
    }

    static bool MoveIsMate(Board board, Move move, bool Mate = true)
    {
        board.MakeMove(move);
        bool isMate = Mate ? board.IsInCheckmate() : board.IsDraw();
        board.UndoMove(move);
        return isMate;
    }

}