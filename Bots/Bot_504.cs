namespace auto_Bot_504;
using ChessChallenge.API;

public class Bot_504 : IChessBot
{
    public int MIN_SCORE = -10000;
    public int MAX_SCORE = 10000;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Move[] possibleMoves = new Move[moves.Length];
        int nbPossibleMoves = 0;

        System.Random rng = new();

        int bestScore = MIN_SCORE;

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            board.MakeMove(move);

            int score = ComputeScore(board, move);

            if (score > bestScore)
            {
                bestScore = score;
                nbPossibleMoves = 1;
                possibleMoves[0] = move;
            }
            else if (score == bestScore)
            {
                possibleMoves[nbPossibleMoves] = move;
                nbPossibleMoves++;
            }

            board.UndoMove(move);
        }

        int chosenMoveIndex = rng.Next(nbPossibleMoves);

        return possibleMoves[chosenMoveIndex];
    }
    public int ComputeScore(Board board, Move move)
    {
        int score = 0;

        bool iAmWhite = !board.IsWhiteToMove;

        if (board.IsInCheckmate())
        {
            score = MAX_SCORE;
            return score;
        }

        if (board.IsDraw())
        {
            score = MIN_SCORE;
            return score;
        }

        if (move.IsCastles)
        {
            score += 5;
        }

        Move[] opponentMoves = board.GetLegalMoves();

        int worstScore = MAX_SCORE;

        // Iteration 2
        for (int i = 0; i < opponentMoves.Length; i++)
        {
            Move opponentMove = opponentMoves[i];
            board.MakeMove(opponentMove);
            if (board.IsInCheckmate())
            {
                score = MIN_SCORE;
                board.UndoMove(opponentMove);

                return score;
            }

            // Iteration 3
            int bestScore = MIN_SCORE;
            Move[] myNextMoves = board.GetLegalMoves();

            for (int j = 0; j < myNextMoves.Length; j++)
            {
                Move myNextMove = myNextMoves[j];
                board.MakeMove(myNextMove);

                if (board.IsInCheckmate())
                {
                    bestScore = MAX_SCORE;
                    board.UndoMove(myNextMove);
                    break;
                }

                int currentScore = (ComputeBoardPoints(board, iAmWhite) - ComputeBoardPoints(board, !iAmWhite));
                if (currentScore > bestScore)
                {
                    bestScore = currentScore;
                }

                board.UndoMove(myNextMove);
            }

            if (bestScore < worstScore)
            {
                worstScore = bestScore;
            }
            // End iteration 3

            board.UndoMove(opponentMove);
        }
        // End iteration 2

        if (board.IsInCheck() && !board.SquareIsAttackedByOpponent(move.TargetSquare))
            score += 1;

        score += worstScore;

        return score;
    }

    public int ComputeBoardPoints(Board board, bool forWhite)
    {
        int points = 0;

        points += board.GetPieceList(PieceType.Queen, forWhite).Count * 9;
        points += board.GetPieceList(PieceType.Rook, forWhite).Count * 5;
        points += board.GetPieceList(PieceType.Bishop, forWhite).Count * 3;
        points += board.GetPieceList(PieceType.Knight, forWhite).Count * 3;
        points += board.GetPieceList(PieceType.Pawn, forWhite).Count * 1;
        points *= 10;

        if (board.HasKingsideCastleRight(forWhite))
            points += 2;
        if (board.HasQueensideCastleRight(forWhite))
            points += 1;

        foreach (Piece queen in board.GetPieceList(PieceType.Queen, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Queen, queen.Square, board));
        foreach (Piece rook in board.GetPieceList(PieceType.Rook, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Rook, rook.Square, board));
        foreach (Piece bishop in board.GetPieceList(PieceType.Bishop, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(PieceType.Bishop, bishop.Square, board));
        foreach (Piece knight in board.GetPieceList(PieceType.Knight, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKnightAttacks(knight.Square));
        foreach (Piece pawn in board.GetPieceList(PieceType.Pawn, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPawnAttacks(pawn.Square, forWhite));
        foreach (Piece king in board.GetPieceList(PieceType.King, forWhite))
            points += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(king.Square));

        return points;
    }
}