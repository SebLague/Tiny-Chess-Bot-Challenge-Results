namespace auto_Bot_259;
using ChessChallenge.API;
using System;

public class Bot_259 : IChessBot
{
    // { None, Pawn, Knight, Bishop, Rook, Queen, King}
    private int[] _pieceValues = { 0, 100, 320, 320, 500, 1000, 10000 };
    private int[] _bonusPointsPerAttackEarly = { 0, 0, 4, 5, 1, 1, 0 };
    private int[] _bonusPointsPerAttackLate = { 0, 0, 2, 3, 5, 3, 1 };
    private int[] _moveScores = new int[218]; // for sorting moves
    Random _rng = new Random();

    // Transposition table
    private TTEntry[] _ttEntries = new TTEntry[16000000];

    public Move Think(Board board, Timer timer)
    {
        // Forced move, don't waste time searching
        Span<Move> legalMoves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref legalMoves);
        Move bestMove = legalMoves[0];
        if (legalMoves.Length == 1)
            return bestMove;

        // Iterative deepening
        for (int currentDepth = 1; timer.MillisecondsElapsedThisTurn < (Math.Min(1000, timer.MillisecondsRemaining / 40) + timer.IncrementMilliseconds) / 4; currentDepth++)
            Search(currentDepth, 0, -1000000000, 1000000000);

        return bestMove;

        int Search(int depth, int ply, int alpha, int beta)
        {
            // First check if there's a checkmate
            if (board.IsInCheckmate())
                return -100000 + ply * 1000; // multiply by depth, the sooner the mate the better
            if (board.IsDraw())
                return 0;

            // Try get evaluation from Transposition Table
            TTEntry entry = _ttEntries[board.ZobristKey % 16000000];
            int tableEval = entry._evalValue;
            if (ply != 0 && entry._zobristKey == board.ZobristKey && entry._depth >= depth
                && (entry._nodeType == 0
                || entry._nodeType == 1 && tableEval <= alpha
                || entry._nodeType == 2 && tableEval >= beta))
                return tableEval;

            int extend = 0;
            if (board.IsInCheck()) // Check extension
                extend = 1;

            if (depth == 0)
                return QSearch(alpha, beta);

            byte evalType = 1; // Alpha
            Span<Move> legalMoves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref legalMoves);

            if (legalMoves.Length > 1)
                OrderMoves(ref legalMoves, ply == 0 && depth > 1);
            else
                extend = 1; // Forced move/One reply extension
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = -Search(depth - 1 + extend, ply + 1, -beta, -alpha);
                board.UndoMove(move);

