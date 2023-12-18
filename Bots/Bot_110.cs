namespace auto_Bot_110;
using ChessChallenge.API;
using System;
using System.Linq;


//Please note: this bot makes use of some fun code that simply mirrors the players movement.
//However this behavior can be easily outsmarted by the player leaving the bot in a non-mirror able 
//position. To cope with this, ive added the example code of 'evil bot' for use when my bot doesn't
//know what to do. You could call this cheating, which I would be ok with, however I am still including
//it as otherwise my bot crashes pretty easily.
//This also means that the later in the game you get, the more and more the bot acts like evil bot as 
//mirroring is less and less possible.

//also while testing my bot (with evil bot included) i found it was actually somewhat better than evil bot?
// I have no-idea how I've managed that, but its kindof interesting.

//edit: out of 100 games it actually tied with evil bot, so i guess it was just an early lead (6 wins to 6 wins - 88 draws)
public class Bot_110 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 }; //THIS LINE IS USED IN THE EVIL BOT CODE, IT IS NOT MINE
    public Move Think(Board board, Timer timer) //my code starts here
    {
        Move[] legal_moves = board.GetLegalMoves();
        Move[] past_moves = board.GameMoveHistory;

        Move lastMove = new Move(); //defines last move out of the If statement
        Move final_move = new Move(); //defines final_move out of the If statement

        if (past_moves.Length > 0)
        {
            lastMove = past_moves[^1]; //if there is actually a move history
            final_move = InvertMove(lastMove, board); //'mirror' the move
        }
        else
        { //otherwise
            final_move = EvilbotMove(board, timer); //get the best 'evil bot' move
        }


        if (legal_moves.Contains(final_move))
        { //if the move generated is legal
            return final_move;
        }
        else
        { //otherwise
            return final_move = EvilbotMove(board, timer); //get the best 'evil bot' move
        }

    }

    public Move InvertMove(Move lastMove, Board board)
    {
        int[] inverted_nums = { 7, 6, 5, 4, 3, 2, 1, 0 };
        string[] inverted_letters = { "a", "b", "c", "d", "e", "f", "g", "h" };

        int invertedStartFile = inverted_nums[lastMove.StartSquare.File];
        int invertedStartRank = inverted_nums[lastMove.StartSquare.Rank];

        int invertedEndFile = inverted_nums[lastMove.TargetSquare.File];
        int invertedEndRank = inverted_nums[lastMove.TargetSquare.Rank];

        String StartSquare = inverted_letters[invertedStartFile] + (invertedStartRank + 1).ToString();
        String EndSquare = inverted_letters[invertedEndFile] + (invertedEndRank + 1).ToString();
        Move invertedMove = new Move(StartSquare + EndSquare, board);

        return invertedMove;
    }

    //WARNING: PLEASE NOTE THE CODE BELOW IS NOT MINE!!!
    //THIS IS THE CODE OF THE PROVIDED EVIL BOT, FOR USE WHEN MY BOT FAILS.
    //THIS IS MOSTLY BECAUSE I WASNT SURE HOW TO MAKE A PROPER CHESS BOT
    //THIS CODE CAN BE TAKEN OUT AND LEFT WITH MY BOT THAT WILL SIMPLY CRASH WHEN UNSURE OF HOW TO PROCEED
    public Move EvilbotMove(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        // Pick a random move to play if nothing better is found
        Random rng = new();
        Move moveToPlay = allMoves[rng.Next(allMoves.Length)];
        int highestValueCapture = 0;

        foreach (Move move in allMoves)
        {
            // Always play checkmate in one
            if (MoveIsCheckmate(board, move))
            {
                moveToPlay = move;
                break;
            }

            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                moveToPlay = move;
                highestValueCapture = capturedPieceValue;
            }
        }

        return moveToPlay;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }


}