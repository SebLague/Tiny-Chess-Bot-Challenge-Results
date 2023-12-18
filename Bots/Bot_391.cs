namespace auto_Bot_391;
using ChessChallenge.API;
using System.Collections.Generic;

public class Bot_391 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    public Move Think(Board board, Timer timer)
    {
        System.Random rng = new();

        //Lists for specal moves
        List<Move> checksNotCaptures = new List<Move>();
        List<Move> allCaptures = new List<Move>();
        List<Move> goodCaptures = new List<Move>();
        List<Move> Promotions = new List<Move>();

        //List for legal moves
        Move[] legalMoves = board.GetLegalMoves();

        //Loop for each legal move
        for (int i = 0; i < legalMoves.Length; i++)
        {
            board.MakeMove(legalMoves[i]);
            if (board.IsInCheck() && (!legalMoves[i].IsCapture))
            {
                checksNotCaptures.Add(legalMoves[i]);
            }
            if (board.IsInCheckmate())
            {
                return legalMoves[i];
            }
            if (legalMoves[i].IsCapture)
            {
                allCaptures.Add(legalMoves[i]);
            }
            if (legalMoves[i].IsPromotion && (legalMoves[i].PromotionPieceType == PieceType.Queen || legalMoves[i].PromotionPieceType == PieceType.Rook))
            {
                Promotions.Add(legalMoves[i]);
            }
            board.UndoMove(legalMoves[i]);
        }


        //checking are captures worht it
        goodCaptures.AddRange(allCaptures);

        foreach (Move move in allCaptures)
        {

            Square TargetSquare = move.TargetSquare;
            List<Move> dangerousMoves = new List<Move>();

            board.MakeMove(move);
            Move[] oponetMoves = board.GetLegalMoves(true);
            foreach (Move responce in oponetMoves)
            {
                if (responce.TargetSquare == TargetSquare)
                {
                    dangerousMoves.Add(responce);
                }
            }
            board.UndoMove(move);

            foreach (Move attack in dangerousMoves)
            {
                if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    if (attack.MovePieceType == 0)
                    {

                    }
                    if (attack.MovePieceType == PieceType.King)
                    {
                        goodCaptures.Remove(move);
                    }
                    else if (board.GetPiece(move.StartSquare).PieceType > attack.MovePieceType)
                    {
                        goodCaptures.Remove(move);
                    }
                }
            }
        }


        //Checking Promotions
        if (Promotions.Count > 0)
        {
            for (int i = 0; i < 2; i++)
            {
                board.MakeMove(Promotions[i]);
                if (board.IsDraw() || board.IsInCheckmate())
                {
                    Promotions.Remove(Promotions[i]);
                }
                board.UndoMove(Promotions[i]);
            }
        }


        List<Move> goodMoves = new List<Move>();
        goodMoves.AddRange(checksNotCaptures);
        //Finding best capture
        if (goodCaptures.Count > 0)
        {
            Move bestCapture = bestCapt(board, goodCaptures);
            goodMoves.Add(bestCapture);
        }
        goodMoves.AddRange(Promotions);

        List<Move> finalGoodMoves = new List<Move>();
        finalGoodMoves.AddRange(goodMoves);
        foreach (Move move in goodMoves)
        {
            board.MakeMove(move);
            if (IsBoardMateInOne(board))
            {
                finalGoodMoves.Remove(move);
            }
            board.UndoMove(move);
        }



        //Returning Moves

        if (goodMoves.Count != 0)
        {
            return goodMoves[rng.Next(goodMoves.Count)];
        }
        else
        {
            //we need to check random moves before making a random move, because it can be very stupit sometimes
            List<Move> nonCheckmateMoves = new List<Move>();
            List<Move> nonDrawMoves = new List<Move>();
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                if (!IsBoardMateInOne(board))
                {
                    nonCheckmateMoves.Add(move);
                }
                if (!board.IsDraw())
                {
                    nonDrawMoves.Add(move);
                }
                board.UndoMove(move);

            }
            if (nonDrawMoves.Count != 0)
            {
                return nonDrawMoves[rng.Next(nonDrawMoves.Count)];
            }
            else if (nonCheckmateMoves.Count != 0)
            {
                return nonCheckmateMoves[rng.Next(nonCheckmateMoves.Count)];
            }
            else
            {
                return legalMoves[rng.Next(legalMoves.Length)];
            }
        }

    }

    public bool IsBoardMateInOne(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        foreach (Move m in moves)
        {
            board.MakeMove(m);
            if (board.IsInCheckmate())
            {
                board.UndoMove(m);
                return true;
            }
            board.UndoMove(m);

        }
        return false;
    }
    public Move bestCapt(Board board, List<Move> goodCaptures)
    {
        int highestValueCapture = 0;
        Move bestM = board.GetLegalMoves()[0];
        foreach (Move move in goodCaptures)
        {
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

            if (capturedPieceValue > highestValueCapture)
            {
                bestM = move;
                highestValueCapture = capturedPieceValue;
            }
        }
        return bestM;
    }

}