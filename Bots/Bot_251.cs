namespace auto_Bot_251;
using ChessChallenge.API;
using System.Collections.Generic;
using static System.Math;

public class Bot_251 : IChessBot
{
    double[] PieceValues = { 1, 2.95, 3.25, 5, 9, 0.0/*king*/, -1, -2.95, -3.25, -5, -9, -0.0/*king*/}, AggressionWeights = { 0.0/*nullpiece*/, -0.2, 4, 2, 3, 2.25, -0.4, 2, 2.6, 1.5, 1.75, 1.2, -0.4 };
    double MaxPawnAdvancement = 0.04, Aggression = 0.175, Activity = 0.002, HavingPawns = 2.5, pawnAdvancement = 0.02, Centralistaion = 0.055, miv = double.MinValue, mav = double.MaxValue;

    int ttt;
    Timer timer_;
    int depth_;
    public Move Think(Board board, Timer timer)
    {
        depth_ = 0;

        timer_ = timer;

        var moves = new List<Move>(board.GetLegalMoves());

        ttt = (int)(((double)moves.Count / 12.0) * ((double)timer.MillisecondsRemaining / 60.0)) + (timer.MillisecondsRemaining - timer.OpponentMillisecondsRemaining) / 2;

        Move bestMove = moves[0];
        double bestScore = board.IsWhiteToMove ? -900 : 900;

        for (; timer.MillisecondsElapsedThisTurn < ttt && Abs(bestScore) < 999 && moves.Count > 1; ++depth_)
        {
            Move bestMove_ = moves[0];
            double bestScore_ = board.IsWhiteToMove ? miv : mav;


            for (int i = moves.Count - 1; i >= 0 && !ExitCalculations(); --i)
            {
                Move move = moves[i];
                board.MakeMove(move);
                double res = Search(board, depth_, miv, mav, 1, board.IsInCheck());

                board.UndoMove(move);

                if (board.IsWhiteToMove)
                {
                    if (res > bestScore_)
                    {
                        bestScore_ = res;
                        bestMove_ = move;
                    }

                }
                else
                {
                    if (res < bestScore_)
                    {
                        bestScore_ = res;
                        bestMove_ = move;
                    }
                }
            }

            if (ExitCalculations())
                break;

            bestMove = bestMove_;
            bestScore = bestScore_;
        }

        //DivertedConsole.Write("Best: " + bestMove.ToString() + ", Eval: " + bestScore + ", Depth: " + depth_ + ", Time used: " + timer_.MillisecondsElapsedThisTurn);
        return bestMove;
    }

    bool ExitCalculations() => (timer_.MillisecondsElapsedThisTurn > ttt && (timer_.MillisecondsRemaining < (timer_.GameStartTimeMilliseconds / 60) || depth_ > 0));

    double Eval(Board board)
    {
        var pls = board.GetAllPieceLists();

        double res = 0.0, maxRank = 0, pawnAdvancementSum = 0, wd = 0, bd = 0;

        int i = 0;
        foreach (var pl in pls)
            res += PieceValues[i++] * pl.Count;

        if (board.TrySkipTurn())
        {
            double movesnext = board.GetLegalMoves().Length;

            board.UndoSkipTurn();

            double movesnow = board.GetLegalMoves().Length;

            res += Activity * (board.IsWhiteToMove ? movesnow - movesnext : movesnext - movesnow);
        }

        if (pls[0].Count == 0)
            res -= HavingPawns;

        if (pls[6].Count == 0)
            res += HavingPawns;

        foreach (Piece piece in pls[0])
        {
            maxRank = Max(maxRank, piece.Square.Rank);
            pawnAdvancementSum += piece.Square.Rank;
        }

        res += MaxPawnAdvancement * maxRank;

        maxRank = 0;

        foreach (Piece piece in pls[6])
        {
            maxRank = Max(maxRank, 7 - piece.Square.Rank);
            pawnAdvancementSum -= 7 - piece.Square.Rank;
        }

        res -= maxRank * MaxPawnAdvancement;

        res += pawnAdvancementSum * pawnAdvancement;

        {
            Square wk = board.GetKingSquare(true), bk = board.GetKingSquare(false);

            int wi = 0, bi = 0;
            for (int pt = 1; pt < 7; ++pt)
            {
                PieceList pl = board.GetPieceList((PieceType)pt, pt != (int)PieceType.King);

                foreach (Piece p in pl)
                {
                    Square s = p.Square;
                    res -= Max(Abs(-3.5 + s.Rank), Abs(-3.5 + s.File)) * Centralistaion;
                    ++wi;
                    wd += Dist(s, bk) * AggressionWeights[pt] + Dist(s, wk) * AggressionWeights[pt + 6];
                }

                pl = board.GetPieceList((PieceType)pt, pt == (int)PieceType.King);

                foreach (Piece p in pl)
                {
                    Square s = p.Square;
                    res += Max(Abs(-3.5 + s.Rank), Abs(-3.5 + s.File)) * Centralistaion;
                    ++bi;
                    bd += Dist(s, wk) * AggressionWeights[pt] + Dist(s, bk) * AggressionWeights[pt + 6];
                }
            }

            wd /= bi;
            bd /= wi;

            res += Aggression / wd - Aggression / bd;
        }

        return res;
    }
    double Dist(Square a, Square b) => Max(Abs(a.Rank - b.Rank), Abs(a.File - b.File));

    double Search(Board board, int depth, double alpha, double beta, int bonusLeft, bool wasCheck)
    {
        if (ExitCalculations() || board.IsDraw())
            return 0;

        double res = board.IsWhiteToMove ? miv : mav;

        if (board.IsInCheckmate())
            return res;

        double defaultRes = depth <= 0 ? Eval(board) : 0;

        var moves = board.GetLegalMoves();

        foreach (var move in moves)
        {
            double tmp = defaultRes;

            board.MakeMove(move);
            if (depth > 0 || (move.IsCapture && move.CapturePieceType != PieceType.Pawn) || move.IsPromotion || wasCheck)
                tmp = Search(board, depth - 1, alpha, beta, bonusLeft, board.IsInCheck());
            else if (bonusLeft > 0)
                tmp = Search(board, -1, alpha, beta, bonusLeft - 1, board.IsInCheck());
            board.UndoMove(move);

            if (Abs(tmp) > 999)
                tmp /= 2;

            if (board.IsWhiteToMove)
            {
                res = Max(res, tmp);
                if (res > beta)
                    break;
                alpha = Max(alpha, res);
            }
            else
            {
                res = Min(res, tmp);
                if (res < alpha)
                    break;
                beta = Min(beta, res);
            }
        }

        return res;
    }
}