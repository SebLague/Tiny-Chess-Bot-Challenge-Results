namespace auto_Bot_613;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_613 : IChessBot
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 10, 10000 };
    List<Move> testMoves = new List<Move>();

    int depth = 2;
    int minimumTradeOutcomeAcceptance = 0;

    bool isPlayingWhite;

    public Move Think(Board board, Timer timer)
    {
        isPlayingWhite = board.IsWhiteToMove;

        if (GetCurrentScore(board, isPlayingWhite) >= 0)
        {
            minimumTradeOutcomeAcceptance = 0;
        }
        else minimumTradeOutcomeAcceptance = 1;


        Move[] moves = board.GetLegalMoves();

        List<Move> validMoves = CalculatetMoves(board, moves);

        List<Move> validMovesAfterMinmax = new List<Move>();
        int highestMinMaxPoints = -999999;

        foreach (Move move in validMoves)
        {
            int minMaxScore = MinMax(board, move, depth);
            if (highestMinMaxPoints < minMaxScore)
            {
                validMovesAfterMinmax.Clear();
                validMovesAfterMinmax.Add(move);
                highestMinMaxPoints = minMaxScore;
            }
            else if (highestMinMaxPoints == minMaxScore)
            {
                validMovesAfterMinmax.Add(move);
            }
        }

        Move bestMove = validMoves[0];
        int currentHighestBestMove = -100000;

        foreach (Move move in validMovesAfterMinmax)
        {
            MakeMove(board, move);
            if (board.IsInCheckmate())
                return move;
            else if (board.IsInStalemate())
            {
                DeleteAllTestMoves(board);
                continue;
            }

            int numberOfMovesAvailable = 0;

            //Prioritize Checks
            if (board.IsInCheck() && !board.IsDraw()) numberOfMovesAvailable += 100;

            board.ForceSkipTurn();

            numberOfMovesAvailable += board.GetLegalMoves().Length;

            //prioritize castle
            if (move.IsCastles) numberOfMovesAvailable += 100;
            else if (move.MovePieceType == PieceType.King && !move.IsCapture) numberOfMovesAvailable -= 100;

            //avoir repetition
            if ((board.GameMoveHistory.Length > 5 && board.GameMoveHistory[board.GameMoveHistory.Count() - 2].StartSquare == move.TargetSquare &&
                board.GameMoveHistory[board.GameMoveHistory.Count() - 2].TargetSquare == move.StartSquare) ||
                board.IsDraw())
                numberOfMovesAvailable -= 150;

            //prioritize pawn
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) numberOfMovesAvailable += 200;
            if (move.MovePieceType == PieceType.Pawn) numberOfMovesAvailable += 3;

            if (move.IsCapture)
            {
                numberOfMovesAvailable -= pieceValues[(int)move.MovePieceType] * 100;
            }

            if (numberOfMovesAvailable > currentHighestBestMove)
            {
                currentHighestBestMove = numberOfMovesAvailable;
                bestMove = move;
            }

            board.UndoSkipTurn();
            DeleteAllTestMoves(board);
        }

        validMoves.Clear();
        return bestMove;
    }

    int MinMax(Board board, Move move, int currentDepth)
    {
        int lowestValue = 999999;

        board.MakeMove(move);

        if (board.IsInCheckmate())
        {
            if (isPlayingWhite == board.IsWhiteToMove)
            {
                board.UndoMove(move);
                return -10000;
            }
            else
            {
                board.UndoMove(move);
                return 10000;
            }
        }
        else if (board.IsDraw())
        {
            board.UndoMove(move);
            return -10000;
        }

        if (currentDepth >= 0)
        {
            List<Move> newValidMoves;
            if (board.IsWhiteToMove == isPlayingWhite)
            {
                newValidMoves = CalculatetMoves(board, board.GetLegalMoves());
            }
            else
            {
                newValidMoves = board.GetLegalMoves().ToList();
            }

            for (int i = 0; i < newValidMoves.Count; i++)
            {
                int value = MinMax(board, newValidMoves[i], currentDepth - 1);

                if (value < lowestValue)
                {
                    lowestValue = value;
                }
            }
        }
        else
        {
            lowestValue = GetCurrentScore(board, isPlayingWhite);
        }

        board.UndoMove(move);

        return lowestValue;
    }

    int GetCurrentScore(Board board, bool checkForWhite)
    {
        int blackScore = 0;
        int whiteScore = 0;

        PieceList[] pieces = board.GetAllPieceLists();

        for (int i = 0; i < 6; i++)
        {
            whiteScore += pieces[i].Count * pieceValues[i + 1];
        }
        for (int i = 6; i < pieces.Count(); i++)
        {
            blackScore += pieces[i].Count * pieceValues[i % 6 + 1];
        }

        if (checkForWhite)
        {
            return whiteScore - blackScore;
        }
        return blackScore - whiteScore;
    }

    List<Move> CalculatetMoves(Board board, Move[] moves)
    {
        List<Move> validMoves = new List<Move>();
        int highestMovePoint = -1000000;

        foreach (Move move in moves)
        {
            if (IsMateInOne(board, move))
            {
                validMoves.Clear();
                validMoves.Add(move);
                DeleteAllTestMoves(board);
                return validMoves;
            }

            int moveOutcome = MoveOutcome(board, move);

            if (moveOutcome >= highestMovePoint)
            {
                if (moveOutcome > highestMovePoint)
                {
                    highestMovePoint = moveOutcome;
                    validMoves.Clear();
                    validMoves.Add(move);
                }
                else
                {
                    validMoves.Add(move);
                }
            }
        }

        DeleteAllTestMoves(board);
        return validMoves;
    }
    int MoveOutcome(Board board, Move move)
    {
        int tradeOutcome = IsWinningTrade(board, move, pieceValues[(int)move.CapturePieceType]);
        if (tradeOutcome > minimumTradeOutcomeAcceptance)
        {
            tradeOutcome = pieceValues[(int)move.CapturePieceType];
        }
        else if (tradeOutcome >= 500)
        {
            tradeOutcome = 99999;
        }
        DeleteAllTestMoves(board);

        int highestEnemyTradeOutcome = 0;
        board.MakeMove(move);
        foreach (Move enemyMove in board.GetLegalMoves(true))
        {
            if (enemyMove.TargetSquare != move.TargetSquare)
            {
                int enemyTradeOutcome = IsWinningTrade(board, enemyMove, pieceValues[(int)enemyMove.CapturePieceType]);
                DeleteAllTestMoves(board);

                if (enemyTradeOutcome > highestEnemyTradeOutcome)
                {
                    highestEnemyTradeOutcome = enemyTradeOutcome;
                }
            }
        }
        board.UndoMove(move);

        return tradeOutcome - highestEnemyTradeOutcome;
    }

    int IsWinningTrade(Board board, Move move, int points)
    {
        MakeMove(board, move);

        //On check si le move enemi peut prendre avec la boucle
        foreach (Move enemyMove in board.GetLegalMoves(true))
        {
            if (enemyMove.TargetSquare == move.TargetSquare)
            {
                //Si c'est le cas on check si on peut reprendre derrière et on enlève les points a move
                points -= pieceValues[(int)enemyMove.CapturePieceType];
                MakeMove(board, enemyMove);

                foreach (Move myNewMoves in board.GetLegalMoves(true))
                {
                    //Si on peut reprendre, alors on retourne la recursion
                    if (myNewMoves.TargetSquare == enemyMove.TargetSquare)
                    {
                        points += pieceValues[(int)myNewMoves.CapturePieceType];
                        return IsWinningTrade(board, myNewMoves, points);
                    }
                }

                //Si on peut pas reprendre derrière alors on retourne directement les points
                return points;
            }
        }
        //Si aucun ennemi ne peut prendre, alors on retourne les points
        return points;
    }

    void MakeMove(Board board, Move move)
    {
        board.MakeMove(move);

        testMoves.Add(move);

    }

    void DeleteAllTestMoves(Board board)
    {
        for (int i = testMoves.Count - 1; i >= 0; i--)
            board.UndoMove(testMoves[i]);

        testMoves.Clear();
    }

    bool IsMateInOne(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}