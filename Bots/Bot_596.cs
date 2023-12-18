namespace auto_Bot_596;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Node
{
    public Move move { get; set; }
    public float value { get; set; }
    public int timesTraversed { get; set; }
}
public class TreeNode
{
    //data = move, its value, how many times it's been visited before
    public Node Data { get; set; }
    public TreeNode Parent { get; set; }
    public List<TreeNode> Children { get; set; }
}
public class Tree
{
    public TreeNode Root { get; set; }
}
public class Bot_596 : IChessBot
{
    //setting up an evaluation function for each node in the MCTS tree
    //currently UCB1
    // Piece values: pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 1, 3, 3, 5, 9, 0 };

    //this function evalutes a node based on the UCB1 function
    public float EvaluateNode(Node Data, int counter)
    {
        if (Data.timesTraversed == 0)
        {
            return 1000000;
        }
        float evaluation = Data.value / Data.timesTraversed + 0.9f * (float)Math.Sqrt((float)Math.Log(counter) / Data.timesTraversed);
        return evaluation;
    }

    public Move Think(Board board, Timer timer)
    {
        //Declaring general variables
        var startTime = DateTime.UtcNow;
        Random rngMovePicker = new();


        Tree MCTSTree = new Tree() { Root = new TreeNode() { Data = new Node() { value = 0f, timesTraversed = 0 }, Children = new List<TreeNode>() } };


        List<Move> possibleMoves = board.GetLegalMoves().ToList();

        int counter = 0;


        //default move
        Move moveToPlay = possibleMoves[0];

        //adding the possible moves as children of the root node of the tree
        foreach (Move move in possibleMoves)
        {
            MCTSTree.Root.Children.Add(new TreeNode() { Data = new Node() { move = move, value = 0f, timesTraversed = 0 }, Parent = MCTSTree.Root, Children = new List<TreeNode>() });
        }
        //starting the mcts loop
        while (DateTime.UtcNow - startTime < TimeSpan.FromSeconds(Math.Ceiling(timer.MillisecondsRemaining / 12000f) / 5f))
        {


            //Declaration of variables
            List<Move> MCTMovesPlayed = new List<Move>(); //this should be able to be removed
            List<Move> rolloutMovesPlayed = new List<Move>();



            //SELECTION OF LEAF NODE
            TreeNode leafNode = MCTSTree.Root;

            while (leafNode.Children.Count != 0 && !board.IsInCheckmate() && !board.IsDraw())
            {
                //adding to timesTraversed (up here so that the actual leaf node has timesTraversed = 0)
                leafNode.Data.timesTraversed++;
                float maxNodeValue = -1000000000f;
                TreeNode bestNode = new TreeNode(); //I think I can remove this, but not sure
                //setting the leaf node equal to the child that has the max value according to EvaluateNode (there should be a shorter way to do this)
                foreach (TreeNode child in leafNode.Children)
                {

                    //evaluating the node according to UCB1
                    float value = EvaluateNode(child.Data, leafNode.Data.timesTraversed);
                    //finding the max value and thereby the best node
                    if (value > maxNodeValue)
                    {
                        maxNodeValue = value;
                        bestNode = child;
                    }
                }

                if (bestNode == new TreeNode())
                {
                    DivertedConsole.Write("Error: bestNode is not defined");
                    bestNode = leafNode.Children[0];
                }


                //progressing the leaf node down the tree
                leafNode = bestNode;
                Move moveToBeMade = leafNode.Data.move;
                //progressing the board down the tree as well
                board.MakeMove(moveToBeMade);
                MCTMovesPlayed.Add(leafNode.Data.move);
            }

            //checking if the game is already over. if so, no need for expansion and rollout
            //assume it's a draw (reward 0), then add or subtract
            float endBoardValue = 0;
            if (board.IsInCheckmate())
            {
                //NOTE: we are calculating rewards objectively now, from white's point of view
                if (board.IsWhiteToMove)
                {
                    //if they got mated, negative reward
                    endBoardValue -= 100;
                }
                else
                {
                    endBoardValue += 100;
                }

            }
            else if (!board.IsDraw())
            {
                //EXPANSION OF TREE

                //if the leaf node has been traversed before, add new nodes
                if (leafNode.Data.timesTraversed != 0)
                {
                    //get all possible moves in the position
                    possibleMoves = board.GetLegalMoves().ToList();
                    foreach (Move move in possibleMoves)
                    {
                        //add each possible move as a leaf node
                        leafNode.Children.Add(new TreeNode() { Data = new Node() { move = move, value = 0f, timesTraversed = 0 }, Parent = leafNode, Children = new List<TreeNode>() });
                    }
                    //we are about to traverse this leaf node another time, so increment counter
                    leafNode.Data.timesTraversed++;

                    //since the new nodes haven't been explored, the first is as good as a random one (to save tokens)

                    leafNode = leafNode.Children[rngMovePicker.Next(leafNode.Children.Count)];

                    //progressing the board down the tre

                    board.MakeMove(leafNode.Data.move);
                    MCTMovesPlayed.Add(leafNode.Data.move);
                }
                //else, increment times traversed counter (should be a way to remove this later)
                else
                {
                    leafNode.Data.timesTraversed++;
                }


                //evaluating the position based on piece values
                int valueOfPositionByPieces = 0;
                List<PieceList> pieces = board.GetAllPieceLists().ToList();
                for (int i = 0; i < pieces.Count; i++)
                {
                    //I think this works. Basically, the first two elements make up the value of that piece, and the last element decides whether it is a white or black piece
                    valueOfPositionByPieces += pieces[i].Count * pieceValues[i % 6] * (1 - 2 * (i / 6));
                }

                //If we want to add the value of the position by pieces
                endBoardValue += valueOfPositionByPieces;



                //ROLLOUT ALGORITHM
                while (!board.IsInCheckmate() && !board.IsDraw())
                {
                    Move[] moves = board.GetLegalMoves();
                    moveToPlay = moves[rngMovePicker.Next(moves.Length)];
                    board.MakeMove(moveToPlay);
                    rolloutMovesPlayed.Add(moveToPlay);
                }


                if (board.IsInCheckmate())
                {
                    //checking if white or black was mated
                    if (board.IsWhiteToMove)
                    {
                        //if white got mated, negative reward
                        endBoardValue -= 1;
                    }
                    else
                    {
                        endBoardValue += 1;
                    }
                }
                rolloutMovesPlayed.Reverse();
                foreach (Move move in rolloutMovesPlayed)
                {
                    board.UndoMove(move);
                    //endBoardValue *= gamma; //(in case we want) to use gamma to value positions where checkmate is faster better
                }


            }
            //BACKPROPAGATION
            while (leafNode != MCTSTree.Root)
            {
                //should I update timesTraversed down here instead of at the beginning?
                //checking whose turn it is, and applying the reward based on that 
                //(that way, black will make the best moves for black, and white will make the best moves for white)
                if (board.IsWhiteToMove)
                {
                    leafNode.Data.value -= endBoardValue;
                }
                else
                {
                    leafNode.Data.value += endBoardValue;
                }
                board.UndoMove(leafNode.Data.move);
                leafNode = leafNode.Parent;
            }

            //DivertedConsole.Write("Iteration:" + counter);
            //updating values
            counter++;

        }
        float maxValue = -10000f;
        foreach (TreeNode child in MCTSTree.Root.Children)
        {
            float value = child.Data.value / child.Data.timesTraversed;

            if (value > maxValue)
            {
                maxValue = value;
                moveToPlay = child.Data.move;
            }
        }
        return moveToPlay;
    }
}