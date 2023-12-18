namespace auto_Bot_481;
using ChessChallenge.API;

public class Bot_481 : IChessBot
{
    int thoughtDepth = 1;

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 330, 500, 900, 10000 };

    // Lookup for Piece Square Tables
    int[,] PSTableLookup =
    {
        { // Pawn Table
            0,  0,  0,  0,
            5, 10, 10,-20,
            5, -5,-10,  0,
            0,  0,  0, 20,
            5,  5, 10, 25,
           10, 10, 20, 30,
           50, 50, 50, 50,
            0,  0,  0,  0
        },
        { // Knight Table
            -50,-40,-30,-30,
            -40,-20,  0,  5,
            -30,  5, 10, 15,
            -30,  0, 15, 20,
            -30,  5, 15, 20,
            -30,  0, 10, 15,
            -40,-20,  0,  0,
            -50,-40,-30,-30
        },
        { // Bishop Table
            -20,-10,-10,-10,
            -10,  5,  0,  0,
            -10, 10, 10, 10,
            -10,  0, 10, 10,
            -10,  5,  5, 10,
            -10,  0,  5, 10,
            -10,  0,  0,  0,
            -20,-10,-10,-10
        },
        { // Rook Table
            0,  0,  0,  5,
           -5,  0,  0,  0,
           -5,  0,  0,  0,
           -5,  0,  0,  0,
           -5,  0,  0,  0,
           -5,  0,  0,  0,
            5, 10, 10, 10,
            0,  0,  0,  0
        },
        { // Queen Table
            -20,-10,-10, -5,
            -10,  0,  0,  0,
            -10,  5,  5,  5,
              0,  0,  5,  5,
             -5,  0,  5,  5,
            -10,  0,  5,  5,
            -10,  0,  0,  0,
            -20,-10,-10, -5
        },
        { // King Table
             20, 30, 10,  0,
             20, 20,  0,  0,
            -10,-20,-20,-20,
            -20,-30,-30,-40,
            -30,-40,-40,-50,
            -30,-40,-40,-50,
            -30,-40,-40,-50,
            -30,-40,-40,-50
        }
    };

    // Record Last Move
    Square lastFrom;
    Square lastTo;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Move bestMove = moves[0];
        int bestScore = int.MinValue;

        // Think through every move
        foreach (Move move in moves)
        {
            // Always go for checkmate
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            // Initialize Score
            int score = EvaluateScore(board, move);

            // Subtract Opponents Best Possible Move
            board.MakeMove(move);
            score += thinkForward(board, 1);
            board.UndoMove(move);

            // Compare Against Best Move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

        }

        lastFrom = bestMove.StartSquare;
        lastTo = bestMove.TargetSquare;
        return bestMove;
    }

    // Test if move gives checkmate (From EvilBot.cs)
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    // Check if Board is in Endgame
    bool BoardIsEndgame(Board board, bool botIsWhite)
    {
        int pieceCount = 0;

        int idx = 0;
        int end = 0;
        if (!botIsWhite)
        {
            idx = 6;
            end = -6;
        }

        PieceList[] pieces = board.GetAllPieceLists();

        for (; idx < pieces.Length + end; ++idx)
        {
            pieceCount += pieces[idx].Count;
        }

        return pieceCount <= 4;
    }

    int thinkForward(Board board, int depth)
    {

        // End Search If Below Thought Depth
        if (depth > thoughtDepth)
            return 0;

        bool isMyTurn = depth % 2 == 0;

        Move[] moves = board.GetLegalMoves();

        if (moves.Length == 0)
            return 0;


        Move bestMove = moves[0];
        int bestScore = int.MinValue;

        // Think through every move
        foreach (Move move in moves)
        {
            // Always go for checkmate
            if (MoveIsCheckmate(board, move))
                return 1000;

            // Initialize Score
            int score = EvaluateScore(board, move);

            // Subtract Opponents Best Possible Move
            board.MakeMove(move);
            score += thinkForward(board, depth + 1);
            board.UndoMove(move);

            // Compare Against Best Move
            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }

        if (!isMyTurn)
            bestScore *= -1;

        return bestScore;

    }

    int EvaluateScore(Board board, Move move)
    {
        int score = 0;

        int capturedType = (int)move.CapturePieceType;

        // Penalize Moving Back To Last Location
        if (move.StartSquare == lastFrom && move.TargetSquare == lastTo)
            score -= 200;

        // Change Behaviour For Endgame 
        // Non-Endgame Behaviour
        if (!BoardIsEndgame(board, board.IsWhiteToMove))
        {
            // Add Score for Captured Piece
            score += pieceValues[capturedType];

            // Get Table Index from Position
            int tableIdx = move.TargetSquare.Index;

            if (board.IsWhiteToMove)
                tableIdx = 63 - tableIdx;


            int x = tableIdx % 8;
            int y = tableIdx / 8;

            // Clamp x to 0-3 
            if (x > 3)
            {
                x -= 7;
                x *= -1;
            }

            // Add Score for Position after move
            if (capturedType != 0)
            {
                score += PSTableLookup[capturedType - 1, y * 4 + x];
            }

            // Subtract Score if Moving King
            if (capturedType == 6)
            {
                score -= 300;
            }
        }
        else // Endgame Behaviour
        {
            // Add Score for Captured Piece
            score += pieceValues[capturedType];

            // Get Opponent King
            Piece opKing = board.GetPieceList(PieceType.King, !board.IsWhiteToMove)[0];

            // Encourage Movement Towards Opponent King
            int rankDistance = move.TargetSquare.Rank - opKing.Square.Rank;
            int fileDistance = move.TargetSquare.File - opKing.Square.File;

            if (rankDistance < 0)
                rankDistance *= -1;

            if (fileDistance < 0)
                fileDistance *= -1;

            int distance = fileDistance;

            if (rankDistance > fileDistance)
                distance = rankDistance;

            score += 100 - distance * 10;
        }

        return score;
    }
}