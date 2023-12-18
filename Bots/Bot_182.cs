namespace auto_Bot_182;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;

public class Bot_182 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        return board.GetLegalMoves()[FindMinEval(board, 3, 0, -10000000, 10000000)];
    }

    public int FindMinEval(Board board, int depth, int current_depth, int alpha, int beta)
    {
        Move[] moves = board.GetLegalMoves();

        Dictionary<int, int> evaluations = new Dictionary<int, int>();

        //if there is an immediate checkmate
        if (current_depth == 0)
        {
            int j;
            if (CheckmateImmediate(moves, board, out j))
                return j;
        }

        int i = 0;
        foreach (var move in moves)
        {
            board.MakeMove(move);

            //if bot has no legal moves
            if (current_depth != depth)
            {
                evaluations.Add(i, FindMaxEval(board, depth, current_depth + 1, alpha, beta));

                if (evaluations.Last().Value < beta)
                    beta = evaluations.Last().Value;

                if (beta <= alpha)
                {
                    board.UndoMove(move);
                    break;
                }
            }
            else
            {
                evaluations.Add(i, EvaluateBoard(board));

                if (evaluations.Last().Value < beta)
                    beta = evaluations.Last().Value;

                if (beta <= alpha)
                {
                    board.UndoMove(move);
                    break;
                }
            }

            board.UndoMove(move);

            i++;
        }

        //check for a stalemate
        var min = moves.Length == 0 ? new KeyValuePair<int, int>(0, 0) : evaluations.MinBy(kvp => kvp.Value);
        return current_depth == 0 ? min.Key : min.Value;
    }

    public int FindMaxEval(Board board, int depth, int current_depth, int alpha, int beta)
    {
        Move[] moves = board.GetLegalMoves();
        Dictionary<int, int> evaluations = new Dictionary<int, int>();

        //if there is an immediate checkmate
        if (current_depth == 0)
        {
            int j;
            if (CheckmateImmediate(moves, board, out j))
                return j;
        }

        int i = 0;
        foreach (var move in moves)
        {
            board.MakeMove(move);

            //if opponent has no legal moves
            if (current_depth != depth)
            {
                evaluations.Add(i, FindMinEval(board, depth, current_depth + 1, alpha, beta));

                if (evaluations.Last().Value > alpha)
                    alpha = evaluations.Last().Value;

                if (beta <= alpha)
                {
                    board.UndoMove(move);
                    break;
                }
            }
            else
            {
                evaluations.Add(i, EvaluateBoard(board));

                if (evaluations.Last().Value > alpha)
                    alpha = evaluations.Last().Value;

                if (beta <= alpha)
                {
                    board.UndoMove(move);
                    break;
                }
            }

            board.UndoMove(move);

            i++;
        }

        var max_eval = moves.Length == 0 ? new KeyValuePair<int, int>(0, 0) : evaluations.MaxBy(kvp => kvp.Value);

        return current_depth == 0 ? max_eval.Key : max_eval.Value;
    }

    public int EvaluateBoard(Board board)
    {
        int evaluation = 0;

        //get your legal moves
        board.ForceSkipTurn();

        Move[] new_moves = board.GetLegalMoves();
        evaluation += new_moves.Length;

        evaluation += board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).Count * 900;
        evaluation += board.GetPieceList(PieceType.Rook, board.IsWhiteToMove).Count * 500;
        evaluation += board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove).Count * 300;
        evaluation += board.GetPieceList(PieceType.Knight, board.IsWhiteToMove).Count * 300;
        evaluation += board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).Count * 100;

        board.UndoSkipTurn();

        //get opponents legal moves
        Move[] new_moves_opp = board.GetLegalMoves();
        evaluation -= new_moves_opp.Length;

        evaluation -= board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).Count * 900;
        evaluation -= board.GetPieceList(PieceType.Rook, board.IsWhiteToMove).Count * 500;
        evaluation -= board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove).Count * 300;
        evaluation -= board.GetPieceList(PieceType.Knight, board.IsWhiteToMove).Count * 300;
        evaluation -= board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).Count * 100;

        return evaluation;
    }

    public bool CheckmateImmediate(Move[] moves, Board board, out int index)
    {

        //if there is an immediate checkmate
        for (index = 0; index < moves.Length; index++)
        {
            board.MakeMove(moves[index]);
            bool checkmateImmediate = board.IsInCheckmate();
            board.UndoMove(moves[index]);

            if (checkmateImmediate)
                return checkmateImmediate;
        }

        return false;
    }
}