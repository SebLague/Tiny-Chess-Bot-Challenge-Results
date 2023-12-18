namespace auto_Bot_89;
using ChessChallenge.API;
using System;

public class Bot_89 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int depth = 3;
        if (timer.MillisecondsRemaining >= 20000)
        {
            depth = 4;
        }

        Move[] allMoves = board.GetLegalMoves(false);
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
        Move[] bestMoves = new Move[depth + 1];

        Func<Board, int, int> loop = null;
        loop = (b, d) =>
        {
            if (d == 0)
            {
                return 0;
            }
            else
            {
                int highestValueMove = int.MinValue;
                Move[] allMoves = b.GetLegalMoves(false);

                foreach (Move move in allMoves)
                {
                    int sum = 0;

                    if (move.IsCapture)
                    {
                        sum += pieceValues[(int)b.GetPiece(move.TargetSquare).PieceType];
                    }
                    if (move.IsPromotion)
                    {
                        sum += 800;
                    }
                    if (move.IsCastles)
                    {
                        sum += 100;
                    }
                    if (move.IsEnPassant)
                    {
                        sum += 100;
                    }


                    b.MakeMove(move);

                    if (b.IsInCheck())
                    {
                        sum += 5;
                    }

                    int value = sum;

                    if (b.IsInCheckmate())
                    {
                        value += 1000000;
                    }
                    else if (b.IsDraw())
                    {
                        sum -= 500000;
                    }
                    else
                    {
                        value -= loop(b, d - 1);
                    }

                    b.UndoMove(move);

                    switch (move.MovePieceType)
                    {
                        case PieceType.Pawn:
                            value += 9;
                            break;
                        case PieceType.Bishop:
                        case PieceType.Knight:
                            value += 7;
                            break;
                        case PieceType.Rook:
                            value += 5;
                            break;
                        case PieceType.Queen:
                            value += 2;
                            break;
                    }

                    if (highestValueMove < value)
                    {
                        highestValueMove = value;
                        bestMoves[d] = move;
                    }
                }

                return highestValueMove;
            }
        };

        loop(board, depth);
        return bestMoves[depth];
    }

}