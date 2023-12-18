namespace auto_Bot_581;
using ChessChallenge.API;
using System;
using System.Linq;


public class Bot_581 : IChessBot
{
    Move BestMove;
    int DepthMax = 6;



    public Move Think(Board board, Timer timer)
    {
        BestMove = Move.NullMove;

        int temp = 0;
        int tempdepth = 0;
        for (int depth = 2; depth <= DepthMax; depth++)
        {
            temp = FindBestMove(board, DepthMax, -999999, 999999, board.IsWhiteToMove ? 1 : -1);

            tempdepth = depth;
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;
        }

        return BestMove.IsNull ? board.GetLegalMoves()[0] : BestMove;
    }

    //
    int FindBestMove(Board board, int depth, int alpha, int beta, int turnMultiplier) //white = 1, black = -1
    {
        ulong key = board.ZobristKey;
        int highScore = -999999;

        if (depth == 0) return turnMultiplier * Score(board);

        TTEntry entry = tt[key & 0x7FFFFF];

        if (entry.key == key && entry.depth >= depth && entry.flag == 1)
        {
            return entry.score;
        }

        foreach (var move in board.GetLegalMoves().OrderByDescending(move => move.CapturePieceType))
        {
            board.MakeMove(move);
            var score = -FindBestMove(board, depth - 1, -beta, -alpha, -turnMultiplier); //white = 1, black = -1


            tt[key & 0x7FFFFF] = new TTEntry(key, move, depth, score, 1);

            if (score > highScore)
            {
                highScore = score;
                if (depth == DepthMax) BestMove = move;

            }

            board.UndoMove(move);

            if (highScore > alpha) alpha = highScore;

            if (alpha >= beta) break;

        }

        return highScore;
    }


    int Score(Board board)
    {
        int matScore = 0, posScore = 0, square = 0;
        //white = true
        foreach (bool whiteMove in new[] { true, false })
        {
            for (int i = 1; i < 7; i++)
            {
                square = 0;
                ulong bitboard = board.GetPieceBitboard((PieceType)i, whiteMove);

                matScore += (whiteMove ? 1 : -1) * BitboardHelper.GetNumberOfSetBits(bitboard) * pieceValues[i - 1];

                while (bitboard != 0)
                {
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard) ^ 56 * (whiteMove ? 0 : 1);

                    posScore += (whiteMove ? 1 : -1) * positionTable[i - 1][square];

                }

            }
        }

        return (matScore + posScore);

    }

    public Bot_581()
    {
        for (int table = 0; table < 6; table++)
        {
            positionTable[table] = new int[64];
            for (int square = 0; square < 64; square++)
            {

                var tempArray = BitConverter.GetBytes(positionTableCompressed[table + (square / 8) + (table * 7)]);
                var bit = (int)(((float)(square / 8f) - square / 8) * 8);
                positionTable[table][square] = tempArray[bit] - 100;
            }
        }
    }

    //tables
    struct TTEntry
    {
        public ulong key;
        public Move move;
        public int depth, score, flag;
        public TTEntry(ulong _key, Move _move, int _depth, int _score, int _flag)
        {
            key = _key; move = _move; depth = _depth; score = _score; flag = _flag;
        }
    }

    TTEntry[] tt = new TTEntry[0x800000];

    static int[] pieceValues = new int[] { 100, 320, 330, 500, 900, 0 };

    int[][] positionTable = new int[6][];

    static ulong[] positionTableCompressed = new ulong[] {
    7234017283807667300,
    7597130912646458985,
    7592886883996819305,
    7234017370042557540,
    7595723731791407465,
    7957430093540257390,
    10851025925711500950,
    7234017283807667300,
    3619845468139699250,
    4346084044315054140,
    5073707897346484550,
    5072306041580119110,
    5073713416463673670,
    5072300522462929990,
    4346084022756331580,
    3619845468139699250,
    5790039615047621200,
    6514848718311942490,
    6516267131429875290,
    6513452381662766170,
    6514854258987854170,
    6513446884104299610,
    6513441343428387930,
    5790039615047621200,
    7234017305366389860,
    6873729313618027615,
    6873729313618027615,
    6873729313618027615,
    6873729313618027615,
    6873729313618027615,
    7597131041998794345,
    7234017283807667300,
    5790039636606343760,
    6513441343428387930,
    6513446862545578330,
    6873734832735216740,
    6873734832735216735,
    6513446862545577050,
    6513441343428387930,
    5790039636606343760,
    8683624408984486520,
    8680798664100444280,
    6507789767425413210,
    5784387995927201360,
    5060986267546434630,
    5060986267546434630,
    5060986267546434630,
    5060986267546434630 };


}