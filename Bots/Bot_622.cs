namespace auto_Bot_622;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_622 : IChessBot
{
    Dictionary<ulong, int> transpositionTable = new();
    Dictionary<ulong, int> moveScoreTable = new();
    const int inf = 30000;
    bool isEndgame = false;
    private readonly int[] whitePawnDesiredPositions = { 0, 0, 0, 0, 0, 0, 0, 0,
                                                10, 10, 10, 0, 0, 10, 10, 10,
                                                0, 5, 0, 11, 11, 0, 5, 0,
                                                0, 0, 0, 21, 21, 0, 0, 0,
                                                5, 5, 5, 25, 25, 5, 5, 5,
                                                20, 20, 20, 30, 30, 20, 20, 20,
                                                40, 40, 40, 40, 40, 40, 40, 40};
    private readonly int[] whitePawnDesiredRank = { 0, 0, 10, 20, 30, 40, 50, 60 };
    Board board;
    public Move Think(Board inputBoard, Timer timer)
    {
        board = inputBoard;
        int depth = 0;
        do
        {
            transpositionTable.Clear();
            depth += 10;
            MiniMax(depth, -inf, inf, false);
            Eval();
        }
        while (timer.MillisecondsElapsedThisTurn * 200 < timer.MillisecondsRemaining && depth < 200);
        System.Span<Move> moves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref moves);
        SortMoves(ref moves);
        //DivertedConsole.Write(moveScoreTable.Count); //# DEBUG  
        if (moveScoreTable.Count > 6000000)
            moveScoreTable.Clear();
        return moves[0];
    }

    /// <summary>
    /// Returns evaluation of the position calculated at specified depth (high value if position is good for the player to move)
    /// Makes use of transpositionTable for improved efficiency and moveScoreTable for sorting the moves (also better efficiency)
    /// </summary>
    public int MiniMax(int depth, int a, int b, bool isLastMoveCapture)
    {
        // Check if node was visited before
        ulong key = board.ZobristKey ^ ((ulong)board.PlyCount << 1) ^ (ulong)(isLastMoveCapture ? 1 : 0) ^ ((ulong)depth << 8) ^ ((ulong)a << 16) ^ ((ulong)b << 24);
        if (transpositionTable.TryGetValue(key, out var value))
            return value;

        // Check if node is final node
        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return -inf - depth;

        int shallowEval = Eval();

        // or if we reached depth limit
        if ((depth <= 0 && !isLastMoveCapture) || depth <= -60)
            return shallowEval;


        //get available moves
        System.Span<Move> allMoves = stackalloc Move[128];
        board.GetLegalMovesNonAlloc(ref allMoves, depth <= 0);

        if (allMoves.Length == 0)
            return shallowEval;

        if (depth >= -50) SortMoves(ref allMoves);

        int bestEval = -inf;
        if (depth <= 0)
        {
            bestEval = shallowEval; // do not go deeper if a player prefers to not capture anything
            if (bestEval > b) // beta pruning
                return bestEval;

            if (bestEval > a) // update alpha (the improvement is probably only minor)
                a = bestEval;
        }
        bool isPV = true;
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);
            int eval;
            if (isPV)
            {
                eval = -MiniMax(depth - (board.IsInCheck() ? 5 : 10), -b, -a, move.IsCapture);
                isPV = false;
            }
            else
            {
                eval = -MiniMax(depth - (board.IsInCheck() ? 5 : 10), -a - 1, -a, move.IsCapture);
                if (eval > a)
                    eval = -MiniMax(depth - (board.IsInCheck() ? 5 : 10), -b, -a, move.IsCapture);
            }
            board.UndoMove(move);

            moveScoreTable[move.RawValue ^ board.ZobristKey] = -eval - 900;
            if (eval > b) // beta pruning
            {
                transpositionTable[key] = eval;
                return eval;
            }
            if (eval > bestEval)
            {
                bestEval = eval;
                if (eval > a) // update alpha
                    a = eval;
            }
        }
        transpositionTable[key] = bestEval;
        return bestEval;
    }

    /// <summary>
    /// Sorts the moves based on move score
    /// </summary>
    public void SortMoves(ref Span<Move> moves)
    {
        System.Span<int> moveOrderKeys = stackalloc int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
            moveOrderKeys[i] = GetMoveScore(moves[i]);
        MemoryExtensions.Sort(moveOrderKeys, moves);
    }

    /// <summary>
    /// Returns evaluation of the move 
    /// Good moves recieve lower values
    /// Best efficiency obtained with iterative deepening
    /// Uses moveScoreTable
    /// </summary>

    public int GetMoveScore(Move move)
    {
        if (moveScoreTable.TryGetValue(move.RawValue ^ board.ZobristKey, out var value))
        {
            return value;
        }
        else
        {
            board.MakeMove(move);
            int score = Eval();
            board.UndoMove(move);
            moveScoreTable[move.RawValue ^ board.ZobristKey] = score;
            return score;
        }
    }

    /// <summary>
    /// Returns evaluation of the position (high value if position is good for the player to move)
    /// </summary>
    public int Eval()
    {
        int white = CountMaterial(board.IsWhiteToMove);
        int black = CountMaterial(!board.IsWhiteToMove);
        isEndgame = white + black < 2750;
        return white - black + GetBonuses(board.IsWhiteToMove) - GetBonuses(!board.IsWhiteToMove);
    }

    public int CountMaterial(bool colour)
    {
        return 100 * board.GetPieceList(PieceType.Pawn, colour).Count
         + 300 * board.GetPieceList(PieceType.Knight, colour).Count
         + 300 * board.GetPieceList(PieceType.Bishop, colour).Count
         + 500 * board.GetPieceList(PieceType.Rook, colour).Count
         + 900 * board.GetPieceList(PieceType.Queen, colour).Count;
    }

    public int GetBonuses(bool colour)
    {
        int eval = 0;
        // bonusess:
        //if (isEndgame && 0 < board.GetKingSquare(colour).Rank && board.GetKingSquare(colour).Rank < 7 && 0 < board.GetKingSquare(colour).File && board.GetKingSquare(colour).File < 7) eval += 20;

        PieceList pawns = board.GetPieceList(PieceType.Pawn, colour);
        for (int i = 0; i < pawns.Count; i++)
        {
            if (isEndgame)
            {
                eval -= DistanceFromKing(pawns.GetPiece(i), colour) * 2;
                int rank = pawns.GetPiece(i).Square.Rank;
                if (!colour)
                    rank = 7 - rank;
                eval += whitePawnDesiredRank[rank];
            }
            else
            {
                int index = pawns.GetPiece(i).Square.Index;
                if (!colour)
                    index = 63 - index;
                eval += whitePawnDesiredPositions[index];
            }
        }
        //other pieces
        eval += GetDistEvalBonus(board.GetPieceList(PieceType.Knight, colour));
        eval += GetDistEvalBonus(board.GetPieceList(PieceType.Bishop, colour));
        eval += GetDistEvalBonus(board.GetPieceList(PieceType.Rook, colour));
        eval += GetDistEvalBonus(board.GetPieceList(PieceType.Queen, colour));
        return eval;
    }

    public int GetDistEvalBonus(PieceList pieceList)
    {
        int bonus = 0;
        for (int i = 0; i < pieceList.Count; i++)
        {
            bonus -= DistanceFromKing(pieceList.GetPiece(i), !pieceList.IsWhitePieceList);
            if (isEndgame)
            {
                bonus -= DistanceFromKing(pieceList.GetPiece(i), pieceList.IsWhitePieceList);
            }
        }
        return bonus;
    }
    public int DistanceFromKing(Piece piece, bool kingColour)
    {
        return Math.Abs(board.GetKingSquare(kingColour).File - piece.Square.File) + Math.Abs(board.GetKingSquare(kingColour).Rank - piece.Square.Rank);
    }
}