namespace auto_Bot_448;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_448 : IChessBot
{
    // Transposition Table
    Transposition[] _transpositionTable = new Transposition[(1 << 25)/4];

    // Heuristics
    Move[,] _killerMoves = new Move[2, 32];

    // This holds the best found moves for each search
    Move _bestMove, _bestMoveIteration;

    // int _searchedNodes, _savedNodes, _foundNodes = 0;

    // Null, Pawn, Knight, Bishop, Rook, Queen, King 
    int[] _pieceValues = { 0, 82, 337, 365, 477, 1025, 0 };

    public Move Think(Board board, Timer timer)
    {
        // Reset the best move before starting the search
        // I usually had reset the best move before each iteration, which caused many unsual blunders, fixed now
        _bestMove = Move.NullMove;

        // Start iterative deepening serach at depth of 1
        for (var depth = 1; depth <= 50; depth++)
        {
            // _searchedNodes = _foundNodes = 0;

            Search(board, timer, depth, -99999, 99999);

            if (_bestMoveIteration != Move.NullMove)
            {
                _bestMove = _bestMoveIteration;
            }

            // DivertedConsole.Write("(Depth " + depth + ") Best move: " + _bestMove.StartSquare.Name + _bestMove.TargetSquare.Name + " (" + _searchedNodes + " nodes / " + _savedNodes + " saved / " + _foundNodes + " found) (took " + timer.MillisecondsElapsedThisTurn +"ms)");

            // Break deepening if thinking took to long
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 20)
            {
                break;
            }
        }

        // DivertedConsole.Write("Evaluation: " + Evaluation(board, board.IsWhiteToMove));
        // DivertedConsole.Write("");

