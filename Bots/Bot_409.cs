namespace auto_Bot_409;
using ChessChallenge.API;
using System;

public class Bot_409 : IChessBot
{
    const int CHECKMATE = 100000;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 1000 };

    const int ttSize = (1 << 20);
    Transposition[] tt = new Transposition[ttSize];

    ulong[] masks = { 217020518514230019, 506381209866536711, 1012762419733073422, 2025524839466146844,
                            4051049678932293688, 8102099357864587376, 16204198715729174752, 13889313184910721216 };

    Move[,] killers;

    public Move Think(Board board, Timer timer)
    {
        int mult = -1;
        if (board.IsWhiteToMove)
        {
            mult = 1;
        }

        int maxDepth = 1;
        int absMax = 15;

        int increment = timer.IncrementMilliseconds;
        double startTime = timer.GameStartTimeMilliseconds;
        double timeLeft = timer.MillisecondsRemaining;

        double moveTime = Math.Min(Math.Max(.95 * increment, .9 * increment + timeLeft / 30), startTime / 50 + increment);

        //double moveTime = timer.MillisecondsRemaining / 30;

        bool timedOut = false;
        bool mateFound = false;

        Move moveToPlay = board.GetLegalMoves()[0];
        Move prevBest = new Move();

        int positionsSearched = 0;

        int NegaMax(Board board, int depth, int turnMult, int alpha, int beta, int ply)
        {
            if (timer.MillisecondsElapsedThisTurn > moveTime)
            {
                timedOut = true;
                return beta;
            }

            bool quiescing = depth <= 0;

            positionsSearched++;

            if (ply > 0 && board.IsRepeatedPosition())
                return 0;

            ulong boardKey = board.ZobristKey;
            Transposition entry = tt[boardKey % ttSize];

            if (entry.key == boardKey && entry.depth == depth &&
                (
                    entry.kind == 1 ||
                    entry.kind == 2 && entry.score >= beta ||
                    entry.kind == 3 && entry.score <= alpha
                )
            ) return entry.score;

            int maxScore = -CHECKMATE;

            if (quiescing)
            {
                maxScore = turnMult * Evaluate(board);

                if (maxScore >= beta) return maxScore;

                alpha = Math.Max(alpha, maxScore);
            }

            Move[] allMoves = board.GetLegalMoves(quiescing);

            if (!quiescing && allMoves.Length == 0) return board.IsInCheck() ? -CHECKMATE : 0;

            OrderMoves(allMoves, prevBest, entry.move, ply, quiescing);

            int intlAlpha = alpha;
            Move thisDepthBest = Move.NullMove;

            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                int score = -NegaMax(board, depth - 1, -turnMult, -beta, -alpha, ply + 1);
                board.UndoMove(move);

                if (score > maxScore)
                {
                    maxScore = score;
                    thisDepthBest = move;
                    if (depth == maxDepth && !timedOut)
                    {
                        moveToPlay = move;
                        if (score == CHECKMATE)
                        {
                            mateFound = true;
                        }
                    }
                }

                alpha = Math.Max(alpha, maxScore);

                if (alpha >= beta)
                {
                    if (!(move.IsCapture || move.IsPromotion || move.Equals(killers[ply, 0])))
                    {
                        killers[ply, 1] = killers[ply, 0];
                        killers[ply, 0] = move;
                    }

                    break;
                }
            }

            // add to transposition table
            entry.key = boardKey;
            entry.move = thisDepthBest;
            entry.depth = depth;
            entry.score = maxScore;
            if (maxScore < intlAlpha)
                entry.kind = 3;
            else if (maxScore >= beta)
            {
                entry.kind = 2;
            }
            else entry.kind = 1;
            tt[boardKey % ttSize] = entry;

            return maxScore;
        }

        do
        {
            killers = new Move[maxDepth, 2];
            int result = NegaMax(board, maxDepth, mult, -CHECKMATE, CHECKMATE, 0);
            if (!timedOut)
            {
                prevBest = moveToPlay;
            }

            maxDepth++;
            //DivertedConsole.Write(killers);
        } while (!timedOut && !mateFound && maxDepth < absMax);

        return moveToPlay;
    }

    struct Transposition
    {
        public ulong key;
        public Move move;
        public int depth, score, kind;
    }

    int Evaluate(Board board)
    {
        int score = 0;

        PieceList[] piecesOnBoard = board.GetAllPieceLists();

        foreach (bool side in new[] { true, false })
        {
            int mult = side ? 1 : -1;
            int bonusInd = side ? 0 : 6;

            ulong enemyPawns = board.GetPieceBitboard((PieceType)1, !side);
            Square enemyKingSquare = board.GetKingSquare(!side);

            if (board.PlyCount < 70)
            {
                score -= mult * 5 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(board.GetKingSquare(!side)) & enemyPawns & 65020719620220672);
                score += enemyKingSquare.Rank == (side ? 7 : 0) ? 0 : 4;
            }

            foreach (Piece pawn in piecesOnBoard[bonusInd])
            {
                Square pawnSquare = pawn.Square;
                int pawnRank = pawnSquare.Rank;
                ulong passerMask = masks[pawnSquare.File] & (side ? ulong.MaxValue << 8 * (pawnRank + 1) : ulong.MaxValue >> 8 * (8 - pawnRank));
                score += mult * ((passerMask & enemyPawns) == 0 ? 100 + 10 * (side ? pawnRank : 8 - pawnRank) : 100);
            }

            for (int i = 1; i < 6; i++)
            {
                PieceList pieces = piecesOnBoard[i + bonusInd];
                int value = pieceValues[i + 1];

                foreach (Piece piece in pieces) score += mult * (BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks((PieceType)i + 1, piece.Square, board, side)) + value);
            }
        }

        return score;
    }

    void OrderMoves(Move[] moveList, Move prevBest, Move thisEntry, int ply, bool quiescing)
    {
        int numMoves = moveList.Length;
        int[] moveScores = new int[numMoves];
        Random random = new Random();

        for (int i = 0; i < numMoves; i++)
        {
            int score = 0;
            Move move = moveList[i];

            // iterative deepening -- always start search with previous best
            score += move.Equals(prevBest) ? 10000 : 0;

            score += move.Equals(thisEntry) ? 1000 : 0;

            score += move.IsPromotion ? 900 : 0;

            if (!quiescing)
            {
                score += move.Equals(killers[ply, 0]) ? 285 : 0;
                score += move.Equals(killers[ply, 1]) ? 280 : 0;
            }

            if (move.IsCapture)
            {
                score += pieceValues[(int)move.CapturePieceType] - (pieceValues[(int)move.MovePieceType] / 10);
            }
            else
            {
                score += random.Next(0, 10);
            }

            moveScores[i] = -score;
        }

        Array.Sort(moveScores, moveList);
    }
}