namespace auto_Bot_465;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_465 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // Check for checkmate first and formost
        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                return move;
            }

            board.UndoMove(move);
        }

        // Dictionary of piece values (I tried to get a constructor to do this but could not get it working)
        Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>();
        pieceValues.Add(PieceType.Pawn, 1);
        pieceValues.Add(PieceType.Bishop, 3);
        pieceValues.Add(PieceType.Knight, 3);
        pieceValues.Add(PieceType.Rook, 5);
        pieceValues.Add(PieceType.Queen, 9);
        pieceValues.Add(PieceType.King, 999);

        int turns = board.PlyCount;

        if (turns >= 8)
        {
            return ArbitrayMove(board, timer, pieceValues);
        }

        //Attempt to Scholars Mate:

        // Fun Data and Storage 
        //Scholar Moves (All in one array, white moves are even and black moves are odd
        String[] scholarMoves = {
            "e2e4",
            "e7e5",
            "f1c4",
            "f8c5",
            "d1h5",
            "d8h4",
            "h5e5", //Trap Play
            "h4e4"  //Trap Play
        };

        String[] scholarBackupMoves = {
            "",
            "",
            "",
            "",
            "d1f3", //Don't move queen as far (if this triggers the other moves won't be legal, but that is fine since I check)
            "d8f6", //Don't move queen as far (if this triggers the other moves won't be legal, but that is fine since I check)
            "h5f3", //Move queen from forward space to backwards space
            "h4f6"  //Move queen from forward space to backwards space
        };

        Square[] bishopSquares = {
            new Square("c4"),
            new Square("c5")
        };

        Square[] queenSquares = {
            new Square("h5"),
            new Square("h4")
        };


        // CHECK BISHOP SQUARE WITH QUEEN MOVE
        if (turns >= 4)
        {
            if (board.SquareIsAttackedByOpponent(bishopSquares[turns % 2]))
            { //Bishop Square is Under Attack
                return ArbitrayMove(board, timer, pieceValues);
            }
        }

        Move bestMove = new Move(scholarMoves[turns], board);

        DivertedConsole.Write(bestMove);

        foreach (Move move in moves)
        {
            if (move.Equals(bestMove))
            {
                if (!board.SquareIsAttackedByOpponent(bestMove.TargetSquare))
                {
                    return bestMove;
                }
                break;
            }
        }

        if (scholarBackupMoves[turns] != "")
        {
            bestMove = new Move(scholarBackupMoves[turns], board);
            foreach (Move move in moves)
            {
                if (move.Equals(bestMove))
                {
                    if (!board.SquareIsAttackedByOpponent(bestMove.TargetSquare))
                    {
                        return bestMove;
                    }
                    break;
                }
            }
        }

        return ArbitrayMove(board, timer, pieceValues);
    }

    public int MinMax(int value, int min, int max)
    {
        if (value > max)
        {
            return max;
        }
        else if (value < min)
        {
            return min;
        }
        return value;
    }

    // Returns an Arbitrary Move Based on Legal Moves
    // All scoring is completley arbitrary based on what I felt like.
    // BEST TO DO: Defending Pieces

    public Move ArbitrayMove(Board board, Timer timer, Dictionary<PieceType, int> pieceValues)
    {

        int turns = board.PlyCount;
        Move[] moves = board.GetLegalMoves();
        // Best Move Start Random
        System.Random random = new();
        Move bestMove = new Move();
        int bestScore = -999;

        foreach (Move move in moves)
        {
            //Start with a slight random score so that it doesn't repeat the same move. Good moves will blast past this
            int score = random.Next(5) - 2; //-2  -  2
            bool targetUnderAttack = board.SquareIsAttackedByOpponent(move.TargetSquare);

            // Scoring that requires the move to be made
            board.MakeMove(move);

            //Checkmate is already checked for

            // Safe checks are good though (and non safe checks are okay)
            // This makes checkmating a lot more consistent (though it still can't mate with a rook and king)
            if (board.IsInCheck() && !targetUnderAttack)
            {
                score += 6;
            }
            else if (board.IsInCheck())
            {
                score += 2;
            }

            // Don't get Checkmate in 1
            Move[] blackMoves = board.GetLegalMoves();
            foreach (Move blackMove in blackMoves)
            {
                board.MakeMove(blackMove);
                if (board.IsInCheckmate())
                {
                    score = -9999;
                }
                board.UndoMove(blackMove);
            }

            // Draws are boring, even if it means going down in glory
            if (board.IsDraw())
            {
                score -= 10;
            }

            // Gain some bonus if we're moving to a defended place from a non defended place
            if (!board.SquareIsAttackedByOpponent(move.StartSquare) && board.SquareIsAttackedByOpponent(move.TargetSquare) && move.MovePieceType != PieceType.King)
            {
                score += (int)Math.Sqrt(pieceValues.GetValueOrDefault(move.MovePieceType)); // Square roots make larger numbers smaller while maintaining smaller numbers
            }

            board.ForceSkipTurn();

            // Don't move to an endangered square
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                score -= pieceValues.GetValueOrDefault(move.MovePieceType);
            }

            board.UndoSkipTurn();
            board.UndoMove(move);

            // If in check, we prefer to not move the king. This cancels out the bonus for moving out of a danger square
            // We give every other piece a bonus score, and even more if it captures the attacking pawn
            if (board.IsInCheck())
            {
                if (move.MovePieceType == PieceType.King)
                {
                    score -= pieceValues.GetValueOrDefault(PieceType.King) + 3;
                }
                else
                {
                    score += 5;
                    if (move.IsCapture)
                    {
                        score += 3;
                    }
                }
            }

            if (move.MovePieceType == PieceType.King && turns < 50)
            { // Negative To Moving a King, Gone on later turns
                score -= 3;
            }


            //If a piece is in danger move it out of danger
            if (board.SquareIsAttackedByOpponent(move.StartSquare))
            {
                score += pieceValues.GetValueOrDefault(move.MovePieceType);
            }
            //The check for moving into danger is above

            //We like captures
            if (move.IsCapture)
            {
                score += 1;
                if (!targetUnderAttack)
                {
                    //Free Piece!
                    score += 60;
                }
                //Weight the capture based on the value of the piece we're using 
                score += (pieceValues.GetValueOrDefault(move.CapturePieceType) - pieceValues.GetValueOrDefault(move.MovePieceType)) * 3;
            }

            //Just promote to a queen please
            if (move.IsPromotion)
            {
                if (move.PromotionPieceType == PieceType.Queen)
                {
                    score += 10;
                }
                else
                {
                    score -= 10;
                }
            }

            //Castling is Cool I guess
            if (move.IsCastles)
            {
                score += 6;
            }

            //Let's move up the board (especially pawns) but ONLY for development
            if (!targetUnderAttack && turns < 50)
            {
                int multiplier = move.MovePieceType == PieceType.Pawn ? 2 : 1;
                if (turns % 2 == 0)
                { //White
                    score += MinMax((move.TargetSquare.Rank - move.StartSquare.Rank) * multiplier, -4, 3);
                }
                else
                {
                    score += MinMax((move.StartSquare.Rank - move.TargetSquare.Rank) * multiplier, -4, 3);
                }
            }


            //Get "Best" Move
            if (score > bestScore)
            {
                bestMove = move;
                bestScore = score;
            }

        }

        if (bestMove.IsNull)
        { //Backup
            DivertedConsole.Write("NO MOVE FOUND");
            bestMove = moves[random.Next(moves.Length)];
        }
        //DivertedConsole.Write("CHOSEN MOVE");
        //DivertedConsole.Write(bestMove);
        //DivertedConsole.Write(bestScore);
        return bestMove;
    }
}