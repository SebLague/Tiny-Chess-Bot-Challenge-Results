namespace auto_Bot_447;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_447 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        double highestEval = 0.0;
        var goodMoves = new List<Move>();
        var notTerribleMoves = new List<Move>();
        foreach (Move move in allMoves)
        {
            // If only one move exists, just play it
            if (allMoves.Length == 1)
            {
                break;
            }

            // En passant is forced
            if (move.IsEnPassant)
            {
                moveToPlay = move;
                break;
            }

            // If no en passant, play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // If no mate in one, play mate in two
            if (MoveIsMateInTwo(board, move))
            {
                moveToPlay = move;
                break;
            }

            // If no mate in one or two, play mate in three
            if (MoveIsMateInThree(board, move))
            {
                moveToPlay = move;
                break;
            }

            // If no mate in one or two or three, play mate in four (only check 10% of time and if time permits to avoid timeout)
            if (rng.Next(10) < 1 && timer.MillisecondsRemaining > 30000)
            {
                if (MoveIsMateInFour(board, move))
                {
                    moveToPlay = move;
                    break;
                }
            }

            // Find highest eval move
            double evaluation = Eval(board, move);

            if (evaluation > highestEval)
            {
                moveToPlay = move;
                highestEval = evaluation;
            }

            if (evaluation >= 0)
            {
                goodMoves.Add(move);
            }
            else if (evaluation > -5)
            {
                notTerribleMoves.Add(move);
            }
            // Bitmaps to train AIs
        }

        // If no move with evaluation > 0 exists, so moveToPlay was never updated, play random move with evaluation == 0
        // to not play previously randomly selected move, which might have negative evaluation
        // Else if no move with evaluation == 0 exists, play one with at least evaluation -5
        if (highestEval == 0 && goodMoves.Count > 0)
        {
            moveToPlay = goodMoves[rng.Next(goodMoves.Count)];
        }
        else if (highestEval == 0 && goodMoves.Count == 0 && notTerribleMoves.Count > 0)
        {
            moveToPlay = notTerribleMoves[rng.Next(notTerribleMoves.Count)];
        }
        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    bool MoveIsMateInTwo(Board board, Move move)
    {
        bool mateTwo = true;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            board.MakeMove(moveOpponent);
            Move[] allMovesForMe = board.GetLegalMoves();
            bool gotMate = false;
            foreach (Move myMove in allMovesForMe)
            {
                if (MoveIsCheckmate(board, myMove))
                {
                    gotMate = true;
                }
            }
            board.UndoMove(moveOpponent);
            if (gotMate == false)
            {
                mateTwo = false;
                break;
            }
            // If all opponents moves allow for at least one mate in one, its mate in two
        }
        board.UndoMove(move);
        return mateTwo;
    }

    bool MoveIsMateInThree(Board board, Move move)
    {
        bool mateThree = true;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            board.MakeMove(moveOpponent);
            Move[] allMovesForMe = board.GetLegalMoves();
            bool gotMate = false;
            foreach (Move myMove in allMovesForMe)
            {
                if (MoveIsMateInTwo(board, myMove))
                {
                    gotMate = true;
                }
            }
            board.UndoMove(moveOpponent);
            if (gotMate == false)
            {
                mateThree = false;
                break;
            }
            // If all opponents moves allow for at least one mate in one, its mate in two
        }
        board.UndoMove(move);
        return mateThree;
    }

    bool MoveIsMateInFour(Board board, Move move)
    {
        bool mateFour = true;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            board.MakeMove(moveOpponent);
            Move[] allMovesForMe = board.GetLegalMoves();
            bool gotMate = false;
            foreach (Move myMove in allMovesForMe)
            {
                if (MoveIsMateInThree(board, myMove))
                {
                    gotMate = true;
                }
            }
            board.UndoMove(moveOpponent);
            if (gotMate == false)
            {
                mateFour = false;
                break;
            }
            // If all opponents moves allow for at least one mate in one, its mate in two
        }
        board.UndoMove(move);
        return mateFour;
    }

    bool MoveLetsOpponentMateInOne(Board board, Move move)
    {
        bool isMateOpponent = false;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            isMateOpponent = MoveIsCheckmate(board, moveOpponent);
            if (isMateOpponent == true)
            {
                break;
            }
        }
        board.UndoMove(move);
        return isMateOpponent;
    }

    bool MoveLetsOpponentMateInTwo(Board board, Move move)
    {
        bool isMateOpponent = false;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            isMateOpponent = MoveIsMateInTwo(board, moveOpponent);
            if (isMateOpponent == true)
            {
                break;
            }
        }
        board.UndoMove(move);
        return isMateOpponent;
    }

    double PieceIsHanging(Board board, Move move)
    {
        double punishment = 0.0;
        board.MakeMove(move);
        Move[] allMovesForOpponent = board.GetLegalMoves();
        foreach (Move moveOpponent in allMovesForOpponent)
        {
            int attackedPiece = (int)board.GetPiece(moveOpponent.TargetSquare).PieceType;
            bool attacked = board.SquareIsAttackedByOpponent(moveOpponent.TargetSquare);
            if (!attacked)
            {
                punishment += pieceValues[attackedPiece] / 90;
            }
        }
        board.UndoMove(move);
        return punishment;
    }

    double Eval(Board board, Move move)
    {

        // Higher value captures score a higher evaluation
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        double eval = pieceValues[(int)capturedPiece.PieceType] / 100;
        // If attacked piece is defended, subtract slightly less than attacking piece value 
        // to allow trades of equally valued pieces, but not trading at a loss
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            if (eval > 0)
            {
                eval -= (pieceValues[(int)move.MovePieceType] - 50) / 100;
            }
            else
            {
                // If square is defended and move is not a capture, highly discourage this move
                eval -= 10000.0;
            }
        }
        // When promoting, only promote to a queen, taking undefended Queen scores higher than promotion
        if (move.IsPromotion)
        {
            if ((int)move.PromotionPieceType == 5)
            {
                eval += 8.9;
            }
            else
            {
                eval -= 10000.0;
            }
        }
        // Give some value to castling
        if (move.IsCastles)
        {
            eval += 2.5;
        }
        // Do not do moves that let the opponent play Mate in One
        if (MoveLetsOpponentMateInOne(board, move))
        {
            eval -= 100000.0;
        }
        // Or Mate in two
        if (MoveLetsOpponentMateInTwo(board, move))
        {
            eval -= 100000.0;
        }
        // If number of pieces on board is low or high, prefer pawn moves
        PieceList[] allPieces = board.GetAllPieceLists();
        int numberOfPieces = 0;
        foreach (PieceList list in allPieces)
        {
            numberOfPieces += list.Count;
        }
        if (numberOfPieces < 12 && (int)move.MovePieceType == 1)
        {
            eval += .9;
        }
        if (numberOfPieces > 24 && (int)move.MovePieceType == 6)
        {
            eval -= 1;
        }
        // Check if move leaves a piece hanging, subtract hanging penalty
        eval -= PieceIsHanging(board, move);
        return eval;
    }
}