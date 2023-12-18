namespace auto_Bot_70;
using ChessChallenge.API;
using System;



public class Bot_70 : IChessBot
{

    double[] pieceValues = { 0.00, 1.00, 3.05, 3.33, 5.63, 9.50, 100.00 };


    //Evaluates based on materiel
    public double tenativeEval(Board board)
    {
        if (board.IsInCheckmate())
        {
            return -100.00;
        }
        else if (board.IsDraw())
        {
            return 0.00;
        }
        else
        {
            double materielEval = 0;
            PieceList[] plist = board.GetAllPieceLists();
            foreach (PieceList list in plist)
            {
                bool isWhite = list.IsWhitePieceList;
                if (isWhite)
                {
                    materielEval += (list.Count * pieceValues[(int)list.TypeOfPieceInList]);
                }
                else
                {
                    materielEval -= (list.Count * pieceValues[(int)list.TypeOfPieceInList]);
                }
            }
            if (!board.IsWhiteToMove)
            {
                materielEval = -materielEval;
            }
            return 0.20 + materielEval + (-0.000001 + rng.NextDouble() * 0.000002);
        }
    }

    Move bestMove;
    Random rng = new();

    //we want to think deeper about interesting lines,
    //shallower about less interesting lines, where one side obviously loses.
    public double eval(Board board, int nodeBudget, double alpha, double beta, bool isRoot)
    {
        //DivertedConsole.Write(nodeBudget);
        double guessedEval = tenativeEval(board);
        nodeBudget--;
        if (nodeBudget <= 0)
        {
            return guessedEval;
        }
        if (board.IsInCheckmate() || board.IsDraw())
        {
            return guessedEval;
        }
        Move[] moves = board.GetLegalMoves();
        int numMoves = moves.Length;
        double[] weight = new double[numMoves];
        double totSum = 0;
        for (int i = 0; i < numMoves; i++)
        {
            board.MakeMove(moves[i]);
            weight[i] = Math.Pow(0.7, Math.Clamp(tenativeEval(board) + guessedEval, -10, 10));
            totSum += weight[i];
            board.UndoMove(moves[i]);
        }
        for (int i = 0; i < numMoves; i++)
        {
            for (int j = 0; j < numMoves; j++)
            {
                if (weight[i] > weight[j])
                {
                    double temp = weight[j];
                    weight[j] = weight[i];
                    weight[i] = temp;
                    Move temp2 = moves[j];
                    moves[j] = moves[i];
                    moves[i] = temp2;
                }
            }
        }
        /*for(int i = 0; i < numMoves; i++){
            DivertedConsole.Write(Math.Round(weight[i], 1) + " ");
        }
        DivertedConsole.Write();*/
        for (int i = 0; i < numMoves; i++)
        {
            board.MakeMove(moves[i]);
            double score = -eval(board, (int)(weight[i] * nodeBudget / totSum), -beta, -alpha, false);
            board.UndoMove(moves[i]);
            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
                if (isRoot)
                {
                    bestMove = moves[i];
                }
            }
        }
        if (alpha >= 99.00)
        {
            alpha -= 0.01;
        }
        if (alpha <= -99.00)
        {
            alpha += 0.01;
        }
        return alpha;
    }

    public Move Think(Board board, Timer timer)
    {
        //double x = tenativeEval(board);
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        bestMove = allMoves[rng.Next(allMoves.Length)];

        double factor;
        if (board.PlyCount < 15)
        {
            factor = 1;
        }
        else if (board.PlyCount < 30)
        {
            factor = 1.6;
        }
        else if (board.PlyCount < 45)
        {
            factor = 1.3;
        }
        else
        {
            factor = 1;
        }
        double target = timer.MillisecondsRemaining / (40000.0);
        double time = factor * target;

        //700000 nodes/second
        double y = eval(board, (int)(700000.0 * time), -100000.00, 100000.00, true);



        //DivertedConsole.Write(y);
        //DivertedConsole.Write(board.PlyCount);
        //  DivertedConsole.Write(time);
        //   DivertedConsole.Write((int)(500000.0 * time));
        // DivertedConsole.Write(timer.MillisecondsElapsedThisTurn);
        return bestMove;
    }
}