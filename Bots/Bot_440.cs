namespace auto_Bot_440;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_440 : IChessBot
{
    static readonly float[] pieceValues = new float[] { 0, 100, 320, 330, 500, 900, 20000 };
    const float MIN = float.MinValue / 8 + 1;
    readonly float[,,] orderTable = new float[600, 64, 64];

    public Move Think(Board board, Timer timer)
    {
        float eval = Math.Min(0, Eval(board, board.IsWhiteToMove));
        var moves = GetMoves(board);
        (Move, float) bestMove = (moves.First(), float.NegativeInfinity);
        foreach (var move in moves)
        {
            board.MakeMove(move);
            float score = -Search(board, board.IsWhiteToMove);
            board.UndoMove(move);
            if (score > bestMove.Item2)
                bestMove = (move, score);

            if (timer.MillisecondsElapsedThisTurn > timer.GameStartTimeMilliseconds / 100 && score > eval) break;
        }
        return bestMove.Item1;
    }

    float Search(Board board, bool player, int depth = 4, float alpha = float.MinValue, float beta = float.MaxValue)
    {
        if (board.IsInCheckmate()) return MIN * depth;
        else if (board.IsDraw()) return 0;
        else if (depth <= 0) return Eval(board, player) - Eval(board, !player);

        var moves = GetMoves(board);

        foreach (var move in moves)
        {
            board.MakeMove(move);
            alpha = Math.Max(alpha, -Search(board, !player, depth - 1 + (board.IsInCheck() ? 1 : 0), -beta, -alpha));
            board.UndoMove(move);
            int
                ply = board.PlyCount,
                from = move.StartSquare.Index,
                to = move.TargetSquare.Index;
            orderTable[ply, from, to] += alpha >= beta ? 10 : 1;
            if (alpha >= beta) break;
        }

        return alpha;
    }

    IEnumerable<Move> GetMoves(Board board) => board.GetLegalMoves().OrderByDescending(move => orderTable[board.PlyCount, move.StartSquare.Index, move.TargetSquare.Index] / 100 + pieceValues[(int)move.CapturePieceType]);

    static float Eval(Board board, bool player)
    {
        ulong
            leftShiftMask = 0xfefefefefefefefe,
            rightShiftMask = 0x7f7f7f7f7f7f7f7f,
            leftPawnShieldMask = 0x00e000000000e000,
            rightPawnShieldMask = 0x0007000000000700,
            fileMask = 0x0101010101010101,
            pawns = board.GetPieceBitboard(PieceType.Pawn, player),
            oppPawns = board.GetPieceBitboard(PieceType.Pawn, !player),
            queen = board.GetPieceBitboard(PieceType.Queen, player),
            bishops = board.GetPieceBitboard(PieceType.Bishop, player),
            knights = board.GetPieceBitboard(PieceType.Knight, player),
            backMask = player ? 0xff : 0xff00000000000000,
            king = board.GetPieceBitboard(PieceType.King, player),
            rooks = board.GetPieceBitboard(PieceType.Rook, player);

        float
            development = 100 - BitboardHelper.GetNumberOfSetBits((knights | bishops) & backMask) * 25,
            push = 0,
            isolated = 0,
            stacked = 0,
            threat = 0,
            safety = 0,
            material;

        /*
            threat on open files
            stacked pawns
            passing pawns
            development of pawns
            storing counts for calculating isolated pawns
        */
        int[] isolations = new int[10]; // with padding
        for (int i = 1; i < 9; i++)
        {
            ulong
                file = fileMask << (8 - i),
                pass = file | ((file << 1) & leftShiftMask) | ((file >> 1) & rightShiftMask),
                tempPawns = pawns & file;
            int
                y = player ? i - 1 : 64 - i,
                count = BitboardHelper.GetNumberOfSetBits(pawns & file),
                oppCount = BitboardHelper.GetNumberOfSetBits(oppPawns & file);
            while (tempPawns > 0)
            {
                int k = BitboardHelper.ClearAndGetIndexOfLSB(ref tempPawns);
                y = player ? Math.Max(y, k) : Math.Min(y, k);
            }
            y /= 8;
            pass = player ? pass << ((y + 1) * 8) : pass >> ((8 - y) * 8);
            if ((oppPawns & pass) == 0) push += player ? y : 7 - y;
            development += player ? y : 7 - y;
            stacked += count > 1 ? 0 : 1;
            isolations[i] = count;
            threat += BitboardHelper.GetNumberOfSetBits((rooks | queen) & file) > 0 && count == 0 && oppCount == 0 ? 1 : 0;
        }

        /*
            isolated pawns
            chained pawns
        */
        for (int i = 1; i < 9; i++) isolated += isolations[i - 1] + isolations[i + 1] == 0 && isolations[i] != 0 ? 0 : 1;

        // material
        material =
            board.GetPieceList(PieceType.Pawn, player).Count * 100 +
            board.GetPieceList(PieceType.Knight, player).Count * 320 +
            board.GetPieceList(PieceType.Bishop, player).Count * 330 +
            board.GetPieceList(PieceType.Rook, player).Count * 500 +
            board.GetPieceList(PieceType.Queen, player).Count * 900;

        // king safety
        if (!board.HasKingsideCastleRight(player) && !board.HasQueensideCastleRight(player) && (king & backMask) > 0 && (rooks & backMask) > 0)
        {
            int
                i = board.GetKingSquare(player).Index,
                j = i,
                k = i,
                count = 0,
                t;
            ulong tempRooks = rooks & backMask;
            while (tempRooks > 0)
            {
                count++;
                t = BitboardHelper.ClearAndGetIndexOfLSB(ref tempRooks);
                j = Math.Min(j, t);
                k = Math.Max(k, t);
            }
            safety = (i == j || i == k) && j != k && count == 2 ? 33 : 0;
            safety *=
                Convert.ToInt32(i == k) * BitboardHelper.GetNumberOfSetBits((player ? (leftPawnShieldMask >> 40) : (leftPawnShieldMask << 40)) & pawns) +
                Convert.ToInt32(i == j) * BitboardHelper.GetNumberOfSetBits((player ? (rightPawnShieldMask >> 40) : (rightPawnShieldMask << 40)) & pawns);
        }

        return
            material +
            2 * safety +
            development +
            11 * threat +
            3 * push +
            stacked +
            2 * isolated;
    }
}