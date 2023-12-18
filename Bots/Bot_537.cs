namespace auto_Bot_537;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MV = System.ValueTuple<ChessChallenge.API.Move, ChessChallenge.API.Move>;

public class Bot_537 : IChessBot
{
    //This engine is to prove that MCTS has no chance as a singlethreaded operation, but while MCTS is parallel scalable, AB is not really 
    //AB parallelism is via filling the TT with reasonable data, MCTS treesearch allows true massive parallel chess engines. 

    //About MYBOT
    //Color agnostic self balanced MCT with another engine as the evaluation function for the playout phase. Uses quake inv.sqrt for faster UCB1

    //Color agnostic: Its always our turn. The opponent does not exist in the sense that we play a move and we get 3 sensible responses (fix: no compute for 3 - reduced to 1).
    //This helps as we need to prune away bad branches quickly - and on second re-entry of think we can continue UCB1 - searching the tree. and forget untaken paths
    //We dont forget any reachable evaluation() ever as long as its reachable in the tree. 

    //Upon backtracking we keep the best child assigned correctly
    //We never have nodes with 0 visits ever. Waste of cycles to setup the board from root all the time. 

    //Evaluation function
    //For MCTS random playouts we use ErwanF nanochess 200 token engine + TT (important as a random source!)

    //Generality: 
    //A game engine is responsible for LEAF evals and we run MCTS on top of that. 
    //.NET allows us to easily prune(delete) non taken branches away

    class MCNode
    {
        public MV move;
        public float score;
        public ulong visits;
        public MCNode Parent;

        public MCNode best;
        public MCNode[] Children;
        public float ucb1;
        public ulong zobrist;
    }

    float InvSqrt(float x)
    {
        float xhalf = 0.5f * x;
        int i = BitConverter.SingleToInt32Bits(x);
        i = 0x5f3759df - (i >> 1);
        x = BitConverter.Int32BitsToSingle(i);
        x = x * (1.5f - xhalf * x * x);
        return x;
    }

    // w/n + c * (ln(N)/n)^0.5 //Original UCB1
    // w/n + c* (log2(N)/(n* log2(e)))^0.5 //We can move any constant out of sqrt into c and convert ln to log2
    // w/n + c* (n / log2(N)) ^ -0.5 //We can use log2 approximation and fast quake square root to get a much quicker result
    float UCB1(ulong visits, float score, ulong parentvisits)
    {
        return (score / (1f * visits)) + InvSqrt(visits / (float)(63 - BitOperations.LeadingZeroCount(parentvisits)));
    }

    MCNode root;

    public Move Think(Board board, Timer timer)
    {
        MCNode makeNode(MCNode parent = default, MV move = default) => new MCNode() { Parent = parent, move = move, zobrist = board.ZobristKey };

        void makeMove(MCNode node) { board.MakeMove(node.move.Item1); board.MakeMove(node.move.Item2); };
        void undoMove(MCNode node) { board.UndoMove(node.move.Item2); board.UndoMove(node.move.Item1); };

        root = root?.Children?.FirstOrDefault(x => x.zobrist == board.ZobristKey) ?? makeNode(); //We prune the tree or start a new one. 
        root.Parent = null; root.move = default;

        ulong visits = 0;
        long scores = 0;

        MCNode leaf = root;
        ulong zob = board.ZobristKey;

        while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 45)
        {
            //Set backpropagation values
            visits = 0;
            leaf = root;

            //Setup the board
            while (leaf.Children != null)
            {
                leaf = leaf.best;
                makeMove(leaf);
            }

            ExpandSimulate();
            Backpropagate(leaf, true);
        }
        //Should actually orderby vitits but that only correct for statistically significant number of playouts. (not here)
        return root.Children.MaxBy(x => x.score).move.Item1;


        void ExpandSimulate(bool dualPly = false)
        {
            //We expand and evaluate new children to get a new ucb policy. 
            leaf.Children = getAllMoves().OrderBy(x => x.Item3).Select(x => makeNode(leaf, (x.Item1, x.Item2))).ToArray();
            leaf.visits = visits = (ulong)leaf.Children.Length;

            foreach (var newchild in leaf.Children)
            {
                makeMove(newchild);

                newchild.score = Selfplay();
                newchild.visits = 1;
                newchild.zobrist = board.ZobristKey;

                undoMove(newchild);
            }

            if (leaf.Children.Length == 0)
            {
                leaf.score += whichEnd();
                leaf.visits++;
            }
        }

