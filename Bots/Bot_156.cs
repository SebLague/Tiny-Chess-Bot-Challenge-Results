// Side note: The weights (and a couple other things) have been trained in python. The source for that can be found both in the repo (https://github.com/noelhug/Chess-Challenge/blob/main/create_training_data.py)
// And on google colab (https://colab.research.google.com/drive/1-awHSkViFT9v_bLo6VB_sHjfOijhA4CB?usp=sharing)
namespace auto_Bot_156;
using System;
using System.Text;
using ChessChallenge.API;

public class Bot_156 : IChessBot
{
    // Weights encoded (took weights as comma seperated list and convert to hex)
    private readonly ulong[] _weightsAndBiasEncoded = new ulong[]
    {
        0x302e303230333230,
        0x3736353637343131,
        0x343232372c302e30,
        0x3231303439333234,
        0x3432333037343732,
        0x322c302e30323239,
        0x3432343831353632,
        0x34393532332c302e,
        0x3032323738383131,
        0x3131323034363234,
        0x31382c302e303232,
        0x3935373933323230,
        0x343030383130322c,
        0x302e303232343139,
        0x3230383636303732,
        0x3137382c302e3032,
        0x3337363535353237,
        0x3838393732383535,
        0x2c302e3032303739,
        0x3433393334393437,
        0x32353232372c302e,
        0x3032303838303937,
        0x3131303339303636,
        0x332c302e30323433,
        0x3438363931313035,
        0x38343235392c302e,
        0x3032313735333431,
        0x3336303237303937,
        0x372c302e30323036,
        0x3336373633343233,
        0x36383132362c302e,
        0x3032313234373330,
        0x3638333836333136,
        0x332c302e30323337,
        0x3133303431303936,
        0x3932353733352c30,
        0x2e30323336383039,
        0x3939383735303638,
        0x3636352c302e3032,
        0x3233373830343233,
        0x3430323738363235,
        0x2c302e3032313337,
        0x3634393234353536,
        0x30313639322c302e,
        0x3032333538333638,
        0x3431313636303139,
        0x34342c302e303230,
        0x3433303638373831,
        0x343935303934332c,
        0x302e303235383534,
        0x3438353130393434,
        0x383433332c302e30,
        0x3232393331333133,
        0x3134323138303434,
        0x332c302e30323231,
        0x3136363739363938,
        0x3232383833362c30,
        0x2e30323232393937,
        0x3934343830323034,
        0x3538322c302e3032,
        0x3431323731393436,
        0x353739323137392c,
        0x302e303230313536,
        0x3639303835303835,
        0x3339322c302e3032,
        0x3032393038333131,
        0x3033393230393337,
        0x2c302e3032323532,
        0x3336343930333638,
        0x38343330382c302e,
        0x3032353831333132,
        0x3530373339303937,
        0x362c302e30323139,
        0x3137333030323938,
        0x3831303030352c30,
        0x2e30323236393237,
        0x3330363530333035,
        0x3734382c302e3032,
        0x3438363634393731,
        0x343431303330352c,
        0x302e303237323934,
        0x3330323335393232,
        0x333336362c302e30,
        0x3234313533373434,
        0x3830313837383933,
        0x2c302e3032323238,
        0x3933343837363632,
        0x30373639352c302e,
        0x3032303737343233,
        0x3232323336323939,
        0x352c302e30323536,
        0x3436373438303231,
        0x3234353030332c30,
        0x2e30323230303938,
        0x3430323335313134,
        0x3039382c302e3032,
        0x3338323933343833,
        0x3835333334303135,
        0x2c302e3032313934,
        0x3739353031323437,
        0x3430362c302e3032,
        0x3430353836313736,
        0x3531343632353535,
        0x2c302e3032313535,
        0x3030393437303838,
        0x30303331362c302e,
        0x3032303638373236,
        0x3334353839363732,
        0x312c302e30323238,
        0x3132303634373337,
        0x3038313532382c30,
        0x2e30323538333437,
        0x3936393530323231,
        0x30362c302e303234,
        0x3835383930363836,
        0x353131393933342c,
        0x302e303232353038,
        0x3630383137373330,
        0x343236382c302e30,
        0x3233313335363238,
        0x3535313234343733,
        0x362c302e30323232,
        0x3235373135323139,
        0x3937343531382c30,
        0x2e30313935323839,
        0x3038363535303437,
        0x3431372c302e3032,
        0x3430343034313032,
        0x3935313238383232,
        0x2c302e3032303035,
        0x3334303930333939,
        0x37343231332c302e,
        0x3032333539313131,
        0x3630373037343733,
        0x37352c302e303232,
        0x3437333731373130,
        0x383336383837342c,
        0x302e303231343030,
        0x3331373534393730,
        0x353530352c302e30,
        0x3233393130373231,
        0x3736333936383436,
        0x382c302e30313939,
        0x3833323639323734,
        0x32333437372c302e,
        0x3032303638313630,
        0x3834363832393431,
        0x34342c302e303139,
        0x3032363333313630,
        0x3335323730372c30,
        0x2e30323339333030,
        0x3933323733353230,
        0x34372c302e303232,
        0x3435343931393239,
        0x333532323833352c,
        0x302e303232353731,
        0x3635343939303331,
        0x353433372c302e30,
        0x3230373637323139,
        0x3336343634333039,
        0x372c302e30323138,
        0x3336333130363235,
        0x3037363239342c30,
        0x2e30313938383633,
        0x3432383038363034,
        0x32342c2d302e3030,
        0x3739383435383937,
        0x3835333337343438
    };

    private double[] _weights = new double[64];
    private LinearModel _model;

