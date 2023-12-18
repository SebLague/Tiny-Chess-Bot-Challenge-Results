namespace auto_Bot_170;
using ChessChallenge.API;
using System;

public class Bot_170 : IChessBot
{
    int depth = 4;

    private float Negamax(Board board, int depth, float alpha, float beta, int color)
    {
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return -1000 - depth; //hmmmm
        if (depth == 0)
        {
            return Quiescence(board, 2, alpha, beta, color);
        }

        float value = -1000;
        Move[] moves = orderedmoves(board); //ordering
        //Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            value = -Negamax(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(move);
            alpha = Math.Max(alpha, value);
            if (alpha >= beta) return beta;
        }
        return alpha;
    }

    private float Quiescence(Board board, int depth, float alpha, float beta, int color)
    {
        float initialeval = evaluate(board) * color;
        Move[] capturemoves = orderedmoves(board, true);
        if (board.IsDraw()) return -5;
        if (board.IsInCheckmate()) return -1000 - depth;
        if (depth == 0) return initialeval;
        if (capturemoves.Length == 0) return initialeval;
        if (alpha >= beta) return beta;
        alpha = Math.Max(alpha, initialeval);

        foreach (Move move in capturemoves)
        {
            board.MakeMove(move);
            float value = -Quiescence(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(move);
            if (alpha >= beta) return beta;
            alpha = Math.Max(alpha, value);

        }
        return alpha;

    }

    private Move[] orderedmoves(Board board, bool capture = false)
    {
        Move[] moves = board.GetLegalMoves(capture);
        (Move, float)[] scoredmoves = new (Move, float)[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            scoredmoves[i] = (moves[i], Score_Move(moves[i], board));
        }
        Array.Sort(scoredmoves, (tuple1, tuple2) => tuple2.Item2.CompareTo(tuple1.Item2));
        for (int i = 0; i < moves.Length; i++)
        {
            moves[i] = scoredmoves[i].Item1;
        }
        return moves;
    }

    private float Score_Move(Move move, Board board)
    {
        if (move.IsPromotion) return 9;
        if (move.IsCapture)
            return 10 * (int)move.CapturePieceType - (int)move.MovePieceType;
        if (board.SquareIsAttackedByOpponent(move.TargetSquare)) return -(int)move.MovePieceType;
        return 0;
    }

    private float evaluate(Board board)
    {
        float value = 0;
        Move[] moves = board.GetLegalMoves();
        PieceList[] lists = board.GetAllPieceLists();
        value += lists[0].Count;
        value += lists[1].Count * 3;
        value += lists[2].Count * 3;
        value += lists[3].Count * 5;
        value += lists[4].Count * 9;
        value -= lists[6].Count;
        value -= lists[7].Count * 3;
        value -= lists[8].Count * 3;
        value -= lists[9].Count * 5;
        value -= lists[10].Count * 9;

        ulong whiteAttacks = 0;
        ulong blackAttacks = 0;

        // Calculate white attacks
        for (int index = 0; index < 64; index++)
        {
            Square square = new Square(index);
            Piece piece = board.GetPiece(square);
            PieceType pieceType = board.GetPiece(square).PieceType;
            if (piece.IsNull) continue;
            if (piece.IsWhite)
            {
                if (piece.IsBishop || piece.IsRook || piece.IsQueen)
                {
                    whiteAttacks |= BitboardHelper.GetSliderAttacks(pieceType, square, board);
                }
                else if (piece.IsPawn)
                {
                    whiteAttacks |= BitboardHelper.GetPawnAttacks(square, true);
                }
                else if (piece.IsKnight)
                {
                    whiteAttacks |= BitboardHelper.GetKnightAttacks(square);
                }
                else if (piece.IsKing)
                {
                    whiteAttacks |= BitboardHelper.GetKingAttacks(square);
                }
            }
            else
            {
                if (piece.IsBishop || piece.IsRook || piece.IsQueen)
                {
                    blackAttacks |= BitboardHelper.GetSliderAttacks(pieceType, square, board);
                }
                else if (piece.IsPawn)
                {
                    blackAttacks |= BitboardHelper.GetPawnAttacks(square, false);
                }
                else if (piece.IsKnight)
                {
                    blackAttacks |= BitboardHelper.GetKnightAttacks(square);
                }
                else if (piece.IsKing)
                {
                    blackAttacks |= BitboardHelper.GetKingAttacks(square);
                }
            }
        }
        int whiteAttackedSquares = BitboardHelper.GetNumberOfSetBits(whiteAttacks);
        int blackAttackedSquares = BitboardHelper.GetNumberOfSetBits(blackAttacks);
        return value + (whiteAttackedSquares - blackAttackedSquares) / 2;
    }


    public Move Think(Board board, Timer timer)
    {
        for (int d = 0; d < 20; d++)
        {
            break;
        }
        Move[] moves = board.GetLegalMoves();
        Move bestmove = moves[0];
        float bestvalue = -1010;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float value;
            if (board.IsWhiteToMove)
            {
                value = -Negamax(board, depth, -1010, 1000, 1);
            }
            else
            {
                value = -Negamax(board, depth, -1010, 1000, -1);
            }
            board.UndoMove(move);
            if (value > bestvalue)
            {
                bestvalue = value;
                bestmove = move;
            }
        }
        DivertedConsole.Write(bestvalue);


        return bestmove;
    }
}