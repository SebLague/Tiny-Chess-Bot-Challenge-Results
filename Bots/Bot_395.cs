namespace auto_Bot_395;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using Board = ChessChallenge.API.Board;
using Move = ChessChallenge.API.Move;

public class Bot_395 : IChessBot
{
    private int[] _values =
    {
        0, // None
        80, // Pawn
        300, // Knight
        300, // Bishop
        525, // Rook
        1000, // Queen
        0 // King
    };

    private int[] _gamePhaseIncrement =
    {
        0, // None
        0, // Pawn
        1, // Knight
        1, // Bishop
        2, // Rook
        4, // Queen
        0 // King
    };

    // Dictionary of zobrist keys to evaluation scores (Item1), the depth the position was evaluated to (Item2), and the best move in that position (Item3)
    // Used to avoid re-evaluating the same position multiple times
    private readonly Dictionary<ulong, (int, int, Move)> _transpositionTable = new();

    private Move _bestMove;
    private bool _searchAborted;
    // The time remaining in milliseconds when the search should stop
    private int _timeRemainingToStopSearch;
    private Timer _timer;

    private readonly ulong[] _packedPieceSquareTables =
    {
        0, 17876852006827220035, 17442764802556560892, 17297209133870877174, 17223739749638733806, 17876759457677835758,
        17373217165325565928, 0,
        13255991644549399438, 17583506568768513230, 2175898572549597664, 1084293395314969850, 18090411128601117687,
        17658908863988562672, 17579252489121225964, 17362482624594506424,
        18088114097928799212, 16144322839035775982, 18381760841018841589, 18376121450291332093, 218152002130610684,
        507800692313426432, 78546933140621827, 17502669270662184681,
        2095587983952846102, 2166845185183979026, 804489620259737085, 17508614433633859824, 17295224476492426983,
        16860632592644698081, 14986863555502077410, 17214733645651245043,
        2241981346783428845, 2671522937214723568, 2819295234159408375, 143848006581874414, 18303471111439576826,
        218989722313687542, 143563254730914792, 16063196335921886463,
        649056947958124756, 17070610696300068628, 17370107729330376954, 16714810863637820148, 15990561411808821214,
        17219209584983537398, 362247178929505537, 725340149412010486,
        0, 9255278100611888762, 4123085205260616768, 868073221978132502, 18375526489308136969, 18158510399056250115,
        18086737617269097737, 0,
        13607044546246993624, 15920488544069483503, 16497805833213047536, 17583469180908143348, 17582910611854720244,
        17434276413707386608, 16352837428273869539, 15338966700937764332,
        17362778423591236342, 17797653976892964347, 216178279655209729, 72628283623606014, 18085900871841415932,
        17796820590280441592, 17219225120384218358, 17653536572713270000,
        217588987618658057, 145525853039167752, 18374121343630509317, 143834816923107843, 17941211704168088322,
        17725034519661969661, 18372710631523548412, 17439054852385800698,
        1010791012631515130, 5929838478495476, 436031265213646066, 1812447229878734594, 1160546708477514740,
        218156326927920885, 16926762663678832881, 16497506761183456745,
        17582909434562406605, 580992990974708984, 656996740801498119, 149207104036540411, 17871989841031265780,
        18015818047948390131, 17653269455998023918, 16424899342964550108
    };

    public Move Think(Board board, Timer timer)
    {
        // Average 40 moves per game, so we start targeting 1/40th of the remaining time
        // Will move quicker as the game progresses / less time is remaining
        // Minimum 1/10 of the remaining time, to avoid timing out, divide by zero errors or negative numbers
        _timeRemainingToStopSearch = timer.MillisecondsRemaining - timer.MillisecondsRemaining / Math.Max(40 - board.PlyCount / 2, 10);
        _searchAborted = false;
        _timer = timer;

        int score;
        var searchDepth = 1;
        _bestMove = Move.NullMove;
        do
        {
            // 9999999 and -9999999 are used as "infinity" values for alpha and beta
            score = Search(board, -9999999, 9999999, searchDepth, 0);
        } while (searchDepth++ < 20 && !_searchAborted); // 20 is the max depth

        // This is for debugging purposes only, comment it out so it doesn't use up tokens!
        // The ttMemory calculation is storing 2 ints, so divide by 2 to get bytes, then divide by 1000000 to get MB
        // DivertedConsole.Write($"Current eval: {Evaluate(board)}, Best move score: {score}, Result: {_bestMove}, Depth: {searchDepth}, time remaining: {timer.MillisecondsRemaining}, ttSize: {_transpositionTable.Count}, ttMemory: {(_transpositionTable.Count / 2d) / 1000000d}MB, fen: {board.GetFenString()}");

        return _bestMove.IsNull ? GetOrderedLegalMoves(board, 0)[0] : _bestMove;
    }

