namespace auto_Bot_337;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_337 : IChessBot
{
    class PosEvalComparer : IComparer<MoveGraph>
    {
        public int Compare(MoveGraph? x, MoveGraph? y)
        {
            if (x == null) return (y == null) ? 0 : 1;
            else if (y == null) return -1;

            MoveGraph a = (MoveGraph)x, b = (MoveGraph)y;

            if (a.PosEval == b.PosEval) return 0;
            int priority = a.PosEval > b.PosEval ? 1 : -1;
            return a.Color > 0 ? priority : -priority;
        }
    }

    class EvalComparer : IComparer<MoveGraph>
    {
        public int Compare(MoveGraph? x, MoveGraph? y)
        {
            if (x == null) return (y == null) ? 0 : 1;
            else if (y == null) return -1;

            MoveGraph a = (MoveGraph)x, b = (MoveGraph)y;

            if (a.Eval == b.Eval) return 0;
            int priority = a.Eval > b.Eval ? 1 : -1;
            return a.Color > 0 ? priority : -priority;
        }
    }

    class MoveGraph
    {
        private Board ChessBoard { get; set; }
        private Move? PreviousMove { get; set; }
        private MoveGraph? Parent { get; set; }
        private PriorityQueue<MoveGraph, MoveGraph> NextMoves { get; set; }
        public double PosEval { get; set; }
        public double EvalChange { get; set; }
        public double Eval { get; set; }
        public int Color { get; set; }
        public int Height { get; set; }

        private MoveGraph? StrongestChild { get; set; }

        public void Search(int depth)
        {
            if (depth > 0)
            {
                if (PreviousMove != null) ChessBoard.MakeMove((Move)PreviousMove);
                NextMoves = new(new PosEvalComparer());
                Span<Move> moves = stackalloc Move[1024];
                ChessBoard.GetLegalMovesNonAlloc(ref moves);

                foreach (var next_move in moves)
                {
                    if (next_move.IsNull) continue;
                    MoveGraph child = new(ChessBoard, this, next_move);
                    NextMoves.Enqueue(child, child);
                }

                PriorityQueue<MoveGraph, MoveGraph> refresh;
                if (depth < 2)
                {
                    refresh = new(new PosEvalComparer());
                }
                else
                {
                    refresh = new(new EvalComparer());
                }

                while (NextMoves.Count > 0)
                {
                    var child = NextMoves.Dequeue();
                    child.Search(depth - 1);
                    refresh.Enqueue(child, child);
                }

                if (refresh.Count > 0)
                {
                    StrongestChild = refresh.Peek();
                    EvalChange = StrongestChild.PosEval - PosEval;
                    if (depth < 2) Eval = EvalChange;
                    else Eval = PosEval + EvalChange + 0.1 * StrongestChild.EvalChange;
                }

                NextMoves = refresh;

                if (PreviousMove != null) ChessBoard.UndoMove((Move)PreviousMove);
            }
        }

        public MoveGraph(Board board, MoveGraph? parent, Move? previous_move)
        {
            ChessBoard = board;
            PreviousMove = previous_move;
            Height = 0;
            if (parent != null)
            {
                Color = -parent.Color;
            }
            else
            {
                Color = board.IsWhiteToMove ? 1 : -1;
            }


            CalcPosEval();
            Eval = PosEval;
        }

        public Move? GetStrongestMove()
        {
            return StrongestChild?.PreviousMove;
        }

        private static readonly double[] PieceValue = { 0.0, 1.0, 3.0, 3.1, 5.0, 8.0, 0.0 };
        private void CalcPosEval()
        {
            if (PreviousMove != null) ChessBoard.MakeMove((Move)PreviousMove);
            if (ChessBoard.IsInCheckmate()) PosEval = ChessBoard.IsWhiteToMove ? double.NegativeInfinity : double.PositiveInfinity;
            else if (ChessBoard.IsDraw()) PosEval = 0;
            else
            {
                double white_value = 0.0, black_value = 0.0;
                int index;
                Square square;
                ulong pieces_bitboard = ChessBoard.AllPiecesBitboard;
                ulong white_pieces_bitboard = ChessBoard.WhitePiecesBitboard;
                ulong black_pieces_bitboard = ChessBoard.BlackPiecesBitboard;
                while (pieces_bitboard > 0)
                {
                    index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces_bitboard);
                    square = new(index);
                    Piece piece = ChessBoard.GetPiece(square);
                    if (piece.IsWhite) white_value += PieceValue[(int)piece.PieceType];
                    else black_value -= PieceValue[(int)piece.PieceType];

                    ulong piece_attack = BitboardHelper.GetPieceAttacks(piece.PieceType, square, ChessBoard, piece.IsWhite);
                    ulong piece_targets = piece_attack & (piece.IsWhite ? black_pieces_bitboard : white_pieces_bitboard);
                    if (piece.IsWhite) white_value += 0.01 * BitboardHelper.GetNumberOfSetBits(piece_targets);
                    else black_value -= 0.01 * BitboardHelper.GetNumberOfSetBits(piece_targets);
                }

                PosEval = white_value + black_value;
            }

            if (PreviousMove != null) ChessBoard.UndoMove((Move)PreviousMove);
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        MoveGraph graph = new MoveGraph(board, null, null);
        int sec = timer.MillisecondsRemaining / 1000, depth = 4;
        if (sec < 10) depth = 2;
        else if (sec < 20) depth = 3;
        else depth = 4;

        graph.Search(depth);
        Move? bot_move = graph.GetStrongestMove();
        if (bot_move == null) DivertedConsole.Write("Bot made null move!");
        return bot_move ?? moves[0];

    }
}