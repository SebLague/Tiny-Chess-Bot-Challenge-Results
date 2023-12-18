namespace auto_Bot_142;
using ChessChallenge.API;
using System;

public class Bot_142 : IChessBot
{
    private readonly int[,] PawnScores = new int[8, 8]
    {
        { 0,  0,  0,  0,  0,  0,  0,  0 },
        { 50, 50, 50, 50, 50, 50, 50, 50 },
        { 10, 10, 20, 30, 30, 20, 10, 10 },
         { 5,  5, 10, 25, 25, 10,  5,  5 },
         { 0,  0,  0, 20, 20,  0,  0,  0 },
         { 5, -5,-10,  0,  0,-10, -5,  5 },
         { 5, 10, 10,-20,-20, 10, 10,  5 },
         { 0,  0,  0,  0,  0,  0,  0,  0 }
    };
    /*
    private readonly int[] PawnScores = Array.ConvertAll("000000005050505050501010203030201050505000050-5-1000-10-5-51010-20201050".ToCharArray(), c => c - '0');
    */

    private readonly int[,] KnightScores = new int[8, 8]
    {
       { -50,-40,-30,-30,-30,-30,-40,-50 },
       { -40,-20,  0,  0,  0,  0,-20,-40 },
       { -30,  0, 10, 15, 15, 10,  0,-30 },
       { -30,  5, 15, 20, 20, 15,  5,-30 },
       { -30,  0, 15, 20, 20, 15,  0,-30 },
       { -30,  5, 10, 15, 15, 10,  5,-30 },
       { -40,-20,  0,  5,  5,  0,-20,-40 },
       { -50,-40,-30,-30,-30,-30,-40,-50 }
    };

    private readonly int[,] BishopScores = new int[8, 8]
    {
       { -20,-10,-10,-10,-10,-10,-10,-20 },
       { -10,  0,  0,  0,  0,  0,  0,-10 },
       { -10,  0,  5, 10, 10,  5,  0,-10 },
       { -10,  5,  5, 10, 10,  5,  5,-10 },
       { -10,  0, 10, 10, 10, 10,  0,-10 },
       { -10, 10, 10, 10, 10, 10, 10,-10 },
       { -10,  5,  0,  0,  0,  0,  5,-10 },
       { -20,-10,-10,-10,-10,-10,-10,-20 }
    };

    private readonly int[,] RookScores = new int[8, 8]
    {
         { 0,  0,  0,  0,  0,  0,  0,  0 },
         { 5, 10, 10, 10, 10, 10, 10,  5 },
         {-5,  0,  0,  0,  0,  0,  0, -5 },
         {-5,  0,  0,  0,  0,  0,  0, -5 },
         {-5,  0,  0,  0,  0,  0,  0, -5 },
         {-5,  0,  0,  0,  0,  0,  0, -5 },
         {-5,  0,  0,  0,  0,  0,  0, -5 },
         { 0,  0,  0,  5,  5,  0,  0,  0 }
    };

    private readonly int[,] QueenScores = new int[8, 8]
    {
        {-20,-10,-10, -5, -5,-10,-10,-20 },
        {-10,  0,  0,  0,  0,  0,  0,-10 },
        {-10,  0,  5,  5,  5,  5,  0,-10 },
        { -5,  0,  5,  5,  5,  5,  0, -5 },
        {  0,  0,  5,  5,  5,  5,  0, -5 },
        {-10,  5,  5,  5,  5,  5,  0,-10 },
        {-10,  0,  5,  0,  0,  0,  0,-10 },
        {-20,-10,-10, -5, -5,-10,-10,-20 }
    };