                if (eval >= beta)
                {
                    StoreEvalInTT(eval, 2); // Beta
                    return beta;
                }
                if (eval > alpha)
                {
                    evalType = 0; // Exact
                    alpha = eval;
                    if (ply == 0)
                        bestMove = move;
                }
            }

            StoreEvalInTT(alpha, evalType);
            return alpha;

            void StoreEvalInTT(int evalValue, byte nodeType)
            {
                _ttEntries[board.ZobristKey % 16000000] = new TTEntry
                {
                    _nodeType = nodeType,
                    _depth = (byte)depth,
                    _zobristKey = board.ZobristKey,
                    _evalValue = evalValue
                };
            }
        }

        // Search only captures
        int QSearch(int alpha, int beta)
        {
            int eval = Evaluate() * (board.IsWhiteToMove ? 1 : -1);
            if (eval >= beta)
                return beta;
            if (alpha < eval)
                alpha = eval;

            Span<Move> legalMoves = stackalloc Move[256];
            board.GetLegalMovesNonAlloc(ref legalMoves, true);
            OrderMoves(ref legalMoves, false);
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                eval = -QSearch(-beta, -alpha);
                board.UndoMove(move);

                if (eval >= beta)
                    return beta;
                if (eval > alpha)
                    alpha = eval;
            }

            return alpha;
        }


        // Evaluates a board, positive score is good for white, negative for black
        int Evaluate()
        {
            // Evaluate based on material value
            int evaluation = 0;

            var bonusPointsPerAttack = board.PlyCount > 30 ? _bonusPointsPerAttackLate : _bonusPointsPerAttackEarly;
            foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
            {
                if (pieceType == PieceType.None)
                    continue;
                EvaluatePieces(pieceType, true, 1);
                EvaluatePieces(pieceType, false, -1);
            }

            // Add a tiny bit of rng to eval, this way we can pick evaluated positions with same score
            return evaluation + _rng.Next(-1, 2);

            void EvaluatePieces(PieceType pieceType, bool isWhite, int sign)
            {
                var pieceList = board.GetPieceList(pieceType, isWhite);
                evaluation += pieceList.Count * _pieceValues[(int)pieceType] * sign; // Evaluate material value
                // Evaluate attacks
                foreach (var piece in pieceList)
                {
                    var pieceBitboard = BitboardHelper.GetPieceAttacks(pieceType, piece.Square, board, isWhite);
                    var attacks = BitboardHelper.GetNumberOfSetBits(pieceBitboard);
                    evaluation += attacks * bonusPointsPerAttack[(int)pieceType] * sign;
                }

                // Evaluate Pawns
                if (pieceType == PieceType.Pawn)
                {
                    var pawnFileFlags = 0;
                    foreach (var pawn in pieceList)
                    {
                        var fileFlag = 1 << pawn.Square.File;
                        // Double pawn penalty
                        if ((pawnFileFlags & fileFlag) != 0) // We know there was a pawn on this file, so it's a double pawn
                            evaluation -= 15 * sign;
                        pawnFileFlags |= fileFlag;

                        // Passed pawns
                        ulong passedPawnMask = 0;
                        BitboardHelper.SetSquare(ref passedPawnMask, pawn.Square);
                        if (pawn.Square.File < 7)
                            passedPawnMask |= passedPawnMask << 1;
                        if (pawn.Square.File > 0)
                            passedPawnMask |= passedPawnMask >> 1;
                        if (isWhite)
                        {
                            passedPawnMask <<= 8;
                            passedPawnMask |= passedPawnMask << 8;
                            passedPawnMask |= passedPawnMask << 16;
                            passedPawnMask |= passedPawnMask << 32;
                        }
                        else
                        {
                            passedPawnMask >>= 8;
                            passedPawnMask |= passedPawnMask >> 8;
                            passedPawnMask |= passedPawnMask >> 16;
                            passedPawnMask |= passedPawnMask >> 32;
                        }
                        // Passed pawn bonus, the closer to promotion the better
                        if ((passedPawnMask & board.GetPieceBitboard(PieceType.Pawn, !isWhite)) == 0) // Check interesction between mask and enemy pawns
                            evaluation += 20 * (isWhite ? pawn.Square.Rank : 7 - pawn.Square.Rank) * sign;
                    }

                    foreach (var pawn in pieceList)
                    {
                        var fileFlag = 1 << pawn.Square.File;
                        // Isolated pawn penalty
                        if ((pawnFileFlags & ((fileFlag << 1) | (fileFlag >> 1))) == 0) // Check adjacent files for other friendly pawns
                            evaluation -= 10 * sign;
                    }
                }
            }
        }

        void OrderMoves(ref Span<Move> moves, bool useBestMove)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                _moveScores[i] = 0;
                if (useBestMove && move == bestMove)
                {
                    _moveScores[i] = 10000000;
                    continue;
                }

                if (move.CapturePieceType != PieceType.None)
                    _moveScores[i] += 10 * _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType];

                if (move.IsPromotion)
                    _moveScores[i] += _pieceValues[(int)move.PromotionPieceType];

                if (move.IsCastles)
                    _moveScores[i] += 10000;
            }

            _moveScores.AsSpan().Slice(0, moves.Length).Sort(moves, (a, b) => b.CompareTo(a));
        }
    }

    public struct TTEntry
    {
        public int _evalValue;
        public byte _depth;
        public byte _nodeType; // 0 = Exact, 1 = Alpha, 2 = Beta
        public ulong _zobristKey;
    }
}