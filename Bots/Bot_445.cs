namespace auto_Bot_445;
using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

public class Bot_445 : IChessBot
{
    private int INF = 1000000;
    private Move nullMove = Move.NullMove;
    private const int transpositionEntries = 8446717;
    private TimeoutException times_up = new TimeoutException();

    private (ulong, int)[] Transpositions = new (ulong, int)[transpositionEntries];

    private Board board;
    private Timer timer;

    private int depth;
    private int time_promille;

    private Move bestfoundMove;
    private int bestfoundScore;

    private readonly int[,] unpackedPST = new int[12, 64];
    private readonly int[] gamephaseInc = { 0, 1, 1, 2, 4, 0 };

    // middle/end game: Pawn, Knight, Bishop, Rook, Queen, King
    private readonly int[] pieceVal = { 70, 260, 285, 475, 930, -30, 95, 255, 310, 505, 980, -40 };
    private readonly decimal[] packedPST = // https://github.com/dorinon/Chess-Pst-Quantization and  https://www.chessprogramming.org/Simplified_Evaluation_Function
        { 23611833821723696824345m,  6201813252365462050828456985m,  9296663350578912738160153625m,
13944983126997161936380236825m,  13944983126997161936380236825m,  9296663350578912738160153625m,
6201813252365462050828456985m,  23611833821723696824345m,  6201836864220394397359150155m,
9308799926134463267237402965m,  13951075073449009798742682975m,  15498500122555735142366588255m,
15498500122555735142366588255m,  13951075073449009798742682975m,  9308799926134463267237408085m,
6201836864220394397359150155m,  9296639738763386888117621815m,  13951027849784181101451685185m,
15498452991479285305753611595m,  18593303089692735993001422155m,  18593303089692735993001424715m,
15498452991479285305753617995m,  13951027849784181101451685185m,  9296639738763386888117621815m,
13944959515173191837036385310m,  15498452898883869570658154275m,  18593303089688513868352075560m,
20140728323983255889450776370m,  20140728323983255889450776370m,  18593303089688513868352075560m,
15498452898883869570658152995m,  13944959515173191837036385310m,  13944959515168969733860561945m,
15498452898879647446007819545m,  18593303089685699118584968985m,  20140728323980441139683669810m,
20140728323980441139683669810m,  18593303089685699118584967710m,  15498452898879647446007820825m,
13944959515168969712385725440m,  9296639738747905764398863385m,  13951027849770107374091969305m,
15498452991470841056453278745m,  18593303089684291743700759070m,  18593303089684291743700759070m,
15498452991470841056453278745m,  13951027849770107352616810280m,  9296639738747905764398863375m,
6201789640531673312397890585m,  9308752702449969804317825305m,  13951027849770118369207917593m,
15498452898876843691608322575m,  15498452898876843691608322560m,  13951027849770118347901579811m,
9308752702449975301875969335m,  6201789640531684307514168330m,  23611833843713426063385m,
6201813252365500533063685145m,  9296663350578951220647367705m,  13945006738829587279638696985m,
13945006738829614767429391385m,  9296663350578923733108331545m,  6201813252365517025738101785m,
23611833871201216757785m };

    public Bot_445()
    {
        // https://github.com/dorinon/Chess-Pst-Quantization (optimized for tokens)

        for (int j = 0; j < 12; ++j)
            for (int i = 0; i < 64; ++i)
                unpackedPST[j, i] = (int)(((BigInteger)packedPST[i] >> (j * 8)) & 255);
    }

