namespace auto_Bot_518;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_518 : IChessBot

{
    public Move Think(Board board, Timer timer)
    {
        bool ownColorWhite = board.IsWhiteToMove;


        // Fancy Fence Defence (which is actually just the the "french defence" (?) i guess and mirrored for white)
        int currentMove = board.GameMoveHistory.Length / 2;

        //DivertedConsole.Write($"'normalStartingposition': '{normalStartingposition}' ende");
        //DivertedConsole.Write($"'board.GameStartFenString': '{board.GameStartFenString}' ende");
        if ((currentMove < 5) & String.Equals(board.GameStartFenString.TrimEnd('\r'), "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1"))
        {
            String[] moveString;
            if (ownColorWhite)
            {
                moveString = new string[5] { "e2e3", "d2d4", "c2c4", "d1b3", "c1d2" };
            }
            else
            {
                moveString = new string[5] { "e7e6", "d7d5", "c7c5", "d8b6", "c8d7" };
            }
            return new Move(moveString[currentMove], board);
        }


        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            // Minimax for guaranteed Checkmate, i guess would be good to avoid guaranteed checkmates from enemy
            // Is faster than evaluation-minimax since -guaranteed- means only one not checkmate move (should) cancel the search

            if (WinningPosition(board, 0, 20, false))
            {
                board.UndoMove(move);
                return move;
            }
            else
            {
                board.UndoMove(move);
            }
            // Always promote to Queen cause overpowered
            if (move.IsPromotion & (move.PromotionPieceType == PieceType.Queen))
            {
                return move;
            }
        }

        return GetBestMove(board);

    }
    Move GetBestMove(Board board)
    {
        int nPieces = Math.Max(BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard), BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard));

        Move[] legalMoves = board.GetLegalMoves();
        var boardValues = new int[board.GetLegalMoves().Length];
        for (int index = 0; index < legalMoves.Length; index++)
        {
            board.MakeMove(legalMoves[index]);
            boardValues[index] = MiniMax(board, Math.Max((16 - nPieces) / 4, 2)); // Math.Max((16-nPieces)/4,2);
            board.UndoMove(legalMoves[index]);
        }
        Array.Sort(boardValues, legalMoves);
        // dont know, why here the first ove seems the best. normally it should be the other way around
        //Array.Reverse(legalMoves);
        return legalMoves[0];
    }

    int MiniMax(Board board, int depth)
    {
        int bestScore = int.MinValue;

        if (depth == 0)
        {
            return Evaluate(board, board.IsWhiteToMove);
        }
        else
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                int score = MiniMax(board, depth - 1);
                bestScore = Math.Max(score, bestScore);
                board.UndoMove(move);
            }
        }
        return bestScore; // rng.Next(legalMoves.Length/10)
    }

    bool WinningPosition(Board board, int depth, int maxDepth, bool maximizing)
    {
        // Uses Minimax to determine if there is a guaranteed Checkmate in maxDepth moves. Therefore for maximizing one true is enough but for !maximizing it needs ALL moves to lead to true
        if (board.IsInCheckmate())
        {
            if (!maximizing)
            {
                return true;
            }
            else { return false; }
        }

        if (depth > maxDepth)
        {
            return false;
        }

        if (maximizing)
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                if (WinningPosition(board, depth + 1, maxDepth, !maximizing))
                {
                    board.UndoMove(move);
                    return true;
                }
                board.UndoMove(move);
            }
            return false;
        }
        else
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                if (!WinningPosition(board, depth + 1, maxDepth, maximizing))
                {
                    board.UndoMove(move);
                    return false;
                }
                board.UndoMove(move);
            }
            return true;
        }
    }

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int Evaluate(Board board, bool ownColorWhite)
    {
        int evaluation = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (var el in allPieces)
        {
            foreach (Piece piece in el)
            {
                evaluation += (pieceValues[(int)piece.PieceType] + EvaluatePicePosition(board, piece, ownColorWhite)) * (ownColorWhite == piece.IsWhite ? 1 : -1);
            }
        }
        return evaluation;
    }

    int EvaluatePicePosition(Board board, Piece piece, bool ownColorWhite)
    {
        // each piece has its own "rule" for a better position. Didnt know what to do for the bishop and rook
        int positionValue = 0;
        List<Square> defendedSquares = SquaresDefendedByMe(board);
        if (defendedSquares.Contains(piece.Square))
        {
            positionValue += 200;
        }

        if (piece.IsPawn)
        {
            return (piece.IsWhite ? piece.Square.Rank ^ 2 * 50 : 8 - piece.Square.Rank ^ 2) * 50;

        }
        else if (piece.IsKnight)
        {
            // If this actually works, checks for forks on squares which are not attacked by oppenent
            ulong knightAttacks = BitboardHelper.GetKnightAttacks(piece.Square);

            for (int i = 0; i < BitboardHelper.GetNumberOfSetBits(knightAttacks); i++)
            {
                Square attackedSquare = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref knightAttacks));
                if (!board.SquareIsAttackedByOpponent(attackedSquare))
                {
                    positionValue += pieceValues[(int)board.GetPiece(attackedSquare).PieceType] / 2;
                }
            }
            return positionValue;

        }
        else if (piece.IsQueen)
        {
            board.SquareIsAttackedByOpponent(piece.Square);
            return positionValue - 500;

        }
        else if (piece.IsKing)
        {
            // King should be way back at the beginning of the game and afterwards stay close to other pieces (mainly pawns)
            /*
            No Memory left
            int minDistance = int.MaxValue;
            foreach(PieceList otherPieces in board.GetAllPieceLists())
            {
                foreach(Piece otherPiece in otherPieces)
                {   
                    if ( (piece.IsWhite == otherPiece.IsWhite) & !(piece.IsWhite & otherPiece.IsWhite & otherPiece.IsKing) )
                    {
                        int distance = Distance(piece, otherPiece);
                        minDistance = Math.Min(distance,minDistance);
                        
                    }
                }    
            }
            int numberOwnPieces = (ownColorWhite ? BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard) : BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard));
            if ( numberOwnPieces > 1 )
            {
                positionValue -= minDistance * board.GameMoveHistory.Length;
                positionValue += (- Math.Abs(piece.Square.Rank-4) + 4) *10;
            }
            */
            return positionValue += (board.HasKingsideCastleRight(ownColorWhite) || board.HasQueensideCastleRight(ownColorWhite) ? 100 : 0);
        }
        else
        {
            return 0;
        }

    }

    List<Square> SquaresDefendedByMe(Board board)
    {
        // would be better with bitboard......

        List<Square> defendedSquares = new List<Square>();
        board.ForceSkipTurn();

        for (int index = 0; index < 64; index++)
        {
            Square square = new Square(index);

            if (board.SquareIsAttackedByOpponent(square))
            {
                defendedSquares.Add(square);
            }
        }
        board.UndoSkipTurn();
        return defendedSquares;
    }
    /*
    No Memory for that
    int Distance(Piece fromPiece, Piece toPiece)
    {   
        return Math.Max(Math.Abs(fromPiece.Square.Rank-toPiece.Square.Rank), Math.Abs(fromPiece.Square.File-toPiece.Square.File) );
    }
    */
}
