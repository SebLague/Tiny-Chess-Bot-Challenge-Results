namespace auto_Bot_125;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_125 : IChessBot
{
    private static int Value(PieceType piece)
    {
        switch (piece)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
            case PieceType.Bishop:
                return 300;
            case PieceType.Rook:
                return 500;
            case PieceType.Queen:
                return 900;
            case PieceType.King:
                return 1000;
            default:
                return 0;
        }
    }

    private static int SquaresAttacking(Board board, PieceList[] pieceLists, bool white)
    {
        ulong bitboard = 0;
        foreach (PieceList pieceList in pieceLists)
        {
            if (pieceList == null || pieceList.IsWhitePieceList != white) continue;
            foreach (Piece piece in pieceList)
            {
                ulong attacked = 0;
                switch (pieceList.TypeOfPieceInList)
                {
                    case PieceType.Pawn:
                        attacked = BitboardHelper.GetPawnAttacks(piece.Square, white);
                        break;
                    case PieceType.Knight:
                        attacked = BitboardHelper.GetKnightAttacks(piece.Square);
                        break;
                    case PieceType.Bishop:
                    case PieceType.Rook:
                    case PieceType.Queen:
                        attacked = BitboardHelper.GetSliderAttacks(pieceList.TypeOfPieceInList, piece.Square, board);
                        break;
                    case PieceType.King:
                        attacked = BitboardHelper.GetKingAttacks(piece.Square);
                        break;
                }
                if (attacked != 0)
                {
                    int index = BitboardHelper.ClearAndGetIndexOfLSB(ref attacked);
                    do
                    {
                        Square set = new Square(index);
                        BitboardHelper.SetSquare(ref bitboard, set);
                        index = BitboardHelper.ClearAndGetIndexOfLSB(ref attacked);
                    } while (index != 64 && bitboard != 0);
                }
            }
        }
        int score = 0;
        if (bitboard != 0)
        {
            int i = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
            do
            {
                score += Math.Abs(4 - (i / 8));
                i = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
            } while (i != 64);
        }
        return score;
    }

    private static int Score(Board board, bool white)
    {
        int score = 0;

        PieceList[] pieceLists = board.GetAllPieceLists();
        int highestValue = 0; //piece of whichever players turn it is with the highest value
        foreach (PieceList pieceList in pieceLists)
        {
            if (pieceList == null) continue;
            bool pieceWhite = pieceList.IsWhitePieceList;
            foreach (Piece piece in pieceList)
            {
                //raw piece value
                score += (pieceWhite ? 1 : -1) * Value(pieceList.TypeOfPieceInList);

                //check if piece is threatened, then check if there is another piece threatening the original attacker

                int attackers = 0; //how many pieces are threatening the original piece
                bool attackerSafe = false; //is there an attacking piece that is not also being attacked
                bool covered = false; //is the original piece covered by another piece of the same color

                int lowestValueAttacker = 9999999;

                bool skip = pieceWhite == (board.PlyCount % 2 == 0);

                if (skip) skip = board.TrySkipTurn();
                foreach (Move capture in board.GetLegalMoves(true))
                {
                    if (board.GetPiece(capture.TargetSquare) == piece)
                    {
                        attackers++;
                        int attackerValue = Value(capture.MovePieceType);
                        if (attackerValue < lowestValueAttacker)
                        {
                            lowestValueAttacker = attackerValue;
                        }
                        if (board.SquareIsAttackedByOpponent(capture.TargetSquare))
                        {
                            covered = true;
                        }
                        if (!board.SquareIsAttackedByOpponent(capture.StartSquare))
                        {
                            attackerSafe = true;
                        }
                    }
                }
                if (skip) board.UndoSkipTurn();

                int worth = Value(pieceList.TypeOfPieceInList);
                int signedWorth = (pieceWhite ? 1 : -1) * worth;
                if (!covered && ((attackers > 0 && attackerSafe) || attackers > 1))
                {
                    if ((board.PlyCount % 2 == 0) == pieceWhite) //if it will be the turn of the owner of the current piece
                    {
                        if (worth > highestValue)
                        {
                            score -= ((pieceWhite ? 1 : -1) * highestValue * 3) / 4;
                            highestValue = worth;
                        }
                        else
                        {
                            score -= (signedWorth * 3) / 4;
                        }
                    }
                    else
                    {
                        score -= (signedWorth * 3) / 4;
                    }
                }
                else if (attackers == 1 && !attackerSafe)
                {

                }
                else if (covered && attackers > 0 && (board.PlyCount % 2 == 0) != pieceWhite)
                {
                    if (lowestValueAttacker < worth)
                    {
                        score -= ((signedWorth - (pieceWhite ? 1 : -1) * lowestValueAttacker) * 3) / 4;
                    }
                    else
                    {
                        score -= (signedWorth * attackers) / 25;
                    }
                }

            }
        }
        /*
        //if either is in check
        if (board.TrySkipTurn())
        {
            if (board.IsInCheck())
            {
                score += (board.PlyCount % 2 == 0 ? 100 : -100);
            }
            board.UndoSkipTurn();
        } else
        {
            score -= (board.PlyCount % 2 == 0 ? 100 : -100);
        }*/

        //board control
        score += SquaresAttacking(board, pieceLists, true);
        score -= SquaresAttacking(board, pieceLists, false);
        return (white ? 1 : -1) * score;
    }

    private (int, Move) FindBest(Board board, int maxPly)
    {
        Move[] moves = board.GetLegalMoves();
        (int, Move)[] scores = new (int, Move)[moves.Length];

        bool white = board.PlyCount % 2 == 0;

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            scores[i].Item1 = Score(board, white);
            scores[i].Item2 = moves[i];
            board.UndoMove(moves[i]);
        }

        scores = scores.OrderByDescending(t => t.Item1).ToArray();
        if (board.PlyCount >= maxPly)
        {
            return scores[0];
        }

        //represents the highest score after the opponent playes an optimal move
        int bestScore = -int.MaxValue;
        Move bestMove = moves[0];

        //the lower this value is, the better that the score function is expected/needs to be
        int pruneThreshold = 150;

        for (int i = 0; i < Math.Min(scores.Length, 6) && scores[i].Item1 > scores[0].Item1 - pruneThreshold; i++)
        {
            Move move = scores[i].Item2;
            board.MakeMove(move);

            //if draw or checkmate
            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }
            else if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return (999999, move);
            }

            (int, Move) next = FindBest(board, maxPly);

            board.MakeMove(next.Item2);
            int score = Score(board, white);
            board.UndoMove(next.Item2);

            //maximize the minimum score we could have after the opponents move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            board.UndoMove(move);
        }

        return (bestScore, bestMove);
    }

    public Move Think(Board board, Timer timer)
    {
        int layers = Math.Min(4, (board.PlyCount + 2) / 2);
        if (timer.MillisecondsRemaining < 10000)
        {
            layers--;
        }

        (int, Move) best = FindBest(board, board.PlyCount + layers);
        DivertedConsole.Write(best.Item1);

        return best.Item2;
    }
}