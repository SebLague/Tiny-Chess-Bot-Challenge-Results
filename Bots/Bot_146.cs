namespace auto_Bot_146;
using ChessChallenge.API;

public class Bot_146 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    int[] fileValues = { 0, 100, 200, 300, 300, 200, 100, 0 };
    int[] rankValues = { 0, 100, 200, 300, 300, 300, 200, 100 };

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        Move bestMove = moves[0];
        int bestMoveScore = -10000000;

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                return move;
            }

            int moveScore = 0;

            if (move.IsEnPassant || move.IsCastles)
            {
                moveScore += 400;
            }

            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveScore -= 900;
            }

            // optimize for endgame
            if (GetNumberOfOpponentPieces(board) < 5)
            {
                // incourage to move the pawns up
                if ((int)move.MovePieceType == 1)
                {
                    moveScore += 200;
                }
            }

            // encourage to move pieces to the center of the board
            // subtract the file and rank from the startSquare
            // to turn Pieces away from the center
            int file = move.TargetSquare.File;
            int rank = move.TargetSquare.Rank;
            int fileStart = move.StartSquare.File;
            int rankStart = move.StartSquare.Rank;

            moveScore += ((fileValues[file] - fileValues[fileStart] + rankValues[rank] - rankValues[rankStart]) / 4);

            // stop the bot from moving the king too much
            if ((int)move.MovePieceType == 6)
            {
                moveScore -= 300;
            }

            if (move.IsCapture)
            {
                PieceType capture = move.CapturePieceType;
                moveScore += pieceValues[(int)capture];
            }

            // check if the opponents next Move is a Mate
            if (checkForOpponentMate(board, move))
            {
                moveScore -= 99999;
            }

            // if the move has already been played reduce its value
            if (moveHasBeenPlayed(board, move))
            {
                moveScore -= 400;
            }

            // subtract the move score if the opponent can make good captures after our move
            moveScore -= (getHighestValueOfPieceOpponentCanCapture(board, move) / 4);

            // if the new Move is the current best update scores
            if (moveScore > bestMoveScore)
            {
                bestMove = move;
                bestMoveScore = moveScore;
            }
        }

        return bestMove;
    }

    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    int GetNumberOfOpponentPieces(Board board)
    {
        int numberOfOpponentPieces = 0;

        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList pieces in allPieces)
        {
            if (board.IsWhiteToMove)
            {
                // check for black pieces
                if (!pieces.IsWhitePieceList)
                {
                    numberOfOpponentPieces += pieces.Count;
                }
            }
            else
            {
                // check for white pieces
                if (pieces.IsWhitePieceList)
                {
                    numberOfOpponentPieces += pieces.Count;
                }
            }
        }

        return numberOfOpponentPieces;
    }

    bool checkForOpponentMate(Board board, Move nextMove)
    {
        board.MakeMove(nextMove);

        // loop through the opponents moves
        Move[] moves = board.GetLegalMoves();

        foreach (Move move in moves)
        {
            if (MoveIsCheckmate(board, move))
            {
                board.UndoMove(nextMove);
                return true;
            }
        }

        board.UndoMove(nextMove);
        return false;
    }

    bool moveHasBeenPlayed(Board board, Move move)
    {
        foreach (Move used in board.GameMoveHistory)
        {
            if (move.Equals(used))
            {
                return true;
            }
        }

        return false;
    }

    /**
     * get the highest value of Piece the opponent can get after the bots move
     */
    int getHighestValueOfPieceOpponentCanCapture(Board board, Move move)
    {
        int bestPieceValue = 0;
        board.MakeMove(move);

        // get the legal moves the opponent can make
        foreach (Move oppMove in board.GetLegalMoves())
        {
            if (move.IsCapture)
            {
                PieceType capture = move.CapturePieceType;
                int pieceValue = pieceValues[(int)capture];

                if (pieceValue > bestPieceValue)
                {
                    bestPieceValue = pieceValue;
                }
            }
        }

        board.UndoMove(move);

        return bestPieceValue;
    }
}