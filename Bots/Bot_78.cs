namespace auto_Bot_78;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_78 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };
    Dictionary<ulong, IEnumerable<Move>> cache = new();
    public Move Think(Board board, Timer timer)
    {
        /*if (board.PlyCount <= 5)
        {
            var found = FindOpening(board);
            if (found != Move.NullMove)
            {
                return found;
            }
        }*/
        int maxDepth = 11;
        int time = 420;
        if (timer.MillisecondsRemaining < 12000)
        {
            time = 100;
        }
        if (timer.MillisecondsRemaining < 8000)
        {
            time = 20;
        }
        Tuple<Move, double> best = null;
        for (int i = 2; i < maxDepth - 1; i++)
        {
            if (timer.MillisecondsElapsedThisTurn > time)
            {
                break;
            }
            best = BestOnTurn(board, 0, i + 1, -1000000, 1000000, best != null ? best.Item1 : Move.NullMove);
        }
        DivertedConsole.Write(best.Item2);
        return best.Item1;
    }
    private Tuple<Move, double> BestOnTurn(Board board, int currDepth, int maxDepth, double alpha, double beta, Move firstMove)
    {
        if (currDepth == maxDepth)
            return new Tuple<Move, double>(Move.NullMove, BestOnFinalTurn(board, alpha, beta));
        if (board.IsInCheckmate())
            return new Tuple<Move, double>(Move.NullMove, -1000000 + currDepth);// TODO THIS IS DANGEROUS
        else if (board.IsDraw())
            return new Tuple<Move, double>(Move.NullMove, 0);// TODO THIS IS DANGEROUS
        else
        {
            IEnumerable<Move> moves = GetAndOrderMoves(board);
            Tuple<Move, double> currBestMove = null; // TODO THIS IS DANGEROUS
            if (firstMove != Move.NullMove)
            {
                moves = moves.Except(new List<Move> { firstMove });
                moves = moves.Prepend(firstMove);
            }
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                var bestMoveInFuture = BestOnTurn(board, currDepth + 1, maxDepth, -beta, -alpha, Move.NullMove);
                bestMoveInFuture = new Tuple<Move, double>(move, -bestMoveInFuture.Item2);
                board.UndoMove(move);

                if (bestMoveInFuture.Item2 >= beta)
                    return new Tuple<Move, double>(move, beta);   //  fail hard beta-cutoff
                if (currBestMove == null || bestMoveInFuture.Item2 > currBestMove.Item2)
                {
                    currBestMove = bestMoveInFuture;
                    alpha = Math.Max(alpha, bestMoveInFuture.Item2);
                }

                if (bestMoveInFuture.Item1 == Move.NullMove)// TODO THIS IS DANGEROUS
                    break;

            }
            return currBestMove;
        }
    }

    private double BestOnFinalTurn(Board board, double alpha, double beta)
    {
        var value = CalcValue(board);
        if (value >= beta)
            return beta;
        alpha = Math.Max(alpha, value);

        IEnumerable<Move> quiteMoves = board.GetLegalMoves(true);

        double bestMove = value; // TODO THIS IS DANGEROUS

        foreach (Move move in quiteMoves)
        {
            board.MakeMove(move);
            var moveTuple = -BestOnFinalTurn(board, -beta, -alpha);
            board.UndoMove(move);

            if (moveTuple >= beta)
                return beta;   //  fail hard beta-cutoff
            if (moveTuple > bestMove)
            {
                bestMove = moveTuple;
                if (moveTuple > alpha)
                    alpha = moveTuple;
            }
        }

        return bestMove;
    }

    private IEnumerable<Move> GetAndOrderMoves(Board board, bool captureOnly = false)
    {
        cache.TryGetValue(board.ZobristKey, out IEnumerable<Move> val);
        if (val != null)
            return val;
        var moves = board.GetLegalMoves(captureOnly);

        var newMoves = moves.OrderByDescending(item =>
        {
            if (!item.IsCapture) return -100 - (board.SquareIsAttackedByOpponent(item.TargetSquare) ? 1 : 0);
            return (int)item.CapturePieceType * 2 - (int)item.MovePieceType * 2 - (board.SquareIsAttackedByOpponent(item.TargetSquare) ? 1 : 0);
        });

        cache.Add(board.ZobristKey, newMoves);
        return newMoves;
    }
    private double CalcValue(Board board)
    {
        if (board.IsInCheckmate())
            return -100000;
        else if (board.IsDraw())
            return 0;

        double value = 0;

        foreach (var pieceList in board.GetAllPieceLists())
        {
            if (pieceList != null)
            {
                int side = pieceList.IsWhitePieceList ? 1 : -1;
                value += pieceValues[(int)pieceList.TypeOfPieceInList] * side * pieceList.Count;
                foreach (var piece in pieceList)
                {
                    var bitboard = piece.PieceType switch
                    {
                        PieceType.Pawn => BitboardHelper.GetPawnAttacks(piece.Square, pieceList.IsWhitePieceList),
                        PieceType.Knight => BitboardHelper.GetKnightAttacks(piece.Square),
                        PieceType.Rook => BitboardHelper.GetSliderAttacks(PieceType.Rook, piece.Square, board),
                        PieceType.Bishop => BitboardHelper.GetSliderAttacks(PieceType.Bishop, piece.Square, board),
                        PieceType.Queen => BitboardHelper.GetSliderAttacks(PieceType.Queen, piece.Square, board),
                        PieceType.King => BitboardHelper.GetSliderAttacks(PieceType.Queen, piece.Square, board),
                        _ => (ulong)0
                    };
                    /*if (piece.PieceType != PieceType.King)
                    {
                        value += (BitboardHelper.SquareIsSet(bitboard, new Square(27)) ? 0.2 : 0) * side;
                        value += (BitboardHelper.SquareIsSet(bitboard, new Square(28)) ? 0.2 : 0) * side;
                        value += (BitboardHelper.SquareIsSet(bitboard, new Square(35)) ? 0.2 : 0) * side;
                        value += (BitboardHelper.SquareIsSet(bitboard, new Square(36)) ? 0.2 : 0) * side;
                    }
                    double addedValue = BitboardHelper.GetNumberOfSetBits(bitboard & (pieceList.IsWhitePieceList ? board.BlackPiecesBitboard : board.WhitePiecesBitboard)) * side * 100 / pieceValues[(int)pieceList.TypeOfPieceInList];
                    value += addedValue;
                    if (board.IsWhiteToMove == pieceList.IsWhitePieceList)
                        value -= board.SquareIsAttackedByOpponent(piece.Square) ? 1 : 0;*/
                    value += BitboardHelper.GetNumberOfSetBits(bitboard) * side;
                }
            }
        }
        return value * (board.IsWhiteToMove ? 1 : -1);
    }

    private string DecodeMoveInt(int moveInt)
    {
        return new string(new char[] { (char)(moveInt / 1000000), (char)((moveInt / 10000) % 100), (char)((moveInt / 100) % 100), (char)(moveInt % 100) }).ToLower();
    }
    /*private Move FindOpening(Board board)
    {
        int[][] plays = { new int[] { 68506852, 68556853, 67497052, 71567054, 69506951, 69556954 }, //d2d4
                          new int[] { 69506952, 67556754, 68506852, 68556853, 68526853, 67567053 }, //karokan
                          new int[] { 69506952, 67556754, 66496751, 71567054, 70507052, 68556853 }, //vienna

                          new int[] { 69506952, 69556953, 71497051, 66566754, 70496752, 67566753 } };//e2e4
        foreach (var playChain in plays)
        {
            var newBoard = Board.CreateBoardFromFEN(board.GameStartFenString);
            for (int i = 0; i < playChain.Length; i++)
            {
                var compareFen = newBoard.GetFenString();
                var boardFen = board.GetFenString();
                if (compareFen == boardFen)
                {
                    return new Move(DecodeMoveInt(playChain[i]), board);
                }
                newBoard.MakeMove(new Move(DecodeMoveInt(playChain[i]), newBoard));
            }
        }
        return Move.NullMove;
    }*/
}