namespace auto_Bot_136;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_136 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // List to contain all moves with same rating
        List<Move> similarBestMoves = new List<Move>();

        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        bool foundMove = false;
        int highestRating = -10000;


        foreach (Move move in allMoves)
        {
            // Check for mate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                foundMove = true;
                break;
            }

            if (board.GetFenString() == "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1" && move.TargetSquare.Name == "e4")
            {
                moveToPlay = move;
                foundMove = true;
                break;
            }

            // Skip if promotion to anything other then a queen
            if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen)
            {
                continue;
            }

            // Slip if move allowes opponent to win next turn
            if (IsSuicide(board, move))
            {
                continue;
            }

            // If none of above, give the move a rating
            int pointsToGet = pieceValues[(int)board.GetPiece(move.TargetSquare).PieceType];

            board.MakeMove(move);
            var tuple = GetMaxCaptureValue(board);
            int maxOpponentPoints = tuple.Item1;

            board.MakeMove(tuple.Item2);
            var tuple1 = GetMaxCaptureValue(board);
            int myNextMaxPoints = tuple1.Item1;

            board.MakeMove(tuple1.Item2);
            var tuple2 = GetMaxCaptureValue(board, true);
            int opponentNextMaxPoints = tuple2.Item1;


            board.MakeMove(tuple2.Item2);
            var tuple3 = GetMaxCaptureValue(board);
            int myNext2MaxPoints = tuple3.Item1;

            board.MakeMove(tuple3.Item2);
            var tuple4 = GetMaxCaptureValue(board, true);
            int opponentNext2MaxPoints = tuple4.Item1;


            board.UndoMove(tuple3.Item2);
            board.UndoMove(tuple2.Item2);
            board.UndoMove(tuple1.Item2);
            board.UndoMove(tuple.Item2);
            board.UndoMove(move);

            int rating = (pointsToGet - maxOpponentPoints) + (myNextMaxPoints - opponentNextMaxPoints) + (myNext2MaxPoints - opponentNext2MaxPoints);

            // Not prioritating to move king
            if (move.MovePieceType == PieceType.King && !board.IsInCheck())
            {
                rating -= 99;
            }

            if (CanOpponentPromote(board, move))
            {
                rating -= 900;
            }

            if (board.GetPiece(move.TargetSquare).PieceType == PieceType.Queen)
            {
                // rating += 200;
            }


            board.MakeMove(move);
            if (board.IsInCheck())
            {
                rating += 99;
            }
            board.UndoMove(move);


            if (IsDraw(board, move))
            {
                if (GetPlayerValue(board, board.IsWhiteToMove) >= GetPlayerValue(board, !board.IsWhiteToMove))
                {
                    rating -= 1500;
                }
                else
                {
                    rating += 1500;
                }
            }

            if (rating == highestRating)
            {
                similarBestMoves.Add(move);
            }

            if (rating > highestRating)
            {
                moveToPlay = move;
                highestRating = rating;

                similarBestMoves.Clear();
                similarBestMoves.Add(move);
            }
        }

        // Pick a random one of the best moves
        if (similarBestMoves.Count > 1 && !foundMove)
        {
            moveToPlay = similarBestMoves[rng.Next(similarBestMoves.Count)];
        }

        // DivertedConsole.Write(highestRating);

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool IsSuicide(Board board, Move move)
    {
        board.MakeMove(move);
        Move[] allMoves = board.GetLegalMoves();
        bool isSuicide = false;

        foreach (Move opponentMove in allMoves)
        {
            if (MoveIsCheckmate(board, opponentMove))
            {
                isSuicide = true;
                break;
            }
        }

        board.UndoMove(move);

        return isSuicide;

    }

    bool IsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }

    bool CanOpponentPromote(Board board, Move move)
    {
        Move[] allMoves = board.GetLegalMoves();
        bool canPromote = false;
        board.MakeMove(move);
        foreach (Move opponentMove in allMoves)
        {
            if (opponentMove.IsPromotion)
            {
                canPromote = true;
                break;
            }
        }
        board.UndoMove(move);
        return canPromote;
    }


    int GetPlayerValue(Board board, bool white)
    {
        int value = 0;

        PieceList[] allPieceLists = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieceLists)
        {
            if (pieceList.IsWhitePieceList == white)
            {
                int valueOfPieceType = pieceValues[(int)pieceList.TypeOfPieceInList];
                value += valueOfPieceType * pieceList.Count;
            }
        }

        return value;
    }


    Tuple<int, Move> GetMaxCaptureValue(Board board, bool last = false)
    {
        Move[] allMoves = board.GetLegalMoves();
        int highestValue = 0;
        Move bestMove = Move.NullMove;

        if (allMoves.Length > 0)
        {
            bestMove = allMoves[0];
        }

        foreach (Move opponentMove in allMoves)
        {
            int targetPieceValue = pieceValues[(int)board.GetPiece(opponentMove.TargetSquare).PieceType];

            if (last)
            {
                if (board.SquareIsAttackedByOpponent(opponentMove.TargetSquare))
                {
                    targetPieceValue -= pieceValues[(int)opponentMove.MovePieceType];
                }
            }

            if (targetPieceValue > highestValue)
            {
                bestMove = opponentMove;
                highestValue = targetPieceValue;
            }
        }

        return new Tuple<int, Move>(highestValue, bestMove);
    }
}