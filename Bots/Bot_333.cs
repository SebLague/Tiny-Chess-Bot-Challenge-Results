namespace auto_Bot_333;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_333 : IChessBot
{
    int maxDepth = 5;
    int[] pieceValues = { 0, 100, 350, 350, 600, 1200, 5000 };

    public Move Think(Board board, Timer timer)
    {
        Tuple<Move, int> choice = BestMove(board, timer, 1, false, false);
        return choice.Item1;
    }

    // Compare moves for sorting
    int rankMoveForSorting(Board board, Move move)
    {
        // Capturing high value pieces is often a good place to start
        int sortValue = pieceEval(board, board.GetPiece(move.TargetSquare));

        // Saving a piece is often good
        if (board.SquareIsAttackedByOpponent(move.StartSquare))
        {
            sortValue += pieceEval(board, board.GetPiece(move.StartSquare));
        }

        return sortValue;
    }

    public Tuple<Move, int> BestMove(Board board, Timer timer, int depth, bool previousMoveWasCheck, bool previousMoveWasPieceCapture)
    {
        // Get and sort moves for best candidates first
        Move[] allMoves = board.GetLegalMoves();
        Move[] sortedMoves = allMoves.OrderByDescending(thisMove => rankMoveForSorting(board, thisMove)).ToArray();

        // Prepare to do the loop de loop
        //thinks[depth - 1]++;
        Move moveToPlay = allMoves[0];
        bool whiteToMove = board.IsWhiteToMove;
        int bestEvaluation = whiteToMove ? -99999 : 99999;

        foreach (Move move in sortedMoves)
        {
            board.MakeMove(move);
            PieceList[] pieces = board.GetAllPieceLists();
            int moveEvaluation = boardEval(board, pieces, depth);
            Piece movingPiece = board.GetPiece(move.StartSquare);
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            bool pieceCapture = move.IsCapture && !capturedPiece.IsPawn;

            // Consider the future carefully
            if (
                depth <= maxDepth && !board.IsDraw() && !board.IsInCheckmate() &&
                (depth < 3 || timer.MillisecondsRemaining > 8000) &&
                (depth < 3 || (whiteToMove ? moveEvaluation + 200 > bestEvaluation : moveEvaluation - 200 < bestEvaluation)) &&
                (depth < 3 || board.IsInCheck() || previousMoveWasCheck || pieceCapture || (move.IsCapture && previousMoveWasPieceCapture)) &&
                (depth < 4 || timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 30) &&
                (depth < 4 || (whiteToMove ? moveEvaluation > bestEvaluation : moveEvaluation < bestEvaluation))
                )
            {
                moveEvaluation = BestMove(board, timer, depth + 1, board.IsInCheck(), pieceCapture).Item2;
            }

            // Castling is outside evaluation
            // Castle, and preserve castling for first few moves
            if (move.IsCastles)
            {
                moveEvaluation += whiteToMove ? 80 : -80;
            }
            // Avoid moving king or rooks early
            else if (board.PlyCount <= 16 && movingPiece.IsKing || movingPiece.IsRook)
            {
                moveEvaluation -= whiteToMove ? 10 : -10;
            }

            // Use the best outcome
            if (whiteToMove ? moveEvaluation > bestEvaluation : moveEvaluation < bestEvaluation)
            {
                bestEvaluation = moveEvaluation;
                moveToPlay = move;
            }
            board.UndoMove(move);
        }

        return Tuple.Create(moveToPlay, bestEvaluation);
    }

    // Get simple board eval
    int boardEval(Board board, PieceList[] pieces, int depth)
    {
        int eval = 0;

        // Checkmate, better when closer
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -80000 + depth : 80000 - depth;
        }

        // Draw
        if (board.IsDraw())
        {
            return 0;
        }

        Piece lastPawn = new Piece();
        foreach (PieceList pieceList in pieces)
        {
            // Bishop Pair
            if ((int)pieceList.TypeOfPieceInList == 3 && pieceList.Count == 2)
            {
                eval += pieceList.IsWhitePieceList ? 50 : -50;
            }
            foreach (Piece piece in pieceList)
            {
                if ((int)pieceList.TypeOfPieceInList == 1)
                {
                    if (!lastPawn.IsNull && lastPawn.IsWhite == piece.IsWhite)
                    {
                        // Doubled pawns bad
                        if (lastPawn.Square.File == piece.Square.File)
                        {
                            eval += pieceList.IsWhitePieceList ? -10 : 10;
                        }
                        // Connected pawns good
                        if (Math.Abs(lastPawn.Square.File - piece.Square.File) == 1 && Math.Abs(lastPawn.Square.Rank - piece.Square.Rank) == 1)
                        {
                            eval += pieceList.IsWhitePieceList ? 10 : -10;
                        }
                    }
                    lastPawn = piece;
                }
                eval += pieceEval(board, piece) * (piece.IsWhite ? 1 : -1);
            }
        }
        return eval;
    }

    // Estimate value of a piece
    int pieceEval(Board board, Piece piece = new Piece())
    {
        int pieceValue = pieceValues[(int)piece.PieceType];
        Square otherKingSquare = board.GetKingSquare(!piece.IsWhite);
        int otherKingRank = otherKingSquare.Rank;
        int otherKingFile = otherKingSquare.File;
        int rank = piece.Square.Rank;
        int file = piece.Square.File;
        // In general, better when not attacked
        if (!piece.IsPawn && !piece.IsKing && board.SquareIsAttackedByOpponent(piece.Square))
        {
            pieceValue -= pieceValue / 20;
        }
        // Pawns
        if (piece.IsPawn)
        {
            // Pawns better closer to promotion
            if (rank == (piece.IsWhite ? 5 : 2))
            {
                pieceValue += 50;
            }
            if (rank == (piece.IsWhite ? 6 : 1))
            {
                pieceValue += 300;
            }
            // Center pawns good
            if ((piece.IsWhite && file == 3) || (!piece.IsWhite && file == 4))
            {
                pieceValue += 30;
            }
        }
        // Get your pieces out
        if (board.PlyCount < 20 && (piece.IsKnight || piece.IsBishop) && rank == (piece.IsWhite ? 0 : 7))
        {
            pieceValue -= 30;
        }
        // King should approach other king in end game
        if (piece.IsKing && (board.PlyCount > 60 && Math.Abs(file - otherKingFile) <= 2 && Math.Abs(rank - otherKingRank) <= 2))
        {
            pieceValue += 30;
        }
        // Knights
        if (piece.IsKnight)
        {
            // Knights on the rim are dim
            if (rank == 0 || rank == 7 || file == 0 || file == 7)
            {
                pieceValue -= 30;
            }
        }
        // Favor the center
        if (board.PlyCount < 80)
        {
            if (rank == 3 || rank == 4 || file == 2 || file == 5)
            {
                pieceValue += 10;
            }
            if (file == 3 || file == 4)
            {
                pieceValue += 20;
            }
        }
        // Piece horizontal or vertical with opponnent King
        if (piece.IsRook || piece.IsQueen)
        {
            if (file == otherKingFile || rank == otherKingRank)
            {
                pieceValue += 20;
            }
            if (Math.Abs(otherKingFile - file) <= 1 || Math.Abs(otherKingRank - rank) <= 1)
            {
                pieceValue += 10;
            }
        }
        // Piece diagonal with opponnent King
        if ((piece.IsBishop || piece.IsQueen) && Math.Abs(otherKingFile - file) == Math.Abs(otherKingRank - rank))
        {
            pieceValue += 20;
        }
        // In opponnent King area
        if (Math.Abs(otherKingFile - file) <= 3 && Math.Abs(otherKingRank - rank) <= 3)
        {
            pieceValue += 10;
        }
        return pieceValue;
    }
}