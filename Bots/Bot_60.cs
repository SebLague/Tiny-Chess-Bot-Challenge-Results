namespace auto_Bot_60;
using ChessChallenge.API;
using System;

public class Bot_60 : IChessBot
{

    // A bot I accidentally created which attempts to offer up as many pieces for sacrifice as possible 

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        BestMove _bestMove = new BestMove(moves[0]);
        int i = 0;
        int score = -99999;
        foreach (var move in moves)
        {
            i++;

            score = TestMove(board, move);
            board.MakeMove(move);
            score += TestPosition(board);
            board.UndoMove(move);


            DivertedConsole.Write(
                move.ToString() + " " +
                score + " " +
                _bestMove.SetMove(move, score)
                );

        }
        DivertedConsole.Write();
        return _bestMove.GetMove();
    }

    private int TestPosition(Board board)
    {
        int score = 0;
        Move[] moves = board.GetLegalMoves();

        foreach (var boardMove in moves)
        {
            if (boardMove.IsCapture)
            {
                score += 100;
            }
        }

        return score;
    }

    private int TestMove(Board board, Move move)
    {
        int score = 0;
        int moves;
        board.MakeMove(move);

        // Opponent's turn
        moves = board.GetLegalMoves().Length;
        {

            score += (200 / ((3 * moves) + 1));
        }

        if (!board.TrySkipTurn())
        {
            score += 15; // Bonus for giving check
        }
        else
        {
            // Next turn
            moves = board.GetLegalMoves().Length;
            {
                score -= (500 / ((3 * moves) + 1));
            }

            board.UndoSkipTurn();
        }
        board.UndoMove(move);
        return score;
    }

    private struct BestMove
    {
        private int score;
        private Move move;

        public BestMove(Move newMove)
        {
            move = newMove;
            score = -99999;
        }

        public bool SetMove(Move newMove, int newScore)
        {
            if (newScore > score)
            {
                move = newMove;
                score = newScore;
                return true;
            }
            else
            {
                return false;
            }
        }

        public Move GetMove()
        {
            return move;
        }

    }
}