namespace auto_Bot_556;
using ChessChallenge.API;
using System.Linq;
using static System.Math;

public class Bot_556 : IChessBot
{
    int currentDepth;
    double moveTimeModifier = 0.025;
    double maxTime;

    public Move Think(Board board, Timer timer)
    {
        maxTime = timer.MillisecondsRemaining * moveTimeModifier;
        moveTimeModifier += 0.005;
        if (moveTimeModifier > 0.1) moveTimeModifier = 0.075;

        if (maxTime > 60000) maxTime = 60000;
        if (timer.OpponentMillisecondsRemaining > timer.MillisecondsRemaining) maxTime *= 0.5;

        Move bestMove = board.GetLegalMoves()[0];
        if (board.GetLegalMoves().Length == 1) return bestMove;

        // iterative deepening until time surpasses maxTime milliseconds
        for (int depth = 1; timer.MillisecondsElapsedThisTurn < maxTime; depth++)
        {
            currentDepth = depth;

            (int evaluation, Move move, bool cancelled) = MiniMax(board, depth, -2147483647, 2147483647, board.IsWhiteToMove, timer);
            if (!cancelled)
            {
                bestMove = move;
                if (Abs(evaluation) > 99900) break;
            }
        }
        return bestMove;
    }

    int EvaluateBoard(Board board, int compensation)
    {
        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -100000 + compensation : 100000 - compensation;
        if (board.IsDraw()) return 0;

        int score = 0;
        int[] PieceValues = { 0, 105, 320, 340, 550, 1000, 0 };

        int[] PawnValues = { 0, 0, 5, 20, 45, 75, 90, 0 };

        // int[] KnightValues = new List<int> { 0, 0, 2, 5, 5, 2, 0, 0 }.SelectMany(x => (new int[] { x, x, x, x, x, x, x, x })).ToArray();
        int startSquarePunishment = -50;
        int[] KnightValues = {
            0, startSquarePunishment, 2, 5, 5, 2, startSquarePunishment, 0,
            0, 0, 5, 35, 35, 5, 0, 0,
            10, 5, 50, 25, 25, 50, 5, 10,
            0, 5, 20, 50, 50, 20, 5, 0,
            0, 5, 20, 50, 50, 20, 5, 0,
            10, 5, 50, 25, 25, 50, 5, 10,
            0, 0, 5, 35, 35, 5, 0, 0,
            0, startSquarePunishment, 2, 5, 5, 2, startSquarePunishment, 0
        };

        int[] BishopValues = {
            0, 0, startSquarePunishment, 0, 0, startSquarePunishment, 0, 0,
            2, 40, 0, 12, 20, 0, 40, 2,
            0, 5, 20, 25, 25, 20, 5, 0,
            0, 35, 50, 10, 10, 50, 35, 0,
            0, 35, 50, 10, 10, 50, 35, 0,
            0, 5, 20, 25, 25, 20, 5, 0,
            2, 40, 0, 12, 20, 0, 40, 2,
            0, 0, startSquarePunishment, 0, 0, startSquarePunishment, 0, 0
        };

        PieceList[] allPieceLists = board.GetAllPieceLists();

        foreach (PieceList pieces in allPieceLists)
        {
            int isWhiteMultiplier = pieces.IsWhitePieceList ? 1 : -1;
            score += PieceValues[(int)pieces.TypeOfPieceInList] * pieces.Count * isWhiteMultiplier;

            foreach (Piece piece in pieces)
            {
                if (pieces.TypeOfPieceInList == PieceType.Pawn)
                    if (piece.IsWhite) score += PawnValues[piece.Square.Rank];
                    else score -= PawnValues[7 - piece.Square.Rank];

                if (pieces.TypeOfPieceInList == PieceType.Knight)
                    score += KnightValues[piece.Square.Index] * isWhiteMultiplier;

                if (pieces.TypeOfPieceInList == PieceType.Bishop)
                    score += BishopValues[piece.Square.Index] * isWhiteMultiplier;
            }
        }

        if (board.IsRepeatedPosition()) score = (int)(score * 0.5);

        int whitePieces = BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard);
        int blackPieces = BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard);

        Square whiteKingSquare = board.GetKingSquare(true);
        Square blackKingSquare = board.GetKingSquare(false);

        // calculate opposite king space in king + rook/queen endgames
        if (whitePieces + blackPieces == 3)
            if (!board.IsInCheck())
            {
                Piece piece = allPieceLists.Where(x => x.Count == 1 && x.TypeOfPieceInList != PieceType.King).First()[0];
                Square king = piece.IsWhite ? blackKingSquare : whiteKingSquare;

                int pFile = piece.Square.File;
                int pRank = piece.Square.Rank;

                int direction = (king.Rank - pRank < 0) ? ((king.File - pFile < 0) ? 2 : 3) : ((king.File - pFile < 0) ? 0 : 1);
                int isWhiteMultiplier = piece.IsWhite ? -15 : 15;

                int rankScore = (direction == 0 || direction == 1) ? (7 - pRank) : pRank;
                int fileScore = (direction == 1 || direction == 3) ? (7 - pFile) : pFile;

                if (piece.IsRook) score = score * 10 + rankScore * fileScore * isWhiteMultiplier;
                else if (piece.IsQueen) score = score * 10 + (rankScore * fileScore - Min(rankScore, fileScore)) * isWhiteMultiplier;
            }

        if (whitePieces + blackPieces <= 10 && whitePieces != blackPieces) score += (Abs(whiteKingSquare.File - blackKingSquare.File) + Abs(whiteKingSquare.Rank - blackKingSquare.Rank)) * (whitePieces > blackPieces ? -10 : 10);

        return board.IsWhiteToMove ? score - compensation : score + compensation;
    }

    (int evaluation, Move move, bool cancelled) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, Timer timer)
    {
        bool cancelled = false;
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate()) return (EvaluateBoard(board, currentDepth - depth), Move.NullMove, false);

        Move bestMove = new();
        int isWhiteMultiplier = maximizingPlayer ? 1 : -1;
        int maxOrMinEval = isWhiteMultiplier * -2147483647;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, timer).evaluation;
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > maxTime)
            {
                cancelled = true;
                break;
            }

            if (eval * isWhiteMultiplier > maxOrMinEval * isWhiteMultiplier)
            {
                maxOrMinEval = eval;
                bestMove = move;
            }

            if (maximizingPlayer) alpha = Max(alpha, eval);
            else beta = Min(beta, eval);
            if (beta <= alpha) break;
        }
        return (maxOrMinEval, bestMove, cancelled);
    }
}