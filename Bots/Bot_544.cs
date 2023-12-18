namespace auto_Bot_544;
using ChessChallenge.API;
using System;
// using System.Collections.Generic;

public class Bot_544 : IChessBot
{
    int[] capturedPieceValueList = { 0, 1, 3, 3, 5, 9, 100 };
    int[] allPieceValues = { 1, 3, 3, 5, 9, 100, -1, -3, -3, -5, -9, -100 };
    const int infinity = 50000;

    public Move Think(Board board, Timer timer)
    {
        // Move[] allMoves = board.GetLegalMoves();
        // Random rng = new();
        // Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        // double evaluation = 0, highestEvaluation = -20000;
        // int whiteToMove = board.IsWhiteToMove? 1 : -1;

        // foreach (Move move in allMoves)
        // {
        //     evaluation = EvaluatePosition(board, move, whiteToMove);

        //     if (evaluation >= highestEvaluation)
        //     {
        //         moveToPlay = move;
        //         highestEvaluation = evaluation;
        //     }
        // }

        // DivertedConsole.Write("I choose to play");
        // DivertedConsole.Write(moveToPlay);
        // DivertedConsole.Write("With eval = " + evaluation);

        Move alphaBetaPruningMove = GameTreeSearch(board);

        return alphaBetaPruningMove;
    }


    double EvaluatePosition(Board board, Move move, int whiteToMove)
    {
        Piece capturedPiece;
        int capturedPieceValue;

        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        if (isMate == true)
        {
            board.UndoMove(move);
            return (infinity + 1);
        }

        double evaluation = 0, oneMoveTactic = 0;
        Move[] nextTurnLegalMoves;
        nextTurnLegalMoves = board.GetLegalMoves();

        foreach (Move nextMove in nextTurnLegalMoves)
        {
            capturedPiece = board.GetPiece(nextMove.TargetSquare);
            capturedPieceValue = capturedPieceValueList[(int)capturedPiece.PieceType];
            if (capturedPieceValue >= oneMoveTactic)
            {
                oneMoveTactic = capturedPieceValue;
            }

            if (MoveIsCheckmate(board, nextMove) == true)
            {
                // board.UndoMove(move);
                evaluation -= 6;
            }
        }
        oneMoveTactic = (oneMoveTactic * -0.8);

        board.ForceSkipTurn();
        nextTurnLegalMoves = board.GetLegalMoves();
        double possibleMoves = nextTurnLegalMoves.Length * 0.05;

        foreach (Move nextMove in nextTurnLegalMoves)
        {
            if (MoveIsCheckmate(board, nextMove) == true)
            {
                DivertedConsole.Write("Xeque!");
                evaluation += 6;
            }
        }
        board.UndoSkipTurn();

        PieceList[] pieceList = board.GetAllPieceLists();
        int sumPieceValues = 0;
        for (int i = 0; i < pieceList.Length; i++)
        {
            sumPieceValues += pieceList[i].Count * allPieceValues[i];
        }
        sumPieceValues *= whiteToMove;

        DivertedConsole.Write(move);
        evaluation = (oneMoveTactic + sumPieceValues + possibleMoves);
        DivertedConsole.Write("Evaluating: " + oneMoveTactic + ";" + sumPieceValues + ";" + possibleMoves);

        // evaluation *= whiteToMove;
        DivertedConsole.Write(evaluation);

        board.UndoMove(move);
        return evaluation;
    }

    // Alpha-Beta Pruning Development

    Move GameTreeSearch(Board board)
    {
        Tuple<Move, double> bestMoveAndEvaluation = AlphaBetaPruning(board, 3, -infinity, infinity, Move.NullMove);
        DivertedConsole.Write("GameTreeSearchResult: ");
        DivertedConsole.Write(bestMoveAndEvaluation.Item1);
        DivertedConsole.Write("With eval = " + bestMoveAndEvaluation.Item2);
        return bestMoveAndEvaluation.Item1;
    }

    // Move[] GetOkMoves(Board board)
    // {
    //     Move[] allMoves = board.GetLegalMoves();
    //     int allMovesLength = allMoves.Length;
    //     int okMovesLength = 1 + (allMovesLength/4);
    //     List<Move> okMoves = new List<Move>();

    //     int i = 0;
    //     while (i < allMovesLength || okMoves.Count < allMovesLength){
    //         DivertedConsole.Write("aqui");
    //         foreach (Move move in allMoves)
    //         {
    //             board.MakeMove(move);
    //             double eval = SimpleBoardEvaluation(board);
    //             if(eval >= 0)
    //                 okMoves.Add(move);
    //             board.UndoMove(move);
    //         }
    //         DivertedConsole.Write("aqui");
    //     }
    //     if(okMoves.Count == 0)
    //         return allMoves;

    //     return okMoves.ToArray();
    // }

    // This implements the fail-hard variation of alpha-beta pruning
    // lastMove is necessary to create the move/evaluation 
    // we don't need a maximizing player because Board has the variable IsWhiteToMove
    Tuple<Move, double> AlphaBetaPruning(Board board, int depth, double alpha, double beta, Move lastMove)
    {
        if (depth == 0 || board.IsInCheckmate())
            return new Tuple<Move, double>(lastMove, SimpleBoardEvaluation(board));

        Move[] allMoves;
        allMoves = board.GetLegalMoves();

        double value = board.IsWhiteToMove ? -infinity : infinity;
        Move bestMove = lastMove;

        if (board.IsWhiteToMove)
        {
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                Tuple<Move, double> childEvaluation = AlphaBetaPruning(board, depth - 1, alpha, beta, move);
                board.UndoMove(move);

                if (childEvaluation.Item2 > value)
                {
                    bestMove = move;
                    value = childEvaluation.Item2;
                }

                if (value > beta)
                {
                    // beta cutoff
                    DivertedConsole.Write("CUTTOFF");
                    break;
                }
                alpha = value > alpha ? value : alpha;
            }
        }
        else
        {
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                Tuple<Move, double> childEvaluation = AlphaBetaPruning(board, depth - 1, alpha, beta, move);
                board.UndoMove(move);

                if (childEvaluation.Item2 < value)
                {
                    bestMove = move;
                    value = childEvaluation.Item2;
                }

                if (value < alpha)
                {
                    // alpha cutoff
                    DivertedConsole.Write("CUTTOFF");
                    break;
                }
                beta = value < beta ? value : beta;
            }
        }

        return new Tuple<Move, double>(bestMove, value);
    }

    double SimpleBoardEvaluation(Board board)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        int sumPiecesValues = 0;

        int whiteToMove = board.IsWhiteToMove ? -1 : 1;
        if (board.IsInCheckmate())
        {
            return 150 * whiteToMove;
        }

        int legalMoves = board.GetLegalMoves().Length;

        for (int i = 0; i < pieces.Length; i++)
        {
            sumPiecesValues += pieces[i].Count * allPieceValues[i];
        }

        double finalEval = sumPiecesValues + legalMoves * 0.1;

        if (board.IsDraw())
        {
            if (finalEval < -5)
                return 150 * whiteToMove;
            // else if(finalEval < -5)
            //     return 150 * whiteToMove;
        }
        //DivertedConsole.Write("Simple Evaluation = " + sumPiecesValues);

        return finalEval;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}