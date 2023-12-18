namespace auto_Bot_375;
using ChessChallenge.API;
using System;

public class Bot_375 : IChessBot
{
    private int MaxDepth = 3;
    private string[] moves = { "d2d4", "g1f3", "c1f4" };
    private string[] otherMoves = { "d7d5", "g8f6", "c8f5" };
    private int startMoveCount = 0;

    public Move Think(Board board, Timer timer)
    {
        bool earlyGame = board.PlyCount < 6 ? true : false;
        if (earlyGame && startMoveCount < 6)
        {
            return EarlyGame(board, timer);
        }
        else
        {
            return MidGame(board, timer);
        }
    }

    public Move EarlyGame(Board board, Timer timer)
    {
        Move move = Move.NullMove;
        if (board.IsWhiteToMove)
        {
            move = new Move(moves[startMoveCount], board);
            Board clonedBoard = Board.CreateBoardFromFEN(board.GetFenString());
            clonedBoard.MakeMove(move);
            //if (!board.GetLegalMoves().Contains(move) || EvaluateBoard(clonedBoard, board.IsWhiteToMove) < EvaluateBoard(board, board.IsWhiteToMove)+5){
            //    move = MidGame(board, timer);
            //    startMoveCount += 8;
            //}
        }
        else
        {
            move = new Move(otherMoves[startMoveCount], board);
            Board clonedBoard = Board.CreateBoardFromFEN(board.GetFenString());
            clonedBoard.MakeMove(move);
            //if (!board.GetLegalMoves().Contains(move) || EvaluateBoard(clonedBoard, !board.IsWhiteToMove) < EvaluateBoard(board, !board.IsWhiteToMove)+5){
            //    move = MidGame(board, timer);
            //    startMoveCount += 8;
            //}
        }
        startMoveCount++;
        return move;
    }

    public Move MidGame(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        int bestScore = -100;
        Move bestMove = Move.NullMove;
        if (timer.MillisecondsRemaining < 5000)
        {
            MaxDepth = 2;
        }

        foreach (Move move in moves)
        {
            Board clonedBoard = Board.CreateBoardFromFEN(board.GetFenString());
            clonedBoard.MakeMove(move);

            int score = Minimax(clonedBoard, MaxDepth, int.MinValue, int.MaxValue, false);
            bool canPlay = true;
            if (clonedBoard.IsDraw() || clonedBoard.IsInCheckmate())
            {
                canPlay = false;
            }
            if (score > bestScore && canPlay)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        return bestMove;
    }

    private int Minimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            // Calculate and return the static evaluation of the current board state
            return EvaluateBoard(board, !board.IsWhiteToMove);
        }

        Move[] moves = board.GetLegalMoves();
        if (maximizingPlayer)
        {
            int maxEval = int.MinValue;
            foreach (Move move in moves)
            {
                Board clonedBoard = Board.CreateBoardFromFEN(board.GetFenString());
                clonedBoard.MakeMove(move);
                int eval = Minimax(clonedBoard, depth - 1, alpha, beta, false);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                    break; // Beta cutoff (alpha-beta pruning)
            }
            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in moves)
            {
                Board clonedBoard = Board.CreateBoardFromFEN(board.GetFenString());
                clonedBoard.MakeMove(move);
                int eval = Minimax(clonedBoard, depth - 1, alpha, beta, true);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                    break; // Alpha cutoff (alpha-beta pruning)
            }
            return minEval;
        }
    }

    private int EvaluateBoard(Board board, bool isWhite)
    {
        // Simple evaluation function that just counts the material difference
        int whiteMaterial = CalculateMaterial(board, false);
        int blackMaterial = CalculateMaterial(board, true);
        if (isWhite)
        {
            return whiteMaterial - blackMaterial;
        }
        else
        {
            return blackMaterial - whiteMaterial;
        }
    }

    private int minimum(int a, int b)
    {
        return a < b ? a : b;
    }

    private int CalculateMaterial(Board board, bool isWhite)
    {
        int pawnValue = 4;
        int knightValue = 12;
        int bishopValue = 12;
        int rookValue = 20;
        int queenValue = 60;
        int kingValue = 90;

        int material = 0;
        PieceList[] allPieceLists = board.GetAllPieceLists();
        int colorIndex = isWhite ? 0 : 6;
        int reverseColorIndex = isWhite ? 6 : 0;

        bool endgame = false;
        if (pieceCount(allPieceLists, reverseColorIndex) <= 3)
        {
            endgame = true;
        }
        bool myEndgame = false;
        if (pieceCount(allPieceLists, colorIndex) <= 3)
        {
            myEndgame = true;
        }

        //Get distance to corner
        int kingPosX = allPieceLists[reverseColorIndex + 5][0].Square.File;
        int kingPosY = allPieceLists[reverseColorIndex + 5][0].Square.Rank;
        int distanceToCorner = minimum(kingPosX, 8 - kingPosX) + minimum(kingPosY, 8 - kingPosY);
        if (endgame)
        {
            material += 4 - distanceToCorner;
        }
        else
        {
            material += 8 - kingPosY;
        }

        int akingPosX = allPieceLists[colorIndex + 5][0].Square.File;
        int akingPosY = allPieceLists[colorIndex + 5][0].Square.Rank;
        int adistanceToCorner = minimum(akingPosX, 8 - akingPosX) + minimum(akingPosY, 8 - akingPosY);
        if (myEndgame)
        {
            material += adistanceToCorner;
        }
        else
        {
            material += 8 - kingPosY;
        }

        foreach (Piece piece in allPieceLists[colorIndex])
        {
            material += piece.Square.Rank / 3;
        }

        material += -allPieceLists[colorIndex + 5][0].Square.Rank;

        material += allPieceLists[colorIndex].Count * pawnValue;
        material += allPieceLists[colorIndex + 1].Count * knightValue;
        material += allPieceLists[colorIndex + 2].Count * bishopValue;
        material += allPieceLists[colorIndex + 3].Count * rookValue;
        material += allPieceLists[colorIndex + 4].Count * queenValue;
        material += allPieceLists[colorIndex + 5].Count * kingValue;
        if (board.IsDraw() || board.IsInCheckmate() || board.IsRepeatedPosition())
        {
            material -= -100;
        }
        return material;
    }

    private int pieceCount(PieceList[] allPieceLists, int colorIndex)
    {
        int total = 0;
        total += allPieceLists[colorIndex].Count;
        total += allPieceLists[colorIndex + 1].Count;
        total += allPieceLists[colorIndex + 2].Count;
        total += allPieceLists[colorIndex + 3].Count;
        total += allPieceLists[colorIndex + 4].Count;
        return total;
    }
}