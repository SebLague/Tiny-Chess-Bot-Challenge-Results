namespace auto_Bot_28;
using ChessChallenge.API;

public class Bot_28 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {

        Move[] moves = board.GetLegalMoves();
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].IsEnPassant)
            {
                return moves[i];
            }
        }
        PieceList opponentPawns = board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove);
        PieceList friendlyPawns = board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove);
        for (int i = 0; i < opponentPawns.Count; i++)
        {

            Piece opponentPawn = opponentPawns.GetPiece(i);
            Square opponentPawnSquare = opponentPawn.Square;
            int rank = opponentPawnSquare.Rank;
            if (!board.IsWhiteToMove)
            {
                rank -= 1;
            }

            if (rank != 3)
            {
                continue;
            }

            for (int f = 0; f < friendlyPawns.Count; f++)
            {
                Piece friendlyPawn = friendlyPawns.GetPiece(f);
                Square friendlyPawnSquare = friendlyPawn.Square;
                int fRank = friendlyPawnSquare.Rank;
                if (!board.IsWhiteToMove)
                {
                    fRank -= 4;
                }

                if (fRank != 2)
                { continue; }

                int file = opponentPawnSquare.File;

                if (friendlyPawnSquare.File != file + 1 && friendlyPawnSquare.File != file - 1)
                { continue; }


                for (int m = 0; m < moves.Length; m++)
                {
                    if (moves[m].StartSquare.Name == friendlyPawnSquare.Name && moves[m].TargetSquare.Rank == opponentPawnSquare.Rank)
                    {
                        return moves[m];
                    }
                }
            }

        }
        //so what I want to do next is make the AI bongcloud. I do this by playing e4 or e5, then playing ke2 or ke7.
        for (int m = 0; m < moves.Length; m++)
        {
            int targetPawnRank = 3;
            int targetKingRank = 1;
            int targetfile = 4;
            if (!board.IsWhiteToMove)
            {
                targetPawnRank += 1;
                targetKingRank += 5;
            }
            Square targetSquare = moves[m].TargetSquare;
            if ((targetSquare.File == targetfile && targetSquare.Rank == targetPawnRank) || (targetSquare.File == targetfile && moves[m].MovePieceType == PieceType.King && targetKingRank == targetSquare.Rank))
            {
                return moves[m];
            }
        }
        System.Random rng = new();
        return moves[rng.Next(moves.Length)];
    }
}