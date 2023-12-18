namespace auto_Bot_615;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Bot_615 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // ANT COLONY OPTIMIZATION ALGHORITM
        //
        // Using ant colony optimization to find the best move in chess is a not a common approach as far as I know
        // I haven't found any papers or articles about this topic, so I decided to try it out for fun in this competition, but it has been used to solve at least one chess related problem (https://theconversation.com/how-to-get-ants-to-solve-a-chess-problem-22282)
        //
        // The basic idea behind Ant colony optimization algorithms is to simulate the behavior of ants when they are looking for food using their pheromones.
        // A large number of "ants" are sent along the possible paths in a pretty random way. When they find an interesting path they leave a pheromone trail behind them, calling more ants to explore that path instead of boring ones.
        //
        // The ant will keep making moves until a minimum depth is reached and position is quiescent enought, then a static evaluation is ran. Nodes with higher pheromone may be explored at a higher depth.
        // All nodes starts with a pheromone value of 0.0, and when a node is visited the pheromone value is multiplied by a constant to simulate the evaporation of the pheromone.
        //
        // If the final position is advantageous for white, the pheromone value is increased by a constant to simulate the ants leaving a pheromone trail,
        // likewise if the position is advantageous for black the pheromone value is decreased.
        //
        // When searching for white, we want to maximize the pheromone value, and when searching for black we want to minimize it.
        // After all the ants have finished their search, the move with the highest pheromone value is returned.

        //
        // Result:
        // Cute idea, it hang pieces
        //



        // Decide how much time to allocate to this move
        // A chess game has on average ~84 moves. http://facta.junis.ni.ac.rs/acar/acar200901/acar2009-07.pdf
        // Since our bot sucks, try gaining an edge on the first moves. So assume our average games are only 50 moves long, (or at least lasting another 10 moves if we go beyond 50)
        int moves_remaining = Math.Max(50 - board.PlyCount, 10);
        int ms_move = (timer.MillisecondsRemaining + moves_remaining * timer.IncrementMilliseconds) / moves_remaining;


        NestedList root = new(new(), 0, null);

        // let the ants go
        int ants = 0;
        while (timer.MillisecondsElapsedThisTurn < ms_move)
        {
            ++ants;

            NestedList current_node = root;
            int depth = 0;

            do
            {
                NestedList? next_node = current_node.DoMove(board, board.IsWhiteToMove);
                if (next_node == null) break;

                current_node = next_node;

                depth++;
            } while ((depth < 4 || board.IsInCheck() || current_node.Move.IsPromotion || current_node.Move.IsCapture || current_node.Move.IsCastles)
                    && depth < 10);

            int score = Evaluate(board);

            current_node.SetPheromone(board, score);
        }

        DivertedConsole.Write("Ants count: " + ants);

        return root.BestMove(board.IsWhiteToMove);
    }


    static int Evaluate(Board board)
    {
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate())
        {
            return board.IsWhiteToMove ? int.MinValue : int.MaxValue;
        }

        int score =
              10 * board.GetPieceList(PieceType.Pawn, true).Count
            + 30 * board.GetPieceList(PieceType.Bishop, true).Count
            + 30 * board.GetPieceList(PieceType.Knight, true).Count
            + 50 * board.GetPieceList(PieceType.Rook, true).Count
            + 90 * board.GetPieceList(PieceType.Queen, true).Count
            - 10 * board.GetPieceList(PieceType.Pawn, false).Count
            - 30 * board.GetPieceList(PieceType.Bishop, false).Count
            - 30 * board.GetPieceList(PieceType.Knight, false).Count
            - 50 * board.GetPieceList(PieceType.Rook, false).Count
            - 90 * board.GetPieceList(PieceType.Queen, false).Count;

        bool white = board.IsWhiteToMove;

        if (white)
        {
            score += (int)(board.GetLegalMoves().Length * 0.5);
            if (board.TrySkipTurn())
            {
                score -= (int)(board.GetLegalMoves().Length * 0.5);
                board.UndoSkipTurn();
            }
        }
        else
        {
            score -= (int)(board.GetLegalMoves().Length * 0.5);
            if (board.TrySkipTurn())
            {
                score += (int)(board.GetLegalMoves().Length * 0.5);
                board.UndoSkipTurn();
            }
        }

        return score;
    }
}

public class NestedList
{
    public int Pheromone { get; set; }
    public Move Move { get; set; }
    public List<NestedList> Children { get; set; }

    public bool SubMovesLoaded { get; set; } = false;
    public NestedList? Parent { get; }

    public NestedList(Move move, int pheromone, NestedList? parent)
    {
        Move = move;
        Pheromone = pheromone;
        Children = new();
        Parent = parent;
    }

    public void SetChildrenMoves(Move[] moves)
    {
        foreach (Move move in moves)
        {
            Children.Add(new(move, 0, this));
        }
    }

    // Make random move, with higher probability of one with higher (or lower for black pheromone)
    public NestedList? DoMove(Board board, bool white)
    {
        // Evaporation
        Pheromone /= 95;
        Pheromone *= 100;

        if (!SubMovesLoaded)
        {
            SetChildrenMoves(board.GetLegalMoves());
            SubMovesLoaded = true;
        }

        if (Children.Count == 0)
        {
            return null;
        }

        if (white)
            Children.Sort((a, b) => b.Pheromone.CompareTo(a.Pheromone));
        else
            Children.Sort((a, b) => a.Pheromone.CompareTo(b.Pheromone));

        Random rand = new();
        foreach (NestedList child in Children)
        {
            int random = rand.Next(0, 100);
            if (random < (100 / Children.Count * 2))
            {
                board.MakeMove(child.Move);
                return child;
            }
        }
        board.MakeMove(Children.Last().Move);
        return Children.Last();
    }

    public void SetPheromone(Board board, int score)
    {
        if (Parent != null)
        {
            double new_score_weight = 0.1;

            Pheromone = (int)((Pheromone + score * new_score_weight) / (1.0 + new_score_weight));
            board.UndoMove(Move);
            Parent.SetPheromone(board, score);
        }
    }

    // Returns the Move of the child with highest pheromone value for white, and the lowest for black
    public Move BestMove(bool white)
    {
        int best_pheromone = white ? int.MinValue : int.MaxValue;
        NestedList best_child = new(new(), 0, null);

        foreach (NestedList child in Children)
        {
            if (white && child.Pheromone > best_pheromone)
            {
                best_pheromone = child.Pheromone;
                best_child = child;
            }
            else if (!white && child.Pheromone < best_pheromone)
            {
                best_pheromone = child.Pheromone;
                best_child = child;

            }
        }

        /*
        DivertedConsole.Write("Best move: " + best_child.Move);
        if(best_child.SubMovesLoaded) {
            best_child.BestMove(white);
        }
        DivertedConsole.Write("----");*/

        return best_child.Move;
    }
}
