namespace auto_Bot_3;
using ChessChallenge.API;
using System;

public class Bot_3 : IChessBot
{
    // null, pawn, knight, bishop, rook, queen, king
    int[] pieceScore = { 0, 100, 300, 300, 500, 900, 10000 };
    const int sortaEndGameScore = 1000;
    int currentScore = 0;

    Move highIQMove = Move.NullMove;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        var rnd = new Random();

        Move finalMove = moves[rnd.Next(moves.Length)];
        int highestScore = 0;

        if (highIQMove != Move.NullMove)
        {
            var toDo = highIQMove;
            highIQMove = Move.NullMove;
            return toDo;
        }

        foreach (var move in moves)
        {

            if (CheckOrMate(board, move))
                return move;

            Piece targetP = board.GetPiece(move.TargetSquare);
            int takenPScore = pieceScore[(int)targetP.PieceType];

            if (currentScore >= sortaEndGameScore)
            {
                if (targetP.IsKing)
                {
                    finalMove = move;
                    if (Draw(board, move))
                        continue;
                    break;
                }
                else if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
                {
                    finalMove = move;
                    {
                        board.MakeMove(finalMove);
                        var movesLayer2 = board.GetLegalMoves();
                        foreach (var move2 in movesLayer2)
                        {
                            if (CheckOrMate(board, move2))
                            {
                                highIQMove = move2;
                                board.UndoMove(finalMove);
                                return finalMove;
                            }
                        }
                    }

                    if (Draw(board, move))
                        continue;
                    break;
                }
            }

            if (takenPScore > highestScore)
            {
                finalMove = move;
                highestScore = takenPScore;
            }

            if (Draw(board, move))
                continue;
        }


        currentScore += highestScore;
        return finalMove;
    }

    bool CheckOrMate(Board board, Move m)
    {
        board.MakeMove(m);
        bool dewIt = board.IsInCheckmate() || board.IsInCheck();
        board.UndoMove(m);
        return dewIt;
    }

    bool Draw(Board board, Move m)
    {
        board.MakeMove(m);
        bool dontDewIt = board.IsDraw();
        board.UndoMove(m);
        return dontDewIt;
    }
}