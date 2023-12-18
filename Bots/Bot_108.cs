namespace auto_Bot_108;
using ChessChallenge.API;

public class Bot_108 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        int maxAttackedSquares = 0;
        int moveIdx = 0;

        // Check over all potential moves...
        for (int i = 0; i < moves.Length; i++)
        {
            // Check if the move is safe, ignore it if not. I.e. don't hang your pieces
            if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
            {
                continue;
            }


            // Apply the move to the board
            board.MakeMove(moves[i]);

            // Did the move give checkmate?
            if (board.IsInCheckmate())
            {
                // Of course we're going to play it!
                return moves[i];
            }

            // ======================== TODO =============================
            // Decide on how to go about chasing checks - we don't want to start searching multiple moves in the future
            // so how do we know if a check is worth playing?

            // Does the move give check?
            if (board.IsInCheck())
            {
                // Might as well play it for now, though it's not ideal - there could be a better check for example
                return moves[i];
            }

            // Will we capture a piece by playing the move?
            if (moves[i].IsCapture)
            {
                // Meh, go for it
                return moves[i];
            }

            // ======================== TODO =============================
            // Are all our pieces safe?




            // Check how many squares we would attack with the move
            ulong bitAttackedSquares = BitboardHelper.GetPieceAttacks(moves[i].MovePieceType, moves[i].TargetSquare, board, !board.IsWhiteToMove);

            // Ignore squares in our own territory
            int rankStart = board.IsWhiteToMove ? 6 : 0;

            for (int file = 0; file < 8; file++)
            {
                for (int rank = rankStart; rank < rankStart + 2; rank++)
                {
                    BitboardHelper.ClearSquare(ref bitAttackedSquares, new Square(file, rank));
                }
            }

            // See how many new squares we are attacking
            // This strongly encourages knights to dive into the opposing territory - not great
            int numAttackedSquares = BitboardHelper.GetNumberOfSetBits(bitAttackedSquares);
            if (numAttackedSquares > maxAttackedSquares)
            {
                moveIdx = i;
                maxAttackedSquares = numAttackedSquares;
            }

            // Revert the board's state
            board.UndoMove(moves[i]);
        }

        return moves[moveIdx];
    }

    int GetAttackedSquares(Board board)
    {
        // Only care about squares on the opponent's side of the board
        // but remember that at this stage, we are looking at the board from
        // the opponent's perspective because we have applied our own move to the board
        int rankStart = board.IsWhiteToMove ? 0 : 2;
        int numAttackedSquares = 0;

        for (int file = 0; file < 8; file++)
        {
            for (int rank = rankStart; rank < rankStart + 6; rank++)
            {
                numAttackedSquares += board.SquareIsAttackedByOpponent(new Square(file, rank)) ? 1 : 0;
            }
        }

        return numAttackedSquares;
    }
}