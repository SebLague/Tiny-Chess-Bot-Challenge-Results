namespace auto_Bot_562;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


// Code you want to measure




public class Bot_562 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static int[] pieceValues = { 0, 100, 300, 320, 500, 900, 2000 };
    int checkMateValue = 1234567;

    //Stopwatch[] stopwatches = new Stopwatch[7];
    //static Stopwatch stopwatchTopMoves, stopwatchEval, swSquareValue, swListAttackers, swMySquare, swEnemySquare, swSquareControl, swSquareValue1, swSquareValue2, swSquareValue3;

    public Move Think(Board board, Timer timer)
    {
        //for (int i = 0; i < stopwatches.Length; i++)
        //{
        //    stopwatches[i] = new Stopwatch();
        //}
        //stopwatchEval = new Stopwatch();
        //stopwatchTopMoves = new Stopwatch();
        //swSquareValue = new Stopwatch();
        //swListAttackers = new Stopwatch();
        //swMySquare = new Stopwatch();
        //swEnemySquare = new Stopwatch();
        //swSquareControl = new Stopwatch();
        //swSquareValue1 = new Stopwatch();
        //swSquareValue2 = new Stopwatch();
        //swSquareValue3 = new Stopwatch();

        Move ret = deepBestMove(board, 4, -checkMateValue, checkMateValue); //todo:balance   (3)
        //DivertedConsole.Write($"\n");
        //for (int i = 0; i < stopwatches.Length; i++)
        //{
        //    DivertedConsole.Write($"Elapsed time on depth {i}: {stopwatches[i].ElapsedMilliseconds} ms");
        //}
        //DivertedConsole.Write($"Elapsed time on eval: {stopwatchEval.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on topMoves: {stopwatchTopMoves.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on squareValue: {swSquareValue.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on listAttackers: {swListAttackers.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on mySquare: {swMySquare.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on enemySquare: {swEnemySquare.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on squareControl: {swSquareControl.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on swSquareValue1: {swSquareValue1.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on swSquareValue2: {swSquareValue2.ElapsedMilliseconds} ms");
        //DivertedConsole.Write($"Elapsed time on swSquareValue3: {swSquareValue3.ElapsedMilliseconds} ms");
        return ret;
    }

    Move deepBestMove(Board board, int depth, int alpha, int beta)
    {
        //stopwatches[depth].Start();
        //Move[] allMoves = board.GetLegalMoves();
        Move[] allMoves = topMoves(board, 3 * depth + 1); //todo: balance (2*depth +1)
        Move bestMove = allMoves[0];
        int bestMoveValue = -checkMateValue;

        foreach (Move move in allMoves)
        {
            int moveValue = deepValue(board, move, depth, alpha, beta);
            if (moveValue > bestMoveValue)
            {
                bestMove = move;
                bestMoveValue = moveValue;
                if (bestMoveValue > beta) break;
                if (bestMoveValue > alpha) alpha = bestMoveValue;
            }
        }
        //stopwatches[depth].Stop();        
        return bestMove;
    }
    Move[] topMoves(Board board, int nMoves)
    {
        //stopwatchTopMoves.Start();
        Move[] ret = board.GetLegalMoves().OrderByDescending(move => deepValue(board, move, 0, -checkMateValue, checkMateValue)).Take(nMoves).ToArray();
        //stopwatchTopMoves.Stop();
        return ret;

        //Move[] captureMoves = board.GetLegalMoves(true);        
        //Move[] allMoves = board.GetLegalMoves();       
        //int nNonCaptureMoves = Math.Max(nMoves - captureMoves.Length, 2);
        //Move[] topMoves = allMoves.OrderByDescending(move => deepValue(board, move, 0, -checkMateValue, checkMateValue)).Take(nNonCaptureMoves).ToArray();
        //return topMoves.Concat(captureMoves).ToArray();
        ////return allMoves;
    }
    int deepValue(Board board, Move move, int depth, int alpha, int beta)
    {
        board.MakeMove(move);
        int value = 0;
        if (board.IsInCheckmate())
        {
            value = checkMateValue;
        }
        else if (!board.IsDraw()) //draw has value 0
        {
            if (depth == 0)
            {
                //stopwatchEval.Start();
                int[] valuePerSquare = new int[64];
                //swListAttackers.Start();
                List<int>[] myAttackerValues = new List<int>[64];
                List<int>[] enemyAttackerValues = new List<int>[64];
                for (int i = 0; i < 64; i++)
                {
                    myAttackerValues[i] = new List<int>();
                    enemyAttackerValues[i] = new List<int>();
                }
                ulong allPieces = board.AllPiecesBitboard;
                while (allPieces != 0) // Continue until all bits are cleared
                {
                    Square otherPieceSquare = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref allPieces));
                    Piece otherPiece = board.GetPiece(otherPieceSquare);
                    ulong attacks = BitboardHelper.GetPieceAttacks(otherPiece.PieceType, otherPieceSquare, board, otherPiece.IsWhite);
                    while (attacks != 0)
                    {
                        int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref attacks);
                        if (otherPiece.IsWhite == !board.IsWhiteToMove)
                        {
                            myAttackerValues[squareIndex].Add(pieceValues[(int)otherPiece.PieceType]);
                        }
                        else
                        {
                            enemyAttackerValues[squareIndex].Add(pieceValues[(int)otherPiece.PieceType]);
                        }
                    }
                }
                //swListAttackers.Stop();

                for (int i = 0; i < 64; i++)
                {
                    int sv = squareValue(board, new Square(i), myAttackerValues[i], enemyAttackerValues[i], !board.IsWhiteToMove);
                    value += sv;
                    valuePerSquare[i] = sv;
                }
                //stopwatchEval.Stop();
            }
            else
            {
                value = -deepValue(board, deepBestMove(board, depth - 1, -beta, -alpha), depth - 1, -beta, -alpha);//todo: eh menos mesmo?
            }

        }

        board.UndoMove(move);
        return value;
    }

    int squareValue(Board board, Square square, List<int> myAttackerValues, List<int> enemyAttackerValues, bool amIWhite)
    {
        //swSquareValue.Start();
        //swSquareValue1.Start();
        Piece pieceOnSquare = board.GetPiece(square);
        enemyAttackerValues.Sort();
        myAttackerValues.Sort();
        //DivertedConsole.Write("square = " + square + " Value = " + value);
        int valueOfPieceOnSquare = pieceValues[(int)pieceOnSquare.PieceType] * (pieceOnSquare.IsWhite == amIWhite ? 1 : -1);
        //TODO: king safety
        //TODO: mate with king and queen
        //TODO: develop minors pieces before queen
        int ret;
        //swSquareValue1.Stop();
        if (valueOfPieceOnSquare == 0)
        {

            ret = valueOfSquareControl(square, board, myAttackerValues.Count - enemyAttackerValues.Count);

        }
        else if (valueOfPieceOnSquare > 0)
        {
            //swMySquare.Start();
            ret = valueOfMySquare(square, board, valueOfPieceOnSquare, enemyAttackerValues, myAttackerValues);
            //swMySquare.Stop();
        }
        else
        {
            //swEnemySquare.Start();
            ret = valueOfEnemySquare(square, board, valueOfPieceOnSquare, myAttackerValues, enemyAttackerValues);
            //swEnemySquare.Stop();
        }
        //swSquareValue.Stop();
        return ret;
        //int atkCount = attackedSquaresCountFrom(square, board);
        //return atkCount * (piece.IsWhite == isWhite ? 1 : -1);
    }
    static int valueOfMySquare(Square square, Board board, int pieceValue, List<int> attackerValues, List<int> defenderValues)
    {
        if (attackerValues.Count == 0)
        {
            if (pieceValue == pieceValues[1]) //push pawns
            {
                double promotionRank = board.GetPiece(square).IsWhite ? 6.9 : 0.1;
                pieceValue += (int)(80 / Math.Abs(square.Rank - promotionRank) - 20);
            }
            return pieceValue + valueOfSquareControl(square, board, defenderValues.Count);
        }
        int stayValue = pieceValue + valueOfSquareControl(square, board, defenderValues.Count - attackerValues.Count);
        int takerValue = attackerValues[0];
        attackerValues.RemoveAt(0);
        int takeValue = takerValue - valueOfMySquare(square, board, takerValue, defenderValues, attackerValues);
        return Math.Min(stayValue, takeValue);
    }
    static int valueOfEnemySquare(Square square, Board board, int pieceValue, List<int> attackerValues, List<int> defenderValues)
    {
        return pieceValue + valueOfSquareControl(square, board, attackerValues.Count - defenderValues.Count);
        //todo: handle forks. (Guarda valor das capturas possiveis e adiciona o valor da segunda melhor)
    }
    static int valueOfSquareControl(Square square, Board board, int control)
    {
        //return 5 * control;
        //too much code, too little value takes 33% of the time.
        //swSquareControl.Start();
        Square whiteKingSquare = board.GetKingSquare(true);
        Square blackKingSquare = board.GetKingSquare(false);
        double distWhiteKing = size(square.File - whiteKingSquare.File, square.Rank - whiteKingSquare.Rank);
        double distBlackKing = size(square.File - blackKingSquare.File, square.Rank - blackKingSquare.Rank);
        double distCenter = size(3.5 - square.File - 3.5, square.Rank - 3.5);
        double squareValue = 6 - distCenter + 18 / Math.Pow(2, distWhiteKing / 2) + 18 / Math.Pow(2, distBlackKing / 2);
        double controlValue = Math.Sqrt(Math.Abs(control)) * ((control > 0) ? 1 : -1);
        //swSquareControl.Stop();
        return (int)(squareValue * controlValue * 2);

        //return 5 * control;
    }

    static double size(double a, double b)
    {
        return Math.Abs(a) + Math.Abs(b);
        // return Math.Sqrt(Math.Pow(a, 2) + Math.Pow(b, 2));
    }

}