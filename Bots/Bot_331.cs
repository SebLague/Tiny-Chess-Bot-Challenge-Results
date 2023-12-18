namespace auto_Bot_331;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

// Iterative deepening
public class Bot_331 : IChessBot
{
    bool White;
    Dictionary<ulong, int> TranspositionTable;
    int maxDepth;
    int curDepth;
    Timer t;
    readonly Random rng = new();
    public Move Think(Board board, Timer timer)
    {
        t = timer;
        TranspositionTable = new();
        White = board.IsWhiteToMove;

        Move BestMove;

        BestMove = IterativeDeepeningSearch(board);

        TranspositionTable.Clear();
        TranspositionTable.TrimExcess();
        return BestMove;
    }

    //Iteratively search for the best move until it runs out of time for the move
    Move IterativeDeepeningSearch(Board board)
    {
        maxDepth = 2;
        KeyValuePair<Move, int> BestMove = default; ;
        while (StillInTime())
        {
            curDepth = 0;
            if (White)
                BestMove = Max_fct(board, -10000000, 10000000, 0);
            else
                BestMove = Min_fct(board, -10000000, 10000000, 0);

            if (Math.Abs(BestMove.Value) == 10000000)
                break;
            maxDepth += 2;
        }
        if (BestMove.Key.IsNull)
        {
            DivertedConsole.Write("oups");

            dynamic allMoves = board.GetLegalMoves();
            return allMoves[rng.Next(allMoves.Length)];
        }
        DivertedConsole.Write("Depth reach " + maxDepth);
        DivertedConsole.Write("cur eval" + GetBoardEval(board));
        DivertedConsole.Write("eval" + BestMove.Value);

        return BestMove.Key;
    }

    bool StillInTime()
    {
        return t.MillisecondsElapsedThisTurn < t.MillisecondsRemaining / 1000;
    }

    KeyValuePair<Move, int> Min_fct(Board board, int alpha, int beta, int depth)
    {
        depth++;
        if (EndBranchSearch(board, depth))
        {
            return new KeyValuePair<Move, int>(default, GetBoardEval(board));
        }
        Move BestMove = default;
        int MinEval = 10000000;

        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            dynamic res = Max_fct(board, alpha, beta, depth);
            board.UndoMove(move);

            if (res.Value < MinEval)
            {
                MinEval = res.Value;
                BestMove = move;
            }

            beta = Math.Min(beta, MinEval);
            if (res.Key == null)
                break;

            if (beta <= alpha)
                break;
        }
        return new KeyValuePair<Move, int>(BestMove, MinEval);
    }
    KeyValuePair<Move, int> Max_fct(Board board, int alpha, int beta, int depth)
    {
        depth++;
        if (EndBranchSearch(board, depth))
        {
            return new KeyValuePair<Move, int>(default, GetBoardEval(board));
        }
        Move bestMove = default;
        int MaxEval = -10000000;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            dynamic res = Min_fct(board, alpha, beta, depth);
            board.UndoMove(move);

            if (res.Value > MaxEval)
            {
                MaxEval = res.Value;
                bestMove = move;
            }

            alpha = Math.Max(alpha, MaxEval);
            if (res.Key == null)
                break;

            if (beta <= alpha)
                break;
        }
        return new KeyValuePair<Move, int>(bestMove, MaxEval); ;
    }

    //Return the board evaluation
    int CalculateBoardEval(Board board)
    {
        if (board.IsDraw())
            return 0;

        if (TranspositionTable.TryGetValue(board.ZobristKey, out int eval))
            return eval;

        eval = 0;
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                return -10000000;
            return 10000000;
        }
        foreach (PieceList p in board.GetAllPieceLists())
        {
            if (p.TypeOfPieceInList == PieceType.Pawn)
                eval += PawnPositionEval(p);
            eval += GetPieceEvalFromPieceList(p) * p.Count * (p.IsWhitePieceList ? 1 : -1);
        }

        eval += GetPieceEvalFromActivity(board);

        return eval;
    }

    //Returns the board eval and saves it in the transposition table
    int GetBoardEval(Board board)
    {
        int eval = CalculateBoardEval(board);
        if (!TranspositionTable.TryAdd(board.ZobristKey, eval))
            TranspositionTable[board.ZobristKey] = eval;

        return eval;
    }

    //The further a pawn is on the board, the more it's worth
    int PawnPositionEval(PieceList p)
    {
        int eval = 0;
        foreach (Piece pawn in p)
        {
            if (p.IsWhitePieceList)
                eval += Math.Max(0, pawn.Square.Rank - 1) * 5;
            else
                eval -= Math.Max(0, pawn.Square.Rank * -1 + 6) * 5;
        }
        return eval;
    }

    bool EndBranchSearch(Board board, int curDepth)
    {
        return board.IsDraw() || board.IsInCheckmate() || curDepth > maxDepth;
    }

    int GetPieceEvalFromPieceList(PieceList p)
    {
        switch (p.TypeOfPieceInList)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 300;
            case PieceType.Bishop:
                return 300;
            case PieceType.Rook:
                return 500;
            case PieceType.Queen:
                return 1000;
            default: return 0;
        }
    }

    //The more move a player has, the better the eval
    int GetPieceEvalFromActivity(Board board)
    {
        int eval = board.GetLegalMoves().Length * (board.IsWhiteToMove ? 1 : -1) * 3;
        if (board.TrySkipTurn())
        {
            eval += board.GetLegalMoves().Length * (board.IsWhiteToMove ? 1 : -1) * 3;
            board.UndoSkipTurn();
        }
        return eval;
    }
}