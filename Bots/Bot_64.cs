namespace auto_Bot_64;
using ChessChallenge.API;
using System;

public class Bot_64 : IChessBot
{
    readonly int[] pieceValues = { 0, 1, 3, 3, 5, 9, 10 };
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        return GetBestMove(board, moves);
    }

    //returns move with best evaluation or specific move
    private Move GetBestMove(Board board, Move[] moves)
    {
        Move bestMove = moves[0];
        float bestEvaluation = -100;

        foreach (Move move in moves)
        {
            float currentEvaluation = GetMoveEvaluation(board, move);

            //playes checkmante
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
            //doesnt play move that allows checkmate
            if (MoveAllowsCheckmate(board, move))
            {
                currentEvaluation = -100;
            }
            //doesnt play move that allows draw at an advantadge
            if (MoveAllowsDraw(board, move) && GetTeamPieceScoreDifference(board) > 8)
            {
                currentEvaluation = -50;
            }
            //plays move that forces checkmate in two
            if (OddsOfCheckmateInTwo(board, move) == 1)
            {
                currentEvaluation = 200;
            }
            //plays move that draws at a disavantage
            if (MoveIsDraw(board, move) && GetTeamPieceScoreDifference(board) <= 0)
            {
                currentEvaluation = 100;
            }

            if (currentEvaluation > bestEvaluation)
            {
                bestMove = move;
                bestEvaluation = currentEvaluation;
            }
        }
        //Debug.WriteLine(bestEvaluation);

        return bestMove;
    }

    private float GetMoveEvaluation(Board board, Move move)
    {
        Random rnd = new Random();

        int thisPieceValue = GetPieceValue(move.MovePieceType);
        int capturedPieceValue = GetPieceValue(move.CapturePieceType);
        int promotionPieceValue = GetPieceValue(move.PromotionPieceType);

        float evaluation = 0;

        //prioritize pieces in danger
        evaluation += (board.SquareIsAttackedByOpponent(move.StartSquare) && !board.IsInCheck()) ? +thisPieceValue * 0.9f : 0;

        //halt pieces heading to danger
        evaluation += board.SquareIsAttackedByOpponent(move.TargetSquare) ? -thisPieceValue * 1f : 0;

        //reward protected squares
        evaluation += GetNPiecesProtectingSquareOnMove(board, move) * 0.02f;

        //reward check
        evaluation += MoveIsCheck(board, move) ? 0.03f : 0;

        //adds capured piece value
        evaluation += capturedPieceValue * 1;

        //adds promotion profit
        evaluation += promotionPieceValue * 4;

        //adds of checkmate in two
        evaluation += (float)Math.Pow(OddsOfCheckmateInTwo(board, move), 6) * 0.2f;

        if (thisPieceValue == 1 && move.TargetSquare.File > 0 && move.TargetSquare.File < 7)
        {
            evaluation += (board.IsWhiteToMove ? 0.7f / (7 - move.TargetSquare.File) : 0.7f / MathF.Abs(0 - move.TargetSquare.File));
        }

        evaluation += rnd.Next(1) * 0.01f;

        return evaluation;
    }

    private int GetNPiecesProtectingSquareOnMove(Board board, Move move)
    {
        Move[] myMoves = board.GetLegalMoves();
        int nPiecesProtectingSquare = 0;

        foreach (Move m in myMoves)
        {
            if (m == move) continue;
            if (m.TargetSquare == move.TargetSquare)
            {
                nPiecesProtectingSquare++;
            }
        }

        return nPiecesProtectingSquare;
    }

    private static bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    private bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    private bool MoveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }

    private bool MoveAllowsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);

        Move[] adversaryMoves = board.GetLegalMoves();

        foreach (Move ad in adversaryMoves)
        {
            board.MakeMove(ad);
            if (board.IsInCheckmate())
            {
                board.UndoMove(ad);
                board.UndoMove(move);
                return true;
            }
            board.UndoMove(ad);
        }

        board.UndoMove(move);

        return false;
    }

    private bool MoveAllowsDraw(Board board, Move move)
    {
        board.MakeMove(move);

        Move[] adversaryMoves = board.GetLegalMoves();

        foreach (Move ad in adversaryMoves)
        {
            board.MakeMove(ad);
            if (board.IsDraw())
            {
                board.UndoMove(ad);
                board.UndoMove(move);
                return true;
            }
            board.UndoMove(ad);
        }

        board.UndoMove(move);

        return false;
    }

    private int GetTeamPieceTotalScore(Board board, bool isWhite)
    {
        PieceList[] boardPieceList = board.GetAllPieceLists();
        int totalScore = 0;

        for (int i = 0; i < pieceValues.Length - 1; i++)
        {
            for (int j = 0; j < boardPieceList[i].Count; j++)
            {
                Piece currentPiece = boardPieceList[i][j];
                if (currentPiece.IsWhite == isWhite)
                {
                    totalScore += GetPieceValue(currentPiece.PieceType);
                }
            }
        }

        return totalScore;
    }

    //score difference, smaller is worse, < 0 is disavantage
    private int GetTeamPieceScoreDifference(Board board)
    {
        int ourTeamScore = GetTeamPieceTotalScore(board, board.IsWhiteToMove);
        int opponentTeamScore = GetTeamPieceTotalScore(board, !board.IsWhiteToMove);

        return ourTeamScore - opponentTeamScore;
    }

    //odds of move forcing a checkmate in two, 0-1
    private float OddsOfCheckmateInTwo(Board board, Move move)
    {
        board.MakeMove(move);

        //get possible responses
        Move[] adversaryMoves = board.GetLegalMoves();

        //array of possible chekmate on opponent move
        bool[] adversaryMovesAllowCheckmate = new bool[adversaryMoves.Length];

        //loop through opponent moves
        for (int i = 0; i < adversaryMoves.Length; i++)
        {
            //do move
            board.MakeMove(adversaryMoves[i]);
            //get our possible responses
            Move[] ourNextMoves = board.GetLegalMoves();

            for (int j = 0; j < ourNextMoves.Length; j++)
            {
                //allows a checkmate
                if (MoveIsCheckmate(board, ourNextMoves[j]))
                {
                    adversaryMovesAllowCheckmate[i] = true;
                    break;
                }

            }
            board.UndoMove(adversaryMoves[i]);
        }

        board.UndoMove(move);

        int nMovesAllowingCheckmate = 0;

        //check if checkmate is certain
        foreach (bool moveAllows in adversaryMovesAllowCheckmate)
        {
            if (moveAllows) nMovesAllowingCheckmate++;
        }

        if (adversaryMoves.Length == 0) return 1;
        return nMovesAllowingCheckmate / adversaryMoves.Length;
    }

    private int GetPieceValue(PieceType pieceType)
    {
        return pieceValues[(int)pieceType];
    }
}