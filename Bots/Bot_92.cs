namespace auto_Bot_92;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_92 : IChessBot
{
    private Dictionary<ulong, TranspositionEntry> transpositionTable;
    private Move bestMove;
    private Random rand;
    private Boolean giveup;
    private Boolean is_me;
    public class TranspositionEntry
    {
        public double Value { get; set; }
        public int Depth { get; set; }
        // Add any additional information you may need, such as best move, etc.      
    }


    public Bot_92()
    {
        transpositionTable = new Dictionary<ulong, TranspositionEntry>();
        rand = new Random();
    }

    public Move Think(Board board, Timer timer)
    {
        int startDepth = 1;
        giveup = false;
        is_me = board.IsWhiteToMove;
        while (
            (timer.MillisecondsRemaining > 15000) ?
            (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 17) :
            (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 200)
        )
        {
            AlphaBeta(board, startDepth, int.MinValue, int.MaxValue, board.IsWhiteToMove,
            timer, true, startDepth + 3);
            startDepth += 1;
            if (board.PlyCount < 6 && timer.MillisecondsElapsedThisTurn > 500)
                break;
        };
        // Value updated within AlphaBeta
        return bestMove;
    }

    public List<Move> getCandidateMoves(Board board, int num)
    {
        List<Move> candidateMoves = new List<Move>();
        // Generate all legal moves
        Move[] moves = board.GetLegalMoves();

        // Panic mode in case checkmate for oponnent
        // if(!board.IsInCheck()) {
        //     board.TrySkipTurn();
        //     foreach(Move move in board.GetLegalMoves()) {
        //         board.MakeMove(move);
        //         if(board.IsInCheckmate())                     
        //             num = 100;                
        //         board.UndoMove(move);
        //     }
        //     board.UndoSkipTurn();
        // };       

        double[] scores = moves.Select(move =>
        {
            board.MakeMove(move);
            double score = EvalBoard(board, 0);
            board.UndoMove(move);
            return score;
        }).ToArray();

        var candidates = scores
            .Select((score, index) => new { Score = board.IsWhiteToMove ? score : -score, Index = index })
            .OrderByDescending(entry => entry.Score)
            .Take(num)
            .Select(entry => moves[entry.Index])
            .ToList();

        candidateMoves.InsertRange(0, candidates);

        // Add checks and captures to candidate moves
        foreach (Move move in moves)
        {
            if (candidateMoves.Contains(move))
                continue;
            // else if (move.IsCapture)
            // {
            //     candidateMoves.Insert(0,move);
            // }
            if (board.SquareIsAttackedByOpponent(move.StartSquare))
            {
                candidateMoves.Insert(0, move);
            }
            else
            {
                //if(!board.SquareIsAttackedByOpponent(move.TargetSquare)) {                
                board.MakeMove(move);
                if (board.IsInCheck())
                    candidateMoves.Insert(0, move);
                board.UndoMove(move);
            }
        }

        return candidateMoves;
    }


    public double AlphaBeta(Board board, int depth, double alpha, double beta, bool maximizingPlayer, Timer timer, bool update_best_move, int real_max_depth)
    {

        if (board.IsDraw() || board.IsInCheckmate() || depth == 0)
            return EvalBoard(board, depth);

        if (!update_best_move && transpositionTable.ContainsKey(board.ZobristKey))
        {
            TranspositionEntry entry = transpositionTable[board.ZobristKey];
            if (entry.Depth >= depth)
                return entry.Value;

        }
        Move newBestMove = new Move();
        List<Move> moves = getCandidateMoves(board, 5);
        double bestEval = maximizingPlayer ? int.MinValue : int.MaxValue;
        foreach (var next_move in moves)
        {
            if (timer.MillisecondsRemaining < timer.MillisecondsElapsedThisTurn * 17)
            {
                giveup = true;
                break;
            };
            bool good_move = next_move.IsCapture && !board.SquareIsAttackedByOpponent(next_move.TargetSquare);
            // Give up, dont update the result                    
            board.MakeMove(next_move);
            int curr_depth = depth;

            // Go deeper with forcing moves
            if (
                depth <= 2 &&
                real_max_depth > 0 && (board.IsInCheck() || good_move))
                curr_depth += 1;

            double eval = AlphaBeta(board, curr_depth - 1, alpha, beta, !maximizingPlayer, timer, false, real_max_depth - 1);
            board.UndoMove(next_move);
            if (!giveup && maximizingPlayer ? eval >= bestEval : eval <= bestEval)
            {
                bestEval = eval;
                if (update_best_move)
                    newBestMove = next_move;

                if (maximizingPlayer)
                {
                    alpha = Math.Max(alpha, eval);
                }
                else
                {
                    beta = Math.Min(beta, eval);
                }
            };
            if (beta <= alpha)
                break; // Beta cutoff                        
        }
        if (!giveup)
        {
            transpositionTable[board.ZobristKey] = new TranspositionEntry { Value = bestEval, Depth = depth };
            if (update_best_move)
                bestMove = newBestMove;
        };
        return bestEval;
    }

    public double EvalBoard(Board board, int depth)
    {

        if (board.IsRepeatedPosition() || board.IsDraw())
            return 0;

        double result = 0;
        if (board.IsInCheckmate())
            return ((board.FiftyMoveCounter < 45 && (board.IsWhiteToMove != is_me)) ? (board.IsWhiteToMove ? -5 + depth : 5 - depth) : (board.IsWhiteToMove ? int.MinValue : int.MaxValue));

        ulong hash = board.ZobristKey;
        if (transpositionTable.ContainsKey(hash))
            return transpositionTable[hash].Value;

        foreach (PieceList piece in board.GetAllPieceLists())
        {
            for (int i = 0; i < piece.Count; i += 1)
            {
                result += EvalPiece(board, piece.GetPiece(i)) * (piece.IsWhitePieceList ? 1 : -1);
            }
        }

        // Some randomness not to be exploited too hard        
        result += rand.NextDouble() * 0.01;

        transpositionTable[hash] = new TranspositionEntry { Value = result, Depth = 0 }; ;
        CheckTableSize();

        return result;
    }

    public double EvalPiece(Board board, Piece piece)
    {
        // Not really great but saves tokens
        double result = Math.Pow(1.7, (int)piece.PieceType);
        int rank = piece.Square.Rank;
        int file = piece.Square.File;
        int real_rank = (piece.IsWhite ? rank : 7 - rank);
        if (board.PlyCount < 60)
        {
            switch (piece.PieceType)
            {
                case PieceType.Pawn:
                    //Encourage pawns to control and attack the middle squares (d4, d5, e4, e5)                                                      
                    if (real_rank >= 2 && real_rank <= 4 && file >= 2 && file <= 4)
                        result += 0.4;
                    break;
                case PieceType.Knight:
                    // Correct eval for the knight        
                    result += 2;
                    //Encourage knights to control and attack the middle squares (d4, d5, e4, e5)                
                    if (file > 1 && file < 6)
                        result += 0.3;
                    break;
                case PieceType.Bishop:
                    if (real_rank == 0)
                        result -= 0.25;
                    break;
                case PieceType.Rook:
                    //Encourage rooks to control and attack the middle squares (d4, d5, e4, e5)                    
                    if ((int)(file - 3.5) == 0)
                        result += 0.2;

                    break;
                case PieceType.King:
                    // Encourage the King to stay safe and not move too much                    
                    result -= real_rank * 0.5;

                    // safe and Castle!?
                    if (file < 2 || file > 5)
                        result += 1;
                    break;
            }
        }
        else
        {
            // Pawns advance  
            result += real_rank * (piece.IsPawn ? 0.1 : 0.001);
        }

        return result;
    }


    private void CheckTableSize()
    {
        // If the table size exceeds the maximum allowed size, remove half of the entries
        if (transpositionTable.Count > 6710886)
        {
            // Sort the transposition table by Depth in ascending order
            var sortedEntries = transpositionTable.OrderBy(e => e.Value.Depth).ToList();
            // Determine the number of entries to remove (half of the current table size)
            int entriesToRemove = transpositionTable.Count / 2;

            // Remove the oldest entries until the number of removed entries reaches the target count
            for (int i = 0; i < entriesToRemove; i++)
            {
                transpositionTable.Remove(sortedEntries[i].Key);
            }
        }
    }
}

