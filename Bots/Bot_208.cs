namespace auto_Bot_208;
using ChessChallenge.API;
using System;

public class Bot_208 : IChessBot
{
    public struct MyMove
    {
        public Move move;
        public int score;

        public MyMove()
        {
            this.move = new Move();
            this.score = 0;
        }
    }
    int depthLimit = 2;
    bool earlyGame = true;
    public Move Think(Board board, Timer timer)
    {
        // Kickoff recursive minimax
        Move move = EvaluateMoves(board, 0).move;
        return move;
    }
    public MyMove EvaluateMoves(Board board, int depth)
    {
        MyMove bestMove = new MyMove();
        var legalMoves = board.GetLegalMoves();
        foreach (var move in legalMoves)
        {
            int effectiveDepthLimit = depthLimit;
            earlyGame = true;
            // Calculate whether or not it is early or late game depending on the number of enemy pieces and number of own pieces
            if (CountBits(board.IsWhiteToMove ? board.BlackPiecesBitboard : board.WhitePiecesBitboard) < 4
                && CountBits(!board.IsWhiteToMove ? board.BlackPiecesBitboard : board.WhitePiecesBitboard) > 3)
            {
                effectiveDepthLimit = depthLimit * 2;
                earlyGame = false;
            }

            // Score the currently selected move
            MyMove currentMove = earlyGame ? EvaluateMoveEarlyGame(move, board) : EvaluateMoveLateGame(move, board);

            if (depth < effectiveDepthLimit)
            {
                board.MakeMove(currentMove.move);
                if (board.IsInCheckmate())
                {
                    board.UndoMove(currentMove.move);
                    currentMove.score += 999;
                    return currentMove; // always immediately do checkmates
                }
                currentMove.score -= EvaluateMoves(board, depth + 1).score;
                board.UndoMove(currentMove.move);
            }

            if (currentMove.score > bestMove.score || bestMove.move.IsNull) bestMove = currentMove;
        }
        return bestMove;
    }
    public MyMove EvaluateMoveEarlyGame(Move move, Board board)
    {
        MyMove currentMove = new MyMove();
        currentMove.move = move;

        //Scoring
        {
            // EnPassant is Gigachad and needs to always happen if possible
            if (move.IsEnPassant) currentMove.score += 1000;
            // Capturing good
            if (move.IsCapture) currentMove.score += 50;
            // Only promote to Queen and do so if possible
            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) currentMove.score += 50;
            // Don't move king unless its a castle
            if (move.MovePieceType == PieceType.King && !move.IsCastles) currentMove.score -= 30;
            // Rook bias to balance center and forward bias
            if (move.MovePieceType == PieceType.Rook) currentMove.score += 1;
            // Moving the queen often is probably bad considering the AI will just blunder it but maybe I'll be surprised
            if (move.MovePieceType == PieceType.Queen) currentMove.score += 1;
            // Don't get captured
            if (board.SquareIsAttackedByOpponent(move.TargetSquare)) currentMove.score -= 5;
            // Check is good
            if (board.IsInCheck()) currentMove.score += 5;
            if (board.IsInCheckmate()) currentMove.score += 999; //(not as good as enpassant but i'd take it
            // Reward the AI for moving up the board (with diminishing returns)
            int targetRank = move.TargetSquare.Rank;
            if (!board.IsWhiteToMove) targetRank = 7 - targetRank;
            currentMove.score += Math.Min(targetRank, 6);
            // Reward the AI to take the center
            currentMove.score += (int)(8 - (2 * Math.Abs(3.5f - move.TargetSquare.File)));
        }
        return currentMove;
    }
    public MyMove EvaluateMoveLateGame(Move move, Board board)
    {
        MyMove currentMove = new MyMove();
        currentMove.move = move;

        //Scoring
        {
            if (board.IsInCheck()) currentMove.score += 200;
            if (board.IsInCheckmate()) currentMove.score += 9999;
            if (move.MovePieceType == PieceType.Queen) currentMove.score += 10;
            if (move.MovePieceType == PieceType.Rook) currentMove.score += 10;
            if (move.MovePieceType != PieceType.Pawn) currentMove.score += 1;

            var queenList = board.GetPieceList(PieceType.Queen, board.IsWhiteToMove);
            var rookList = board.GetPieceList(PieceType.Rook, board.IsWhiteToMove);
            // push pawns if there is not enough Queens and Rooks to easily checkmate
            if (queenList.Count + rookList.Count < 2) if (move.MovePieceType == PieceType.Pawn) currentMove.score += 50;

            if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen) currentMove.score += 100;
            if (board.IsRepeatedPosition()) currentMove.score -= 30;

        }
        return currentMove;
    }

    public static int CountBits(ulong value)
    {
        int count = 0;
        while (value != 0)
        {
            count++;
            value &= value - 1;
        }
        return count;
    }
}
