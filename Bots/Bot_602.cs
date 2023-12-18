namespace auto_Bot_602;
using ChessChallenge.API;
using System;

// Excuse some weird ternary to limit tokens
public class Bot_602 : IChessBot
{
    Board board;
    Timer timer;
    int stopTime;

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        this.timer = timer;

        // give itself a relatively short time limit to find a move
        stopTime = timer.MillisecondsRemaining - timer.MillisecondsRemaining / 90;

        // get a baseline score for the current position
        var (bestScore, bestMove) = Search(1);

        // search deeper until time is up
        for (int depth = 4; timer.MillisecondsRemaining > stopTime; depth++)
        {
            var (score, move) = Search(depth);
            if (timer.MillisecondsRemaining > stopTime)
                bestMove = move;
        }

        // Will return a null move if in Zugzwang :/
        return bestMove;
    }

    (double, Move) Search(int depth, double alpha = -1000000, double beta = 1000000)
    {
        // Alpha-beta pruning basics
        if (depth <= 0 && EvaluatePoints() >= beta)
            return (beta, new Move());
        if (depth <= 0 && EvaluatePoints() > alpha)
            alpha = EvaluatePoints();

        Move[] moves = board.GetLegalMoves(depth <= 0);

        // Sort moves by how good they are, sadly this is too useful to full remove
        double[] moveScores = new double[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            // captures, promotions and castles are better
            moveScores[i] = moves[i].IsCapture || moves[i].IsPromotion || moves[i].IsCastles ? -2 : 0;
            if ((int)moves[i].MovePieceType % 5 == 1) // if is pawn or king it's not as good
                moveScores[i] += 1;
        }

        Array.Sort(moveScores, moves);

        // alphaMove is the move with the highest alpha so far(the best move)
        Move alphaMove = new Move();

        for (int i = 0; i < moves.Length && timer.MillisecondsRemaining > stopTime; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsDraw() && moves.Length > 1)
            {
                board.UndoMove(moves[i]); // I don't believe in draws, don't even look at them
                continue;
            }

            var (score, topMove) = Search(depth - 1, -beta, -alpha);
            board.UndoMove(moves[i]);

            score *= -0.999; // Taking action now is better than later (negative to invert score for turn)

            // Alpha-beta pruning
            if (score >= beta)
                return (beta, moves[i]);
            if (score > alpha)
            {
                alpha = score;
                alphaMove = moves[i];
            }
        }

        return (alpha, alphaMove);
    }


    double EvaluatePoints()
    {
        double points = 0;
        // loop through all 5 pieces(not kings) and add their value to points
        for (int i = 0; i < 5; i++)
            points += board.GetAllPieceLists()[i].Count * (i + 1) -
                      board.GetAllPieceLists()[i + 6].Count * (i + 1);
        // Queen is worth 5 pawns, good enough
        // Bishop > knight as it should be


        return board.IsInCheckmate() ? -1000000 : board.IsWhiteToMove ? points : -points;
    }
}