    private int BlIsDo(bool white) // check for blocked, isolated and doubled pawns
    {
        int blocked = 0, isolated = 0, doubled = 0;
        int[] file_occupation = new int[8];

        foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, white))
        {
            ++file_occupation[pawn.Square.File];

            blocked += (board.AllPiecesBitboard & ((ulong)1 << pawn.Square.Index + (white ? 8 : -8))) == 0 ? 0 : 1;
        }

        for (int file = 0; file < 8; ++file)
        {
            if (file_occupation[file] > 0)
            {
                isolated += (file > 0 ? file_occupation[file - 1] == 0 : true) && (file < 7 ? file_occupation[file + 1] == 0 : true) ? file_occupation[file] : 0;

                doubled += file_occupation[file] - 1;
            }
        }

        return blocked + 2 * isolated + doubled;
    }

    private int material(bool white) // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    {
        int gamephase = 0, mgScore = 0, egScore = 0;

        for (int pstIdx = 0; pstIdx <= 5; ++pstIdx)
        {
            ulong pcs = board.GetPieceBitboard((PieceType)pstIdx + 1, white);

            while (pcs != 0)
            {
                int sqIdx = BitboardHelper.ClearAndGetIndexOfLSB(ref pcs) ^ (white ? 56 : 0);

                mgScore += (pieceVal[pstIdx] + unpackedPST[pstIdx, sqIdx]);
                egScore += (pieceVal[pstIdx + 6] + unpackedPST[pstIdx + 6, sqIdx]);

                gamephase += gamephaseInc[pstIdx];
            }
        }



        int mgPhase = gamephase > 24 ? 24 : gamephase;
        int egPhase = 24 - mgPhase;

        return (int)((mgScore * mgPhase + egScore * egPhase) / 24.0);
    }

    // https://www.chessprogramming.org/NegaScout und https://www.chessprogramming.org/Quiescence_Search
    private int NegAlphaBeta(int alpha, int beta, int depthleft, bool quiet = false, bool selection = false)
    {
        /* Check if its time to stop searching */
        time_promille = 1000 * timer.MillisecondsElapsedThisTurn / (timer.MillisecondsRemaining + 1);


        if (depth > 6)
        {
            if (time_promille >= 10)
                throw times_up;
        }
        else if (depth > 4)
        {
            if (time_promille >= 15)
                throw times_up;
        }
        else if (depth > 2)
        {
            if (time_promille >= 30)
                throw times_up;
        }
        else if (depth > 1)
        {
            if (time_promille >= 50)
                throw times_up;
        }


        Move[] legalMoves = board.GetLegalMoves();
        bool is_in_check = board.IsInCheck();

        bool white2move = board.IsWhiteToMove;
        int legalMoveCount = legalMoves.Length;


        /* Check if board is in terminal position */
        if (!selection)
        {
            if (is_in_check && (legalMoveCount == 0)) // checkmate
                return -INF + board.PlyCount;


            else if (board.IsFiftyMoveDraw() ||
                board.IsInsufficientMaterial() ||
                (legalMoveCount == 0) ||
                board.IsRepeatedPosition()) // draw by: 50 moves, insufficent material, stalemate, repetition

                return 0;

        }

        if (quiet) // Quiet search activities
        {
            int stand_pat;
            ulong hash = board.ZobristKey;
            (ulong entry_hash, int entry_eval) = Transpositions[hash % transpositionEntries];

            if (entry_hash != hash)
            {
                int mobility = legalMoveCount; // 1 point for every legal move we have

                if (board.TrySkipTurn())
                {
                    mobility -= board.GetLegalMoves().Count(); // -1 point for every legal move our opponent would have if it was his turn
                    board.UndoSkipTurn();
                }

                stand_pat = 2 * mobility + (white2move ? 1 : -1) *
                    (material(true) - material(false)
                    + 5 * (BlIsDo(false) - BlIsDo(true)));

                Transpositions[hash % transpositionEntries] = (hash, stand_pat);

            }
            else
                stand_pat = entry_eval;

            if (stand_pat >= beta) return beta;
            if (depthleft <= 0) return stand_pat;
            if (stand_pat > alpha) alpha = stand_pat;

        }
        else if (depthleft <= 0) // End of normal search reached, go to quiet search
            return NegAlphaBeta(alpha, beta, 8, true, false);

        // Determine board eval by searching possible following board states
        foreach (Move move in selection && (bestfoundMove != nullMove) ? legalMoves.Select(move => move == bestfoundMove ? legalMoves.First() : move == legalMoves.First() ? bestfoundMove : move) : legalMoves)
        {
            // check only captures, promotions, check evasions for quiet search, or when its not much work. Search all moves otherwise
            if (!quiet || is_in_check || move.IsCapture || move.IsPromotion || (legalMoveCount <= 3))
            {
                board.MakeMove(move);

                int score = -NegAlphaBeta(-beta, -alpha, depthleft - 1, quiet, false);

                board.UndoMove(move);

                if (selection)
                {
                    if ((score > bestfoundScore) || (bestfoundMove == nullMove))
                    {
                        bestfoundScore = score;
                        bestfoundMove = move;
                    }
                }

                if (score >= beta) // cut-off
                    return beta;

                if (score > alpha) // better move found
                    alpha = score;
            }
        }

        return alpha;

    }

    public Move Think(Board evalBoard, Timer moveTimer)
    {
        board = evalBoard;
        timer = moveTimer;

        depth = 1;
        bestfoundMove = nullMove;

        while (true)
        {
            try
            {
                bestfoundScore = -INF;
                NegAlphaBeta(-INF, INF, depth, false, true);
                ++depth; // https://www.chessprogramming.org/Iterative_Deepening
            }
            catch (TimeoutException) // time is up! we finally decide on a move
            {
                return bestfoundMove;
            }
        }
    }
}
