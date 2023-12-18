namespace auto_Bot_404;
using ChessChallenge.API;
using System;

public class Bot_404 : IChessBot
{
    int fullEval(Board board, bool start, bool end, bool mid)
    {
        int num = 0;
        PieceList[] allPieces = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieces)
        {
            if (pieceList.TypeOfPieceInList == PieceType.Pawn)
            {
                if (pieceList.IsWhitePieceList) { num = num + 100 * pieceList.Count; }
                else { num += -100 * pieceList.Count; }


                if (end | start)
                {
                    foreach (Piece pawn in pieceList)
                    {
                        if (pieceList.IsWhitePieceList) { num = num + pawn.Square.Rank; }
                        else { num += pawn.Square.Rank - 7; }
                    }
                }

            }

            else if (pieceList.TypeOfPieceInList == PieceType.Knight | pieceList.TypeOfPieceInList == PieceType.Bishop)
            {
                if (pieceList.IsWhitePieceList) { num = num + 300 * pieceList.Count; }
                else { num += -300 * pieceList.Count; }


                if (start | mid)
                {
                    foreach (Piece pie in pieceList)
                    {
                        if (pieceList.IsWhitePieceList & (pie.Square.Rank == 0))
                        {
                            num -= 10;
                        }
                        else if ((pie.Square.Rank == 7))
                        {
                            num += 10;
                        }
                    }
                }


            }
            else if (pieceList.TypeOfPieceInList == PieceType.Rook)
            {
                if (pieceList.IsWhitePieceList == true) { num = num + 500 * pieceList.Count; }
                else { num += -500 * pieceList.Count; }




            }
            else if (pieceList.TypeOfPieceInList == PieceType.Queen)
            {
                if (pieceList.IsWhitePieceList == true) { num = num + 900 * pieceList.Count; }
                else { num += -900 * pieceList.Count; }


                if (start)
                {
                    foreach (Piece pie in pieceList)
                    {
                        if (pieceList.IsWhitePieceList == true & pie.Square.Rank == 0)
                        {
                            num += -5;
                        }
                        else if (pieceList.IsWhitePieceList == false & pie.Square.Rank == 7)
                        {
                            num += +5;
                        }
                    }
                }

            }
            else if (mid)
            {
                int[] temp = new int[] { -1, 1 };
                int newRank;
                int newFile;
                foreach (int i in temp)
                {
                    foreach (int j in temp)
                    {
                        newRank = pieceList[0].Square.Rank + i;
                        newFile = pieceList[0].Square.File + j;
                        if (newRank > 0 & newRank < 8 & newFile > 0 & newFile < 8) // if valid square
                        {
                            Square testSq = new Square(newFile, newRank);
                            if (board.SquareIsAttackedByOpponent(testSq))
                            {
                                if (pieceList.IsWhitePieceList)
                                {
                                    num += -10;
                                }
                                else
                                {
                                    num += 10;
                                }
                            }


                        }
                    }
                }
            }
        }



        return num;
    }

    int midEval(Board board)
    {
        return fullEval(board, false, false, true);
    }

    int startEval(Board board)
    {
        return fullEval(board, true, false, true);
    }

    int endEval(Board board)
    {
        return fullEval(board, false, true, false);
    }
    public int minMax(Board board, int turn, int depth, int capDepth, Func<Board, int> evaluator, int alpha, int beta)
    {
        if ((depth < 1) & (capDepth < 1))
        {
            return evaluator(board);
        }

        if (board.IsInCheckmate())
        {
            return -10000 * turn;
        }

        if (board.IsDraw())
        {
            return 0;
        }

        Move[] moves;

        if (depth > 0)
        {
            moves = board.GetLegalMoves();
        }
        else
        {
            moves = board.GetLegalMoves(true); // captures only
                                               //DivertedConsole.Write("move");
                                               //DivertedConsole.Write(moves.Length);


        }

        if (moves.Length == 0)
        {
            return evaluator(board);
        }

        int[] moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {

            board.MakeMove(moves[i]);
            if (depth > 0)
            {
                moveScores[i] = minMax(board, turn * -1, depth - 1, capDepth, evaluator, alpha, beta);
            }
            else
            {
                moveScores[i] = minMax(board, turn * -1, depth, capDepth - 1, evaluator, alpha, beta);
            }
            board.UndoMove(moves[i]);

            if (turn == 1)
            {
                alpha = Math.Max(alpha, moveScores[i]);
                if (moveScores[i] >= beta) { break; }
            }
            else
            {
                beta = Math.Min(beta, moveScores[i]);
                if (moveScores[i] <= alpha) { break; }
            }
        }

        int bestScore = int.MinValue * turn;
        for (int i = 0; i < moves.Length; i++)
        {
            if (bestScore * turn < moveScores[i] * turn)
            {
                bestScore = moveScores[i];
            }
        }

        return bestScore;
    }

    public int pieceCount(Board board)
    {
        int num = 0;

        foreach (PieceList pieces in board.GetAllPieceLists())
        {
            num += pieces.Count;
        }


        return num;
    }

    public Move Think(Board board, ChessChallenge.API.Timer timer)
    {

        int boardValue = pieceCount(board);
        //if (boardValue == 32) { return new Move("d2d4", board); }

        int turn = -1;
        if (board.IsWhiteToMove) { turn = 1; }

        Move[] moves = board.GetLegalMoves();
        int[] moveScores = new int[moves.Length];


        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            if (board.IsInCheckmate()) { return moves[i]; }

            if (timer.MillisecondsRemaining > 10000)
            {
                if (boardValue > 28) { moveScores[i] = minMax(board, turn * -1, 3, 1, startEval, -10000, 10000); }
                else if (boardValue < 14) { moveScores[i] = minMax(board, turn * -1, 3, 3, endEval, -10000, 10000); }
                else { moveScores[i] = minMax(board, turn * -1, 3, 1, midEval, -10000, 10000); }
            }
            else
            {
                moveScores[i] = minMax(board, turn * -1, 3, 0, midEval, -10000, 10000);
            }

            board.UndoMove(moves[i]);
        }
        Move bestMove = moves[0];
        int bestScore = int.MinValue * turn;
        for (int i = 0; i < moves.Length; i++)
        {
            if (bestScore * turn < moveScores[i] * turn)
            {
                bestScore = moveScores[i];
                bestMove = moves[i];
            }
        }
        return bestMove;
    }
}