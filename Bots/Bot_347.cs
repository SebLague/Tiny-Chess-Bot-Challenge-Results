namespace auto_Bot_347;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_347 : IChessBot
{
    int isWhite = 1;

    public Move Think(Board board, Timer timer)
    {
        // Determine if the bot is playing as white or black.
        isWhite = board.IsWhiteToMove ? 1 : -1;

        // Order available moves and set the search depth based on time remaining.
        var moves = OrderMoves(board.GetLegalMoves());
        int depth, remainingTime = timer.MillisecondsRemaining;

        if (remainingTime > 90000 && board.GameMoveHistory.Length > 8)
            depth = 3;
        else if (remainingTime > 5000)
            depth = 2;
        else
            depth = 1;

        // Find and return the best move.
        return GetBestMoves(board, moves, 1, depth)[0];
    }

    Move[] OrderMoves(Move[] moves)
    {
        // Initialize the scores for each move.
        var moveScores = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            var move = moves[i];

            // Prioritize captures and promotions first.
            if (move.IsCapture || move.IsPromotion)
                moveScores[i] = -1000;
            else
                moveScores[i] = 0;
        }

        // Sort the moves based on their scores in descending order.
        Array.Sort(moveScores, moves);

        return moves;
    }

    Move[] GetBestMoves(Board board, Move[] movesToTry, int movesToReturn, int depth)
    {
        Queue<Move> moves = new();
        CustomMove[] m = ComputeMoves(board, depth, movesToTry);

        int curBest = -15000;
        for (int i = 0; i < m.Length; i++)
        {

            int v = GetBestValueMove(m[i], -1);
            if (curBest < v)
                curBest = v;

            if (curBest == v)
            {
                // Keep track of the best moves within the specified limit (movesToReturn).
                if (movesToReturn <= moves.Count)
                    moves.Dequeue();

                moves.Enqueue(movesToTry[i]);
            }

        }
        return moves.ToArray();
    }

    int GetBestValueMove(CustomMove move, int botToPlay, int alpha = -15000, int beta = 15000)
    {
        int curBest = -15000 * botToPlay;
        for (int i = 0; i < move.childMoves.Count; i++)
        {
            int? curValue = move.childMoves[i].moveValue;
            curValue ??= GetBestValueMove(move.childMoves[i], -botToPlay, alpha, beta); //if curvalue is null

            // Update the best value based on the bot's perspective.
            if (curBest < curValue && botToPlay == 1 || curBest > curValue && botToPlay == -1)
                curBest = (int)curValue;

            // Alpha-beta pruning.
            if (botToPlay == 1)
                alpha = Math.Max(alpha, (int)curValue);
            else if (botToPlay == -1)
                beta = Math.Min(beta, (int)curValue);

            if (alpha >= beta)
                break;
        }


        return curBest;
    }

    CustomMove[] ComputeMoves(Board board, int depth, Move[] movesToTry)
    {
        CustomMove[] moves = new CustomMove[movesToTry.Length];
        CustomMove? parentMove = null;
        for (int i = 0; i < movesToTry.Length; i++)
            moves[i] = FindNextMove(board, ref parentMove, movesToTry[i], depth);


        return moves;
    }

    CustomMove FindNextMove(Board board, ref CustomMove parent, Move move, int depth)
    {
        CustomMove customMove = new CustomMove(ref parent);
        board.MakeMove(move);
        Move[] movesToTry = OrderMoves(board.GetLegalMoves());
        if (depth > 0 && (!board.IsDraw() || !board.IsInCheckmate()))
            for (int i = 0; i < movesToTry.Length; i++)
                customMove.childMoves.Add(FindNextMove(board, ref customMove, movesToTry[i], depth - 1));
        else
            if (board.IsDraw())
            customMove.moveValue = 0;
        else if (board.IsInCheckmate())
            customMove.moveValue = (board.IsWhiteToMove ? -1 : 1) * 15000;
        else
            customMove.moveValue = GetMaterialValue(movesToTry, board.GetAllPieceLists(), isWhite);

        board.UndoMove(move);
        return customMove;
    }

    // Calculates the material value of the current board position.
    // This function assigns a value to each piece type and evaluates their positions on the board.
    // The function takes into account the number of pawns, knights, bishops, rooks, queens, and kings on the board.
    // It also considers factors like pawn structure, piece mobility, and whether the pieces belong to the bot or the opponent.
    private int GetMaterialValue(Move[] legalMoves, PieceList[] pieces, int white)
    {
        int value = 0, whitePawns = 0, blackPawns = 0, totalCount = 0;
        for (int i = 0; i < pieces.Length; i++)
        {
            var curPieces = pieces[i];

            int tempValue = 0, curPiecesCount = curPieces.Count, multiplier = -1;

            // Determine if the pieces are white or black based on the 'white' parameter.
            if (curPieces.IsWhitePieceList == (white == 1))
                multiplier = 1;

            switch (curPieces.TypeOfPieceInList)
            {
                case PieceType.Pawn:
                    if (curPieces.IsWhitePieceList)
                        whitePawns = i;
                    else
                        blackPawns = i;
                    tempValue += curPiecesCount * 10;
                    int[] files = new int[8];
                    foreach (Piece pawn in curPieces)
                    {
                        files[pawn.Square.File]++;
                        if (pawn.IsWhite)
                            tempValue += pawn.Square.Rank - 1;
                        else
                            tempValue += 6 - pawn.Square.Rank;
                    }
                    for (int j = 0; j < files.Length; j++)
                    {
                        if (files[j] > 1)
                            tempValue -= (files[j] - 1) * 5;

                        if (j > 0 && j < files.Length - 1)
                            if (files[j] >= 1 && files[j - 1] == 0 && files[j + 1] == 0)
                                tempValue -= 5;
                    }
                    break;
                case PieceType.Knight:
                    tempValue += curPiecesCount * 29;
                    break;
                case PieceType.Bishop:
                    tempValue += curPiecesCount * 30;
                    break;
                case PieceType.Rook:
                    tempValue += curPiecesCount * 50;
                    break;
                case PieceType.Queen:
                    tempValue += curPiecesCount * 86;
                    break;
                case PieceType.King:
                    tempValue += curPiecesCount * 10000;
                    var sq = curPieces[0].Square;
                    if (sq.Rank == 0)
                        if (sq.File == 2 || sq.File == 6)
                            tempValue += 3;
                        else if (sq.Rank == 7)
                            if (sq.File == 1 && sq.File == 5)
                                tempValue += 3;
                    break;
            }

            // Evaluate the mobility of each piece by counting the number of legal moves it has.
            foreach (Piece piece in curPieces)
                tempValue += legalMoves.Where(mv => !piece.IsKing && !piece.IsPawn && mv.StartSquare == piece.Square).Count();

            totalCount += curPiecesCount;

            // Add the piece's value to the total value, considering whether it belongs to the bot or the opponent.
            value += tempValue * multiplier;
        }

        // Adjust the total value based on the total number of pieces on the board.
        if (value > 0)
            value -= totalCount / 10;
        else
            value += totalCount / 10;

        return value;
    }

}

// Class for custom moves
class CustomMove
{
    public List<CustomMove> childMoves = new(); // List of child moves.
    public int? moveValue; // The value associated with this move.
    public CustomMove? parent; // Reference to the parent move.

    public CustomMove(ref CustomMove _parent)
    {
        parent = _parent;
    }

    public CustomMove()
    {

    }
}