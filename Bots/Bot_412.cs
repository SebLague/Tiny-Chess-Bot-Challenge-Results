namespace auto_Bot_412;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_412 : IChessBot
{
    bool followPV, scorePV;
    int plyOut;
    int[] maxAttack = { 1, 2, 8, 16, 16, 32, 8 }, probabilities, pieceValues = { 0, 1, 3, 3, 5, 9, 100 }, vicScore = { 0, 100, 200, 300, 400, 500, 600 }, pvLength = new int[64];
    int[,] historyMoves = new int[12, 64];
    Move lastMove, bestMove;
    Move[,] killerMoves = new Move[2, 64], pvTable = new Move[64, 64];

    public Move Think(Board board, Timer timer)
    {
        followPV = false;
        scorePV = false;
        Array.Clear(killerMoves, 0, killerMoves.Length);
        Array.Clear(historyMoves, 0, historyMoves.Length);
        Array.Clear(pvTable, 0, pvTable.Length);
        Array.Clear(pvLength, 0, pvLength.Length);
        for (int itdeep = 1; itdeep < 64; itdeep++)
        {
            followPV = true;
            alphaBeta(board, float.NegativeInfinity, float.PositiveInfinity, itdeep, timer, 0);
            lastMove = bestMove;
            if (timer.MillisecondsElapsedThisTurn > (timer.MillisecondsRemaining / 20))
                break;

        }
        if (!bestMove.IsNull)
            return bestMove;
        return board.GetLegalMoves()[0];
    }

    float alphaBeta(Board board, float alpha, float beta, int depth, Timer timer, int plyCount)
    {
        Move[] moves = board.GetLegalMoves();
        pvLength[plyCount] = plyCount;
        plyOut = plyCount;
        moveOrdering(moves, board);
        float bestscore = float.NegativeInfinity;

        if (board.IsInCheckmate())
            return -1000000 + plyCount;

        else if (board.IsDraw())
            return 0;

        if (depth == 0)
            return quiesce(alpha, beta, board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float eval = -alphaBeta(board, -beta, -alpha, depth - 1, timer, plyCount + 1);
            board.UndoMove(move);

            if (eval >= beta)
            {
                if (!board.GetLegalMoves(true).Contains(move))
                {
                    killerMoves[1, plyCount] = killerMoves[0, plyCount];
                    killerMoves[0, plyCount] = move;
                }
                return eval;
            }
            if (eval > bestscore)
            {
                bestscore = eval;

                if (plyCount == 0)
                    bestMove = move;

                if (eval > alpha)
                {
                    if (!board.GetLegalMoves(true).Contains(move))
                        historyMoves[(int)board.GetPiece(move.StartSquare).PieceType, move.TargetSquare.Index] += depth;

                    alpha = eval;
                    pvTable[plyCount, plyCount] = move;

                    for (int next_ply = plyCount + 1; next_ply < pvLength[plyCount + 1]; next_ply++)
                        pvTable[plyCount, next_ply] = pvTable[plyCount + 1, next_ply];

                    pvLength[plyCount] = pvLength[plyCount + 1];
                }
            }
            if (timer.MillisecondsElapsedThisTurn > (timer.MillisecondsRemaining / 20))
            {
                bestMove = lastMove;
                break;
            }
        }
        return bestscore;
    }
    float quiesce(float alpha, float beta, Board board)
    {
        Move[] moves = board.GetLegalMoves(true);
        moveOrdering(moves, board);
        float evaluation = evaluate(board);
        if (evaluation >= beta)
            return beta;
        if (alpha < evaluation)
            alpha = evaluation;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            evaluation = -quiesce(-beta, -alpha, board);
            board.UndoMove(move);

            if (evaluation >= beta)
                return beta;
            if (evaluation > alpha)
                alpha = evaluation;
        }
        return alpha;
    }
    float evaluate(Board board)
    {
        float evaluation = 0;
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Square square = new Square(i, j);
                Piece piece = board.GetPiece(square);
                int activity = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(piece.PieceType, square, board, piece.IsWhite));
                evaluation += (pieceValues[(int)piece.PieceType] + 0.1f * activity / (maxAttack[(int)piece.PieceType])) * (piece.IsWhite ? 1 : -1);
            }
        }
        return (evaluation) * (board.IsWhiteToMove ? 1 : -1);
    }
    int MvvLva(int Attacker, int Victim)
    {
        int score = 0;
        score = vicScore[Victim] + 6 - (vicScore[Attacker] / 100);
        return score;
    }
    void enablePvProbs(Move[] moves)
    {
        followPV = false;
        for (int i = 0; i < moves.Length; i++)
        {
            if (pvTable[0, plyOut] == moves[i])
            {
                scorePV = true;
                followPV = false;
            }
        }
    }
    void moveOrdering(Move[] moves, Board board)
    {
        int index = 0;
        probabilities = new int[218];
        foreach (Move move in moves)
        {
            int probability = 0;
            int movePiece = (int)board.GetPiece(move.StartSquare).PieceType;
            int capturedPiece = (int)board.GetPiece(move.TargetSquare).PieceType;
            if (followPV)
                enablePvProbs(moves);
            if (scorePV)
            {
                if (pvTable[0, plyOut] == move)
                {
                    scorePV = false;
                    probability += 20000;
                }
            }
            if (capturedPiece != 0)
                probability += MvvLva(movePiece, capturedPiece) + 10000;
            else
            {
                if (killerMoves[0, plyOut] == move)
                    probability += 9000;

                else if (killerMoves[1, plyOut] == move)
                    probability += 8000;

                else
                    probability += historyMoves[movePiece, move.TargetSquare.Index];
            }
            probabilities[index] = probability;
            index++;
        }
        for (int i = 0; i < moves.Length; i++)
        {
            for (int j = 0; j < moves.Length; j++)
            {
                if (probabilities[i] > probabilities[j])
                {
                    (probabilities[i], probabilities[j]) = (probabilities[j], probabilities[i]);
                    (moves[i], moves[j]) = (moves[j], moves[i]);
                }
            }
        }
    }
}