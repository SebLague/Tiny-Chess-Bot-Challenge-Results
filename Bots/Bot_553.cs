namespace auto_Bot_553;
using ChessChallenge.API;
using System;

public class Bot_553 : IChessBot
{
    public Board board;
    Move bestMove;
    public Move Think(Board currentBoard, Timer timer)
    {
        board = currentBoard;
        bestMove = board.GetLegalMoves()[0];
        search(4, 0, -9999, 9999, 0);
        return bestMove;


    }
    int search(int depth, int plyFromRoot, int alpha, int beta, int numExtentions)
    {
        if (depth == 0)
        {
            return evaluateBoard();

        }
        Move[] moves = board.GetLegalMoves();
        orderMoves(moves);
        if (moves.Length == 0)
        {
            if (board.IsInCheck())
            {
                return -9999;
            }
            return 0;
        }
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int ext = numExtentions < 6 && board.IsInCheck() ? 1 : 0;
            int eval = -search(depth - 1 + ext, plyFromRoot + 1, -beta, -alpha, numExtentions + ext);

            board.UndoMove(move);
            if (eval >= beta)
            {
                if (plyFromRoot > 0)
                    return beta;
            }
            if (eval > alpha)
            {
                alpha = eval;
                if (plyFromRoot == 0)
                {
                    bestMove = move;
                }
            }
        }
        return alpha;
    }
    static double EndgamePhaseWeight(int materialCountWithoutPawns)
    {
        const double multiplier = 0.0625;
        return 1 - Math.Min(1, materialCountWithoutPawns * multiplier);
    }


    void orderMoves(Move[] moves)
    {

        int[] moveScores = new int[moves.Length];
        for (int i = 0; i < moves.Length - 1; i++)
        {

            int moveScore = 0;
            PieceType movePieceType = moves[i].MovePieceType;
            PieceType capturePieceType = moves[i].CapturePieceType;

            if (capturePieceType != PieceType.None)
            {
                moveScore = getPieceValue(capturePieceType) - getPieceValue(movePieceType);
            }
            if (moves[i].IsPromotion)
            {
                moveScore += getPieceValue(moves[i].PromotionPieceType);
            }
            if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
            {
                moveScore -= getPieceValue(movePieceType);
            }
            if (moves[i].IsCapture == true)
            {
                moveScore += getPieceValue(capturePieceType);
            }
            if (moves[i].IsCastles)
            {
                moveScore += 300;
            }
            if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
            {
                moveScore -= getPieceValue(movePieceType);
            }
            if (movePieceType == PieceType.Pawn || movePieceType == PieceType.Knight || movePieceType == PieceType.Bishop)
                moveScore -= (Math.Max(3 - moves[i].TargetSquare.Rank, moves[i].TargetSquare.Rank - 4) + Math.Max(3 - moves[i].TargetSquare.File, moves[i].TargetSquare.File - 4));



            board.MakeMove(moves[i]);

            if (board.IsInCheckmate())
            {
                moveScore += 999999;
            }
            if (board.IsInCheck())
            {
                moveScore += 100;
            }

            if (board.IsInStalemate())
            {
                moveScore -= 9999;
            }
            if (board.IsFiftyMoveDraw())
            {
                moveScore -= 9999;
            }
            if (board.IsInsufficientMaterial())
            {
                moveScore -= 9999;
            }
            if (board.IsRepeatedPosition())
            {
                moveScore -= 9999;
            }
            board.UndoMove(moves[i]);
            moveScores[i] = moveScore;

        }

        Sort(moves, moveScores);
    }

    int getPieceValue(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.Pawn:
                return pawnValue;
            case PieceType.Knight:
                return knightValue;
            case PieceType.Bishop:
                return bishopValue;
            case PieceType.Rook:
                return rookValue;
            default:
                return queenValue;
        }



    }
    const int pawnValue = 100;
    const int knightValue = 300;
    const int bishopValue = 330;
    const int rookValue = 500;
    const int queenValue = 900;
    public int evaluateBoard()
    {
        int whiteEval = countPieces(true, false) + ForceKingToCornerEndgameEval(board.GetKingSquare(true), board.GetKingSquare(false), countPieces(true, false), countPieces(false, false), EndgamePhaseWeight(countPieces(false, false) - board.GetPieceList(PieceType.Pawn, false).Count));
        int blackEval = countPieces(false, false) + ForceKingToCornerEndgameEval(board.GetKingSquare(false), board.GetKingSquare(true), countPieces(false, false), countPieces(true, false), EndgamePhaseWeight(countPieces(true, false) - board.GetPieceList(PieceType.Pawn, true).Count));
        int evaluation = (whiteEval - blackEval) * (board.IsWhiteToMove ? 1 : -1);
        if (board.IsRepeatedPosition() || board.IsInStalemate() || board.IsInCheckmate())
        {
            evaluation = -evaluation;
        }
        return evaluation;
    }
    int ForceKingToCornerEndgameEval(Square friendlyKingSquare, Square opponentKingSquare, int myMaterial, int opponentMaterial, double endGameWeight)
    {
        int eval = 0;
        int opponentKingRank = opponentKingSquare.Rank;
        int opponentKingFile = opponentKingSquare.File;
        int opponentKingDstFromCenterFile = Math.Max(3 - opponentKingFile, opponentKingFile - 4);
        int opponentKingDstFromCenterRank = Math.Max(3 - opponentKingRank, opponentKingRank - 4);
        int opponentKingDstFromCenter = opponentKingDstFromCenterFile + opponentKingDstFromCenterRank;
        eval += opponentKingDstFromCenter * 10;
        int friendlyKingRank = friendlyKingSquare.Rank;
        int friendlyKingFIle = friendlyKingSquare.File;
        int distBtwnKingsFile = Math.Abs(friendlyKingFIle - opponentKingFile);
        int distBtwnKingsRank = Math.Abs(friendlyKingRank - opponentKingRank);
        int dist = distBtwnKingsFile + distBtwnKingsRank;
        eval += (14 - dist) * 4;

        return (int)(eval * endGameWeight);
    }
    int countPieces(bool isWhite, bool withoutPawns)
    {
        int material = 0;
        if (withoutPawns == false)
        {
            material += board.GetPieceList(PieceType.Pawn, isWhite).Count * pawnValue;
        }
        material += board.GetPieceList(PieceType.Knight, isWhite).Count * knightValue;
        material += board.GetPieceList(PieceType.Bishop, isWhite).Count * bishopValue;
        material += board.GetPieceList(PieceType.Rook, isWhite).Count * rookValue;
        material += board.GetPieceList(PieceType.Queen, isWhite).Count * queenValue;

        return material;
    }

    void Sort(Move[] moves, int[] moveScores)
    {
        for (int i = 0; i < moves.Length - 1; i++)
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
    }
}