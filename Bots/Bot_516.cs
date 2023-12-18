namespace auto_Bot_516;
using ChessChallenge.API;
using System.Collections.Generic;
using System.Linq;


//Apologies about my poor coding practices. This is my first time using C sharp, and I'm still new to OOP more generally.
//Good luck TacTxs!


//Apologies about my poor coding practices. This is my first time using C sharp, and I'm still new to OOP more generally.

public class Bot_516 : IChessBot
{
    Timer timer;
    Board board;
    Dictionary<ulong, TranspositionTableEntry> transpositionTable;
    double[,] squareValues = {
        //Pawn Value
        {
              0,   0,   0,   0,
            150, 150, 150, 150,
            110, 110, 120, 130,
            105, 105, 110, 125,
            100, 100, 100, 120,
            105,  95,  90, 100,
            105, 110, 110,  80,
              0,   0,   0,   0
        },
        //Knight Value
        {
            270, 280, 290, 290,
            280, 300, 320, 320,
            290, 320, 330, 335,
            290, 325, 335, 340,
            290, 320, 335, 340,
            290, 325, 330, 335,
            280, 300, 320, 325,
            270, 280, 290, 290
        },
        //Bishop Value
        {
            310, 320, 320, 320,
            320, 330, 330, 330,
            320, 330, 335, 340,
            320, 335, 335, 340,
            320, 330, 340, 340,
            320, 340, 340, 340,
            320, 335, 330, 330,
            310, 320, 320, 320
        },
        //Rook Value
        {
            500, 500, 500, 500,
            505, 510, 510, 510,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            495, 500, 500, 500,
            500, 500, 500, 505
        },
        //Queen Value
        {
            880, 890, 890, 895,
            890, 900, 900, 900,
            890, 900, 905, 905,
            895, 900, 905, 905,
            898, 900, 905, 905,
            890, 903, 905, 905,
            890, 900, 903, 900,
            880, 890, 890, 895
        },
        //King Value
        {
             70,  60,  60,  50,
             70,  60,  60,  50,
             70,  60,  60,  50,
             70,  60,  60,  50,
             80,  70,  70,  60,
             90,  80,  80,  80,
            120, 120, 100, 100,
            120, 130, 110, 100
        }
    };

    //Which square some piece is associated with for the purpsoes of the squareValues lookup table
    public int PieceSquare(Piece piece)
    {
        int square_of_piece = piece.Square.Index;

        //Directionalise based on white vs black pieces
        square_of_piece = piece.IsWhite ? 63 - square_of_piece : square_of_piece;

        //Symmetrise the chess board
        return square_of_piece - (square_of_piece % 8 * 2 - 7) * (square_of_piece / 4 % 2) - square_of_piece / 8 * 4;
    }

    //Board Evaluation: Defined to be the evaluation of the player whose turn it is.
    public double BoardEvaluation()
    {

        if (board.IsInCheckmate())
        {
            //Game loss: value neg inf
            return -100000;
        }
        if (board.IsDraw() || board.IsRepeatedPosition())
        {
            //Game draw: value = 0
            return 0;
        }

        double BoardEval = 0.0;
        for (int i = 2; i < 14; i++)
        {
            PieceList PList = board.GetPieceList((PieceType)(i / 2), i % 2 == 0);

            //Individual Pieces Eval
            for (int j = 0; j < PList.Count; j++)
            {
                //Update the board's eval based on the colour of the piece and the colour of the player's turn multiplied by the value of the piece (based on the squareValues lookup table)
                BoardEval += (i % 2 == 0 == board.IsWhiteToMove ? 1 : -1) * squareValues[i / 2 - 1, PieceSquare(PList.GetPiece(j))];
            }
        }

        return BoardEval;
    }

    //Heuristic for prioritising move search ordering
    double OrderingMovesPriority(Move move, Move? searchThisMoveFirst)
    {
        double priority_score = 0;
        Piece startPiece = board.GetPiece(move.StartSquare);

        //Captures should be searched earlier
        if ((int)move.CapturePieceType > 0)
        {
            //...especially if the piece is undefended
            priority_score -= board.SquareIsAttackedByOpponent(move.TargetSquare) ? 110 : 1110;

            Piece targetPiece = board.GetPiece(move.TargetSquare);

            //Need to treat enpassant separately, as for this move the target piece is not on the target square. So just fix the hardcoded value of enpassant.
            priority_score -= move.IsEnPassant ? 30 : squareValues[(int)targetPiece.PieceType - 1, PieceSquare(targetPiece)] - squareValues[(int)startPiece.PieceType - 1, PieceSquare(startPiece)];
        }

        //Prioritise checks
        board.MakeMove(move);
        priority_score -= board.IsInCheck() ? 150 : 0;
        board.UndoMove(move);

        //If previously decided a move is to be searched first, then give it first priority. Else prioritise promotions
        priority_score -= move.Equals(searchThisMoveFirst) ? 10000 : move.IsPromotion ? 800 : 0;
        return priority_score;
    }

