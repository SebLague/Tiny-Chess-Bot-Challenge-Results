namespace auto_Bot_400;
/*
* 
* Yangbot V1 by Cyang946.
* Start date: 28/08/2023
* Complete date: 1/10/2023
* 
* TODO:
* - bishop pair bonus
* - fix time management 
* - reduce token count
* ISSUES:
* - poorer performance playing as black
* 
* 998 tokens
*/

using ChessChallenge.API;
using System;
using System.Collections.Generic;


public class Bot_400 : IChessBot
{
    // class variables
    Board b;
    Move BestMove = Move.NullMove;


    Dictionary<ulong, object[]> table; // transposition table
    Move[] killers;

    int[] values = { 0, 100, 280, 320, 479, 929, 60000 }; // piece values

    int searchTime;
    Timer t;


    // 37 tokens - still can optimise
    // Compressed PST - not very good but works
    (ulong, int)[][] pst = new[]
    {
        new[] // pawn
        {
            (71776119061217280UL, 50), (214559386265088UL, 10), (837527109888UL, 5),
            (39582821253120UL, 20), (103079215104UL, 25), (26388279066624UL, 30),
            (4325376UL, -5), (2359296UL, -10), (6144UL, -20)
        },
        new[] // knight
        {
            (4323598035499155516UL, -30), (4792111478498951490UL, -40),
            (9295429630892703873UL, -50), (18577348462920192UL, -20),
            (39582420959232UL, 10), (283472173056UL, 5), (26543503441920UL, 15),
            (103481868288UL, 20)
        },
        new[] // bishop
        {
            (9115709513998107006UL, -10), (9295429630892703873UL, -20),
            (26492373172224UL, 10), (40020505281024UL, 5)
        },
        new[] // rook
        {
            (35465847065542656UL, 10), (36310271995674624UL, 5),
            (142393223512344UL, -5)
        },
        new[] // queen
        {
            (66229406401536UL, 5), (1729382813108535320UL, -5),
            (7386326700872794470UL, -10), (9295429630892703873UL, -20)
        },
        new[] // king
        {
            (8454144UL, -10), (2172518400UL, -20),
            (9331882295650418688UL, -30), (7378697628168486912UL, -40),
            (1736164147709607936UL, -50), (36UL, 10),
            (50049UL, 20), (66UL, 30)
        }
    };



    public Move Think(Board board, Timer timer)
    {


        //Move[] moves = board.GetLegalMoves();
        //return moves[0];

        table = new Dictionary<ulong, object[]>();
        b = board;
        t = timer;
        killers = new Move[2048];

        // time management
        searchTime = t.MillisecondsRemaining / 40 + t.IncrementMilliseconds / 2; // use some clever equation
        if (searchTime >= t.MillisecondsRemaining) searchTime = t.MillisecondsRemaining - 500;
        if (searchTime < 0) searchTime = 100;


        // Start iterative deepening
        int depth = 2;
        while (t.MillisecondsElapsedThisTurn < searchTime)
        {
            Search(depth, 0, -999999, 999999);

            // Don't start another search if we have less than half our time left.
            if (t.MillisecondsElapsedThisTurn * 2 > searchTime) { break; }

            depth++;

        }
        DivertedConsole.Write("DEPTH: " + depth.ToString());
        return BestMove;

    }



    int evaluate()
    {
        // sum the weights of each piece
        int eval = 0;

        // there's got to be a better way to do this
        foreach (var item in b.GetAllPieceLists())
        {

            foreach (var piece in item)
            {
                int score = values[(int)piece.PieceType];
                foreach (var ps in pst[(int)piece.PieceType - 1])
                {
                    score += BitboardHelper.SquareIsSet(item.IsWhitePieceList ? ps.Item1 : ps.Item1 ^ 56, piece.Square) ? ps.Item2 : 0;
                }
                eval += (score + BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, b, piece.IsWhite)) / 10) * (piece.IsWhite ? 1 : -1);
            }



        }

        return eval * (b.IsWhiteToMove ? 1 : -1);
    }




    // Qsearch + move ordering + Alpha beta search
    int Search(int depth, int plyFromRoot, int alpha, int beta)
    {


        int mating = 999999 - plyFromRoot, eval = 0, count = 0, flag = 2, bestEval = -999999, searchedMoves = 0;
        bool qSearch = depth <= 0, check = b.IsInCheck();

        if (check) depth++; // extend search if in check

        // Get transposition table values
        var e = table.TryGetValue(b.ZobristKey, out object[] value) ? value : null;

        // mate distance pruning
        if (plyFromRoot > 0)
        {
            if (mating < beta)
            {
                beta = mating;
                if (alpha >= mating) return mating;
            }
            else if (-mating > alpha)
            {
                alpha = -mating;
                if (beta <= -mating) return -mating;
            }

        }
        if (b.IsDraw()) return e != null ? -(int)e[2] : 0;  // repetition detection with rudimentary contempt factor

        if (b.IsInCheckmate())
            return -mating;

        // access TT
        if (e != null && (int)e[0] >= depth && plyFromRoot > 0 && (int)e[2] < 900000)
        {
            if ((int)e[1] == 1)
                return (int)e[2];
            if ((int)e[1] == 3 && (int)e[2] <= alpha)
                return alpha;
            if ((int)e[1] == 2 && (int)e[2] >= beta)
                return beta;
        }


        if (qSearch) // Q search
        {
            eval = evaluate();
            if (eval >= beta)
                return beta;
            alpha = Math.Max(alpha, eval);
        }
        else if (!check) // null move pruning
        {
            b.ForceSkipTurn();
            eval = -Search(depth - 3, plyFromRoot + 1, -beta, -beta + 1);
            b.UndoSkipTurn();
            if (eval >= beta) return eval;

        }


        Span<Move> moves = stackalloc Move[256];
        b.GetLegalMovesNonAlloc(ref moves, qSearch && !check);

        // order moves
        int[] scores = new int[moves.Length];

        foreach (Move m in moves)
        {
            // sorry for terrible syntax
            scores[count] = values[(int)m.PromotionPieceType] + (!m.IsCapture ? (killers[plyFromRoot] == m ? 90000 : 0) : ((int)b.GetPiece(m.TargetSquare).PieceType * 10 + 6 - (int)b.GetPiece(m.StartSquare).PieceType));
            count++;
        }

        scores.AsSpan(0, moves.Length).Sort(moves);
        moves.Reverse();



        foreach (Move m in moves)
        {

            b.MakeMove(m);


            if (searchedMoves >= 3 && !b.IsInCheck() && !m.IsCapture)
                eval = -Search(depth - 2, plyFromRoot + 1, -beta, -alpha); // LMR
            else eval = alpha + 1;
            if (eval > alpha)
                eval = -Search(depth - 1, plyFromRoot + 1, -beta, -alpha); // full search


            b.UndoMove(m);



            if (eval > bestEval)
            {
                bestEval = eval;
                if (eval > alpha)
                {
                    flag = 1; // exact
                    alpha = eval;
                    if (plyFromRoot == 0) // we want to assign a good move only for the next turn
                        BestMove = m;
                    //bestIterationEval = eval;
                }
                if (eval >= beta)
                {
                    flag = 3;
                    bestEval = beta;
                    if (!m.IsCapture && !m.IsPromotion)
                        killers[plyFromRoot] = m;
                    break;
                }
            }
            searchedMoves++;
        }

        // Store in Transposition table - hopefully it won't take up 256MB limit
        table[b.ZobristKey] = new object[] { depth, flag, bestEval };
        return bestEval;

    }
}