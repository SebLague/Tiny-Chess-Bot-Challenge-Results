namespace auto_Bot_501;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_501 : IChessBot
{
    Random rng = new(0);

    private struct SearchUnit
    {
        public Move Move { get; set; }
        public int Score { get; set; }
    }

    public Move Think(Board board, Timer timer)
    {
        GameStatus gs = new(board);

        DateTime startTime = DateTime.UtcNow;
        int msThreshold = Math.Min(timer.GameStartTimeMilliseconds / 200 + timer.IncrementMilliseconds / 10, timer.MillisecondsRemaining / 30);

        // iterative deepening
        for (int depth = 3; ; depth++)
        {
            var bestMove = AlphaBetaNegaMax(gs, depth, int.MinValue + 10, int.MaxValue - 10, true);

            var elapsedMilliseconds = (DateTime.UtcNow - startTime).TotalMilliseconds;

            if (elapsedMilliseconds > msThreshold || bestMove.Score > 9999999 || depth >= 100)
            {
                return bestMove.Move;
            }
        }
    }

    private void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    private Move[] GetOrderedMoves(Board board)
    {
        var moves = board.GetLegalMoves();

        Shuffle(moves);

        // capture / promotion first

        var movesWithPriority = moves.Where(m => m.IsCapture || m.IsPromotion);
        var movesWithoutPriority = moves.Where(m => !(m.IsCapture || m.IsPromotion));

        var res = movesWithPriority.Concat(movesWithoutPriority).ToArray();

        return res;
    }

    // main search function
    private SearchUnit AlphaBetaNegaMax(GameStatus gs, int depth, int alpha, int beta, bool root)
    {
        var board = gs.Board;

        var moves = GetOrderedMoves(board);

        if (moves.Length == 0)
        {
            var score = board.IsInCheck() ? int.MinValue + 10000 : 0;
            return new SearchUnit { Score = score };
        }

        if (board.IsInsufficientMaterial() || board.FiftyMoveCounter >= 100 || (board.IsRepeatedPosition() && !root))
        {
            return new SearchUnit { Score = 0 };
        }

        if (depth == 0)
        {
            return new SearchUnit { Score = QuiescenceNegaMaxInf(gs, alpha, beta) };
        }

        Move bestMove = new();
        var bestScore = int.MinValue + 10;

        foreach (var move in moves)
        {
            gs.MakeMove(move);

            var result = -AlphaBetaNegaMax(gs, depth - 1, -beta, -alpha, false).Score;

            if (result > 9999999) result -= 1;
            else if (result < -9999999) result += 1;

            gs.UndoMove(move);

            if (result > bestScore)
            {
                bestScore = result;
                bestMove = move;
            }

            alpha = Math.Max(alpha, bestScore);

            if (alpha >= beta)
            {
                break;
            }
        }

        return new SearchUnit { Score = bestScore, Move = bestMove };
    }

    // quiescence search - until quiet
    private int QuiescenceNegaMaxInf(GameStatus gs, int alpha, int beta)
    {
        var eval = gs.Score;

        alpha = Math.Max(alpha, eval);
        if (alpha >= beta)
        {
            return eval;
        }

        var moves = gs.Board.GetLegalMoves(true).OrderByDescending(m => m.CapturePieceType);

        foreach (var move in moves)
        {
            gs.MakeMove(move);

            var result = -QuiescenceNegaMaxInf(gs, -beta, -alpha);

            gs.UndoMove(move);

            eval = Math.Max(eval, result);

            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
            {
                break;
            }
        }

        return eval;
    }
}

