namespace auto_Bot_438;
using ChessChallenge.API;

using System;


public class Bot_438 : IChessBot
{
    int[] pieceValues = { 0, 10, 30, 32, 70, 200, 1500 };

    public Move Think(Board board, Timer timer)
    {

        int bestMoveScore = 0, currentMoveScore, TimeRemanig = timer.MillisecondsRemaining;
        Move nextMove;
        Move[] Ourmoves = board.GetLegalMoves();

        Random rng = new();
        nextMove = Ourmoves[0];


        bestMoveScore = 0;
        short[] attackInOne = new short[64];



        calculeteAttackInOne(Ourmoves, 1);





        foreach (Move move in Ourmoves)
        {

            Square start = move.StartSquare, target = move.TargetSquare;

            currentMoveScore = attackInOne[target.Index];
            if (MoveIsCheckmate(board, move))
            {
                nextMove = move;
                break;
            }
            if (moveAllowsCheckmate(board, move))
            {
                currentMoveScore = -10000;
            }
            if (MoveIsdraw(board, move))
            {
                currentMoveScore += -31;
            }
            if (moveAllowsCheck(board, move, target))
            {
                currentMoveScore += -5;
            }

            currentMoveScore += posibleLoss(board, move);
            currentMoveScore += boardcontrol(board, move, 1);
            if (move.IsCapture)
            {

                int gain = pieceValues[(int)board.GetPiece(target).PieceType];


                currentMoveScore += gain;

            }


            if (bestMoveScore < currentMoveScore)
            {
                nextMove = move;
            }

            else if (bestMoveScore == currentMoveScore && rng.Next(3) == 1)
            {
                nextMove = move;
            }
        }



        bool canRecapture(Square square, Move move, Board board)
        {
            bool isrecapture = false;



            board.MakeMove(move);

            Move[] moves = board.GetLegalMoves(true);
            foreach (Move bit in moves)
            {
                if (bit.TargetSquare == square)
                {

                    isrecapture = !canRecapture(square, bit, board);
                    break;

                }
            }

            board.UndoMove(move);



            return isrecapture;

        }
        void calculeteAttackInOne(Move[] theMoves, short add)
        {
            bool howPlays = (board.IsWhiteToMove == true) ? true : false;
            int mult = (howPlays == true) ? 1 : -1;

            foreach (Move move in theMoves)
            {
                Square Start = move.StartSquare;
                int index = move.TargetSquare.Index;

                if (board.GetPiece(Start).IsPawn == false)
                {
                    attackInOne[index] += add;
                }

            }
            PieceList list = board.GetPieceList(PieceType.Pawn, howPlays);
            foreach (Piece piece in list)
            {
                int pawnIndex = piece.Square.Index, file = piece.Square.File;
                if (file != 0)
                {
                    attackInOne[pawnIndex - 1 + 8 * mult] += add;
                }

                if (file != 7)
                {
                    attackInOne[pawnIndex + 1 + 8 * mult] += add;
                }

            }
        }
        bool MoveIsCheckmate(Board board, Move move)
        {
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            return isMate;
        }

        bool moveAllowsCheckmate(Board board, Move move)
        {

            board.MakeMove(move);

            Move[] moves = board.GetLegalMoves();
            bool allowsMate = false;

            foreach (Move move4 in moves)
            {
                if (MoveIsCheckmate(board, move4))
                {
                    allowsMate = true;
                    break;
                }
            }
            board.UndoMove(move);
            return allowsMate;
        }
        int posibleLoss(Board board, Move move)
        {
            board.MakeMove(move);
            Move[] moves = board.GetLegalMoves(true);
            int materialTaken, bestMaterialTaken = 0;

            foreach (Move move2 in moves)
            {

                Square target2 = move2.TargetSquare;
                Square start2 = move2.StartSquare;
                int gain = pieceValues[(int)board.GetPiece(target2).PieceType];

                if (canRecapture(target2, move2, board))
                {
                    int lost = pieceValues[(int)move2.MovePieceType];
                    materialTaken = gain - lost;
                }
                else
                {
                    materialTaken = gain;
                }
                if (materialTaken > bestMaterialTaken)
                {
                    bestMaterialTaken = materialTaken;
                }
            }

            board.UndoMove(move);
            return bestMaterialTaken / -1;
        }

        int boardcontrol(Board board, Move move, int add)
        {

            int currentMoveControl = 0;
            board.MakeMove(move);
            board.ForceSkipTurn();

            bool howPlays = board.IsWhiteToMove;
            int rightrank = (howPlays == true) ? 4 : 0;

            Move[] theMoves = board.GetLegalMoves();
            foreach (Move move6 in theMoves)
            {
                Square Start = move6.StartSquare;

                int rank = move.TargetSquare.Rank;
                if (board.GetPiece(Start).IsPawn == false && rank + rightrank >= 5 && rank + rightrank < 9)
                {

                    currentMoveControl += add;
                }

            }

            board.UndoSkipTurn();
            board.UndoMove(move);
            return currentMoveControl;
        }

        bool MoveIsdraw(Board board, Move move)
        {
            board.MakeMove(move);
            bool isDraw = board.IsDraw();
            board.UndoMove(move);
            return isDraw;
        }
        bool moveAllowsCheck(Board board, Move move, Square target)
        {

            board.MakeMove(move);
            Move[] moves = board.GetLegalMoves();
            bool allowsCheck = false;

            foreach (Move move4 in moves)
            {
                if (board.IsInCheck() && false == canRecapture(target, move4, board))
                {
                    allowsCheck = true;
                    break;
                }
            }
            board.UndoMove(move);
            return allowsCheck;

        }

        return nextMove;
    }
}