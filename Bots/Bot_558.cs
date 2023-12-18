namespace auto_Bot_558;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_558 : IChessBot
{
    //Piece values: null, pawn, knight, bishop, rook, queen, king
    static readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move bestMove;
        int depth;

        //Determine the depth by looking at the amount of seconds left.
        if (timer.MillisecondsRemaining < 2000)
            depth = 3;
        else if (timer.MillisecondsRemaining < 10000)
            depth = 4;
        else if (timer.MillisecondsRemaining < 40000)
            depth = 5;
        else
            depth = 6;

        NegaMaxAlphaBeta(board, depth, float.NegativeInfinity, float.PositiveInfinity, timer, out bestMove);

        return bestMove;
    }

    /// <summary>
    /// Determines the value of a move by looking at all possible outcomes recursively. 
    /// </summary>
    /// <param name="board">The current board</param>
    /// <param name="depth">The current depth of the algorithm</param>
    /// <param name="alpha">The alpha value of the NegaMax algorithm</param>
    /// <param name="beta">The beta value of the NegaMax algorithm</param>
    ///<param name="timer">The current timer</param>
    /// <param name="bestMoveFound">The best move found by the algorithm</param>
    /// <returns>The value of the best move</returns>
    float NegaMaxAlphaBeta(Board board, int depth, float alpha, float beta, Timer timer, out Move bestMoveFound)
    {
        bestMoveFound = new Move();

        //Prevents the algorithm from thinking too long.
        if (timer.MillisecondsElapsedThisTurn > 12000 || (timer.MillisecondsElapsedThisTurn > 5000 && timer.MillisecondsRemaining < 12000))
            return float.NegativeInfinity;

        //If the final depth value is reached, then return the value of a capture-only search.
        //This is to prevent catastrophic misjudgement by the evaluation algorithm in the case that there is an immediate attack
        //on a highly-valued piece after the final depth is reached within the regular NegaMax algorithm.
        if (depth == 0)
        {
            return CaptureSearch(board, alpha, beta);
        }

        //If this node is an end node in the tree, then return the heuristic value of the node.
        if (board.IsInCheckmate() || board.IsDraw())
        {
            return EvaluateBoard(board);
        }

        //This node is NOT an end node, so we will determine the best valued child node for the current player.

        //Guess the value of each possible legal move and order them from best to worst in order to search through the tree in the most optimal way.
        List<ValuedMove> possibleMoves = GetLegalOrderedMoves(board, false);

        float bestScore = float.NegativeInfinity;

        foreach (ValuedMove valuedMove in possibleMoves)
        {
            Move move = valuedMove.move;

            board.MakeMove(move);

            //Determine the value of this move by examining either its child nodes or its heuristic value following the NegaMax algorithm.
            float score = -NegaMaxAlphaBeta(board, depth - 1, -beta, -alpha, timer, out _);

            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoveFound = move;
            }

            alpha = MathF.Max(alpha, score);

            //Prune the irrelevant branch of the tree.
            if (alpha >= beta) break;
        }

        return alpha;
    }

    /// <summary>
    /// Determines the value of a move by looking at the possible capture moves only.
    /// This is to prevent catastrophic misjudgement by the evaluation algorithm in the case that there is an immediate attack
    /// on a highly-valued piece after the final depth is reached within the regular NegaMax algorithm.
    /// </summary>
    /// <param name="board">The current board</param>
    /// <param name="alpha">The alpha value of the NegaMax algorithm</param>
    /// <param name="beta">The beta value of the NegaMax algorithm</param>
    /// <returns>The value of the best outcome</returns>
    float CaptureSearch(Board board, float alpha, float beta)
    {
        float eval = EvaluateBoard(board);

        if (eval >= beta)
            return beta;

        alpha = MathF.Max(alpha, eval);

        List<ValuedMove> captureMoves = GetLegalOrderedMoves(board, true);

        foreach (ValuedMove valuedCapture in captureMoves)
        {
            Move capture = valuedCapture.move;

            board.MakeMove(capture);

            //Determine the value of this move by examining either its child nodes or its heuristic value following the NegaMax algorithm.
            eval = -CaptureSearch(board, -beta, -alpha);

            board.UndoMove(capture);

            //Prune the irrelevant branch of the tree.
            if (eval >= beta)
                return beta;

            alpha = MathF.Max(alpha, eval);
        }

        return alpha;
    }

    /// <summary>
    /// A struct for storing a move and their likeliness to be good.
    /// </summary>
    struct ValuedMove : IComparable<ValuedMove>
    {
        public Move move;
        public int value;

        public ValuedMove(Move move, int value)
        {
            this.move = move;
            this.value = value;
        }

        public int CompareTo(ValuedMove other)
        {
            return other.value - value;
        }
    }

    /// <summary>
    /// Guess the value of each possible move and order from best to worst.
    /// </summary>
    /// <param name="board">The current board</param>
    /// <param name="capturesOnly">Whether to only take captures into consideration</param>
    /// <returns>A list of all legal moves on the given board ordered from (most likely) best to (most likely) worst. </returns>
    List<ValuedMove> GetLegalOrderedMoves(Board board, bool capturesOnly)
    {
        List<Move> moves = board.GetLegalMoves(capturesOnly).ToList();

        List<ValuedMove> valuedMoves = moves.ConvertAll(move =>
        {
            return new ValuedMove(move, GuessMoveValue(move, board));
        });

        //Sorts all moves based on their likeliness to be good.
        valuedMoves.Sort();

        return valuedMoves;
    }

    /// <summary>
    /// Guesses the value of any given move.
    /// </summary>
    /// <param name="move">The move to guess the value of</param>
    /// <param name="board">The board before the move is played</param>
    /// <returns>The guessed value of the move.</returns>
    int GuessMoveValue(Move move, Board board)
    {
        int moveValue = 0;

        int movePieceValue = pieceValues[(int)move.MovePieceType];

        //Capturing high valued pieces with a low valued piece is likely to be good.
        if (move.IsCapture)
            moveValue = 10 * pieceValues[(int)move.CapturePieceType] - movePieceValue;

        //Promoting a pawn to a higher valued piece is likely to be good.
        if (move.IsPromotion)
            moveValue += pieceValues[(int)move.PromotionPieceType];

        //Moving a piece to a square currently under attack by the opponent is likely to be bad.
        if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            moveValue -= movePieceValue;

        return moveValue;
    }

    /// <summary>
    /// Evaluates the position of the current player to move.
    /// </summary>
    /// <param name="board">The current board</param>
    /// <returns>The board evaluation for the current player</returns>
    int EvaluateBoard(Board board)
    {
        //Always or never play a move if it results in checkmate. (Unless it is the only move available)
        if (board.IsInCheckmate())
            return -1000000000;

        if (board.IsDraw())
            return 0;

        int whitePieces = 0, blackPieces = 0;

        //Board material evaluation
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                    whitePieces += pieceValues[(int)piece.PieceType];
                else
                    blackPieces += pieceValues[(int)piece.PieceType];
            }
        }

        //Endgame evaluation
        int endgameEval = EvaluateEndgame(board, (board.IsWhiteToMove ? (13900 - blackPieces) : (13900 - whitePieces)) * 0.001f);

        //Return the material evaluation of the board multiplied by the perspective plus the endgame evaluation.
        return (whitePieces - blackPieces) * (board.IsWhiteToMove ? 1 : -1) + endgameEval;
    }

    /// <summary>
    /// Adds an endgame evaluation which incentivises the current player to drive the enemy king away from the center,
    /// according to which phase the game is currently in.
    /// </summary>
    /// <param name="board">The current board</param>
    /// <param name="weight">A weight value, calculated by looking at the amount of enemy score lost</param>
    /// <returns>The endgame evaluation for the current player</returns>
    int EvaluateEndgame(Board board, float weight)
    {
        int eval = 0;

        Square enemyKingSquare = board.GetKingSquare(!board.IsWhiteToMove);

        int enemyDistToCenterFile = Math.Max(3 - enemyKingSquare.File, enemyKingSquare.File - 4);
        int enemyDistToCenterRank = Math.Max(3 - enemyKingSquare.Rank, enemyKingSquare.Rank - 4);
        int enemyDistToCenter = enemyDistToCenterFile + enemyDistToCenterRank;

        eval += enemyDistToCenter;

        return (int)(eval * 10 * weight);
    }
}