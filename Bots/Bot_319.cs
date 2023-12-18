namespace auto_Bot_319;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_319 : IChessBot
{
    // Variables to keep track of the current turn number and piece values
    int turnNum = 1;
    int[] pieceValues = { 0, 10, 30, 30, 60, 90, 10000 };

    // A 2D array representing the values of each square on the chessboard
    private int[,] squareValues = {
        {-5, -4, -3, -3, -3, -3, -4, -5},
        {-4, -2,  0,  0,  0,  0, -2, -4},
        {-3,  0,  1,  2,  2,  1,  0, -3},
        {-3,  1,  2,  3,  3,  2,  1, -3},
        {-3,  1,  2,  3,  3,  2,  1, -3},
        {-3,  0,  1,  2,  2,  1,  0, -3},
        {-4, -2,  0,  0,  0,  0, -2, -4},
        {-5, -4, -3, -3, -3, -3, -4, -5}};


    public Move Think(Board board, Timer timer)
    {

        int index = 0;

        bool isTargetDefended;
        bool isStartDefended;

        Move[] moves = board.GetLegalMoves();
        Move finalMove;
        int[] scores = new int[moves.Length];



        // Loop through each move in the 'moves' list.
        foreach (var currentMove in moves)
        {
            // Retrieve the captured piece and its value
            Piece capturedPiece = board.GetPiece(currentMove.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            // Retrieve the moving piece and its value
            Piece myPiece = board.GetPiece(currentMove.StartSquare);
            int myPieceValue = pieceValues[(int)myPiece.PieceType];
            bool myColor = myPiece.IsWhite;


            board.MakeMove(currentMove);
            isTargetDefended = board.SquareIsAttackedByOpponent(currentMove.TargetSquare);
            isStartDefended = board.SquareIsAttackedByOpponent(currentMove.StartSquare);

            // Check for check and adjust the score
            if (board.IsInCheck())
            {
                scores[index] += 12;
            }

            // Check for checkmate and adjust the score
            if (board.IsInCheckmate())
            {
                scores[index] += 10000;
            }

            // Analyze opponent's moves
            Move[] opponentMoves = board.GetLegalMoves();
            foreach (var opponentMove in opponentMoves)
            {
                board.MakeMove(opponentMove);
                if (board.IsInCheckmate())
                {
                    scores[index] -= 1000;
                }

                // Analyze moves ahead
                Move[] movesAhead = board.GetLegalMoves();
                foreach (var moveAhead in movesAhead)
                {
                    board.MakeMove(moveAhead);
                    if (board.IsInCheckmate())
                    {
                        scores[index] += 90 / movesAhead.Length;
                    }

                    board.UndoMove(moveAhead);

                }
                board.UndoMove(opponentMove);
            }

            board.UndoMove(currentMove);

            // Give the move a score based on captures;
            scores[index] += capturedPieceValue - myPieceValue * IsEaten(currentMove.TargetSquare, board);

            // Add score if promoting to a queen
            if (currentMove.IsPromotion && currentMove.PromotionPieceType == PieceType.Queen)
            {
                scores[index] += 90;
            }

            // Calculate score based on distance to the king and piece count
            scores[index] += DistCalcs(board, currentMove, myPiece);
            scores[index] += CountPieces(board, myColor);
            scores[index] -= CountPieces(board, !myColor);


            // Add score if it's late game and the target is defended
            if (turnNum > 9 && isTargetDefended)
            {
                scores[index] += 5;
            }


            // Increment index
            index++;

        }

        // Find the best move based on scores
        int bestMoveIndex = 0;
        for (int i = 1; i < scores.Length; i++)
        {
            if (scores[i] > scores[bestMoveIndex])
            {
                bestMoveIndex = i;
            }
        }

        finalMove = moves[bestMoveIndex];
        DivertedConsole.Write(finalMove + " " + scores[bestMoveIndex]);//#DEBUG
        DivertedConsole.Write("turn: " + turnNum + "\n");//#DEGUB
        turnNum++;

        return finalMove;
    }

    // Calculate score based on proximity to the king after a move
    int DistCalcs(Board board, Move currentMove, Piece myPiece)
    {
        int proximityToKingAftMove;
        int score = 0;
        if (turnNum > 9)
        {
            if (myPiece.IsPawn)
            {
                if (myPiece.IsWhite)
                {
                    score += currentMove.TargetSquare.Rank - 7;
                }
                else
                {
                    score += 7 - currentMove.TargetSquare.Rank;
                }
            }
        }

        // Add score based on proximity to the king
        if (!myPiece.IsKing)
        {
            proximityToKingAftMove = (14 - Math.Abs(currentMove.TargetSquare.Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank) - Math.Abs(currentMove.TargetSquare.File - board.GetKingSquare(!board.IsWhiteToMove).File)) * 3 / 2;
            proximityToKingAftMove += (Math.Abs(currentMove.StartSquare.Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank) + Math.Abs(currentMove.StartSquare.File - board.GetKingSquare(!board.IsWhiteToMove).File) - (Math.Abs(currentMove.StartSquare.Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank) - Math.Abs(currentMove.StartSquare.File - board.GetKingSquare(!board.IsWhiteToMove).File))) / 5 * 2;

            score += proximityToKingAftMove;

            if (myPiece.IsKnight)
            {
                score += squareValues[currentMove.TargetSquare.Rank, currentMove.TargetSquare.File];
            }
        }
        else
        {
            if (myPiece.IsWhite)
            {
                score += 7 - currentMove.TargetSquare.Rank;
            }
            else
            {
                score += currentMove.TargetSquare.Rank - 7;
            }
        }
        return score;
    }

    // Check if a square is attacked by an opponent
    int IsEaten(Square square, Board board)
    {
        if (board.SquareIsAttackedByOpponent(square))
        {
            return 1;
        }
        else
        {
            return 0;
        }
    }

    // Count the number of pieces on the board
    int CountPieces(Board board, bool IsWhite)
    {
        int score = 0;

        // Define a dictionary to store the piece values.
        Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int> {
        { PieceType.Pawn, 1 },
        { PieceType.Knight, 3 },
        { PieceType.Bishop, 3 },
        { PieceType.Rook, 5 },
        { PieceType.Queen, 9 }
    };

        // Iterate through the piece types and add their values to the score.
        foreach (var pieceType in pieceValues.Keys)
        {
            foreach (var piece in board.GetPieceList(pieceType, IsWhite))
            {
                score += pieceValues[pieceType];
            }
        }

        return score * 10;
    }



}