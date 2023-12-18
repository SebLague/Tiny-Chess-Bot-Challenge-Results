namespace auto_Bot_297;
using ChessChallenge.API;
using System;

public class Bot_297 : IChessBot
{
    private Move fMove;
    private Random rdm = new Random();
    bool isWhiteTeam;


    public Move Think(Board board, Timer timer)
    {
        isWhiteTeam = board.IsWhiteToMove;

        Move[] moves = board.GetLegalMoves();
        fMove = moves[rdm.Next(0, moves.Length)];
        double points = 0;
        foreach (Move move in moves)
        {

            // Don't do this move if it make a draw
            board.MakeMove(move);
            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }
            board.UndoMove(move);

            // Unit Move For Don't Be Attack
            if (board.SquareIsAttackedByOpponent(move.StartSquare) && !board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                // Calcul Point to save unit
                double localPoint = (double)board.GetPiece(move.StartSquare).PieceType * 2 - distanceBetween(move.StartSquare, move.TargetSquare) / 10;
                //double ActuPoints = (double) board.GetPiece(move.StartSquare).PieceType;

                localPoint += posPointCalc(board, move);

                if (points <= localPoint)
                {

                    points = localPoint;
                    fMove = move;
                }
            }
            // The Move don't let unit attacked
            if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                // Verif if can checkmate
                board.MakeMove(move);
                if (board.IsInCheckmate())
                {
                    points = 10;
                    fMove = move;
                }
                board.UndoMove(move);

                double localPoint = (double)board.GetPiece(move.TargetSquare).PieceType;

                localPoint += posPointCalc(board, move);

                // kill the best enemie unit
                if (points <= localPoint)
                {
                    // If go more close to enemie king, it's better
                    localPoint += goCloseKing(board, move);

                    points = localPoint;
                    fMove = move;
                }
            }
            else if (board.GetPiece(move.TargetSquare).PieceType >= board.GetPiece(move.StartSquare).PieceType)
            {
                // Move that kill and surely suicide unit 
                double localPoint = (double)(board.GetPiece(move.TargetSquare).PieceType - board.GetPiece(move.StartSquare).PieceType) + 1;
                if (points <= localPoint)
                {
                    // If go more close to enemie king, it's better
                    localPoint += goCloseKing(board, move);

                    points = localPoint;
                    fMove = move;
                }
            }
        }
        return fMove;
    }

    private double posPointCalc(Board board, Move move)
    {
        double localPoint = 0;
        // Minus if another unit can be attack
        int totPoints1 = 0;
        PieceList pieceList = board.GetPieceList(PieceType.None, isWhiteTeam);
        if (pieceList != null)
            foreach (Piece unit in pieceList)
            {
                if (board.SquareIsAttackedByOpponent(unit.Square))
                    totPoints1 -= (int)unit.PieceType;
            };
        board.MakeMove(move);
        int totPoints2 = 0;
        pieceList = board.GetPieceList(PieceType.None, isWhiteTeam);
        if (pieceList != null)
            foreach (Piece unit in pieceList)
            {
                if (board.SquareIsAttackedByOpponent(unit.Square))
                    totPoints2 += (int)unit.PieceType;
            };
        localPoint -= totPoints2 - totPoints1;
        board.UndoMove(move);
        // Add points if can attack enemie unit
        totPoints1 = 0;
        pieceList = board.GetPieceList(PieceType.None, !isWhiteTeam);
        if (pieceList != null)
            foreach (Piece unit in pieceList)
            {
                if (board.SquareIsAttackedByOpponent(unit.Square))
                    totPoints1 += (int)unit.PieceType;
            };
        board.MakeMove(move);
        totPoints2 = 0;
        pieceList = board.GetPieceList(PieceType.None, !isWhiteTeam);
        if (pieceList != null)
            foreach (Piece unit in pieceList)
            {
                if (board.SquareIsAttackedByOpponent(unit.Square))
                    totPoints2 += (int)unit.PieceType;
            };
        localPoint += (totPoints1 - totPoints2);
        board.UndoMove(move);
        // Add Point For Kill
        if (board.GetPiece(move.TargetSquare).PieceType != 0)
            localPoint += (int)board.GetPiece(move.TargetSquare).PieceType;

        return localPoint;
    }

    private double goCloseKing(Board board, Move move)
    {
        // Not The King
        if (!board.GetPiece(move.StartSquare).IsKing)
        {
            // Add or remove point 
            double actudistance = distanceBetween(move.StartSquare, board.GetKingSquare(!board.IsWhiteToMove));
            double finalDistance = distanceBetween(move.TargetSquare, board.GetKingSquare(!board.IsWhiteToMove));
            if (finalDistance < actudistance)
            {
                return actudistance / 100;
            }
            else
            {
                return -actudistance / 100;
            }
        }
        else return 0;
    }

    private double distanceBetween(Square startSquare, Square targetSquare)
    {
        return Math.Pow(posXWithCaseName(startSquare.Name) - posXWithCaseName(targetSquare.Name), 2)
        + Math.Pow(startSquare.Name[1] - targetSquare.Name[1], 2);
    }

    public int posXWithCaseName(String caseName)
    {
        switch (caseName[0])
        {
            case ('a'):
                return 1;
            case ('b'):
                return 2;
            case ('c'):
                return 3;
            case ('d'):
                return 4;
            case ('e'):
                return 5;
            case ('f'):
                return 6;
            case ('g'):
                return 7;
            case ('h'):
                return 8;
        }
        return 0;
    }
}