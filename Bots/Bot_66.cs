namespace auto_Bot_66;
using ChessChallenge.API;
using System;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class Bot_66 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        TreeNode root = new TreeNode(board);
        EvalResult bestMove = root.Expand(1, 4, false);
        DivertedConsole.Write("Choose move ");
        DivertedConsole.Write(bestMove.move);
        DivertedConsole.Write(" widh eval ");
        DivertedConsole.Write(bestMove.eval);
        return bestMove.move;
    }
    public static int EvaluateBoard(Board board)
    {
        if (board.IsDraw())
        {
            return 0;
        }
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? -10000000 : 10000000;
        }
        int v = 0;
        v += SumPieces(board, PieceType.Rook) * 50;
        v += SumPieces(board, PieceType.Knight) * 30;
        v += SumPieces(board, PieceType.Bishop) * 35;
        v += SumPieces(board, PieceType.King) * 1000000;
        v += SumPieces(board, PieceType.Queen) * 90;
        v += SumPieces(board, PieceType.Pawn) * 10;
        return v;
    }

    private static int SumPieces(Board board, PieceType pieceType)
    {
        return board.GetPieceList(pieceType, true).Count - board.GetPieceList(pieceType, false).Count;
    }
}

class TreeNode
{
    private Board board;

    public TreeNode(Board board)
    {
        this.board = board;
    }

    public EvalResult? Expand(int levels, int continueOnCapture, bool isFirst)
    {
        if (isFirst)
        {
            DivertedConsole.Write("Expandind board -> ");
            DivertedConsole.Write(board.GetFenString());
            DivertedConsole.Write(" with eval ");
            DivertedConsole.Write(Bot_66.EvaluateBoard(board));
        }
        Move[] legalMoves = board.GetLegalMoves();
        EvalResult? bestMove = null;
        foreach (var move in legalMoves)
        {
            if (isFirst)
            {
                DivertedConsole.Write("  Evaluating move ");
                DivertedConsole.Write(move);
                DivertedConsole.Write(" -> ");
            }
            Board newBoard = Board.CreateBoardFromFEN(board.GetFenString());
            newBoard.MakeMove(move);
            TreeNode node = new TreeNode(newBoard);
            int moveEval = 0;
            if (levels == 0)
            {
                var ativoExpand = true;
                if (ativoExpand && move.IsCapture && continueOnCapture > 0 && !newBoard.IsInCheckmate() && !newBoard.IsDraw())
                {
                    EvalResult e = node.Expand(0, continueOnCapture - 1, false);
                    moveEval = e.eval;
                }
                else
                {
                    moveEval = Bot_66.EvaluateBoard(newBoard);
                }
            }
            else
            {
                if (newBoard.IsDraw())
                {
                    moveEval = 0;
                }
                else if (newBoard.IsInCheckmate())
                {
                    moveEval = board.IsWhiteToMove ? 10000000 : -10000000;
                }
                else
                {
                    EvalResult e = node.Expand(levels - 1, continueOnCapture, false);
                    moveEval = e.eval;
                }
            }

            System.Random rng = new Random();

            if (board.IsWhiteToMove)
            {
                if (bestMove == null || moveEval > bestMove.eval || (moveEval == bestMove.eval && rng.Next(0, 1) == 0))
                {
                    bestMove = new EvalResult(move, moveEval);
                }
            }
            else
            {
                if (bestMove == null || moveEval < bestMove.eval || (moveEval == bestMove.eval && rng.Next(0, 1) == 0))
                {
                    bestMove = new EvalResult(move, moveEval);
                }

            }
            if (isFirst)
            {
                DivertedConsole.Write(moveEval);
            }
        }
        return bestMove;
    }
}

class EvalResult
{
    public int eval;
    public Move move;
    public EvalResult(Move move, int eval)
    {
        this.eval = eval;
        this.move = move;
    }
}