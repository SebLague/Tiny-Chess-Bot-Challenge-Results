namespace auto_Bot_264;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_264 : IChessBot
{

    Timer timer;
    Board board;

    Move bestRootMove = Move.NullMove;


    int[] pieceValues_mg = { 0, 82, 337, 365, 477, 1025, 10000 };
    int[] pieceValues_eg = { 0, 94, 281, 297, 512, 936, 10000 };
    int[] phaseValues = { 0, 0, 3, 3, 5, 10, 0 };
    ulong[] pesto_encoded_5bit = { 814929927758887374, 451049958341527348, 556700758272654829, 488023096226627055, 520674162454704587, 195013036481133006, 704367403400221032, 595198166125073205, 557791509274410445, 447561101476282864, 404704267523696005, 378990024618065260, 667041219135195693, 521838680527552049, 521835348741307886, 445202580448429583, 706555605354628689, 562474466734064277, 519474591010667948, 412587801325223343, 556665435385672106, 669439287141740015, 557792815041952108, 520709376756533872, 519547228468458893, 520710512807722446, 485836061232575918, 445237798111787369, 447419199145818637, 373036099324720524, 339439457392734637, 558885419221499306, 932136767768181198, 780903555366089530, 484681750185197073, 484645430901553581, 520674193528307183, 226740615022852558, 557756217390739884, 485808785035997614, 556700791557602765, 374236870109180367, 483447886576005452, 484610213208798637, 557864109218412974, 484681784544934383, 519512008664006093, 484609144801080750, 557865209804866031, 520674228995309006, 521800127793806798, 483483247077406158, 520673127266990542, 593930360532122030, 665948304512173581, 634501308806054450, 557757528601608717, 409138632277310959, 483407275447628171, 559061512813459949, 595052929762181615, 485843969408188976, 556665608260304332, 374269819989965263 };

    struct TranspositionTableEntry
    {
        public ulong hashKey;
        public int score, depth, nodeType;
        public Move move;
    }

    const int TTSize = 4194303;
    TranspositionTableEntry[] table = new TranspositionTableEntry[4194304];

    Move[] killerMoves = new Move[1024];
    int[,,] historyHeuristics;

    public int Evaluate(Board board)
    {
        int material_mg = 0, material_eg = 0, phase = 0;

        foreach (int side in new[] { 1, -1 })
        {
            bool isWhite = side == 1;

            for (PieceType type = PieceType.Pawn; type <= PieceType.King; type++)
            {
                ulong pieceBitBoard = board.GetPieceBitboard(type, isWhite);
                while (pieceBitBoard != 0)
                {
                    int SquareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard);
                    int pieceType = (int)type;
                    int index = (pieceType - 1) * 64 + SquareIndex ^ (isWhite ? 56 : 0);
                    material_mg += (pieceValues_mg[pieceType] + GetTableValue_5bit(index)) * side;
                    material_eg += (pieceValues_eg[pieceType] + GetTableValue_5bit(index + 384)) * side;
                    phase += phaseValues[pieceType];
                }
            }

        }

        return (material_mg * phase + material_eg * (64 - phase)) / 64 * (board.IsWhiteToMove ? 1 : -1);
    }

    public int GetTableValue_5bit(int index) => ((int)((pesto_encoded_5bit[index / 12] >> index * 5 % 60) & 0x1F)) * 12 - 168;

    public int Search(int ply, int depth, int alpha, int beta, bool allowNullMove = true)
    {

        bool qSearch = depth <= 0,
            notRoot = ply > 0,
            inCheck = board.IsInCheck();
        int bestScore = -50_000_000,
            score,
            score0,
            movesSearched = 0,
            originalAlpha = alpha;

        if (notRoot && board.IsRepeatedPosition())
            return 0;

        if (inCheck)
            depth++;

        ulong hashKey = board.ZobristKey;
        TranspositionTableEntry entry = table[hashKey & TTSize];

        if (notRoot && entry.hashKey == hashKey && entry.depth >= depth && (
                    entry.nodeType == 3
                || entry.nodeType == 2 && entry.score >= beta
                || entry.nodeType == 1 && entry.score <= alpha
        )) return entry.score;

        score0 = Evaluate(board);

        if (qSearch)
        {
            bestScore = score0;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }
        else if (alpha == beta - 1 && !inCheck)
        {

            if (depth < 8 && score0 - 100 * depth >= beta)
                return score0;

            if (depth >= 2 && allowNullMove)
                if (board.TrySkipTurn())
                {
                    score = -Search(ply + 1, depth - 4, -beta, -beta + 1, false);
                    board.UndoSkipTurn();

                    if (score >= beta)
                        return score;
                }

        }

        Move[] moves = board.GetLegalMoves(qSearch);

        Move bestMove = Move.NullMove;

        Array.Sort(
        moves.Select(move =>
            move.Equals(entry.move) ? 9_000_000 :
            move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
            move.Equals(killerMoves[ply]) ? 900_000 :
            historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]).ToArray(),
            moves);
        Array.Reverse(moves);
        foreach (Move move in moves)
        {

            bool ZWS = movesSearched++ > 0 && !qSearch;
            if (ZWS && depth < 6 && score0 + 150 * depth <= alpha)
                continue;

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                return 50_000_000;

            board.MakeMove(move);

            score = -Search(ply + 1, depth - (ZWS && depth >= 3 && !(move.IsCapture || move.IsPromotion || board.IsInCheck() || inCheck) ? 3 : 1), ZWS ? -alpha - 1 : -beta, -alpha);
            score = ZWS && score > alpha ? -Search(ply + 1, depth - 1, -beta, -alpha) : score;

            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (!notRoot) bestRootMove = move;

                alpha = Math.Max(alpha, score);

                if (score >= beta)
                {
                    if (!move.IsCapture)
                    {
                        killerMoves[ply] = bestMove;
                        historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;
                    }
                    break;
                }

            }
        }

        if (!qSearch && moves.Length == 0) return inCheck ? -50_000_000 + ply : 0;

        table[hashKey & TTSize] = new TranspositionTableEntry { hashKey = hashKey, score = bestScore, depth = depth, nodeType = bestScore >= beta ? 2 : bestScore > originalAlpha ? 3 : 1, move = bestMove };

        return bestScore;
    }



    public Move Think(Board _board, Timer _timer)
    {
        board = _board;
        timer = _timer;

        historyHeuristics = new int[2, 7, 64];

        bestRootMove = board.GetLegalMoves()[0];

        for (int depth = 1, alpha = -50_000_000, beta = 50_000_000, score; ;)
        {
            score = Search(0, depth, alpha, beta);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30)
                break;

            if (score <= alpha)
                alpha -= 75;
            else if (score >= beta)
                beta += 75;
            else
            {
                alpha = score - 25;
                beta = score + 25;
                depth++;
            }

        }

        return bestRootMove;
    }
}
