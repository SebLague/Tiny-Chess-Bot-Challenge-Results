namespace auto_Bot_307;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
public class Bot_307 : IChessBot
{
    public double[] pieceVals = { 0, 1, 3, 3.2, 5, 9, 220000 };
    public Position bestTotal = new(0);

    public class Position
    {
        public double Value { get; }
        public bool IsTerminal { get; }
        public List<Move> To { get; }

        public Position(double value, Move? move = null, bool isTerminal = false)
        {
            Value = value;
            To = new List<Move>();
            if (move != null)
                To.Add((Move)move);
            IsTerminal = isTerminal;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        // Time how long it takes to think, then if it is less than a tenth of a second
        // Run it at a higher depth
        int pieces = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            var e = list.GetEnumerator();
            do pieces++;
            while (e.MoveNext());
        }

        bestTotal = new Position(0);

        for (int i = 4; ; i++)
        {
            DivertedConsole.Write("Depth is " + i);
            DateTime start = DateTime.Now;
            Position pos = MinMax(board.IsWhiteToMove, board, i);
            DateTime end = DateTime.Now;
            if ((end - start).TotalMilliseconds >=
            (timer.MillisecondsRemaining - timer.MillisecondsElapsedThisTurn) / pieces / 6 || i == 20)
            {
                return pos.To[0];
            }
            bestTotal = pos;
        }
        throw new("Mov is null");
    }

    public double PiecePriority(Board board, Square sqr, PieceType piece, bool isWhite)
    {
        double value = 0;
        switch (piece)
        {
            case PieceType.Pawn:
                // Rank incentive
                value += (new[] { 0, -2, -1, 6, 8, 10, 12, 0 })[isWhite ? sqr.Rank : 7 - sqr.Rank] / 5 *
                // File incentive
                (new[] { 2, 3, 2, 5, 5, 0, 3, 2 })[sqr.File] / 5;
                break;
            case PieceType.Knight:
                // Development incentive
                value += (new[] { -4, 2, 3, 5, 5, 3, 2, -4 })[sqr.Rank] / 20 +
                // File incentive
                (new[] { -4, 2, 3, 5, 5, 3, 2, -4 })[sqr.Rank] / 20;
                break;
            case PieceType.Rook:
                int y = sqr.Rank;
                for (int dist = 1; dist < 8; dist++)
                {
                    if (y < 0 || y > 7)
                        break;
                    if (board.GetPiece(new Square(sqr.File, y)).IsNull)
                    {
                        value += dist / 5;
                        y += isWhite ? 1 : -1;
                        break;
                    }
                }
                break;
            case PieceType.King:
                value += (new[] { 2, 4, 0, -8, 0, -8, 4, 2 })[sqr.File] / board.PlyCount * 4;
                break;
            case PieceType.Bishop:
                // Development incentive
                value += sqr.Rank == (isWhite ? 0 : 7) ? 0 : 0.2;
                break;
            case PieceType.Queen:
                value += sqr.Rank == (isWhite ? 0 : 7) ? 0.5 : 0;
                break;
        }
        return value;
    }

    public Position MinMax(bool turn, Board board, int depth, int counterDepth = 0, double alpha = int.MinValue, double beta = int.MaxValue)
    {
        if (depth <= counterDepth)
        {
            double white = new Random().NextDouble() / 10000;
            double black = 0;
            foreach (PieceList list in board.GetAllPieceLists())
            {
                var e = list.GetEnumerator();
                do
                    if (list.IsWhitePieceList)
                        white += pieceVals[(int)list.TypeOfPieceInList] +
                            PiecePriority(board, e.Current.Square, list.TypeOfPieceInList, true);
                    else
                        black += pieceVals[(int)list.TypeOfPieceInList] +
                            PiecePriority(board, e.Current.Square, list.TypeOfPieceInList, false);
                while (e.MoveNext());
            }
            // Incomplete evaluation
            return new(white / black);
        }
        // Game over evals
        if (board.IsInCheckmate())
            return new(turn ?
                counterDepth / pieceVals[(int)PieceType.King] : // Black win
                pieceVals[(int)PieceType.King] / counterDepth, null, true); // White win
        if (board.IsDraw())
            return new(1, null, true);

        var moves = board.GetLegalMoves();
        List<Position> movVals = new();
        // Mapping each to a position before sorting to speed it up.
        Array.ForEach(moves, (a) =>
            movVals.Add(new(
                PiecePriority(board, a.TargetSquare, a.MovePieceType, turn) +
                pieceVals[(int)a.CapturePieceType], a))
        );

        // Sort by priority
        movVals.Sort((a, b) => b.Value.CompareTo(a.Value));
        Position best = new(turn ? Double.MinValue : Double.MaxValue);
        int i = 0;
        foreach (Position movVal in movVals)
        {
            i++;
            Move move = movVal.To[0];
            board.MakeMove(move);
            Position pos = MinMax(!turn, board, depth, counterDepth + 1, alpha, beta);
            board.UndoMove(move);
            pos.To.Insert(0, move);

            if (turn)
            {
                if (pos.Value > best.Value)
                    best = pos;

                alpha = Math.Max(alpha, pos.Value);
            }
            else
            {
                if (pos.Value < best.Value)
                    best = pos;

                beta = Math.Min(beta, pos.Value);
            }

            if (alpha >= beta)
                break;
        }
        return best;
    }
}