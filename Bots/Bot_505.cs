namespace auto_Bot_505;
using ChessChallenge.API;
using System;
using System.Collections;

public class Bot_505 : IChessBot // Terminus
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 100 };

    public enum Team { White, Black };
    public enum PositionFlag { Exact, Lower, Upper };

    public class PositionEntry
    {
        public PositionEntry(int depth, int evaluation, PositionFlag flag)
        {
            this.depth = depth;
            this.evaluation = evaluation;
            this.flag = flag;
        }

        public int depth;
        public int evaluation;
        public PositionFlag flag;
    };

    public Hashtable transpositionTable = new Hashtable();

    // Transposition functions
    public void StoreTransposition(Board board, int depth, int value, PositionFlag flag)
    {
        transpositionTable[board.ZobristKey] = new PositionEntry(depth, value, flag);
    }

    // Piece functions
    public bool IsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheckmate = board.IsInCheckmate();
        board.UndoMove(move);
        return isCheckmate;
    }

    public int GetPieceValue(Piece piece) => pieceValues[(int)piece.PieceType];

    // Evaluation functions
    public int EvaluateMaterial(Board board, Team currentTeam)
    {
        int materialValue = 0;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            bool myList = (pieceList.IsWhitePieceList && currentTeam == Team.White) || (!pieceList.IsWhitePieceList && currentTeam == Team.Black);

            foreach (Piece piece in pieceList)
            {
                int pieceValue = GetPieceValue(piece);
                materialValue += myList ? pieceValue : -pieceValue;
            }
        }

        return materialValue;
    }

    public int Evaluate(Board board, Team currentTeam)
    {
        int evaluation = 0;

        // Checkmate evaluation
        if (board.IsInCheckmate())
            evaluation += (currentTeam == Team.White && board.IsWhiteToMove) || (currentTeam == Team.Black && !board.IsWhiteToMove) ? -9999 : 9999;

        // Other evaluation
        evaluation += EvaluateMaterial(board, currentTeam);

        return evaluation;
    }

    // Search functions
    public int Search(Board board, int alpha, int beta, int depth, bool max, Team currentTeam)
    {
        if (depth == 0)
            return Evaluate(board, currentTeam);

        Move[] moves = board.GetLegalMoves();

        // Transposition lookup
        PositionEntry positionEntry = (PositionEntry)transpositionTable[board.ZobristKey];

        if (positionEntry != null && positionEntry.depth >= depth)
        {
            if (positionEntry.flag == PositionFlag.Exact)
                return positionEntry.evaluation;
            else if (positionEntry.flag == PositionFlag.Lower)
                alpha = Math.Max(alpha, positionEntry.evaluation);
            else if (positionEntry.flag == PositionFlag.Upper)
                beta = Math.Min(beta, positionEntry.evaluation);

            if (alpha >= beta)
                return positionEntry.evaluation;
        }

        // Standard search
        bool myTurn = (board.IsWhiteToMove && currentTeam == Team.White) || (!board.IsWhiteToMove && currentTeam == Team.Black);
        int bestValue = max ? int.MinValue : int.MaxValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int value = Search(board, alpha, beta, depth - 1, max ? false : true, currentTeam);
            board.UndoMove(move);

            if (max)
            {
                bestValue = Math.Max(bestValue, value);
                alpha = Math.Max(alpha, bestValue);
            }
            else
            {
                bestValue = Math.Min(bestValue, value);
                beta = Math.Min(beta, bestValue);
            }

            if (beta <= alpha)
                break;
        }

        // Store transposition value
        if (bestValue <= alpha)
            StoreTransposition(board, depth, bestValue, PositionFlag.Upper);
        else if (bestValue >= beta)
            StoreTransposition(board, depth, bestValue, PositionFlag.Lower);
        else
            StoreTransposition(board, depth, bestValue, PositionFlag.Exact);

        return bestValue;
    }

    public Move GetBestMove(Board board, int depth, Team currentTeam)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[Random.Shared.Next(moves.Length)];
        int bestValue = 0;

        foreach (Move move in moves)
        {
            if (IsCheckmate(board, move))
            {
                bestMove = move;
                break;
            }

            board.MakeMove(move);
            int moveValue = Search(board, int.MinValue, int.MaxValue, depth, false, currentTeam);
            board.UndoMove(move);

            if (moveValue > bestValue)
            {
                bestValue = moveValue;
                bestMove = move;
            }
        }

        return bestMove;
    }

    public Move Think(Board board, Timer timer)
    {
        Team currentTeam = board.IsWhiteToMove ? Team.White : Team.Black;
        return GetBestMove(board, 2, currentTeam);
    }
}