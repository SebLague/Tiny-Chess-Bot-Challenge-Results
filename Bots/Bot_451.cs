namespace auto_Bot_451;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_451 : IChessBot
{
    Board board;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int val1 = 1;
    int val2 = 1;
    int val3 = 1;
    int val4 = 1;

    IDictionary<Move, int> MoveChoices = new Dictionary<Move, int>();
    IDictionary<Move, int> EnemyMoveChoices = new Dictionary<Move, int>();

    Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        MoveChoices.Clear();
        Move[] allMoves = board.GetLegalMoves();
        Move[] allAttacks = board.GetLegalMoves();


        foreach (Move move in allMoves)
        {

            board.MakeMove(move);

            if (badMove())
            {
                board.UndoMove(move);
                continue;
            }

            if (board.IsDraw())
            {
                MoveChoices.Add(move, 1);
                board.UndoMove(move);
                continue;
            }

            if (board.IsRepeatedPosition())
            {
                MoveChoices.Add(move, 1);
                board.UndoMove(move);
                continue;
            }

            if (board.IsInCheckmate())
            {
                MoveChoices.Add(move, 1000);
                board.UndoMove(move);
                break;
            }

            if (MateIn(2))
            {
                MoveChoices.Add(move, 999);
                board.UndoMove(move);
                continue;
            }

            if (MateIn(3))
            {
                MoveChoices.Add(move, 998);
                board.UndoMove(move);
                continue;
            }
            MoveChoices.Add(move, (int)Math.Round(70 + (tradeValue(move) / 50) - 30 * Math.Log(getHighestValueAttacked() + 1, 10)) + ((move.IsPromotion) ? (int)move.PromotionPieceType : 0) + ((board.IsInCheck()) ? val1 : 0) + ((possibleMoves(false) > allMoves.Length) ? val2 : 0) + ((possibleMoves(true) > allAttacks.Length) ? val3 : 0) + (MoreDefenders(move) ? val4 : 0) + (((int)move.MovePieceType == 6) ? -1 : 0));

            board.UndoMove(move);
        }

        if (MoveChoices.Count == 0)
        {
            MoveChoices.Add(allMoves[rng.Next(allMoves.Length)], 10);
        }

        MoveChoices = MoveChoices.OrderBy(x => rng.Next()).ToDictionary(item => item.Key, item => item.Value);

        return MoveChoices.MaxBy(kvp => kvp.Value).Key;
    }

    bool MateIn(int i)
    {
        if (i < 0) return false;
        if (i == 1) return board.IsInCheckmate();

        bool goodmove = false;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            goodmove = false;
            foreach (Move temp in board.GetLegalMoves())
            {
                board.MakeMove(temp);

                if (MateIn(i - 1))
                {
                    goodmove = true;
                    board.UndoMove(temp);
                    break;
                }

                board.UndoMove(temp);
            }
            if (!goodmove)
            {
                board.UndoMove(move);
                break;
            }

            board.UndoMove(move);
        }

        return goodmove;
    }

    bool badMove()
    {
        foreach (Move temp in board.GetLegalMoves())
        {
            board.MakeMove(temp);
            if (board.IsInCheckmate() || MateIn(2))
            {
                board.UndoMove(temp);
                return true;
            }
            board.UndoMove(temp);
        }
        return false;
    }

    int getHighestValueAttacked()
    {
        int max = 0;

        foreach (Move move in board.GetLegalMoves(true))
        {
            max = Math.Max(max, pieceValues[(int)move.CapturePieceType]);
        }

        return max / 10;
    }

    int tradeValue(Move move)
    {
        int value = pieceValues[(int)move.CapturePieceType];
        Move weakestPiece = Move.NullMove;

        foreach (Move temp in board.GetLegalMoves(true))
        {
            if (temp.TargetSquare == move.TargetSquare)
            {
                if (weakestPiece.IsNull)
                {
                    weakestPiece = temp;
                }
                else
                {
                    if (pieceValues[(int)weakestPiece.MovePieceType] > pieceValues[(int)temp.MovePieceType])
                    {
                        weakestPiece = temp;
                    }
                }
            }
        }

        if (!weakestPiece.IsNull)
        {
            board.MakeMove(weakestPiece);
            value -= tradeValue(weakestPiece);
            board.UndoMove(weakestPiece);
        }

        return value;
    }

    int possibleMoves(bool attacks)
    {
        int sum = 0;

        foreach (Move temp in board.GetLegalMoves())
        {
            board.MakeMove(temp);
            sum += board.GetLegalMoves(attacks).Length;
            board.UndoMove(temp);
        }

        int avg = board.GetLegalMoves().Length > 0 ? sum / board.GetLegalMoves().Length : 0;

        return avg;
    }

    bool MoreDefenders(Move move)
    {
        board.UndoMove(move);
        int def = 0;
        int att = 0;

        for (int i = 0; i < 64; i++)
        {
            Piece temp = board.GetPiece(new Square(i));
            if ((temp.Square != move.StartSquare) && (BitboardHelper.SquareIsSet(BitboardHelper.GetPieceAttacks(temp.PieceType, temp.Square, board, board.IsWhiteToMove), move.TargetSquare)))
            {
                if (temp.IsWhite == board.IsWhiteToMove)
                {
                    def++;
                }
                else
                {
                    att++;
                }
            }
        }

        board.MakeMove(move);
        return def >= att;
    }

}