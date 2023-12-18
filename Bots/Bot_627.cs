namespace auto_Bot_627;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_627 : IChessBot
{

    public Dictionary<PieceType, int> PieceValues = new Dictionary<PieceType, int>()
    {
        {PieceType.Pawn, 100},
        {PieceType.Knight, 70},
        {PieceType.Bishop, 60},
        {PieceType.Rook, 30},
        {PieceType.Queen, 30},
        {PieceType.King, 1},
    };

    public Move Think(Board board, Timer timer)
    {

        /* DIARY ENTRY : Sunday Oct 1 2pm Sydney
         * Toddler's asleep, I have 2-3 hours. Let's go!
         * Keep it simple
         * Recently, I've been working on implementing images using rng algorithms.
         * Let's make a heatmap of the board that prioritises diagonals and see what happens. */

        /* DIARY ENTRY : Monday Oct 2 7am Sydney
         * The above worked better than I thought.
         * The knight and bishops behave as I wanted.
         * King's a little ... bold.
         * I can see I'm going to have to factor in if I'm attacked or about to capture.
         * I have 2 hours before the kid awakens, let's write some weights for pieces! */

        /* DIARY ENTRY : Monday Oct 2 8pm Sydney
         * I've run out of time. :( I've return to clean up and submit as is.
         * I believe this algorithm has merit. It's keen to attack and has good pawn structure.
         * I'm going to make a quick GetThreats function and ship. */

        /* DIARY ENTRY : Monday Oct 2 10pm Sydney
         * I had a lot of fun working with your OOP style. Super impressive!
         * If this competition had come at a different time in my life, I would have loved to
         * continue to explore this. Thank you SebLague. 
         * With Love. Pix. */


        // What are my options
        Move[] moves = board.GetLegalMoves();

        // Let's set a default value... just incase.
        Random rand = new Random();
        int result = rand.Next(0, moves.Length);

        // A Heatmap is just an image where brightness = good target square.
        HeatMap options = new HeatMap();
        // Emphasized attacked pieces for the next iterations
        options.Zip(GetThreats(board));
        // This algorithm returns a weighted table focussing on the diagonals.
        options.Iterate();


        // An int to help keep track of the top choice
        double bestScore = 0;
        // Calculate the best move.
        for (int k = 0; k < moves.Length; k++)
        {
            // Get constants for this loop
            Node threat = GetThreats(board)[moves[k].StartSquare.Index];
            int pieceVal = PieceValues[moves[k].MovePieceType];
            foreach (Node nd in options.map)
            {
                if (nd.Weight == 0) continue; //This is key!
                if (nd.ToSquare() == moves[k].TargetSquare) //We only want intersects!
                {
                    // Is this a good capture
                    double capture = moves[k].IsCapture ? GetExchangeQuality(moves[k]) : 1;
                    // If I take this moved am I under attack?
                    double attacked = board.SquareIsAttackedByOpponent(nd.ToSquare()) ? 100 : 1;
                    // Calculate a score.
                    double score = ((nd.Weight + pieceVal) * capture) / (attacked);
                    if (threat.Weight != 0 & moves[k].StartSquare.Index == threat.Index)
                    {
                        // If this piece I'm moving is under threat, it's probably a good move.
                        score = score * threat.Weight;
                    }
                    // DO IT!
                    if (score > bestScore)
                    {
                        bestScore = score;
                        result = k;
                    }
                }
            }
        }

        // This prevents the same piece moving over an over again.
        UpdatePieceValues(moves[result].MovePieceType, 10, 1.05);
        // Hope for the best...
        return moves[result];
    }

    public Node[] GetThreats(Board bd)
    {
        // A weighted map of the bots threatened peices.
        Node[] threats = new Node[64];
        for (int i = 0; i < threats.Length; i++)
        {
            threats[i] = new Node(0.0, i);
        }

        for (int k = 0; k < 64; k++)
        {
            Square sq = new Square(k);
            bool x = bd.SquareIsAttackedByOpponent(sq);
            if (x)
            {
                Piece pc = bd.GetPiece(sq);
                if (pc.PieceType == PieceType.None) continue;
                if (pc.PieceType == PieceType.Pawn) continue;
                if (!pc.IsWhite ^ bd.IsWhiteToMove)
                {
                    threats[k].Weight = threats[k].Weight + PieceValues[pc.PieceType];
                }
            }
        }
        return threats;
    }

    public double GetExchangeQuality(Move mv)
    {
        // Returns high numbers for productive exchanges
        double A = PieceValues[mv.MovePieceType];
        double B = PieceValues[mv.CapturePieceType] * 1.2;
        return B / A;
    }

    public void UpdatePieceValues(PieceType movedPiece, int deduct, double factor)
    {
        // Increases the weights of all pieces except what we just moved.
        foreach (PieceType key in PieceValues.Keys)
        {
            if (key == movedPiece)
            {
                PieceValues[key] = PieceValues[key] - deduct;
            }
            PieceValues[key] = (int)Math.Ceiling(PieceValues[key] * factor);
            PieceValues[key] = Math.Abs(PieceValues[key] % 100) + 1;
        }
    }

    public class Node
    {
        // Modified to remove illegal namespace -- seb
        public double Weight;
        public int Index;

        public Node()
        {
            Weight = 0;
            Index = 0;
        }

        public Node(double w, int idx)
        {
            Weight = w;
            Index = idx;
        }

        public Square ToSquare()
        {
            return new Square(Index);
        }

    }

    public class HeatMap
    {
        public Node[] map;

        public HeatMap()
        {
            Random rand = new Random();
            map = new Node[64];
            for (int i = 0; i < map.Length; i++)
            {
                map[i] = new Node(rand.NextDouble(), i);
            }
        }

        public void Iterate()
        {
            for (int j = 0; j < 8; j++)
            {
                for (int i = 0; i < 8; i++)
                {
                    // This Algorithm Ephasizes diagonals.
                    // My hope is that it will encourage good pawn structures
                    // and good knight moves.
                    // Maybe the rooks will fend for themselves??
                    int idx = i + j * 8;
                    double a = map[i + j * Wrap(j - 1)].Weight;
                    double b = map[Wrap(i - 1) + j * Wrap(j + 1)].Weight;
                    double c = map[Wrap(i - 1) + j * Wrap(j + 1)].Weight;
                    double d = map[i + j * Wrap(j + 3)].Weight;
                    double result = Math.Abs((1.1 * a * (b + c)) / (d + 0.1) + 1);
                    map[idx].Weight = (result * result) % 256;
                }
            }
            Print();
        }

        public void Zip(Node[] other)
        {
            if (other.Length != 64) return;
            for (int i = 0; i < 64; i++)
            {
                map[i].Weight += other[i].Weight;
            }
        }

        private static int Wrap(int i)
        {
            return ((i % 8) + 8) % 8;
        }

        private void Print()
        {
            for (int i = 63; i >= 0; i--)
            {
                if (map[i].Index % 8 == 0)
                {
                    DivertedConsole.Write();
                }
                DivertedConsole.Write(Math.Round(map[i].Weight, 1));
                DivertedConsole.Write(" ");
            }
            DivertedConsole.Write();
        }
    }
}