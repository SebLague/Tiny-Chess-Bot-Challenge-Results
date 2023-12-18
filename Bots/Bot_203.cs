namespace auto_Bot_203;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
// using System.Text;
// using System.IO;
public struct TranspositionEntry
{
    public int Depth; // The depth at which the entry was computed
    public int Score; // The score associated with the position
    public TranspositionType Type; // Type of entry (exact, lower bound, upper bound)
    public Move BestMove; // The best move found for this position
}
public enum TranspositionType
{
    Exact,
    LowerBound,
    UpperBound
}
public class Bot_203 : IChessBot
{
    // int maxDepth = 7; // Maximum search depth
    int startColor = 1; // 1 for White, -1 for Black
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    private Dictionary<ulong, TranspositionEntry> transpositionTable = new Dictionary<ulong, TranspositionEntry>();
    private string[] openingMoves = new string[] {
            "e2e4", // Your opening move 1: Move pawn from e2 to e4
            "d2d4", // Your opening move 2: Move pawn from d2 to d4
            "g1f3", // Your opening move 3: Move knight from g1 to f3
            "e7e5", // Respond to 1. e4 with 1... e5
            "d7d6", // Prepare for a flexible development
            "g8f6" // Develop the kingside knight
        };
    public Move Think(Board board, Timer timer)
    {
        // If you are playing Black, you need to negate the score
        if (!board.IsWhiteToMove)
            startColor = -1;
        // Check if we are still in the opening phase
        // DivertedConsole.Write("Opening move: " + board.PlyCount/2);
        if (board.PlyCount / 2 < openingMoves.Length / 2 || board.PlyCount / 2 < openingMoves.Length / 2)
        {
            return startColor == 1 ? new Move(openingMoves[board.PlyCount / 2], board) : new Move(openingMoves[board.PlyCount / 2 + 3], board); // Play the next opening move
        }
        // Call the Minimax algorithm with alpha-beta pruning
        // return MiniMax(board, maxDepth, int.MinValue, int.MaxValue, true).BestMove;


        // Implement iterative deepening with a time limit
        int currentDepth = 1;
        Move bestMove = Move.NullMove;

        // Set a time limit (in milliseconds) for searching
        // long timeLimitMs = timer.MillisecondsRemaining - 100; // Leave some time for processing
        long timeLimitMs = 200; // Leave some time for processing

        // while (currentDepth <= maxDepth)
        while (currentDepth < 7)
        {
            bestMove = MiniMax(board, currentDepth, int.MinValue, int.MaxValue, true).BestMove;

            // Check if we have exceeded the time limit
            if (timer.MillisecondsElapsedThisTurn >= timeLimitMs)
                break;

            // Increment the search depth
            currentDepth++;
        }

        if (bestMove == Move.NullMove)
        {
            var allMoves = board.GetLegalMoves();
            if (allMoves.Length > 0)
            {
                bestMove = allMoves[0]; // Just pick the first legal move as a default
            }
        }
        // DivertedConsole.Write("V4 Depth: " + currentDepth);
        return bestMove;
    }
    private (Move BestMove, int Score) MiniMax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        ulong zobristKey = board.ZobristKey;
        int alphaOrig = alpha; // Add this line to store the original alpha value
        if (transpositionTable.ContainsKey(zobristKey) && transpositionTable[zobristKey].Depth >= depth)
        {
            TranspositionEntry entry = transpositionTable[zobristKey];
            if (entry.Type == TranspositionType.Exact)
            {
                return (entry.BestMove, entry.Score);
            }
            else if (entry.Type == TranspositionType.LowerBound)
            {
                alpha = Math.Max(alpha, entry.Score);
            }
            else if (entry.Type == TranspositionType.UpperBound)
            {
                beta = Math.Min(beta, entry.Score);
            }
            if (alpha >= beta)
            {
                return (entry.BestMove, entry.Score);
            }
        }
        if (depth == 0 || board.IsInCheckmate() || board.IsInStalemate() || board.IsDraw() || board.IsFiftyMoveDraw())
        {
            // Evaluate the current board position
            return (Move.NullMove, EvaluateBoard(board));
        }
        Move[] allMoves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;
        int bestScore = maximizingPlayer ? int.MinValue : int.MaxValue;
        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            board.MakeMove(move);
            bool isMate = board.IsInCheckmate();
            board.UndoMove(move);
            if (isMate)
                return (move, maximizingPlayer ? int.MaxValue : int.MinValue);

