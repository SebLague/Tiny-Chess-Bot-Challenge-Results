namespace auto_Bot_84;
using ChessChallenge.API;
using System;

public class Bot_84 : IChessBot
{
    // Setup Random
    Random rng = new Random();
    // Assign Piece Values
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    // Control No. Iterations for game prediction.
    // 2 seems to be the most optimum value agains EvilBot.cs, as higher iterations cause the bot to play moves seemingly without reason.
    public int ITERATIONS = 2;


    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        int highestRankedScore = 0;
        Move moveToMake = allMoves[rng.Next(allMoves.Length)];

        foreach (Move move in allMoves)
        {
            // Check for Checkmate (highest priority)
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            // Avoid stalemate moves and skip moves that result in them.
            // This is seemingly only useful only when winning. When losing, the bot wont look for moves that will result in a stalement. This could lead to a loss.
            if (MoveIsStalemate(board, move))
            {
                continue;
            }

            // Prioritze promoting pieces into queens over other moves.
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
            {
                return move;
            }


            // Detect for endgame conditions. "Endgame" happens when only the opponents King remains.
            if (isEndgame(board))
            {


                // Aim to get pawns to the end so that they can be promoted into queens.
                if (move.MovePieceType == PieceType.Pawn && distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) > 1)
                {
                    return move;
                }


                // If the piece is a king, try and get 2 spaces away from the opposing king. This helps setup the conditions for a King & Rook Checkmate.
                else if (move.MovePieceType == PieceType.King && distance(board.GetKingSquare(true), board.GetKingSquare(false)) != 2)
                {
                    if (distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) < distance(board.GetKingSquare(board.IsWhiteToMove), board.GetKingSquare(!board.IsWhiteToMove)))
                    {
                        return move;
                    }
                }

                // If the King is 2 spaces away, move the Queen or Rook onto the same axis as the opposing king, to push it back.
                else if ((move.MovePieceType == PieceType.Queen || move.MovePieceType == PieceType.Rook) && distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) > 1)
                {
                    // If kings are facing
                    if (distance(board.GetKingSquare(board.IsWhiteToMove), board.GetKingSquare(!board.IsWhiteToMove)) == 2)
                    {
                        if (move.TargetSquare.Rank - board.GetKingSquare(board.IsWhiteToMove).Rank == 2 && move.TargetSquare.Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank == 0
                            ||
                            move.TargetSquare.File - board.GetKingSquare(board.IsWhiteToMove).File == 2 && move.TargetSquare.File - board.GetKingSquare(!board.IsWhiteToMove).File == 0
                            )
                        {
                            return move;
                        }
                    }
                }

                // After a few iterations of King & Queen/Rook moving, this should result in Checkmate.
            }
            else
            {
                // Core Movement
                // This is taken from EvilBot.cs, but edited so that it predicts future moves.
                // This is done with a score system.
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
                int totalScore = predict(board, move, capturedPieceValue, ITERATIONS);

                if (totalScore > highestRankedScore)
                {
                    highestRankedScore = capturedPieceValue;
                    moveToMake = move;
                }
            }
        }
        return moveToMake;
    }


    //
    int predict(Board b, Move m, int score, int iter)
    {
        int adjust = 0;
        // Catch for when the iterations hit zero. Don't add anything, just return the score.
        if (iter > 0)
        {
            // Make the move and then find the next player move.
            b.MakeMove(m);
            Move nm = recursionLoop(b);

            // This case catches when there are no more avaliable moves, meaning Checkmate or Stalemate.
            // I was too lazy to give scores depending on if it was a good or bad ending, so I just defaulted it to being negative.
            if (nm == Move.NullMove)
            {
                adjust -= 500;
            }
            else
            {
                // This is code is taken again from EvilBot.cs
                Piece capturedPiece = b.GetPiece(nm.TargetSquare);
                adjust = pieceValues[(int)capturedPiece.PieceType];
            }

            // Because MakeMove() flips the player, I wanted to have the opponents scores take away from the overall score.
            // That way when MyBot is predicting moves, it'll take into account the pieces taken and the pieces lost.
            if (iter % 2 == 0)
            {
                adjust *= -1;
            }


            // The change in score becomes weaker the more iterations you go. Idk why but this felt right and it worked out.
            // There's probably some valid mathematical reason why but im too dumb to figure it out.
            score += (int)(adjust * 0.5);

            // Add the previous iteration to it.
            score += predict(b, nm, score, iter - 1);
            // Undo the board move. This happens after all the iterating.
            b.UndoMove(m);
        }
        // Return the final score.s
        return score;
    }


    // The recursionLoop code is the same as the code for Think(), just without doing recursion in the Core Movement section.
    // when recursion is added, MyBot spends all its time predicting every possible move in the game, resulting in a forfeit.
    Move recursionLoop(Board board)
    {
        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0)
        {
            return Move.NullMove;
        }
        int highestRankedScore = 0;
        Move moveToMake = allMoves[rng.Next(allMoves.Length)];

        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            if (MoveIsStalemate(board, move))
            {
                continue;
            }

            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
            {
                return move;
            }

            if (isEndgame(board))
            {
                if (move.MovePieceType == PieceType.Pawn && distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) > 1)
                {
                    return move;
                }

                else if (move.MovePieceType == PieceType.King)
                {
                    if (distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) < distance(board.GetKingSquare(board.IsWhiteToMove), board.GetKingSquare(!board.IsWhiteToMove)))
                    {
                        return move;
                    }
                }
                else if ((move.MovePieceType == PieceType.Queen || move.MovePieceType == PieceType.Rook) && distance(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove)) > 1)
                {
                    if (distance(board.GetKingSquare(board.IsWhiteToMove), board.GetKingSquare(!board.IsWhiteToMove)) == 2)
                    {
                        if (move.TargetSquare.Rank - board.GetKingSquare(board.IsWhiteToMove).Rank == 1 && move.TargetSquare.Rank - board.GetKingSquare(!board.IsWhiteToMove).Rank == 1
                        ||
                        move.TargetSquare.File - board.GetKingSquare(board.IsWhiteToMove).File == 1 && move.TargetSquare.File - board.GetKingSquare(!board.IsWhiteToMove).File == 1
                            )
                        {
                            return move;
                        }
                    }
                }
            }
            else
            {
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                if (capturedPieceValue > highestRankedScore)
                {
                    highestRankedScore = capturedPieceValue;
                    moveToMake = move;
                }
            }
        }
        return moveToMake;
    }

    // "Endgame" is detected when only the opponents King remains.
    bool isEndgame(Board board)
    {
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            if (board.IsWhiteToMove != pieceList.IsWhitePieceList)
            {
                if (pieceList.TypeOfPieceInList != PieceType.King && pieceList.Count > 0)
                {
                    return false;
                }
            }
        }

        return true;
    }

    // This calculates the distance between 2 squares.
    int distance(Square a, Square b)
    {
        return Math.Max(Math.Abs(a.Rank - b.Rank), Math.Abs(a.File - b.File));
    }


    // This tests to see if the given move results in checkmate.
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // This tests to see if the given move results in stalemate.
    bool MoveIsStalemate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }
}