namespace auto_Bot_217;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;


public class Bot_217 : IChessBot
{

    int[] pieceValues = new int[] { 0, 1, 3, 3, 5, 9, 0 };
    private const float maxValue = 1234567f;
    private int depth = 3;
    private float startAlpha = -maxValue;
    private float startBeta = maxValue;

    int[] numPrunes = new int[4];
    int evaluationCalls;
    float sign = 1f;

    private Move bestMoveTotal;

    private Board board;

    public Move Think(Board board_, Timer timer)
    {
        board = board_;
        sign = board.IsWhiteToMove ? 1f : -1f;

        bestMoveTotal = board.GetLegalMoves()[0];

        /*
        debugMode = true;
        DivertedConsole.Write("Evaluation before: " + AnalyzePosition().ToString());
        debugMode = false;
        */

        for (int i = 0; i < numPrunes.Length; i++)
        {
            numPrunes[i] = 0;
        }
        evaluationCalls = 0;

        float curEvaluation = Search(depth, startAlpha, startBeta, true);

        /*
        Move tempMove = bestMoveTotal;
        foreach(Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            float evaluation = Search(depth, startAlpha, startBeta, true);
            board.UndoMove(move);
            DivertedConsole.Write(PrintMove(move) + ": " + evaluation.ToString());
        }
        */

        return bestMoveTotal;
    }

    // add checks, captures and moves of pieces that can be captured to move list
    private List<Move> MoveFilter()
    {
        // add all captures
        List<Move> newMoves = board.GetLegalMoves(true).ToList();

        // Find all attacked squares
        List<Square> attackedSquares = new List<Square>();
        if (board.TrySkipTurn())
        {
            foreach (Move curMove in board.GetLegalMoves(true))
            {
                Square curSquare = curMove.TargetSquare;
                if (!attackedSquares.Any(item => item == curSquare))
                {
                    attackedSquares.Add(curSquare);
                }
            }
            board.UndoSkipTurn();
        }
        else
        {
            // if turn cannot be skipped -> is in check, return all possible moves
            return board.GetLegalMoves().ToList();
        }

        foreach (Move curMove in board.GetLegalMoves(false))
        {
            List<float> pieceRating = new List<float>();
            // Add all promotions
            if (curMove.IsPromotion)
            {
                newMoves.Add(curMove);
            }

            board.MakeMove(curMove);
            // add all checks
            if (board.IsInCheck())
            {
                newMoves.Add(curMove);
            }

            // add best n moves for each figure


            board.UndoMove(curMove);

            // Add all moves of attacked pieces
            if (attackedSquares.Contains(curMove.StartSquare))
            {
                newMoves.Add(curMove);
            }
        }

        return newMoves;
    }

    private string PrintMove(Move move)
    {
        return move.MovePieceType + " " + move.StartSquare.Name + " to " + move.TargetSquare.Name;
    }

    private float CountMaterial()
    {
        // 8 + 6 + 6 + 10 + 9 = 39
        PieceList[] pieces = board.GetAllPieceLists();

        int evaluation = 0;
        for (int i = 0; i < pieces.GetLength(0) / 2; i++)
        {
            evaluation += pieces[i].Count * pieceValues[i + 1];
            evaluation -= pieces[i + pieces.GetLength(0) / 2].Count * pieceValues[i + 1];
        }

        return (float)(sign * evaluation);
    }

    private float AnalyzePosition()
    {
        return EvaluateAllFigures();
        //return CountMaterial();
    }

