namespace auto_Bot_521;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_521 : IChessBot
{
    private int[] pieceValues = { 126, 781, 825, 1276, 2538, 0, -126, -781, -825, -1276, -2538, 0, 208, 854, 915, 1380, 2682, 0, -208, -854, -915, -1380, -2682, 0 };
    private readonly int MAX_VALUE = 2147483647;
    private int millisForThisMove;
    private Timer globalTimer;
    private int currentDepth = 1;
    private Move bestMove;
    private (ulong, Move, int, int, int)[] tranpositionTable = new (ulong, Move, int, int, int)[1000000];

    private int[][] decompressed = new[]
    {
        200900000872180000040502m, 200033110028182519001746m, 201831170072184114032786m, 201921260990186932044877m,
        201921260990186932044877m, 201831170072184114032786m, 200033110028182519001746m, 200900000872180000040502m,
        221526100279102314012054m, 245143181004123631044399m, 316550231335277228125299m, 387740371780239841117099m,
        367740371780349841117099m, 416550231335247228125299m, 295143181004123631044399m, 171526100279002314012054m,
        113132060296095029193589m, 57552201159116339055699m, 319833301870088239116599m, 359949341221239947077799m,
        519949341221209947077799m, 439833301870218239116599m, 267552201159106339055699m, 3132060296135029193589m,
        175733180965255526075199m, 9945261091198836147199m, 289955271439109940048799m, 399964251366169952209899m,
        599964251366049952209899m, 379955271439059940048799m, 229945261091078836147199m, 155733180965125526075199m,
        315829040555304528084597m, 169957161980247439216899m, 99952271706209930208399m, 229959341066129951079599m,
        319959341066139951079599m, 209952271706149930208399m, 89957161980327439216899m, 255829040555274528084597m,
        238326090124453919193693m, 99941291546364644145699m, 149938371182377443066399m, 429945431382479944237599m,
        129945431382489944237599m, 159938371182277443066399m, 69941291546264644145699m, 98326090124323919193693m,
        132525290089172118172448m, 266527431166044026184799m, 189641471566313939335099m, 99937491313409941086699m,
        249937491313429941086699m, 69641471566353939335099m, 306527431166254026184799m, 112525290089252118172448m,
        200903140360180008310012m, 200938120390180211132260m, 203627300646183414323174m, 206621400300187323264079m,
        206621400300187323264079m, 203627300646183414323174m, 200938120390180211132260m, 200903140360180008310012m,
    }.Select(value => value.ToString().PadLeft(24, '0')).Select(value => Enumerable.Range(0, 12).Select(i => Int32.Parse(value.Substring(i * 2, 2))).ToArray()).ToArray();

    public Move Think(Board board, Timer timer)
    {
        millisForThisMove = timer.MillisecondsRemaining / 40 + timer.IncrementMilliseconds;
        if (millisForThisMove < 10) return SortMoves(board.GetLegalMoves(), Move.NullMove)[0];

        globalTimer = timer;
        currentDepth = 1;
        bestMove = board.GetLegalMoves()[0];

        while (timer.MillisecondsElapsedThisTurn < millisForThisMove)
        {
            Search(board, currentDepth, -MAX_VALUE, MAX_VALUE, board.IsWhiteToMove ? 1 : -1);
            currentDepth++;
        }

        return bestMove;
    }

    private int Search(Board board, int depth, int alpha, int beta, int color)
    {
        var (ttKey, ttMove, ttValue, ttDepth, ttFlag) = tranpositionTable[board.ZobristKey % 1000000];
        if (ttKey == board.ZobristKey && ttDepth >= depth && currentDepth != depth && Math.Abs(ttValue) < 50000 && (ttFlag == 1 || ttFlag == 2 && ttValue <= alpha || ttFlag == 3 && ttValue >= beta)) return ttValue;

        if (board.IsDraw() || board.IsInCheckmate() || globalTimer.MillisecondsElapsedThisTurn > millisForThisMove) return color * Evaluate(board);

        bool isQuiescenceSearch = depth <= 0;
        int bestValue = -MAX_VALUE;
        Move bestLocalMove = Move.NullMove;
        ttFlag = 2;

        if (isQuiescenceSearch)
        {
            bestValue = color * Evaluate(board);
            if (bestValue >= beta) return bestValue;
            alpha = Math.Max(alpha, bestValue);
        }

        Move[] legalMoves = SortMoves(board.GetLegalMoves(isQuiescenceSearch && !board.IsInCheck()), ttMove);

        foreach (Move m in legalMoves)
        {
            board.MakeMove(m);
            int currentValue = -Search(board, depth - 1, legalMoves[0] == m || isQuiescenceSearch ? -beta : -alpha - 1, -alpha, -color);
            if (alpha < currentValue) currentValue = -Search(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(m);

            if (currentValue > bestValue)
            {
                bestValue = currentValue;
                if (currentValue > alpha)
                {
                    ttFlag = 1;
                    alpha = currentValue;
                    bestLocalMove = m;
                }

                if (alpha >= beta)
                {
                    ttFlag = 3;
                    break;
                }
            }
        }

        if (currentDepth == depth && globalTimer.MillisecondsElapsedThisTurn < millisForThisMove) bestMove = bestLocalMove;

        tranpositionTable[board.ZobristKey % 1000000] = (board.ZobristKey, bestLocalMove, bestValue, depth, ttFlag);

        return bestValue;
    }

    private int Evaluate(Board board)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -60000 + board.PlyCount : 60000 - board.PlyCount;
        if (board.IsDraw()) return board.IsWhiteToMove ? -300 : 300;
        int midGameEval = 0, endGameEval = 0, phase = 24;
        int[] phaseValues = { 0, 0, 1, 1, 2, 4, 0 };

        for (int i = 0; i < 12; i++)
        {
            midGameEval += board.GetAllPieceLists()[i].Count * pieceValues[i];
            endGameEval += board.GetAllPieceLists()[i].Count * pieceValues[i + 12];
        }

        for (int index = 0; index < 64; index++)
        {
            Piece current = board.GetPiece(new Square(index));
            phase -= phaseValues[(int)current.PieceType];
            if (current.PieceType == PieceType.None) continue;
            int pstIndex = board.IsWhiteToMove ? index : (7 - index / 8) * 8 + index % 8;
            midGameEval += decompressed[pstIndex][(int)current.PieceType - 1] * (current.IsWhite ? 1 : -1);
            endGameEval += decompressed[pstIndex][(int)current.PieceType + 5] * (current.IsWhite ? 1 : -1);
        }

        phase = (phase * 256 + 12) / 24;
        return (midGameEval * (256 - phase) + endGameEval * phase) / 256;
    }

    private Move[] SortMoves(Move[] moves, Move pvMove) => moves.Length > 0 ? moves.Select(m => (m, m.Equals(pvMove) ? 1000 : m.CapturePieceType == PieceType.None ? -50 : pieceValues[(int)m.CapturePieceType - 1] - pieceValues[(int)m.MovePieceType - 1])).OrderByDescending(t => t.Item2).Select(t => t.Item1).ToArray() : Array.Empty<Move>();
}
