namespace auto_Bot_159;
using ChessChallenge.API;
using System;
using System.Linq;
using static ChessChallenge.API.PieceType;
public class Bot_159 : IChessBot
{
    int max = int.MaxValue;
    int min = int.MinValue + 1;
    ulong CenterBitboard;
    ulong BishopBitboard;
    ulong KingBitboard;
    ulong EndgameKingBitboard;
    // (zobrist, eval, ply)
    (ulong, int, int)[] TranspositionTable;
    ulong ttSize = 1 << 20;

    int evalPositions = 0;
    int ttPositions = 0;

    int timeLimit = 1000;

    ChessChallenge.API.Timer? timer;

    Board board;

    public Bot_159()
    {
        CenterBitboard = 60 << 16;
        CenterBitboard |= CenterBitboard << 8 | CenterBitboard << 16 | CenterBitboard << 24;
        BishopBitboard = CenterBitboard | 66 << 8 | 66L << 48;
        KingBitboard = 70 | 70L << 56;
        TranspositionTable = new (ulong, int, int)[ttSize];
    }
    public Move Think(Board b, Timer t)
    {
        timer = t;
        board = b;
        evalPositions = 0;
        ttPositions = 0;

        timeLimit = Math.Max(750, t.MillisecondsRemaining / (b.PlyCount < 40 ? (50 - b.PlyCount) : 10));
        // DivertedConsole.Write($"Time Limit: {timeLimit} ({b.PlyCount} elapsed)"); // #DEBUG

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves);
        Move toPlay = moves[0];
        int ply = 0;

        while (timer.MillisecondsElapsedThisTurn * 4 < timeLimit)
        {
            ply++;
            Move best_move = moves[0];
            int best_eval = min;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                int eval;
                try
                {
                    eval = -REval(move, ply, best_eval, max, false); // Negate because the opponent is the moving player
                }
                catch (TimeoutException)
                {
                    // DivertedConsole.Write($"Ply: {ply} Interrupted evalPositions: {evalPositions}\n"); // #DEBUG
                    return toPlay;
                }
                board.UndoMove(move);

                if (eval > best_eval)
                {
                    best_move = move;
                    best_eval = eval;
                }
            }
            // DivertedConsole.Write($"Ply: {ply} {best_move} Time: {timer.MillisecondsElapsedThisTurn}ms Eval: {best_eval} evalPositions: {evalPositions} ttPositions: {ttPositions}"); // #DEBUG
            toPlay = best_move;
        }
        // DivertedConsole.Write("Gave Up On Evaluation\n"); // #DEBUG
        return toPlay;
    }

    (ulong, int, int) getTransposition(Board board) => TranspositionTable[board.ZobristKey % ttSize];

    // Get position value in pawns for this position (+ favors moving player - favors other player)
    // Recursive evaluation using alpha beta pruning
    // Alpha = my best assured score
    // Beta  = my opponent's best assured score
    int REval(Move move, int recurseDepth, int alpha, int beta, bool onlyCaptures)
    {
        if (board.IsInCheckmate()) return min + 99 - recurseDepth;
        if (board.IsDraw()) return 0;

        if (timer?.MillisecondsElapsedThisTurn > timeLimit) throw new TimeoutException();

        if (recurseDepth <= 0)
        {
            if (move.IsCapture)
                onlyCaptures = true;
            else return Eval();
        }

        // We've already eval'd this position at the same or deeper depth
        if (getTransposition(board).Item3 > recurseDepth && getTransposition(board).Item1 == board.ZobristKey)
        {
            ttPositions++;
            return getTransposition(board).Item2;
        }

        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, onlyCaptures);
        if (moves.Length < 1) return Eval();
        moves.Sort((b, a) => getMoveHeuristic(a).CompareTo(getMoveHeuristic(b))); // Put more promising moves first

        if (onlyCaptures) alpha = Math.Max(alpha, Eval());

        foreach (Move m in moves)
        {
            board.MakeMove(m);
            int eval = -REval(m, recurseDepth - 1, -beta, -alpha, onlyCaptures); // Reverse everything
            board.UndoMove(m);
            if (eval >= beta) return beta;  // Too good to be true - opponent knows a guaranteed path that's better than this
            alpha = Math.Max(alpha, eval); // I can do at least this well, so i update alpha
        }
        TranspositionTable[board.ZobristKey % ttSize] = (board.ZobristKey, alpha, recurseDepth);
        return alpha;
    }

    // For sorting purposes
    int getMoveHeuristic(Move move)
    {
        if (move.IsPromotion) return (int)move.PromotionPieceType * 4;
        if (move.IsCapture) return (int)move.CapturePieceType;
        if ((1UL << move.TargetSquare.Index & CenterBitboard) != 0)
        {
            return 0;
        }
        return -1;
        // return getTransposition(board).Item2;
    }

    // Non recursive evaluation
    int Eval()
    {
        // We've already seen this position
        if (getTransposition(board).Item1 == board.ZobristKey) return getTransposition(board).Item2;

        evalPositions++;

        int eval = OSEval(board.IsWhiteToMove) - OSEval(!board.IsWhiteToMove);
        TranspositionTable[board.ZobristKey % ttSize] = (board.ZobristKey, eval, 0);
        return eval;
    }

    int popcnt(ulong i) => BitboardHelper.GetNumberOfSetBits(i);
    // One Sided evaluation (non recursive)
    int OSEval(bool white)
    {
        ulong pawn, knight, bishop, rook, queen;
        int evaluation = 100 * popcnt(pawn = board.GetPieceBitboard(Pawn, white))
                       + 310 * popcnt(knight = board.GetPieceBitboard(Knight, white))
                       + 320 * popcnt(bishop = board.GetPieceBitboard(Bishop, white))
                       + 500 * popcnt(rook = board.GetPieceBitboard(Rook, white))
                       + 900 * popcnt(queen = board.GetPieceBitboard(Queen, white))

                       + 40 * popcnt((knight | queen) & CenterBitboard)
                       + 40 * popcnt(bishop & BishopBitboard)
                       + 40 * popcnt(board.GetPieceBitboard(King, white) & KingBitboard)
                       + 10 * popcnt(pawn & CenterBitboard)
                       + (board.HasKingsideCastleRight(white) ? 5 : 0) + (board.HasQueensideCastleRight(white) ? 5 : 0);

        foreach (PieceList pl in board.GetAllPieceLists().Where(pl => pl.IsWhitePieceList == white))
        {
            foreach (Piece p in pl)
            {
                ulong b = BitboardHelper.GetPieceAttacks(p.PieceType, p.Square, board, white);
                evaluation += 10 * popcnt(b) + 5 * popcnt(b | CenterBitboard);
            }
        }
        // int evaluation = 100 * (pawn   = board.GetPieceList(Pawn,    white).Count)
        //    + 310 * (knight = board.GetPieceList(Knight,  white).Count)
        //    + 320 * (bishop = board.GetPieceList(Bishop,  white).Count)
        //    + 500 * (rook   = board.GetPieceList(Rook,    white).Count)
        //    + 900 * (queen  = board.GetPieceList(Queen,   white).Count);

        return evaluation;
    }
}