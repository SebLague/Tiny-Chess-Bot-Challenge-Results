namespace auto_Bot_302;
using ChessChallenge.API;
using System;
using static System.Double;
using static System.Math;

// Note that I avoid using functions and inline everything to save code tokens, which is a goal of the challenge.
public class Bot_302 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };

    // 15MB * 16 bytes = 240MB, below the 256MB limit, checked via Marshal.SizeOf<Transposition>()
    Transposition[] transpositions = new Transposition[15_000_000];

    struct Transposition
    {
        public ulong zobristKey; // 8 byte: Store zobristKey to avoid hash collisions (not 100% perfect, but probably good enough)
        public ushort bestMoveRawValue; // 2 bytes
        public sbyte flag, depth; // 2 x 1 byte
        public float eval; // 4 bytes
    }

    // See https://en.wikipedia.org/wiki/Alpha%E2%80%93beta_pruning#Heuristic_improvements
    // Lets hope that we never have more than 1000 moves in a game
    Move[] killerMoves = new Move[1000];

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = default;
        double bestMoveEval = 0.0;
        var cancel = false;
        var historyHeuristics = new int[2, 7, 64]; // Resetting is fine, we get new values quite fast

        double minimax(int depth, double alpha, double beta, bool assignBestMove, bool allowNull = true)
        {
            // Check inside the search also for the timer to cancel a search if it took really too long
            if (timer.MillisecondsElapsedThisTurn * 15 > timer.MillisecondsRemaining && !cancel)
                cancel = true;

            if (cancel) return NaN;
            double bestEval = -100000000 - depth;

            if (board.IsDraw()) return 0;

            ref var transposition = ref transpositions[board.ZobristKey % 15_000_000];
            if (!assignBestMove && transposition.depth >= depth && transposition.zobristKey == board.ZobristKey)
            {
                // See https://web.archive.org/web/20071031100051/http://www.brucemo.com/compchess/programming/hashing.htm
                if (transposition.flag == 0) return transposition.eval; // EXACT
                if (transposition.flag == 1 && transposition.eval <= alpha) return alpha; // ALPHA
                if (transposition.flag == 2 && transposition.eval >= beta) return beta; // BETA
            }

            var ply = board.PlyCount;


            ////////////////////////////////////////
            // Start of inlined evaluate function // (inlining reduces tokens and lets us access more than 1 computed value without additional token overhead)
            ////////////////////////////////////////
            var score = 0.0;
            var whitePieceCount = 0;
            var blackPieceCount = 0;

            // Midgame evaluation (but also needed for endgame to find actual mate)
            foreach (var pieceList in board.GetAllPieceLists())
                for (int pieceIndex = 0; pieceIndex < pieceList.Count; pieceIndex++)
                {
                    var piece = pieceList[pieceIndex];
                    var pieceSquare = piece.Square;
                    if (pieceList.IsWhitePieceList) whitePieceCount++;
                    else blackPieceCount++;
                    var attacks = BitboardHelper.GetPieceAttacks(piece.PieceType, pieceSquare, board,
                        pieceList.IsWhitePieceList);

                    score += (pieceValues[(int)piece.PieceType] +
                              // Make pawns move forward
                              (piece.IsPawn ? pieceList.IsWhitePieceList ? pieceSquare.Rank : 7 - pieceSquare.Rank : 0) +
                              // Move pieces to places with much freedom
                              0.5 * BitboardHelper.GetNumberOfSetBits(attacks) +
                              // Make pieces attacking/defending other pieces
                              1.5 * BitboardHelper.GetNumberOfSetBits(attacks & board.AllPiecesBitboard))
                             * (pieceList.IsWhitePieceList ? 1 : -1);
                }

            // Checkmate is of course always best. But a checkmate with a queen-promotion is considered best (because we might have overlooked an escape route that might have been possible with a rook-promotion)
            var whiteBoardMultiplier = board.IsWhiteToMove ? -1 : 1;

            // Add/Subtract plyCount to prefer mate in fewer moves. Multiply by more than any e.g. pawn->queen promotion while taking opponent queen would bring
            if (board.IsInCheckmate()) score += whiteBoardMultiplier * (100000000.0 - ply * 10000);

            // Endgame evaluation: https://www.chessprogramming.org/Mop-up_Evaluation
            if (whitePieceCount < 2 || blackPieceCount < 2)
            {
                // Endgame evaluation: https://www.chessprogramming.org/Mop-up_Evaluation
                var whiteIsLoosing = whitePieceCount < blackPieceCount;
                var loosingKingSquare = board.GetKingSquare(whiteIsLoosing);
                var winningKingSquare = board.GetKingSquare(!whiteIsLoosing);

                var centerDistanceOfLoosingKing =
                    Abs(loosingKingSquare.Rank - 3.5) + Abs(loosingKingSquare.File - 3.5);
                // Scaling factor is trimmed to not make blunders in e.g. "8/8/5k1P/8/5K2/7B/8/8 w - - 1 75" or "8/1K6/6p1/5k2/R3n3/8/8/8 w - - 4 86"
                score += whiteBoardMultiplier * (3 * centerDistanceOfLoosingKing + 14 -
                                                 Abs(loosingKingSquare.Rank - winningKingSquare.Rank) +
                                                 Abs(loosingKingSquare.File - winningKingSquare.File));
            }

            var eval = score * -whiteBoardMultiplier;
            //////////////////////////////////////
            // End of inlined evaluate function //
            //////////////////////////////////////


            if (depth <= -100) return eval; // Don't over-evaluate certain positions (this also avoids underflow of sbyte)

            // Null move pruning (but not in endgame, there we might skip a mate, e.g. on "8/8/5k1P/8/5K2/7B/8/8 w - - 1 75"
            if (depth >= 5 && allowNull && eval >= beta && whitePieceCount > 2 && blackPieceCount > 2 && board.TrySkipTurn())
            {
                // depth - 15 is essentially skipping 3 depth-levels (due to depth - 5 in the other minimax call)
                double nullMoveEval = -minimax(depth - 15, -beta, -beta + 1, false, false);
                board.UndoSkipTurn();
                if (nullMoveEval >= beta) return nullMoveEval;
            }

            if (depth <= 0)
            {
                bestEval = eval;
                if (bestEval >= beta)
                {
                    transposition.zobristKey = board.ZobristKey;
                    transposition.eval = (float)bestEval;
                    transposition.depth = (sbyte)depth;
                    transposition.flag = 0;
                    // We don't set the bestMove here. Keep it the way it was because it might not be bad

                    return bestEval; // eval seems to be quiet, so stop here
                }

                alpha = Max(alpha, bestEval);
            }

            Span<Move> moves = stackalloc Move[256];
            // On e.g. "r7/1b4B1/kp1r1PQP/p3bB2/P2p2R1/5qP1/2R4K/8 w - - 0 58" with depth=5 we make a blunder if we wouldn't test for check here
            board.GetLegalMovesNonAlloc(ref moves, depth <= 0 && !board.IsInCheck());

            // Shortcut for when there is only one move available (only keep it when we have tokens left).
            // If we implement any caching, don't cache this case, because it is not a real evaluation.
            // Evaluation might be therefore pretty wrong
            if (moves.Length == 1 && assignBestMove)
            {
                bestMove = moves[0];
                return NaN;
            }

            // Optimize via ab-pruning: first check moves that are more likely to be good
            Span<int> movePotential = stackalloc int[moves.Length];
            int moveIndex = 0;
            foreach (var move in moves)
                // I saved ~15 tokens by using those hard to read elvis statements
                movePotential[moveIndex++] = -
                    // Check transposition table for previously good moves
                    (transposition.zobristKey == board.ZobristKey && move.RawValue == transposition.bestMoveRawValue ? 2_000_000_000 :
                    // Capture moves
                    move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType :
                    // Killer moves
                    move == killerMoves[ply] ? 900_000 :
                    // History heuristics: value is between 0 and 2M, avg between 1k-100k.
                    // So it can be bigger than killerMoves/captureMoves, but then this move seems really to be better.
                    historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);

            movePotential.Sort(moves);

            foreach (var move in moves)
            {
                board.MakeMove(move);
                // Extension: Getting a check is quite often so unstable, that we need to check 1 more move deep (but not forever, so otherwise reduce by 0.2)
                eval = -minimax(depth - (board.IsInCheck() ? 1 : 5), -beta, -alpha, false);
                board.UndoMove(move);
                if (cancel) return NaN;
                alpha = Max(alpha, eval);

                if (eval > bestEval)
                {
                    bestEval = eval;

                    // Update transposition as early as possible, to let it find on subsequent searches
                    transposition.zobristKey = board.ZobristKey;
                    transposition.eval = (float)bestEval;
                    transposition.depth = (sbyte)depth;
                    transposition.bestMoveRawValue = move.RawValue;
                    transposition.flag = 1;

                    if (assignBestMove)
                        bestMove = move;

                    alpha = Max(alpha, bestEval);
                    if (beta <= alpha)
                    {
                        transposition.flag = 2;
                        // By trial and error I figured out, that checking for promotion/castles/check doesn't help here
                        if (!move.IsCapture)
                        {
                            killerMoves[ply] = move;
                            // Squaring depth doesn't change anything, so use the non-square-variation which has less tokens.
                            // Changing movePieceType to startSquareIndex doesn't change anything, but this way we need less memory.
                            historyHeuristics[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index] += depth + 100;
                        }

                        break;
                    }
                }
            }

            return bestEval;
        }

        // Search via iterative deepening
        var depth = 1;
        // Check here for the timer, so we don't start searching when we have almost time left.
        // Max 25 depth, so we don't end up in a loop on forced checkmate. Also 5*25=125, and sbyte can hold up to 127.
        // Aspiration window (dynamic alpha+beta window)
        var alpha = -1000000000.0;
        var beta = 1000000000.0;
        while (timer.MillisecondsElapsedThisTurn * 100 < timer.MillisecondsRemaining && depth < 25)
        {
            var newEval = minimax(5 * depth, alpha, beta, true);

            if (newEval <= alpha)
                alpha = -1000000000.0;
            else if (newEval >= beta)
                beta = 1000000000.0;
            else
            {
                alpha = newEval - 55;
                beta = newEval + 55;
                depth++;
            }

            // 1 depth is represented as 5*depth, so we can also do smaller depth-steps on critical positions
            if (IsNaN(newEval)) break;
        }

        return bestMove;
    }
}