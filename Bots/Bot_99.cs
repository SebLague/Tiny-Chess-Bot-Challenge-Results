namespace auto_Bot_99;
using ChessChallenge.API;
using System;

public class Bot_99 : IChessBot
{
    // Piece values: none, pawn, knight, bishop, rook, queen, king, maps to PieceType enum as int
    static readonly int[] pieceValues = { 0, 100, 280, 320, 479, 929, 60000 };
    Board board;
    int exchangeDepth = 1_000;
    const int posInf = 1_000_000_000; // Note: -int.MaxValue == int.MaxValue due to overflow.
    const int negInf = -posInf;
    Random rng = new(1234);
    int moveCount = 0; // Track how many moves were made when searching
    int pruneCount = 0;
    int exchangeCount = 0; // Track how many exchanges were made when searching
    int exchangePruneCount = 0;

    short GetPieceSquareTableValue(int rank, int file, bool whiteToPlay = true)
    {
        // Flip the rank if whiteToPlay is true
        int rankFactor = (whiteToPlay ? 7 - rank : rank); // Prefer to go slightly higher rank
        int fileFactor = 4 - Math.Abs(file - 4); // Prefer to be in the center
        return (short)(rankFactor * 10 + fileFactor * 20);
    }
    public int GetBoardScore()
    {
        // Returns score of board, + favors current side to play
        // Calculate score, positive means better for side whose turn it is.
        // Initially calculated where negative means better for black, positive for white.
        if (board.IsInCheckmate()) return negInf; // Lose for side
        else if (board.IsDraw()) return 0; // Draw
        int score = 0;
        PieceList[] piecelists = board.GetAllPieceLists();
        // White pieces +, Black pieces -
        for (int i = 0; i < 6; i++)
        {
            // White
            foreach (Piece piece in piecelists[i])
            {
                score += pieceValues[i + 1] + GetPieceSquareTableValue(piece.Square.Rank, piece.Square.File);
            }
            // Black
            foreach (Piece piece in piecelists[i + 6])
            {
                score -= pieceValues[i + 1] + GetPieceSquareTableValue(piece.Square.Rank, piece.Square.File, false);
            }
        }
        return board.IsWhiteToMove ? score : -score; // Positive for side if preferred
    }

    // Return a score for a given board position, for the side to play currently.
    public int Evaluate(int alpha, int beta)
    {
        if (board.IsInCheckmate()) return negInf; // Lose
        else if (board.IsDraw()) return 0; // Draw
        return ExchangeCalculator(exchangeDepth, alpha, beta);
    }

    public int ExchangeCalculator(int depth, int alpha, int beta)
    {
        // Returns score for swapping down capturable material or not capturing if it's worse.
        // positive favors side to play on board.
        int score = GetBoardScore();
        if (depth == 0 || score >= beta)
        {
            return score; // Drop out early, score > beta seems to get stuck on bad choices.
        }
        alpha = Math.Max(alpha, score);

        // Search through only capturing moves
        foreach (Move capture_move in board.GetLegalMoves(true))
        {
            var piece_capturing = board.GetPiece(capture_move.StartSquare);
            var piece_captured = board.GetPiece(capture_move.TargetSquare);
            // Skip moves that are worse or equal trades, this is inaccurate but saves a lot of time
            if ((pieceValues[(int)piece_captured.PieceType] <= pieceValues[(int)piece_capturing.PieceType])) continue;

            // Appear to have something to gain, see if the score improves with capture or not.
            board.MakeMove(capture_move);
            // Negate for opponent, alpha-beta pruning
            int new_score = -ExchangeCalculator(depth - 1, -beta, -alpha);
            board.UndoMove(capture_move);
            exchangeCount++;
            if (new_score >= beta)
            {
                // Move too good, opponent avoids.
                exchangePruneCount++;
                return beta;
            }
            if (new_score > alpha)
            {
                alpha = new_score;
            }
        }
        return alpha;
    }

    public int ScoreMove(Move move)
    {
        // Score the move with some arbitrary heuristic for ordering which moves to check first.
        int score = 0;
        // Captures are valuable
        score += move.IsPromotion ? 300_000 : 0;
        score += move.IsCapture ? 200_000 : 0;
        score += move.IsCastles ? 200_000 : 0;
        score += move.IsEnPassant ? 100_000 : 0;
        score += pieceValues[(int)move.MovePieceType]; // Bigger pieces are higher on the list
        // Note: This'll often draw/deadlock endgames with few pieces
        // since pawn moves are last here and the 4/5 ply is often too little to reach the end.
        return score;
    }
    // Use negamax search
    public (int, Move, Move) Search(int maxPly, int alpha, int beta)
    {
        if (board.IsInCheckmate() || board.IsDraw() || maxPly == 0)
        {
            return (Evaluate(alpha, beta), Move.NullMove, Move.NullMove);
        }
        // Evaluate score of board assuming optimal choices up to a maxPly.
        Move[] allMoves = board.GetLegalMoves();
        // Sort moves by score
        Array.Sort(allMoves, (move1, move2) => ScoreMove(move2).CompareTo(ScoreMove(move1)));
        Move moveToPlay = allMoves[0]; // If all moves are losing, just play first one.
        Move expected_response = Move.NullMove;

        foreach (Move move in allMoves)
        {
            // Make move and recursively get negative negamax score (for opponent) to maxPly
            board.MakeMove(move);
            (int score, Move next_move, _) = Search(maxPly - 1, -beta, -alpha);
            score = -score; // invert since opponents best score is our worst.
            board.UndoMove(move);
            moveCount++;

            if (score >= beta)
            {
                // Move too good, opponent avoids.
                pruneCount++;
                return (beta, move, Move.NullMove);
            }
            if (score > alpha) // Note: because of pruning can't choose alternatives score==alpha here as it may be a bad prune?
            {
                // New best move
                int prevAlpha = alpha;
                alpha = score;
                moveToPlay = move;
                expected_response = next_move;
            }
        }
        return (alpha, moveToPlay, expected_response);
    }
    public int getPieceCount()
    {
        return System.Numerics.BitOperations.PopCount(board.AllPiecesBitboard);
    }

    public Move Think(Board board, Timer timer)
    {
        this.board = board;
        moveCount = 0;
        pruneCount = 0;
        exchangeCount = 0;
        exchangePruneCount = 0;

        // Generally, decide how much effort to search with based on time left.
        // TODO : Iterative deepening search, max time limit on search and break out otherwise
        // TODO : Keep previous search and continue growing it instead of starting fresh
        // Start with lower ply for first few moves
        int maxPly = board.PlyCount < 5 ? 4 : 5;
        // Drop search depth as less time remains
        if (timer.MillisecondsRemaining < 1_000)
        {
            // Time for hope chess.
            maxPly = 1;
            exchangeDepth = 0;
        }
        else if (timer.MillisecondsRemaining < 10_000)
        {
            maxPly = 3;
            exchangeDepth = 2;
        }
        else if (timer.MillisecondsRemaining < 30_000)
        {
            maxPly = 4;
            exchangeDepth = 5;
        }
        // If only a few pieces left, go deep and hope to find a win/loss.
        if (getPieceCount() <= 5 && timer.MillisecondsRemaining > 10_000)
        {
            maxPly = 10;
            exchangeDepth = 1_000;
        }

        int curScore = Evaluate(negInf, posInf);
        (int bestScore, Move moveToPlay, Move expected_response) = Search(maxPly, negInf, posInf);

        return moveToPlay;
    }
}
