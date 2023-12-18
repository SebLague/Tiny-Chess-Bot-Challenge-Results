namespace auto_Bot_512;
using ChessChallenge.API;
using System;

public class Bot_512 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 9999 };
    bool white = false;
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        int[] values = new int[moves.Length];
        Random rand = new Random();
        white = board.IsWhiteToMove;

        for (int i = 0; i < moves.GetLength(0); i++)
        {
            Move m = moves[i];
            //int value = rand.Next()%3 - 1;
            int value = 0;
            //Don't move king
            if ((int)m.MovePieceType == 6)
            {
                value -= 4;
                if ((board.HasKingsideCastleRight(white) || board.HasQueensideCastleRight(white)) && !m.IsCastles) value -= 7;
            }
            board.MakeMove(m);//                         THIS IS WHERE THE MOVE HAPPENS!!!!!!!!!!!!!!!!!!!!!!!
            //Move forward
            value += (white ? 1 : -1) * (m.TargetSquare.Rank - m.StartSquare.Rank);
            //move back row pawns
            if ((int)m.MovePieceType == 1 && (m.StartSquare.Rank == 2 || m.StartSquare.Rank == 7)) value += 2;
            //Take checkmates
            if (board.IsInCheckmate()) return m;
            //Take checks
            if (board.IsInCheck()) value += 75;
            //Promote pawns
            if (m.IsPromotion) value += pieceValues[(int)m.PromotionPieceType] - 100;
            //Castle
            if (m.IsCastles) value += 6;
            //Avoid Draw when winning
            if (board.IsDraw()) value -= (white ? 1 : -1) * evalBoard(board) / 3;
            //protect pieces and endanger enemy's
            value += checkProtection(board, white);
            //minimize reactions
            Move[] movesBack = board.GetLegalMoves(false);
            value -= movesBack.Length;
            if (movesBack.Length > 0)
            {
                //Don't trade unfavorably
                int highestCaptured = 0;
                int movesBackBack = 0;
                for (int j = 0; j < movesBack.Length; j++)
                {
                    Move b = movesBack[j];
                    board.MakeMove(b);
                    movesBackBack += board.GetLegalMoves().Length;
                    //Avoid Draw when winning
                    if (board.IsDraw()) value -= (white ? 1 : -1) * evalBoard(board) / 4;
                    //don't lose
                    if (board.IsInCheckmate()) value -= 10000;
                    bool check = false;
                    if (board.IsInCheck())
                    {
                        check = true;
                        value -= 200;
                    }
                    board.UndoMove(b);
                    if (board.SquareIsAttackedByOpponent(b.TargetSquare) && check)
                    {
                        value += 180;
                    }
                    highestCaptured = (int)Math.Max(highestCaptured, pieceValues[(int)b.CapturePieceType] * (board.SquareIsAttackedByOpponent(b.TargetSquare) ? 0.5 : 1));
                }
                //maximise options
                movesBackBack /= movesBack.Length;
                value += (int)(Math.Pow(movesBackBack, 2) / 50);
                value += pieceValues[(int)m.CapturePieceType] - highestCaptured;
            }

            board.UndoMove(m);
            values[i] = value;
        }

        Move best = moves[0];
        int bestvalue = -999999;
        for (int i = 0; i < moves.Length; i++) if (values[i] > bestvalue)
            {
                bestvalue = values[i];
                best = moves[i];
            }
        if (false)
        {
            board.TrySkipTurn();
            DivertedConsole.Write(checkProtection(board, white) + "\t");
            DivertedConsole.Write();
        }
        board.MakeMove(best);
        return best;
    }

    int checkProtection(Board board, bool white)
    {
        const double friendlyMult = 0.01;
        const double enemyMult = 0.01;
        int score = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList pl in allPieces) foreach (Piece p in pl) if ((int)p.PieceType != 6) score += (int)(((white ^ p.IsWhite) ? enemyMult : friendlyMult) * pieceValues[(int)p.PieceType]) * (board.SquareIsAttackedByOpponent(p.Square) ? 1 : 0);
        return score;
    }

    int evalBoard(Board board)
    {
        int score = 0;
        PieceList[] allPieces = board.GetAllPieceLists();
        foreach (PieceList pl in allPieces)
        {
            foreach (Piece p in pl) score += (p.IsWhite ? 1 : -1) * pieceValues[(int)p.PieceType];
        }
        return score;
    }

}