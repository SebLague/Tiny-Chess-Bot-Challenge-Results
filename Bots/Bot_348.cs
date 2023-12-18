namespace auto_Bot_348;
// SimplerBot
// by Rick Schaffer (rick@simplersystems.com)

using ChessChallenge.API;
using System;
using System.Linq;

using static System.MathF;

public class Bot_348 : IChessBot
{
    struct Position
    {
        public float Value;
        public byte ValueType;
        public byte Depth;
        public ushort Move;
        public int HashCode;
    }

    Position[] TT = new Position[0x1000000];

    public Move Think(Board board, Timer timer)
    {
        void unpack(Array destination, params ulong[] data)
        {
            Buffer.BlockCopy(data, 0, destination, 0, data.Length * 8);
        }

        var (TIME, MVV_LVA, PHASE, PIECE_O, PIECE_E, PSQT_O, PSQT_E, PSQT_SCALE, K, color, maxDepth, think) = (
            new byte[24],
            new byte[7, 8],
            new byte[16],
            new short[12],
            new short[12],
            new sbyte[384],
            new sbyte[384],
            new byte[16],
            new Move[3, 64],
            board.IsWhiteToMove ? 1 : -1,
            0,
            Move.NullMove
        );

        unpack(TIME, 0x1011110F0D0A0601, 0x304040406080B0E, 0x101010101010203);
        unpack(MVV_LVA, 0xFFC8C8C8C8FF00, 0x22504C4C4A3A00, 0x18564839381C00, 0x16544636351A00, 0x125234201E1400, 0xA3214100E0C00);
        unpack(PHASE, 0x100000402010100, 0x40201);
        unpack(PIECE_O, 0x191012901210064, 0xFEDFFF9C000003D0, 0xFC30FE6FFED7);
        unpack(PIECE_E, 0x1D1012B01220064, 0xFEDEFF9C000003A1, 0xFC5FFE2FFED5);
        unpack(PSQT_O, 0x345FA22D468343F4, 0xD4EEEAC6A2A9E800, 0xEFFFF9F3CFC8F719, 0xEA04FE0CEAC40D0F, 0xFA2627380BE2251F, 0x116D7F735328503D, 0x4D7F7F7F74537F7E, 0xF7878D1878C97E3C, 0xD0BDE0D3D3D9ADB8, 0xD9EEFFE5DAE4E6D5, 0xDCFADC080EE0EBC5, 0xDF1B1209ECF7FCDF, 0x17033D082C25F7F4, 0x173B6F634E3D2D15, 0xFAD31F3F5423F3DB, 0xAC14F6423416F9B4, 0xEBFFE3EFEFD203E9, 0x5F609E7EA11EB06, 0xFC14FBEAF30413E9, 0xF101F00B06F802D9, 0x3E80C061608F3F7, 0xFA111F12170F06EC, 0xE3E5F6070301EBE1, 0xC2FEFAFEE8DEF2C0, 0xD300005070603F1, 0x303E191310111301, 0x3140170F101011FE, 0x2D32181012100EFE, 0x2C331811151411FF, 0x48482A2E2827210D, 0x695765645A553C2B, 0x7F7F7F7F7F76664C, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x2F280DE0DB124351, 0x342F12FFFA2A4656, 0x2322080413143D3B, 0x1915130C19172229, 0x13211B261C1D221E, 0xFA1D1F3B3B1B1C32, 0xF1B275168540C25, 0x25F2242130330015);
        unpack(PSQT_E, 0xB02DA6BB181246E1, 0xDCE3BACE9EA8C5C9, 0xDBE6BFC593A2CEC8, 0xE3ECC5C9A5BCD4DD, 0xFE07DBE2C6DEF500, 0x35320B22051C3441, 0x7B7F707F60717F7F, 0xC432AB209DE26889, 0xE9F6F90503F6EFE3, 0xEEF60E100812F5EA, 0x2070C1D1C07FF01, 0x111615231912F6, 0xEE0F0C1B161009EF, 0xEAF0F9020B06F3EB, 0xE1F0ECF5FDF1F3E6, 0xE6DBF3E9EFF0E6DE, 0xEBF9F6FEF9F0EDEA, 0xF5F4FFFD00F6F7EE, 0xF4FD01060007F6FB, 0xF9FA03030F020102, 0xF0FFFA0B0203FFF4, 0xEEF4FDF8FDFAFDEA, 0xEAF8F5F7F4F9F4EE, 0xF5E9F1EEF4EFF1EE, 0x1004100607080D12, 0x2FC0D0C0E0E0C10, 0x3F70709090B0913, 0xFFFA02090D080D11, 0xFF07050B0F1112, 0xF8FF060D090D15, 0xFDFBFAFC060F0F14, 0xF3F8F7FA010210, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0xCDD2EAF0F7F7E4DD, 0xDEEEFF060C0700EA, 0xEF010E1414160BF9, 0xFC0F131516181906, 0x41A22202326230C, 0x1126272023292E0D, 0x9241C1814182F0D, 0xE1F14120F0F1A12);
        unpack(PSQT_SCALE, 0x5C2F5F0057495C2F, 0x5F005749);

        float evaluate(float initiative)
        {
            if (board.IsInCheckmate()) return board.IsWhiteToMove ? -1 : 1;

            var (phase, eval_o, eval_e, i) = (0, initiative, initiative, 0);

            foreach (var pieceList in board.GetAllPieceLists())
            {
                var (count, psqt_scale) = (pieceList.Count, PSQT_SCALE[i] / 128.0f);
                phase += PHASE[i] * count;
                eval_o += PIECE_O[i] * count;
                eval_e += PIECE_E[i] * count;
                for (int j = 0; j < count; j++)
                {
                    var (index, square) = (i * 64, pieceList[j].Square.Index);
                    if (i < 6)
                    {
                        index += square;
                        eval_o += PSQT_O[index] * psqt_scale;
                        eval_e += PSQT_E[index] * psqt_scale;
                    }
                    else
                    {
                        index += (square ^ 56) - 384;
                        eval_o -= PSQT_O[index] * psqt_scale;
                        eval_e -= PSQT_E[index] * psqt_scale;
                    }
                }
                i++;
            }

            if (Abs(eval_e) > 200 && board.IsDraw()) return 0;

            return Tanh((eval_o * phase + eval_e * (24 - phase)) / 24000.0f);
        }

        float qsearch(int side, float alpha, float beta)
        {
            var eval = evaluate(side) * side * color;
            if (eval >= beta) return beta;
            alpha = Max(alpha, eval);

            var moves = board.GetLegalMoves(true);
            Array.Sort(moves.Select(_ => (int)_.MovePieceType - (int)_.CapturePieceType).ToArray(), moves);
            foreach (var move in moves)
            {
                board.MakeMove(move);
                eval = -qsearch(-side, -beta, -alpha);
                board.UndoMove(move);
                if (eval >= beta) return beta;
                alpha = Max(alpha, eval);
            }

            return alpha;
        }

        float search(int side, int depth, float alpha, float beta, bool nullMove = false)
        {
            if (board.IsRepeatedPosition()) return 0;

            if (depth == 0) return qsearch(side, alpha, beta);

            var (initialAlpha, position, hashCode, eval, bestMove) = (alpha, TT[board.ZobristKey & 0xFFFFFF], board.ZobristKey.GetHashCode(), -2.0f, UInt16.MinValue);

            if (think != default && position.HashCode == hashCode && position.Depth >= depth)
            {
                var value = position.Value;
                if (position.ValueType == 3)
                    beta = Min(beta, value);
                else if (position.ValueType == 1)
                    alpha = Max(alpha, value);
                else
                    return value;
                if (alpha >= beta) return value;
            }

            if (!nullMove && depth >= 3 && board.TrySkipTurn())
            {
                var s = -search(-side, depth - (depth > 3 ? 3 : 2), -beta, -beta + 0.001f, true);
                board.UndoSkipTurn();
                if (s >= beta) return beta;
            }

            var moves = board.GetLegalMoves();
            if (moves.Length == 0) return board.IsInCheckmate() ? (board.IsWhiteToMove ? -side : side) * color : 0;
            Array.Sort(moves.Select(_ => _.RawValue == position.Move || _.IsPromotion ? 0 : _ == K[side + 1, depth] ? 100 : MVV_LVA[(int)_.CapturePieceType, (int)_.MovePieceType]).ToArray(), moves);
            foreach (var move in moves)
            {
                board.MakeMove(move);
                var s = -search(-side, depth - 1, -beta, -alpha);
                board.UndoMove(move);
                if (s > eval)
                {
                    eval = s;
                    bestMove = move.RawValue;
                    if (depth == maxDepth) think = move;
                }
                alpha = Max(alpha, eval);
                if (alpha >= beta)
                {
                    if (!move.IsCapture) K[side + 1, depth] = move;
                    break;
                }
            }

            TT[board.ZobristKey & 0xFFFFFF] = new() { Value = eval, ValueType = (byte)(eval <= initialAlpha ? 3 : eval >= beta ? 1 : 2), Depth = (byte)depth, Move = bestMove, HashCode = hashCode };

            return eval;
        }

        while (Abs(search(1, ++maxDepth, -1, 1)) < 1 && maxDepth < 32 && timer.MillisecondsElapsedThisTurn * 3 < Min(timer.GameStartTimeMilliseconds / 480.0f * TIME[(int)Min(board.PlyCount >> 3, 23)] + timer.IncrementMilliseconds, timer.MillisecondsRemaining / 12)) ;

        return think;
    }
}
