namespace auto_Bot_321;
using ChessChallenge.API;
using System;
using System.Linq;

public class TranspositionTableEntry
{
    public double score;
    public Int16 NodeType; //0 for exact, 1 for lowerbound, 2 for Upperbound
    public int depth;
    public ulong key;

    public TranspositionTableEntry(double s)
    {
        score = s;
    }

}

public class Bot_321 : IChessBot
{
    public TranspositionTableEntry[] TranspositionTable = Array.ConvertAll(new double[16777216/4], i => new TranspositionTableEntry(double.NaN));
    public Move BestMove = Move.NullMove;
    static int CurrentDepth;
    static readonly int MaxDepth = 6;

    public static double Eval(Board b)
    { //return which side is better

        PieceList[] lP = b.GetAllPieceLists();
        int white = lP[0].Count * 100 + lP[1].Count * 300 + lP[2].Count * 300 + lP[3].Count * 500 + lP[4].Count * 900;
        int black = lP[6].Count * 100 + lP[7].Count * 300 + lP[8].Count * 300 + lP[9].Count * 500 + lP[10].Count * 900;
        int material = (white - black) * (b.IsWhiteToMove ? 1 : -1);

        double knightPlacement = (Enumerable.Sum(Enumerable.Select(lP[1], p => 10 - Math.Pow(p.Square.File - 3.5, 2) - Math.Pow(p.Square.Rank - 3.5, 2))) - Enumerable.Sum(Enumerable.Select(lP[7], p => 10 - Math.Pow(p.Square.File - 3.5, 2) - Math.Pow(3.5 - p.Square.Rank, 2)))) * (b.IsWhiteToMove ? 1 : -1);
        double kingPlacement = 2 * (Enumerable.Sum(Enumerable.Select(lP[5], p => Math.Pow(p.Square.File - 3.5, 2) / 2 - Math.Pow(p.Square.Rank - 3.5, 3) / 8 - 7)) - Enumerable.Sum(Enumerable.Select(lP[11], p => Math.Pow(p.Square.File - 3.5, 2) / 2 + Math.Pow(3.5 - p.Square.Rank, 3) / 8 - 7))) * (b.IsWhiteToMove ? 1 : -1);
        double pawnPlacement = 2 * (Enumerable.Sum(Enumerable.Select(lP[0], p => Math.Pow(2, 4 - p.Square.Rank) - Math.Pow(4, 3 - p.Square.Rank) - Math.Pow(p.Square.File - 3.5, 2) / 4 + 1)) - Enumerable.Sum(Enumerable.Select(lP[6], p => Math.Pow(2, p.Square.Rank - 3) - Math.Pow(4, p.Square.Rank - 4) - Math.Pow(p.Square.File - 3.5, 2) / 4 + 1))) * (b.IsWhiteToMove ? 1 : -1);
        return (material + knightPlacement + kingPlacement + pawnPlacement);


    }

    public double ABNegaMax(Board board, double alpha, double beta, int depth, bool colour, Timer timer)
    {
        //Transposition Table
        double alphaOrigin = alpha;
        ulong index = board.ZobristKey & (0xFFFFFF);
        TranspositionTableEntry entry = TranspositionTable[index];
        if (!double.IsNaN(entry.score) && entry.depth > depth && board.ZobristKey == entry.key && depth != CurrentDepth)
        {
            switch (entry.NodeType)
            {
                case (0): //Exact Node
                    return entry.score;
                case (1):
                    alpha = Math.Max(alpha, entry.score);
                    break;
                case (2):
                    beta = Math.Min(beta, entry.score);
                    break;
            }
        }
        if (alpha >= beta)
        {
            return entry.score;
        }

        //Negamax main implementation

        if (board.IsInCheckmate())
        {
            return -100000;//*(board.IsWhiteToMove ^ colour ? 1 : -1);
        }
        else if (board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.FiftyMoveCounter >= 100)
        {
            return 0;
        }

        if (depth == 0) { return Eval(board); }

        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves);

        double score = double.NegativeInfinity;
        foreach (Move m in moves)
        {
            board.MakeMove(m);
            score = Math.Max(score, -ABNegaMax(board, -beta, -alpha, depth - 1, !colour, timer));
            board.UndoMove(m);
            if (score > alpha)
            {
                alpha = score;
                if (depth == CurrentDepth)
                {
                    BestMove = m;
                }
            }
            if (alpha >= beta)
            {
                break;
            }
        }

        //Update Transposition table
        TranspositionTable[index] = new TranspositionTableEntry(score);
        if (score <= alphaOrigin)
        {
            TranspositionTable[index].NodeType = 2;
        }
        else if (score >= beta)
        {
            TranspositionTable[index].NodeType = 1;
        }
        else
        {
            TranspositionTable[index].NodeType = 0;
        }
        TranspositionTable[index].depth = depth;
        TranspositionTable[index].key = board.ZobristKey;

        return score;
    }

    public Move Think(Board board, Timer timer)
    {

        for (CurrentDepth = 1; CurrentDepth < MaxDepth; CurrentDepth++)
        {
            if (timer.MillisecondsElapsedThisTurn < Math.Min(timer.MillisecondsRemaining * 0.03, 500))
            {
                BestMove = Move.NullMove;
                ABNegaMax(board, double.NegativeInfinity, double.PositiveInfinity, CurrentDepth, board.IsWhiteToMove, timer);
            }
        }

        //Select Best Move
        return BestMove;
    }
}