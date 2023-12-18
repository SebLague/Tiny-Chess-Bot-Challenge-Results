namespace auto_Bot_268;
/**/
using ChessChallenge.API;
using System;
using System.Collections.Generic;
/**/

// S. Sheta 2023 | Bot name: CaptainBotvious
// Given a board position, the majority of possible moves are most likely
// very bad, and only a few are worth considering. So this bot optimises
// the search by prioritising the most obvious moves.
public class Bot_268 : IChessBot
{
    public Bot_268()
    {
        rng = new System.Random();
    }

    public Move Think(Board board, Timer timer)
    {
        // setup
        isWhite = board.IsWhiteToMove;
        timeRemainingAtStart = timer.MillisecondsRemaining;
        // enter middle game
        if (gamePhase == 0 && board.PlyCount > 6) gamePhase = 1;
        // enter end game
        PieceList[] pieces = board.GetAllPieceLists();
        int numPiecesWhite = 0;
        int numPiecesBlack = 0;
        for (int i = 0; i < 6; i++) numPiecesWhite += pieces[i].Count;
        for (int i = 0; i < 6; i++) numPiecesBlack += pieces[i + 6].Count;
        if (numPiecesWhite <= 8 || numPiecesBlack <= 8) gamePhase = 2;
        // find and return move
        return FindBestMove(board, isWhite, EvaluateBoard(board, isWhite), 0, depthLimit, ref timer);
    }

    // Find best move by recursion.
    private Move FindBestMove(Board board, bool isWhiteToPlay, float startEval, int currentDepth, int maxDepth, ref Timer timer)
    {
        // Initialize algorithm
        Move[] moves = board.GetLegalMoves();
        int numMoves = moves.Length;
        bool isInCheck = board.IsInCheck();
        float currentEval = EvaluateBoard(board, isWhiteToPlay);

        // Score the obviousness of the moves based on the basic principles of chess
        float[] moveObviousness = new float[numMoves];
        for (int i = 0; i < numMoves; i++)
        {
            Move move = moves[i];
            PieceType pieceType = move.MovePieceType;

            // 1. checks
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }
            if (board.IsInCheck()) moveObviousness[i] += 3.0f;
            board.UndoMove(move);
            // 2. capture
            if (move.IsCapture) moveObviousness[i] += GetPieceValue(move.CapturePieceType);
            // 3. movement
            if (pieceType == PieceType.Pawn)
            {
                // pawns should be more active at the start and in the endgame
                moveObviousness[i] += (gamePhase != 1) ? 1.5f : 1.0f;
                // centre bias for the opening
                if ((move.TargetSquare.File == 3 || move.TargetSquare.File == 4) && (move.TargetSquare.Rank == 3 || move.TargetSquare.Rank == 4)) moveObviousness[i] += 1.0f;
            }
            if (pieceType == PieceType.King)
            {
                // castling is good
                if (move.IsCastles) moveObviousness[i] += 3.0f;
                // king should not be active until the endgame
                else moveObviousness[i] += (gamePhase < 2) ? -1.0f : 1.5f;
            }
            if (pieceType == PieceType.Knight || pieceType == PieceType.Bishop) moveObviousness[i] += (move.StartSquare.Rank == 0 || move.StartSquare.Rank == 7 || gamePhase == 2) ? 1.5f : 1.0f;
            if (pieceType == PieceType.Rook || pieceType == PieceType.Queen) moveObviousness[i] += (gamePhase + 1.0f) / 2.0f;
            // 4. special
            if (move.IsPromotion) moveObviousness[i] += GetPieceValue(move.PromotionPieceType) + 1.0f;
        }

