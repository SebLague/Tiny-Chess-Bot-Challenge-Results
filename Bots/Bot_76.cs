namespace auto_Bot_76;
using ChessChallenge.API;
using System;

public class Bot_76 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // Loads all the possible moves
        Move[] possibleMoves = board.GetLegalMoves(false);
        Square kingLocation = board.GetKingSquare(board.IsWhiteToMove);

        // Selects a random move by default in case no good move is found
        Move bestMove = possibleMoves[new Random().Next(possibleMoves.Length)];
        int bestOutcome = 6;
        double score = 999999;
        bool runFromKing = true;
        int[] pieceValue = { 0, 1, 3, 3, 5, 9, 0 };

        // Find whether the king can move forward
        foreach (Move currentMove in possibleMoves)
        {

            // Finds the highest-priority moves
            board.MakeMove(currentMove);
            int moveOutcome = board.IsInCheckmate() ? 6 : board.IsDraw() ? 5 : currentMove.IsPromotion ? 4 : currentMove.IsCapture ? 3 : 2;

            if (moveOutcome > 3 && moveOutcome > bestOutcome)
            {
                board.UndoMove(currentMove);
                continue;
            }

            // Makes sure this move avoids forcing it into checkmating the appointment, and encourages moves where the opponent is more likely to checkmate
            double moveSum = (double)currentMove.PromotionPieceType;
            double currentSum = 0;

            Move[] possibleEnemyMoves = board.GetLegalMoves(false);
            double num_enemy_checkmates = 0;

            foreach (Move enemyMove in possibleEnemyMoves)
            {
                board.MakeMove(enemyMove);
                if (board.IsInCheckmate()) { num_enemy_checkmates++; }
                else if (board.IsDraw()) { num_enemy_checkmates -= 0.5; }
                board.UndoMove(enemyMove);
            }

            if (num_enemy_checkmates == possibleEnemyMoves.Length)
            {
                bestMove = currentMove;
                board.UndoMove(currentMove);
                break;
            }

            if (num_enemy_checkmates >= 1 && moveOutcome < 4)
            {
                moveOutcome -= num_enemy_checkmates / possibleEnemyMoves.Length >= 0.5 ? 2 : 1;
            }

            if (moveOutcome < bestOutcome)
            {
                bestOutcome = moveOutcome;
                bestMove = currentMove;
                score = 999999;
            }
            else if (moveOutcome > bestOutcome)
            {
                board.UndoMove(currentMove);
                continue;
            }

            moveSum -= 1500 * num_enemy_checkmates / possibleEnemyMoves.Length;

            // Evaluates potential king moves
            if (currentMove.MovePieceType == (PieceType)6)
            {
                // Sums up distances from all the enemy's pieces to determine whether the current move will put the king in more or less danger
                for (int pieceType = 1; pieceType < 7; pieceType++)
                {
                    PieceList enemyPieces = board.GetPieceList((PieceType)pieceType, !board.IsWhiteToMove);
                    for (int i = 0; i < enemyPieces.Count; i++)
                    {
                        Square currentEnemyPlace = enemyPieces.GetPiece(i).Square;

                        moveSum += Math.Abs(1 - Math.Abs(currentEnemyPlace.File - currentMove.TargetSquare.File)) + 1.5 * Math.Abs(1 - Math.Abs(currentEnemyPlace.Rank - currentMove.TargetSquare.Rank));
                        currentSum += Math.Abs(1 - Math.Abs(currentEnemyPlace.File - currentMove.StartSquare.File)) + 1.5 * Math.Abs(1 - Math.Abs(currentEnemyPlace.Rank - currentMove.StartSquare.Rank));
                    }
                }

                if (moveSum < currentSum && (moveSum < score || runFromKing))
                {
                    runFromKing = false;
                    bestMove = currentMove;
                    score = moveSum;
                }
            }

            // If no forward move is possbile, find a way to remove one of the king's defenders or box in the king
            if (runFromKing)
            {
                // Avoids checks
                moveSum += 5 * Convert.ToInt32(board.IsInCheck());

                // Gives a score based on how close the piece currently is to the king and how far away it will be moving, giving bonus to moving pieces away from diagonals
                int startSquareFileScore = Math.Abs(currentMove.StartSquare.File - kingLocation.File);
                int startSquareRankScore = Math.Abs(currentMove.StartSquare.Rank - kingLocation.Rank);
                int startSquareScore = startSquareFileScore + startSquareRankScore - 2 * Convert.ToInt32(startSquareFileScore == startSquareRankScore);

                int targetSquareFileScore = Math.Abs(currentMove.TargetSquare.File - kingLocation.File);
                int targetSquareRankScore = Math.Abs(currentMove.TargetSquare.Rank - kingLocation.Rank);
                int targetSquareScore = targetSquareFileScore + targetSquareRankScore - Convert.ToInt32(targetSquareFileScore == targetSquareRankScore);

                moveSum += 4 * startSquareScore / targetSquareScore;

                // Lightly encourages hanging pieces
                moveSum -= 5 * pieceValue[(int)currentMove.MovePieceType] * Convert.ToInt32(board.SquareIsAttackedByOpponent(currentMove.TargetSquare));

                // Checks the score against the current best and updates the suggested move if it's considered more tragic
                if (moveSum < score)
                {
                    bestMove = currentMove;
                    score = moveSum;
                }
            }

            board.UndoMove(currentMove);
        }

        // 10-elo chess at its finest
        return bestMove;

    }
}