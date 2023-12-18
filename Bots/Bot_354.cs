namespace auto_Bot_354;
//#define Stats
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_354 : IChessBot
{

    public int[,,] history = new int[2, 64, 64];

    public int[]
        pieceValues =
            { 0, 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0 },
        phaseWeights = { 0, 1, 1, 2, 4, 0 },
        pieceSquareTables,
        //reverseScores = new int[256],
        killerMoveA = new int[256],
        killerMoveB = new int[256];

    public double
        timeToMove;

    public record struct Entry(ulong Key, int Depth, int Value, Move Move, int Type);

#if Stats
    public int
        positionsEvaluated,
        cutoffs,
        positionsLookedUp;
#endif

    public Board board;
    public Timer timer;

    public Move bestMove;

    public bool EndSearch => timer.MillisecondsElapsedThisTurn > timeToMove;

    static public readonly Entry[] entries = new Entry[2097152]; // 2 << 20
    public ulong TTIndex => board.ZobristKey & 2097151; // (2 << 20) - 1

    public decimal[] pieceSquareTablesCompressed = {
        74879660639201885626451361792m,
        76778888303437726232377626870m,
        4337668193350454506671701998m,
        75224380462801774090478356495m,
        17948908497041709890m,
        74881989004161693197260554240m,
        1867766520422617083970903533m,
        77697719446097427664963898124m,
        13647756623818514825464253434m,
        75828861727344587718515578681m,
        75195180865163132509289563279m,
        26662554952084069263276280m,
        5270959001466895827668175360m,
        78923631730433407317849802775m,
        77047311986497968612621228534m,
        77686654311914626649806350356m,
        77666187336355102871305386228m,
        72725350585174353650741151488m,
        5267261184081420082734493672m,
        3144557932460502999946172176m,
        10548010391875290459640632594m,
        69300392664127301618813437482m,
        79219709312310809925918391273m,
        78612814549352295510995173885m,
        1553422307037805421201387246m,
        11164613932037958695693264403m,
        68096491444117385810118115565m,
        1555844509009025930894568709m,
        71469280734820786676299921399m,
        72100264694604140873190663137m,
        78283991817075333461283246330m,
        2478193354900092165286264571m,
        1862978447432510496124698624m,
        77681932223948714375154565128m,
        938202381267143715744253448m,
        17707302466957358503893336831m,
        9182937923478844792m,
        58752505256987239430083313664m,
        1865315592112220439095344112m,
        75520420341338817255523547648m,
        3096054042548049374188600564m,
        70545600609248740551779679750m,
        78592243660024772968026595053m,
        73956008717690731732150384378m,
        3723505635522632233137864440m,
        311931362114773264575104516m,
        76758322250335567632765352705m,
        74573774441285435147413289214m,
        310679712265366860806095354m,
        76437928592312631340274678522m,
        9680906196640218296419074m,
        78916250245181588771862216705m,
        3109385689967201897914304519m,
        71166950662829553022872651784m,
        1251157775602594511273652722m,
        4673816222678072992794544902m,
        10220282252786924589520260610m,
        24254795630518732044637983m,
        77355431882921062134000258810m,
        76129695249923003928127010797m,
        4969889118723864252222275316m,
        639625965534967986364223250m,
        3415271961819825832591297286m,
        75826297092423864056714041611m,
    };

    public Bot_354()
    {
        //DivertedConsole.Write(System.Runtime.InteropServices.Marshal.SizeOf<Entry>() * entries.Length / 1024 / 1024); // Transposition table size

        pieceSquareTables = pieceSquareTablesCompressed
            .SelectMany(x => decimal.GetBits(x).Take(3))
            .SelectMany(BitConverter.GetBytes)
            .Select(x => (sbyte)x * 187 / 127)
            .ToArray();

        //for (int i = 0; i < 12 * 8 * 8; ++i)
        //{
        //    if (i % 64 == 0) DivertedConsole.Write();
        //    if (i % 8 == 0) DivertedConsole.Write();
        //    DivertedConsole.Write(pieceSquareTables[i] + " ");
        //}

        for (int i = 0; i < 768;)
            pieceSquareTables[i] += pieceValues[i++ / 64 + 1];
    }

    public Move Think(Board board_param, Timer timer_param)
    {
#if Stats
        positionsEvaluated = cutoffs = positionsLookedUp = 0;
#endif
        board = board_param;
        timer = timer_param;


        int currentDepth = 0;
        bestMove = board.GetLegalMoves()[0];

        timeToMove = Math.Max(150, timer.MillisecondsRemaining - 1000) / Math.Max(20, 60 - board.PlyCount) * 0.8;

        while (!EndSearch) Search(0, ++currentDepth, -100_000_000, 100_000_000);

        //timeToMove = 100000;
        //for (; currentDepth < 6;) Search(0, ++currentDepth, -100_000_000, 100_000_000);

#if Stats
        DivertedConsole.Write("Time: " + timer.MillisecondsElapsedThisTurn +
                            " " + bestMove.ToString() +
                            " Cutoffs: " + cutoffs +
                            " Positions: " + positionsEvaluated +
                            " PositionsLookedUp " + positionsLookedUp +
                            " Depth: " + currentDepth);
#endif

        return bestMove;
    }

    public int Evaluate()
    {
#if Stats
        ++positionsEvaluated;
#endif

        int middlegame = 0, endgame = 0,
            phase = 0;

        foreach (bool white in stackalloc[] { true, false })
        {
            //var enemyKing = board.GetKingSquare(!white);
            // Material
            for (int piece = 0; piece < 6; ++piece)
            {
                ulong bitboard = board.GetPieceBitboard((PieceType)(piece + 1), white);

                while (bitboard != 0)
                {
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard),
                        index = piece * 64 + square ^ (white ? 0 : 56);
                    middlegame += pieceSquareTables[index];
                    endgame += pieceSquareTables[index + 384];
                    phase += phaseWeights[piece];

                    // king safety
                    //if (piece != 5) endgame += (Math.Abs((square & 7) - enemyKing.File) + Math.Abs((square >> 3) - enemyKing.Rank)) * pieceValues[piece + 1] / 14 / 40;
                }
            }

            //King shield
            //middlegame += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(board.GetKingSquare(white)) & board.GetPieceBitboard(PieceType.Pawn, white)) * 10;

            endgame = -endgame;
            middlegame = -middlegame;
        }

        //foreach (bool white in stackalloc[] { true, false })
        //{
        //    if (phase < 12 && endgame * (white ? 1 : -1) > 300)
        //    {
        //        Square king = board.GetKingSquare(white), enemyKing = board.GetKingSquare(!white);
        //        int centerManhattanDistance = (enemyKing.File ^ ((enemyKing.File) - 4) >> 8) + (enemyKing.Rank ^ ((enemyKing.Rank) - 4) >> 8);
        //        int kingManhattanDistance = Math.Abs(king.File - enemyKing.File) + Math.Abs(king.Rank - enemyKing.Rank);
        //        int mopupEval = (47 * centerManhattanDistance + 16 * (14 - kingManhattanDistance)) / 10;
        //        endgame += mopupEval;
        //    }
        //    endgame = -endgame;
        //}
        if (phase > 24) phase = 24;
        return (middlegame * phase + endgame * (24 - phase)) / (board.IsWhiteToMove ? 24 : -24);
    }
    public int Search(int plyFromRoot, int plyRemaining, int alpha, int beta)
    {
        if (EndSearch || board.IsInsufficientMaterial() || board.IsRepeatedPosition() || board.IsFiftyMoveDraw()) return 0;

        bool isInCheck = board.IsInCheck();
        if (isInCheck) ++plyRemaining;

        // Lookup value from transposition table
        Entry entry = entries[TTIndex];
        int type = entry.Type,
            value = entry.Value,
            eval = 0,
            turn = board.IsWhiteToMove ? 1 : 0,
            bestEval = -100_000_000;

        if (plyFromRoot > 0 &&
            entry.Key == board.ZobristKey &&
            entry.Depth >= plyRemaining && (
            (type == 0) ||
            (type == 1 && value <= alpha) ||
            (type == 2 && value >= beta)
            ))
#if Stats
            { ++positionsLookedUp; return value; }
#else
            return value;
#endif
        type = 1;

        if (plyRemaining <= 0)
        {
            eval = Evaluate();
            if (eval >= beta)
#if Stats
            { ++cutoffs; return beta; }
#else
                return eval;
#endif
            if (eval > bestEval) bestEval = eval;
        }

        // plyRemaining > 3 ??
        if (!isInCheck && beta - alpha == 1 && plyFromRoot > 2 && plyRemaining > 0)
        {

            eval = Evaluate() - 65 * plyRemaining;
            if (eval >= beta)
#if Stats
            { ++cutoffs; return beta; }
#else
                return eval;
#endif

            if (plyRemaining >= 2)
            {
                board.ForceSkipTurn();

                eval = -Search(plyFromRoot + 1, plyRemaining - 3 - plyRemaining / 6, -beta, 1 - beta);

                board.UndoSkipTurn();

                if (eval >= beta)
#if Stats
            { ++cutoffs; return beta; }
#else
                    return eval;
#endif
            }

        }

        var moves = board.GetLegalMoves(plyRemaining <= 0);
        if (moves.Length == 0) return isInCheck ? -10000000 + plyFromRoot : eval;

        var reverseScores = new int[moves.Length];
        // Move ordering
        for (int i = 0; i < moves.Length; ++i)
        {
            Move move = moves[i];
            reverseScores[i] =
                -(move == entry.Move ? 10000000 : // hashed move
                move.IsCapture ? pieceValues[(int)move.CapturePieceType + 6] * (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 100 : 1000) - pieceValues[(int)move.MovePieceType + 6] : // captures
                move.RawValue == killerMoveA[plyFromRoot] || move.RawValue == killerMoveB[plyFromRoot] ? 8000 : // killer moves
                history[turn, move.StartSquare.Index, move.TargetSquare.Index]); // history
        }
        Array.Sort(reverseScores, moves);

        Move currentBestMove = moves[0];

        for (int i = 0; i < moves.Length; ++i)
        {
            Move move = moves[i];

            board.MakeMove(move);

            bool needsFullSearch = true;
            if (i >= 3 && plyRemaining >= 3 && !move.IsCapture)
                needsFullSearch = (eval = -Search(plyFromRoot + 1, plyRemaining - 2, -alpha - 1, -alpha)) > alpha;

            if (needsFullSearch)
                eval = -Search(plyFromRoot + 1, plyRemaining - 1, -beta, -alpha);

            board.UndoMove(move);
            if (EndSearch) return 0;

            if (eval > bestEval)
            {
                currentBestMove = move;
                bestEval = eval;
            }

            if (eval >= beta)
            {
                // Store position in Transposition Table
                entries[TTIndex] = new(board.ZobristKey, plyRemaining, bestEval, currentBestMove, 2);

                if (!move.IsCapture)
                {
                    killerMoveB[plyFromRoot] = killerMoveA[plyFromRoot];
                    killerMoveA[plyFromRoot] = move.RawValue;
                    history[turn, move.StartSquare.Index, move.TargetSquare.Index] += plyRemaining; // plyRemaining * plyRemaining ??
                }
#if Stats
                ++cutoffs;
#endif
                return eval;
            }
            if (eval > alpha)
            {
                type = 0;
                alpha = eval;
                if (plyFromRoot == 0) bestMove = currentBestMove;
            }
        }

        // Store position in Transposition Table
        entries[TTIndex] = new(board.ZobristKey, plyRemaining, bestEval, currentBestMove, type);

        return bestEval;
    }
}