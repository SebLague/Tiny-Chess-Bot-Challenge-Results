namespace auto_Bot_20;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Bot_20 : IChessBot
{
    private int currentSearchDepth = 2;
    private int savedSearchDepth = 2;
    private Random random = new Random();

    private static readonly Dictionary<string, int> OpeningBook = new Dictionary<string, int>
{
    { "e2e4", 60 },
    { "d2d4", 50 },
    { "g1f3", 40 },
    { "c2c4", 30 },
    { "g2g3", 20 },
    { "c2c3", 20 },
    { "Nf3e5", 10 },
    { "Nf3d2", 10 },
    { "Nc3e5", 5 },
    { "Nf3g5", 5 },
    { "Nf3h5", 5 }
};

    private static readonly int[] PieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    private static readonly int[] PawnSquareTable =
    {
        0,  0,  0,  0,  0,  0,  0,  0,
        5, 10, 10,-20,-20, 10, 10,  5,
        5, -5,-10,  0,  0,-10, -5,  5,
        0,  0,  0, 20, 20,  0,  0,  0,
        5,  5, 10, 25, 25, 10,  5,  5,
        10, 10, 20, 30, 30, 20, 10, 10,
        50, 50, 50, 50, 50, 50, 50, 50,
        0,  0,  0,  0,  0,  0,  0,  0
    };

    private static readonly int[] CenterSquareTable =
    {
        -20, -10, -10, -10, -10, -10, -10, -20,
        -10,   0,   0,   0,   0,   0,   0, -10,
        -10,   0,   5,   5,   5,   5,   0, -10,
        -10,   0,   5,  10,  10,   5,   0, -10,
        -10,   0,   5,  10,  10,   5,   0, -10,
        -10,   0,   5,   5,   5,   5,   0, -10,
        -10,   0,   0,   0,   0,   0,   0, -10,
        -20, -10, -10, -10, -10, -10, -10, -20
    };

    public Move Think(Board board, Timer timer)
    {
        Move[] legalMoves = board.GetLegalMoves();

        // Check for an opening move.
        if (board.PlyCount < 4)
        {
            string fen = board.GetFenString();
            if (OpeningBook.TryGetValue(fen, out int openingValue))
            {
                var openingMoves = legalMoves.Where(move => fen.EndsWith(move.ToString()));
                if (openingMoves.Any())
                    return openingMoves.OrderByDescending(move => move.CapturePieceType != PieceType.None).First();
            }
        }



        Move bestMove = legalMoves[0];
        int bestMoveValue = int.MinValue;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            int moveValue = AlphaBeta(board, currentSearchDepth, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);

            if (moveValue > bestMoveValue)
            {
                bestMove = move;
                bestMoveValue = moveValue;
            }
        }
        currentSearchDepth = savedSearchDepth;

        // Dynamic depth adjustment
        if (random.Next(1, 101) <= 40)
        {
            currentSearchDepth++;
        }

        int totalPieces = board.GetAllPieceLists().SelectMany(list => list).Count();
        int piecesRemaining = totalPieces - legalMoves.Count(move => move.CapturePieceType != PieceType.None);

        if (piecesRemaining <= totalPieces / 5)
        {
            currentSearchDepth = savedSearchDepth = 5;
        }
        else if (piecesRemaining <= totalPieces / 4)
        {
            currentSearchDepth = savedSearchDepth = 4;
        }
        else if (piecesRemaining <= totalPieces / 2)
        {
            currentSearchDepth = savedSearchDepth = 3;
        }

        return bestMove;
    }

    private int AlphaBeta(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            return EvaluateBoard(board, board.IsWhiteToMove);

        if (maximizingPlayer)
        {
            Move[] legalMoves = board.GetLegalMoves();
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                alpha = Math.Max(alpha, AlphaBeta(board, depth - 1, alpha, beta, false));
                board.UndoMove(move);

                if (beta <= alpha)
                    break;
            }
            return alpha;
        }
        else
        {
            Move[] legalMoves = board.GetLegalMoves();
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                beta = Math.Min(beta, AlphaBeta(board, depth - 1, alpha, beta, true));
                board.UndoMove(move);

                if (beta <= alpha)
                    break;
            }
            return beta;
        }
    }

    private int EvaluateBoard(Board board, bool isWhite)
    {
        int numQueens = board.GetAllPieceLists().SelectMany(list => list).Count(piece => piece.IsQueen);
        int numPawns = board.GetAllPieceLists().SelectMany(list => list).Count(piece => piece.IsPawn);

        int promotionRank = isWhite ? 7 : 0;
        int numPromotablePawns = board.GetPieceList(PieceType.Pawn, isWhite)
                                      .Count(piece => piece.Square.Rank == promotionRank);

        int materialValue = 0;
        foreach (var list in board.GetAllPieceLists())
        {
            foreach (var piece in list)
            {
                int pieceValue = PieceValues[(int)piece.PieceType];
                materialValue += (piece.IsWhite == isWhite) ? pieceValue : -pieceValue;
            }
        }

        int promotionValue = 300; // Increased promotion bonus for the queen maker

        // Evaluate pawn positions based on the piece-square table
        int pawnPositionValue = 0;
        foreach (var pawn in board.GetPieceList(PieceType.Pawn, isWhite))
        {
            int squareValue = PawnSquareTable[pawn.Square.Index];
            pawnPositionValue += isWhite ? squareValue : -squareValue;
        }

        // Evaluate control of the center based on the center-square table
        int centerControlValue = 0;
        foreach (var piece in board.GetPieceList(PieceType.Knight, isWhite).Concat(board.GetPieceList(PieceType.Bishop, isWhite)))
        {
            int squareValue = CenterSquareTable[piece.Square.Index];
            centerControlValue += isWhite ? squareValue : -squareValue;
        }

        int checkValue = 150;
        int evaluationValue = materialValue + numQueens * 1000 + numPromotablePawns * promotionValue + pawnPositionValue + centerControlValue;

        Move[] legalMoves = board.GetLegalMoves();
        int captureValue = 100;
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
                evaluationValue += checkValue;
            else if (move.CapturePieceType != PieceType.None)
                evaluationValue += captureValue;
            board.UndoMove(move);
        }

        //the queen maker
        if (numPromotablePawns > 0)
        {
            int promoteToQueenValue = 500;
            int promoteToQueenMultiplier = 5;
            evaluationValue += promoteToQueenValue * numPromotablePawns * promoteToQueenMultiplier;
        }

        return evaluationValue;
    }
}

