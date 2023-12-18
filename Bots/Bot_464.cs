namespace auto_Bot_464;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

/*
 * TODO Fix init of positionValues
 * TODO Reduce impact of positionValues
 */

public class Bot_464 : IChessBot
{

    int StartDepth = 3;
    int TimeUsage = 10;
    int[] _pieceValues = { 0, 10, 30, 30, 50, 90, 9999 };
    Dictionary<ulong, (double, Move)> _cache = new();

    ulong[]?[] _positionTable = {
        null,
        new ulong[]{ 5412005, 2975208681795240456, 4923147017984688640, 3487652126984325, 5334585226876157952 },
        new ulong[]{ 15433109537103872000, 187092916593235632, 5062622360138826252, 1634881644264867072, 1189681297231665946 },
        new ulong[]{ 11863934192600481792, 5224888399041168, 4765094495945761032, 1337714366737453120, 55682726971988 },
        new ulong[]{ 558113, 594906159370998152, 75296145408, 1263259695478573056, 18691698753536 },
        new ulong[]{ 11863916050658623488, 5224888122217096, 148763996252672132, 1265584133993828354, 20498353801812 },
        new ulong[]{ 18428148565586426318, 8330751894883096758, 3579626227749456986, 15318195106760237347, 1784696631828940996 }
    };

    //                    (pieceType, rank, file) -> value
    Dictionary<(int, int, int), int> _positionValues = new();

    // TODO Rewrite Caching (fun) abc-bca kompilieren, donn geat des. hon i gheart.. hot mo a bekonnto.. a vögelein getschwitzert ~ Maxi

    public Bot_464()
    {
        for (int piece = 1; piece < _positionTable.Length; piece++)
        {
            int counter = 0;
            int rank = 0;
            int file = 0;
            ulong value = 0;

            foreach (ulong l in _positionTable[piece])
            {
                ulong rest = l;

                for (int i = 0; i < 64; i++)
                {
                    ulong bit = (rest & 0x8000000000000000) >> 63;
                    rest <<= 1;
                    value = (value << 1) | bit;

                    //DivertedConsole.Write(Convert.ToString((long)bit, 2));

                    if (++counter % 5 == 0)
                    {
                        int intValue = (value & 0x10) == 0x10 ? -1 * (int)(value & 0xf) : (int)value;
                        //DivertedConsole.Write(intValue + " ");
                        _positionValues.Add((piece, rank, file), intValue);
                        rank++;
                        value = 0;
                    }

                    if (counter % 40 == 0)
                    {
                        //DivertedConsole.Write();
                        file++;
                        rank = 0;
                    }
                }
            }
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move move = (board.IsWhiteToMove ? WhiteOpening(board, out move) : BlackOpening(board, out move)) ?
            move : ActualWorkingChessEngineLoL(board, timer);
        return move == Move.NullMove ? board.GetLegalMoves()[0] : move;
    }


    // Fried Liver
    bool WhiteOpening(Board board, out Move move)
    {
        Dictionary<ulong, String> moves = new() {
            { 13227872743731781434, "e2e4" },
            { 17664629573069429839, "g1f3" },
            { 4392852280258111003 , "f1c4" },
            { 10907350113667057435, "f3g5" },
            { 12094595613019000261, "e4d5" },
            { 6149061219614480062 , "g5f7" },
            { 15765041912239346957, "d1f3" }
        };

        move = moves.TryGetValue(board.ZobristKey, out String? m) ? new Move(m, board) : Move.NullMove;
        return move != Move.NullMove;
    }

    // King Indians Defense
    bool BlackOpening(Board board, out Move move)
    {
        Dictionary<ulong, String> moves = new() {
            { 15607329186585411972, "g8f6" },
            { 13920910881790336478, "g8f6" },
            { 3743624616017431465 , "f6e4" },
            { 8500792386727910725 , "g7g6" }
        };

        move = moves.TryGetValue(board.ZobristKey, out String? m) ? new Move(m, board) : Move.NullMove;
        return move != Move.NullMove;
    }

    Move ActualWorkingChessEngineLoL(Board board, Timer timer)
    {
       
        var allottedTime = timer.MillisecondsRemaining / TimeUsage;
        (double lastScore, Move lastMove) = (0, Move.NullMove);
        for (int depth = StartDepth; ; depth++)
        {
            _cache.Clear();

            // TODO Rework Time Control (currently not efficient)
            // TODO - look at time used => enough to calculate next depth?
            // TODO - rewrite MinMax, return boolean for time run out => to get currently best move that got evaluated
            (double score, Move move) = MinMax(board, timer, depth,
                allottedTime,
                double.NegativeInfinity, double.PositiveInfinity);

            if (timer.MillisecondsElapsedThisTurn >= allottedTime / 2)
            {
               
                //DivertedConsole.Write("[" + (depth - 1) + "]\tTime " + watch.ElapsedMilliseconds + "ms\tWill get Score " + lastScore +
                //                  " by playing move: " + lastMove);

                return lastMove;
            }

            lastMove = move;
            lastScore = score;
        }
    }

    (double, Move) MinMax(Board board, Timer timer, int depth, long maxTime, double alpha, double beta)
    {
        if (_cache.TryGetValue(board.ZobristKey, out (double, Move) result))
        {
            return result;
        }

        Move[] legalMoves = board.GetLegalMoves();
        if (legalMoves.Length == 0)
        {
            return (board.IsInCheckmate() ? double.NegativeInfinity : 0, Move.NullMove);
        }

        if (depth == 0 || timer.MillisecondsElapsedThisTurn >= maxTime)
        {
            return (EvaluateBoard(board), Move.NullMove);
        }

        Move bestMove = Move.NullMove;

        Array.Sort(legalMoves, (a, b) => MoveOrder(a, b, board));

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            (double score, Move _) = MinMax(board, timer, depth - 1, maxTime, -beta, -alpha);
            score = -score;
            board.UndoMove(move);

            if (score >= beta)
            {
                return (beta, move);
            }

            bestMove = alpha < score ? move : bestMove;
            alpha = alpha < score ? score : alpha;
        }

        _cache[board.ZobristKey] = (alpha, bestMove);

        return (alpha, bestMove);
    }

