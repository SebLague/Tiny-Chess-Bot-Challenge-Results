namespace auto_Bot_308;

using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_308 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int counterRochade = 0;
    readonly string[,] opening = new string[,] { { "g1f3", "b1c3", "e2e4", "d2d4", "f1d3", "c1e3", "e1g1" }, { "g8f6", "b8c6", "d7d5", "e7e5", "f8d6", "c8e6", "e8g8" } };
    public Move Think(Board board, Timer timer)
    {
        //Filter all Moves, that do not end in draw
        Move[] allMoves = board.GetLegalMoves()
            .Where(move =>
            {
                board.MakeMove(move);
                var isNotDraw = !board.IsDraw();
                board.UndoMove(move);
                return isNotDraw;
            })
            .ToArray();

        if (allMoves.Length == 0) return board.GetLegalMoves().First();

        //All possible moves, that can capture.
        Move[] captureMoves = board.GetLegalMoves(true);

        //Order capture moves by value difference
        Move bestCapture = captureMoves
            //Only trade up
            .Where(move => TradeValue(board, move) > 0)
            //Order by best trade
            .OrderByDescending(move => TradeValue(board, move)
        //Use best trade
        ).FirstOrDefault();


        //Possible enemy counters
        board.MakeMove(bestCapture);
        Square[] enemyTargets = board.GetLegalMoves(true).Select(move => move.TargetSquare).ToArray();
        board.UndoMove(bestCapture);

        //Save queen if under attack
        if (board.TrySkipTurn())
        {
            var enemyQueenCaptureMoves = board.GetLegalMoves(true).Where(move => pieceValues[(int)move.CapturePieceType] == 900).ToArray();
            board.UndoSkipTurn();
            if (enemyQueenCaptureMoves.Length > 0)
            {
                var queenIsSaveMoves = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).Where(move => pieceValues[(int)move.MovePieceType] == 900).ToArray();
                foreach (Move move in queenIsSaveMoves)
                {
                    board.MakeMove(move);
                    if (captureMoves.Contains(move) && !board.SquareIsAttackedByOpponent(move.TargetSquare))
                    {
                        board.UndoMove(move);
                        return move;
                    }
                    board.UndoMove(move);
                }
                if (queenIsSaveMoves.Length > 0) return queenIsSaveMoves.FirstOrDefault();
            }
        }

        //Moves that have an unprotected start square
        //Compares if the moves are protected in a positive trade
        var unprotected = allMoves
            .Where(move => enemyTargets.Contains(move.StartSquare))
            .Where(move => !IsProtected(move, board)).OrderByDescending(move => pieceValues[(int)move.MovePieceType]).ToArray();

        //If the Capture Move can be done in a worthwile trade do it
        if (!bestCapture.IsNull && (IsWorthToTrade(bestCapture, unprotected.FirstOrDefault()) || !board.SquareIsAttackedByOpponent(bestCapture.TargetSquare)))
        {
            return bestCapture;
        }

        //Check if a move can additionally protect an unprotected square
        foreach (Move possibleMove in allMoves)
        {
            string newStartSquare = possibleMove.TargetSquare.Name;
            board.MakeMove(possibleMove);
            bool isSkipSuccess = board.TrySkipTurn();
            //Enemy Moves
            Move[] potentialEnemyMoves = board.GetLegalMoves();
            if (isSkipSuccess) board.UndoSkipTurn();

            var hasMove = potentialEnemyMoves
                .Where(move => move.StartSquare.Name == newStartSquare)
                .Where(move => unprotected.Select(u => u.StartSquare.Name).Contains(move.TargetSquare.Name))
                .Any();
            board.UndoMove(possibleMove);
            if (hasMove) return possibleMove;
        }

        var protectedTargetSquare = unprotected
            .Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).FirstOrDefault();

        if (!protectedTargetSquare.IsNull) return protectedTargetSquare;


        //Rochade, fixed, per Color
        if (counterRochade < opening.GetLength(1))
        {
            //Undo Move if it captured
            int color = board.IsWhiteToMove ? 0 : 1;
            string code = opening[color, counterRochade];
            Move moveRochade = allMoves.Where(move => move.StartSquare.Name == code.Substring(0, 2) && move.TargetSquare.Name == code.Substring(2, 2)).FirstOrDefault();
            counterRochade++;
            if (!moveRochade.IsNull) return moveRochade;
        }

        //Prefer moves that reduce enemy moves
        Move? mostReducingMove = null;
        Move? mostReducingMoveBackup = null;
        int enemyMoveCount = 100000;
        Board boardTemp = board;
        foreach (Move potentialMove in allMoves)
        {
            //If we can mate we do right away
            if (pieceValues[(int)potentialMove.CapturePieceType] == 10000) return potentialMove;

            board.MakeMove(potentialMove);
            boardTemp = board;
            Move[] enemyMoves = board.GetLegalMoves();
            board.UndoMove(potentialMove);
            if (enemyMoves.Length < enemyMoveCount && !board.SquareIsAttackedByOpponent(potentialMove.TargetSquare) && !boardTemp.IsDraw())
            {
                enemyMoveCount = enemyMoves.Length;
                if (potentialMove.MovePieceType == PieceType.King)
                    mostReducingMoveBackup = potentialMove;
                else
                    mostReducingMove = potentialMove;

            }
        }

        if (mostReducingMove.HasValue && !boardTemp.IsDraw()) return mostReducingMove.Value;
        if (mostReducingMoveBackup.HasValue && !boardTemp.IsDraw()) return mostReducingMoveBackup.Value;


        //Random Move if nothing else can be done
        Random rng = new();

        var saveMoves = allMoves.Where(move => !board.SquareIsAttackedByOpponent(move.TargetSquare)).ToArray();

        if (saveMoves.Length > 0 && !board.IsDraw())
        {
            return saveMoves[rng.Next(saveMoves.Length)];
        }

        var fallback = board.GetLegalMoves();
        return fallback[rng.Next(fallback.Length)];
    }

    /// <summary>
    /// Calculates the Trade Value for a move
    /// </summary>
    /// <param name="board">The board</param>
    /// <param name="move">The move to play</param>
    /// <returns>
    ///     The value difference between the capturing piece and the captured piece or just the value of the captured piece if no recapture is possible
    /// </returns>
    int TradeValue(Board board, Move move)
    {
        return pieceValues[(int)move.CapturePieceType] - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? pieceValues[(int)move.MovePieceType] : 0);
    }

    /// <summary>
    /// Checks if the start square of the move is protected
    /// </summary>
    /// <param name="moveWithTarget">The move</param>
    /// <param name="board">The board</param>
    /// <returns>True the move is covered</returns>
    bool IsProtected(Move moveWithTarget, Board board)
    {
        if (board.TrySkipTurn())
        {
            var enemyMove = board.GetLegalMoves(true).Where(move => move.TargetSquare == moveWithTarget.StartSquare).FirstOrDefault();
            if (!enemyMove.IsNull)
            {
                board.MakeMove(enemyMove);
                var hasCounterMoves = board.GetLegalMoves(true).Where(move => move.TargetSquare == moveWithTarget.StartSquare).Any();
                board.UndoMove(enemyMove);
                board.UndoSkipTurn();
                return hasCounterMoves;
            }
            board.UndoSkipTurn();
        }
        return false;
    }

    /// <summary>
    /// Checks if a Trade of pieces would be worth
    /// </summary>
    /// <param name="bestCapture">The Capture move</param>
    /// <param name="isAttacked">The Move with the own piece would be captured in return</param>
    /// <returns>True if the trade is worth</returns>
    bool IsWorthToTrade(Move bestCapture, Move isAttacked)
    {
        int valueBestCapture = pieceValues[(int)bestCapture.CapturePieceType];
        int valueBeschuetzen = pieceValues[(int)isAttacked.MovePieceType];
        return valueBestCapture >= valueBeschuetzen;
    }
}