            board.MakeMove(move);
            // Recursively evaluate the next position
            var (_, score) = MiniMax(board, depth - 1, alpha, beta, !maximizingPlayer);
            board.UndoMove(move);
            if (maximizingPlayer)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, score);
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
                beta = Math.Min(beta, score);
            }
            // Alpha-beta pruning
            if (beta <= alpha)
                break;
        }
        // Store the transposition table entry
        TranspositionType entryType;
        if (bestScore <= alphaOrig)
        {
            entryType = TranspositionType.UpperBound;
        }
        else if (bestScore >= beta)
        {
            entryType = TranspositionType.LowerBound;
        }
        else
        {
            entryType = TranspositionType.Exact;
        }
        var transpositionEntry = new TranspositionEntry
        {
            Depth = depth,
            Score = bestScore,
            Type = entryType,
            BestMove = bestMove
        };
        transpositionTable[zobristKey] = transpositionEntry;
        return (bestMove, bestScore);
    }
    private int EvaluateBoard(Board board)
    {
        int whiteMaterial = 0;
        int blackMaterial = 0;
        // Evaluate piece mobility and central control
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.PieceType == PieceType.Pawn)
                {
                    // Evaluate passed pawns
                    if (IsPassedPawn(piece, pieceList.IsWhitePieceList, board))
                    {
                        if (pieceList.IsWhitePieceList)
                            whiteMaterial += startColor * 100;
                        else
                            blackMaterial -= startColor * 100; // Opponent's passed pawns
                    }

                }
                if (piece.PieceType == PieceType.Pawn && (piece.Square.Rank == 2 || piece.Square.Rank == 7))
                    if (piece.IsWhite) whiteMaterial += 50;
                    else blackMaterial += 50; // Bonus for advanced pawns

                // Add piece value
                if (piece.IsWhite)
                {
                    // whiteMaterial += GetPieceValue(piece.PieceType);
                    whiteMaterial += pieceValues[(int)piece.PieceType];
                    // whiteMaterial += GetPiecePositionValue(board, piece);
                }
                else
                {
                    // blackMaterial += GetPieceValue(piece.PieceType);
                    blackMaterial += pieceValues[(int)piece.PieceType];
                    // blackMaterial += GetPiecePositionValue(board, piece);
                }
            }
        }

        int pawnScore = 0;
        foreach (PieceList pawnList in board.GetAllPieceLists())
        {
            foreach (Piece pawn in pawnList)
            {
                if (pawn.PieceType == PieceType.Pawn)
                {
                    // Evaluate advanced pawns
                    if (pawnList.IsWhitePieceList && pawn.Square.Rank >= 5)
                        // For White, advanced pawns are on rank 5 or higher
                        pawnScore += startColor * 20; // Bonus for advanced pawns
                    else if (!pawnList.IsWhitePieceList && pawn.Square.Rank <= 2)
                        // For Black, advanced pawns are on rank 2 or lower
                        pawnScore -= startColor * 20;

                }
            }
        }

        // Evaluate pawn structure
        whiteMaterial += startColor * pawnScore;
        blackMaterial -= startColor * pawnScore; // Opponent's pawn structure
                                                 // Return the material advantage (positive for White, negative for Black)
        return startColor * (whiteMaterial - blackMaterial);
    }


    private bool IsPassedPawn(Piece pawn, bool isWhite, Board board)
    {
        // int fileIndex = pawn.Square.File;
        // int rankIndex = pawn.Square.Rank;

        // Check if the pawn's file is empty in front of it
        if (isWhite)
        {
            for (int rank = pawn.Square.Rank + 1; rank < 8; rank++)
            {
                if (board.GetPiece(new Square(pawn.Square.File, rank)).PieceType != PieceType.None)
                {
                    return false; // There is a piece blocking the path
                }
            }
        }
        else
        {
            for (int rank = pawn.Square.Rank - 1; rank >= 0; rank--)
            {
                if (board.GetPiece(new Square(pawn.Square.File, rank)).PieceType != PieceType.None)
                {
                    return false; // There is a piece blocking the path
                }
            }
        }

        // Check adjacent files for enemy pawns
        // int[] adjacentFiles = { pawn.Square.File - 1, pawn.Square.File + 1 };
        foreach (int file in new[] { pawn.Square.File - 1, pawn.Square.File + 1 })
        {
            if (file >= 0 && file <= 7)
            {
                if (board.GetPiece(new Square(file, pawn.Square.Rank)).PieceType == PieceType.Pawn &&
                    board.GetPiece(new Square(file, pawn.Square.Rank)).IsWhite != isWhite)
                {
                    return false; // There is an enemy pawn on an adjacent file
                }
            }
        }

        return true; // It's a passed pawn
    }


}
