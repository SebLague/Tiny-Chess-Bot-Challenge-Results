namespace auto_Bot_633;
using ChessChallenge.API;
using System;

public class Bot_633 : IChessBot
{
    int maxAccesses = 10;
    int maxDepth = 6;
    Random random = new Random();

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move toPlay = moves[random.Next(moves.Length)];

        bool checkFound = false;
        int highestScore = int.MinValue;
        for (int i = 0; i < maxAccesses; i++)
        {
            Move move = board.GetLegalMoves()[random.Next(0, board.GetLegalMoves().Length)];
            if (isCheckmate(board, move))
            {
                return toPlay;
            }
            if (isDraw(board, move))
            {
                continue;
            }
            board.MakeMove(move);
            int score = alphaBetaMax(int.MinValue, int.MaxValue, maxDepth, board);
            board.UndoMove(move);
            if (isCheck(board, move))
            {
                if (score >= highestScore)
                {
                    highestScore = score;
                    toPlay = move;
                }
                else
                {
                    if (!checkFound)
                    {
                        toPlay = move;
                        checkFound = true;
                    }
                }
            }

            if (!checkFound)
            {
                if (score >= highestScore)
                {
                    toPlay = move;
                    highestScore = score;
                }
            }
        }
        return toPlay;
    }

    int alphaBetaMax(int alpha, int beta, int depth, Board board)
    {
        int length = board.GetLegalMoves().Length;
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw() || length <= 0) return eval(board);
        for (int i = 0; i < maxAccesses; i++)
        {
            Move move = board.GetLegalMoves()[random.Next(0, length)];
            board.MakeMove(move);
            int score = alphaBetaMin(alpha, beta, depth - 1, board);
            board.UndoMove(move);
            if (score >= beta)
            {
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
            }
        }
        return alpha;
    }

    int alphaBetaMin(int alpha, int beta, int depth, Board board)
    {
        int length = board.GetLegalMoves().Length;

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw() || length <= 0) return -eval(board);
        for (int i = 0; i < maxAccesses; i++)
        {
            Move move = board.GetLegalMoves()[random.Next(0, length)];
            board.MakeMove(move);
            int score = alphaBetaMax(alpha, beta, depth - 1, board);
            board.UndoMove(move);
            if (score <= alpha)
            {
                return alpha;
            }
            if (score < beta)
            {
                beta = score;
            }
        }
        return beta;
    }

    private int eval(Board board)
    {
        int whiteScore = 0;
        int blackScore = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            if (list.IsWhitePieceList)
            {
                whiteScore += pieceScores(list.TypeOfPieceInList) * list.Count;
            }
            else
            {
                blackScore += pieceScores(list.TypeOfPieceInList) * list.Count;
            }
        }


        int whiteWithMobility = whiteScore + board.GetLegalMoves().Length;
        int blackWithMobility;
        if (board.TrySkipTurn())
        {
            blackWithMobility = blackScore + board.GetLegalMoves().Length;
            board.UndoSkipTurn();
            return whiteWithMobility - blackWithMobility;
        }
        return whiteScore - blackScore;


    }


    private bool isCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool result = board.IsInCheckmate();
        board.UndoMove(move);
        return result;
    }

    private bool isCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool result = board.IsInCheck();
        board.UndoMove(move);
        return result;
    }

    private bool isDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool result = board.IsDraw();
        board.UndoMove(move);
        return result;
    }
    private int pieceScores(PieceType type)
    {
        switch (type)
        {
            case PieceType.None: return 0;
            case PieceType.Pawn: return 1;
            case PieceType.Bishop: return 3;
            case PieceType.Knight: return 3;
            case PieceType.Rook: return 5;
            case PieceType.Queen: return 9;
            case PieceType.King: return 10;
            default: return 0;
        }
    }
}