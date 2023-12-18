namespace auto_Bot_157;
// https://github.com/p-rivero/Turochamp-Chess-Challenge/blob/main/Chess-Challenge/src/My%20Bot/MyBot.cs

using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_157 : IChessBot
{
    private const PieceType
        PAWN = PieceType.Pawn,
        KNIGHT = PieceType.Knight,
        BISHOP = PieceType.Bishop,
        ROOK = PieceType.Rook,
        QUEEN = PieceType.Queen;

    private Board board;
    private Move bestMoveUnconfirmed, bestMoveConfirmed;
    private int bestScore, startDepth, timeToThink;
    private Timer timer;
    private int[,,] historyHeuristic;

    public Move Think(Board boardIn, Timer timerIn)
    {
        board = boardIn;
        timer = timerIn;
        // With more than 20 seconds left, think for 1 second (~40 moves)
        // Between 20 and 4 seconds, think for 0.5 seconds (~32 moves)
        // For the last 4 seconds, think for 0.1 seconds (~40 moves)
        timeToThink = timer.MillisecondsRemaining > 20_000 ? 1000 :
            timer.MillisecondsRemaining > 4_000 ? 500 : 100;

        try
        {
            for (startDepth = 1; ; startDepth++)
            {
                bestScore = -999_999;
                historyHeuristic = new int[2, 64, 64];

                int score = AlphaBetaSearch(startDepth, -999_999, 999_999);
                bestMoveConfirmed = bestMoveUnconfirmed;

                // Stop searching when checkmate is found
                if (score > 90_000)
                    return bestMoveConfirmed;
            }
        }
        catch (Exception)
        {
            // Timeout, return the previous best move
        }
        return bestMoveConfirmed;
    }

    private int AlphaBetaSearch(int depth, int alpha, int beta)
    {
        if (depth == 0)
            return QuiescenceSearch(alpha, beta);

        if (board.IsInCheckmate())
            return startDepth - depth - 100_000;

        if (board.IsDraw())
            return 0;

        // Check timeout
        if (depth == 3 && timer.MillisecondsElapsedThisTurn > timeToThink)
            throw new Exception();

        foreach (Move move in OrderMoves(board.GetLegalMoves()))
        {
            board.MakeMove(move);
            int score = -AlphaBetaSearch(depth - 1, -beta, -alpha),
                castlingIncentives = depth == startDepth ? TurochampCastlingIncentives(move) : 0;
            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;

                if (score >= beta)
                {
                    HistoryHeuristicRef(move) += depth * depth;
                    return beta;
                }

                score += castlingIncentives;
                if (depth == startDepth && score > bestScore)
                {
                    bestScore = score;
                    bestMoveUnconfirmed = move;
                }
            }
        }
        return alpha;
    }

    private int QuiescenceSearch(int alpha, int beta)
    {
        int standScore = TurochampEvaluate();

        if (standScore >= beta)
            return beta;

        if (standScore > alpha)
            alpha = standScore;

        foreach (Move move in OrderMoves(board.GetLegalMoves(true)))
        {
            board.MakeMove(move);
            int score = -QuiescenceSearch(-beta, -alpha);
            board.UndoMove(move);

            if (score >= beta)
                return beta;

            alpha = Math.Max(alpha, score);
        }
        return alpha;
    }


    private int TurochampEvaluate()
    {
        int MaterialScoreForColor(bool whiteColor)
        {
            var MaterialScoreForPiece = (PieceType pieceType) => board.GetPieceList(pieceType, whiteColor).Count * TurochampPieceMaterialValue(pieceType);
            return MaterialScoreForPiece(PAWN)
                + MaterialScoreForPiece(KNIGHT)
                + MaterialScoreForPiece(BISHOP)
                + MaterialScoreForPiece(ROOK)
                + MaterialScoreForPiece(QUEEN);
        }

        int PositionalScoreForCurrentPlayer()
        {
            int positionalScore = 0;
            var nonPawnDefenders = NumberOfNonPawnDefenders();
            var pawnDefenders = NumberOfPawnDefenders();

            // Mobility score (rules 1, 3): use the fact that moves are grouped by piece
            int currentPieceIndex = -1,
                currentMoveCount = 0;
            var FlushMobilityScore = () => (int)Math.Sqrt(10_000 * currentMoveCount); // 100 * sqrt(numMoves)
            foreach (Move move in board.GetLegalMoves())
            {
                if (move.MovePieceType == PAWN || move.IsCastles)
                    continue;

                int fromIndex = move.StartSquare.Index;
                if (fromIndex != currentPieceIndex && currentPieceIndex != -1)
                {
                    positionalScore += FlushMobilityScore();
                    currentMoveCount = 0;
                }
                currentMoveCount += move.IsCapture ? 2 : 1;
                currentPieceIndex = fromIndex;
            }
            positionalScore += FlushMobilityScore();

            // Piece safety (rule 2)
            void AddPieceSafetyScoreNonPawn(PieceType pieceType)
            {
                ForEachPieceOfPlayerToMove(pieceType, piece =>
                {
                    int index = piece.Square.Index,
                        defenders = nonPawnDefenders[index] + pawnDefenders[index];
                    positionalScore += defenders > 1 ? 150 : defenders > 0 ? 100 : 0; // 1 point if defended, 1.5 points if defended 2+ times
                });
            }
            AddPieceSafetyScoreNonPawn(ROOK);
            AddPieceSafetyScoreNonPawn(BISHOP);
            AddPieceSafetyScoreNonPawn(KNIGHT);

            // King safety (rule 4)
            currentMoveCount = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(QUEEN, board.GetKingSquare(IsWhiteToMove), board));
            positionalScore -= FlushMobilityScore();


            // Pawn credit (rule 6)
            ForEachPieceOfPlayerToMove(PAWN, piece =>
            {
                Square square = piece.Square;
                positionalScore += (IsWhiteToMove ? square.Rank - 1 : 6 - square.Rank) * 20 // 0.2 points for each rank advanced
                                + (nonPawnDefenders[square.Index] > 0 ? 30 : 0); // 0.3 points if defended by a non-pawn
            });

            // Mates and checks (rule 7) is not implemented (see README.md)

            return positionalScore;
        }

        int scoreCp = MaterialScoreForColor(IsWhiteToMove) - MaterialScoreForColor(!IsWhiteToMove) + PositionalScoreForCurrentPlayer();
        board.ForceSkipTurn();
        scoreCp -= PositionalScoreForCurrentPlayer();
        board.UndoSkipTurn();
        return scoreCp;
    }

    private int TurochampCastlingIncentives(Move move)
    {
        // Castling (rule 5)
        // We don't need to play the move, this function is called from AlphaBetaSearch when the move has already been played

        // Existing implementations do stack the modifiers. See README.md
        if (move.IsCastles)
            return 300;

        bool playerOfMove = !IsWhiteToMove; // Currently it's the opponent's turn
        if (!board.HasKingsideCastleRight(playerOfMove) && !board.HasKingsideCastleRight(playerOfMove))
            // Since IsCastles = false, this move loses castling rights (and it must have been a king or rook move).
            // If we had already lost castling rights, this function always returns 0 for all moves, so no move has priority.
            return 0;

        // We can castle. See if we can castle in the next turn
        board.ForceSkipTurn();
        foreach (Move nextMove in board.GetLegalMoves())
            if (nextMove.IsCastles)
            {
                board.UndoSkipTurn();
                return 200;
            }

        // We can castle, but not in the next turn.
        board.UndoSkipTurn();
        return 100;
    }

    private int TurochampPieceMaterialValue(PieceType pieceType) => pieceType switch
    {
        PAWN => 200,
        KNIGHT => 600,
        BISHOP => 700,
        ROOK => 1000,
        QUEEN => 2000,
        _ => 0,
    };

    private void AddDefendersForPiece(PieceType pieceType, ref int[] defenders)
    {
        foreach (Piece piece in board.GetPieceList(pieceType, IsWhiteToMove))
        {
            ulong bitboard = BitboardHelper.GetPieceAttacks(pieceType, piece.Square, board, IsWhiteToMove);
            while (bitboard != 0)
                defenders[BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard)]++;
        }
    }

    private int[] NumberOfNonPawnDefenders()
    {
        var defenders = new int[64];
        var AddDefenders = (PieceType pieceType) => AddDefendersForPiece(pieceType, ref defenders);
        AddDefenders(KNIGHT);
        AddDefenders(BISHOP);
        AddDefenders(ROOK);
        AddDefenders(QUEEN);
        return defenders;
    }

    private int[] NumberOfPawnDefenders()
    {
        var defenders = new int[64];
        AddDefendersForPiece(PAWN, ref defenders);
        return defenders;
    }

    private void ForEachPieceOfPlayerToMove(PieceType pieceType, Action<Piece> callback)
    {
        foreach (Piece piece in board.GetPieceList(pieceType, IsWhiteToMove))
            callback(piece);
    }

    private bool IsWhiteToMove => board.IsWhiteToMove;

    private ref int HistoryHeuristicRef(Move move) => ref historyHeuristic[IsWhiteToMove ? 0 : 1, move.StartSquare.Index, move.TargetSquare.Index];

    private IEnumerable<Move> OrderMoves(Move[] moves) =>
        moves.Select(move =>
        {
            int score = HistoryHeuristicRef(move);
            if (move.IsCapture)
                score += 100_000 + TurochampPieceMaterialValue(move.CapturePieceType) * 4 - TurochampPieceMaterialValue(move.MovePieceType);

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                score -= 50;

            if (move.IsPromotion)
                score += 10_000;

            return (move, score);
        })
        .OrderByDescending(x => x.score)
        .Select(x => x.move);
}