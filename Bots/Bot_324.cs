namespace auto_Bot_324;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_324 : IChessBot
{
    const PieceType PAWN = PieceType.Pawn, QUEEN = PieceType.Queen;

    Board board;

    Timer timer;

    Move bestMove, bestMoveThisIteration;

    int searchedMoves;

    readonly int[] PieceValue = new int[] { 0, 100, 300, 320, 500, 900, 10000 };
    const int valWindow = 100;
    int[,,] historyHeuristic;

    public Move Think(Board board_, Timer timer_)
    {
        timer = timer_;
        board = board_; // Update the board locally

        int searchDepth = 1;
        searchedMoves = 0;

        // Iterative deepening

        int alpha = -99999;
        int beta = 99999;

        while (!Timeout())
        {
            historyHeuristic = new int[2, 64, 64];

            int val = Search(searchDepth, 0, alpha, beta, 0);

            // Only set it when the search is completed otherwise go with the previous complete search move
            bestMove = bestMoveThisIteration;

            if ((val <= alpha) || (val >= beta))
            {
                alpha = -99999;    // We fell outside the window, so try again with a
                beta = 99999;      //  full-width window (and the same depth).
                continue;
            }
            alpha = val - valWindow;  // Set up the window for the next iteration.
            beta = val + valWindow;
            searchDepth++;
        }

        return bestMove;
    }

    bool Timeout()
    {
        return timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 60;
    }

    int Search(int plyRemaining, int plyFromRoot, int alpha, int beta, int numExtension)
    {
        if (Timeout()) return alpha;

        // Check for checkmate and draw
        if (board.IsInCheckmate()) return -99999;
        if (board.IsDraw()) return 0;

        searchedMoves++;

        if (plyRemaining == 0)
        {
            int eval = QuiscenceSearch(alpha, beta);
            return eval;
        }

        bool isRootNode = plyFromRoot == 0; // If it's the root

        Move[] moves = board.GetLegalMoves();

        // Score every move based on how likely it is to be a good move
        moves = moves.OrderByDescending(move => Score(move, isRootNode)).ToArray();

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            // Checks will be searched deeper
            int extension = board.IsInCheck() && numExtension < 16 ? 1 : 0;
            int evaluation = -Search(plyRemaining - 1 + extension, plyFromRoot + 1, -beta, -alpha, numExtension + extension);

            board.UndoMove(move);

            if (Timeout()) return alpha;

            if (evaluation >= beta)
            {
                if (isRootNode)
                    bestMoveThisIteration = move;

                GetHistoryHeuristic(move) += plyRemaining * plyRemaining;

                return beta;
            }
            if (evaluation > alpha)
            {
                alpha = evaluation;

                if (isRootNode)
                    bestMoveThisIteration = move;
            }
        }
        return alpha;
    }

    ref int GetHistoryHeuristic(Move move) =>
        ref historyHeuristic[board.IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index];

    int QuiscenceSearch(int alpha, int beta)
    {
        int eval = Evaluate();

        if (eval >= beta)
            return beta;

        alpha = Math.Max(eval, alpha);

        Move[] captureMoves = board.GetLegalMoves(capturesOnly: true);
        captureMoves = captureMoves.OrderByDescending(move => GetCaptureValue(move)).ToArray();

        foreach (Move capture in captureMoves)
        {
            board.MakeMove(capture);

            eval = -QuiscenceSearch(-beta, -alpha);

            board.UndoMove(capture);

            if (eval >= beta)
                return beta;

            alpha = Math.Max(eval, alpha);
        }

        return alpha;
    }

    int Score(Move move, bool isRoot)
    {
        // Best move of the previous iteration should be first
        // And only if it's the root node scoring
        int score = (isRoot && move.Equals(bestMove)) ? 99999 : 0;
        // Take the history into account
        score += GetHistoryHeuristic(move);
        // The more valued capture piece the more likely it is to be a good move
        score += GetCaptureValue(move);
        // Promotional move is generally good
        score += move.IsPromotion ? GetPieceValue(move.PromotionPieceType) : 0;

        return score;
    }

    int Evaluate()
    {
        // Calculate material for black and white
        int whiteMaterial = 0, blackMaterial = 0;

        for (int i = 1; i < PieceValue.Length - 1; i++) // Excluding None and King
        {
            PieceType type = (PieceType)i; // Cast the index to the PieceType type
            whiteMaterial += board.GetPieceList(type, true).Count * GetPieceValue(type);
            blackMaterial += board.GetPieceList(type, false).Count * GetPieceValue(type);
        }

        int perspective = board.IsWhiteToMove ? 1 : -1;
        // Material evaluation
        int eval = (whiteMaterial - blackMaterial) * perspective;

        int EndgameEval()
        {
            // Endgame Eval
            // As endgame comes if the player is winning the closer the king is to the opponent king the higher
            // it will be evaluated and also the closer the opponent king is to the corners the higher the evaluation
            //-------------------------------------------------------------------------------------------------
            // Two pawns advantage will be considered as winning endgame

            // Inverse to the amount of pieces opponent has
            int endgameEval = 0;

            if (eval > GetPieceValue(PieceType.Pawn) * 2)
            {
                int opponentKingRank = board.GetKingSquare(!board.IsWhiteToMove).Rank;
                int opponentKingFile = board.GetKingSquare(!board.IsWhiteToMove).File;

                int opponentKingDstFromCentre = Math.Max(3 - opponentKingRank, opponentKingRank - 4) +
                                                Math.Max(3 - opponentKingFile, opponentKingFile - 4);

                endgameEval += opponentKingDstFromCentre;

                int friendlyKingRank = board.GetKingSquare(board.IsWhiteToMove).Rank;
                int friendlyKingFile = board.GetKingSquare(board.IsWhiteToMove).File;

                int dstBetweenKings = Math.Abs(friendlyKingRank - opponentKingRank) +
                                      Math.Abs(friendlyKingFile - opponentKingFile);

                endgameEval += 14 - dstBetweenKings;
            }
            return endgameEval;
        }
        eval += EndgameEval();

        bool IsWhiteToMove = board.IsWhiteToMove;

        int PositionalEval()
        {
            // Positional Eval
            // ------------------------------------------------------------------------------------------------
            // Positional Mobility
            // a measure of the number of choices (legal moves) a player has in a given position. 
            int positionalEval = 0;
            int currentPieceIndex = -1,
                currentMoveCount = 0;
            int GetMobilityScore() => (int)Math.Sqrt(100 * currentMoveCount);

            foreach (Move move in board.GetLegalMoves())
            {
                if (move.MovePieceType == PieceType.Pawn || move.IsCastles)
                    continue;

                int fromIndex = move.StartSquare.Index;
                if (fromIndex != currentPieceIndex && currentPieceIndex != -1)
                {
                    positionalEval += GetMobilityScore();
                    currentMoveCount = 0;
                }
                currentMoveCount += move.IsCapture ? 2 : 1;
                currentPieceIndex = fromIndex;
            }
            positionalEval += GetMobilityScore();

            // Adjacent pawns connected score
            int PawnScore()
            {
                int pawnScore = 0;

                ulong pawnsBitboard = board.GetPieceBitboard(PAWN, IsWhiteToMove);
                foreach (Piece piece in board.GetPieceList(PAWN, IsWhiteToMove))
                {
                    ulong pawnAttacks = BitboardHelper.GetPieceAttacks(PAWN, piece.Square, board, IsWhiteToMove);
                    ulong defendedPawns = pawnAttacks & pawnsBitboard;

                    pawnScore += BitboardHelper.GetNumberOfSetBits(defendedPawns) * 100;
                    // Pawns ahead and connected would evaluate more
                    Square square = piece.Square;
                    pawnScore += (IsWhiteToMove ? square.Rank - 1 : 6 - square.Rank) * 10;
                }

                return pawnScore;
            }

            positionalEval += PawnScore();

            // King safety
            currentMoveCount = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(QUEEN, board.GetKingSquare(IsWhiteToMove), board));

            positionalEval -= GetMobilityScore();

            return positionalEval;
        }

        if (board.GetPieceList(PAWN, IsWhiteToMove).Count > 4)
        {
            eval += PositionalEval();
            board.ForceSkipTurn();
            eval -= PositionalEval();
            board.UndoSkipTurn();
        }

        return eval;
    }

    int GetPieceValue(PieceType type)
    {
        return PieceValue[(int)type];
    }

    int GetCaptureValue(Move move)
    {
        return GetPieceValue(move.CapturePieceType) - GetPieceValue(move.MovePieceType);
    }
}