    //Search the move space
    (Move?, double) Search(int depth, int max_depth, double alphaBestPlayerMoveScore, double betaBestOpponentMoveScore, bool CapturesOnly)
    {
        //If there is a check, search all moves, even if otherwise would have only done "CapturesOnly"
        bool effectiveCapturesOnly = CapturesOnly && !board.IsInCheck();
        double optimum_eval = -3000000; //neg inf
        Move? optimum_move = null;

        if (depth < max_depth || CapturesOnly)
        {
            Move? searchThisMoveFirst = null;

            //See if already evaluated position
            if (transpositionTable.TryGetValue(board.ZobristKey, out TranspositionTableEntry? entry))
            {
                //todo. Check alpha, beta
                if (entry.depth >= max_depth - depth || CapturesOnly)
                {
                    return (entry.Optimove, entry.value_best);
                }

                //If already evaluated this position, recall the best move in order to search it first at greater depth
                searchThisMoveFirst = entry.Optimove;
            }

            //Get all possible moves (captures) ordered according to heuristic
            Move[] moves = board.GetLegalMoves(effectiveCapturesOnly).OrderBy(x => OrderingMovesPriority(x, searchThisMoveFirst)).ToArray();

            //If terminal node, evaluate the board
            if (moves.Length == 0 || board.IsDraw())
            {
                return (optimum_move, BoardEvaluation());
            }

            //Else iterate through moves and evaluate each recursively
            for (int i = -1; i < moves.Length; i++)
            {
                //Begin with the null move evaluating the board as is (only applicable for "CapturesOnly" search - as it is important to check what happens if the opponent doesn't make a 'bad' capture)
                if (i < 0)
                {
                    if (effectiveCapturesOnly)
                    {
                        optimum_eval = BoardEvaluation();
                    }
                }
                else
                {
                    //For each move, make the move on the board and search the subnodes to estimate the position evaluation
                    board.MakeMove(moves[i]);

                    double node_evaluation = -Search(depth + 1, max_depth, -betaBestOpponentMoveScore, -alphaBestPlayerMoveScore, CapturesOnly).Item2;
                    //Negative because the better moves are the ones with a worse resulting board situation for the opponent
                    //Similarly, reverse alpha and beta as the "player" and "opponent" states have swapped

                    //If the current eval is the best yet seen, update the current champion move/eval.
                    if (node_evaluation > optimum_eval)
                    {
                        optimum_move = moves[i];
                        optimum_eval = node_evaluation;
                    }
                    //Undo the move to preserve the board state
                    board.UndoMove(moves[i]);
                }

                //Alphabeta pruning. If the current eval is better than a higher level's eval, the opponent would never pick this move. So stop searching
                if (optimum_eval >= betaBestOpponentMoveScore)
                {
                    break;
                }

                //Abandon the search if time is running out
                if (TimerCheckFail())
                {
                    optimum_eval = -1000000; //This node should be considered offlimits as it has not finished being searched.
                    break;
                }

                //If the current eval is the best ever seen, update the current champion evaluation.
                if (optimum_eval > alphaBestPlayerMoveScore)
                {
                    alphaBestPlayerMoveScore = optimum_eval;
                }
            }
        }
        else
        {
            //When reached max_depth, continue searching captures to fully evaluate position (rather than just evaluating the board presently)
            (optimum_move, optimum_eval) = Search(depth, max_depth, alphaBestPlayerMoveScore, betaBestOpponentMoveScore, true);
        }

        //Store the node result in the lookup table
        transpositionTable[board.ZobristKey] = new TranspositionTableEntry(optimum_eval, max_depth - depth, optimum_move);

        //Return the best move and associated eval
        return (optimum_move, optimum_eval);

    }

    //Determine if there is insufficient time to keep searching
    bool TimerCheckFail()
    {
        if (timer.MillisecondsRemaining > timer.GameStartTimeMilliseconds * 0.4)
        {
            return timer.MillisecondsElapsedThisTurn > timer.GameStartTimeMilliseconds / 45 + 1.5 * timer.IncrementMilliseconds;
        }
        return timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 15 + timer.IncrementMilliseconds;
    }

    public Move Think(Board board, Timer timer)
    {
        //globalise the board and transposition lookup tables
        this.board = board;
        this.timer = timer;
        this.transpositionTable = new Dictionary<ulong, TranspositionTableEntry>();

        Move? optimum_move = null;
        double move_eval = 0;
        int max_depth = 5;

        //iteratively deepen the seach from depth 1 upwards, recording the best move each time so if the search gets cancelled the best move is still output
        for (int searchDepth = 1; searchDepth <= max_depth; searchDepth++)
        {
            //the best moves according the current depth
            (optimum_move, move_eval) = Search(0, searchDepth, -2000000, 2000000, false);

            //if the eval is mate or running out of time, don't search deeper
            if (move_eval > 10000 || move_eval < -10000 || TimerCheckFail())
            {
                break;
            }
            //Use more time if available to go to greater depth
            if (searchDepth == max_depth && 80 * timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining + 60 * timer.IncrementMilliseconds)
            {
                max_depth += 1;
            }
        }

        //What it all amounts to
        return (Move)optimum_move;
    }
}


//The attributes of the transposition table entries: the node value, the depth at which this node occurred, and the best move to search first if deeper searches are to be made.
public class TranspositionTableEntry
{
    public double value_best;
    public int depth;
    public Move? Optimove;
    public TranspositionTableEntry(double value_best_, int depth_, Move? Optimove_)
    {
        value_best = value_best_;
        depth = depth_;
        Optimove = Optimove_;
    }
}

