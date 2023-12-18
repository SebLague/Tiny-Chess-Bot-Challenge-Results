namespace auto_Bot_189;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_189 : IChessBot
{
    public int EvaluateBoard(Board board, bool isDraw)
    {
        int score = 0;
        PieceList[] list = board.GetAllPieceLists();
        int PiecesNumber = list.Length;
        Square myKingSquare = new Square("a8");
        Square oppKingSquare = new Square("a1");
        int kingmalus;
        bool me = board.IsWhiteToMove;
        for (int i = 0; i < PiecesNumber; i++)
        {
            for (int j = 0; j < list[i].Count(); j++)
            {

                Piece piece = list[i].GetPiece(j);
                int multiple = 1;
                kingmalus = 0;
                if (piece.PieceType == PieceType.King)
                {
                    if ((me && piece.IsWhite) || (!me && !piece.IsWhite))
                        myKingSquare = piece.Square;
                    else
                        oppKingSquare = piece.Square;
                }
                if (board.PlyCount < 15)
                {
                    multiple = 2;
                    kingmalus = 10;
                }
                if (board.PlyCount < 12)
                {
                    if (me)
                    {
                        if (board.GetPiece(new Square("d1")).IsNull)
                            score -= 3;
                        if (board.GetPiece(new Square("b1")).IsNull)
                            score += 1;
                        if (board.GetPiece(new Square("g1")).IsNull)
                            score += 1;
                    }
                    else
                    {
                        if (board.GetPiece(new Square("d8")).IsNull)
                            score += 3;
                        if (board.GetPiece(new Square("b8")).IsNull)
                            score -= 1;
                        if (board.GetPiece(new Square("g8")).IsNull)
                            score -= 1;
                    }
                }
                switch (piece.PieceType)
                {

                    case PieceType.Pawn:
                        score += (piece.IsWhite ? 1 : -1) * 10;
                        if (piece.IsWhite)
                        {


                            score += (piece.Square.Rank / 2 + 2) * multiple;
                            if (piece.Square.File >= 3 && piece.Square.File <= 5 && piece.Square.Rank > 3)
                                score += 4 * multiple;
                        }
                        else
                        {
                            score += (-8 + piece.Square.Rank / 2 - 2) * multiple;
                            if (piece.Square.File >= 3 && piece.Square.File <= 5 && piece.Square.Rank < 5)
                                score -= 4 * multiple;
                        }
                        break;
                    case PieceType.Knight:
                        score += (piece.IsWhite ? 1 : -1) * 31;
                        break;
                    case PieceType.Bishop:
                        score += (piece.IsWhite ? 1 : -1) * 33;
                        break;
                    case PieceType.Rook:
                        score += (piece.IsWhite ? 1 : -1) * 56;
                        break;
                    case PieceType.Queen:
                        score += (piece.IsWhite ? 1 : -1) * 95;
                        break;
                    case PieceType.King:
                        if (piece.IsWhite && piece.Square.Rank > 1)
                            score -= kingmalus;
                        if (!piece.IsWhite && piece.Square.Rank < 7)
                            score += kingmalus;
                        break;
                }
            }
        }
        if (board.PlyCount >= 40)
        {

            int distance = Math.Abs(myKingSquare.File - oppKingSquare.File) + Math.Abs(myKingSquare.Rank - oppKingSquare.Rank);

            int modifier = board.IsWhiteToMove ? 1 : -1;

            score += modifier * (10 - distance) * 3;
        }

        if (isDraw)
        {
            if (board.IsWhiteToMove && score >= 10) // Bot is white
                score = -2000;
            else if (!board.IsWhiteToMove && score <= -10)
                score = 2000;
        }
        return score;
    }

    private bool wasDrawDetected = false;

    public (Move, int) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer, bool isDraw)
    {
        Move[] moves;
        if (depth == 1)
            moves = board.GetLegalMoves(true);
        else
            moves = board.GetLegalMoves(false);
        if (moves.Length == 0)
            moves = board.GetLegalMoves(false);
        Move bestMove = Move.NullMove;
        int maxEval;
        int minEval;
        if (isDraw || board.IsDraw())
            wasDrawDetected = true;
        if (depth == 0)
        {
            return (Move.NullMove, EvaluateBoard(board, wasDrawDetected));
        }
        if (maximizingPlayer)
        {
            maxEval = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                if (board.IsDraw())
                    wasDrawDetected = true;

                int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, wasDrawDetected).Item2;

                board.UndoMove(move);
                if (wasDrawDetected && !board.IsDraw())
                    wasDrawDetected = false;

                if (maxEval < eval)
                {
                    maxEval = eval;
                    bestMove = move;
                }

                if (eval >= beta)
                {
                    break;
                }

                alpha = Math.Max(alpha, eval);
            }
            return (bestMove, maxEval);
        }
        else
        {
            minEval = int.MaxValue;
            foreach (Move move in moves)
            {


                board.MakeMove(move);
                if (board.IsDraw())
                    wasDrawDetected = true;

                int eval = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer, wasDrawDetected).Item2;

                board.UndoMove(move);
                if (wasDrawDetected && !board.IsDraw())
                    wasDrawDetected = false;

                if (minEval > eval)
                {
                    minEval = eval;
                    bestMove = move;
                }

                if (eval <= alpha)
                {
                    break;
                }

                beta = Math.Min(beta, eval);
            }
            return (bestMove, minEval);
        }

    }

    public Move Process(Board board, Timer timer)
    {
        int initialDepth = 4;
        int nbMove = board.GetLegalMoves().Length;
        if (nbMove < 3)
            initialDepth = 6;
        else if (nbMove < 15)
            initialDepth = 5;
        if (timer.MillisecondsRemaining < 5000)
            initialDepth = 4;
        Move bestMove = MiniMax(board, initialDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove, false).Item1;

        if (bestMove == Move.NullMove)
            bestMove = board.GetLegalMoves()[0];
        return bestMove;
    }

    public Move Think(Board board, Timer timer)
    {
        Move move = Process(board, timer);
        return move;
    }
}