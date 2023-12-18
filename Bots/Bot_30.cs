namespace auto_Bot_30;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_30 : IChessBot
{
    private int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };

    private int _pawnMovesAtBeginning = new Random().Next(2, 4);
    private int _knightMovesAtBeginning = 2;
    private int _bishopMovesAtBeginning = 2;
    private bool _hasCastle;

    int AddUpPieces(Board board, bool white)
    {
        var pieces = 0;
        pieces += board.GetPieceList(PieceType.Pawn, white).Count * pieceValues[1];
        pieces += board.GetPieceList(PieceType.Knight, white).Count * pieceValues[2];
        pieces += board.GetPieceList(PieceType.Bishop, white).Count * pieceValues[3];
        pieces += board.GetPieceList(PieceType.Rook, white).Count * pieceValues[4];
        pieces += board.GetPieceList(PieceType.Queen, white).Count * pieceValues[5];

        return pieces;
    }

    int Evaluate(Board board)
    {
        var whitePieces = AddUpPieces(board, true);
        var blackPieces = AddUpPieces(board, false);

        return whitePieces - blackPieces;
    }


    Move GetBestMove(Board board, bool lookIntoFuture = true)
    {
        var moves = board.GetLegalMoves();
        var movesEval = new int[moves.Length];
        var isWhite = board.IsWhiteToMove;

        // Determine if it's the endgame (for example, when both sides have only a few pieces left)
        var isEndgame = AddUpPieces(board, true) + AddUpPieces(board, false) <= 4400;

        for (var i = 0; i < moves.Length; i++)
        {
            var move = moves[i];

            if (move.IsEnPassant)
                return move;

            if (MoveIsCheckmate(board, move))
                return move;

            // Find highest value capture
            var capturedPiece = board.GetPiece(move.TargetSquare);
            var capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];
            var movePiece = board.GetPiece(move.StartSquare);
            var movePieceValue = pieceValues[(int)movePiece.PieceType];

            if (capturedPiece.PieceType != PieceType.None && capturedPiece.IsWhite != movePiece.IsWhite)
                movesEval[i] += 40;


            // During the endgame, give a higher value to pawn moves
            if (isEndgame && movePiece.PieceType == PieceType.Pawn)
                movesEval[i] += 20; // Adjust the value as needed based on your evaluation
            else if (isEndgame && movePiece.PieceType == PieceType.Queen)
                movesEval[i] += 10;

            if (!isEndgame && !_hasCastle && movePiece.PieceType == PieceType.King)
                movesEval[i] -= 60;
            else if (!isEndgame && movePiece.PieceType == PieceType.King &&
                     move.TargetSquare.Rank != move.StartSquare.Rank)
                movesEval[i] -= 40;

            if (board.PlyCount < 20 && movePiece.IsRook)
                movesEval[i] -= 20;

            if (board.PlyCount < 20 && movePiece.Square.Rank == (isWhite ? 0 : 7) && _pawnMovesAtBeginning <= 0)
                movesEval[i] += 20;

            if (move.IsCastles)
                movesEval[i] += 60;
            if (move.IsPromotion)
                movesEval[i] += 65;

            if (move.TargetSquare.Rank < move.StartSquare.Rank)
                movesEval[i] -= 5;

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                movesEval[i] -= 2;

            board.MakeMove(move);
            var opponentMoves = board.GetLegalMoves();

            if (opponentMoves.Any(m => MoveIsCheckmate(board, m)))
            {
                movesEval[i] = -100_000;
            }

            if (board.SquareIsAttackedByOpponent(board.GetKingSquare(!isWhite)))
                movesEval[i] += 5;
            var eval = Evaluate(board);


            if (lookIntoFuture)
            {
                var bestMoveForOpponent = GetBestMove(board, false);
                board.MakeMove(bestMoveForOpponent);
                eval = Evaluate(board);
                if (bestMoveForOpponent.IsPromotion)
                    movesEval[i] -= 40;
                board.UndoMove(bestMoveForOpponent);
            }
            movesEval[i] += isWhite ? eval : -eval;

            board.UndoMove(move);
        }

        var bestMove = BestMove(movesEval, moves);
        return bestMove;
    }

    Move BestMove(int[] movesEval, Move[] moves, int count = 5)
    {
        var bestMoves = new List<int>();
        var maxEval = movesEval.Length != 0 ? movesEval.Max() : 0;

        for (var i = 0; i < movesEval.Length; i++)
        {
            if (movesEval[i] == maxEval)
                bestMoves.Add(i);
        }
        var random = new Random();
        if (_pawnMovesAtBeginning > 0 && bestMoves.Any(i => moves[i].MovePieceType == PieceType.Pawn))
        {
            bestMoves.RemoveAll(i => moves[i].MovePieceType != PieceType.Pawn);
        }
        else if (_knightMovesAtBeginning > 0 && bestMoves.Any(i => moves[i].MovePieceType == PieceType.Knight))
        {
            bestMoves.RemoveAll(i => moves[i].MovePieceType != PieceType.Knight);
        }
        else if (_bishopMovesAtBeginning > 0 && bestMoves.Any(i => moves[i].MovePieceType == PieceType.Bishop))
        {
            bestMoves.RemoveAll(i => moves[i].MovePieceType != PieceType.Bishop);
        }

        var randomIndex = random.Next(bestMoves.Count);
        var bestMoveIndex = 0;
        if (randomIndex < bestMoves.Count)
            bestMoveIndex = bestMoves[randomIndex];
        if (bestMoveIndex < moves.Length)
        {
            var bestMove = moves[bestMoveIndex];
            return bestMove;
        }

        if (count <= 0)
            return Move.NullMove;
        return BestMove(movesEval, moves, count - 1);
    }

    // bool CheckIfMateOnBoard(Board board)
    // {
    //     var moves = board.GetLegalMoves();
    //
    //     return moves.Any(move => MoveIsCheckmate(board, move));
    // }

    public Move Think(Board board, Timer timer)
    {
        var bestMove = GetBestMove(board);
        if (bestMove == Move.NullMove)
        {
            var legalMoves = board.GetLegalMoves();
            var rand = new Random();
            bestMove = legalMoves[rand.Next(legalMoves.Length)];
        }

        if (bestMove.MovePieceType == PieceType.Pawn) _pawnMovesAtBeginning--;
        else if (bestMove.MovePieceType == PieceType.Knight) _knightMovesAtBeginning--;
        else if (bestMove.MovePieceType == PieceType.Bishop) _bishopMovesAtBeginning--;

        if (bestMove.IsCastles) _hasCastle = true;

        return bestMove;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }
}