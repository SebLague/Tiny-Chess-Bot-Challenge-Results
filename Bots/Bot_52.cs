namespace auto_Bot_52;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

struct ValuedMove : IComparable<ValuedMove>
{
    public Move move;
    public long value;

    public int CompareTo(ValuedMove other) => value.CompareTo(other.value);
}

public class Bot_52 : IChessBot
{
    Board board = null!;
    static Dictionary<ulong, long> mem1 = new();
    static Dictionary<ulong, (int, ValuedMove)> mem2 = new();

    public Move Think(Board inBoard, Timer timer)
    {
        if (1000_000 < mem1.Count)
            mem1.Clear();
        if (1000_000 < mem2.Count)
            mem2.Clear();
        board = inBoard;
        var v = Value(5).move;
        return v;
    }

    ValuedMove Value(int depth, long alpha = long.MinValue, long beta = long.MaxValue)
    {
        if (board.IsDraw()) return new();
        if (board.IsInCheckmate())
            return new()
            {
                value = board.IsWhiteToMove ? -500_000 : 500_000,
            };

        (int, ValuedMove) mem2Val;
        if (mem2.TryGetValue(board.ZobristKey, out mem2Val) && depth <= mem2Val.Item1)
            return mem2Val.Item2;

        var vmoves = board.GetLegalMoves().Select(mv => new ValuedMove() { move = mv, value = GetQValue(mv) }).ToArray();
        ValuedMove bestMove = new();
        if (0 < depth)
        {
            Array.Sort<ValuedMove>(vmoves);
            if (board.IsWhiteToMove)
            {
                bestMove.move = vmoves.Last().move;
                bestMove.value = long.MinValue;
                foreach (var vmove in vmoves.Reverse().Take(10))
                {
                    board.MakeMove(vmove.move);
                    var nextVmove = Value(depth - 1, alpha, beta);
                    board.UndoMove(vmove.move);
                    if (bestMove.value < nextVmove.value)
                    {
                        bestMove.move = vmove.move;
                        bestMove.value = nextVmove.value;
                    }
                    if (beta < bestMove.value) break;
                    alpha = Math.Max(alpha, bestMove.value);
                }
            }
            else
            {
                bestMove.move = vmoves.First().move;
                bestMove.value = long.MaxValue;
                foreach (var vmove in vmoves.Take(10))
                {
                    board.MakeMove(vmove.move);
                    var nextVmove = Value(depth - 1, alpha, beta);
                    board.UndoMove(vmove.move);
                    if (nextVmove.value < bestMove.value)
                    {
                        bestMove.move = vmove.move;
                        bestMove.value = nextVmove.value;
                    }
                    if (bestMove.value < alpha) break;
                    beta = Math.Min(beta, bestMove.value);
                }
            }
        }
        else
            bestMove = board.IsWhiteToMove ? vmoves.Max() : vmoves.Min();

        mem2[board.ZobristKey] = (depth, bestMove);
        return bestMove;
    }

    static byte[] valTable = BigInteger.Parse($"{14009480113084354580}{4710989260832664046}{8770812715471071954}{7709213516782997514}{2899345418702097324}{6789784933833740125}{3175239678632615335}{2724087279875727935}{9770529170139685103}{3760575204299892527}{2250220655591732737}{7500230955780411264}{5471323616759146255}{6323958653159957771}{13003120103776892747}{6407956628804974009}{6218995182379074757}0{2154739029502361031}{8616345129328406006}{7394996637703794628}{9691296699332327419}{9293859875100735894}{5514712581703264892}{6865221637541309424}{8126670321339879936}{8254211409230051274}{3878893080430677362}{5758554612688308749}{29792}").ToByteArray(true, true);
    long PieceValue(Piece p)
    {
        var i = p.Square.Index;
        var r = i / 2 & 28;
        i = 32 * (int)p.PieceType | (p.IsWhite ? 28 - r : r) | i & 3 ^ i / 4 % 2 * 3;
        i = (new[] { 0, 100, 320, 330, 500, 900, 20000 })[(int)p.PieceType] + (int)valTable[i] - 128;

        return p.IsWhite ? i : -i;
    }

    long GetQValue(Move mv)
    {
        board.MakeMove(mv);
        if (board.IsDraw())
        {
            board.UndoMove(mv);
            return 0;
        }
        long val = 40 * Convert.ToInt64(mv.IsCastles)
            + 90 * Convert.ToInt64(board.IsInCheck())
            + 500_000 * Convert.ToInt64(board.IsInCheckmate());
        if (board.IsWhiteToMove)
            val = -val;
        long bval;
        if (!mem1.TryGetValue(board.ZobristKey, out bval))
            bval = mem1[board.ZobristKey] = board.GetAllPieceLists().Sum(pl => pl.Sum(PieceValue));
        val += bval;

        board.UndoMove(mv);
        return val;
    }
}