class Evaluate
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    static int[] pieceValues = { 0, 100, 280, 320, 479, 929, 0 };

    // to save tokens, zipped 8 values into 64bit
    // value from sunfish engine
    static ulong[][] PieceSquareZippedValues =
    {
        Enumerable.Repeat(0UL, 8).ToArray(),
        new ulong[]
        {
            0x0000000000000000,
            0x4E5356496652555A,
            0x071D152C281F2C07,
            0xEF10FE0F0E000FF3,
            0xE6030A09060100E9,
            0xEA0905F5F6FE03ED,
            0xE108F9DBDCF203E1,
            0x0000000000000000
        },
        new ulong[]
        {
            0xBECBB5B5F6C9C6BA,
            0xFDFA64DC043EFCF2,
            0x0A43014A491B3EFE,
            0x18182D2521291911,
            0xFF051F1516230200,
            0xEE0A0D16120F0BF2,
            0xE9F102000200E9EC,
            0xB6E9E6E8EDDDEABB
        },
        new ulong[]
        {
            0xC5B2AEB4E995DBCE,
            0xF51423D6D91F02EA,
            0xF727E02934F61CF2,
            0x191114221A190F0A,
            0x0D0A111711100007,
            0x0E19180F0819140F,
            0x13140B0607061410,
            0xF902F1F4F2F1F6F6
        },
        new ulong[]
        {
            0x231D210425213832,
            0x371D3843373E223C,
            0x13231C212D1B190F,
            0x0005100D12FCF7FA,
            0xE4DDF0EBF3E3D2E2,
            0xD6E4D6E7E7DDE6D2,
            0xCBDAE1E6E3D5D4CB,
            0xE2E8EE05FEEEE1E0,
        },
        new ulong[]
        {
            0x0601F8984518581A,
            0x0E203CF6144C3918,
            0xFE2B203C483F2B02,
            0x01F016111914F3FA,
            0xF2F1FEFBFFF6ECEA,
            0xE2FAF3F5F0F5F0E5,
            0xDCEE00EDF1F1EBDA,
            0xD9E2E1F3E1DCDED6,
        },
        new ulong[]
        {
            0x04362F9D9D3C53C2,
            0xE00A373838370A03,
            0xC20CC72CBD1C25E1,
            0xC9320BFCED0D00CF,
            0xC9D5CCE4CDD1F8CE,
            0xD1D6D5B1C0E0E3E0,
            0xFC03F2CEC7EE0D04,
            0x111EFDF206FF2812
        }
    };

    // void zipPST()
    // {
    //     for (int k = 0; k < 7; k++)
    //     {
    //         var map = PieceSquareValues[k];
    //         for (int i = 0; i < 8; i++)
    //         {
    //             ulong packed = 0;
    //             for (int j = 0; j < 8; j++)
    //             {
    //                 var val = map[8 * i + j];
    //                 packed = (packed << 8) | (byte)val;
    //             }
    //
    //             for (int j = 0; j < 8; j++)
    //             {
    //                 var t = (sbyte)((packed >> (8 * (7 - j))) & 0xFF);
    //                 if(t != map[8 * i + j])
    //                 {
    //                     throw new Exception();
    //                 }
    //             }
    //         
    //             DivertedConsole.Write(packed.ToString("X16"));
    //         }
    //     }
    // }

    static public int GetPieceValue(PieceType pieceType, Square square, bool isWhitePiece, bool isWhiteToMove)
    {
        var direction = isWhitePiece == isWhiteToMove ? 1 : -1;

        var file = square.File;
        var rank = isWhitePiece ? 7 - square.Rank : square.Rank;

        var packed = PieceSquareZippedValues[(int)pieceType][rank];
        var pieceSquareValue = (sbyte)((packed >> (8 * (7 - file))) & 0xFF);

        return (pieceValues[(int)pieceType] + pieceSquareValue) * direction;
    }

    static public int EvaluateBoard(Board board)
    {
        return board.GetAllPieceLists()
                      .Sum(pieceList => pieceList.Sum(piece =>
                          GetPieceValue(piece.PieceType, piece.Square, pieceList.IsWhitePieceList, board.IsWhiteToMove)));
    }
}

class GameStatus
{
    public Board Board;
    public int Score;

    public GameStatus(Board board)
    {
        Board = board;
        Score = Evaluate.EvaluateBoard(board);
    }

    private int GetScoreDiff(Move move)
    {
        var pieceType = move.MovePieceType;
        var startSquare = move.StartSquare;
        var targetSquare = move.TargetSquare;

        var isWhiteToMove = Board.IsWhiteToMove;

        var ret = 0;

        ret -= Evaluate.GetPieceValue(pieceType, startSquare, isWhiteToMove, isWhiteToMove);
        ret += Evaluate.GetPieceValue(pieceType, targetSquare, isWhiteToMove, isWhiteToMove);

        if (move.IsCastles)
        {
            var isKingsideCastle = move.TargetSquare.File == 6;
            var rookStartSquare = new Square(isKingsideCastle ? 7 : 0, startSquare.Rank);
            var rookTargetSquare = new Square(isKingsideCastle ? 5 : 3, startSquare.Rank);
            ret += GetScoreDiff(new Move(rookStartSquare.Name + rookTargetSquare.Name, Board));
        }

        if (move.IsCapture)
        {
            var captureSquare = move.IsEnPassant ? new Square(targetSquare.File, startSquare.Rank) : targetSquare;
            ret -= Evaluate.GetPieceValue(move.CapturePieceType, captureSquare, !isWhiteToMove, isWhiteToMove);
        }

        if (move.IsPromotion)
        {
            ret += Evaluate.GetPieceValue(move.PromotionPieceType, targetSquare, isWhiteToMove, isWhiteToMove);
        }

        return ret;
    }

    public void MakeMove(Move move)
    {
        Score += GetScoreDiff(move);

        Score = -Score;

        Board.MakeMove(move);
    }

    public void UndoMove(Move move)
    {
        Board.UndoMove(move);

        Score = -Score;

        Score -= GetScoreDiff(move);
    }
}