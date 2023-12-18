namespace auto_Bot_72;
using ChessChallenge.API;

public class Bot_72 : IChessBot
{
    // somewhat aggressive (idk i don't play chess)

    // priorities big pushes of weak pieces to the center
    private float PushScore(bool white, Board board, Move move)
    {
        int startSquare = 0;
        int endSquare = 0;
        float salt = new System.Random().Next() / 10f;

        float pieceBonus = 0f;
        // these values are entirely meaningless
        switch (move.MovePieceType)
        {
            case PieceType.Pawn:
                pieceBonus = 0.9f;
                break;
            case PieceType.Knight:
                pieceBonus = 0.3f;
                break;
            case PieceType.Rook:
                pieceBonus = 0.25f;
                break;
            case PieceType.Bishop:
                pieceBonus = 0.27f;
                break;
            case PieceType.Queen:
                pieceBonus = 0.1f;
                break;
            case PieceType.King:
                pieceBonus = -0.1f;
                break;
        }

        if (move.IsCapture)
        {
            switch (move.CapturePieceType)
            {
                case PieceType.Knight:
                    pieceBonus += 0.25f;
                    break;
                case PieceType.Rook:
                    pieceBonus += 0.4f;
                    break;
                case PieceType.Bishop:
                    pieceBonus += 0.3f;
                    break;
                case PieceType.Queen:
                    pieceBonus += 20f;
                    break;
            }
        }

        if (white)
        {
            startSquare = move.StartSquare.Rank;
            endSquare = move.TargetSquare.Rank;
        }
        else
        {
            startSquare = 8 - move.StartSquare.Rank;
            endSquare = 8 - move.TargetSquare.Rank;
        }

        float midBonus = 0f;
        if (move.TargetSquare.File >= 4)
        {
            midBonus = 4f - 0.5f * move.TargetSquare.File;
        }
        else
        {
            midBonus = 0.5f * move.TargetSquare.File;
        }

        float enemyMovePenalty = 0f;

        board.MakeMove(move);
        Move[] enemyMoves = board.GetLegalMoves();
        foreach (Move enemyMove in enemyMoves)
        {
            board.MakeMove(enemyMove);
            enemyMovePenalty -= 0.2f;
            if (enemyMove.IsCapture)
            {
                enemyMovePenalty -= 0.3f;
            }
            if (board.IsInCheck())
            {
                enemyMovePenalty -= 0.7f;
            }
            if (board.IsInCheckmate())
            {
                enemyMovePenalty -= 100f;
            }
            board.UndoMove(enemyMove);
        }
        board.UndoMove(move);


        return (8 - startSquare) + 2 * (endSquare - startSquare) + salt + pieceBonus + midBonus + enemyMovePenalty;
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        // take checkmate
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                return move;
            }

            board.UndoMove(move);
        }

        bool isWhite = board.IsWhiteToMove;
        float highestScore = float.NegativeInfinity;
        Move? bestMove = null;

        // take best promotion

        foreach (Move move in moves)
        {
            float score = PushScore(isWhite, board, move);

            if (move.IsPromotion && score > highestScore && move.PromotionPieceType == PieceType.Queen)
            {
                highestScore = score;
                bestMove = move;
            }
        }

        if (bestMove != null) return bestMove ?? moves[0];

        // take best check
        foreach (Move move in moves)
        {
            board.MakeMove(move);

            bool check = board.IsInCheck();

            board.UndoMove(move);

            float score = PushScore(isWhite, board, move);

            if (check && score > highestScore)
            {
                highestScore = score;
                bestMove = move;
            }

        }

        if (bestMove != null) return bestMove ?? moves[0];

        // take best capture
        foreach (Move move in moves)
        {
            float score = PushScore(isWhite, board, move);

            if (move.IsCapture && score > highestScore)
            {
                highestScore = score;
                bestMove = move;
            }
        }

        if (bestMove != null) return bestMove ?? moves[0];

        // push a piece that is further back forward
        foreach (Move move in moves)
        {
            float score = PushScore(isWhite, board, move);
            if (score > highestScore)
            {
                bestMove = move;
                highestScore = score;
            }
        }

        // uhh?? checkmate???
        return bestMove ?? moves[0];
    }
}