namespace auto_Bot_576;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MCTSNode
{
    public static double explorationWeight = 1.0f; // Determines how much we want to explore unvisited nodes vs. exploiting visited nodes
    public Move action; // The move that lead to this node
    public List<MCTSNode> children; // The children of this node
    public MCTSNode parent; // The parent of this node
    public int numberOfTimesVisited; // The number of times this node has been visited
    public double averageValue; // The average value of this node


    /* 
     * The UCB is a function that determines how valuable it would be to
     * visit this node based its value and how many times we've visited it.
     * 
     * When exploring the tree, we always choose the node with the highest UCB value
     */
    public double UCB
    {
        get
        {
            var logNParent = Math.Log(parent.numberOfTimesVisited);
            return averageValue + explorationWeight * Math.Sqrt(logNParent / numberOfTimesVisited);
        }
    }

    public MCTSNode(Move action, List<MCTSNode> children, MCTSNode parent, int numberOfTimesVisited, float averageValue)
    {
        this.action = action;
        this.children = children;
        this.parent = parent;
        this.numberOfTimesVisited = numberOfTimesVisited;
        this.averageValue = averageValue;
    }
}

/*
 * This bot's implementation of MCTS is mainly from these two articles
 * https://gibberblot.github.io/rl-notes/single-agent/mcts.html
 * https://towardsdatascience.com/monte-carlo-tree-search-an-introduction-503d8c04e168
 * 
 * Unfortunately due to having a bunch of college work to do, I didn't have nearly as much
 * free time to work on this as I wanted. As a result this AI is basically just a 
 * slightly smarter version of EvilBot that uses Monte Carlo Tree Search to think about
 * future board states.
 * 
 * If this piece of garbage wins even a single game I will be shocked, but at least I learned about MCTS while making it.
 */

public class Bot_576 : IChessBot
{
    private MCTSNode _rootNode;
    private Random _random;
    private int _movesExpanded;
    private bool _isPlayerWhite;
    private const int NUM_ROLLOUTS = 20;
    private const int SIMULATION_DEPTH = 20;
    private readonly int[] PIECE_WEIGHTS = { 1, 3, 3, 5, 9, 0, 1, 3, 3, 5, 9 };

    public Move Think(Board board, Timer timer)
    {
        // Set up some initial information for the MCTS
        _isPlayerWhite = board.IsWhiteToMove;
        _rootNode = new MCTSNode(Move.NullMove, new(), null, 0, 0);
        _random = new Random();

        // Keep running MCTS until we're out of time for this turn

        while (timer.MillisecondsElapsedThisTurn < 100)
        {
            RunMCTS(_rootNode, board);
        }

        // Sort the moves by their fitness (descending)

        _rootNode.children.Sort(ReverseCompareMCTSNodes);
        //DivertedConsole.Write("Moves Expanded: {0}, Final Weight: {1}", _movesExpanded, _rootNode.children[0].averageValue);

        // Return the best move

        return _rootNode.children[0].action;
    }

    /*
     * This function is where the MCTS selection, expansion, and backpropogation is. 
     */

