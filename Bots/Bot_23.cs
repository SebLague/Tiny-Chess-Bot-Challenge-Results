namespace auto_Bot_23;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_23 : IChessBot
{
    // HalfWit bot.
    private int[] PieceValue = { 0, 100, 275, 300, 500, 850, 10000, 100000 }; // Extra value pads our token count

    private Dictionary<ulong, int> scoreCache = new();
    private Board currentBoard;
    private int depth;
    private int temporalDepth;
    private Timer time;
    private Random rng = new();
    private int noValue = Int32.MinValue;
    private int checkmateScore = 100000;

    public Move Think(Board board, Timer timer)
    {
        currentBoard = board;
        time = timer;
        depth = 0;

        var legalMoves = board.GetLegalMoves();
        var bestMove = GetBestMove(legalMoves).Moves;
        if (bestMove.Any())
            legalMoves = bestMove.ToArray();

        return legalMoves[rng.Next(legalMoves.Length)];
    }

    int GetMoveScore(Move move)
    {
        int type = (int)move.MovePieceType;
        bool isOpponentsTurn = (depth % 2) == 1;
        bool evade = false;

        if (depth == 0)
            evade = currentBoard.SquareIsAttackedByOpponent(move.StartSquare);
        else if (depth > 1 && isOpponentsTurn)
            if (!move.IsCapture && !move.IsPromotion)
                return 0;

        currentBoard.MakeMove(move);

        ulong zkey = currentBoard.ZobristKey;
        int result;
        if (scoreCache.TryGetValue(zkey, out result))
        {
            currentBoard.UndoMove(move);
            return result;
        }

        if (currentBoard.IsInCheckmate())
        {
            currentBoard.UndoMove(move);
            return checkmateScore;
        }

        if (isOpponentsTurn || depth > 4)
            if (!move.IsCapture && !move.IsPromotion)
            {
                currentBoard.UndoMove(move);
                return 0;
            }

        // We play to win, a draw is always undesirable
        if (currentBoard.IsDraw())
            result += isOpponentsTurn ? 100 : -100;

        if (currentBoard.IsInCheck())
            result += 20;

        if (move.IsCastles || move.IsCapture)
            result += 35;

        if (move.IsPromotion)
            result += (int)move.PromotionPieceType * 5;

        if (depth == 0)
        {
            if (evade)
                result += type * type * 2;

            if ((result < 10 && type < 5))
                temporalDepth = 3;
            else
            {
                temporalDepth = 9 - (time.MillisecondsElapsedThisTurn / 600) - (time.MillisecondsRemaining < 20000 ? 5 : 0);
                if (temporalDepth < 3)
                    temporalDepth = 3;
            }
        }

        // Seems to work ok without a little bias for the piece type.
        //result += type <= 3 ? type + 10 : type;

        if (depth < temporalDepth)
        {
            depth++;
            int score = GetBestMove(currentBoard.GetLegalMoves(false)).Score;
            if (score != noValue)
                result -= score;
            depth--;
        }

        currentBoard.UndoMove(move);

        result += PieceValue[(int)currentBoard.GetPiece(move.TargetSquare).PieceType];
        scoreCache[zkey] = result;

        return result;
    }

    (int Score, List<Move> Moves) GetBestMove(Move[] currentMoves)
    {
        int curScore = noValue;
        List<Move> bestMoves = new List<Move>();
        foreach (Move move in currentMoves)
        {
            int score = GetMoveScore(move);
            if (score > curScore)
            {
                curScore = score;
                bestMoves.Clear();
            }
            if (score == curScore)
                bestMoves.Add(move);

            if (curScore >= checkmateScore)
                break;
        }

        if (bestMoves.Any())
            return (curScore, bestMoves);

        return (noValue, new List<Move>());
    }
}