namespace auto_Bot_309;
using ChessChallenge.API;
using System.Linq;

public class Bot_309 : IChessBot
{
    //// Mirror Move Things ////
    // a list of files to make my life easiear
    private string[] fileIndices = { "a", "b", "c", "d", "e", "f", "g", "h" };

    // mirrores the move given for the opposite side
    private Move mirrorMove(Move inMove, Board board)
    {
        // get move ranks
        int sr = inMove.StartSquare.Rank;
        int tr = inMove.TargetSquare.Rank;

        // mirror them
        sr = 7 - sr + 1;
        tr = 7 - tr + 1;

        // return the move
        return new Move(this.fileIndices[inMove.StartSquare.File] + sr.ToString() + this.fileIndices[inMove.TargetSquare.File] + tr.ToString(), board);
    }

    // might make a move that mirrors the opponent
    private Move makeMirroredMove(Board board)
    {
        // get all valid moves
        Move[] moves = board.GetLegalMoves();
        // get move history
        Move[] history = board.GameMoveHistory;

        if (history.Length > 0)
        {
            // get the latest move
            Move lastMove = history[history.Length - 1];
            // DEBUG: print last move
            //DivertedConsole.Write("[DEBUG MIRROR BOT] Last "+history[history.Length-1].ToString());
            // get mirrored move
            Move mirroredMove = this.mirrorMove(lastMove, board);
            // DEBUG: print move
            //DivertedConsole.Write("[DEBUG MIRROR BOT] Mirrored "+mirroredMove.ToString());

            // if move is legal, perform it
            if (!mirroredMove.IsNull && moves.Contains(mirroredMove))
            {
                return mirroredMove;
            }/* DEBUG: print if the mirrored move was illegal */ //else { DivertedConsole.Write("[DEBUG MIRROR BOT] Move was not legal, checking alternatives..."); }

            // otherwise check if there is a move with the same outcome
            for (int i = 0; i < moves.Length; i++)
            {
                // check if the resulting position matches the mirrored position
                if (moves[i].TargetSquare == mirroredMove.TargetSquare)
                {
                    // DEBUG: print the move if it go to the "second round"
                    //DivertedConsole.Write("[DEBUG MIRROR BOT] Current Alternative (2nd Round): "+moves[i].ToString());
                    // check if pieces are equal
                    if (moves[i].MovePieceType == lastMove.MovePieceType)
                    {
                        // DEBUG: print if an alternative piece was found
                        //DivertedConsole.Write("[DEBUG MIRROR BOT] Alternative found: "+moves[i].ToString());
                        return moves[i];
                    }
                }
            }
        }
        // if no valid move was found, return a null move
        return Move.NullMove;
    }

    //// "Good" Bot ////

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int evalMove(Move move, Board board)
    {
        int score = 0;
        // capture evaluation
        // check if its a capture,
        if (move.IsCapture)
        {
            int value = 0;
            // then get the value of the piece captured
            value = pieceValues[(int)move.CapturePieceType];
            // if its not the bots turn, invert value
            if (board.IsWhiteToMove != amIWhite) { value *= -1; }
            score += value;
        }
        return score;
    }

    // check all paths of a move
    private int branchCheck(Move move, Board board, int deepness = 0)
    {
        int value = 0;
        // get all resulting moves
        Move[] result = board.GetLegalMoves();
        // check all resulting moves
        for (int i = 0; i < result.Length; i++)
        {
            // if there is a new branch to be checked, make the current move
            if (deepness != 0) { board.MakeMove(result[i]); }
            // evaluate move
            value += evalMove(result[i], board);

            // if the move should go a layer deeper, do that
            if (deepness != 0) { value += branchCheck(result[i], board, deepness - 1); }

            // if the move was made, undo it
            if (deepness != 0) { board.UndoMove(result[i]); }
        }

        return value;
    }

    // make a "good" move
    private Move makeGoodMove(Board board)
    {
        // "good" move magic //

        // get all valid moves
        Move[] moves = board.GetLegalMoves();

        // eval vars
        int score = 0;
        Move bestmove = Move.NullMove;

        // go through all moves
        for (int i = 0; i < moves.Length; i++)
        {
            // eval move
            int value = evalMove(moves[i], board);
            // make the move
            board.MakeMove(moves[i]);

            // check the branch
            value += branchCheck(moves[i], board, 2);

            // if the move is better than the current best move, replace it
            if (value > score)
            {
                score = value;
                bestmove = moves[i];
            }

            // at the end, undo the move
            board.UndoMove(moves[i]);
        }

        // return the best move
        return bestmove;
    }

    //// vars ////

    bool mirror = true; // if true, the bot will, when it can, mirror the opponents move

    bool amIWhite = false;

    //// Think Function ////
    public Move Think(Board board, Timer timer)
    {
        // get all valid moves
        Move[] moves = board.GetLegalMoves();
        // get move history
        Move[] history = board.GameMoveHistory;

        if (mirror)
        { // incase the bot should try to make non-mirrored moves
            // get the mirrored move
            Move mmove = this.makeMirroredMove(board);
            // if the move is not a null move, return it
            if (mmove != Move.NullMove) { return mmove; }
        }

        // otherwise, make "good" moves (they may or maynot be bad)
        // get a "good" move
        Move gmove = this.makeGoodMove(board);
        // if the move is not a null move, return it
        if (gmove != Move.NullMove) { return gmove; }

        // as a last resort, pick a random move
        System.Random rng = new();
        return moves[rng.Next(moves.Length)];
    }
}