    double EvaluateBoard(Board board)
    {
        double eval = 0;

        foreach (PieceList list in board.GetAllPieceLists())
        {
            eval += (list.IsWhitePieceList ? 1 : -1) * _pieceValues[(int)list.TypeOfPieceInList] * list.Count;

            foreach (Piece piece in list)
            {
                var rank = piece.Square.Rank;
                var file = piece.Square.File;
                eval += _positionValues[((int)piece.PieceType, piece.IsWhite ? 7 - rank : rank, piece.IsWhite ? 7 - file : file)];
            }
        }

        // TODO + Bit tables for positioning

        return (board.IsWhiteToMove ? 1 : -1) * eval;
    }

    int MoveOrder(Move a, Move b, Board board)
    {
        return -(_pieceValues[(int)a.CapturePieceType] - _pieceValues[(int)b.CapturePieceType]
                 + _pieceValues[(int)a.PromotionPieceType] - _pieceValues[(int)b.PromotionPieceType]
                 - ((board.SquareIsAttackedByOpponent(a.TargetSquare) ? _pieceValues[(int)a.MovePieceType] : 0)
                    - (board.SquareIsAttackedByOpponent(b.TargetSquare) ? _pieceValues[(int)b.MovePieceType] : 0)));

        // + Taking moves
        //weight += _pieceValues[(int)a.CapturePieceType] - _pieceValues[(int)b.CapturePieceType];

        // + Promote pawn
        //weight += _pieceValues[(int)a.PromotionPieceType] - _pieceValues[(int)b.PromotionPieceType];

        // - Gifting piece
        //weight -= (board.SquareIsAttackedByOpponent(a.TargetSquare) ? _pieceValues[(int)a.MovePieceType] : 0 )
        //          - (board.SquareIsAttackedByOpponent(b.TargetSquare) ? _pieceValues[(int)b.MovePieceType] : 0 );

        // TODO + Check move
        // TODO Positional awareness
        //return -weight; // Invert because of ascending order
    }
}