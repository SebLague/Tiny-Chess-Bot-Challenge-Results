namespace auto_Bot_386;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_386 : IChessBot
{
    private Move _bestMoveRoot = Move.NullMove;

    // Piece values for evaluation
    private readonly int[] _pieceValues = { 0, 100, 330, 360, 500, 1000, 10000 };

    private readonly int[] _piecePhase = { 0, 0, 1, 1, 2, 4, 0 };

    private Move[,] _killerMoves = new Move[64, 2];

    // Piece square table values for evaluation
    private readonly ulong[] _pstValues = {
        657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569,
        366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421,
        366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430,
        402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514,
        329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759,
        291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181,
        402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804,
        347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047,
        347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538,
        384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492,
        347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100,
        366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863,
        419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932,
        329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691,
        383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375,
        329978099633296596, 67159620133902
    };

    // Transposition Table Entry structure
    private struct TtEntry
    {
        public Move Move;
        public int Depth, Score, Bound;

        public TtEntry(Move move, int depth, int score, int bound)
        {
            Move = move;
            Depth = depth;
            Score = score;
            Bound = bound;
        }
    }

    private Dictionary<ulong, TtEntry> _transpositionTable = new Dictionary<ulong, TtEntry>();

    private int GetPieceSquareTableValue(int squareIndex)
    {
        return (int)(((_pstValues[squareIndex / 10] >> (6 * (squareIndex % 10))) & 63) - 20) * 8;
    }

    private int EvaluateBoard(Board board)
    {
        int midgameScore = 0, endgameScore = 0, phase = 0;

        foreach (var isWhite in new[] { true, false })
        {
            for (var pieceType = PieceType.Pawn; pieceType <= PieceType.King; pieceType++)
            {
                int piece = (int)pieceType, index;
                var mask = board.GetPieceBitboard(pieceType, isWhite);
                while (mask != 0)
                {
                    phase += _piecePhase[piece];
                    index = (128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask)) ^ (isWhite ? 56 : 0);

                    int pieceValue = _pieceValues[piece];

                    midgameScore += GetPieceSquareTableValue(index) + pieceValue;
                    endgameScore += GetPieceSquareTableValue(index + 64) + pieceValue;
                }
            }

            midgameScore = -midgameScore;
            endgameScore = -endgameScore;
        }

        return (midgameScore * phase + endgameScore * (24 - phase)) / 24 * (board.IsWhiteToMove ? 1 : -1);
    }

    private int Search(Board board, Timer timer, int alpha, int beta, int depth, int ply)
    {
        var key = board.ZobristKey;
        var isQuiescenceSearch = depth <= 0;
        var bestScore = int.MinValue;

        // Check for repetition (this is much more important than material and 50-move rule draws)
        if (ply > 0 && board.IsRepeatedPosition())
            return 0;

        // Transposition Table lookup
        if (_transpositionTable.TryGetValue(key, out var entry) && entry.Depth >= depth && (entry.Bound == 3 ||
                                                                      entry.Bound == 2 && entry.Score >= beta ||
                                                                      entry.Bound == 1 && entry.Score <= alpha))
            return entry.Score;


        var evaluation = EvaluateBoard(board);

        // Quiescence search is in the same function as negamax to save tokens
        if (isQuiescenceSearch)
        {
            bestScore = evaluation;
            if (bestScore >= beta) return bestScore;
            alpha = Math.Max(alpha, bestScore);
        }

        // Generate moves, only captures in quiescence search
        var moves = board.GetLegalMoves(isQuiescenceSearch);
        var moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i] == _killerMoves[ply, 0])
            {
                // Move the killer move to the front of the list
                (moves[i], moves[0]) = (moves[0], moves[i]);
                break;
            }
        }

        if (_bestMoveRoot != Move.NullMove && !_bestMoveRoot.IsCapture && _killerMoves.Length > 0)
            _killerMoves[ply, 0] = _bestMoveRoot;

        // Score moves
        for (var i = 0; i < moves.Length; i++)
        {
            var move = moves[i];
            // Transposition Table move
            if (move == entry.Move)
                moveScores[i] = 1000000;
            // MVV-LVA (Most Valuable Victim - Least Valuable Attacker)
            else if (move.IsCapture) moveScores[i] = 100 * (int)move.CapturePieceType - (int)move.MovePieceType;
        }

        var bestMove = Move.NullMove;

        // Search moves
        for (var i = 0; i < moves.Length; i++)
        {
            // Incrementally sort moves
            for (var j = i + 1; j < moves.Length; j++)
                if (moveScores[j] > moveScores[i])
                    (moveScores[i], moveScores[j], moves[i], moves[j]) =
                        (moveScores[j], moveScores[i], moves[j], moves[i]);

            var move = moves[i];
            board.MakeMove(move);
            var score = -Search(board, timer, -beta, -alpha, depth - 1, ply + 1);
            board.UndoMove(move);

            // New best move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
                if (ply == 0) _bestMoveRoot = move;

                // Improve alpha
                alpha = Math.Max(alpha, score);

                // Fail-high
                if (alpha >= beta) break;
            }

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30) return evaluation;
            //DivertedConsole.Write(move + ", Score: " + score);
        }

        // (Check/Stale)mate
        if (!isQuiescenceSearch && moves.Length == 0) return board.IsInCheck() ? -30000 + ply : 0;

        // Did we fail high/low or get an exact score?
        var bound = bestScore >= beta ? 2 : bestScore > alpha ? 3 : 1;

        // Push to Transposition Table
        _transpositionTable[key] = new TtEntry(bestMove, depth, bestScore, bound);

        return bestScore;
    }

    public Move Think(Board board, Timer timer)
    {
        //DivertedConsole.Write(board.GetFenString());

        if (board.GetFenString() == "rnbqkbnr/ppp1pppp/8/3p4/8/2N5/PPPPPPPP/R1BQKBNR w KQkq d6 0 2")
        {
            return board.GetLegalMoves()[17];
        }

        if (board.GetFenString() == board.GameStartFenString)
        {
            int n = new Random().Next(14, 18);

            if (n == 17) return board.GetLegalMoves()[6];

            return board.GetLegalMoves()[n];
        }

        // Iterative Deepening
        for (int depth = 1; depth <= 30; depth++)
        {
            Search(board, timer, -50000, 50000, depth, 0);
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining) break;
        }

        DivertedConsole.Write("Best " + _bestMoveRoot);
        return _bestMoveRoot.IsNull ? board.GetLegalMoves()[0] : _bestMoveRoot;
    }
}