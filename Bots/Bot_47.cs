namespace auto_Bot_47;
using ChessChallenge.API;
using System;

public class Bot_47 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        float bestValue = int.MinValue;
        float mostOppMoves = int.MinValue;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            var oppMoves = board.GetLegalMoves();
            var oppMoveCount = oppMoves.Length; // number of moves our opponent can make
            var nextMoveSum = 0;
            foreach (Move oppMove in oppMoves)
            {
                board.MakeMove(oppMove);
                nextMoveSum += board.GetLegalMoves().Length; // number of moves if our opponent made oppMove
                board.UndoMove(oppMove);
            }
            board.UndoMove(move);

            float nextMoveAvg = (float)nextMoveSum / (float)oppMoves.Length;
            DivertedConsole.Write(nextMoveAvg);
            if (nextMoveAvg < bestValue) // if this move would result in 
            {
                bestValue = nextMoveAvg;
                bestMove = move;
            }
            if (nextMoveAvg == bestValue) // if the average is the same, then maybe choose move with move opponent moves
            {
                if (oppMoves.Length > mostOppMoves)
                {
                    mostOppMoves = oppMoves.Length;
                    bestValue = nextMoveAvg;
                    bestMove = move;
                }
            }
        }
        board.MakeMove(bestMove);
        // keep track of the last few moves, and if we've made the same move twice - move a random piece instead
        return bestMove;
    }
}