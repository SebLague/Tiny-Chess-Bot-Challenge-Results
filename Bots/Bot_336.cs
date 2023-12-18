namespace auto_Bot_336;
using ChessChallenge.API;
using System;


public class Bot_336 : IChessBot
{// Piece values: null, pawn + 75 in eval, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 125, 300, 330, 500, 900, 10000 };


    int moveToPlay = 0;
    Move[] allMoves;
    Move bestMove;

    //Move[] bestMoves = new Move[100];

    private int max_depth = 4;

    int evaluatedNumber;
    int prunedNumber;

    int evaluatedTOTAL;
    int GamePlayed;

    Timer timer;

    int capture_deepening = -20;


    public Move Think(Board board, Timer timer)
    {
        this.timer = timer;


        // set max depth based on time and number of pieces left
        double multiplicator = 3 * Math.Pow(timer.MillisecondsRemaining / 1000.0, 1.0 / 4.0) / 10.0;
        //multiplicator = 0.7;
        max_depth = (int)(1 + multiplicator * (3.5 + 0 * Math.Exp(-board.GetLegalMoves().Length) + 15 * Math.Exp(-BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) / 8.0)));

        DivertedConsole.Write("--------- " + Evaluate(board) + " ---------");

        DivertedConsole.Write("depth : " + max_depth + "  \tTime : " + timer.MillisecondsRemaining + "  \tMoves : " + board.GetLegalMoves().Length + "  pieces" + BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) + "  multip : " + multiplicator);

        evaluatedNumber = 0;
        prunedNumber = 0;

        // call the negamax function
        bestMove = board.GetLegalMoves()[0];
        int bestScore = NegamaxAB(board, max_depth, int.MinValue + 1, int.MaxValue - 1, board.IsWhiteToMove ? 1 : -1);

        evaluatedTOTAL += evaluatedNumber;
        GamePlayed++;
        capture_deepening = -(int)Math.Pow(-capture_deepening * 1000000 / evaluatedNumber, 1.0 / 2.0);
        DivertedConsole.Write("Best score: " + bestScore + "  \tBest move: " + bestMove.ToString() + "  \tEvaluated: " + evaluatedNumber + "   \tPruned: " + prunedNumber + "    Evaluated average: " + evaluatedTOTAL / GamePlayed + "   \tCapture deepening: " + capture_deepening);

        board.MakeMove(bestMove);
        DivertedConsole.Write("-------> " + Evaluate(board) + " ---------");
        board.UndoMove(bestMove);

        //lookUpBestmoves(board, 0);

        return bestMove;
    }

    // the evaluation function
    int Evaluate(Board board)
    {
        // check for checkmate
        if (board.IsInCheckmate())
        {
            return 999999 * (board.IsWhiteToMove ? -1 : 1);
        }
        if (board.IsDraw())
        {
            return 0;
        }

        PieceList[] piecesLists = board.GetAllPieceLists();
        int score = 0;

        foreach (var pieces in piecesLists)
        {
            foreach (var p in pieces)
            {
                score += (pieceValues[(int)p.PieceType]
                          + 5 * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(p.PieceType, p.Square, board, p.IsWhite))
                          ) * (p.IsWhite ? 1 : -1)
                          + (p.Square.Rank * 10 - 35) * (p.IsPawn ? 1 : 0);
            }
        }

        return score;
    }


    // negamax with alpha beta pruning
    int NegamaxAB(Board board, int depth, int alpha, int beta, int color)
    {
        Move[] allMoves;
        int bestScore;
        if (depth < capture_deepening || timer.MillisecondsElapsedThisTurn / 1000 > 10)
        {
            return Evaluate(board) * color;
        }
        if (depth <= 0)
        {
            allMoves = board.GetLegalMoves(true);
            bestScore = Evaluate(board) * color;
        }
        else
        {
            // get all the moves
            allMoves = board.GetLegalMoves();

            bestScore = -88888888;
        }


        // if there is no move, it is a checkmate or a stalemate
        if (allMoves.Length == 0 || board.IsDraw())
        {
            return Evaluate(board) * color;
        }

        // sort the moves
        Array.Sort(allMoves, (x, y) => EvaluateMove(board, y).CompareTo(EvaluateMove(board, x)));

        // for every move
        for (int i = 0; i < allMoves.Length; i++)
        {
            // we move the board to the position
            board.MakeMove(allMoves[i]);

            // we update the score
            evaluatedNumber++;
            int score = -NegamaxAB(board, depth - 1, -beta, -alpha, -color);
            if (score > bestScore)
            {
                bestScore = score;
                if (depth == max_depth)
                {
                    bestMove = allMoves[i];
                }

                // save all the best moves
                //bestMoves[max_depth - depth] = allMoves[i];

            }
            alpha = Math.Max(alpha, bestScore);

            // we move back the board
            board.UndoMove(allMoves[i]);

            // we prune the tree
            // if alpha is greater than beta, we stop the search
            if (alpha >= beta)
            {
                prunedNumber++;
                break;
            }
        }

        return bestScore;
    }


    // the evaluation function for the moves
    int EvaluateMove(Board board, Move move)
    {
        int score = 0;
        if (move.IsCapture)
        {
            //score += pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType]
            score += pieceValues[(int)move.CapturePieceType];

        }
        score -= pieceValues[(int)board.GetPiece(move.StartSquare).PieceType] / 150;

        if (move.IsPromotion)
        {
            score += pieceValues[(int)move.PromotionPieceType];
        }

        // penalty for moving to an attacked square
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            score -= pieceValues[(int)board.GetPiece(move.StartSquare).PieceType];
        }

        return score;
    }
}