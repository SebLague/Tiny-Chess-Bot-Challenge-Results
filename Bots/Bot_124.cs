namespace auto_Bot_124;
using ChessChallenge.API;
using System;

public class Bot_124 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 100000 };

    // Tracks last 2 moves to try and reduce repititions
    string secondlastmove = "a1";
    string lastmove = "a1";

    public Move Think(Board board, Timer timer)
    {
        // Get a random variable
        Random rng = new();
        // Get moves and set up a rank array for moves
        Move[] moves = board.GetLegalMoves();
        double[] rank = new double[moves.Length];

        // Looks 2 moves ahead and ranks the moves
        for (int j = 0; j < moves.Length; j++)
        {

            rank[j] += depthrank(board, moves[j], 2, 2);
        }
        // Sorts the moves by rank
        System.Array.Sort(rank, moves);

        // Makes a choice, randomly choosing either the top choice or the top portions of the moves
        int choice = 0;
        int[] choices = { rng.Next(moves.Length), rng.Next((int)(moves.Length / 5)), rng.Next((int)(moves.Length / 10)), rng.Next((int)(moves.Length / 20)) };
        for (int i = 0; i < 100; i++)
        {
            if (rank[0] < -40000)
            {
                int[] rands = { 0, 0, 0, 0, choices[3] };
                choice = rands[rng.Next(rands.Length)];
            }
            else
            {
                int[] rands = { 0, 0, 0, choices[3], choices[2], choices[1] };
                choice = rands[rng.Next(rands.Length)];
            }

            // Tries to prevent repitions
            if (moves[choice].TargetSquare.Name != secondlastmove)
            {
                secondlastmove = lastmove;
                lastmove = moves[choice].TargetSquare.Name;
                return moves[choice];
            }
        }
        return moves[choices[0]];
    }

    // Counts the total value of the opponent's pieces
    double advantage(Board board)
    {
        // Get my color
        bool mycol = board.IsWhiteToMove;

        // Find number of my pieces vs opponent pieces
        PieceList[] pieces = board.GetAllPieceLists();
        double oppieces = 0;

        foreach (PieceList p in pieces)
        {
            if ((int)p.TypeOfPieceInList == 6) { continue; }
            if (p.IsWhitePieceList != mycol) { oppieces += p.Count * pieceValues[(int)p.TypeOfPieceInList]; }
        }

        return oppieces / 50;
    }

    // Checks if a move is good, makes a move and checks if opponent's move is good.
    // It alternates and weights each move 1/2^depth since guesses could get worse
    double depthrank(Board board, Move move, int depth, int start)
    {
        double rr = 0;

        // Get my color
        bool mycol = board.IsWhiteToMove;

        // Get value of opponent's pieces
        double adv = advantage(board);

        // Find highest value capture
        Piece capturedPiece = board.GetPiece(move.TargetSquare);
        int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

        // Find moving piece value
        Piece movingPiece = board.GetPiece(move.StartSquare);
        int movingPieceValue = pieceValues[(int)movingPiece.PieceType];

        // Find distance from center
        double vdis = Math.Abs(move.TargetSquare.Rank - 7 * Convert.ToInt32(mycol));
        double hdis = Math.Abs(move.TargetSquare.File - 3.5);

        // Find distance from opponent king
        Square oks = board.GetKingSquare(!mycol);
        double cokvdis = Math.Abs(move.StartSquare.Rank - oks.Rank);
        double cokhdis = Math.Abs(move.StartSquare.File - oks.File);

        double okvdis = Math.Abs(move.TargetSquare.Rank - oks.Rank);
        double okhdis = Math.Abs(move.TargetSquare.File - oks.File);

        // ^ rank if valuable piece in danger
        rr -= 2 * movingPieceValue * Convert.ToInt32(board.SquareIsAttackedByOpponent(move.StartSquare));

        // ^ rank based on captured piece
        rr -= 50 * capturedPieceValue;

        // ^ rank if pawn promoted
        rr -= 200 * (move.IsPromotion ? 1 : 0);

        // ^ rank as the pieces move to positions I believe are more adavantagous
        double newdis = (cokvdis - okvdis) * (cokhdis - okhdis) / (adv + 0.1) * (!move.IsCapture ? 1 : 0);
        switch (Convert.ToInt32(move.MovePieceType))
        {
            case 1:
                rr -= 20 * movingPieceValue * vdis / (adv + 1);
                break;
            case 2:
                rr -= movingPieceValue / 100 * ((3.5 - hdis) * adv + newdis);
                break;
            case 3:
                rr -= movingPieceValue / 100 * newdis;
                break;
            case 4:
                rr -= movingPieceValue / 50 * newdis;
                break;
            case 5:
                rr -= movingPieceValue / 50 * newdis;
                break;
            case 6:
                rr -= 10 * newdis;
                break;
        }

        board.MakeMove(move);
        // Returns checkmate
        if (board.IsInCheckmate())
        {
            board.UndoMove(move);
            return -2000000 / Math.Pow(2, start - depth);
        }
        // Checks deeper
        if (depth > 0)
        {
            Move[] tmoves = board.GetLegalMoves();
            double[] trank = new double[tmoves.Length];

            for (int j = 0; j < tmoves.Length; j++)
            {
                trank[j] += depthrank(board, tmoves[j], depth - 1, start);
            }
            System.Array.Sort(trank, tmoves);

            // Changes rank based on best move
            if (tmoves.Length > 0)
            {
                rr -= trank[0];
            }
        }
        board.UndoMove(move);
        return rr / Math.Pow(2, start - depth);
    }
}