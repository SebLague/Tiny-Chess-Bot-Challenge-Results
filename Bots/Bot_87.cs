namespace auto_Bot_87;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_87 : IChessBot
{
    private int Minimax(Board board, int depth, bool isMaximizingPlayer)
    {
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
        {
            return gPE(board);
        }

        if (isMaximizingPlayer)
        {
            int bestScore = int.MinValue;
            Move[] moves = board.GetLegalMoves();


            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = 0;
                if (board.IsDraw())
                {
                    if (isWhiteSide)
                    {
                        score = -30;
                    }
                    else
                    {
                        score = 30;
                    }
                }
                score = Minimax(board, depth - 1, false);
                board.UndoMove(move);

                bestScore = Math.Max(isWhiteSide ? bestScore : -bestScore, score);
            }

            return bestScore;
        }
        else
        {
            int bestScore = int.MaxValue;
            Move[] moves = board.GetLegalMoves();

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int score = Minimax(board, depth - 1, true);
                board.UndoMove(move);

                bestScore = Math.Min(isWhiteSide ? bestScore : -bestScore, score);
            }
            return bestScore;
        }
    }

    int gPE(Board board)
    {
        int pe = 0;
        foreach (PieceList pieces in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieces)
            {
                if (piece.IsWhite)
                {
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            pe += 10;
                            break;

                        case PieceType.Knight:
                            pe += 30;
                            break;

                        case PieceType.Bishop:
                            pe += 32;
                            break;
                        case PieceType.Rook:
                            pe += 50;
                            break;
                        case PieceType.Queen:
                            pe += 90;
                            break;
                        case PieceType.King:
                            pe += 9999;
                            break;
                    }
                }
                else
                {
                    switch (piece.PieceType)
                    {
                        case PieceType.Pawn:
                            pe -= 10;
                            break;

                        case PieceType.Knight:
                            pe -= 30;
                            break;

                        case PieceType.Bishop:
                            pe -= 32;
                            break;
                        case PieceType.Rook:
                            pe -= 50;
                            break;
                        case PieceType.Queen:
                            pe -= 90;
                            break;
                        case PieceType.King:
                            pe -= 9999;
                            break;
                    }
                }
            }
        }

        return pe;
    }

    private int openingMoveIndex = 0;
    private bool isWhiteSide = true;

    public Move Think(Board board, Timer timer)
    {
        Random r = new Random();
        Move[] moves = board.GetLegalMoves();

        isWhiteSide = board.IsWhiteToMove;
        List<Move> sicilianDefenseMoves = new List<Move>
        {
            new Move("e2e4", board),
            new Move("c7c5", board),
            new Move("g1f3", board),
            new Move("d7d6", board),
            new Move("d2d4", board),
            new Move("c5d4", board),
            new Move("f3d4", board),
            new Move("g8f6", board),
            new Move("b1c3", board),
            new Move("a7a6", board),
        };
        List<Move> italianGameMoves = new List<Move>
        {
            new Move("e2e4", board),
            new Move("e7e5", board),
            new Move("g1f3", board),
            new Move("b8c6", board),
            new Move("f1c4", board),
        };
        List<Move> currentOpeningMoves = isWhiteSide ? italianGameMoves : sicilianDefenseMoves;
        if (openingMoveIndex < currentOpeningMoves.Count)
        {
            Move currentMove = currentOpeningMoves[openingMoveIndex];
            openingMoveIndex += 2;
            return checkValid(currentMove);
        }

        int depth = 5 * timer.MillisecondsRemaining / 60000;
        int bestScore = int.MinValue;
        Move bestMove = Move.NullMove;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int score = 0;
            if (board.IsDraw())
            {
                if (isWhiteSide)
                {
                    score = -30;
                }
                else
                {
                    score = 30;
                }
            }
            score = Minimax(board, depth - 1, false);
            board.UndoMove(move);
            if (board.IsWhiteToMove ? score > bestScore : score < bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        Move checkValid(Move check)
        {
            Move movee = Move.NullMove;
            bool allowed = false;
            if (check != Move.NullMove)
            {
                foreach (Move m in moves)
                {
                    if (movee == m) allowed = true;
                }
            }
            else
            {
                movee = moves[r.Next(moves.Count())];
            }

            if (!allowed) movee = moves[r.Next(moves.Count())];
            return movee;
        }

        return checkValid(bestMove);
    }
}