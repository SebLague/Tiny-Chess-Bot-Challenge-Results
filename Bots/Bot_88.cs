namespace auto_Bot_88;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;






public class Bot_88 : IChessBot
{

    private const int MaxDepth = 4;
    private bool myColor;
    private Board board;
    int[] pawnTable = new int[]
{
    0,  0,  0,  0,  0,  0,  0,  0,
    50, 50, 50, 50, 50, 50, 50, 50,
    10, 10, 20, 35, 35, 20, 10, 10,
    5,  5, 10, 30, 30, 10,  5,  5,
    0,  0,  0, 25, 25,  0,  0,  0,
    5, 10,-10,  0,  0,-10, 10,  5,
    5, 5, 10,-20,-20, 10, 5,  5,
    0,  0,  0,  0,  0,  0,  0,  0
};
    // Piece-square table for kings (middle-game)
    int[] kingMiddleGameTable = new int[]
    {
   -30,-40,-40,-50,-50,-40,-40,-30,
   -30,-40,-40,-50,-50,-40,-40,-30,
   -30,-40,-40,-50,-50,-40,-40,-30,
   -30,-40,-40,-50,-50,-40,-40,-30,
   -20,-30,-30,-40,-40,-30,-30,-20,
   -10,-20,-20,-20,-20,-20,-20,-10,
    20, 20,  0,  0,  0,  0, 20, 20,
    20, 50, 40,  0,  0, 10, 50, 20
    };

    // Piece-square table for kings (endgame)
    int[] kingEndGameTable = new int[]
    {
   -50,-40,-30,-20,-20,-30,-40,-50,
   -30,-20,-10,  0,  0,-10,-20,-30,
   -30,-10, 20, 30, 30, 20,-10,-30,
   -30,-10, 30, 40, 40, 30,-10,-30,
   -30,-10, 30, 40, 40, 30,-10,-30,
   -30,-10, 20, 30, 30, 20,-10,-30,
   -30,-30,  0,  0,  0,  0,-30,-30,
   -50,-30,-30,-30,-30,-30,-30,-50
    };
    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        myColor = board.IsWhiteToMove;

        List<Move> legalMoves = board.GetLegalMoves().ToList();

        if (legalMoves.Count == 1 || timer.MillisecondsRemaining < 300) return legalMoves[0]; //Preventing the bot to lose on time or waste time for calculating the only move

        double bestScore = myColor ? double.MinValue : double.MaxValue;
        Move bestMove = legalMoves[0];

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            double score = Minimax(MaxDepth - 1, double.MinValue, double.MaxValue, !myColor);
            if (board.IsInCheckmate()) { return move; }
            board.UndoMove(move);

            if ((score > bestScore && myColor) || (score < bestScore && !myColor))
            {
                bestScore = score;
                bestMove = move;
            }
        }



        return bestMove;
    }

    private double Minimax(int depth, double alpha, double beta, bool isMaximizing)
    {
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return isMaximizing ? double.MinValue : double.MaxValue;
        if (depth == 0) return EvaluateBoard();

        double eval, bestEval = isMaximizing ? double.MinValue : double.MaxValue;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            eval = Minimax(depth - 1, alpha, beta, !isMaximizing);
            board.UndoMove(move);

            if (isMaximizing)
            {
                if (eval > bestEval) bestEval = eval;
                if (eval > alpha) alpha = eval;
            }
            else
            {
                if (eval < bestEval) bestEval = eval;
                if (eval < beta) beta = eval;
            }

            if (beta <= alpha) break;
        }

        return bestEval;
    }

    private double EvaluateBoard()
    {
        double score = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                int value = GetPieceValue(piece.PieceType);
                double squareCount = GetPieceSquareCount(piece);

                switch (piece.PieceType)
                {
                    case PieceType.Rook: { squareCount = squareCount * 0.7; break; }
                    case PieceType.Queen: { squareCount = squareCount * 0.5; break; }

                }


                score += (pieceList.IsWhitePieceList ? 1 : -1) * (value + (squareCount / 20));



            }
        }
        return score;
    }


    private int GetPieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn: return 1;
            case PieceType.Knight: return 3;
            case PieceType.Bishop: return 3;
            case PieceType.Rook: return 5;
            case PieceType.Queen: return 9;
            case PieceType.King: return 100;
            default: return 0;
        }
    }
    private double GetPieceSquareCount(Piece piece)
    {
        ulong attacks = 0;
        int index = (piece.Square.Rank * 8) + piece.Square.File;
        if (!piece.IsWhite) index = 63 - index;
        switch (piece.PieceType)
        {
            case PieceType.Pawn:
                return pawnTable[index] / 5;
            case PieceType.Knight:
                attacks = BitboardHelper.GetKnightAttacks(piece.Square);
                break;
            case PieceType.Bishop:
                attacks = BitboardHelper.GetSliderAttacks(PieceType.Bishop, piece.Square, board.AllPiecesBitboard);
                break;
            case PieceType.Rook:
                attacks = BitboardHelper.GetSliderAttacks(PieceType.Rook, piece.Square, board.AllPiecesBitboard);
                break;
            case PieceType.Queen:
                attacks = BitboardHelper.GetSliderAttacks(PieceType.Queen, piece.Square, board.AllPiecesBitboard);
                break;
            case PieceType.King:
                int[] kingTable = (morePiecesThan(10)) ? kingMiddleGameTable : kingEndGameTable;
                return ((board.HasKingsideCastleRight(piece.IsWhite) || board.HasQueensideCastleRight(piece.IsWhite) ? 3 : 0 + kingTable[index])) / 4;
        }

        return BitboardToCount(attacks);
    }

    private int BitboardToCount(ulong bitboard)
    {
        int count = 0;
        for (int i = 0; i < 64; i++)
        {
            if (((bitboard >> i) & 1) == 1)
            {
                count++;
            }
        }
        return count;
    }
    private bool morePiecesThan(int count)
    {
        return board.GetAllPieceLists().Length < count;
    }

}