        return _bestMove.IsNull ? board.GetLegalMoves()[0] : _bestMove;
    }

    // https://www.chessprogramming.org/Negamax with Quiescence
    int Search(Board board, Timer timer, int depth, int alpha, int beta, int extensionCount = 0, int plyCount = 0)
    {
        if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 20)
        {
            return 0;
        }

        // Detect for draw by repetition (Idea from the Chess Discord server)
        if (plyCount > 0 && board.IsRepeatedPosition())
        {
            return 0;
        }

        var zobristKey = board.ZobristKey;
        var transposition = _transpositionTable[zobristKey % 25]; // fixed this with real entries (before: & 20)

        // Try to look up current position in transposition table
        if (transposition.Hash == zobristKey && transposition.Depth >= depth

            // Exact
            && (transposition.Bound == 0

            // Lower
            || (transposition.Bound == 2 && transposition.Evaluation >= beta)

            // Upper
            || (transposition.Bound == 1 && transposition.Evaluation <= alpha)))
        {

            if (plyCount == 0)
            {
                _bestMoveIteration = transposition.Move;
            }

            // _foundNodes++;
            return transposition.Evaluation;
        }

        // Quiescence evaluation
        if (depth <= 0)
        {
            return QuiescenceSearch(board, alpha, beta);
        }

        var moves = SortedMoves(board, plyCount == 0 ? _bestMove : transposition.Move, false, plyCount);
        if (moves.Length == 0)
        {
            // Detecting checks and prioritizing them, otherwise draw
            return board.IsInCheck() ? -99999 + plyCount : 0;
        }

        var previousAlpha = alpha;
        var bestMovePosition = Move.NullMove;

        for (var index = 0; index < moves.Length; index++)
        {
            var move = moves[index];
            board.MakeMove(move);

            var deepSearch = true;
            var evaluation = 0;
            var extension = extensionCount < 16 && board.IsInCheck() ? 1 : 0;

            // https://www.chessprogramming.org/Late_Move_Reductions
            // https://github.com/SebLague/Chess-Coding-Adventure/blob/Chess-V2-UCI/Chess-Coding-Adventure/src/Core/Search/Searcher.cs#L270C42-L270C42
            // Reduce the depth of these searches, since they are ordered more unlikely to be good
            if (depth >= 3 && index >= 3 &&

                // Search extensions
                extension == 0 &&

                // Tactical moves
                !move.IsCapture)
            {

                evaluation = -Search(board, timer, depth - 1 - 1, -alpha - 1, -alpha, extensionCount, plyCount + 1);

                // But we still perform a deeper search if the evaluation is better than we had expected
                deepSearch = evaluation > alpha;
            }
            if (deepSearch)
            {
                evaluation = -Search(board, timer, depth - 1 + extension, -beta, -alpha, extensionCount + extension, plyCount + 1);
            }

            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 20)
            {
                return 0;
            }

            // _searchedNodes++;

            // This is where the best move for this position is found
            if (evaluation > alpha)
            {
                alpha = evaluation;
                bestMovePosition = move;

                if (plyCount == 0)
                {
                    _bestMoveIteration = move;
                }
            }

            // Break out of loop if move was "too good" not to be picked anyways
            // Beta Cutoff
            if (evaluation >= beta)
            {

                // Idea from https://github.com/SebLague/Chess-Coding-Adventure/blob/Chess-V2-UCI/Chess-Coding-Adventure/src/Core/Search/Searcher.cs#L297C6-L297C6
                // We basically also store the lower bounds move, because there might be one, although we didn't look at them
                _transpositionTable[zobristKey % 25] = new Transposition(zobristKey, beta, depth, 2, move);
                // _savedNodes++;

                // https://www.chessprogramming.org/Killer_Heuristic
                if (!move.IsCapture && plyCount < 32)
                {
                    _killerMoves[1, plyCount] = _killerMoves[0, plyCount];
                    _killerMoves[0, plyCount] = move;
                }

                return beta;
            }
        }

        // Save current Transposition with it's information in TT
        _transpositionTable[zobristKey % 25] = new Transposition(zobristKey, alpha, depth, alpha >= beta ? 2 : alpha > previousAlpha ? 0 : 1, bestMovePosition);
        // _savedNodes++;

        return alpha;
    }

    // https://www.chessprogramming.org/Quiescence_Search
    int QuiescenceSearch(Board board, int alpha, int beta)
    {
        var evaluation = Evaluation(board, board.IsWhiteToMove);
        if (evaluation >= beta)
        {
            return beta;
        }
        if (evaluation > alpha)
        {
            alpha = evaluation;
        }

        foreach (var move in SortedMoves(board, Move.NullMove, true))
        {
            board.MakeMove(move);
            evaluation = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation > alpha)
            {
                alpha = evaluation;
            }
            if (evaluation >= beta)
            {
                return beta;
            }
        }

        return alpha;
    }

    // https://www.chessprogramming.org/Move_Ordering
    // Ideas from Coding Adventures: Chess - https://www.youtube.com/watch?v=U4ogK0MIzqk
    Move[] SortedMoves(Board board, Move hashMove, bool quiescence, int plyCount = 0)
    {
        return board.GetLegalMoves(quiescence).OrderByDescending(move =>

                // Hash Move
                move == hashMove ? 90000 :

                // https://www.chessprogramming.org/MVV-LVA
                move.IsCapture ? 100 * _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType] :

                // Promotions
                move.IsPromotion && !move.IsCapture ? 3 :

                // 1st Killer move
                !quiescence && !move.IsCapture && move == _killerMoves[0, plyCount] ? 2 :

                // 2nd Killer move
                !quiescence && !move.IsCapture && move == _killerMoves[1, plyCount] ? 1 :

                // Default value
                0).ToArray();
    }

    int Evaluation(Board board, bool whiteToMove)
    {
        // No more time to squeeze things up, pretty bad since I'm just using one table for knights
        var knightTable = new[] {
                                    -167, -89, -34, -49,  61, -97, -15, -107,
                                    -73, -41,  72,  36,  23,  62,   7,  -17,
                                    -47,  60,  37,  65,  84, 129,  73,   44,
                                    -9,  17,  19,  53,  37,  69,  18,   22,
                                    -13,   4,  16,  13,  28,  19,  21,   -8,
                                    -23,  -9,  12,  10,  19,  17,  25,  -16,
                                    -29, -53, -12,  -3,  -1,  18, -14,  -19,
                                    -105, -21, -58, -33, -17, -28, -19,  -23
                                };
        // var bishopTable = new [] {
        //                             -29,   4, -82, -37, -25, -42,   7,  -8,
        //                             -26,  16, -18, -13,  30,  59,  18, -47,
        //                             -16,  37,  43,  40,  35,  50,  37,  -2,
        //                             -4,   5,  19,  50,  37,  37,   7,  -2,
        //                             -6,  13,  13,  26,  34,  12,  10,   4,
        //                             0,  15,  15,  15,  14,  27,  18,  10,
        //                             4,  15,  16,   0,   7,  21,  33,   1,
        //                             -33,  -3, -14, -21, -13, -12, -39, -21
        //                         };

        var score = 0;
        foreach (var pieceType in Enum.GetValues<PieceType>())
        {
            if (pieceType == PieceType.None)
            {
                continue;
            }

            foreach (var white in new[] { true, false })
            {
                var pieces = board.GetPieceList(pieceType, white);
                var perspective = white ? 1 : -1;

                score += perspective * pieces.Count * _pieceValues[(int)pieceType];

                foreach (var piece in pieces)
                {
                    var index = white ? piece.Square.Index : 63 - piece.Square.Index;
                    // var value = 0;

                    // switch(piece.PieceType) {
                    //     case PieceType.Bishop:
                    //         value = bishopTable[index];
                    //         break;
                    //     case PieceType.Knight:
                    //         value = knightTable[index];
                    //         break;
                    // }

                    score += perspective * (piece.PieceType == PieceType.Knight ? knightTable[index] : 0);
                }
            }
        }

        return (whiteToMove ? 1 : -1) * score;
    }

    // https://www.chessprogramming.org/Transposition_Table
    struct Transposition
    {
        public ulong Hash;
        public int Evaluation;
        public int Depth;
        public int Bound;
        public Move Move;

        public Transposition(ulong hash, int evaluation, int depth, int bound, Move move)
        {
            Hash = hash;
            Evaluation = evaluation;
            Depth = depth;
            Bound = bound;
            Move = move;
        }
    }
}