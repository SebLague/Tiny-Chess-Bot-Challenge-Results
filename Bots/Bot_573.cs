namespace auto_Bot_573;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_573 : IChessBot
{
    // Current color (White or Black)
    public bool ogColor;

    // Starting depth for bot
    public int baseDepth = 2;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        ogColor = board.IsWhiteToMove;

        getDepth(board, timer);

        Move finalMove = GetMove(moves, board, 0, ogColor, timer);
        return finalMove;
    }

    private void getDepth(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 3500)
        {
            baseDepth = 0;
        }

        if (materialAdvantage(board, ogColor, true) < 7)
        {
            baseDepth = 4;
        }
        else if (materialAdvantage(board, ogColor, true) < 12)
        {
            baseDepth = 3;
        }
        else if (timer.MillisecondsRemaining < 17000)
        {
            baseDepth = 1;
        }
    }

    private Move GetMove(Move[] moves, Board board, int depth, bool color, Timer timer)
    {
        // Presorts the moves so that alpha-beta pruning is more effective
        moves = preSort(board, moves);

        Move bestMove = moves[0];
        double bestEval = Double.MinValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            double newEval = getDeepEval(board, 0, color, -1000000, 1000000, timer);

            // Gets the best evaluation and move
            if (newEval > bestEval)
            {
                bestMove = move;
                bestEval = newEval;
            }

            board.UndoMove(move);
        }

        /*DivertedConsole.Write("Best Eval - " + bestEval);*/

        return bestMove;
    }

    // Minimax algorithm with alpha beta pruning
    private double getDeepEval(Board board, int depth, bool color, double alpha, double beta, Timer timer)
    {
        // The best eval
        double bestEval;

        if ((depth > baseDepth) || (depth > baseDepth - 1 && timer.MillisecondsElapsedThisTurn > 4000))
        {
            bestEval = getBoardVal(board, color);
            return bestEval;
        }

        // Minimax algorithm with alpha-beta pruning
        if (color == ogColor)
        {
            bestEval = 100000.0;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                bestEval = Math.Min(getDeepEval(board, depth + 1, !color, alpha, beta, timer), bestEval);
                board.UndoMove(move);

                beta = Math.Min(beta, bestEval);
                if (beta <= alpha)
                {
                    break;
                }
            }

            return bestEval;
        }
        else
        {
            bestEval = -100000.0;
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                bestEval = Math.Max(getDeepEval(board, depth + 1, !color, alpha, beta, timer), bestEval);
                board.UndoMove(move);

                alpha = Math.Max(alpha, bestEval);
                if (beta <= alpha)
                {
                    break;
                }
            }

            return bestEval;
        }
    }

    // This function shortens time for alpha-beta
    private Move[] preSort(Board board, Move[] moves)
    {
        IEnumerable<Move> orderedMoves = moves.OrderBy(move => moveVal(board, move));

        int i = 0;
        foreach (Move move in orderedMoves)
        {
            moves[i] = move;
            i++;
        }

        return moves;
    }

    // Gets a rough move value for the presort
    private double moveVal(Board board, Move move)
    {
        double val = 0.0;

        // Orderby sorts negative so we use - to indicate a better move
        board.MakeMove(move);
        if (board.IsInCheck())
        {
            val -= 3.0;
        }

        if (move.IsCapture)
        {
            if ((getPieceValue(move.MovePieceType, move.TargetSquare, ogColor) - getPieceValue(move.CapturePieceType, move.TargetSquare, ogColor)) < 0)
            {
                val -= 4;
            }
            val -= 2.0;
        }

        if (move.IsPromotion)
        {
            val -= 1.0;
        }

        val -= getBoardVal(board, ogColor);
        board.UndoMove(move);

        return val;
    }

    // This is the evaluation function for this bot
    private double getBoardVal(Board board, bool color)
    {
        // This is the evaluation of the position that the computer thinks it is
        double evaluation = 0;

        // This gets both our king square and the opponents
        Square kingSquare = board.GetPieceList(PieceType.King, ogColor)[0].Square;
        Square opponentKingSquare = board.GetPieceList(PieceType.King, !ogColor)[0].Square;

        // The material advantage
        double matAdv = materialAdvantage(board, ogColor, false);

        // This just makes sure checkmate happens and values checks higher
        if (board.IsInCheckmate())
        {
            evaluation -= 100000;
        }
        else if (board.IsInCheck())
        {
            evaluation -= .3;
        }
        else if (board.IsDraw() && (matAdv < 0))
        {
            evaluation -= 4;
        }
        else if (board.IsDraw() && (matAdv > 3))
        {
            evaluation += 15;
        }
        else if (board.IsDraw() && (matAdv > 0))
        {
            evaluation += 5;
        }

        if (materialAdvantage(board, ogColor, true) > 30)
        {
            evaluation += Math.Abs(opponentKingSquare.Rank - kingSquare.Rank) * 12;
        }

        if (color == ogColor)
        {
            evaluation = -evaluation;
        }

        // END OF COLOR SPECIFIC

        // This gets our basic positioning
        evaluation -= countUpPostitionalAdvantage(board, kingSquare, opponentKingSquare, matAdv);

        // This gets material advantage
        evaluation += matAdv * 7;

        return evaluation;
    }

    private double materialAdvantage(Board board, bool color, bool totalCount)
    {
        PieceList[] listOfAllPieces = board.GetAllPieceLists();

        double myMaterialNum = 0.0;
        double enemyMaterialNum = 0.0;

        // Counts up material advantage
        foreach (PieceList piecelist in listOfAllPieces)
        {
            if (piecelist != null)
            {
                if (piecelist.IsWhitePieceList == color)
                {
                    myMaterialNum += countUpPieces(piecelist);
                }
                else
                {
                    enemyMaterialNum += countUpPieces(piecelist);
                }
            }
        }

        // Just provides a double use for the function
        if (totalCount)
        {
            return (myMaterialNum + enemyMaterialNum);
        }

        // Returns positive if we have a material advantage, negative otherwise
        return (myMaterialNum - enemyMaterialNum);
    }

    // Evaluates the amount of material advantage each piece gives
    private double countUpPieces(PieceList myPieceList)
    {
        double retTotalPieceNum = 0.0;

        foreach (Piece piece in myPieceList)
        {
            retTotalPieceNum += getPieceValue(piece.PieceType, piece.Square, piece.IsWhite);
        }

        return retTotalPieceNum;
    }

    // This calculated absolute distance to king
    private double countUpPostitionalAdvantage(Board board, Square kingSquare, Square opponentKingSquare, double matAdv)
    {
        double moveVal = 0.0;
        int count = 0;
        double weight = 1;

        if (matAdv > .25)
        {
            weight = 3;
        }
        else if (matAdv < -.25)
        {
            weight = .3;
        }

        // Positioning!
        foreach (PieceList piecelist in board.GetAllPieceLists())
        {
            if (piecelist != null)
            {
                foreach (Piece piece in piecelist)
                {
                    if (piecelist.IsWhitePieceList == ogColor)
                    {
                        double tempMoveVal = 0;
                        tempMoveVal += (Math.Abs(piece.Square.File - kingSquare.File) + Math.Abs(piece.Square.Rank - kingSquare.Rank)) * weight;
                        tempMoveVal += Math.Abs(piece.Square.File - opponentKingSquare.File) + Math.Abs(piece.Square.Rank - opponentKingSquare.Rank);
                        moveVal += tempMoveVal;

                        count++;
                    }
                }
            }
        }

        return moveVal / (count);
    }

    // Gets values of the different pieces
    private double getPieceValue(PieceType pieceType, Square square, bool color)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                // Increases value of pawn as it gets closer to promotion
                double pawnIncrease = square.Rank / 100.0;
                if (color == false) // Not White
                {
                    pawnIncrease = -pawnIncrease + .08;
                }

                return 1 + pawnIncrease;
            case PieceType.Bishop:
                return 3.2;
            case PieceType.Knight:
                return 3.0;
            case PieceType.Rook:
                return 5.0;
            case PieceType.Queen:
                return 9.0;
            default: return 0;

        }
    }
}