    private float Search(int depth_, float alpha, float beta, bool maximPlayer)
    {
        if (board.IsInCheckmate())
        {
            if (maximPlayer)
            {
                return -maxValue;
            }
            else
            {
                return maxValue;
            }
        }
        if (board.IsDraw())
        {
            return 0f;
        }
        if (depth_ == 0)
        {
            evaluationCalls++;
            return (AnalyzePosition());
        }
        List<Move> legalMoves = board.GetLegalMoves().ToList();
        if (maximPlayer)
        {
            float maxEval = -maxValue;
            Move bestMove = bestMoveTotal;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float evaluation = Search(depth_ - 1, alpha, beta, false);
                board.UndoMove(move);
                if (evaluation > maxEval)
                {
                    bestMove = move;
                    maxEval = evaluation;
                    alpha = evaluation;
                }

                if (alpha >= beta)
                {
                    numPrunes[depth_]++;
                    break;
                }
            }
            if (depth_ == depth)
            {
                bestMoveTotal = bestMove;
            }
            return (maxEval);
        }
        else
        {
            float minEval = maxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float evaluation = Search(depth_ - 1, alpha, beta, true);
                board.UndoMove(move);
                if (evaluation < minEval)
                {
                    minEval = evaluation;
                    beta = evaluation;
                }
                if (alpha >= beta)
                {
                    numPrunes[depth_]++;
                    break;
                }
            }
            return (minEval);
        }
    }

    float EvaluateFigure(Piece curPiece, bool pieceTurn_)
    {
        bool isAttacked = false;
        int curPieceValue = pieceValues[(int)curPiece.PieceType];
        int numPossibleMoves = 0;
        int bestCapture = 0;
        float pieceEvaluation = (float)curPieceValue;
        bool attackedByWorseFigure = true;

        bool skipSuccess = false;
        if (pieceTurn_)
        {
            skipSuccess = true;
            /*
            if (!skipSuccess)
            {
                return pieceEvaluation;
            }
            */
            board.ForceSkipTurn();
        }

        //DivertedConsole.Write((curPiece.IsWhite? "White" : "Black") + " piece and " + (board.IsWhiteToMove? "white" : "black") + " to move");


        bool isDefended = board.SquareIsAttackedByOpponent(curPiece.Square);
        //pieceEvaluation += isDefended? +0.2f : 0f;

        attackedByWorseFigure = (from move
                                    in board.GetLegalMoves()
                                 where (move.TargetSquare.Name == curPiece.Square.Name) && pieceValues[(int)move.MovePieceType] < curPieceValue
                                 select move.TargetSquare).Count() > 0;

        //if (board.TrySkipTurn())
        //{
        board.ForceSkipTurn();
        numPossibleMoves = (from move in board.GetLegalMoves() where (move.StartSquare.Name == curPiece.Square.Name) select move.TargetSquare).Count();
        isAttacked = board.SquareIsAttackedByOpponent(curPiece.Square);

        IEnumerable<int> canCapture = from move in board.GetLegalMoves(true)
                                      where (move.StartSquare.Name == curPiece.Square.Name)
                                      select pieceValues[(int)move.CapturePieceType] - curPieceValue * (board.SquareIsAttackedByOpponent(move.TargetSquare) ? 1 : 0);
        bestCapture = canCapture.Count() > 0 ? canCapture.Max() : 0;
        bestCapture = bestCapture < 0 ? 0 : bestCapture;


        board.UndoSkipTurn();

        if (skipSuccess)
        {
            board.UndoSkipTurn();
        }
        pieceEvaluation += (float)(numPossibleMoves - 1.25f) / 10f;
        pieceEvaluation += (attackedByWorseFigure ? -0.5f : 0f) * (float)curPieceValue;
        pieceEvaluation += ((isAttacked && !isDefended) ? -0.5f : 0f) * (float)curPieceValue;

        return pieceEvaluation;
    }

    float EvaluateAllFigures()
    {
        // Evaluate position of all figures
        float totalEvaluation = 0f;
        foreach (PieceList curList in board.GetAllPieceLists())
        {
            foreach (Piece curPiece in curList)
            {
                bool pieceTurn = curPiece.IsWhite == board.IsWhiteToMove;
                if (curPiece.IsWhite)
                {
                    totalEvaluation += EvaluateFigure(curPiece, pieceTurn);
                }
                else
                {
                    totalEvaluation -= EvaluateFigure(curPiece, pieceTurn);
                }
            }
        }
        return sign * totalEvaluation;
    }
}