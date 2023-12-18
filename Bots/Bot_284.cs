namespace auto_Bot_284;
using ChessChallenge.API;
using System;

public class Bot_284 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        //Gets All Possible Moves
        Move[] moves = board.GetLegalMoves();

        int[] weights = new int[moves.Length];
        int curBestMove_id = 0;
        int curBestMove_weight = 0;

        //Assign weights to moves with optimal outcomes
        for (int i = 0; i < moves.Length; i++)
        {
            //Prioritize Checkmate first OBVIOUSLY
            if (MoveIsCheckmate(board, moves[i]))
            {
                return moves[i];
            }

            //If the enemy can put us in checkmate after making this move skip this move
            if (FutureCheckmatePossible(board, moves[i]))
            {
                continue;
            }

            //If move in question doesn't result in a capture add weight accordingly
            if (!board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
            {
                //If piece that is moving can capture or block a Check on our king without dying-Prioritize second
                if (board.SquareIsAttackedByOpponent(board.GetKingSquare(board.IsWhiteToMove)))
                {
                    if (!KingStillChecked(board, moves[i]))
                    {
                        return moves[i];
                    }
                }

                //Priotitize Check without being captured third
                if (MoveIsCheck(board, moves[i]))
                {
                    weights[i] += 7;
                }

                //Add weight to this move if the next turn yields a possible checkmate to the enemy king
                if (FutureEnemyCheckmatePossible(board, moves[i]))
                {
                    weights[i] += 6;
                }

                //Add weight to this move if the next turn yields a possible check to the enemy king
                if (FutureEnemyCheckPossible(board, moves[i]))
                {
                    weights[i] += 6;
                }

                //If piece that is moving is in danger of being captured add weight accordingly
                if (board.SquareIsAttackedByOpponent(moves[i].StartSquare))
                {
                    switch (moves[i].MovePieceType)
                    {
                        case PieceType.None:
                            break;
                        case PieceType.Pawn:
                            weights[i] += 3;
                            break;
                        case PieceType.Knight:
                            weights[i] += 4;
                            break;
                        case PieceType.Bishop:
                            weights[i] += 5;
                            break;
                        case PieceType.Rook:
                            weights[i] += 5;
                            break;
                        case PieceType.Queen:
                            weights[i] += 6;
                            break;
                        case PieceType.King:
                            weights[i] += 8;
                            break;
                        default:
                            break;
                    }
                }
                else if (moves[i].MovePieceType == PieceType.Pawn)
                    weights[i] += 2;
                else
                    weights[i] += 1;

                //If move in question results in capturing enemy piece without being captured add weight accordingly
                if (moves[i].IsCapture)
                {
                    if (moves[i].CapturePieceType == PieceType.Pawn)
                        weights[i] += 3;
                    if (moves[i].CapturePieceType == PieceType.Knight)
                        weights[i] += 4;
                    if (moves[i].CapturePieceType == PieceType.Bishop || moves[i].CapturePieceType == PieceType.Rook)
                        weights[i] += 5;
                    if (moves[i].CapturePieceType == PieceType.Queen)
                        weights[i] += 6;
                }
            }
            else
            {
                //If piece that is moving can capture or block a Check while being captured add weight accordingly
                if (board.SquareIsAttackedByOpponent(board.GetKingSquare(board.IsWhiteToMove)))
                {
                    if (!KingStillChecked(board, moves[i]))
                    {
                        if (moves[i].MovePieceType == PieceType.Pawn)
                            return moves[i];
                        if (moves[i].MovePieceType == PieceType.Knight)
                            weights[i] += 8;
                        if (moves[i].MovePieceType == PieceType.Bishop || moves[i].MovePieceType == PieceType.Rook)
                            weights[i] += 7;
                        if (moves[i].MovePieceType == PieceType.Queen)
                            weights[i] += 6;
                    }
                }

                //If move in question results in capturing enemy piece while being captured add weight accordingly
                if (moves[i].IsCapture)
                {
                    int weightmod = 0;

                    //Assign a negative to the weight assignments based on what piece we are sacrificing
                    if (moves[i].MovePieceType == PieceType.Knight && board.SquareIsAttackedByOpponent(moves[i].StartSquare))
                        weightmod = 3;
                    if (moves[i].MovePieceType == PieceType.Bishop || moves[i].MovePieceType == PieceType.Rook)
                        weightmod = 4;
                    if (moves[i].MovePieceType == PieceType.Queen)
                        weightmod = 5;
                    if (board.SquareIsAttackedByOpponent(moves[i].StartSquare) && moves[i].MovePieceType != PieceType.Pawn)
                        weightmod -= 2;

                    //Apply negative weight mod to our weights based on the piece we are capturing
                    if (moves[i].CapturePieceType == PieceType.Pawn)
                        weights[i] += 3 - weightmod;
                    if (moves[i].CapturePieceType == PieceType.Knight)
                        weights[i] += 4 - weightmod;
                    if (moves[i].CapturePieceType == PieceType.Bishop || moves[i].CapturePieceType == PieceType.Rook)
                        weights[i] += 5 - weightmod;
                    if (moves[i].CapturePieceType == PieceType.Queen)
                        weights[i] += 6 - weightmod;
                }
            }
        }

        //Assign the best move to choose based on whichever has the highest weight 
        for (int i = 0; i < weights.Length; i++)
        {
            if (curBestMove_weight < weights[i])
            {
                curBestMove_id = i;
                curBestMove_weight = weights[i];
            }
            else if (curBestMove_weight == weights[i])
            {
                Random rng = new();
                int rand = rng.Next(100);
                if (rand > 50)
                {
                    curBestMove_id = i;
                    curBestMove_weight = weights[i];
                }
            }
        }

        return moves[curBestMove_id];
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Test if this move gives check
    bool MoveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }

    //Check if our King is under Check
    bool KingStillChecked(Board board, Move move)
    {
        board.MakeMove(move);
        bool isChecked = board.SquareIsAttackedByOpponent(board.GetKingSquare(!board.IsWhiteToMove));
        board.UndoMove(move);
        return isChecked;
    }

    //Check if a checkmate can be made after this move is made on our next turn
    bool FutureEnemyCheckmatePossible(Board board, Move move)
    {
        board.MakeMove(move);

        bool possibleCheckmate = false;

        if (board.TrySkipTurn())
        {
            //Gets All Possible Moves
            Move[] moves = board.GetLegalMoves();

            for (int i = 0; i < moves.Length; i++)
            {
                if (MoveIsCheckmate(board, moves[i]))
                {
                    possibleCheckmate = true;
                    break;
                }
            }

            board.UndoSkipTurn();
        }

        board.UndoMove(move);

        return possibleCheckmate;
    }

    //Check if a check can be made after this move is made on our next turn
    bool FutureEnemyCheckPossible(Board board, Move move)
    {
        board.MakeMove(move);

        bool possibleCheck = false;

        if (board.TrySkipTurn())
        {
            //Gets All Possible Moves
            Move[] moves = board.GetLegalMoves();

            for (int i = 0; i < moves.Length; i++)
            {
                if (MoveIsCheck(board, moves[i]))
                {
                    possibleCheck = true;
                    break;
                }
            }

            board.UndoSkipTurn();
        }

        board.UndoMove(move);

        return possibleCheck;
    }

    //Check if we will will be put into checkmate after making this move
    bool FutureCheckmatePossible(Board board, Move move)
    {
        board.MakeMove(move);

        bool possibleCheckmate = false;

        //Gets All Possible Moves for enemy
        Move[] moves = board.GetLegalMoves();

        for (int i = 0; i < moves.Length; i++)
        {
            if (MoveIsCheckmate(board, moves[i]))
            {
                possibleCheckmate = true;
                break;
            }
        }

        board.UndoMove(move);

        return possibleCheckmate;
    }
}