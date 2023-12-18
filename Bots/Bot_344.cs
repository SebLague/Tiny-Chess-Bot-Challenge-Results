namespace auto_Bot_344;
using ChessChallenge.API;
using System;
using System.Linq;


public class Bot_344 : IChessBot
{
    //sry for the bad english
    //made by DigestDig9

    Move[] moves;
    Board board;
    int[] Values = { 0, 1, 2, 2, 3, 100, 1000000 };
    int[] Ranges = { 0, 1, 3, 11, 8, 3, 1 };
    bool white;
    int moveCount = 0;
    Timer timer;
    int edge;
    Move lastMove = new();
    public Move Think(Board board, Timer timer)
    {

        this.timer = timer;
        white = board.IsWhiteToMove; //get the plaing color
        edge = 1;
        if (white)
        {
            edge = 8; //gets the edge of the board
        }
        moves = board.GetLegalMoves();
        this.board = board;
        Move move = getBestMove(moves, out float bestMoveValue, out bool safe);
        if (move == Move.NullMove)
        { //safety = important
            move = getBestMove(moves, out bestMoveValue, out safe);
        }
        moveCount++;
        return move;

    }
    Move getBestMove(Move[] moves, out float bestmovevalue, out bool safe)
    {
        safe = false;
        Move bestmove = new();
        bestmovevalue = float.MinValue;
        foreach (Move move in moves)
        {
            safe = isMoveSafe(move); //can the enemy recapture me after this move?
            PieceType mypiecetype = move.MovePieceType;
            int myValue = Values[(int)mypiecetype];
            float moveValue = 0;
            int captureValue;

            Piece c = board.GetPiece(move.TargetSquare);
            if (startWinnigProtocol())
            {
                int currentKingMOves = EnemyKingMoves(board);
                board.MakeMove(move);
                int newKingMoves = EnemyKingMoves(board);
                board.UndoMove(move);
                if (newKingMoves > currentKingMOves)
                {  //raises the probabilities of a checkmate
                    moveValue += 50;
                    myValue += 100;
                }
                if (mypiecetype == PieceType.Pawn)
                { //pawns are more likely to move
                    moveValue += 45;

                }
            }
            if (isQueenInDanger(move))
            { // protect the queen
                moveValue -= 50;
            }
            if (!safe)
            {
                if (c.IsNull)
                { //avoid loosing pieces
                    moveValue = float.MinValue;
                }
                else if ((captureValue = Values[(int)c.PieceType]) > myValue)
                { //capture enemy pieces
                    moveValue += captureValue * 2;
                }
            }
            //developing pieces
            moveValue += ControlSquares(move) * 4;
            moveValue -= DistanceToCentre(move) * 2;
            myValue += (8 - DistanceToCentre(move)) * 2;


            int kingRank = board.GetKingSquare(!white).Rank;
            int kingFile = board.GetKingSquare(!white).File;
            if (mypiecetype == PieceType.Pawn)
            {
                moveValue += Math.Max(-4, 40 * MathF.Cos(-0.71f * moveCount)); //teorically it should move the pawns at the ending 

            }
            if (move.PromotionPieceType == PieceType.Queen && isMoveSafe(move))
            { //promotion time
                moveValue += 100;
            }
            if (mypiecetype != PieceType.King)
            { //encorages puieces to move torwards the enemy king
                moveValue -= ((float)Math.Sqrt(((move.TargetSquare.Rank - kingRank) * (move.TargetSquare.Rank - kingRank)) + ((move.TargetSquare.File - kingFile) * (move.TargetSquare.File - kingFile))) - Ranges[(int)mypiecetype]) * moveCount;
                moveValue -= Math.Abs(move.TargetSquare.File - edge) * moveCount;
            }
            else { moveValue -= 25 * moves.Length; } //if there are other moves dont move the king




            if (MoveIsCheckmate(board, move))
            { //time to win
                return move;
            }
            if (lastMove.StartSquare == move.TargetSquare)
            { //avoid repetitions
                moveValue -= 30;
            }
            if (myValue > 80 && !safe)
            {      //avoid losing pieces
                moveValue = float.MinValue;
            }
            if (this.moves.Contains(move) && move != Move.NullMove)
            { //avoids making null moves maybe
                if (moveValue > bestmovevalue)
                {
                    bestmovevalue = moveValue;
                    bestmove = move;
                }
            }
        }
        lastMove = bestmove;

        return bestmove;
    }



    bool MoveIsCheckmate(Board board, Move move) //thx to sebastian league for this
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
    bool isQueenInDanger(Move move) //checks if some enemy piece can capture your queen
    {
        board.MakeMove(move);
        Move[] enemyMoves = board.GetLegalMoves(true);
        foreach (Move enemyMove in enemyMoves)
        {
            if (enemyMove.CapturePieceType == PieceType.Queen)
            {
                board.UndoMove(move);
                return true;
            }
        }
        board.UndoMove(move);
        return false;

    }
    bool startWinnigProtocol() //if the enemy dosn't have a queen try to checkmate
    {
        return board.GetPieceList(PieceType.Queen, !white).Count == 0;
    }
    int EnemyKingMoves(Board board) //how many squares can the enemy king attach
    {
        ulong king = BitboardHelper.GetKingAttacks(board.GetKingSquare(!white));
        return BitboardHelper.GetNumberOfSetBits(king);
    }
    ulong GetDangerSquares(Board board, bool white) //the enemy squares thath are in danger
    {
        PieceList[] pieces = board.GetAllPieceLists();
        ulong TotalDangerSquares = 0;
        foreach (PieceList piecess in pieces)
        {
            foreach (Piece piece in piecess)
            {
                if (piece.IsWhite != white)
                {
                    ulong pieceAttachs = BitboardHelper.GetPieceAttacks(piece.PieceType, piece.Square, board, !white);
                    TotalDangerSquares |= pieceAttachs;

                }
            }
        }
        return TotalDangerSquares;
    }
    bool isMoveSafe(Move move)   //dosn't work 100% of the times but better than nothing
    {
        board.MakeMove(move);
        ulong danger = GetDangerSquares(board, white);
        board.UndoMove(move);
        return !BitboardHelper.SquareIsSet(danger, move.TargetSquare);
    }
    int ControlSquares(Move move) //how many sqares do i control whit this move?
    {
        ulong attachs = 0;
        attachs = GetDangerSquares(board, !white);
        return BitboardHelper.GetNumberOfSetBits(attachs);
    }
    int DistanceToCentre(Move move) // I guess is self explanatory
    {
        int distance1 = distanceToSquare(move.TargetSquare, new Square("d4"));
        int distance2 = distanceToSquare(move.TargetSquare, new Square("d5"));
        int distance3 = distanceToSquare(move.TargetSquare, new Square("e4"));
        int distance4 = distanceToSquare(move.TargetSquare, new Square("e5"));
        return Math.Min(Math.Min(distance1, distance2), Math.Min(distance3, distance4));
    }
    int distanceToSquare(Square start, Square target) //the distance betwenn 2 squares
    {
        int fileDifference = target.File - start.File;
        int rankDifference = target.Rank - start.Rank;
        return (int)Math.Round(Math.Sqrt((fileDifference * fileDifference) + (rankDifference * rankDifference)));
    }

}