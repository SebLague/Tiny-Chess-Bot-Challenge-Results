namespace auto_Bot_385;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_385 : IChessBot
{
    public Move bestMove;

    public int bestEvaluation = -9999999;
    public bool isWhite;

    public Move Move1;
    public Move Move2;
    public Move Move3;

    public bool pawnDeveloped = false;

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();
        bool isWhite = (board.IsWhiteToMove) ? true : false;
        bestMove = allMoves[0];

        foreach (Move move in allMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }
        }

        if (board.IsInCheckmate() == false)
        {
            findMove(board);
        }
        bestEvaluation = -9999999;

        return bestMove;
    }

    static int materialEvaluation(Board board)
    {
        int whiteMaterial = 0;
        int blackMaterial = 0;

        whiteMaterial += 1 * board.GetPieceList(PieceType.Pawn, true).Count();
        whiteMaterial += 3 * board.GetPieceList(PieceType.Knight, true).Count();
        whiteMaterial += 3 * board.GetPieceList(PieceType.Bishop, true).Count();
        whiteMaterial += 5 * board.GetPieceList(PieceType.Rook, true).Count();
        whiteMaterial += 9 * board.GetPieceList(PieceType.Queen, true).Count();

        blackMaterial += 1 * board.GetPieceList(PieceType.Pawn, false).Count();
        blackMaterial += 3 * board.GetPieceList(PieceType.Knight, false).Count();
        blackMaterial += 3 * board.GetPieceList(PieceType.Bishop, false).Count();
        blackMaterial += 5 * board.GetPieceList(PieceType.Rook, false).Count();
        blackMaterial += 9 * board.GetPieceList(PieceType.Queen, false).Count();

        int netMaterial = whiteMaterial - blackMaterial;
        int perspective = (board.IsWhiteToMove) ? 1 : -1;

        return netMaterial * perspective;
    }

    static int kingEndgame(Board board, bool isWhite)
    {
        Square opponentKingSquare = board.GetKingSquare(!isWhite);
        int opponentKingRank = opponentKingSquare.Rank;
        int opponentKingFile = opponentKingSquare.File;

        Square friendlyKingSquare = board.GetKingSquare(isWhite);
        int friendlyKingRank = friendlyKingSquare.Rank;
        int friendlyKingFile = friendlyKingSquare.File;

        int kingFileDisCenter = Math.Abs(4 - opponentKingFile);
        int kingRankDistCenter = Math.Abs(4 - opponentKingRank);

        int kingDistFile = Math.Abs(friendlyKingFile - opponentKingFile);
        int kingDistRank = Math.Abs(friendlyKingRank - opponentKingRank);
        int kingDistance = 14 - kingDistFile - kingDistRank;
        int weight = 1;

        return (kingFileDisCenter + kingRankDistCenter + kingDistance) * weight;
    }

    public int opening(Board board, Move move, bool isWhite, bool isPawnDeveloped)
    {
        int evaluation = 0;

        Piece currentPiece = board.GetPiece(move.TargetSquare);

        int currentMoveRank = move.TargetSquare.Rank;
        int currentMoveFile = move.TargetSquare.File;

        if ((currentMoveRank == 3 || currentMoveRank == 4) & (currentMoveFile == 3 || currentMoveFile == 4) & currentPiece.IsPawn == true & pawnDeveloped == false)
        {
            evaluation += 10;
            pawnDeveloped = true;
        }
        if ((currentMoveFile == 2 || currentMoveFile == 5) & currentPiece.IsKnight == true & pawnDeveloped == true)
        {
            evaluation += 10;
        }


        int weight = 1;

        return evaluation * weight;
    }

    public void findMove(Board board)
    {
        Move[] allMoves1 = board.GetLegalMoves();

        foreach (Move move1 in allMoves1)
        {
            board.MakeMove(move1);
            Move[] allMoves2 = board.GetLegalMoves();
            int worstEvaluation = 9999999;

            if (board.IsInCheckmate() == true)
            {
                return;
            }

            int currentEval = 0;

            if (board.GameMoveHistory.Count() <= 5)
            {
                currentEval += opening(board, move1, isWhite, pawnDeveloped);
            }

            Move opponentsBestMove = Move2;

            foreach (Move move2 in allMoves2)
            {
                board.MakeMove(move2);
                int opponentsEval = materialEvaluation(board);

                if (opponentsEval < worstEvaluation)
                {
                    worstEvaluation = opponentsEval;
                    opponentsBestMove = move2;
                }

                board.UndoMove(move2);
            }

            board.MakeMove(opponentsBestMove);
            Move[] allMoves3 = board.GetLegalMoves();

            foreach (Move move3 in allMoves3)
            {
                board.MakeMove(move3);
                currentEval -= materialEvaluation(board);

                int opponentPieces = board.GetPieceList(PieceType.Pawn, !isWhite).Count;
                opponentPieces += 3 * board.GetPieceList(PieceType.Knight, !isWhite).Count;
                opponentPieces += 3 * board.GetPieceList(PieceType.Bishop, !isWhite).Count;
                opponentPieces += 5 * board.GetPieceList(PieceType.Rook, !isWhite).Count;
                opponentPieces += 9 * board.GetPieceList(PieceType.Queen, !isWhite).Count;

                if (opponentPieces <= 3)
                {
                    currentEval += (3 - opponentPieces) / 3 * kingEndgame(board, isWhite);
                }

                if (currentEval > bestEvaluation)
                {
                    bestEvaluation = currentEval;
                    bestMove = move1;
                }

                board.UndoMove(move3);
            }

            board.UndoMove(opponentsBestMove);
            board.UndoMove(move1);
        }
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}