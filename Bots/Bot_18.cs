namespace auto_Bot_18;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_18 : IChessBot
{

    /// <summary>
    /// A collection of piece values overtime
    /// More valueable pieces are more likely to move
    /// List element to reduce the number of tokens
    /// </summary>
    private List<int> pieceValueOvertime = new List<int>(){
        /*  Base Value, FirstFewMoves, OpeningOffset, MidGameOffset, EndGameOffset */
        /*Pawn*/     1,            10,             7,             1,            5,
        /*Knight*/   3,             1,             7,             7,            1,
        /*Bishop*/   5,            10,             7,             7,            1,
        /*Rook*/     8,             1,             1,             7,            1,
        /*Queen*/   20,            10,             1,             8,            1,
        /*King*/    99,             1,             1,             1,            1,
    };

    int moveNum = 0;
    int phaseOffset;
    int minDepth = 2;
    int maxDepth = 6;
    int nextDepth = 2;

    Random random = new Random();

    public Move Think(Board board, Timer timer)
    {
        moveNum++;

        // Store legal moves
        Move[] moves = board.GetLegalMoves();
        if (moves.Length == 1)
        {
            return moves[0];
        }

        // Decide which phase and strategy to deploy
        phaseOffset = moveNum < 3
            ? 1 // First few moves
            : moveNum < 6
                ? 2 // Opening phase
                : (moveNum < 10 || moveNum < 30 && moves.Length > 6)
                    ? 3  // Midgame phase
                    : 4; // Endgame phase

        // Generate move lottery, add tickets based on the piece value phase offsets
        List<Move> moveLottery = new List<Move>();
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            for (int ticketsAdded = 0; ticketsAdded < pieceValueOvertime[4 * (((int)move.MovePieceType - 1)) + phaseOffset]; ticketsAdded++)
            {
                moveLottery.Add(move);
            }
        }

        Move[] candidateMoves = new Move[20];

        if (moves.Length <= 30)
        {
            // Few enough moves to consider all
            candidateMoves = moves;
        }
        else
        {
            // Select a handful of moves to consider
            int addedMoves = 0;

            // Select at least one move per piece type
            for (int type = ((int)PieceType.Queen); type > 0 && addedMoves < 20; type--)
            {
                foreach (Move move in moves)
                {
                    if (((int)move.MovePieceType) == type)
                    {
                        candidateMoves[addedMoves++] = move;
                        break;
                    }
                }

            }

            // Select other moves randomly from the "lottery", duplicates may occur
            while (addedMoves < 20)
            {
                int index = random.Next(moveLottery.Count);
                candidateMoves[addedMoves++] = moveLottery[index];
            }
        }

        // Evaluate the chosen subset of moves
        int depth = nextDepth;
        nextDepth += 1;
        if (nextDepth > maxDepth)
        {
            nextDepth = maxDepth;
        }
        return getBestMove(candidateMoves, board, board.IsWhiteToMove, timer, phaseOffset, depth, 1).Item1;
    }

    /// <summary>
    /// Recursive depth first search
    /// </summary>
    private Tuple<Move, int> getBestMove(Move[] moves, Board board, bool scoreForWhite, Timer timer, int phaseOffset, int maxDepth, int depth)
    {
        bool timeLimitReached = timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining >> 2;

        if (timeLimitReached)
        {
            // Decrease the depth of next turn to reduce time consumption
            nextDepth = minDepth;
        }

        if (depth > maxDepth || timeLimitReached)
        {
            // End condition reached
            // Calculate current score
            int score = 0;

            if (board.IsInCheckmate() || board.IsDraw())
            {
                score =
                    ((board.IsWhiteToMove && scoreForWhite) || board.IsDraw())
                        ? -99999
                        : 99999;
            }

            score += getBoardValue(board, phaseOffset, scoreForWhite);
            score -= getBoardValue(board, phaseOffset, !scoreForWhite);
            // Add some random variation to spice it up
            score += random.Next(8) - 4;

            // Add more value to checks in the later game phases
            if (board.IsInCheck())
            {
                score += ((board.IsWhiteToMove && scoreForWhite) ? -10 : 10) * (phaseOffset - 1);
            }

            // Only the score matters
            return new Tuple<Move, int>(new Move(), score);

        }

        // Depth first search
        Tuple<Move, int> bestMove = new Tuple<Move, int>(moves[0], -99999);
        Move[] newMoves;
        for (int m = 0; m < moves.Length; m++)
        {
            board.MakeMove(moves[m]);

            Tuple<Move, int> moveScore;
            newMoves = board.GetLegalMoves();
            if (board.IsInCheckmate() || board.IsDraw() || newMoves.Length == 0)
            {
                moveScore = new Tuple<Move, int>(
                        moves[m],
                        ((board.IsWhiteToMove && scoreForWhite) || board.IsDraw())
                            ? -99999
                            : 99999
                    );
            }
            else
            {
                // Search for the best opponent move
                moveScore = getBestMove(newMoves, board, !scoreForWhite, timer, phaseOffset, newMoves.Length > 30 ? maxDepth - 1 : maxDepth, depth + 1);
                // Invert score to get the worst result the opponent can generate
                int score = -moveScore.Item2 + pieceValueOvertime[4 * (((int)moves[m].MovePieceType) - 1) + phaseOffset];
                moveScore = new Tuple<Move, int>(moveScore.Item1, score);
            }

            if (bestMove == null || moveScore.Item2 > bestMove.Item2)
            {
                bestMove = new Tuple<Move, int>(moves[m], moveScore.Item2);
            }

            board.UndoMove(moves[m]);
        }
        return bestMove;
    }

    /// <summary>
    /// Calculate board value for one side
    /// </summary>
    private int getBoardValue(Board board, int phaseOffset, bool whitePieces)
    {
        int score = 0;
        foreach (PieceType type in (PieceType[])Enum.GetValues(typeof(PieceType)))
        {
            if (type == PieceType.None)
            {
                continue;
            }
            int typeOffset = 4 * (((int)type) - 1);
            score += board.GetPieceList(type, whitePieces).Count
                 * (pieceValueOvertime[typeOffset] + pieceValueOvertime[typeOffset + phaseOffset]);
        }
        return score;
    }
}