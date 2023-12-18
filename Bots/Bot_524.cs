namespace auto_Bot_524;

using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_524 : IChessBot
{
    //Give pieces estimates of future move value
    int[] pieceVals = { 0, 150, 400, 450, 750, 1500, 100000 };

    public Move Think(Board board, Timer timer)
    {
        return Search(board, timer);
    }

    public Move Search(Board board, Timer timer)
    {
        Dictionary<Move, int> evals = new Dictionary<Move, int>();
        Move[] moves = board.GetLegalMoves();
        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            int pSquare = board.SquareIsAttackedByOpponent(moves[i].TargetSquare) ? 0 : 1;
            int capture = (int)moves[i].CapturePieceType;
            int mover = (int)moves[i].MovePieceType;
            evals.Add(moves[i], Eval(board, pSquare, capture, mover));
            board.UndoMove(moves[i]);
        }
        var sortedEvals = evals.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);


        return sortedEvals.Keys.First();
    }

    //eval of position on opponent's turn
    public int Eval(Board board, int pSquare, int capture, int mover)
    {
        // This section of eval is a measure of present position value, whereas
        // future potential is evaluated in the move eval section under oneSideEval()

        //skip eval if checkmate
        if (board.IsInCheckmate()) { return 100000; }

        int eval = 0;

        //add value if check
        ulong piecesBoard = board.AllPiecesBitboard;
        int pieceNum = 0;
        for (int j = 0; j < 64; j++)
        {
            pieceNum += (int)((piecesBoard >> j) & 0x01);
        }
        if (board.IsInCheck()) { eval += 3000 * (32 - pieceNum) * pSquare; }

        //add capture value, and subtract mover value if retakable
        eval += pieceVals[capture] - pieceVals[mover] * pSquare;

        //add possible player moves
        if (board.TrySkipTurn())
        {
            eval += oneSideEval(board, 0);
            board.UndoSkipTurn();
        }
        else
        {
            //Opponent is in check. 
            return -1 * oneSideEval(board, 1);
        }

        //subtract opponent moves
        //Only count attacks from opponent moves because it's their turn, so only
        //their potential captures will have an impact.
        eval -= oneSideEval(board, 1);
        return eval;
    }

    public int oneSideEval(Board board, int opponentMove)
    {
        int eval = 0;
        // This section of eval is a measure of future potential moves. What you
        // maximize here, it will maximize for the future, often at the cost of
        // the present.
        Move[] moves = board.GetLegalMoves();
        for (int i = 0; i < moves.Length; i++)
        {
            int draw = 1;
            bool mate = false;
            int check = 0;
            board.MakeMove(moves[i]);
            //if opponent moves check for checkmate
            if (opponentMove > 0) { mate = board.IsInCheckmate(); }
            draw = board.IsDraw() ? 0 : 1;
            check = board.IsInCheck() ? 1 : 0;
            board.UndoMove(moves[i]);
            if (mate) { return 100000; }

            Square oppKing = board.GetKingSquare(!(board.IsWhiteToMove));
            int distToKing = Math.Abs(oppKing.Rank - moves[i].StartSquare.Rank)
                + Math.Abs(oppKing.File - moves[i].StartSquare.File);
            int protectedSquare = board.SquareIsAttackedByOpponent(moves[i].TargetSquare) ? 0 : 1;
            ulong piecesBoard = board.AllPiecesBitboard;
            int pieceNum = 0;
            for (int j = 0; j < 64; j++)
            {
                pieceNum += (int)((piecesBoard >> j) & 0x01);
            }
            int isKing = (int)moves[i].MovePieceType == 6 ? 1 : 0;
            int isPawn = (int)moves[i].MovePieceType == 1 ? 5 : 1;
            int isWhite = board.IsWhiteToMove ? 1 : -1;
            int lessThan = pieceNum < 6 ? -1 : 1;
            // This is where the magic happens. This is the estimation of the
            // value of a possible move in the position.
            // Different values in the formula are activated or adjusted based
            // on whether or not they are relevant.
            eval += (7 * protectedSquare) //the value of a simple move (unless it can be taken)
                - (isKing * pieceNum * ((opponentMove) + 1)) //king safety (wants to be surrounded unless it's late game)
                + (check * (32 - pieceNum)) //desire to be able to check increases throughout game
                + (distToKing * (32 - pieceNum) * (isKing + 1)) //future distance to king wants to increase as time goes on (not present distance)
                + ((moves[i].StartSquare.Rank - 4) * isWhite * isPawn) //pawns want to push to promote
                + (opponentMove * (pieceVals[(int)moves[i].CapturePieceType])); //if the piece can capture, count the estimated capture value
        }
        return eval;
    }
}