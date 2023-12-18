namespace auto_Bot_253;
using ChessChallenge.API;
using System;

public class Bot_253 : IChessBot
{

    bool isEndgame;

    /*
     * The method to implement
     */
    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();
        float[] scores = new float[legalMoves.Length];
        int color = board.IsWhiteToMove ? 1 : -1;
        if (!isEndgame)
        {
            isEndgame = evaluate(board, true) <= 20;
        }

        //First move as white always e4 or d4 (else it plays nonsense at shallow depth)
        if (board.PlyCount == 0) return new Move("e2e4", board);

        //Game tree search
        for (int depth = 0; true; depth++)
        {
            for (int i = 0; i < legalMoves.Length; i++)
            {

                board.MakeMove(legalMoves[i]);
                float score = search(board, depth, -10001.0f, 10001.0f, -1 * color, timer);
                board.UndoMove(legalMoves[i]);

                if (color * score == 10000.0f) return legalMoves[i];
                if (score == float.MaxValue)
                {
                    Array.Sort(scores, legalMoves);
                    return legalMoves[0];
                }

                scores[i] = -1 * color * score;
            }

            Array.Sort(scores, legalMoves);
        }

    }

    /*
     * Evaluates the position on the given board using a simple heuristic based on piece positioning, material value and mobility 
     */
    private float evaluate(Board board, bool onlyCountAllPieces = false)
    {
        float score = 0;
        PieceList[] allLists = board.GetAllPieceLists();

        //Material count (excluding hanging pieces)
        for (int i = 0; i < 12; i++)
        {
            bool col = i < 6 ? true : false;
            foreach (Piece p in allLists[i])
            {
                float multi = 1;
                bool attacked1 = board.SquareIsAttackedByOpponent(p.Square);
                board.ForceSkipTurn();
                bool attacked2 = board.SquareIsAttackedByOpponent(p.Square);
                board.UndoSkipTurn();
                if (board.IsWhiteToMove != col) multi = (attacked2 && !attacked1) ? 0 : 1;
                score += multi * (col || onlyCountAllPieces ? 1 : -1) * (p.IsPawn ? 1 : (p.IsKnight || p.IsBishop ? 3 : (p.IsRook ? 5 : (p.IsQueen ? 9 : 0))));
            }
        }

        if (onlyCountAllPieces) return score;

        //Simple positioning
        for (int i = 0; i < 12; i++)
        {
            foreach (Piece p in allLists[i])
            {
                //Pawns (the more forward the better)
                score += 0.03f * (i == 0 ? p.Square.Rank - 1 : (i == 6 ? p.Square.Rank - 6 : 0));
                //Knights and endgame kings (the more central the better)
                score += 0.08f * ((i == 1 || i == 5 && isEndgame) ? 5 - Math.Abs(p.Square.Rank - 3.5f) - Math.Abs(p.Square.File - 3.5f) : ((i == 7 || i == 11 && isEndgame) ? Math.Abs(p.Square.Rank - 3.5f) + Math.Abs(p.Square.File - 3.5f) - 5 : 0));
                //Bishops (penalty for undeveloped bishops, except in the endgame)
                score += 0.06f * ((!isEndgame && i == 2 && p.Square.Rank == 0) ? -1 : ((!isEndgame && i == 8 && p.Square.Rank == 7) ? 1 : 0));
                //Rooks (the more forward the better, except in the endgame)
                score += 0.1f * ((!isEndgame && i == 3) ? p.Square.Rank / 7.0f : ((!isEndgame && i == 9) ? (p.Square.Rank - 7) / 7.0f : 0));
                //Kings (this should encourage castling in the early game)
                score += 0.35f * ((i == 5 && board.PlyCount < 30 && (p.Square.Index == 6 || p.Square.Index == 2)) ? 1 : ((i == 11 && board.PlyCount < 30 && (p.Square.Index == 58 || p.Square.Index == 62)) ? -1 : 0));
            }
        }

        //Mobility
        int color = board.IsWhiteToMove ? 1 : -1;
        Move[] legalMovesCurrColor = board.GetLegalMoves();
        board.ForceSkipTurn();
        Move[] legalMovesOppColor = board.GetLegalMoves();
        board.UndoSkipTurn();
        score += 0.01f * (((float)color * legalMovesCurrColor.Length) + ((float)-1 * color * legalMovesOppColor.Length));

        return score;
    }

    /*
     * Iteratively searches the game tree up to a given depth using minimax with pruning
     */
    private float search(Board board, int depth, float alpha, float beta, int color, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();

        //End search if time is up
        if ((timer.OpponentMillisecondsRemaining > timer.MillisecondsRemaining && timer.MillisecondsElapsedThisTurn > timer.GameStartTimeMilliseconds / 70 || timer.MillisecondsElapsedThisTurn > timer.GameStartTimeMilliseconds / 50 || timer.MillisecondsRemaining < 10000) && timer.MillisecondsElapsedThisTurn > 199)
        {
            return float.MaxValue;
        }
        //Return if game is over or at leaf node
        if (board.IsInCheck() && legalMoves.Length == 0)
        {
            return -1 * color * 10000;
        }
        else if (board.IsFiftyMoveDraw() || board.IsRepeatedPosition() || !board.IsInCheck() && legalMoves.Length == 0)
        {
            return 0;
        }
        else if (depth == 0)
        {
            return evaluate(board);
        }

        float highestEval = -1 * color * 10001.0f;

        for (int i = 0; i < legalMoves.Length; i++)
        {
            Move m = legalMoves[i];

            board.MakeMove(m);
            float newEval = search(board, depth - 1, alpha, beta, -1 * color, timer);
            board.UndoMove(m);

            if (newEval == float.MaxValue) return float.MaxValue;
            highestEval = color * newEval > color * highestEval ? newEval : highestEval;

            //Alpha beta pruning
            if (highestEval > beta && color == 1 || highestEval < alpha && color == -1) break;
            alpha = color == 1 && highestEval > alpha ? highestEval : alpha;
            beta = color == -1 && highestEval < beta ? highestEval : beta;
        }

        return highestEval;
    }

}
