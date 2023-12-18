namespace auto_Bot_414;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_414 : IChessBot
{
    // Global variables
    int[] valueArray = { 100, 300, 320, 500, 900, 0, -100, -300, -320, -500, -900, 0 };

    int DEPTH = 5;

    Move bestMove;

    public Move Think(Board board, Timer timer)
    {
        bestMove = Move.NullMove;
        Search(board, DEPTH, int.MinValue, int.MaxValue, board.IsWhiteToMove);



        if (bestMove == Move.NullMove || !board.GetLegalMoves().Contains(bestMove))
        {
            return board.GetLegalMoves()[0];
        }
        return bestMove;

    }

    int Search(Board board, int depth, int alpha, int beta, bool maximizingPlayer) //Based on the pseudocode of "https://en.wikipedia.org/wiki/Alpha-beta_pruning"
    {
        int bestScore = int.MinValue * (maximizingPlayer ? 1 : -1);
        //Variables
        int value;
        Move[] moves = OrderMoves(board, board.GetLegalMoves(), maximizingPlayer);
        //If on the end of node, return the evaluation

        if (board.IsDraw())
        {
            return 0;
        }

        if (depth == 0)
        {
            return Evaluate(board, maximizingPlayer);
        }



        if (maximizingPlayer)
        {
            value = int.MinValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                if (board.IsInCheckmate() || board.IsFiftyMoveDraw() || board.IsInStalemate())
                {
                    board.UndoMove(move);
                    return int.MaxValue;
                }

                value = Math.Max(value, Search(board, depth - 1, alpha, beta, false));

                board.UndoMove(move);

                if (value >= beta)
                {
                    // Beta cutoff
                    break;
                }

                if (value > alpha)
                {
                    if (depth == DEPTH)
                    {
                        bestMove = move;
                    }
                    alpha = value;
                }

            }
        }
        else
        {
            value = int.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    board.UndoMove(move);
                    return int.MinValue;
                }


                value = Math.Min(value, Search(board, depth - 1, alpha, beta, true));

                board.UndoMove(move);

                if (value <= alpha)
                {
                    // Alpha cutoff
                    break;
                }

                if (value < beta)
                {
                    if (depth == DEPTH)
                    {
                        bestMove = move;
                    }
                    beta = value;

                }
            }
        }
        return value;
    }
    int Evaluate(Board board, bool perspective) //Simple eval function
    {

        PieceList[] pieces = board.GetAllPieceLists();
        int sum = 0;

        //Adds up all the pieces value (negative for black)
        for (int i = 0; i < 12; i++)
        {
            sum += pieces[i].Count * valueArray[i];
        }

        //Keeps the bot from walking his king
        if (board.GetKingSquare(perspective).Rank != (perspective ? 1 : 8) && (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) / 32) > 0.75)
        {
            sum += perspective ? 50 : -50;
        }



        if (board.HasKingsideCastleRight(perspective) || board.HasQueensideCastleRight(perspective))
        {
            sum += perspective ? 10 : -10;
        }

        sum += (perspective ? 1 : -1) * ForceKingToCorners(board.GetKingSquare(perspective), board.GetKingSquare(!perspective), Math.Abs((BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) / 32) - 1));

        return sum;
    }

    int ForceKingToCorners(Square friendlyKingSquare, Square opponentKingSquare, float endgameWeight)
    {
        int evaluation = 0;

        int opponentKingRank = opponentKingSquare.Rank;
        int opponentKingFile = opponentKingSquare.File;

        int opponentKingDstFromCentre = Math.Max(3 - opponentKingRank, opponentKingRank - 4) + Math.Max(3 - opponentKingFile, opponentKingFile - 4);

        evaluation += opponentKingDstFromCentre * 2;

        int friendlyKingRank = friendlyKingSquare.Rank;
        int friendlyKingFile = friendlyKingSquare.File;

        evaluation += 14 - Math.Abs(friendlyKingFile - opponentKingFile) + Math.Abs(friendlyKingRank - opponentKingRank);

        return (int)(evaluation * 10 * endgameWeight);
    }

    // Orders the Moves and it's a damn mess, but it works
    Move[] OrderMoves(Board board, Move[] moves, bool White)
    {
        List<int> _moveScores = new List<int>();

        Dictionary<Move, int> moveDict = new Dictionary<Move, int>(218);

        // Get the scores of each move.
        foreach (Move move in moves)
        {
            int moveScoreGuess = 0;
            PieceType movePieceType = board.GetPiece(move.StartSquare).PieceType;
            PieceType capturePieceType = board.GetPiece(move.TargetSquare).PieceType;

            if (capturePieceType != PieceType.None)
            {
                moveScoreGuess = 10 * GetPieceValue(capturePieceType) - GetPieceValue(movePieceType);
            }
            if (move.IsPromotion)
            {
                moveScoreGuess += GetPieceValue(move.PromotionPieceType);
            }

            if (BitboardHelper.SquareIsSet(board.GetPieceBitboard(PieceType.Pawn, White), move.TargetSquare))
            {
                moveScoreGuess -= GetPieceValue(movePieceType);
            }
            _moveScores.Add(moveScoreGuess);
        }
        int[] moveScores = _moveScores.ToArray();


        for (int i = 0; i < moves.Count() - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
        return moves;
    }

    // Returns the value of a PieceType
    int GetPieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 300;
            case PieceType.Bishop:
                return 320;
            case PieceType.Rook:
                return 500;
            case PieceType.King:
                return 900;
        }
        return 0; //Not all code paths return a value error otherwise
    }

}