        // Evaluate the moves
        float[] moveEvals = new float[numMoves];
        float bestEval = -1000000.0f;
        float bestObviousness = -1000000.0f;
        for (int i = 0; i < numMoves; i++)
        {
            Move move = moves[i];
            board.MakeMove(move); // make the move

            // handle draw
            if (board.IsDraw())
            {
                board.UndoMove(move);
                moveEvals[i] = -startEval;
                continue;
            }
            else if (currentDepth < maxDepth || isWhiteToPlay == isWhite) // evaluate with recursion
            {
                // biases for depth
                float materialBias = (EvaluateBoard(board, isWhiteToPlay) - currentEval + 1.0f) / 10.0f;
                float movesBias = Math.Max(30 - numMoves, 0) / 30.0f;
                float timeBias = (timeRemainingAtStart - timer.MillisecondsElapsedThisTurn) / timer.GameStartTimeMilliseconds;
                float progressBias = timeBias * gamePhase / 2.0f;
                // compute the weighted depth 
                int weightedDepth = Math.Min(Math.Max((board.IsInCheck() || isInCheck) ? 3 : 1, (int)(maxDepth * (materialBias + movesBias) * progressBias)), depthLimit);
                Move response = FindBestMove(board, !isWhiteToPlay, -startEval, currentDepth + 1, weightedDepth, ref timer);
                board.MakeMove(response); // make the response
                moveEvals[i] = EvaluateBoard(board, isWhiteToPlay) - startEval;
                board.UndoMove(response); // undo the response
                if (move.IsCastles && moveEvals[i] >= 0.0f) moveEvals[i] += 1.0f; // encourage castling
            }
            else // evaluate naively
            {
                moveEvals[i] = EvaluateBoard(board, isWhiteToPlay) - startEval;
            }

            board.UndoMove(move); // undo the move

            // update best eval and obviousness
            if (moveEvals[i] > bestEval)
            {
                bestEval = moveEvals[i];
                bestObviousness = moveObviousness[i];
            }
        }

        // Find the best moves and return a random one
        List<Move> bestMoves = new List<Move>();
        for (int i = 0; i < numMoves; i++)
        {
            // smaller eval, skip
            if (moveEvals[i] < bestEval - 0.0001f) continue;
            // equal eval, compare obviousness
            else if (moveEvals[i] < bestEval + 0.0001f)
            {
                // smaller obviousness, skip
                if (moveObviousness[i] < bestObviousness - 0.0001f) continue;
                // equal obviousness, add
                else if (moveObviousness[i] < bestObviousness + 0.0001f) bestMoves.Add(moves[i]);
                // larger obviousness, replace
                else
                {
                    bestObviousness = moveObviousness[i];
                    bestMoves.Clear();
                    bestMoves.Add(moves[i]);
                }
            }
            // larger eval, replace
            else
            {
                bestEval = moveEvals[i];
                bestObviousness = moveObviousness[i];
                bestMoves.Clear();
                bestMoves.Add(moves[i]);
            }
        }

        return bestMoves[rng.Next(bestMoves.Count)];
    }

    // Evaluate the board for the specified player
    private float EvaluateBoard(Board board, bool forWhite)
    {
        if (board.IsInCheckmate()) return (board.IsWhiteToMove == forWhite) ? -100.0f : 100.0f;
        if (board.IsDraw()) return 0.0f;
        PieceList[] pieces = board.GetAllPieceLists();
        float score = 0.0f;
        for (int i = 0; i < 6; i++)
        {
            // add white's score
            score += pieces[i].Count * GetPieceValue(pieces[i].TypeOfPieceInList);
            // substract black's score
            score -= pieces[i + 6].Count * GetPieceValue(pieces[i + 6].TypeOfPieceInList);
        }
        return (forWhite) ? score : -score;
    }

    // Custom piece values
    private float GetPieceValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 1.0f;
            case PieceType.Knight:
                return 2.75f;
            case PieceType.Bishop:
                return 3.0f;
            case PieceType.Rook:
                return 5.0f;
            case PieceType.Queen:
                return 9.0f;
            case PieceType.King:
                return 100.0f;
            default:
                return 0.0f;
        }
    }

    private bool isWhite = true;
    private int gamePhase = 0; // 0 for opening, 1 for midgame, 2 for endgame
    private int depthLimit = 20;
    private float timeRemainingAtStart;
    private System.Random rng;
}