namespace auto_Bot_567;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_567 : IChessBot
{

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    const int checkValue = 250;

    List<Move> highestValueAndChargeMoves = new List<Move>();
    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        List<Move> bestTradeMove = GetBestTradeMoves(board, allMoves.ToList());

        Random rng = new();
        Move moveToPlay;

        if (bestTradeMove != null && bestTradeMove.Count != 0)
        {
            moveToPlay = bestTradeMove[rng.Next(bestTradeMove.Count())];
        }
        else
        {
            moveToPlay = allMoves[rng.Next(allMoves.Length)];
        }

        return moveToPlay;
    }

    List<Move> GetBestTradeMoves(Board board, List<Move> captureMoveList)
    {
        List<Move> highestValueMoves = new List<Move>();
        int highestTradeValue = 0;
        foreach (Move move in captureMoveList)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                highestValueMoves.Clear();
                highestValueMoves.Add(move);
                return highestValueMoves;
            }

            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int tradeValue = pieceValues[(int)capturedPiece.PieceType] + checkValue * Convert.ToInt32(board.IsInCheck());
            board.MakeMove(move);
            tradeValue = GetTradeValue(board, tradeValue, true);
            board.UndoMove(move);
            if (tradeValue > highestTradeValue)
            {
                highestValueMoves.Clear();
                highestValueMoves.Add(move);
                highestTradeValue = tradeValue;
            }
            else if (tradeValue == highestTradeValue)
                highestValueMoves.Add(move);
        }
        return highestValueMoves;
    }

    int GetTradeValue(Board board, int tradeValue, bool enemyToMove)
    {
        Move[] allMoves = board.GetLegalMoves();
        List<Move> highestValueMoves = GetHighestValueMoves(board, allMoves);
        if (highestValueMoves.Count == 0)
            return tradeValue;
        //check if there is no capture
        bool NoCapture = true;
        foreach (Move move in highestValueMoves)
        {
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            if (capturedPiece.PieceType != PieceType.None)
                NoCapture = false;
        }
        if (NoCapture) return tradeValue; //stop the search of the trade if no piece is captured

        if (enemyToMove)
            tradeValue -= pieceValues[(int)board.GetPiece(highestValueMoves[0].TargetSquare).PieceType];
        else
            tradeValue += pieceValues[(int)board.GetPiece(highestValueMoves[0].TargetSquare).PieceType];

        board.MakeMove(highestValueMoves[0]); //Take the first elem best to do tree search but lack of time and/or knowledge
        GetTradeValue(board, tradeValue, !enemyToMove);
        board.UndoMove(highestValueMoves[0]);
        return tradeValue;
    }

    //Return List of highest value moves
    List<Move> GetHighestValueMoves(Board board, Move[] allMoves)
    {
        List<Move> highestValueMoves = new List<Move>();
        int highestValueCapture = -1;

        //filter best move by capture or check value
        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                highestValueMoves.Clear();
                highestValueMoves.Add(move);
                return highestValueMoves;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            board.MakeMove(move);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType] + checkValue * Convert.ToInt32(board.IsInCheck());
            board.UndoMove(move);

            if (capturedPieceValue > highestValueCapture)
            {
                highestValueMoves.Clear();
                highestValueMoves.Add(move);
                highestValueCapture = capturedPieceValue;
            }
            else if (capturedPieceValue == highestValueCapture)
                highestValueMoves.Add(move);
        }
        return highestValueMoves;
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}