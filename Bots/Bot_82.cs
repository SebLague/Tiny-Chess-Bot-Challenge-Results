#define TRAIN
namespace auto_Bot_82;

using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Bot_82 : IChessBot
{
    public bool AmIWhite;
    public Board board;
    public Timer timer;


    //public double p_ratioDeeperTurn = 0.8;//0.95;//0.8;
    public double p_pawnRank = 0.032;// 0.029;//0.025;// 0.02;
    public double p_pawnDiag = 0.015;//0.012;//0.01;
    public double p_valueMoveBase = 0.0035;//0.0036;//0.004;//0.005;
    public double p_metaValueMove = 1.2;//1.1;//0.8;//0.7;//0.6;//0.7;
    public double p_drawFactor = 0.6;//0.75;//0.8;//1;
    public int p_timeFactor3 = 250;//333;//300;//50;//70;//60;
    public double p_metaRankNFile = 0.5;//0.6;//0.7;//0.5;//70;//60;
    //public int p_timeFactor2 = 10;
    public int p_maxDepth = 7;//6;//7;//10;
    public double p_castleRight = 0.3;

    //public int[] p_pickMovesMax = new int[] { 20, 8, 5, 2, 2, 2 };
    // public int[] p_pickMovesMax = new int[] { 20, 8, 5, 2, 1, 1 };
    public double[] p_rankCoverBonus = { 0.0004, 0.0012, 0.0020, 0.0025, 0.0022, 0.0020, 0.0012, 0.0004 };
    public double[] p_fileCoverBonus = { 0.0003, 0.001, 0.003, 0.005, 0.006, 0.0033, 0.001, 0.0005 };
    public double[] p_pressurePieceBonus = { 0, 0.02, 0.16, 0.04, 0.11, 0.24, 0.5 };
    public double[] p_coverPieceBonus = { 0, 0.01, 0.06, 0.06, 0.06, 0.04, 0 };
    public double[] p_valuePiece = { 0, 1.3, 3.3, 3.6, 6.2, 12, 100 };


    //private Dictionary<ulong, double> boardScores = new ();
    private const int cacheEntries = 524288;
    private double[] boardScores = new double[cacheEntries];
    private ulong[] cacheKeys = new ulong[cacheEntries];

    //private Dictionary<ulong, Move[]> cachedMoves = new ();
    //  private Dictionary<ulong, (int,(Move, double))> cachedTree = new ();  //depth, (move, score)
    //private int cacheHits = 0;
    //private int nonCached = 0;
    //private int depthReached = 0;
    private Move[] killerMoves = new Move[100];
    private Move[] moveAr = new Move[1000];
    private Square[] squares = Enumerable.Range(0, 64).Select(x => new Square(x)).ToArray();
    //we may want to generate a reusable list of doubles with some medium precision from a list of longs for space efficiency
    private double[,] tileScores; //represents how much we want to be on tiles. First id is index, 2nd is boardpos.  we use 1 for white pawns, 0 for black pawns
    private double[] boardPerDepth = new double[100];
    private double[] boardPerDepthWentInto = new double[100];

    public Bot_82()
    {

    }
    int maxdepth;
    //int thinkturns;
    Move bestmove = Move.NullMove;
    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;
        AmIWhite = board.IsWhiteToMove;
        //var m = GetBest(0,Move.NullMove);
        //return m.Item1;

        if (tileScores == null)
        {
            tileScores = new double[7, 64];
            for (int piece = 0; piece < 7; piece++)
            {
                for (int s = 0; s < 64; s++)
                {
                    var square = squares[s];
                    ulong attackablesquares =
                        piece == 0 ? BitboardHelper.GetPawnAttacks(square, false) :
                        (piece == 1 ? BitboardHelper.GetPawnAttacks(square, true) :
                        ((piece == 2 ? BitboardHelper.GetKnightAttacks(square) :
                         (piece == 6 ? BitboardHelper.GetKingAttacks(square) : BitboardHelper.GetSliderAttacks((PieceType)piece, square, 0)))));
                    while (attackablesquares != 0)
                    {
                        var sq2 = squares[BitboardHelper.ClearAndGetIndexOfLSB(ref attackablesquares)];
                        tileScores[piece, s] += (p_rankCoverBonus[AmIWhite ? sq2.Rank : 7 - sq2.Rank] + p_fileCoverBonus[sq2.File]) * p_metaRankNFile;
                    }
                }

            }
        }


        maxdepth = 1;
        for (; maxdepth <= p_maxDepth; maxdepth++)
        {
            killerMoves = new Move[100];
            // depthReached++;
            NegaScout(maxdepth, -100000000000, 10000000000, 1);
            //approx how long we'll take calculating the next depth. we don't want it to take too long compared to remaining time
            //it takes us about 8 times as long to the next depth as the previous one. we want to save enough for another good chunk of moves after this one (40+)
            if (timer.MillisecondsElapsedThisTurn * p_timeFactor3 > timer.MillisecondsRemaining) break;
        }
        //this section can be removed for codesize
        //thinkturns++; 
        //DivertedConsole.Write("Nonhits " + nonCached + " cached " + cacheHits + " avg depth: " + (depthReached / ((double)thinkturns)));
        //for (int i = 0; i <= maxdepth; i++)
        //{
        //    DivertedConsole.Write("Depth: " + i + " boards: " + boardPerDepthWentInto[i] + " / " + boardPerDepth[i] +  "  "  + (double)boardPerDepthWentInto[i] / boardPerDepth[i]);
        //}
        return bestmove;
    }

    private double NegaScout(int depth, double alpha, double beta, int color)
    {
        //Move bestMove = Move.NullMove;
        //double val;

        //if (cachedTree.TryGetValue(board.ZobristKey, out (int, (Move, double)) cache) && cache.Item1 >= depth)
        //{
        //    //if (depth > 0)
        //    //{

        //    //}
        //    return cache.Item2; //check if we can always escape here?
        //}
        boardPerDepthWentInto[depth]++;
        //if (depth == 0 || timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / p_timeFactor2)
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw()) //we get blunders with the line above
        {
            return color * curBoardScore();
        }
        else
        {
            bool first = true;
            foreach (var m in SortedMoves(depth, color))
            {
                double score;
                board.MakeMove(m);
                if (first)
                {
                    score = -NegaScout(depth - 1, -beta, -alpha, -color);
                    first = false;
                }
                else
                {
                    score = -NegaScout(depth - 1, -alpha - 0.000001, -alpha, -color);//search with null window
                    if (score >= alpha && score < beta) score = -NegaScout(depth - 1, -beta, -score, -color);

                }
                board.UndoMove(m);
                if (score > alpha)
                {
                    alpha = score;
                    if (depth == maxdepth) bestmove = m;
                    if (alpha >= beta)
                    {
                        killerMoves[depth] = m;
                        break;
                    }
                }
            }
            return alpha;
        }
        //var r = (bestMove, val); ;
        //cachedTree[board.ZobristKey] = (depth, r);
        //return r;
    }

    private IEnumerable<Move> SortedMoves(int depth, int color)
    {
        //Span<Move> moves = moveAr;
        //board.GetLegalMovesNonAlloc(ref moves);
        var moves = board.GetLegalMoves();
        boardPerDepth[depth - 1] += moves.Length;
        //return moves;
        var sorted = moves.OrderByDescending(move =>
        {
            if (move.Equals(killerMoves[depth]))
            {
                return 999999;
            }

            board.MakeMove(move);
            double score = curBoardScore();
            board.UndoMove(move);
            return score * color;
        });
        //if (depth == 1 && maxdepth == 2)
        //{

        //}
        return sorted;
    }
    private Move[] legalMoves()
    {

        return board.GetLegalMoves(); //weirdly caching seems to slow it down? were also saving too much
    }
    private double curBoardScore()
    {
        //We're expecting a board state here where we just did the turn of the bo we're evaluating
        double score = 0;
        ulong cachekey = board.ZobristKey % cacheEntries;

        //if (boardScores.TryGetValue(board.ZobristKey, out double cachedscore))
        //{
        //    score = cachedscore;
        //    cacheHits++;
        //}
        if (cacheKeys[cachekey] == board.ZobristKey)
        {
            score = boardScores[cachekey];
            //  cacheHits++;
        }
        else
        {
            if (board.IsInCheckmate())
            {
                score = AmIWhite == board.IsWhiteToMove ? -99999999999999 : 99999999999999;
            }
            else if (!board.IsDraw())
            {
                score = valueNew(AmIWhite) - valueNew(!AmIWhite);
            }
            cacheKeys[cachekey] = board.ZobristKey;
            boardScores[cachekey] = score;
            //nonCached++;
        }
        return score;
    }

    private double valueNew(bool white)
    {
        //Pawn,   // 1
        //Knight, // 2
        //Bishop, // 3
        //Rook,   // 4
        //Queen,  // 5
        //King    // 6
        //ulong enemyPieces = white ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
        //ulong myPieces = white ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;

        double val = 0;
        ulong[] masks = new ulong[14];
        for (int piece = 1; piece < 7; piece++)
        {
            masks[piece] = board.GetPieceBitboard((PieceType)piece, white);
            masks[piece + 7] = board.GetPieceBitboard((PieceType)piece, !white);
        }

        for (int piece = 1; piece < 7; piece++)
        {
            ulong boardMask = masks[piece];
            while (boardMask != 0)
            {
                var ind = BitboardHelper.ClearAndGetIndexOfLSB(ref boardMask);
                var square = squares[ind];
                ulong attackablesquares = piece == 1 ? BitboardHelper.GetPawnAttacks(square, white) :
                    (piece == 2 ? BitboardHelper.GetKnightAttacks(square) :
                     (piece == 6 ? BitboardHelper.GetKingAttacks(square) : BitboardHelper.GetSliderAttacks((PieceType)piece, square, board)));
                val += p_valuePiece[piece]
                    + p_valueMoveBase * BitOperations.PopCount(attackablesquares)   //note: for non-sliders, this doesnt represent mobility
                    + tileScores[(piece == 1 && !white) ? 0 : piece, ind];
                for (int p2 = 1; p2 < 7; p2++) //for each piece type, check how many pieces of that type this piece covers
                {
                    val += p_coverPieceBonus[p2] * BitOperations.PopCount(attackablesquares & masks[p2]) + p_pressurePieceBonus[p2] * BitOperations.PopCount(attackablesquares & masks[p2 + 7]);
                }
            }
        }
        if (board.HasKingsideCastleRight(white)) val += p_castleRight;
        if (board.HasQueensideCastleRight(white)) val += p_castleRight;
        return val;


        //just counts pieces, fast, for comparison
        //for (int piece = 1; piece < 7; piece++)
        //{
        //    ulong boardMask = board.GetPieceBitboard((PieceType)piece, white);
        //    val += p_valuePiece[piece] * BitOperations.PopCount(boardMask);
        //}

        //return -val;
    }



}