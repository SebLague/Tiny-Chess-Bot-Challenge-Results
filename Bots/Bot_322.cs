namespace auto_Bot_322;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_322 : IChessBot
{
    //None, Pawn, Knight, Bishop, Rook, Queen, King
    int[] PIECE_WEIGHT = { 0, 100, 280, 320, 460, 910, 100000 };
    int[] PAWN_PUSH_BONUS = { 0, 1, 3, 10, 20, 30, 50, 0 };

    Dictionary<ulong, int> book = new()
    {
        {  13227872743731781434, 16 }, //Start position, e4
        {  15607329186585411972, 16 },  //Start position + e4, e5
        { 17664629573069429839,  11 }
    };
    class Comp : IComparer<(int, Move[])>
    {
        public int Compare((int, Move[]) x, (int, Move[]) y) => x.Item1.CompareTo(y.Item1);
    }

    static Comp comp = new();

    public Move Think(Board board, Timer timer)
    {
        Dictionary<ulong, int> evalTable = new();

        var pair = (0, Move.NullMove);
        var firstMoves = board.GetLegalMoves();

        if (book.TryGetValue(board.ZobristKey, out var index))
            return firstMoves[index];

        if (firstMoves.Length == 1)
            return firstMoves[0];


        int maxDepth = 3;
        while ((maxDepth < 4 || timer.MillisecondsElapsedThisTurn < 100 || ((float)timer.MillisecondsElapsedThisTurn / timer.MillisecondsRemaining < 0.005)) && !(Math.Abs(pair.Item1) + 10 >= 100000))
        {
            AlphaBetaNega(-10000000, 10000000, maxDepth, 1, firstMoves, false);

            maxDepth++;


            pair.Item1 *= board.IsWhiteToMove ? 1 : -1;

            if (timer.MillisecondsRemaining < 1000)
                break;
        }

        return pair.Item2;

        int AlphaBetaNega(int alpha, int beta, int depthLeft, int localEval, Move[] moves, bool quisccence)
        {
            int mCount = moves.Length;
            //Finished result || Patt
            if (mCount == 0 || localEval == 0)
                return localEval;

            if (quisccence)
            {
                if (localEval >= beta)
                    return beta;

                if (alpha < localEval)
                    alpha = localEval;

                //Finished result || Patt
                if (mCount == 0 || localEval == 0 || depthLeft == 0)
                    return localEval;
            }
            else if (depthLeft == 0)
                return AlphaBetaNega(alpha, beta, 5, localEval, moves, true);

            var evals = new (int, Move[])[mCount];

            for (int i = 0; i < mCount; i++)
            {
                board.MakeMove(moves[i]);

                evals[i] = StaticEval();

                board.UndoMove(moves[i]);
            }

            //Smallest value first
            Array.Sort(evals, moves, comp);

            for (int i = 0; i < mCount; i++)
            {
                var m = moves[i];

                if (quisccence && !m.IsCapture)
                    continue;

                board.MakeMove(m);

                var score = AlphaBetaNega(-beta, -alpha, depthLeft - 1, evals[i].Item1, evals[i].Item2, quisccence);
                score = -score;
                board.UndoMove(m);

                if (score >= beta)
                    return beta;   // fail hard beta-cutoff

                if (score > alpha)
                {
                    alpha = score; // alpha acts like max in MiniMax

                    if (!quisccence && depthLeft == maxDepth)
                        pair = (score, m);
                }
            }

            return alpha;

            //Returns positive value if next moving player is better
            (int, Move[]) StaticEval()
            {
                var legalMoves = board.GetLegalMoves();
                var factor = board.IsWhiteToMove ? 1 : -1;

                if (board.IsDraw())
                    return (0, legalMoves);

                if (board.IsInCheckmate())
                    return (-(100000 - (maxDepth - depthLeft)), legalMoves);

                if (evalTable.TryGetValue(board.ZobristKey, out var val))
                    return (val, legalMoves);


                //Whites perspective
                int sum = 0;

                var pieceList = board.GetAllPieceLists();

                sum += pieceList[0].Sum(x => PAWN_PUSH_BONUS[x.Square.Rank]);
                sum -= pieceList[6].Sum(x => PAWN_PUSH_BONUS[7 - x.Square.Rank]);

                for (int i = 0; i < 6; i++)
                {
                    var cost = PIECE_WEIGHT[i + 1];
                    sum += pieceList[i].Count * cost;
                    sum -= pieceList[i + 6].Count * cost;
                }

                if (board.TrySkipTurn())
                {
                    Span<Move> opponentMoves = stackalloc Move[218];
                    board.GetLegalMovesNonAlloc(ref opponentMoves);
                    board.UndoSkipTurn();

                    int mCount = legalMoves.Where(x => x.MovePieceType != PieceType.Queen).Count();

                    foreach (Move m in opponentMoves)
                        if (m.MovePieceType != PieceType.Queen)
                            mCount--;

                    sum += mCount * factor * 2;

                    //Center 
                    int centerScore = 0;

                    foreach (Move m in legalMoves)
                        centerScore += CenterScore(m);

                    foreach (Move m in opponentMoves)
                        centerScore -= CenterScore(m);

                    int CenterScore(Move m)
                    {
                        int index = m.TargetSquare.Index;
                        return (index >= 26 && index < 30 ||
                            index >= 34 && index < 38) &&
                            (m.MovePieceType == PieceType.Knight || m.MovePieceType == PieceType.Bishop) ? 1 : 0;
                    }

                    ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
                    ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);

                    centerScore += 3 * (
                        2 * BitboardHelper.GetNumberOfSetBits(whitePawns & 404226048) +
                        BitboardHelper.GetNumberOfSetBits(whitePawns & 606339072) -
                        2 * BitboardHelper.GetNumberOfSetBits(blackPawns & 26491358281728) -
                        BitboardHelper.GetNumberOfSetBits(blackPawns & 39737037422592));

                    sum += centerScore * 8 * factor;
                }
                else
                    sum -= 20 * factor;

                sum += CastleValue(true) - CastleValue(false);

                int CastleValue(bool white) =>
                    (board.HasKingsideCastleRight(white) || board.HasQueensideCastleRight(white)) ? 60 : 0;

                int KingPosValue(bool white) => (board.GetPieceBitboard(PieceType.King, white) & (white ? 0x44UL : 0x0000000000000044UL)) > 0 ? 1 : 0;

                sum += (KingPosValue(true) - KingPosValue(false)) * 80;

                if (pieceList.Sum(x => x.Count) < 5)
                    sum += EdgeDistance(board.GetKingSquare(true)) - EdgeDistance(board.GetKingSquare(false));

                int EdgeDistance(Square s) =>
                    Math.Min(Math.Min(s.Rank, 7 - s.Rank), Math.Min(s.File, 7 - s.File));

                sum *= factor;

                //Eval != Draw
                if (sum == 0)
                    sum = 1;

                if (evalTable.Count < 1_000_000)
                    evalTable.Add(board.ZobristKey, sum);

                return (sum, legalMoves);
            }
        }
    }
}