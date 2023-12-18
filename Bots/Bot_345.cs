namespace auto_Bot_345;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
public class Bot_345 : IChessBot
{

    public static int bitcount(ulong number)
    {
        int count = 0;
        while (number > 0)
        {
            if ((number & 1) == 1)
            {
                count++;
            }
            number >>= 1; // Right shift the number by one bit
        }
        return count;
    }
    int chebyshevDist(Square A, Square B)
    {
        return Math.Max(Math.Abs(A.Rank - B.Rank), Math.Abs(A.File - B.File));
    }

    public int getValueForPieces(PieceType pieceType, bool color, Board board, int materialWeight, int visionWeight)
    {
        int value = 0;
        PieceList pieces = board.GetPieceList(pieceType, color);//get list for all pieces of the color inputted into function

        if (pieceType == PieceType.Bishop || pieceType == PieceType.Rook || pieceType == PieceType.Queen)
        {
            foreach (Piece piece in pieces)
            {
                value += (visionWeight * (bitcount(BitboardHelper.GetSliderAttacks(pieceType, piece.Square, board)))) + (materialWeight * pieceValues[pieceType]); // calculate values for sliding pieces
            }
            return value;
        }
        if (pieceType == PieceType.Pawn)
        {
            foreach (Piece pawn in pieces)
            {
                value += (visionWeight * bitcount(BitboardHelper.GetPawnAttacks(pawn.Square, color))) + (materialWeight * pieceValues[pieceType]);
            }
            return value;
        }
        if (pieceType == PieceType.Knight)
        {
            foreach (Piece knight in pieces)
            {
                value += (visionWeight * bitcount(BitboardHelper.GetKnightAttacks(knight.Square))) + (materialWeight * pieceValues[pieceType]);
            }
            return value;
        }
        if (pieceType == PieceType.King)
        {
            foreach (Piece king in pieces)
            {
                value += (visionWeight * (bitcount(BitboardHelper.GetKingAttacks(king.Square)))) + (materialWeight * pieceValues[pieceType]);
            }
            return value;
        }
        else
        {
            return 0;
        }
    }
    Dictionary<PieceType, int> pieceValues = new Dictionary<PieceType, int>(){
        {PieceType.Pawn, 1},{PieceType.Bishop, 3},{PieceType.Knight, 3},{PieceType.Rook,5},{PieceType.Queen,9},{PieceType.King, 100}
       };


    PieceType[] pieceArray = { PieceType.Pawn, PieceType.Bishop, PieceType.Knight, PieceType.Rook, PieceType.Queen, PieceType.King };

    public float calcAgression(bool color, Board board)
    {
        Square opposingKing = board.GetKingSquare(!color);
        int dist = 0;
        int pieceNum = 0;
        for (int i = 0; i < 6; i++)
        {
            PieceList Pieces = board.GetPieceList(pieceArray[i], color);
            pieceNum += Pieces.Count;
            for (int n = 0; n < Pieces.Count; n++)
            {
                Square square = Pieces.GetPiece(n).Square;
                dist += -(chebyshevDist(opposingKing, square)) + 7;

            }
        }

        return dist / pieceNum;
    }
    public float eval(Board board, bool white, bool black, int matWeight, int visionWeight, int agressionWeight)
    {
        float friendEval = 0;
        float enemyEval = 0;

        for (int i = 0; i < 6; i++)
        {
            friendEval += getValueForPieces(pieceArray[i], white, board, matWeight, visionWeight);
            enemyEval += getValueForPieces(pieceArray[i], black, board, matWeight, visionWeight);
        }
        Random rnd = new Random();
        int num = rnd.Next(-10, 10);
        friendEval += calcAgression(white, board) * agressionWeight;
        enemyEval += calcAgression(black, board) * agressionWeight;
        return (friendEval - enemyEval) + num;
    }

    public float search(Board board, int depth)
    {
        Move[] moves = board.GetLegalMoves();
        bool white = board.IsWhiteToMove;
        bool black = !white;
        //find which color is playing
        if (depth == 0)
        {
            return eval(board, white, black, 2, 3, 2);
        }
        float max = float.MinValue;
        foreach (Move move in moves)
        {
            float score;
            //loop through each move and search the position
            board.MakeMove(move);
            if (board.IsDraw())
            {
                score = 0;
            }
            else
            {
                score = -search(board, depth - 1);
            }


            board.UndoMove(move);

            if (score > max)
            {
                max = score;
            }
        }
        return max;
    }
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        bool white = board.IsWhiteToMove;
        bool black = !white;

        Move bestMove = moves[0];

        float max = float.MinValue;

        //loop thorugh root tree to get move associated with best score
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float score = -search(board, 2);
            board.UndoMove(move);
            if (score > max)
            {
                max = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

}