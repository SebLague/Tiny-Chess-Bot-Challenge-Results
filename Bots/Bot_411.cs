namespace auto_Bot_411;
using ChessChallenge.API;
using System;

public class Bot_411 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        int DEPTH = 3; // Depth for the search. Adjust as needed.
        bool searchCancelled = false;
        Move bestMove = Move.NullMove;
        double bestValue = double.NegativeInfinity;

        // Piece values: null, pawn, knight, bishop, rook, queen, king
        int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        double AlphaBeta(double alpha, double beta, int depth, Board board)
        {
            // LogToFile($"Depth: {depth}");
            bool capturesOnly = false;
            if (board.IsInCheckmate())
            {
                // LogToFile($"Checkmate reached.");
                return -99999;
            };
            if (board.IsDraw()) return 0;
            if ((timer.MillisecondsElapsedThisTurn * 2 > (timer.MillisecondsRemaining - 1500)) | (timer.MillisecondsRemaining < 5000))
            {
                searchCancelled = true;
                return Evaluate(board);
            }
            if (depth <= 0) capturesOnly = true;

            Move[] moves = board.GetLegalMoves(capturesOnly);

            if (moves.Length == 0)
                return Evaluate(board);

            OrderMoves(board, moves);

            foreach (Move move in moves)
            {
                board.MakeMove(move);
                double score = -AlphaBeta(-beta, -alpha, depth - 1, board);
                board.UndoMove(move);

                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }

        void OrderMoves(Board board, System.Span<Move> moves)
        {
            int[] moveScores;
            moveScores = new int[200];
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];

                int score = 0;
                Square startSquare = move.StartSquare;
                Square targetSquare = move.TargetSquare;
                int movePieceType = (int)move.MovePieceType;
                int capturePieceType = (int)move.CapturePieceType;
                int pieceValue = pieceValues[movePieceType];

                const int million = 1000000;
                const int winningCaptureBias = 8 * million;
                const int checkBias = 50 * million;
                const int promoteBias = 6 * million;
                const int losingCaptureBias = 2 * million;

                if (move.IsCapture)
                {
                    // Order moves to try capturing the most valuable opponent piece with least valuable of own pieces first
                    int captureMaterialDelta = pieceValues[(int)capturePieceType] - pieceValue;
                    bool opponentCanRecapture = board.SquareIsAttackedByOpponent(targetSquare);
                    if (opponentCanRecapture)
                    {
                        score += (captureMaterialDelta >= 0 ? winningCaptureBias : losingCaptureBias) + captureMaterialDelta;
                    }
                    else
                    {
                        score += winningCaptureBias + captureMaterialDelta;
                    }
                }

                if (BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(move.MovePieceType, move.TargetSquare, board, board.IsWhiteToMove) & board.GetPieceBitboard(PieceType.King, !board.IsWhiteToMove)) > 0)
                {
                    //  Do checks first
                    score += checkBias;
                };

                if (move.MovePieceType == PieceType.Pawn)
                {
                    if (move.IsPromotion && (move.PromotionPieceType == PieceType.Queen))
                    {
                        score += promoteBias;
                    }
                }
                else if (move.MovePieceType == PieceType.King)
                {
                    score -= 20;
                }
                else
                {

                }

                moveScores[i] = score;
            }

            //Sort(moves, moveScores);
            Quicksort(moves, moveScores, 0, moves.Length - 1);
        }

        static void Quicksort(System.Span<Move> values, int[] scores, int low, int high)
        {
            if (low < high)
            {
                int pivotIndex = Partition(values, scores, low, high);
                Quicksort(values, scores, low, pivotIndex - 1);
                Quicksort(values, scores, pivotIndex + 1, high);
            }
        }

        static int Partition(System.Span<Move> values, int[] scores, int low, int high)
        {
            int pivotScore = scores[high];
            int i = low - 1;

            for (int j = low; j <= high - 1; j++)
            {
                if (scores[j] > pivotScore)
                {
                    i++;
                    (values[i], values[j]) = (values[j], values[i]);
                    (scores[i], scores[j]) = (scores[j], scores[i]);
                }
            }
            (values[i + 1], values[high]) = (values[high], values[i + 1]);
            (scores[i + 1], scores[high]) = (scores[high], scores[i + 1]);

            return i + 1;
        }

        int CountMaterial(bool isWhite)
        {
            // Returns material for given colour (true is white, false is black)
            int material = 0;
            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                // Only look at own pieces
                if (isWhite == pieceList.IsWhitePieceList)
                {
                    // LogToFile($"Piece value for white {isWhite}: {pieceValues[(int)pieceList.TypeOfPieceInList]}");
                    material += pieceList.Count * pieceValues[(int)pieceList.TypeOfPieceInList];
                    // LogToFile($"Piece count and material value {pieceList.Count}: {material}");
                }
            }
            return material;
        }

        int CentralPieces(bool isWhite)
        {
            // Returns material for given colour (true is white, false is black)
            int centrality = 0;
            foreach (PieceList pieceList in board.GetAllPieceLists())
            {
                // LogToFile($"Piece value for white {isWhite}: {pieceValues[(int)pieceList.TypeOfPieceInList]}");
                // Only look at own pieces
                if (isWhite == pieceList.IsWhitePieceList)
                {
                    for (int i = 0; i < pieceList.Count; i++)
                    {
                        if ((2 <= pieceList[i].Square.File) & (pieceList[i].Square.File <= 5))
                        {
                            centrality += 20;
                        }
                        // Put piece near king
                        if ((Math.Abs(board.GetKingSquare(isWhite).File - pieceList[i].Square.File) <= 1) & (Math.Abs(board.GetKingSquare(isWhite).Rank - pieceList[i].Square.Rank) <= 1))
                        {
                            centrality += 30;
                        }
                        if ((board.GetKingSquare(isWhite).Rank == 1) | board.GetKingSquare(isWhite).Rank == 8)
                        {
                            centrality += 20;
                        }
                    }
                }
            }
            return centrality;
        }

        double Evaluate(Board board)
        {
            int evaluationScore = 0;
            // Calculate material for current player
            evaluationScore += CountMaterial(board.IsWhiteToMove);
            // Subtract material, for opponent (multiplied by a factor)
            evaluationScore -= CountMaterial(!board.IsWhiteToMove);
            // Add more here
            // LogToFile($"Eval score pre-central {evaluationScore}");
            evaluationScore += CentralPieces(board.IsWhiteToMove);
            // LogToFile($"Eval score post-central {evaluationScore}");
            return evaluationScore;
        }

        Move[] moves = board.GetLegalMoves();
        OrderMoves(board, moves);
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            double moveValue = -AlphaBeta(-double.MaxValue, double.MaxValue, DEPTH - 1, board);
            board.UndoMove(move);
            if (moveValue > bestValue)
            {
                bestValue = moveValue;
                bestMove = move;
            }
            if (searchCancelled) return bestMove;
        }
        return bestMove;
    }
}