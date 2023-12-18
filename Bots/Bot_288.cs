namespace auto_Bot_288;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_288 : IChessBot
{
    bool botIsWhite;
    int[] pieceWeights;
    int maxDepth = 12;
    int runningEvaluation;
    int valuesOfCaptured;
    int capturesRequiredForLategame = 35;
    int runningEvaluationEarlyGame;
    int runningEvaluationLateGame;
    bool min;
    int[] pieceCaptureValues;
    Board universalBoard;

    public Move Think(Board board, Timer timer)
    {
        universalBoard = board;
        if (pieceWeights == null)
        {
            pieceCaptureValues = new int[] { 0, 1, 3, 3, 5, 9 };
            pieceWeights = new int[768];

            //create piece weights
            for (int i = 0; i < 385; i += 320)
            {
                for (int rank = 0; rank < 8; rank++)
                {
                    int rankOffCenter = Math.Abs(3 - rank) - rank % 2;
                    for (int file = 0; file < 8; file++)
                    {
                        int fileOffCenter = Math.Abs(3 - file) - rank % 2;
                        //a pawn is worth 100 (90 + 10 from its starting rank)
                        pieceWeights[i] = 90
                            //it is worth 10 more for each step forward it has taken
                            + rank * 10
                            //if it is advanced and it is one of the two center pawns, it is worth + 31
                            + (fileOffCenter < 1 && rank > 1 ? 31 : 0);

                        //a knight is worth 340 at the center of the board
                        pieceWeights[i + 64] = 340
                            //it is worth 10 less for each step away from the center it takes
                            - (rankOffCenter + fileOffCenter) * 10;

                        //a bishop is worth 330 its starting space, 340 otherwise
                        pieceWeights[i + 128] = rank == 0 && (file == 2 || file == 5) ? 330 : 340;

                        //a rook is worth 600 if it can get to the opponent's back rank
                        pieceWeights[i + 192] = rank == 6 ? 600 :
                            //otherwise if it is not on the starting rank it is worth 500
                            rank != 0 ? 500 :
                            // if it is on its starting square, it is worth 510
                            fileOffCenter == 3 ? 510 :
                            //to encourage castling, castle destination squares are worth 550 while the remaining file 0 squares are worth 490
                            file == 3 || file == 5 ? 550 : 490;

                        //a queen is worth 900 on all squares.  Its nature lends itself to rapid, massive point swings from captures, so the AI needs no hints
                        pieceWeights[i + 256] = 900;

                        //a king is worth 9000
                        pieceWeights[i + 320] = 9000
                            //a king that has moved away from the starting ranks is penalized 100 as those tiles are often not safe
                            - rank * 100
                            //a king that has moved to the left or right 1 is penalized 80, this is to discourage sacrificing castling rights
                            - (file == 3 || file == 5 ? 80 : 0);

                        if (i > 383)
                            pieceWeights[i + 320] = 9000;
                        i++;
                    }
                }
            }
        }
        var moves = board.GetLegalMoves();
        botIsWhite = board.IsWhiteToMove;

        //calculate milliseconds allowed
        //this is to allow my bot to run on computers that are more or less powerful than my dev machine
        //as it runs out of time, it will interpolate between 1 second of thought and about a 1/5th of a second

        int millisecondsAllowed = 170 + timer.MillisecondsRemaining / 72;
        //int startingEvaluation = EvaluatePosition(board);

        //produce evaluation score
        var moveEvaluations = new int[moves.Length];
        var previousQuinescenceTable = new Move[maxDepth];
        var quinescenceTable = board.GetLegalMoves(true);
        int gameLength = board.GameMoveHistory.Length;
        if (gameLength > 0)
        {
            Move previousOpponentMove = board.GameMoveHistory[gameLength - 1];
            board.UndoMove(previousOpponentMove);
            min = true;
            MakeMoveAndUpdateEvaluation(previousOpponentMove);
        }
        min = false;
        for (int i = 1; i < maxDepth; i++)
        {
            int ii = 0;
            foreach (Move move in moves)
            {
                MakeMoveAndUpdateEvaluation(move);
                int evaluation = RecursiveAssessMove(-999999999, 999999999, i, 0, quinescenceTable, previousQuinescenceTable);
                //this is so that the list will sort from highest evaluation to lowest
                moveEvaluations[ii] = -evaluation;
                UndoMoveAndResetEvaluation(move);
                ii++;
                if (timer.MillisecondsElapsedThisTurn > millisecondsAllowed)
                    break;
            }
            if (timer.MillisecondsElapsedThisTurn > millisecondsAllowed)
                break;
            Array.Sort(moveEvaluations, moves);
        }
        MakeMoveAndUpdateEvaluation(moves[0]);
        return moves[0];
    }

    int RecursiveAssessMove(int alpha, int beta, int depthRemainding, int depth, Move[] quinescenceTable, Move[] previousQuinescenseTable)
    {
        int finalEvaluation = -99999999;
        if (min)
            finalEvaluation = 999999999;
        var moves = universalBoard.GetLegalMoves();
        if (moves.Length > 0)
        {
            var quinescenceCandidates = universalBoard.GetLegalMoves(true);
            if (depthRemainding < 1)
            {
                int bestEvaluation = 0;
                foreach (Move move in quinescenceCandidates)
                {
                    if (previousQuinescenseTable.Contains(move))
                        continue;
                    int evaluation = GetFinalEvaluationChange(move);
                    if (min)
                        bestEvaluation = Math.Min(evaluation, bestEvaluation);
                    else
                        bestEvaluation = Math.Max(evaluation, bestEvaluation);
                }
                return runningEvaluation + bestEvaluation;
            }

            var moveEvaluations = new int[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                moveEvaluations[i] = -(runningEvaluation + GetFinalEvaluationChange(moves[i]));
                if (min)
                    moveEvaluations[i] *= -1;
            }
            Array.Sort(moveEvaluations, moves);

            foreach (Move move in moves)
            {
                MakeMoveAndUpdateEvaluation(move);

                int evaluation = RecursiveAssessMove(alpha, beta, depthRemainding - 1, depth + 1, quinescenceCandidates, quinescenceTable);

                UndoMoveAndResetEvaluation(move);
                if (min)
                {
                    finalEvaluation = Math.Min(finalEvaluation, evaluation);
                    beta = Math.Min(finalEvaluation, beta);
                }
                else
                {
                    finalEvaluation = Math.Max(finalEvaluation, evaluation);
                    alpha = Math.Max(finalEvaluation, alpha);
                }
                if (beta <= alpha)
                    break;
            }
        }
        else
        {
            if (universalBoard.IsInCheckmate())
            {
                if (universalBoard.IsWhiteToMove != botIsWhite)
                    return 999999999;
                else
                    return -999999999;
            }
            else if (universalBoard.IsInStalemate())
                return 0;
        }
        return finalEvaluation;
    }

    //this is what I expect to be the most unique thing about my bot
    //rather than evaluating the entire board, it calculates the change in evaluation each move causes
    //that change is then used to sort moves for move ordering
    int UpdateRunningEvaluation(Move move, bool earlyGame)
    {
        int movePieceType = (int)move.MovePieceType;
        Square startSquare = move.StartSquare;
        Square targetSquare = move.TargetSquare;
        bool isWhite = min != botIsWhite;
        int evaluationChange = -GetPieceValue(movePieceType, isWhite, startSquare, earlyGame);
        evaluationChange += move.IsPromotion ?
            GetPieceValue((int)move.PromotionPieceType, isWhite, targetSquare, earlyGame) :
            GetPieceValue(movePieceType, isWhite, targetSquare, earlyGame);
        if (move.IsCastles)
            //this is hardcoded for reasons that were really hard to troubleshoot
            evaluationChange = 200;
        else
            evaluationChange += move.IsEnPassant ?
                GetPieceValue((int)move.CapturePieceType, !isWhite, new Square(targetSquare.File, startSquare.Rank), earlyGame) :
                GetPieceValue((int)move.CapturePieceType, !isWhite, targetSquare, earlyGame);
        if (min)
            evaluationChange = -evaluationChange;
        return evaluationChange;
    }

    void MakeMoveAndUpdateEvaluation(Move move)
    {
        valuesOfCaptured += pieceCaptureValues[(int)move.CapturePieceType];
        universalBoard.MakeMove(move);
        runningEvaluationEarlyGame += UpdateRunningEvaluation(move, true);
        runningEvaluationLateGame += UpdateRunningEvaluation(move, false);
        runningEvaluation = GetInterpolatedRunningEvaluation();
        min = !min;
    }

    void UndoMoveAndResetEvaluation(Move move)
    {
        min = !min;
        universalBoard.UndoMove(move);
        runningEvaluationEarlyGame -= UpdateRunningEvaluation(move, true);
        runningEvaluationLateGame -= UpdateRunningEvaluation(move, false);
        valuesOfCaptured -= pieceCaptureValues[(int)move.CapturePieceType];
        runningEvaluation = GetInterpolatedRunningEvaluation();
    }

    int GetFinalEvaluationChange(Move move)
    {
        int oldRunningEvaluation = runningEvaluation;
        MakeMoveAndUpdateEvaluation(move);
        int finalEvaluationChange = GetInterpolatedRunningEvaluation() - oldRunningEvaluation;
        UndoMoveAndResetEvaluation(move);
        return finalEvaluationChange;
    }

    int GetInterpolatedRunningEvaluation()
    {
        return runningEvaluationEarlyGame * (capturesRequiredForLategame - valuesOfCaptured) / capturesRequiredForLategame
            + runningEvaluationLateGame * valuesOfCaptured / capturesRequiredForLategame;
    }

    int GetPieceValue(int pieceType, bool isWhite, Square pieceSquare, bool earlyGame)
    {
        if (pieceType == 0)
            return 0;
        int squareCode = pieceSquare.Index;
        //black's weights are assumed to be the same as white's but with a flipped board
        if (!isWhite)
            squareCode = pieceSquare.File + (7 - pieceSquare.Rank) * 8;
        squareCode += 64 * (pieceType - 1);
        if (!earlyGame)
            squareCode += 384;
        return pieceWeights[squareCode];
    }
}