        //Update Parent Chain UCB and swap places with siblings accprding to new values
        //Mostly we have ~25 more visits in a chain of nodes to the root
        void Backpropagate(MCNode node, bool first)
        {
            if (node.move.Item1.RawValue != 0) undoMove(node);

            //This just changed ucb1 for the children
            node.score += first ? 0 : scores;
            node.visits += first ? 0 : visits;
            foreach (var child in node.Children)
            {
                child.ucb1 = UCB1(child.visits, child.score, node.visits);
            }
            node.best = node.Children.MaxBy(x => x.ucb1);

            if (node == root) return;
            Backpropagate(node.Parent, false);
        }


        int whichEnd() => board.IsInCheckmate() ? 1 : board.IsDraw() ? 0 : -1;

        //We return the worst 1 outcomes of the N best moves (minmax). And fix the branching factor of all chess moves
        //We get Sensible opponent responses by TT + extension improved 200 Token challenge winner
        IEnumerable<(Move, Move, float)> getAllMoves()
        {
            foreach (var own in board.GetLegalMoves())
            {
                board.MakeMove(own);
                var eval = Evaluate(3);
                board.UndoMove(own);

                yield return (own, eval.Item2, eval.Item1);
            }
        }

        //Pseudorandom (TT is our source of randomness) playout
        int Selfplay()
        {
            Stack<Move> playedMoves = new();
            int outcome;
            while ((outcome = whichEnd()) == -1)
            {
                playedMoves.Push(Evaluate(3).Item2);
                board.MakeMove(playedMoves.Peek());
                if (playedMoves.Count > 30)
                {
                    outcome = 0;
                    break;
                }
            }
            outcome *= board.IsWhiteToMove ? 1 : -1; //ok for outcome 0

            DivertedConsole.Write(outcome + " " + playedMoves.Count);
            while (playedMoves.Count != 0) board.UndoMove(playedMoves.Pop());

            return outcome;
        }

        //Supercharged the winner of the 200 Token challenge. 
        //It gives us a sensible move AND its evaluation.
        //TT + Check extensions
        (float, Move) Evaluate(int searchDepth)
        {
            Move bestRootMove = default;
            return (Math.Clamp(Search(searchDepth), -1000, 1000) / 1000.0f, bestRootMove); //Cant be (Move, int). Evaluation order!

            int Search(int depth, int alpha = -40000, int beta = 40000, int material = 0, bool notRoot = false)
            {
                // Quiescence & eval
                if (depth <= 0) alpha = Math.Max(alpha, material * 200 + board.GetLegalMoves().Length);  //eval = material + mobility
                                                                                                         //no beta cutoff check here, it will be done latter
                ulong zobristKey = board.ZobristKey;
                int newTTFlag = 2, bestEval = -9999999;
                ref var TTEntry = ref TT[zobristKey & 0x7FFFFF];
                var (entryKey, bestMove, entryScore, entryDepth, entryFlag) = TTEntry;

                if (TTEntry.Item1 == zobristKey && notRoot && entryDepth >= depth && Math.Abs(entryScore) < 50000
                && entryFlag != 3 | entryScore >= beta
                && entryFlag != 2 | entryScore <= alpha)
                    return entryScore;

                foreach (Move move in board.GetLegalMoves(depth <= 0)
                    .OrderByDescending(move => (move == bestRootMove ? 1 : 0, move.CapturePieceType, 0 - move.MovePieceType)))
                {
                    if (alpha >= beta)
                    {
                        newTTFlag = 3;
                        break;
                    }

                    board.MakeMove(move);
                    int score =
                    board.IsDraw() ? 0 :
                        board.IsInCheckmate() ? 30000 :
                        -Search(depth - 1, -beta, -alpha, -material - move.CapturePieceType - move.PromotionPieceType, true);


                    if (score > bestEval)
                    {
                        bestEval = score;
                        if (score > alpha)
                        {
                            bestMove = move;
                            alpha = score;
                            newTTFlag = 1;
                            if (depth == searchDepth)
                                bestRootMove = move;
                        }
                    }

                    board.UndoMove(move);
                }

                // Transposition table insertion
                TTEntry = (zobristKey, bestMove, bestEval, depth, newTTFlag);

                return alpha;
            }
        }
    }

    static (ulong, Move, int, int, int)[] TT = new (ulong, Move, int, int, int)[0x800000];
}