namespace auto_Bot_533;
using ChessChallenge.API;
public class Bot_533 : IChessBot
{
    bool isBotWhite;
    int maxSearchDepth = 5;
    int searchedMoves = 0;
    int scoreCorrection = 0;
    int actualScore = 0;
    int drawBonusTreshold = 3;
    int maxDepthExtenision = 3;
    string[] startingMoves = {
        "e2e4",
        "e7e5",
        "b1c3",
        "b8c6",
        "f1d3",
        "f8d6",
    };
    public Move Think(Board board, Timer timer)
    {
        scoreCorrection = 0;
        actualScore = CalculateScore(board, timer);

        //searchedMoves = 0;
        // What color is the bot
        if (board.PlyCount < 2)
        {
            isBotWhite = board.IsWhiteToMove;
        }

        if (board.PlyCount < 6)
        {
            return new Move(startingMoves[board.PlyCount], board);
        }
        if (board.PlyCount < 40)
        {
            maxSearchDepth = 4;
        }
        else
        {
            maxSearchDepth = 5;
        }

        Move[] moves = board.GetLegalMoves();

        Move bestMove = moves[0];

        int alpha = int.MinValue;
        int beta = int.MaxValue;

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            int currentScore = MiniMax(board, 0, 0, alpha, beta, timer);
            board.UndoMove(moves[i]);

            if (board.IsWhiteToMove && currentScore > alpha)
            {
                alpha = currentScore;
                bestMove = moves[i];
            }

            if (!board.IsWhiteToMove && currentScore < beta)
            {
                beta = currentScore;
                bestMove = moves[i];
            }
        }
        return bestMove;
    }

    public int MiniMax(Board board, int depth, int extendedDepth, int alpha, int beta, Timer timer)
    {
        depth++;

        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove) { return -100; }
            else { return 100; }
        }

        if (depth >= maxSearchDepth)
        {
            if (board.IsInCheck() && (board.IsWhiteToMove != isBotWhite) && extendedDepth <= maxDepthExtenision)
            {
                depth--;
                extendedDepth++;
            }
            else
            {
                return CalculateScore(board, timer);
            }
        }

        Move[] legalMoves = board.GetLegalMoves();

        int currentScore = 0;
        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            currentScore = MiniMax(board, depth, extendedDepth, alpha, beta, timer);
            board.UndoMove(legalMoves[i]);

            if (board.IsWhiteToMove && currentScore > alpha)
            {
                alpha = currentScore;
            }

            if (!board.IsWhiteToMove && currentScore < beta)
            {
                beta = currentScore;
            }

            if (alpha > beta)
            {
                break;
            }
        }

        if (board.IsDraw())
        {
            if (actualScore < -drawBonusTreshold && isBotWhite && alpha < -drawBonusTreshold)
            {
                scoreCorrection += 2;
            }

            if (actualScore > drawBonusTreshold && !isBotWhite && beta > drawBonusTreshold)
            {
                scoreCorrection += 2;
            }
        }

        if (board.IsWhiteToMove)
        {
            return alpha + scoreCorrection;
        }

        return beta + scoreCorrection;
    }

    public int CalculateScore(Board board, Timer timer)
    {
        int score = 0;

        PieceList[] pices = board.GetAllPieceLists();

        for (int i = 0; i < pices.Length; i++)
        {
            int value = 0;

            switch (pices[i].TypeOfPieceInList)
            {
                case (PieceType)1:
                    value = 1;
                    break;
                case (PieceType)2:
                    value = 3;
                    break;
                case (PieceType)3:
                    value = 3;
                    break;
                case (PieceType)4:
                    value = 5;
                    break;
                case (PieceType)5:
                    value = 9;
                    break;
                case (PieceType)6:
                    if (timer.MillisecondsRemaining > 45000)
                    {
                        value = -2;

                    }
                    break;
                default:
                    continue;
            }

            if (pices[i].IsWhitePieceList)
            {
                score += value * pices[i].Count;
            }
            else
            {
                score -= value * pices[i].Count;
            }

        }
        return score;
    }

}

