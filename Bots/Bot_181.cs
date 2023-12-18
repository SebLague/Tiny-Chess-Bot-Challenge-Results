namespace auto_Bot_181;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class NeuralNetwork
{
    private int inputSize = 64;
    private int hiddenSize = 64;
    private int outputSize = 1;
    private double[,] weightsInputHidden = new double[64, 64];
    private double[,] weightsHiddenOutput = new double[64, 1];
    private double[] biasHidden = {
            0.0093,  0.7201, -0.4888, -1.1677, -0.0787,  0.5538, -0.7047, -0.4611,
            -0.3950, -0.2783,  0.5403, -1.4371,  0.6107,  0.2568, -0.1416,  0.6025,
            -1.1769,  0.0499,  0.2756, -1.6897,  0.3364,  0.2278, -1.6972, -0.6835,
            -0.3352, -1.0475,  0.9970, -1.6108, -1.5375, -0.4456, -0.1539,  0.3226,
            0.7910, -1.5217, -0.4274, -1.3362, -1.3930, -1.1615,  0.2305,  0.4338,
            0.1061, -0.1012,  0.4722, -0.1777, -1.5194, -1.4983,  0.0321, -1.7931,
            0.8874, -0.4629,  0.3259,  0.4465,  0.3047,  0.9525,  0.8024,  1.0381,
            0.1891, -1.3641,  0.3612,  0.8040, -0.9125, -0.4735, -0.2301,  0.7228};
    private double[] biasOutput = { -0.1160 };

    public NeuralNetwork()
    {
        // Initialize weights and biases with random values
        RandomizeWeightsAndBiases();

    }
    public double ReLU(double x) => Math.Max(0, x);
    private void RandomizeWeightsAndBiases()
    {
        var random = new Random(0); // Use a fixed seed for reproducibility

        for (int i = 0; i < inputSize; i++)
        {
            for (int j = 0; j < hiddenSize; j++)
            {
                weightsInputHidden[i, j] = random.NextDouble() - 0.5;
            }
        }

        for (int i = 0; i < hiddenSize; i++)
        {
            for (int j = 0; j < outputSize; j++)
            {
                weightsHiddenOutput[i, j] = random.NextDouble() - 0.5;
            }
        }
    }

    public double FeedForward(double[] input)
    {
        // Calculate hidden layer output
        var hiddenLayer = new double[hiddenSize];
        for (int i = 0; i < hiddenSize; i++)
        {
            hiddenLayer[i] = biasHidden[i] + Enumerable.Range(0, inputSize)
                .Sum(j => input[j] * weightsInputHidden[j, i]);
            hiddenLayer[i] = ReLU(hiddenLayer[i]);
        }

        // Calculate output layer output
        var output = biasOutput[0] + Enumerable.Range(0, hiddenSize)
            .Sum(j => hiddenLayer[j] * weightsHiddenOutput[j, 0]);
        return output;
    }
}

public class Bot_181 : IChessBot
{
    private NeuralNetwork neuralNetwork = new NeuralNetwork();

    public Move Think(Board board, Timer timer)
    {
        // Initialize search parameters (e.g., depth, time control)
        int depth = 4;
        double maxTime = timer.MillisecondsRemaining / 1000.0; // Convert to seconds
        double startTime = timer.GameStartTimeMilliseconds / 1000.0;

        // Perform iterative deepening search
        Move[] legalMoves = board.GetLegalMoves();
        Array.Sort(legalMoves, (a, b) => a.IsCapture.CompareTo(b.IsCapture) + a.IsPromotion.CompareTo(b.IsPromotion) + a.IsPromotion.CompareTo(b.IsPromotion));
        Move bestMove = legalMoves[0];
        double bestScore = double.NegativeInfinity;
        double alpha = double.NegativeInfinity;
        double beta = double.PositiveInfinity;

        for (int currentDepth = 1; currentDepth <= depth; currentDepth++)
        {
            // Generate legal moves for the current position
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move); // Make the move
                if (board.IsInCheckmate())
                    return move;
                double score = -AlphaBeta(board, currentDepth - 1, -beta, -alpha);
                board.UndoMove(move); // Undo the move

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, bestScore);

                if (alpha >= beta)
                    break; // Beta cutoff
            }

            // Check if time is running out and decide whether to continue searching
            double elapsedTime = timer.MillisecondsElapsedThisTurn / 1000.0;
            if (startTime + elapsedTime >= maxTime)
                break;
        }
        // Return the best move found
        return bestMove;
    }

    public double AlphaBeta(Board board, int depth, double alpha, double beta)
    {
        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            return Evaluate(board);
        Move[] legalMoves = board.GetLegalMoves();
        Array.Sort(legalMoves, (a, b) => a.IsCapture.CompareTo(b.IsCapture) + a.IsPromotion.CompareTo(b.IsPromotion) + a.IsPromotion.CompareTo(b.IsPromotion));
        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            double score = -AlphaBeta(board, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            alpha = Math.Max(alpha, score);

            if (alpha >= beta)
                break; // Beta cutoff
        }

        return alpha;
    }
    public static double[] FenToBoardVector(string fen)
    {
        string[] piecePlacement = fen.Split(' ')[0].Split('/');

        double[] boardVector = new double[64];

        Dictionary<char, double> pieceToValue = new Dictionary<char, double>
        {
            { 'R', 0.5 },
            { 'N', 0.3 },
            { 'B', 0.35 },
            { 'Q', 0.9 },
            { 'K', 1.0 },
            { 'P', 0.1 },
            { 'p', -0.1 },
            { 'k', -1.0 },
            { 'q', -0.9},
            { 'b', -0.35 },
            { 'n', -0.3 },
            { 'r', -0.5 }
        };
        for (int r = 0; r < piecePlacement.Length; r++)
        {
            int c = 0;
            foreach (char piece in piecePlacement[r])
            {
                if (pieceToValue.ContainsKey(piece))
                {
                    boardVector[r * 8 + c] = pieceToValue[piece];
                    c++;
                }
                else
                {
                    c += int.Parse(piece.ToString());
                }
            }
        }
        return boardVector;
    }
    public double Evaluate(Board board)
    {

        return neuralNetwork.FeedForward(FenToBoardVector(board.GetFenString()));
    }
}
