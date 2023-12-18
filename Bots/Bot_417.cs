namespace auto_Bot_417;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_417 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        bool isWhite = board.IsWhiteToMove;
        int initialDepth = 4;

        Move bestMove = board.GetLegalMoves()[0];
        AlphaBetaSearch(board, ref bestMove, initialDepth, initialDepth, -99999, 99999, true, isWhite);

        return bestMove;
    }

    //Evaluation function that takes into account the pieces of each colour
    public int BoardEvaluation(Board board, bool isWhite)
    {
        //Evaluating according to the board material
        int whiteEval = board.GetPieceList(PieceType.Pawn, true).Count * 10 + (board.GetPieceList(PieceType.Knight, true).Count + board.GetPieceList(PieceType.Bishop, true).Count) * 30 + board.GetPieceList(PieceType.Rook, true).Count * 50 + board.GetPieceList(PieceType.Queen, true).Count * 90;
        int blackEval = board.GetPieceList(PieceType.Pawn, false).Count * 10 + (board.GetPieceList(PieceType.Knight, false).Count + board.GetPieceList(PieceType.Bishop, false).Count) * 30 + board.GetPieceList(PieceType.Rook, false).Count * 50 + board.GetPieceList(PieceType.Queen, false).Count * 90;


        //Evaluating according to center control
        Piece midSquare = board.GetPiece(new Square("d4"));
        if (!midSquare.IsNull)
        {
            if (midSquare.IsWhite)
            {
                whiteEval += 20;
            }
            else
            {
                blackEval += 20;
            }
        }
        midSquare = board.GetPiece(new Square("d5"));
        if (!midSquare.IsNull)
        {
            if (midSquare.IsWhite)
            {
                whiteEval += 20;
            }
            else
            {
                blackEval += 20;
            }
        }
        midSquare = board.GetPiece(new Square("e4"));
        if (!midSquare.IsNull)
        {
            if (midSquare.IsWhite)
            {
                whiteEval += 20;
            }
            else
            {
                blackEval += 20;
            }
        }
        midSquare = board.GetPiece(new Square("e5"));
        if (!midSquare.IsNull)
        {
            if (midSquare.IsWhite)
            {
                whiteEval += 20;
            }
            else
            {
                blackEval += 20;
            }
        }


        //Evaluating according to the capture moves
        Move[] capMoves = board.GetLegalMoves(true);
        if (isWhite)
        {
            foreach (Move move in capMoves)
            {
                PieceType pieceType = move.CapturePieceType;
                if (pieceType == PieceType.King)
                {
                    whiteEval += 200;
                }
                else if (pieceType == PieceType.Queen)
                {
                    whiteEval += 80;
                }
                else
                {
                    whiteEval += 40;
                }
            }

            if (board.TrySkipTurn())
            {
                blackEval += board.GetLegalMoves(true).Length * 30;
                board.UndoSkipTurn();
            }

            return whiteEval - blackEval;
        }
        else
        {
            foreach (Move move in capMoves)
            {
                PieceType pieceType = move.CapturePieceType;
                if (pieceType == PieceType.King)
                {
                    blackEval += 200;
                }
                else if (pieceType == PieceType.Queen)
                {
                    blackEval += 80;
                }
                else
                {
                    blackEval += 40;
                }
            }

            if (board.TrySkipTurn())
            {
                whiteEval += board.GetLegalMoves(true).Length * 30;
                board.UndoSkipTurn();
            }

            return blackEval - whiteEval;
        }
    }

    //Minimax search function with alpha beta pruning
    public int AlphaBetaSearch(Board board, ref Move bestMove, int depth, int initialDepth, int a, int b, bool isMaxPlayer, bool isWhite)
    {
        if (depth == 0 || board.GetLegalMoves().Length == 0)
            return BoardEvaluation(board, isWhite);

        if (isMaxPlayer)
        {
            int value = -99999;
            foreach (Move move in board.GetLegalMoves())
            {
                if (!board.GameMoveHistory.Contains(move))
                {
                    board.MakeMove(move);
                    if (board.IsDraw())
                    {
                        board.UndoMove(move);
                        continue;
                    }

                    int childValue = AlphaBetaSearch(board, ref bestMove, depth - 1, initialDepth, a, b, false, isWhite);
                    if (childValue > value)
                    {
                        value = childValue;
                        if (initialDepth == depth)
                        {
                            bestMove = move;
                        }
                    }
                    board.UndoMove(move);
                    a = Math.Max(a, value);
                    if (a >= b)
                        break;
                }
            }
            return value;
        }
        else
        {
            int value = +99999;
            foreach (Move move in board.GetLegalMoves())
            {
                if (!board.GameMoveHistory.Contains(move))
                {
                    board.MakeMove(move);
                    if (board.IsDraw())
                    {
                        board.UndoMove(move);
                        continue;
                    }

                    int childValue = AlphaBetaSearch(board, ref bestMove, depth - 1, initialDepth, a, b, true, isWhite);
                    if (childValue < value)
                    {
                        value = childValue;
                        if (initialDepth == depth)
                        {
                            bestMove = move;
                        }
                    }
                    board.UndoMove(move);
                    a = Math.Min(a, value);
                    if (a >= b)
                        break;
                }
            }
            return value;
        }
    }
}