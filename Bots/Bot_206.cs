namespace auto_Bot_206;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_206 : IChessBot
{
    public List<Move> MoveHistory { get; set; }
    public Bot_206()
    {
        MoveHistory = new List<Move>();
    }

    public Move Think(Board board, Timer timer)
    {
        bool MinimizingPlayer = !board.IsWhiteToMove;
        Move[] legalMoves = board.GetLegalMoves();
        Move bestMove = legalMoves[0];
        double worstScore = MinimizingPlayer ? 1000000 : -1000000;
        double bestScore = worstScore;

        //depth has to be an even number or it will not account for opponent repetitions and lead to draws by repetition in winning positions :(
        int depth = timer.MillisecondsRemaining < 30000 || board.GetAllPieceLists().Sum(list => list.Count) >= 26 && timer.MillisecondsRemaining < 180000 || board.PlyCount < 16 ? 2 : 4;
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            MoveHistory.Add(move);
            double score = board.IsDraw() ? worstScore : MiniMax(board, depth, -1000000000, 1000000000, MinimizingPlayer);//is draw has to be here or it won't detect stalemate
            board.UndoMove(move);
            MoveHistory.Clear();

            if ((MinimizingPlayer && score < bestScore) || (!MinimizingPlayer && score > bestScore))
            {
                bestScore = score;
                bestMove = move;
            }
        }

        //int index = Array.FindIndex(legalMoves, move => move.Equals(bestMove));

        // Print the index and the length of legalMoves
        //DivertedConsole.Write($"{index}, {legalMoves.Length}, {bestScore}, {board.IsWhiteToMove}, {bestMove}, {board.PlyCount}");

        return bestMove;

    }
    private double Evaluate(Board board, bool MinimizingPlayer)
    {
        Move[] moves = board.GetLegalMoves();
        double Position = moves.Length;
        double Score = 0.0;
        PieceList[] allPieceLists = board.GetAllPieceLists();
        double multiplier = board.IsWhiteToMove ? 1 : -1;
        double IsMinimizerToggle = MinimizingPlayer ? -1 : 1;
        int totalPieces = allPieceLists.Sum(list => list.Count);
        Square minimizingPlayerKingSquare = board.GetKingSquare(MinimizingPlayer);
        Square opposingPlayerKingSquare = board.GetKingSquare(!MinimizingPlayer);

        List<Square> GetAdjacentSquares(Square s)
        {
            List<Square> adjSquares = new List<Square>();
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i != 0 || j != 0)
                    {
                        int file = s.File + i;
                        int rank = s.Rank + j;
                        if (file >= 0 && file < 8 && rank >= 0 && rank < 8)
                        {
                            adjSquares.Add(new Square(file, rank));
                        }
                    }
                }
            }
            return adjSquares;
        }
        if (totalPieces < 10)
        {
            int KingRank = opposingPlayerKingSquare.Rank;
            int KingFile = opposingPlayerKingSquare.File;
            int OtherKingRank = minimizingPlayerKingSquare.Rank;
            int OtherKingFile = minimizingPlayerKingSquare.File;
            Position += (14 - (Math.Abs(OtherKingRank - KingRank) + Math.Abs(OtherKingFile - KingFile))) * 100;

        }
        double[] pieceValues = { 0, 120, 360, 384, 600, 1080, 0 }; // 0 value for King

        double GetPieceValue(PieceType pieceType)
        {
            return pieceValues[(int)pieceType];
        }

        double[] chessBoard = new double[64]
          {
            10, 0, 0, 200, 2.5, -1, -1, 5,
            5, 5, 0, 0, 0, 10, 10, 10,
            0, 0, 10, 10, 10, 5, 2, 0,
            0, 0, 10, 15, 15, 0, 0, 0,
            0, 0, 10, 15, 15, 0, 0, 0,
            0, 0, 10, 10, 10, 5, 2, 0,
            5, 5, 0, 0, 0, 10, 10, 10,
            10, 0, 0, 200, 2.5,-1, -1, 5
          };

        ulong whitePiecesBitboard = board.WhitePiecesBitboard;
        ulong blackPiecesBitboard = board.BlackPiecesBitboard;

        if (board.PlyCount < 16)
        {
            for (int i = 0; i < 64; i++)
            {
                if ((whitePiecesBitboard & (1UL << i)) != 0)
                    Score += 0.5 * chessBoard[i];
                if ((blackPiecesBitboard & (1UL << i)) != 0)
                    Score -= 0.5 * chessBoard[i];
            }
        }
        else
        {
            Score += board.IsInCheck() ? board.PlyCount / multiplier : 0;
            //Score += board.IsInCheckmate() ? 100000/ multiplier : 0;
        }
        foreach (PieceList pieceList in allPieceLists)
        {
            foreach (Piece piece in pieceList)
            {
                double pieceValue = GetPieceValue(piece.PieceType);
                if (GetAdjacentSquares(board.GetKingSquare(pieceList.IsWhitePieceList)).Contains(piece.Square) && board.PlyCount > 16)
                {
                    pieceValue += 120 / pieceValue;
                }
                Score += pieceValue * (pieceList.IsWhitePieceList ? 1 : -1);
            }
        }
        Move move = MoveHistory[0];
        if (move.MovePieceType == PieceType.King) Position *= 0;
        if (move.IsCastles) Position += 100;
        if (move.IsPromotion) Position += 500;
        if (move.MovePieceType == PieceType.Pawn) Position += 1;

        return Score + Position * IsMinimizerToggle;
    }

    private double MiniMax(Board board, int depth, double alpha, double beta, bool minimizingPlayer, double depthIncreases = 0)
    {

        if (depth == 0)
        {

            if (MoveHistory[^1].IsCapture || (board.IsInCheck() && depthIncreases < 10))
            {
                depth++; depthIncreases++;
            }
            if (board.IsDraw())/*is draw has to be here, or it won't detect repetition
               Draw used to return 0, but then it would just try to get repeated positions in losing positions. Hopefully draw = worstscore makes the bot avoid
                giving up and returning a score of 0.*/
            {
                //board.UndoMove(move);
                //MoveHistory.RemoveAt(MoveHistory.Count - 1);
                double worstScore = board.IsWhiteToMove ? 1000000 : -1000000;
                //continue;
                return 0;
            }
            if (BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) <= 10) depth += 3;
            return Evaluate(board, minimizingPlayer);


        }
        if (board.IsInCheckmate())
        {
            return (1000000000000 - depth) * (board.IsWhiteToMove ? -1 : 1);
        }


        Move[] legalMoves = board.GetLegalMoves();

        Array.Sort(legalMoves, (m1, m2) =>
        {
            int scoreDiff = 0;
            if (m1.IsCapture) scoreDiff -= 100;
            if (m1.IsPromotion) scoreDiff -= 50;
            if (m2.IsCapture) scoreDiff += 100;
            if (m2.IsPromotion) scoreDiff += 50;
            return scoreDiff;
        });
        double bestScore = minimizingPlayer ? double.MinValue : double.MaxValue;
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            MoveHistory.Add(move);

            {
                double currentScore = MiniMax(board, depth - 1, alpha, beta, !minimizingPlayer, depthIncreases);
                bestScore = minimizingPlayer ? Math.Max(bestScore, currentScore) : Math.Min(bestScore, currentScore);

                if (minimizingPlayer) alpha = Math.Max(alpha, bestScore); else beta = Math.Min(beta, bestScore);

                board.UndoMove(move);
                MoveHistory.RemoveAt(MoveHistory.Count - 1);

                if (beta <= alpha) break;
            }
        }
        return bestScore;
    }
}