    public Move Think(Board board, Timer timer)
    {
        DivertedConsole.Write("eval: {0}", Evaluate(board));

        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = legalMoves[0];
        float bestScore = float.MinValue;

        bool isWhite = board.IsWhiteToMove;

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            float score = Minimax(board, 3, board.IsWhiteToMove, float.MinValue, float.MaxValue);
            // float score = MonteCarloSearch(board);

            board.UndoMove(move);

            if (!isWhite)
            {
                score = -score;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        return bestMove;
    }

    /*
    private float MonteCarloSearch(Board board)
    {
        int playouts = 500; // Number of playouts to perform
        int totalScore = 0;

        for (int i = 0; i < playouts; i++)
        {
            totalScore += MiniMaxPlayout(board, 6);
        }

        return (float)totalScore / (float)playouts; // Average score
    }

    private int MiniMaxPlayout(Board board, int Depth)
    {
        if (board.IsDraw())
        {
            return 0;
        }

        if (board.IsInCheckmate())
        {
            if (!board.IsWhiteToMove)
            {
                return 200;
            }
            return -200;
        }

        if (Depth == 0)
        {
            float score = Evaluate(board);
            return (int)score;
        }

        Random rng = new Random();

        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = legalMoves[rng.Next(legalMoves.Length)];
        float bestScore = float.MinValue;

  
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            float score = Evaluate(board) + rng.Next(-300, 300);
            board.UndoMove(move);

            if (!board.IsWhiteToMove)
            {
                score = -score;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        board.MakeMove(bestMove);
        int result = MiniMaxPlayout(board, Depth - 1);
        board.UndoMove(bestMove);
        return result;
    }
    */

    public float Evaluate(Board board)
    {

        if (board.IsDraw())
        {
            return 0;
        }

        if (board.IsInCheckmate())
        {
            if (!board.IsWhiteToMove)
            {
                return 200;
            }
            return -200;
        }

        float whiteScore = 0;
        float blackScore = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            float pieceTypeValue = 0;

            foreach (Piece piece in pieceList)
            {
                int a_h = 7 - piece.Square.File;
                int one_eight = piece.Square.Rank;

                if (pieceList.IsWhitePieceList)
                {
                    one_eight = 7 - one_eight;
                    a_h = 7 - a_h;
                }

                switch (piece.PieceType)
                {
                    case PieceType.None:
                        break;
                    case PieceType.Pawn:
                        pieceTypeValue += 100 + PawnScores[one_eight, a_h];
                        break;
                    case PieceType.Knight:
                        pieceTypeValue += 320 + KnightScores[one_eight, a_h];
                        break;
                    case PieceType.Bishop:
                        pieceTypeValue += 330 + BishopScores[one_eight, a_h];
                        break;
                    case PieceType.Rook:
                        pieceTypeValue += 500 + RookScores[one_eight, a_h];
                        break;
                    case PieceType.Queen:
                        pieceTypeValue += 900 + QueenScores[one_eight, a_h];
                        break;
                    case PieceType.King:
                        break;
                    default:
                        break;
                }
            }

            if (pieceList.IsWhitePieceList)
            {
                whiteScore += pieceTypeValue;
            }
            else
            {
                blackScore += pieceTypeValue;
            }
        }

        return (whiteScore - blackScore) / 100;
    }


    // The minimax algorithm with alpha-beta pruning
    public float Minimax(Board board, int depth, bool isMaximizingPlayer, float alpha, float beta)
    {
        Move[] legalMoves = board.GetLegalMoves();

        if (depth == 0 || legalMoves.Length == 0)
        {
            return Evaluate(board);
        }


        if (isMaximizingPlayer)
        {
            float maxEval = float.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float eval = Minimax(board, depth - 1, false, alpha, beta);
                board.UndoMove(move);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (alpha >= beta)
                {
                    break; // Beta pruning
                }
            }
            return maxEval;
        }
        else
        {
            float minEval = float.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                float eval = Minimax(board, depth - 1, true, alpha, beta);
                board.UndoMove(move);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (alpha >= beta)
                {
                    break; // Alpha pruning
                }
            }
            return minEval;
        }
    }
}