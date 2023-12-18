namespace auto_Bot_263;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_263 : IChessBot
{
    //tweakable scores
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 10, 30, 30, 50, 90, 10000 };
    int[] attackValues = { 1, 1, 3, 3, 5, 9, 10 };
    //int[] attackValues = { 0, 0, 0, 0, 0, 0, 0 };
    int pawnRankVal = 0;
    int kingMoveCost = 0;

    Random rng = new();
    List<ulong> keys = new List<ulong>();
    List<int> scores = new List<int>();
    public Move Think(Board board, Timer timer)
    {
        //go through all moves
        //return recursiveThink(board, 2);
        Move domove = ABPruning(board, 1, 0, -10000000, 10000000, timer).Item1;
        return domove;
    }
    (Move, int) ABPruning(Board board, int maxPlayer, int depth, int alpha, int beta, Timer timer)
    {
        Move bestMove = new Move("a1a1", board);
        if (board.IsInCheckmate())
        {
            return (bestMove, 10000000);
        }
        //DivertedConsole.Write(Math.Log(timer.MillisecondsRemaining)/5 + " ");
        if (depth > Math.Log(timer.MillisecondsRemaining) / 5)
        {
            return (bestMove, scoreBoard(board, true));
        }
        Move[] moves = board.GetLegalMoves();
        int bestScore = -1000000 * maxPlayer;
        DivertedConsole.Write(bestScore + " " + moves.Length);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            (Move, int) results = ABPruning(board, -1 * maxPlayer, depth + 1, alpha, beta, timer);
            board.UndoMove(move);

            int testScore = results.Item2;
            //DivertedConsole.Write(move + "1"+testScore+" "+bestScore);
            DivertedConsole.Write(bestMove + " " + bestScore + " " + move + " " + testScore + " " + depth + " " + maxPlayer);
            if (testScore * maxPlayer >= bestScore * maxPlayer)
            {
                bestScore = testScore;
                bestMove = move;
                //DivertedConsole.Write(bestMove + " " + bestScore + " " + depth + " " + maxPlayer);
                //DivertedConsole.Write(bestMove + "2");
            }
            if (maxPlayer > 0)
            {
                alpha = Math.Max(alpha, bestScore);
            }
            else
            {
                beta = Math.Min(beta, bestScore);
            }
            if (beta <= alpha)
            {
                break;
            }
        }
        //DivertedConsole.Write(bestMove + "why");
        DivertedConsole.Write(bestMove + " " + bestScore + " " + depth);
        return (bestMove, bestScore);
    }
    Move recursiveThink(Board board, int depth)
    {
        Move[] moves = board.GetLegalMoves();

        //avoid index error
        if (moves.Length == 0)
        {
            return new Move("a1a1", board);
        }
        // default to first
        Move bestMove = moves[0];

        int maxScore = -1000;
        //go through all moves
        foreach (Move move in moves)
        {
            int testScore = scoreBoard(board, true);
            //minor varience
            //testScore = testScore * 2 + rng.Next(1);
            if (testScore > maxScore)
            {
                //DivertedConsole.Write("nanpa wan sin: " + testScore);
                maxScore = testScore;
                bestMove = move;
            }
        }
        return bestMove;
    }

    int scoreMoveHelper(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        int maxScore = -1000000;
        //prevent softlock
        if (moves.Length == 0)
        {
            return 100000;
        }
        //go through all moves
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int testScore = scoreBoard(board, false);
            board.UndoMove(move);
            //get best
            maxScore = Math.Max(maxScore, testScore);
        }
        return maxScore;
    }
    int scoreBoard(Board board, bool deep)
    {
        //check cache
        ulong code = board.ZobristKey;
        if (deep && keys.Contains(code))
        {
            DivertedConsole.Write("mute pi sona pini: " + keys.Count);
            return scores[keys.IndexOf(code)];
        }
        //base score
        int score = 0;
        //force checkmate
        if (board.IsInCheckmate())
        {
            score = 10000000;
        }
        else
        {
            //value pieces
            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                //only your pieces
                if (pieceList.IsWhitePieceList != board.IsWhiteToMove)
                {
                    score = score + 8 * pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count;
                    // add score for pawn distance
                    if ((int)pieceList.TypeOfPieceInList == 1)
                    {
                        for (int i = 0; i < pieceList.Count; i++)
                        {
                            //based on getting to other side
                            int pawnRank = pieceList.GetPiece(i).Square.Rank;
                            //flipped if black
                            if (board.IsWhiteToMove)
                            {
                                pawnRank = 7 - pawnRank;
                            }
                            //DivertedConsole.Write(pawnRank + "");
                            score = score + pawnRank * pawnRankVal;
                        }
                    }
                }
            }

            //maximize control
            for (int i = 0; i <= 63; i++)
            {
                Square temp = new Square(i);
                if (board.SquareIsAttackedByOpponent(temp))
                {
                    score = score + attackValues[(int)board.GetPiece(temp).PieceType];
                }
            }
            //minimize their options
            score = score - board.GetLegalMoves().Count();
            //especially their king
            Move[] kingMoves = board.GetLegalMoves();
            foreach (Move kingMove in kingMoves)
            {
                if ((int)kingMove.MovePieceType == 6)
                {
                    score = score - kingMoveCost;
                }
            }
        }

        if (deep)
        {
            score = score - scoreMoveHelper(board);
        }

        if (deep)
        {
            keys.Add(code);
            scores.Add(score);
        }
        return score;
    }
}