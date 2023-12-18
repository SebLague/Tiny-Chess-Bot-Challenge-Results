namespace auto_Bot_207;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_207 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    static Random rng = new Random();

    public Move Think(Board board, Timer timer)
    {
        return Play(board);
    }

    /*
    How do _I_ play chess?

    1. Check to see if any of my pieces are threatened
      a. If that piece gets taken, can I take the piece that took it?
        i. If so, check the value differential between the pieces
    2. Check to see if I can take any pieces
      a. If I take that piece, can my opponent take the piece that took it?
        i. If so, check the value differential between the pieces
    3. Can I threaten the King? (safely)
    4. Can I move a more powerful piece towards the center of the board? (safely)

    Conditions to always check for:
    - Can I put my opponent in checkmate?
    - Can I promote a pawn?
    */

    int captureValue(Board board, Move move)
    {
        int takenValue = pieceValues[(int)move.CapturePieceType];
        int takerValue = pieceValues[(int)move.MovePieceType];
        int capture_value = takenValue;
        bool safe = !board.SquareIsAttackedByOpponent(move.TargetSquare);
        if (!safe) capture_value = takenValue - takerValue;
        return capture_value;
    }

    int scorePosition(Move move, Board board, bool white)
    {
        int score = 0;
        Square square = move.TargetSquare;
        board.MakeMove(move);
        switch (move.MovePieceType)
        {
            case PieceType.Pawn:
                score = square.Rank; // pawns further up the board are worth more
                if (!white) score = 7 - score;
                score *= 10;
                break;
            case PieceType.Knight:
                // being in the center is worth more to us
                score = 2 * (int)((3.5 - Math.Abs(square.File - 3.5)));
                break;
            case PieceType.Bishop:
                // naively assume there's nobody else on the board
                score = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Bishop, square, board));
                break;
            case PieceType.Rook:
                score = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Rook, square, board));
                break;
            case PieceType.Queen:
                // queens have double the attacks of a rook or bishop
                score = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Queen, square, board)) / 2;
                break;
            case PieceType.King:
                // I guess we'll say being near corners is better?
                score = (int)((3.5 - Math.Abs(square.File - 3.5)));
                score += (int)((3.5 - Math.Abs(square.Rank - 3.5)));
                score = (int)(score / 3); // we want to discourage king moves in general
                break;
        }
        board.UndoMove(move);
        return score;
    }

    Move Play(Board board)
    {
        bool imWhite = board.IsWhiteToMove;
        (int score, Move chosenMove) best_move = (int.MinValue, Move.NullMove);
        (int score, Move chosenMove) most_worrisome = (int.MinValue, Move.NullMove);
        (int score, Move chosenMove) best_retreat = (int.MinValue, Move.NullMove);
        Dictionary<Move, int> moveScores = new Dictionary<Move, int>();

        if (board.TrySkipTurn())
        {
            Move[] enemyCaptures = board.GetLegalMoves(true);
            foreach (Move capture_move in enemyCaptures)
            {
                int capture_value = captureValue(board, capture_move);
                if (capture_value > most_worrisome.score)
                {
                    most_worrisome = (capture_value, capture_move);
                }
            }
            board.UndoSkipTurn();
        }

        Move[] myCaptureMoves = board.GetLegalMoves(true);
        foreach (Move capture_move in myCaptureMoves)
        {
            int capture_value = captureValue(board, capture_move);
            moveScores.TryAdd(capture_move, capture_value);
            if (capture_value > 0 && capture_value > best_move.score)
            {
                best_move = (capture_value, capture_move);
            }
        }

        Move[] myLegalMoves = board.GetLegalMoves();

        foreach (Move move in myLegalMoves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                moveScores.TryAdd(move, int.MaxValue);
                best_move = (int.MaxValue, move);
            }
            else if (move.IsPromotion)
            {
                moveScores.TryAdd(move, pieceValues[(int)PieceType.Queen]);
                best_move = (pieceValues[(int)PieceType.Queen], move);
            }
            else if (board.IsInCheck())
            {
                board.UndoMove(move);
                if (!board.SquareIsAttackedByOpponent(move.TargetSquare) && best_move.score < pieceValues[(int)PieceType.Queen] / 40) // safe check
                {
                    moveScores.TryAdd(move, pieceValues[(int)PieceType.Queen / 40]);
                    best_move = (pieceValues[(int)PieceType.Queen] / 40, move);
                }
                board.MakeMove(move);
            }
            board.UndoMove(move);
        }

        // we're probably going to lose a valuable piece in a bad trade, and our best capture isn't great
        if (most_worrisome.score > 0 && most_worrisome.score > best_move.score)
        {
            foreach (Move move in myLegalMoves)
            {
                if (move.StartSquare == most_worrisome.chosenMove.TargetSquare) // this move should be one that takes us away from the threat
                {
                    if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                    {
                        int position_score = scorePosition(move, board, imWhite);
                        moveScores.TryAdd(move, position_score);
                        if (position_score > best_retreat.score) best_retreat = (position_score, move);
                    }
                }
            }
            // TODO: sometimes retreating opens a more valuable piece to attack
            best_move = best_retreat;
        }

        // we still haven't chosen a move
        if (best_move.chosenMove == Move.NullMove)
        {
            foreach (Move move in myLegalMoves)
            {
                bool seenBefore = false;
                foreach (Move previousMove in board.GameMoveHistory)
                {
                    if (previousMove == move)
                    {
                        seenBefore = true; // avoid moves we've already made
                        break;
                    }
                }
                if (!seenBefore && !board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    int position_score = scorePosition(move, board, imWhite);
                    moveScores.TryAdd(move, position_score);
                    // if (position_score > best_move.score)
                    // {
                    //   best_move = (position_score, move);
                    // }
                }
            }
        }

        if (best_move.chosenMove == Move.NullMove)
        {
            int best_score = int.MinValue;
            List<Move> least_worst_moves = new List<Move>();
            foreach (KeyValuePair<Move, int> entry in moveScores)
            {
                if (entry.Value > best_score) best_score = entry.Value;
            }
            foreach (KeyValuePair<Move, int> item in moveScores)
            {
                if (item.Value == best_score) least_worst_moves.Add(item.Key);
            }

            if (least_worst_moves.Count > 0)
            {
                best_move = (best_score, least_worst_moves[rng.Next(least_worst_moves.Count)]);
            }
            if (best_move.chosenMove == Move.NullMove)
            {
                best_move.chosenMove = myLegalMoves[rng.Next(myLegalMoves.Length)];
            }
        }

        DivertedConsole.Write(String.Format("Score: {0}. Moving {1}", best_move.score, best_move.chosenMove.MovePieceType));
        return best_move.chosenMove;
    }

}