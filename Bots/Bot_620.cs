namespace auto_Bot_620;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_620 : IChessBot
{
    // Encoded weights
    readonly decimal[] weights = {
        53448653843664175288040992175m,56549567520770799246508739532m,64630222033642671212141593543m,69586827413305713124645395934m,
        69899943977498563072321447131m,1502514513749179168244498432m,1549847624988481415784039940m,7448263149177908324786770454m,
        31058920967985953501964425826m,36950097082093051215633933933m,34790984290857436587329026681m,47209235908666032039877710231m,
        56825150546367545333927820735m,47834459206297746523011060873m,
    };
    readonly double scaling_factor = 212.872;
    readonly double shift = 0.5374;

    public Board myBoard;

    private class Node
    {
        public double prior;
        public double value_sum;
        public int visits;
        public (Move, Node)[]? children;

        public Node(double prior, double value_sum, int visits, (Move, Node)[]? children)
        {
            this.prior = prior;
            this.value_sum = value_sum;
            this.visits = visits;
            this.children = children;
        }
    }

    public unsafe Move Think(Board board, Timer timer)
    {

        myBoard = board;

        var transpositionTable = new Dictionary<ulong, Node>();

        (Move, Node)[] expand()
        {
            var e_ms = myBoard.GetLegalMoves().Select(m => (Evaluate_Move(m), m)).ToArray();
            double min = e_ms.Min(e_m => e_m.Item1) - 0.01;
            double sum = e_ms.Select(e_m => e_m.Item1 - min).Sum();
            return e_ms
                .Select(e_m =>
                {
                    myBoard.MakeMove(e_m.m);
                    var key = myBoard.ZobristKey;
                    if (transpositionTable.ContainsKey(key))
                    {
                        Node node = transpositionTable[key];
#if DEBUG_LEVEL_0
                            transVisits += node.visits;
#endif
                        myBoard.UndoMove(e_m.m);
                        return (e_m.m, node);
                    }
                    else
                    {
                        var newNode = new Node((e_m.Item1 - min) / sum, 0.0, 0, null);
                        transpositionTable.Add(key, newNode);
                        myBoard.UndoMove(e_m.m);
                        return (e_m.m, newNode);
                    }
                })
                .ToArray();
        }

        double UCB(Node node, int parent_visits) =>
            (node.visits > 0 ? node.value_sum / node.visits : 0) +     // Q(s,a)
            (
                (Math.Log((1 + parent_visits + 19652) / 19652) + 1.25) * // C(s)
                node.prior *                                             // P(s)
                Math.Sqrt(parent_visits) / (node.visits + 1)
            );

        Node root = new(1.0, 0.0, 0, null);
        transpositionTable.Add(myBoard.ZobristKey, root);

        while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining * 0.05)
        {
            Node node = root;
            List<(Move, Node)> path = new() { (new Move(), node) };

            // selection
            while (node.visits > 0 && node.children?.Where(c => !path.Contains(c)).Count() > 0)
            {
                (var move, node) = node.children.Where(c => !path.Contains(c)).MaxBy(c => UCB(c.Item2, node.visits));
                myBoard.MakeMove(move);
                path.Add((move, node));
            }

            // expansion
            if (myBoard.GetLegalMoves().Length > 0)
            {
                node.children = expand();
                (var move, node) = node.children.MaxBy(c => UCB(c.Item2, node.visits));
                myBoard.MakeMove(move);
                path.Add((move, node));
            }

            // backpropagation
            double value = myBoard.IsDraw()
                ? 0.0
                : myBoard.IsInCheckmate()
                    ? 1.0
                    : Evaluate();

            for (int i = path.Count - 1; i >= 0; --i)
            {
                path[i].Item2.value_sum += value;
                path[i].Item2.visits++;
                board.UndoMove(path[i].Item1);
                value *= -1;
            }
        }

        return root.children.MaxBy(c => c.Item2.visits).Item1;
    }

    private unsafe double Evaluate_Move(Move move)
    {
        // make move
        myBoard.MakeMove(move);

        // evaluate
        double eval = Evaluate();

        // undo move
        myBoard.UndoMove(move);

        return eval;
    }

    private unsafe double Evaluate()
    {
        // create network
        double[] neurons = new double[163];

        // compute feed forward
        int distToMid(int index) => (int)Math.Abs(index - 3.5);

        fixed (double* neuron_po = neurons)
        {
            double* input_p = neuron_po;

            // construct state vector
            // starting with the pieces 
            foreach (PieceList pieceList in myBoard.GetAllPieceLists())
            {
                foreach (Piece piece in pieceList)
                {
                    *(input_p +
                        pieceList.TypeOfPieceInList switch
                        {
                            PieceType.Pawn => (
                                pieceList.IsWhitePieceList
                                    ? piece.Square.Rank - 1
                                    : 6 - piece.Square.Rank
                            ) * 4 + distToMid(piece.Square.File),
                            PieceType.Knight => distToMid(piece.Square.Rank) + distToMid(piece.Square.File),
                            _ => (
                                pieceList.IsWhitePieceList
                                    ? piece.Square.Rank
                                    : 7 - piece.Square.Rank
                            ) * 4 + distToMid(piece.Square.File)
                        }
                    ) += pieceList.IsWhitePieceList ^ myBoard.IsWhiteToMove ? 1 : -1;
                }
                input_p += pieceList.TypeOfPieceInList switch
                {
                    PieceType.Pawn => 24,
                    PieceType.Knight => 7,
                    PieceType.King => -127,
                    _ => 32
                };
            }

            input_p += 159;

            // then check whether the king is in check
            *input_p++ = myBoard.IsInCheck() ? 1 : -1;

            // check king attacks
            foreach (PieceList pieceList in myBoard.GetAllPieceLists())
            {
                foreach (Piece piece in pieceList)
                {
                    *(input_p + (pieceList.IsWhitePieceList ^ myBoard.IsWhiteToMove ? 1 : 0)) += BitboardHelper.GetNumberOfSetBits(
                        BitboardHelper.GetPieceAttacks(
                            pieceList.TypeOfPieceInList,
                            piece.Square,
                            myBoard,
                            pieceList.IsWhitePieceList
                        ) &
                        (
                            BitboardHelper.GetKingAttacks(myBoard.GetKingSquare(!pieceList.IsWhitePieceList)) |
                            myBoard.GetPieceBitboard(PieceType.King, !pieceList.IsWhitePieceList)
                        )
                    );
                }
            }
            input_p -= 160;

            fixed (decimal* weight_po = weights)
            {
                double* output_p = neuron_po + 162;
                sbyte* weight_p = (sbyte*)weight_po;

                // loop through inputs
                for (int i = 0; i <= 162; i++)
                {
                    // skip 4 bytes at the beginning of each group of 16 weights (one decimal)
                    if ((weight_p - (sbyte*)weight_po) % 16 == 0) weight_p += 4;

                    // compute weighted sum
                    *output_p += (((double)*weight_p++) / scaling_factor + shift) * // decode sbyte [-128, 127] to double [-2.0, 2.0]
                        (i == 162
                            ? 1
                            : *input_p++
                        );
                }
            }
        }

        // return value of output neuron
        return Math.Tanh(neurons[^1]);
    }
}
