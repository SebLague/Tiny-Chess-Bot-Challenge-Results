namespace auto_Bot_570;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_570 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 310, 500, 900, 10000 };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        double endgametracker = EndgameTracker(board);

        int[] OpeningSquareImportance = { 1, 2, 3, 4, 4, 3, 2, 1, 2, 3, 4, 5, 5, 4, 3, 2, 3, 4, 5, 6, 6, 5, 4, 3, 4, 5, 6, 7, 7, 6, 5, 4, 4, 5, 6, 7, 7, 6, 5, 4, 3, 4, 5, 6, 6, 5, 4, 3, 2, 3, 4, 5, 5, 4, 3, 2, 1, 2, 3, 4, 4, 3, 2, 1 };

        DivertedConsole.Write("EndgameTracker:");
        DivertedConsole.Write(endgametracker);

        int[] movescore = new int[allMoves.Length];

        int i = -1;
        foreach (Move move in allMoves)
        {
            i++;
            movescore[i] = 0;
            if (move.StartSquare.Rank == 7 || move.StartSquare.Rank == 0)
            {
                if (board.GetPiece(move.StartSquare).IsQueen || board.GetPiece(move.StartSquare).IsKing)
                {
                    movescore[i] -= Convert.ToInt32(25 * (1 - endgametracker));
                    if (move.IsCastles)
                    {
                        movescore[i] += Convert.ToInt32(75 * (1 - endgametracker)); ;
                    }
                }
                if (board.GetPiece(move.StartSquare).IsRook)
                {
                    movescore[i] -= Convert.ToInt32(10 * (1 - endgametracker)); ;
                }
                if (board.GetPiece(move.StartSquare).IsKnight || board.GetPiece(move.StartSquare).IsBishop)
                {
                    movescore[i] += Convert.ToInt32(10 * (1 - endgametracker)); ;
                }
                //DivertedConsole.Write(move);
                // Always play checkmate in one
                if (MoveIsCheckmate(board, move))
                {
                    return move;
                }
            }
            if (board.GetPiece(move.StartSquare).IsPawn)
            {
                movescore[i] += Convert.ToInt32(20 * endgametracker);
            }
            // Find highest value capture
            Piece capturedPiece = board.GetPiece(move.TargetSquare);
            Piece capturingPiece = board.GetPiece(move.StartSquare);


            movescore[i] += pieceValues[(int)capturedPiece.PieceType]; ;
            board.MakeMove(move);

            if (board.IsInCheck())
            {
                movescore[i] += 15;
            }
            Move[] allOponentsMoves = board.GetLegalMoves();
            int Maxscoreloss = 0;
            foreach (Move Oppmove in allOponentsMoves)
            {
                Piece recapturedPiece = board.GetPiece(Oppmove.TargetSquare);
                if (pieceValues[(int)recapturedPiece.PieceType] > 0)
                {
                    int movescoreloss = CaptureChain(board, Oppmove.TargetSquare);
                    if (movescoreloss > Maxscoreloss)
                    {
                        Maxscoreloss = movescoreloss;
                    }
                }
            }
            movescore[i] -= Maxscoreloss;


            for (int j = 0; j < 64; j++)
            {
                Square squareopeningphase = new Square(j);
                bool opponentattack = board.SquareIsAttackedByOpponent(squareopeningphase);
                board.ForceSkipTurn();
                bool playerattack = board.SquareIsAttackedByOpponent(squareopeningphase);
                board.UndoSkipTurn();
                if (playerattack & !opponentattack)
                {
                    movescore[i] -= Convert.ToInt32(Convert.ToDouble(OpeningSquareImportance[j]) * (1 - endgametracker));
                }
                if (!playerattack & opponentattack)
                {
                    movescore[i] += Convert.ToInt32(Convert.ToDouble(OpeningSquareImportance[j]) * (1 - endgametracker));
                }
            }


            board.UndoMove(move);


        }
        int maxValue = movescore.Max();
        int maxIndex = movescore.ToList().IndexOf(maxValue);
        i = 0;



        foreach (Move move in allMoves)
        {
            DivertedConsole.Write(move);
            DivertedConsole.Write(movescore[i] + "\n");
            i++;
        }
        return allMoves[maxIndex];
    }

    // Test if this move gives checkmate
    bool MoveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    int CaptureChain(Board board, Square square)
    {
        int Value = 0;
        int MaxCapture = 0;
        bool captured = false;
        Move[] AllMoves = board.GetLegalMoves(true);
        foreach (Move move in AllMoves)
        {
            if (MoveIsCheckmate(board, move))
            {
                MaxCapture = -100000;
            }
            if (move.TargetSquare == square)
            {
                Piece capturedPiece = board.GetPiece(move.TargetSquare);
                if (captured == false)
                {
                    Value += pieceValues[(int)capturedPiece.PieceType];
                    captured = true;
                }
                board.MakeMove(move);
                int ThisCapture = CaptureChain(board, square);
                if (ThisCapture > MaxCapture)
                {
                    MaxCapture = ThisCapture;
                }
                board.UndoMove(move);
            }
        }
        Value -= MaxCapture;
        return Math.Max(Value, 0);
    }

    double EndgameTracker(Board board)
    {
        int piecessum = 0;
        PieceList[] pieces = board.GetAllPieceLists();
        foreach (PieceList piecelist in pieces)
        {
            piecessum += piecelist.Count() * pieceValues[(int)piecelist.TypeOfPieceInList];
        }

        double endgamedouble = 1 - (Convert.ToDouble(piecessum) - 20000) / 7840;
        return endgamedouble;
    }
}