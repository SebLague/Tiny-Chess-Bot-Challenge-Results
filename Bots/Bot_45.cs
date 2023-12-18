namespace auto_Bot_45;
using ChessChallenge.API;
using System;

public class Bot_45 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 325, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        int factor = -1;
        if (board.IsWhiteToMove)
        {
            factor = 1;
        }
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = Aggressive(board);
        if (MoveIsCheckmate(board, moveToPlay) || timer.MillisecondsRemaining < 1000)
        {
            return moveToPlay;
        }
        board.MakeMove(moveToPlay);
        int score = 0;
        if (!board.IsDraw())
        {
            Move reply = Aggressive(board);
            board.MakeMove(reply);
            score = factor * Score(board);
            board.UndoMove(reply);
        }
        board.UndoMove(moveToPlay);
        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
            board.MakeMove(move);
            if (!board.IsDraw())
            {
                Move response = Aggressive(board);
                board.MakeMove(response);
                if (Score(board) * factor > score)
                {
                    moveToPlay = move;
                    score = Score(board) * factor;
                }
                board.UndoMove(response);
            }
            else
            {
                if (0 > score)
                {
                    moveToPlay = move;
                    score = 0;
                }
            }
            board.UndoMove(move);
        }
        return moveToPlay;
    }
    public Move Aggressive(Board board)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int Score = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            // Find highest value capture
            int capturedPieceValue = pieceValues[(int)move.CapturePieceType] + pieceValues[(int)move.PromotionPieceType];
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                capturedPieceValue -= pieceValues[(int)move.MovePieceType];
            }
            board.MakeMove(move);
            if (board.TrySkipTurn())
            {
                Move[] nextMoves = board.GetLegalMoves();
                foreach (Move move2 in nextMoves)
                {
                    if (MoveIsCheckmate(board, move2))
                    {
                        capturedPieceValue += 125;
                    }
                    board.MakeMove(move2);
                    if (board.TrySkipTurn())
                    {
                        Move[] thirdMoves = board.GetLegalMoves();
                        foreach (Move move3 in thirdMoves)
                        {
                            if (MoveIsCheckmate(board, move3))
                            {
                                capturedPieceValue += 150;
                            }
                        }
                        board.UndoSkipTurn();
                    }
                    else
                    {
                        capturedPieceValue += 62;
                    }
                    board.UndoMove(move2);
                }
                board.UndoSkipTurn();
            }
            else
            {
                capturedPieceValue += 62;
            }
            board.UndoMove(move);
            if (capturedPieceValue > Score)
            {
                moveToPlay = move;
                Score = capturedPieceValue;
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
    int Score(Board board)
    {
        int factor = -1;
        if (board.IsWhiteToMove)
        {
            factor = 1;
        }
        if (board.IsInCheckmate())
        {
            return -10000 * factor;
        }
        if (board.IsDraw())
        {
            return 0;
        }
        int score = 0;
        PieceList[] pieces = board.GetAllPieceLists();
        for (int i = 0; i < pieces.Length; i++)
        {
            if (pieces[i].IsWhitePieceList)
            {
                score += pieces[i].Count * pieceValues[(int)pieces[i].TypeOfPieceInList];
            }
            else
            {
                score -= pieces[i].Count * pieceValues[(int)pieces[i].TypeOfPieceInList];
            }
        }
        score += 10 * board.GetLegalMoves().Length * factor;
        Move[] allMoves = board.GetLegalMoves();
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        board.MakeMove(moveToPlay);
        if (!board.IsInCheckmate())
        {
            score -= 10 * board.GetLegalMoves().Length * factor;
        }
        board.UndoMove(moveToPlay);
        return score;
    }
}