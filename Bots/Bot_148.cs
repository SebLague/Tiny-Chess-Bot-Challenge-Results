namespace auto_Bot_148;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_148 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        var backupMove = GetRandomMove(board, allMoves);

        try
        {

            foreach (var move in allMoves)
            {
                board.MakeMove(move);

                if (board.IsInCheckmate())
                {
                    //DivertedConsole.Write("Playing Checkmate");
                    board.UndoMove(move);
                    return move;
                }
                else if (move.IsPromotion && IsItSafe(move, board))
                {
                    //DivertedConsole.Write("Promoting");
                    board.UndoMove(move);
                    return move;
                }

                board.UndoMove(move);
            }

            List<Move> freeCaptureMoves = FreeCaptureMoves(board, allMoves);
            List<Move> fairTradeMoves = FairTradeMoves(board, allMoves);

            List<PieceType> piecesInDanger = PiecesInDanger(board, true);
            // Sort the list from highest to lowest value
            piecesInDanger = piecesInDanger.OrderByDescending(pieceType => pieceValues[(int)pieceType]).ToList();
            // Remove duplicates while preserving order (C# 7.0 and later)
            piecesInDanger = piecesInDanger.Distinct().ToList();

            if (piecesInDanger.Count > 0)
            {
                foreach (var piece in piecesInDanger)
                {
                    var pieceValue = pieceValues[(int)piece];
                    foreach (var move in allMoves)
                    {
                        board.MakeMove(move);
                        List<PieceType> stillInDanger = PiecesInDanger(board, false);

                        int maxDangerValue = stillInDanger.Select(p => pieceValues[(int)p]).DefaultIfEmpty(0).Max();

                        if (maxDangerValue < pieceValue)
                        {
                            //DivertedConsole.Write("Got out of danger!");
                            //DivertedConsole.Write(move);
                            board.UndoMove(move);
                            return (move);
                        }
                        board.UndoMove(move);
                    }
                }
            }

            if (freeCaptureMoves.Count > 0)
            {
                //DivertedConsole.Write("Playing free Capture");
                //DivertedConsole.Write(freeCaptureMoves[0]);
                return freeCaptureMoves[0];
            }
            else if (fairTradeMoves.Count > 0)
            {
                //DivertedConsole.Write("Playing fair trade");
                //DivertedConsole.Write(fairTradeMoves[0]);
                return fairTradeMoves[0];
            }

            foreach (var move in allMoves)
            {
                board.MakeMove(move);

                if (move.IsCastles)
                {
                    //DivertedConsole.Write("Castling");
                    board.UndoMove(move);
                    return move;
                }
                else if (move.IsEnPassant)
                {
                    //DivertedConsole.Write("EnPassant");
                    board.UndoMove(move);
                    return move;
                }
                else if (board.IsInCheck() && !board.IsDraw() && IsItSafe(move, board))
                {
                    //DivertedConsole.Write("Checking");
                    board.UndoMove(move);
                    return move;
                }

                if (board.PlyCount < 15)//Remove early game dumb moves
                {

                    if (move.MovePieceType.ToString() == "King" || move.MovePieceType.ToString() == "Rook")
                    {
                        allMoves = allMoves.Where(m => m != move).ToArray();
                    }
                }

                if (!IsItSafe(move, board) || board.IsDraw())
                {
                    allMoves = allMoves.Where(m => m != move).ToArray();
                }

                board.UndoMove(move);
            }

            if (allMoves.Length > 0)
            {
                //DivertedConsole.Write("Playing subset move");
                return GetRandomMove(board, allMoves);
            }
            else
            {
                //DivertedConsole.Write("Playing Random move");
                return backupMove;
            }
        }
        catch
        {
            return backupMove;
        }

    }

    private Move GetRandomMove(Board board, Move[] allMoves)
    {
        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {

            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    private List<Move> FreeCaptureMoves(Board board, Move[] allMoves)
    {
        List<Move> freeCaptures = new List<Move>();

        foreach (var move in allMoves)
        {
            if (IsFreeCapture(move, board))
            {
                freeCaptures.Add(move);
            }
        }
        return freeCaptures;
    }

    private List<Move> FairTradeMoves(Board board, Move[] allMoves)
    {
        List<Move> fairTradeMoves = new List<Move>();

        foreach (var move in allMoves)
        {
            if (move.IsCapture && CanTakeBackFair(board, move))
            {
                fairTradeMoves.Add(move);
            }
        }
        return fairTradeMoves;
    }

    private bool CanTakeBackFair(Board board, Move move) //Given a certain move, could the opponent take the piece back and its fair
    {
        bool canTakeBack = false;
        // Check if the opponent can counter-capture the moved piece on the next turn.
        board.MakeMove(move);

        foreach (var opponentMove in board.GetLegalMoves(true)) // iterate over legal moves that can capture
        {
            if (opponentMove.TargetSquare == move.TargetSquare) // check if the opponent is capturing the square in question
            {
                if (pieceValues[(int)move.CapturePieceType] >= pieceValues[(int)move.MovePieceType])
                {
                    canTakeBack = true;
                    break;
                }
            }
        }
        board.UndoMove(move);

        return canTakeBack;

    }

    private List<PieceType> PiecesInDanger(Board board, bool skip)
    {
        List<PieceType> piecesInDanger = new List<PieceType>();

        if (skip)
        {
            board.TrySkipTurn();
            foreach (var move in board.GetLegalMoves(true)) // Get opponent's legal capture moves
            {
                if (!CanTakeBackFair(board, move))
                {
                    PieceType attackedPiece = move.CapturePieceType;
                    piecesInDanger.Add(attackedPiece);
                }
            }
            board.UndoSkipTurn();
        }
        else
        {
            foreach (var move in board.GetLegalMoves(true)) // Get opponent's legal capture moves
            {
                PieceType attackedPiece = move.CapturePieceType;
                piecesInDanger.Add(attackedPiece);
            }
        }

        return piecesInDanger;
    }

    private bool IsFreeCapture(Move move, Board board)
    {
        board.MakeMove(move);

        if (move.IsCapture && IsItSafe(move, board))
        {
            board.UndoMove(move);
            return true;
        }
        else
        {
            board.UndoMove(move);
            return false;
        }

    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    private bool IsItSafe(Move move, Board board)
    {
        bool isSafe = true;

        foreach (var opponentMove in board.GetLegalMoves(true))
        {
            if (opponentMove.TargetSquare == move.TargetSquare)
            {
                isSafe = false;
                break;
            }
        }
        return isSafe;
    }
}