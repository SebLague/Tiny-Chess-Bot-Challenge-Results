namespace auto_Bot_575;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_575 : IChessBot
{
    Move bestMove = Move.NullMove;
    int myColor = 0;
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };


    public Move Think(Board board, Timer timer)
    {
        myColor = toColor(board.IsWhiteToMove);




        for (int depth = 1; depth <= 50; depth++)
        {
            int eval = MiniMax(board, depth, int.MinValue, int.MaxValue, true, 0);


            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 550)
            {
                break;
            }

        }



        return bestMove;

    }

    private int MiniMax(Board position, int depth, int alpha, int beta, bool maximizingPlayer, int ply)
    {
        int optionNum = position.GetLegalMoves().Length;
        if (depth <= 0 || optionNum == 0)
        {
            return ValueOfThe(position, ply);
        }
        int eval;

        Move[] possibilities = OrderedMovesOf(position);
        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            Move bestMoveFound = possibilities[0];
            foreach (Move currentMove in possibilities)
            {
                position.MakeMove(currentMove);
                eval = MiniMax(position, depth - 1, alpha, beta, false, ply + 1);
                position.UndoMove(currentMove);

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMoveFound = currentMove;
                }
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            bestMove = bestMoveFound;
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move currentMove in possibilities)
            {
                position.MakeMove(currentMove);
                eval = MiniMax(position, depth - 1, alpha, beta, true, ply + 1);
                position.UndoMove(currentMove);

                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            return minEval;
        }
    }

    private int ValueOfThe(Board position, int numOfMovesInTheFuture)
    {
        int turnColor = 2 * Convert.ToInt32(position.IsWhiteToMove) - 1;
        if (position.IsInCheckmate())
        {
            return (1_000_000 - numOfMovesInTheFuture) * -turnColor * myColor;
        }
        if (position.IsDraw())
        {
            return 0;
        }


        int material = 0;
        PieceList[] allPieces = position.GetAllPieceLists();
        foreach (PieceList pieces in allPieces)
        {
            int modifier = 2 * Convert.ToInt32(pieces.IsWhitePieceList) - 1;
            material += modifier * pieces.Count * pieceValues[(int)pieces.TypeOfPieceInList];
        }


        int mobility = 0;
        Move[] legalMoves = position.GetLegalMoves();
        Move[] legalResponses = { };
        mobility += turnColor * legalMoves.Length;
        if (mobility != 0)
        {
            position.MakeMove(legalMoves[0]);
            legalResponses = position.GetLegalMoves();
            mobility += -1 * turnColor * legalResponses.Length;
            position.UndoMove(legalMoves[0]);
        }

        int centerControl = 0;
        List<Square> squaresCovered = new List<Square>();
        Move[][] allMoves = { legalMoves, legalResponses };
        for (int i = 0; i < allMoves.Length; i++)
        {
            Move[] moveSet = allMoves[i];
            foreach (Move move in moveSet)
            {
                if ((int)move.MovePieceType < (int)PieceType.Rook)
                {
                    Square targetedSquare = move.TargetSquare;
                    squaresCovered.Add(move.StartSquare);
                    if ((int)move.MovePieceType != 1 /*Pawn*/ || move.IsCapture)
                    {
                        squaresCovered.Add(targetedSquare);
                    }
                    else if (Math.Abs(targetedSquare.Rank - move.StartSquare.Rank) == 1)
                    {
                        if (targetedSquare.File > 0)
                        {
                            squaresCovered.Add(new Square(targetedSquare.File - 1, targetedSquare.Rank));
                        }
                        if (targetedSquare.File < 7)
                        {
                            squaresCovered.Add(new Square(targetedSquare.File + 1, targetedSquare.Rank));
                        }
                    }
                }
            }
            foreach (Square square in squaresCovered)
            {
                if (Math.Abs(3.5 - square.File) < 1)
                {
                    centerControl += turnColor * (int)Math.Pow(-1, i);
                }
            }
            squaresCovered.Clear();
        }

        return myColor * 1000 * material + 100 * centerControl + mobility;
    }


    private Move[] OrderedMovesOf(Board position)
    {
        List<Move> captures = position.GetLegalMoves(capturesOnly: true).ToList();
        List<Move> nonCaptures = position.GetLegalMoves().ToList();
        List<Move> returnMoves = new List<Move>();

        //Below: Creates List Without Captures
        for (int moveIndex = 0; moveIndex < nonCaptures.Count; moveIndex++)
        {
            Move move = nonCaptures[moveIndex];
            if (move.IsCapture)
            {
                nonCaptures.Remove(move);
                moveIndex--;
            }
        }

        int captureNum = captures.Count;


        //Moves moves from captures to returnMoves prioritizeing MVV-LVA
        for (int nthBestMove = 0; nthBestMove < captureNum; nthBestMove++)
        {
            int highestScore = int.MinValue;
            Move highestScoringCapture = Move.NullMove;
            foreach (Move move in captures)
            {
                int moveScore = pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType]; //MVV-LVA
                if (moveScore > highestScore)
                {
                    highestScore = moveScore;
                    highestScoringCapture = move;
                }
            }
            if (highestScoringCapture.IsCapture)
            {
                returnMoves.Add(highestScoringCapture);
                captures.Remove(highestScoringCapture);
            }
        }

        //Adds prioritized moves to the rest
        returnMoves.AddRange(nonCaptures);

        //Below: Prioritizes Checks and filters out bad king moves
        List<Move> filteredKingMoves = new List<Move>();
        for (int i = 0; i < returnMoves.Count; i++)
        {
            Move currentMove = returnMoves[i];
            position.MakeMove(currentMove);

            if (position.IsInCheck())
            {
                returnMoves.Insert(0, currentMove);
                returnMoves.RemoveAt(i + 1);
            }
            else if (!position.IsInCheck() && !currentMove.IsCapture && !currentMove.IsCastles && currentMove.MovePieceType == PieceType.King)
            {
                filteredKingMoves.Add(currentMove);
            }
            position.UndoMove(currentMove);
        }

        foreach (Move move in filteredKingMoves)
        {
            returnMoves.Remove(move);
            returnMoves.Add(move);
        }



        return returnMoves.ToArray();
    }


    private int toColor(bool isWhite)
    {
        return 2 * Convert.ToInt32(isWhite) - 1;
    }
}