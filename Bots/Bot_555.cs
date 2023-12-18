namespace auto_Bot_555;
// TODO: Add "static using" to shrink code

using ChessChallenge.API;
using System;
using System.Linq;

/// <summary>
///     Guppy
/// </summary>
public class Bot_555 : IChessBot
{
    /// <summary>
    ///     Constants used for sectioning off mating evaluations from normal evaluations
    /// </summary>
    private const int WhiteMating = int.MaxValue - 512, BlackMating = -WhiteMating, Mated = int.MaxValue;

    /// <summary>
    ///     Amount of nodes that can fit in 256 Mb (per the rules of the competition), used for initializing the transposition table and
    ///     resolving indexes of zobrist keys
    /// </summary>
    private const int TableSize = 0xFFFFF;

    /// <summary>
    ///     Array used for storing killer move hashcode indexed by current game ply
    ///     This will cause a crash when calculating past 5899 ply
    ///     https://chess.stackexchange.com/questions/4113/longest-chess-game-possible-maximum-moves
    /// </summary>
    private readonly int[] _killers = new int[5899];

    /// <summary>
    ///     Material values for piece types from Pawn to Queen ordered by PieceType ordinal
    /// </summary>
    private readonly int[] _material = { 0, 100, 300, 320, 500, 900, 0 };

    /// <summary>
    ///     Transposition table used for storing search results used in move ordering and avoiding duplicate searches
    /// </summary>
    private readonly Node[] _transpositionTable = new Node[TableSize + 1];

    /// <summary>
    ///     The board position currently being analysed, stored globally to save tokens
    /// </summary>
    private Board _board;

    /// <summary>
    ///     The global depth the search is at for this iteration
    /// </summary>
    private int _gDepth;

    /// <summary>
    ///     The amount of nodes searched
    /// </summary>
    private int _nodes;

    /// <summary>
    ///     Think function called by the framework to evaluate a position and get the bots best move
    /// </summary>
    public Move Think(Board board, Timer timer)
    {
        // Return immediately if only one legal move is available
        var moves = board.GetLegalMoves();
        if (moves.Length <= 1) return moves[0];

        // Setup before starting search
        Array.Clear(_transpositionTable);
        _board = board;
        _gDepth = 0;
        _nodes = 0;
        int eval;

        do
        {
            // Run iterative deepening until time limit is reached or a forced mate is calculated
            eval = Search(++_gDepth, 0, -Mated, Mated);
        } while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 120 &&
                 Math.Abs(eval) <= WhiteMating);

        // Get best move for this position from transposition table
        var best = moves.First(move =>
            move.GetHashCode() == _transpositionTable[board.ZobristKey & TableSize].refutation);
        DivertedConsole.Write(
            $"(Guppy) {best.ToString()[7..11]}: {eval}\t@{_gDepth} ({Math.Round((double)_nodes / timer.MillisecondsElapsedThisTurn * 1000)} nps)");
        return best;
    }

    /// <summary>
    ///     Implementation of the negamax search algorithm used for searching the game tree
    /// </summary>
    /// <param name="depth">Depth to search to</param>
    /// <param name="extensions">The amount of ply this search should be extended by</param>
    /// <param name="alpha">Lowest achievable evaluation</param>
    /// <param name="beta">Highest achievable evaluation</param>
    /// <returns>Evaluation of the best move at the specified depth</returns>
    private int Search(int depth, int extensions, int alpha, int beta)
    {
        _nodes++;

        // Return immediately if the game would end by draw here
        if (_board.IsRepeatedPosition() || _board.IsInsufficientMaterial() || _board.FiftyMoveCounter >= 100)
            return 0;

        // Find transposition table entry
        var hash = _board.ZobristKey;
        ref var node = ref _transpositionTable[hash & TableSize];

        // Return immediately if this position is stored in the table and we searched it at equal or greater depth
        if (node.hash == hash && node.depth >= depth + extensions) return node.eval;

        // Generate all legal moves for this position and return immediately if the game ends here
        var moves = _board.GetLegalMoves();
        if (moves.Length == 0) return _board.IsInCheck() ? -Mated + _gDepth - depth : 0;

        // If we reached the end of our search return the evaluation of the current
        var quiescence = depth <= -extensions && moves.Any(move => move.IsCapture);
        if (depth-- <= -extensions)
        {
            // Evaluate and return immediately if not doing quiescence search
            var eval = Evaluate(moves.Length);
            if (!quiescence) return eval;

            // Alpha-beta pruning for quiescence search
            if (eval >= beta) return beta;
            if (alpha < eval) alpha = eval;
        }

        // Best move from transposition table used for move ordering
        var best = Move.NullMove;
        var refutation = node.refutation;
        var score = int.MinValue;

        // Order moves and recursively search available moves
        // TODO: Implement proper time management
        // TODO: Compare Array.Sort vs OrderByDescending performance
        foreach (var move in moves.OrderByDescending(GetPriority))
        {
            // Skip non-captures during quiescence search
            if (quiescence && !move.IsCapture) continue;
            _board.MakeMove(move);

            // Apply extensions & reductions
            // TODO: Test PV extensions
            // TODO: Tweak max extension limit
            var ext = extensions;
            if (_board.IsInCheck()) ext = Math.Min(++ext, 3);

            // Evaluate move
            var eval = -Search(depth, ext, -beta, -alpha);
            _board.UndoMove(move);

            // Check if this move is better than current move
            if (eval <= score) continue;
            score = eval;
            best = move;

            // Upper bound cutoff
            if (eval >= beta)
            {
                // Update current killer move
                _killers[_board.PlyCount] = move.GetHashCode();
                return beta;
            }

            // Lower bound
            if (eval > alpha) alpha = eval;
        }

        // Update transposition table entry
        // TODO: Test return if quiescence
        if (depth + extensions < node.depth) return score;
        node.hash = hash;
        node.refutation = best.GetHashCode();
        node.depth = depth + extensions;
        return node.eval = score;

        int GetPriority(Move move)
        {
            // TODO: Promotion ordering
            return move.GetHashCode() == refutation ? 1000 :
                _killers[_board.PlyCount] == move.GetHashCode() ? 999 :
                move.IsCapture ? _material[(int)move.CapturePieceType] - _material[(int)move.MovePieceType] :
                (int)move.PromotionPieceType;
        }
    }

    /// <summary>
    ///     Evaluates the current board state from the current players perspective
    /// </summary>
    private int Evaluate(int mobility)
    {
        // Initialize evaluation with a simple material count from the white players perspective
        // TODO: Compare LINQ vs normal for loop performance
        var evaluation = _board.GetAllPieceLists().Sum(list =>
            _material[(int)list.TypeOfPieceInList] *
            (list.IsWhitePieceList ? list.Count : -list.Count));

        // TODO: Bishop & pawn color complexes
        // TODO: Sliding piece scope & target
        // TODO: Space in opponent side control
        // TODO: Isolated pawns
        // TODO: Passed pawns

        // Having more legal moves available is usually better than having fewer
        // TODO: Test decrease evaluation if we are in check
        evaluation += mobility;

        // Clamp evaluation between mating scores, apply 50 move rule decrease and convert to the side to move
        return (int)(Math.Clamp(evaluation, BlackMating, WhiteMating) * (100 - _board.FiftyMoveCounter) /
                     (_board.IsWhiteToMove ? 100D : -100D));
    }

    /// <summary>
    ///     Transposition table node
    /// </summary>
    private struct Node
    {
        public ulong hash;
        public int depth, eval, refutation;
    }
}