    // If depthLeft is 0 (or less), perform a quiescence search
    // https://www.chessprogramming.org/Quiescence_Search
    private int Search(Board board, int alpha, int beta, int depthLeft, int plyFromRoot)
    {
        var qsearch = depthLeft < 0;
        if (_timer.MillisecondsRemaining <= _timeRemainingToStopSearch)
        {
            _searchAborted = true;
        }

        // If the current position has already been evaluated to at least the same depth, return the stored value
        if (_transpositionTable.TryGetValue(board.ZobristKey, out var value) && value.Item2 >= depthLeft)
        {
            if (plyFromRoot == 0)
                // Get the saved best move in this position
                _bestMove = value.Item3;
            else
                // Return the saved evaluation score
                return value.Item1;
        }

        // Repetition is a draw, so return 0
        if (board.IsRepeatedPosition())
            return 0;

        // Get the legal moves, if it's a quintescence search only get captures
        var legalMoves = GetOrderedLegalMoves(board, plyFromRoot, qsearch);
        if (!qsearch && legalMoves.Length == 0)
        {
            // No legal moves + no check = stalemate
            if (!board.IsInCheck()) return 0;

            // No legal moves + check = checkmate
            // Ply depth is used to make the bot prefer checkmates that happen sooner
            // 9999998 is one less than "infinity" used as initial alpha/beta values, one less to avoid beta comparison failing
            return -9999998 + plyFromRoot;
        }

        // Quiescence search
        if (qsearch)
        {
            var staticEval = Evaluate(board);
            if (staticEval >= beta) return beta; // This position is better than beta, so the opponent will not allow it to happen
            alpha = Math.Max(alpha, staticEval); // This position is a better move
        }

        var bestMoveInPosition = Move.NullMove;
        foreach (var move in legalMoves)
        {
            if (_searchAborted) break;
            board.MakeMove(move);
            var eval = -Search(board, -beta, -alpha, depthLeft - 1, plyFromRoot + 1);
            board.UndoMove(move);
            if (eval >= beta)
                // Opponent will not allow this move to happen because it's too good
                return beta;

            // New best move found
            if (eval > alpha)
            {
                alpha = eval;
                bestMoveInPosition = move;
            }
        }

        if (plyFromRoot == 0)
        {
            // If no best move was found (e.g. search cancelled), just use the first legal move
            _bestMove = bestMoveInPosition.IsNull ? legalMoves[0] : bestMoveInPosition;
        }

        if (!qsearch && !_searchAborted)
            _transpositionTable[board.ZobristKey] = (alpha, depthLeft, bestMoveInPosition);

        return alpha;
    }

    private Move[] GetOrderedLegalMoves(Board board, int plyFromRoot, bool capturesOnly = false)
    {
        return board.GetLegalMoves(capturesOnly).OrderByDescending(move =>
        {
            // Always start with the best move from the previous iteration
            if (plyFromRoot == 0 && _bestMove.Equals(move))
                return 9999999;

            var score = 0;

            // Prioritise moves that capture pieces
            if (move.CapturePieceType != PieceType.None)
            {
                score += _values[(int)move.CapturePieceType];
            }

            // Prioritise moves that promote pawns
            if (move.IsPromotion)
            {
                score += _values[(int)move.PromotionPieceType];
            }

            return score;
        }).ToArray();
    }

    private int GetSquareBonus(int pieceType, bool isWhite, int rank, int file)
    {
        var correctedRank = isWhite ? 7 - rank : rank;
        return (int)Math.Round(unchecked((sbyte)((_packedPieceSquareTables[(pieceType * 8) + correctedRank] >> file * 8) & 0xFF)) * 1.461);
    }

    /**
     * Evaluate the current position. Score is from the perspective of the player to move, where positive is winning.
     */
    private int Evaluate(Board board)
    {
        var (midgameEval, endgameEval, gamePhase) = (0, 0, 0);
        foreach (var pieceList in board.GetAllPieceLists())
        {
            var pieceType = (int)pieceList.TypeOfPieceInList - 1;
            foreach (var piece in pieceList)
            {
                var colorModifier = piece.IsWhite ? 1 : -1;
                midgameEval += (_values[pieceType + 1] + GetSquareBonus(pieceType, piece.IsWhite, piece.Square.Rank, piece.Square.File)) * colorModifier;
                endgameEval += (_values[pieceType + 1] + GetSquareBonus(pieceType + 6, piece.IsWhite, piece.Square.Rank, piece.Square.File)) * colorModifier;
                gamePhase += _gamePhaseIncrement[pieceType + 1];
            }
        }

        var perspective = board.IsWhiteToMove ? 1 : -1;
        var middlegameModifier = Math.Min(gamePhase, 24) / 24d;
        var endgameModifier = 1 - middlegameModifier;
        return (int)(midgameEval * middlegameModifier + endgameEval * endgameModifier) * perspective;
    }
}
