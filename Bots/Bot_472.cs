namespace auto_Bot_472;
using ChessChallenge.API;
using System;

public class Bot_472 : IChessBot
{
    Board simBoard;
    bool isWhite = false;
    public Move Think(Board board, Timer timer)
    {
        simBoard = board;
        isWhite = board.IsWhiteToMove;
        Move[] moves = board.GetLegalMoves();

        Move bestMove = moves[0];
        float bestEval = 0;
        foreach (Move move in moves)
        {
            float eval = Eval(move, 0, 3);
            if (isWhite)
            {
                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }
            else
            {
                if (eval < bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
            }
        }
        DivertedConsole.Write(bestEval + " " + bestMove.MovePieceType);

        return bestMove;
    }

    private float Eval(Move inputMove, float prevEval, int depth)
    {
        bool botTurn = isWhite ^ !simBoard.IsWhiteToMove;
        float moveEval = GetMoveEval(inputMove) * 0.5f; // Weighted to prioritize position eval over single move eval
        moveEval *= simBoard.IsWhiteToMove ? 1 : -1;

        if (depth <= 0) return moveEval;
        if (simBoard.IsRepeatedPosition()) return simBoard.IsWhiteToMove ? -20 : 20;

        simBoard.MakeMove(inputMove);
        botTurn = !botTurn;
        float bestEval = prevEval;
        Move[] moves = simBoard.GetLegalMoves();
        float posEval = GetPositionEval();

        if (moves == null || moves.Length == 0)
        {
            simBoard.UndoMove(inputMove);
            return 0;
        }

        if (botTurn && simBoard.IsInCheckmate()) return simBoard.IsWhiteToMove ? float.PositiveInfinity : float.NegativeInfinity;

        foreach (Move move in moves)
        {
            float eval = Eval(move, posEval, depth - 1);
            if (simBoard.IsWhiteToMove)
            {
                if (eval < bestEval)
                    bestEval = eval;
            }
            else
            {
                if (eval > bestEval)
                    bestEval = eval;
            }
        }
        simBoard.UndoMove(inputMove);
        // Add some randomness to the first 3 moves
        if (simBoard.GameMoveHistory.Length < 3)
        {
            Random r = new Random();
            bestEval += r.NextSingle() * 4 - 2;
        }
        return bestEval + posEval + moveEval + prevEval;
    }

    private float GetMoveEval(Move move)
    {
        if (move.IsEnPassant) return float.PositiveInfinity;        // Holy hell

        float eval = 0;

        PieceType movePiece = move.MovePieceType;
        PieceType capturePiece = move.CapturePieceType;
        PieceType promotionPiece = move.PromotionPieceType;

        if (simBoard.SquareIsAttackedByOpponent(move.TargetSquare))
        {
            if (movePiece == PieceType.Queen) eval -= 9;
            if (movePiece == PieceType.Rook) eval -= 5;
            if (movePiece == PieceType.Bishop) eval -= 3;
            if (movePiece == PieceType.Knight) eval -= 3;
            if (movePiece == PieceType.Pawn) eval -= 1;
            eval *= 2;
        }

        if (move.IsCapture)
        {
            if (capturePiece == PieceType.King) eval += float.PositiveInfinity;
            if (capturePiece == PieceType.Queen) eval += 9;
            if (capturePiece == PieceType.Rook) eval += 5;
            if (capturePiece == PieceType.Bishop) eval += 3;
            if (capturePiece == PieceType.Knight) eval += 3;
            if (capturePiece == PieceType.Pawn) eval += 1;
            // Weights to prioritize capturing with weaker pieces
            if (movePiece == PieceType.Knight) eval *= 0.8f;
            if (movePiece == PieceType.Bishop) eval *= 0.8f;
            if (movePiece == PieceType.Rook) eval *= 0.65f;
            if (movePiece == PieceType.Queen) eval *= 0.5f;
        }
        if (move.IsPromotion)
        {
            if (promotionPiece == PieceType.Queen) eval += 9;
            if (promotionPiece == PieceType.Rook) eval += 5;
            if (promotionPiece == PieceType.Bishop) eval += 3;
            if (promotionPiece == PieceType.Knight) eval += 3;
        }
        if (movePiece == PieceType.Pawn)
        {
            float pawnBias = 0;
            if (move.TargetSquare.Rank == (simBoard.IsWhiteToMove ? 4 : 3))
            {
                if (move.StartSquare.File == 3 || move.StartSquare.File == 4)
                {
                    pawnBias += 1.0f;
                }
                else
                {
                    pawnBias += 0.5f;
                }
            }
            if (Math.Abs(move.StartSquare.Rank - move.TargetSquare.Rank) == 2) pawnBias += 0.1f;

            eval += pawnBias;
        }
        if (move.IsCastles)
            eval += 3;
        else if (movePiece == PieceType.King)
        {
            eval -= 20;
        }

        return eval;
    }

    private float GetPositionEval()
    {
        float eval = 0;
        string s = simBoard.GetFenString();
        if (simBoard.IsInCheck()) eval += simBoard.IsWhiteToMove ? -2.5f : 2.5f;
        foreach (char c in s)
        {
            if (c == 'Q') eval += 9;
            if (c == 'q') eval -= 9;

            if (c == 'R') eval += 5;
            if (c == 'r') eval -= 5;

            if (c == 'N') eval += 3;
            if (c == 'n') eval -= 3;
            if (c == 'B') eval += 3;
            if (c == 'b') eval -= 3;
            if (c == 'P') eval += 1;
            if (c == 'p') eval -= 1;
        }

        return eval;
    }
}