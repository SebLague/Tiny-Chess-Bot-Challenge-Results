namespace auto_Bot_75;
using ChessChallenge.API;
using System;

public class Bot_75 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int[] looseValues = { 0, 310, 510, 510, 750, 1010, 99999 };
    string pHeatMap = "bbbbbbbbaaaaaaaabbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
    string bHeatMap = "eddddddedccccccddccccccddcbaabcddcbaabcddccccccddccccccdedddddde";
    string rHeatMap = "bbbbbbbbbaaaaaabccccccccccccccccccccccccccccccccccccccccccbaabcc";
    string qHeatMap = "eddddddedccccccddccccccddccbbccddccbbccddccccccddccccccdedddddde";
    string kgHeatMap = "dddeeddddddeedddcddeeddccddeeddccddeeddcccccccccccccccccbaccccab";
    Move lastmove;
    Board b;

    public Move Think(Board board, Timer timer)
    {
        b = board;
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        bool movesafe = true;
        int highestValueCapture = 0;
        int highestLooseValue = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                return moveToPlay;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            //--Carefulness--
            board.MakeMove(move);
            bool saf = true;
            foreach (Move mov in board.GetLegalMoves(false))
            {
                if (board.GetPiece(mov.TargetSquare) == board.GetPiece(move.TargetSquare))
                {
                    saf = false;
                    break;
                }
            }
            int loosevar = looseValues[(int)board.GetPiece(move.TargetSquare).PieceType];
            board.UndoMove(move);
            if (saf == false)
            {
                capturedPieceValue -= loosevar;
            }

            if (move.MovePieceType == PieceType.Pawn) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), pHeatMap); }
            if (move.MovePieceType == PieceType.Knight) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), bHeatMap); }
            if (move.MovePieceType == PieceType.Bishop) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), bHeatMap); }
            if (move.MovePieceType == PieceType.Rook) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), rHeatMap); }
            if (move.MovePieceType == PieceType.Queen) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), qHeatMap); }
            if (move.MovePieceType == PieceType.King) { capturedPieceValue += GetHeatMapValue(RightIndex(move.TargetSquare.Index), kgHeatMap); }

            if (capturedPiece.PieceType == PieceType.Pawn)
            {
                if (board.IsWhiteToMove)
                {
                    capturedPieceValue += ((7 - capturedPiece.Square.Rank) * 95);
                }
                else
                {
                    capturedPieceValue += (capturedPiece.Square.Rank * 95);
                }
            }
            if (move.IsPromotion) { capturedPieceValue += 300; }

            if (move.TargetSquare == lastmove.StartSquare && move.MovePieceType == lastmove.MovePieceType)
            {
                capturedPieceValue -= 500;
            }

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                movesafe = saf;
                highestValueCapture = capturedPieceValue;
                highestLooseValue = loosevar;
            }
        }
        lastmove = moveToPlay;
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
    int GetHeatMapValue(int index, string heatmap)
    {
        string y = heatmap.Substring(index, 1);
        if (y == "a") { return 50; }
        if (y == "b") { return 25; }
        if (y == "c") { return 0; }
        if (y == "d") { return -25; }
        return -50;
    }
    int RightIndex(int i)
    {
        if (b.IsWhiteToMove)
        {
            return 63 - i;
        }
        return i;
    }
}