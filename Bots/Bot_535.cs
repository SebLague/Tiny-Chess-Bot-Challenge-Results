namespace auto_Bot_535;
using ChessChallenge.API;
using System;

public class Bot_535 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 350, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {

        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        float highestScore = 0;
        bool firstMove = true;

        foreach (Move move in allMoves)
        {

            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            float moveScore = 0;
            board.MakeMove(move);

            // Check for checking the opponent
            if (board.IsInCheck())
            {
                board.ForceSkipTurn();
                if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    moveScore += 20;
                }
                board.UndoSkipTurn();
            }

            // Evaluate structure
            for (int typeIndex = 1; typeIndex < 7; typeIndex++)
            {
                foreach (Piece piece in board.GetPieceList((PieceType)typeIndex, white: !board.IsWhiteToMove))
                {
                    moveScore += EvaluateStructure(board, piece);
                }
            }

            // Discourage if move would give opponent mate in one, or "check in one"
            // (Plus points if defending against opponent attacks -- omitted due to... not working)
            Move[] oppMoves = board.GetLegalMoves();
            foreach (Move oppMove in oppMoves)
            {

                /*
                if (oppMove.CapturePieceType != PieceType.None) {
                    board.MakeMove(oppMove);
                    // TODO: Somehow this is inverted -- we're checking whether *opponent attacks us*, but we want to check if we attack them after their capture
                    board.ForceSkipTurn();
                    if (board.SquareIsAttackedByOpponent(oppMove.TargetSquare)) {
                        moveScore += pieceValues[(int)oppMove.CapturePieceType];
                    }
                    board.UndoSkipTurn();
                    board.UndoMove(oppMove);
                }
                */

                board.MakeMove(oppMove);

                if (board.IsInCheckmate())
                {
                    moveScore -= pieceValues[(int)PieceType.King];
                }
                else if (board.IsInCheck())
                {
                    moveScore -= 500;
                }

                board.UndoMove(oppMove);

            }

            // Negative score for each of opponent's pieces
            for (int typeIndex = 1; typeIndex < 7; typeIndex++)
            {
                PieceList oppPieces = board.GetPieceList((PieceType)typeIndex, white: board.IsWhiteToMove);
                foreach (Piece oppPiece in oppPieces)
                {
                    moveScore -= pieceValues[(int)oppPiece.PieceType];
                }
            }

            board.UndoMove(move);

            // Discourage moving king when in check
            if (move.MovePieceType == PieceType.King)
            {
                moveScore -= pieceValues[(int)PieceType.Pawn];
            }

            if (firstMove || moveScore > highestScore)
            {

                highestScore = moveScore;
                moveToPlay = move;
                firstMove = false;

            }

        }
        return moveToPlay;

    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Opponent to move when this is called
    float EvaluateStructure(Board board, Piece piece)
    {

        Square oppKingSquare = board.GetKingSquare(!piece.IsWhite);
        float score = 0;
        switch (piece.PieceType)
        {
            case PieceType.Pawn:

                // Pawn chain 
                for (int dFile = -1; dFile <= 1; dFile++)
                {
                    for (int dRank = -1; dRank <= 1; dRank++)
                    {
                        if (dFile == 0 && dRank == 0) { continue; }
                        if (piece.Square.File + dFile < 0 || piece.Square.File + dFile >= 8) { continue; }
                        if (piece.Square.Rank + dRank < 0 || piece.Square.Rank + dFile >= 8) { continue; }

                        bool isDiagonal = (dRank != 0 && dFile != 0);
                        Piece otherPiece = board.GetPiece(new Square(piece.Square.File + dFile, piece.Square.Rank + dRank));
                        if (otherPiece.PieceType == PieceType.Pawn && otherPiece.IsWhite == piece.IsWhite)
                        {
                            // Left-right and top-bottom give 0.5 points, while diagonal gives 1 point
                            score += (isDiagonal ? 1f : 0.2f);
                        }
                    }
                }

                // Center
                float centerScore = 0.25f * (3.5f - Math.Abs(3.5f - piece.Square.File));

                // Advance
                score += 0.25f * (piece.IsWhite ? piece.Square.Rank : 7 - piece.Square.Rank) * centerScore;

                break;
            case PieceType.Rook:

                // This "connect rooks" logic is "expensive" and probably not worth it

                // Horizontal check
                bool foundRook = false;
                for (int file = 0; file < 8; file++)
                {

                    Piece otherPiece = board.GetPiece(new Square(file, piece.Square.Rank));
                    if ((otherPiece.PieceType == PieceType.Rook || otherPiece.PieceType == PieceType.Queen)
                      && otherPiece.IsWhite == piece.IsWhite)
                    {

                        if (foundRook && file != piece.Square.File)
                        {
                            score += 1;
                        }
                        foundRook = true;

                    }
                    else if (foundRook && (otherPiece.PieceType != PieceType.None || otherPiece.IsWhite != piece.IsWhite))
                    {
                        break;
                    }

                }
                foundRook = false;

                // Vertical check
                for (int rank = 0; rank < 8; rank++)
                {

                    Piece otherPiece = board.GetPiece(new Square(piece.Square.File, rank));
                    if ((otherPiece.PieceType == PieceType.Rook || otherPiece.PieceType == PieceType.Queen)
                      && otherPiece.IsWhite == piece.IsWhite)
                    {

                        if (foundRook && rank != piece.Square.Rank)
                        {
                            score += 2;
                        }
                        foundRook = true;

                    }
                    else if (foundRook && (otherPiece.PieceType != PieceType.None || otherPiece.IsWhite != piece.IsWhite))
                    {
                        break;
                    }

                }
                break;
            case PieceType.Knight:
            case PieceType.Queen:
            case PieceType.Bishop:

                // Center
                score += 0.25f * (3.5f - Math.Abs(3.5f - piece.Square.File));

                // Move toward opponent's king
                score -= Math.Abs(oppKingSquare.Rank - piece.Square.Rank) + Math.Abs(oppKingSquare.File - piece.Square.File);

                break;
            case PieceType.King:

                // Sides
                score += 0.25f * Math.Abs(3.5f - piece.Square.File);

                // "Subvance" :)
                score += 0.25f * (piece.IsWhite ? 7 - piece.Square.Rank : piece.Square.Rank);

                break;
            default: break;
        }

        // Minus points if attacked
        Move[] oppMoves = board.GetLegalMoves();
        int numAttackers = 0;
        foreach (Move oppMove in oppMoves)
        {
            if (oppMove.TargetSquare == piece.Square)
            {
                numAttackers++;
            }
        }
        if (numAttackers > 0)
        {
            score -= pieceValues[(int)piece.PieceType];
            score -= numAttackers * 10f;
        }

        return score;

    }

}
