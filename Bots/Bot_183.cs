namespace auto_Bot_183;
using ChessChallenge.API;
using System;

public class Bot_183 : IChessBot
/*
{
// Piece values: null, pawn, knight, bishop, rook, queen, king
int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

public Move Think(Board board, Timer timer)
{
    Move[] allMoves = board.GetLegalMoves();

    // Pick a random move to play if nothing better is found
    Random rng = new();
    Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
    int highestValueCapture = 0;

    foreach (Move move in allMoves)
    {
        // Always play checkmate in one
        if (MoveIsCheckmate(board, move))
        {
            moveToPlay = move;
            break;
        }

        // Find highest value capture
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

        if (capturedPieceValue > highestValueCapture)
        {
            moveToPlay = move;
            highestValueCapture = capturedPieceValue;
        }
    }

    return moveToPlay;
}

// Test if this move gives checkmate
bool MoveIsCheckmate(Board board, Move move)
{
    board.MakeMove(move);
    bool isMate = board.IsInCheckmate();
    board.UndoMove(move);
    return isMate;
}
}*/


{
    const bool DEBUG = false;
    public Move Think(Board board, Timer timer)
    {
        int maxDepth = 4; // Set the maximum search depth
        Move[] legalMoves = board.GetLegalMoves();

        if (board.IsWhiteToMove)
            if (DEBUG) DivertedConsole.Write("I am playing as white.");
            else
            if (DEBUG) DivertedConsole.Write("I am playing as black.");

        if (DEBUG)
            DivertedConsole.Write("Thinking...");


        // Shuffle the legal moves array
        legalMoves = Shuffle(legalMoves);

        Move bestMove = legalMoves[0]; // Initialize with a random move
        bool amWhite = board.IsWhiteToMove; // Store whether we are playing as white
        int bestScore = amWhite ? int.MinValue : int.MaxValue; // Initialize best score based on player

        foreach (Move move in legalMoves)
        {
            if (DEBUG)
                DivertedConsole.Write($"Considering move: {move}");
            board.MakeMove(move); // A single board is used to simulate moves

            int score = MiniMax(board, maxDepth - 1, int.MinValue, int.MaxValue);

            if (DEBUG)
                DivertedConsole.Write($"Move: {move} Score: {score}");

            // Update best move and best score based on player
            if ((amWhite && score > bestScore) || (!amWhite && score < bestScore))
            {
                bestScore = score;
                bestMove = move;
            }

            board.UndoMove(move); // Restore the state of the board
        }

        if (DEBUG)
            DivertedConsole.Write($"Selected move: {bestMove} with score: {bestScore}");
        return bestMove;
    }


    private int MiniMax(Board board, int depth, int alpha, int beta)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            // Evaluate the position and return its value
            return Evaluate(board);
        }

        Move[] legalMoves = board.GetLegalMoves();
        int bestValue = board.IsWhiteToMove ? int.MinValue : int.MaxValue;

        foreach (Move move in legalMoves)
        {
            string indentation = new string(' ', 2 * (3 - depth)); // Create indentation string based on depth

            if (DEBUG)
                DivertedConsole.Write($"{indentation}Considering move: {move}");
            board.MakeMove(move);

            int value = MiniMax(board, depth - 1, alpha, beta);

            if (DEBUG)
                DivertedConsole.Write($"{indentation}Move: {move} Value: {value}");

            board.UndoMove(move);

            if (board.IsWhiteToMove)
            {
                bestValue = Math.Max(bestValue, value);
                alpha = Math.Max(alpha, value);
            }
            else
            {
                bestValue = Math.Min(bestValue, value);
                beta = Math.Min(beta, value);
            }

            if (beta <= alpha)
                break; // Alpha-beta pruning
        }


        if (DEBUG)
            DivertedConsole.Write($"Best value: {bestValue}");
        return bestValue;
    }

    private int Evaluate(Board board)
    {
        // Check if the position is a checkmate
        if (board.IsInCheckmate())
        {
            if (board.IsWhiteToMove)
                return -1000000;
            else
                return 1000000;
        }

        // Calculate the advantage of controlling the center of the board
        ulong centerMask = 0x00007C7C7C7C0000; // Mask for the center squares
        ulong whiteCenterPieces = board.WhitePiecesBitboard & centerMask;
        ulong blackCenterPieces = board.BlackPiecesBitboard & centerMask;
        int centerControlAdvantage = CountBits(whiteCenterPieces) - CountBits(blackCenterPieces);

        // Simple evaluation function - just count material balance
        int whiteMaterial = GetMaterialValue(board, true);
        int blackMaterial = GetMaterialValue(board, false);

        return whiteMaterial - blackMaterial + centerControlAdvantage;
    }

    private int GetMaterialValue(Board board, bool isWhite)
    {
        PieceList[] pieceLists = board.GetAllPieceLists();
        int totalMaterialValue = 0;

        // Iterate through the correct piece lists based on color
        int startIndex = isWhite ? 0 : 6;
        int endIndex = isWhite ? 6 : 12;

        for (int i = startIndex; i < endIndex; i++)
        {
            foreach (Piece piece in pieceLists[i])
            {
                // Add the material value of each piece to the total
                totalMaterialValue += GetPieceMaterialValue(piece.PieceType);
            }
        }

        return totalMaterialValue;
    }

    private int GetPieceMaterialValue(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                return 100;
            case PieceType.Knight:
                return 300;
            case PieceType.Bishop:
                return 300;
            case PieceType.Rook:
                return 500;
            case PieceType.Queen:
                return 900;
            default:
                return 0; // Default case for King or unknown piece type
        }
    }


    // Shuffle an array using Fisher-Yates algorithm
    private static T[] Shuffle<T>(T[] array)
    {
        Random rng = new Random();
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = array[k];
            array[k] = array[n];
            array[n] = value;
        }
        return array;
    }

    // Count the number of set bits (ones) in a ulong using Brian Kernighan's algorithm
    private int CountBits(ulong n)
    {
        int count = 0;
        while (n > 0)
        {
            n &= (n - 1);
            count++;
        }
        return count;
    }


}
