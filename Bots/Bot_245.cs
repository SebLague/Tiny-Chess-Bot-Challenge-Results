namespace auto_Bot_245;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_245 : IChessBot
{
    List<char> letters = new List<char>() { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h' };
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 50, 200, 500, 500, 1500, 2000 };
    bool checkMate = false;
    int possibleCheckmateScore = 5000;
    float possibleCaptureMultiplier = 0.5f;
    float possibleDeathMultiplier = 0.8f;
    Move bestMove;
    int bestScore;
    int remainingPieces;
    class Action
    {
        public Move move;
        public Action prevAction;
        public List<Action> nextMoves = new List<Action>();
        public int score;
        public bool nextMovesSet = false;

        public void AddMove(Action newMove)
        {
            nextMoves.Add(newMove);
        }

        public Action(Move m, int s)
        {
            move = m;
            score = s;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        bestScore = -10000;
        remainingPieces = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            remainingPieces += list.Count;
        }
        Action actions = new Action(Move.NullMove, 0);
        Move[] allMoves = board.GetLegalMoves();
        Move[] captureMoves = board.GetLegalMoves(true);
        bestMove = captureMoves.Length == 0 ? allMoves[0] : captureMoves[0];
        while (timer.MillisecondsElapsedThisTurn < 10 && !checkMate)
        {
            CheckMoves(actions, board);
        }
        foreach (Action action in actions.nextMoves)
        {
            if (action.score > bestScore)
            {
                bestScore = action.score;
                bestMove = action.move;
            }
        }
        return bestMove;
    }

    void CheckMoves(Action action, Board board, bool first = false)
    {
        if (action.nextMovesSet)
        {
            return;
        }
        List<Move> moves = new List<Move>();
        Action tempAction = action;
        while (tempAction.prevAction != null)
        {
            tempAction = tempAction.prevAction;
            moves.Add(tempAction.move);
        }
        for (int i = moves.Count - 1; i >= 0; i--)
        {
            board.MakeMove(moves[i]);
        }

        Move[] legalMoves = board.GetLegalMoves();
        foreach (Move move in legalMoves)
        {
            if (first && isCheckmate(board, move))
            {
                bestMove = move;
                checkMate = true;
                break;
            }
            else
            {
                int tempScore = 0;
                Piece myPiece = board.GetPiece(move.StartSquare);
                switch (myPiece.PieceType)
                {
                    case PieceType.Pawn:
                        tempScore += myPiece.IsWhite ? move.TargetSquare.Rank : 9 - move.TargetSquare.Rank;
                        break;
                    case PieceType.Knight:
                        tempScore += ((int)distToEnemies(board, move.StartSquare.Name) - (int)distToEnemies(board, move.TargetSquare.Name)) / 2;
                        break;
                    case PieceType.King:
                        tempScore += (16 - remainingPieces) / 8 * (int)distToEnemies(board, move.StartSquare.Name) - (int)distToEnemies(board, move.TargetSquare.Name);
                        break;
                }
                Piece targetPiece = board.GetPiece(move.TargetSquare);
                tempScore += pieceValues[(int)targetPiece.PieceType];
                board.MakeMove(move);
                foreach (Move captureMove in board.GetLegalMoves(true))
                {
                    targetPiece = board.GetPiece(captureMove.TargetSquare);
                    tempScore -= (int)(pieceValues[(int)targetPiece.PieceType] * possibleDeathMultiplier);
                }
                Move counterMove = CanKill(board, move.TargetSquare);
                if (counterMove != Move.NullMove)
                {
                    int myMove = -1;
                    List<Move> counterMoves = new List<Move>();
                    PieceType targetPieceType = move.MovePieceType;
                    while (counterMove != Move.NullMove)
                    {
                        counterMoves.Add(counterMove);
                        tempScore += myMove * (int)(pieceValues[(int)targetPieceType] * possibleCaptureMultiplier);
                        myMove *= -1;
                        targetPieceType = counterMove.MovePieceType;
                        board.MakeMove(counterMove);
                        counterMove = CanKill(board, move.TargetSquare);
                    }
                    for (int i = counterMoves.Count - 1; i >= 0; i--)
                    {
                        board.UndoMove(counterMoves[i]);
                    }
                }
                else
                {
                    board.ForceSkipTurn();
                    foreach (Move captureMove in board.GetLegalMoves(true))
                    {
                        if (isCheckmate(board, captureMove))
                        {
                            tempScore += possibleCheckmateScore;
                        }
                        targetPiece = board.GetPiece(captureMove.TargetSquare);
                        tempScore += (int)(pieceValues[(int)targetPiece.PieceType] * possibleCaptureMultiplier);
                    }
                    board.UndoSkipTurn();
                }
                board.UndoMove(move);
                Action newAction = new Action(move, tempScore);
                newAction.prevAction = action;
                action.AddMove(newAction);
            }
        }
        for (int i = 0; i < moves.Count; i++)
        {
            board.UndoMove(moves[i]);
        }
        moves.Clear();
        action.nextMovesSet = true;
    }

    bool isCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheckmate = board.IsInCheckmate();
        board.UndoMove(move);
        return isCheckmate;
    }

    float distToEnemies(Board board, string pos) //  Finds the sum of the distance of the end position of this move to all of the pieces of player who's turn it is.
    {
        float dist = 0;
        PieceList[] pieceLists = board.GetAllPieceLists();
        foreach (PieceList pieces in pieceLists)
        {
            if (pieces.IsWhitePieceList != board.IsWhiteToMove)
            {
                foreach (Piece piece in pieces)
                {
                    dist += distanceBetweenPositions(pos, piece.Square.Name);
                }
            }
        }
        return dist;
    }

    float distanceBetweenPositions(string pos1, string pos2)
    {
        return MathF.Sqrt(MathF.Pow(pos1[1] - pos2[1], 2) + MathF.Pow(letters.IndexOf(pos1[0]) - letters.IndexOf(pos2[0]), 2));
    }

    Move CanKill(Board board, Square square)
    {
        Move currentBestMove = Move.NullMove;
        int currentBestScore = 0;

        foreach (Move move in board.GetLegalMoves(true))
        {
            int tempScore = 0;
            if (move.TargetSquare == square)
            {
                tempScore = 5000 - pieceValues[(int)move.MovePieceType];
                if (tempScore > currentBestScore)
                {
                    currentBestMove = move;
                    currentBestScore = tempScore;
                }
            }
        }

        return currentBestMove;
    }
}