    private float RunMCTS(MCTSNode currentNode, Board board)
    {
        Move[] moves = board.GetLegalMoves();

        currentNode.numberOfTimesVisited++;

        // If every child of this node has been expanded, then we move down into
        // one of the expanded children and select one of it's children
        if (moves.Length == currentNode.children.Count)
        {

            // If there are no moves left, then the game has ended
            if (moves.Length == 0)
            {
                return 0;
            }

            // Iterate over each child node and find the one with the most UCB
            MCTSNode bestNode = currentNode.children[0];
            double bestUCB = bestNode.UCB;

            foreach (MCTSNode node in currentNode.children)
            {
                double nodeUCB = node.UCB;

                if (nodeUCB > bestUCB)
                {
                    bestNode = node;
                    bestUCB = nodeUCB;
                }
            }

            // Move into the best node
            board.MakeMove(bestNode.action);
            bestNode.numberOfTimesVisited++;

            // Get the list of possible opponent responses
            var possibleOpponentMoves = new List<Move>(board.GetLegalMoves());

            if (possibleOpponentMoves.Count() == 0)
            {
                board.UndoMove(bestNode.action);

                bestNode.averageValue = bestNode.averageValue + 1 / bestNode.numberOfTimesVisited * (1 - bestNode.averageValue);
                return 1;
            }

            // Choose a random response
            var opponentMove = possibleOpponentMoves[_random.Next(possibleOpponentMoves.Count())];

            // If the response is already a MCTS node, then get that node
            MCTSNode opponentMoveNode = null;

            foreach (MCTSNode expandedMove in bestNode.children)
            {
                if (expandedMove.action == opponentMove)
                {
                    opponentMoveNode = expandedMove;
                    break;
                }
            }

            // If the response is not a MCTS node, then make a new MCTS node
            if (opponentMoveNode == null)
            {
                opponentMoveNode = new MCTSNode(opponentMove, new(), bestNode, 1, 0);

                bestNode.children.Add(opponentMoveNode);
            }

            // Move into the resulting board state from the opponent's move
            board.MakeMove(opponentMoveNode.action);
            opponentMoveNode.numberOfTimesVisited++;

            // Recursively get the value of the board state a layer down
            var averageValue = RunMCTS(opponentMoveNode, board);

            // The opponent's average value is the reverse of the average value
            opponentMoveNode.averageValue = -averageValue;

            // The value of the best node is updated using the backpropagation equation from this article
            // https://gibberblot.github.io/rl-notes/single-agent/mcts.html
            bestNode.averageValue = bestNode.averageValue + 1 / bestNode.numberOfTimesVisited * (averageValue - bestNode.averageValue);

            // Undo the two moves that were made to return to our original board state
            board.UndoMove(opponentMoveNode.action);

            board.UndoMove(bestNode.action);

            // Return the value of this path
            return averageValue;
        }
        else
        {
            // Filter out all of the moves that we have already explored from the list of possible moves
            var allMoves = new List<Move>(moves);

            foreach (MCTSNode expandedNode in currentNode.children)
                allMoves.Remove(expandedNode.action);

            // Not sure how this could trigger, but if it does then return the fitness of the board to be safe
            if (allMoves.Count() == 0)
                return CalculateFitness(board);

            // Choose a random unexplored move to expand
            var expandMove = allMoves[_random.Next(allMoves.Count)];

            float totalFitness = 0;

            // Move into the board that results from that move
            board.MakeMove(expandMove);

            // Simulate the board a bunch of times to get a total fitness

            for (int i = 0; i < NUM_ROLLOUTS; i++)
                totalFitness += Simulate(board);

            totalFitness /= NUM_ROLLOUTS;

            _movesExpanded++;

            // Make a new MCTS node for the expanded board state
            currentNode.children.Add(new MCTSNode(expandMove, new(), currentNode, 1, totalFitness));

            // Undo the move we made earlier
            board.UndoMove(expandMove);

            return totalFitness;
        }
    }

    /*
     * This method goes deep into the search tree by making random moves, and then
     * returns the fitness of the resulting board state. If we average the fitness
     * over a ton of simulations we should get a fairly good idea of how good the
     * current node is.
     */
    private float Simulate(Board board)
    {
        // The stack to store the moves we made
        var simulatedMoves = new Stack<Move>();

        // Keep making moves until we reach a depth limit
        while (simulatedMoves.Count < SIMULATION_DEPTH)
        {

            // Get all of the possible legal moves
            var moves = board.GetLegalMoves();

            // If there are no possible moves, then stop here
            if (moves.Length == 0)
                break;

            // Make a random move
            var randomMove = moves[_random.NextInt64(moves.Length)];
            simulatedMoves.Push(randomMove);
            board.MakeMove(randomMove);
        }

        // Get the fitness of the resulting board state
        var fitness = CalculateFitness(board);

        // Undo all of the moves we made in reverse order
        while (simulatedMoves.Count > 0)
        {
            board.UndoMove(simulatedMoves.Pop());
        }

        return fitness;
    }

    /*
     * This is the fitness function that evaluates how good a position is for us.
     * 
     * The algorithm is basically just a copy of EvilBot's algorithm, which tries to 
     * maximize the total value of its pieces and minimize the total value of its opponent's pieces.
     * 
     * All of the different features that I tried to add to it ended up severely reducing the bot's
     * performance, my guess is because their weights weren't properly calibrated.
     * 
     * I don't have time to fix that, so I guess a copy of EvilBot is the best I can do.
     */

    private float CalculateFitness(Board board)
    {
        bool isOurTurn = board.IsWhiteToMove == _isPlayerWhite;

        // The color of the player whose turn it is (player or opponent)
        // 1 for white, -1 for black
        int playerColor = board.IsWhiteToMove ? 1 : -1;

        if (board.IsInCheckmate())
            return !isOurTurn ? 1 : -1;

        if (board.IsDraw())
            return 0;

        var pieceLists = board.GetAllPieceLists();
        float fitness = 0;

        // Go through the piece lists and add to the fitness based on what color the current player is
        for (int i = 0; i < 6; i++)
        {
            fitness += pieceLists[i].Count() * PIECE_WEIGHTS[i] * playerColor;
        }

        for (int i = 7; i < pieceLists.Length - 1; i++)
        {
            fitness += pieceLists[i].Count() * PIECE_WEIGHTS[i] * -playerColor;
        }

        // Normalize the piece score based on the maximum possible piece value
        fitness /= 39;

        // If the fitness algorithm evaluated our position, then return positive
        // If it evaluated the opponent's position, then return negative
        return isOurTurn ? fitness : -fitness;
    }

    // A simple comparison for sorting the final list of moves
    private int ReverseCompareMCTSNodes(MCTSNode node1, MCTSNode node2)
    {
        return -node1.averageValue.CompareTo(node2.averageValue);
    }
}