    public Bot_156()
    {
        DecodeWeightsAndBias();
    }

    private void DecodeWeightsAndBias()
    {
        string[] values = ConvertHexedArrayToString().Split(',');
        for (int i = 0; i < 64; i++)
            _weights[i] = double.Parse(values[i]);

        _model = new LinearModel(_weights, double.Parse(values[64]));
    }

    private string ConvertHexedArrayToString()
    {
        StringBuilder decodedText = new();

        foreach (ulong hexNumber in _weightsAndBiasEncoded)
        {
            byte[] bytes = BitConverter.GetBytes(hexNumber);
            // ensures bytes are in the correct order
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);

            string ascii = Encoding.ASCII.GetString(bytes);
            decodedText.Append(ascii);
        }

        return decodedText.ToString();
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        Move bestMove = moves[0];

        int depth = GetDepth(timer.MillisecondsRemaining, moves.Length);
        double alpha = double.MinValue;
        double beta = double.MaxValue;
        double maxEval = double.MinValue;
        double minEval = double.MaxValue;

        foreach (Move move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                bestMove = move;
                break;
            }

            if (board.IsDraw())
                // From testing, it seems like our bot basically never gets in
                // situation where drawing is the best option (i.e. we would
                // loose otherwise), but the bot often gets into situation where
                // it is in a very winning position -> I.e. we will try to avoid
                // draw by repetition.
                if (board.IsRepeatedPosition())
                {
                    board.UndoMove(move);
                    continue;
                }

            double eval = AlphaBeta(board, depth, alpha, beta, board.IsWhiteToMove);

            // we are the maximizing player (we check for the opposit here, due
            // to the call to 'MakeMove' before
            if (!board.IsWhiteToMove)

                if (eval > maxEval)
                {
                    maxEval = eval;
                    bestMove = move;
                }
                else if (eval < minEval)
                {
                    minEval = eval;
                    bestMove = move;
                }

            alpha = Math.Max(alpha, eval);
            board.UndoMove(move);
        }

        return bestMove;
    }

    private static int GetDepth(int milliSecondsRemaining, int numberOfMovesAvailable)
    {
        // In the first 30 seconds, search at depth 4, then until 10 seconds
        // left, search at depth 4, afterwards at depth 1.
        int depth =
            milliSecondsRemaining >= 10_000
                ? milliSecondsRemaining >= 30_000
                    ? 5
                    : 4
                : 1;
        // If we branch a lot on the first level, we would most likely branch a
        // lot multiple levels down (in other words, we will decrease the depth
        // (to make the bot not timout as much)).
        if (numberOfMovesAvailable >= 20)
            depth = Math.Max(1, depth - 1);

        return depth;
    }

    private double AlphaBeta(
        Board node,
        int depth,
        double alpha,
        double beta,
        bool isMaximizingPlayer
    )
    {
        var legalMoves = node.GetLegalMoves();

        if (depth == 0 || legalMoves.Length == 0)
        {
            double eval = Evaluate(node);

            return eval;
        }

        if (node.IsWhiteToMove)
        {
            double maxEval = int.MinValue;
            foreach (Move child in legalMoves)
            {
                node.MakeMove(child);

                if (node.IsInCheckmate()) // white (our bot), just made checkmate
                {
                    node.UndoMove(child);
                    // we can always return 999. Suppose our bot is playing black :
                    // if we loose (i.e. we get checkmated like beneath), 999
                    // is supper dupper bad. If we would play as white, and
                    // checkmate   our oppenent here, 999 is good
                    // (as is checkmate :).
                    return 999;
                }

                double eval = AlphaBeta(node, depth - 1, alpha, beta, isMaximizingPlayer);
                maxEval = Math.Max(maxEval, eval);
                alpha = Math.Max(alpha, eval);

                node.UndoMove(child);
                if (beta <= alpha)
                    break;
            }

            return maxEval;
        }
        else
        {
            double minEval = int.MaxValue;
            foreach (Move child in node.GetLegalMoves())
            {
                node.MakeMove(child);

                if (node.IsInCheckmate())
                {
                    node.UndoMove(child);
                    return -999;
                }

                double eval = AlphaBeta(node, depth - 1, alpha, beta, isMaximizingPlayer);
                minEval = Math.Min(minEval, eval);
                beta = Math.Min(beta, eval);
                node.UndoMove(child);
                if (beta <= alpha)
                    break;
            }

            return minEval;
        }
    }

    private double[] GetBoardArrayFromBoard(Board board)
    {
        double[] pieces = new double[64];
        for (int i = 0; i < 64; i++)
        {
            Piece piece = board.GetPiece(new Square(i));
            int pieceVal = GetPieceValue(piece);
            pieceVal = piece.IsWhite ? pieceVal : -pieceVal;
            pieces[i] = pieceVal;
        }
        return pieces;
    }

    private int GetPieceValue(Piece piece)
    {
        return piece.PieceType switch
        {
            PieceType.Pawn => 1,
            PieceType.Bishop => 2,
            PieceType.Knight => 3,
            PieceType.Rook => 4,
            PieceType.Queen => 5,
            PieceType.King => 6,
            _ => 0
        };
    }

    private double Evaluate(Board board)
    {
        return _model.Predict(GetBoardArrayFromBoard(board));
    }
}

public class LinearModel
{
    private double[] Weights { get; }
    private double Bias { get; }

    public LinearModel(double[] weights, double bias)
    {
        Weights = weights;
        Bias = bias;
    }

    public double Predict(double[] inputs)
    {
        double result = Bias;

        for (int i = 0; i < Weights.Length; i++)
            result += Weights[i] * inputs[i];

        return result;
    }
}
