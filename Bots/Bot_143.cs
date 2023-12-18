namespace auto_Bot_143;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


//Additionally, the following things do not count towards the limit:
//white space,
//new lines,
//comments,
//access modifiers,
//commas,
//and semicolons.
public class Bot_143 : IChessBot
{
    bool areWeWhite;
    public Dictionary<int, Dictionary<int, int>> MoveValueList = new();
    const int drawscore = -32768;
    int eval, bestValue;

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = new();
        bestValue = int.MinValue;
        areWeWhite = board.IsWhiteToMove;
        foreach (Move move in board.GetLegalMoves())
        {
            board.MakeMove(move);
            eval = AlphaBeta(board, 3, bestValue, Int16.MaxValue, false);
            board.UndoMove(move);
            if (eval <= bestValue)//=
                continue;
            bestValue = eval;
            bestMove = move;
        }
        return bestMove;
    }

    Dictionary<ulong, int> memo = new(20000);
    Dictionary<int, HashSet<ulong>> memoList = new();

    int AlphaBeta(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
    {
        if (depth < 1)
        {
            int matchValue = isMaximizingPlayer ? alpha : beta;
            if (memoList.ContainsKey(matchValue) && memoList[matchValue].Contains(board.ZobristKey))
                return matchValue;

            if (!memo.ContainsKey(board.ZobristKey))
            {
                if (memo.Count > 20000)
                {
                    foreach (var keyValuePair in memo)
                    {
                        if (!memoList.ContainsKey(keyValuePair.Value))
                            memoList[keyValuePair.Value] = new();
                        memoList[keyValuePair.Value].Add(keyValuePair.Key);
                    }
                    //Buffer.BlockCopy(memo.Keys.ToArray(),0,zobristKeys,0,20000);
                    memo.Clear();
                }
                memo[board.ZobristKey] = Evaluate(board);// Some function to evaluate the board
            }
            return memo[board.ZobristKey];
        }

        var moves = board.GetLegalMoves();

        if (isMaximizingPlayer)
        {
            int maxEval = drawscore;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = board.IsInCheckmate()
                    ?
                    Int32.MaxValue :
                    board.IsDraw() ? drawscore :
                    AlphaBeta(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);
                eval += moves.Length >> 2;
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break;
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                eval = board.IsInCheckmate()
                    ? -999999 : board.IsDraw()
                    ?
                    drawscore :
                    AlphaBeta(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);
                eval -= moves.Length >> 2;
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break;
            }
            return minEval;
        }
    }

    int Evaluate(Board board)
    {

        Vector<int> fileScores = new(filescore), GetPieceValue = new(pieceValues);

        eval = 0;

        var piecelist = board.GetAllPieceLists();

        foreach (var items in piecelist.Skip(areWeWhite ? 0 : 6).Take(5))//pawns,knights,bishops,rooks,queens
            foreach (var piece in items)
            {
                switch (piece.PieceType)
                {
                    case PieceType.Pawn:
                        eval += 4 << (areWeWhite ? piece.Square.Rank : -(piece.Square.Rank - 7));// * 25;
                        break;
                    case PieceType.Knight:
                        eval += filescore[piece.Square.Rank];
                        break;
                    case PieceType.Bishop:
                    case PieceType.Rook:
                    case PieceType.Queen:
                        break;
                    case PieceType.None:
                    case PieceType.King:
                        continue;
                }
                eval += fileScores[piece.Square.File];
            }

        for (PieceType pieceType = PieceType.Pawn; pieceType <= PieceType.King; ++pieceType)
            eval += GetPieceValue[(int)pieceType] * (CountPieces(board, pieceType, areWeWhite) - CountPieces(board, pieceType, !areWeWhite));
        return eval;
    }

    int CountPieces(Board board, PieceType pieceType, bool isWhite) => board.GetPieceList(pieceType, isWhite).Count;

    //int GetPieceValue(PieceType pieceType) => pieceValues[(int)pieceType];

    int[] pieceValues = { 0, 200, 600, 600, 1000, 1800, 0x7FFF, 0 }, filescore = { 0, 1, 2, 3, 3, 2, 1, 0 };

}
