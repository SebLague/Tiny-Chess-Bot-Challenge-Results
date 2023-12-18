namespace auto_Bot_154;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Bot_154 : IChessBot
{


    private int searchDepth = 5;
    private TimeSpan maxSearchTime = TimeSpan.FromSeconds(8.0);


    public Move Think(Board board, Timer timer)
    {

        DateTime startTime = DateTime.Now;
        TimeSpan elapsedSearchTime = TimeSpan.Zero;

        Move[] legalMoves = board.GetLegalMoves();
        Dictionary<Move, double> moveScores = new Dictionary<Move, double>();
        OrderMoves(legalMoves, moveScores, board);

        legalMoves = legalMoves.OrderByDescending(move => moveScores[move]).ToArray();

        double bestScore = double.MinValue;
        Move bestMove = legalMoves[0];

        foreach (Move move in legalMoves)
        {
            board.MakeMove(move);
            double score = -NegaMax(searchDepth - 1, board, int.MinValue, int.MaxValue, false);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            DateTime currentTime = DateTime.Now;
            elapsedSearchTime = currentTime - startTime;

            // Check if time is running out
            if (elapsedSearchTime >= maxSearchTime)
            {
                break; // Stop searching and use the best move found so far
            }
        }

        return bestMove;
    }

    //https://www.chessprogramming.org/Negamax
    // This is a shorter, more comman way for mini-max.
    private int NegaMax(int depth, Board board, int alpha, int beta, bool maximizingPlayer)
    {


        Dictionary<Move, double> captureScores = new Dictionary<Move, double>();
        Dictionary<Move, double> moveScores = new Dictionary<Move, double>();

        Move[] legalMoves = board.GetLegalMoves();

        OrderMoves(legalMoves, moveScores, board);

        legalMoves = legalMoves.OrderByDescending(move => moveScores[move]).ToArray();



        int SearchAllCaptures(int alpha, int beta)
        {
            int evaluation = Evaluate(board);
            if (evaluation >= beta)
            {
                return beta;
            }
            alpha = Math.Max(alpha, evaluation);

            Move[] captureMoves = board.GetLegalMoves(true);
            OrderMoves(captureMoves, captureScores, board);
            captureMoves = captureMoves.OrderByDescending(move => captureScores[move]).ToArray();

            foreach (Move captureMove in captureMoves)
            {
                board.MakeMove(captureMove);
                evaluation = -SearchAllCaptures(-beta, -alpha);
                board.UndoMove(captureMove);

                if (evaluation >= beta)
                {
                    return beta;
                }
                alpha = Math.Max(alpha, evaluation);
            }
            return alpha;

        }

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
        {
            // Base Case or Terminal Node.
            return SearchAllCaptures(alpha, beta);
        }


        foreach (Move move in legalMoves)
        {



            board.MakeMove(move);

            int score = -NegaMax(depth - 1, board, -beta, -alpha, !maximizingPlayer);

            board.UndoMove(move);

            if (score > alpha)
            {
                alpha = score;
            }

            if (alpha >= beta)
            {
                // Alpha-beta pruning.
                break;
            }
        }

        return alpha;
    }
    public void OrderMoves(Move[] moves, Dictionary<Move, double> moveScores, Board board)
    {


        foreach (Move move in moves)
        {
            int moveScore = 0;
            PieceType movePiece = move.MovePieceType;
            PieceType capturePiece = move.CapturePieceType;
            Square targetSquare = move.TargetSquare;


            int movePieceVal = (int)movePiece;
            int capturePieceVal = (int)capturePiece;




            // Rule 1.
            // If opponents piece is more valuable then ours, capture.
            if (capturePiece != PieceType.None)
            {
                moveScore += 20 * (capturePieceVal - movePieceVal);
            }


            // Rule 2.
            // Promoting is generally a good idea.
            // Capacity was too much, got rid of this.

            // Rule 3.
            // If our move's target is attacked by the opponent, then decrease the moveScore.
            if (board.SquareIsAttackedByOpponent(targetSquare))
            {


                moveScore -= (5 * movePieceVal);

            }

            // Rule 4: Encourage controlling the center by rewarding moves to center squares.
            const int centerValue = 20;
            if (IsInCenter(targetSquare))
            {
                moveScore += centerValue;
            }

            // Rule 5.
            // If it is a capture move and the moveScore is greater than 0, capture material.
            if (capturePiece != PieceType.None && capturePieceVal <= movePieceVal)
            {
                moveScore += 50; //Bonus to encourage capturing
            }

            // Rule 6
            // Encourage to get more mobility score for the bot.
            int mobilityScore = CalculatePieceMobility(movePiece, targetSquare, board);
            moveScore += mobilityScore;

            // Rule 7
            // Encourage castling
            if (move.IsCastles)
            {
                moveScore += 30;
            }


            moveScores[move] = moveScore;

        }



    }

    // Helper method to check if a square is in the center of the board.
    private bool IsInCenter(Square square)
    {
        int file = square.File;
        int rank = square.Rank;
        return (file >= 2 && file <= 5) && (rank >= 2 && rank <= 5);
    }





    public static int Evaluate(Board board)
    {
        int evaluation = 0;

        //Material evaluation
        evaluation += board.GetPieceList(PieceType.Pawn, true).Count * 100;
        evaluation -= board.GetPieceList(PieceType.Pawn, false).Count * 100;
        evaluation += board.GetPieceList(PieceType.Knight, true).Count * 300;
        evaluation -= board.GetPieceList(PieceType.Knight, false).Count * 300;
        evaluation += board.GetPieceList(PieceType.Bishop, true).Count * 320;
        evaluation -= board.GetPieceList(PieceType.Bishop, false).Count * 320;
        evaluation += board.GetPieceList(PieceType.Rook, true).Count * 500;
        evaluation -= board.GetPieceList(PieceType.Rook, false).Count * 500;
        evaluation += board.GetPieceList(PieceType.Queen, true).Count * 900;
        evaluation -= board.GetPieceList(PieceType.Queen, false).Count * 900;


        //threat score code
        int threatScore = 0;
        PieceList[] allPieceLists = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieceLists)
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite == board.IsWhiteToMove)
                {
                    Square pieceSquare = piece.Square;

                    // Check if the current piece is under threat by the opponent
                    bool isUnderThreat = board.SquareIsAttackedByOpponent(pieceSquare);

                    // If the piece is under threat, apply a penalty based on its value
                    if (isUnderThreat)
                    {
                        int pieceValue = (int)piece.PieceType;
                        threatScore -= 10 * pieceValue;
                    }
                }
            }
        }

        evaluation += threatScore;
        //Encouragein controlling the center
        evaluation += ControlCenter(board);

        //Perspective to which side is going to play
        int perspective = (board.IsWhiteToMove) ? 1 : -1;

        if (board.IsInCheck())
        {
            // If the bot is in check, encourage it to find a way out.
            evaluation += 150;
        }

        if (board.IsInCheckmate())
        {
            // If the bot is in checkmate, encourage it to avoid such positions in the future.
            evaluation += 10000;
        }

        return evaluation * perspective;
    }

    private static int ControlCenter(Board board)
    {
        int centerValue = 20;

        // Encourage controlling center squares for the pieces
        int centerControlWhite = 0;
        int centerControlBlack = 0;

        // Squares in the center
        Square[] centerSquares = { new Square("d4"), new Square("e4"), new Square("d5"), new Square("e5") };

        // Evaluate control of center squares for white pieces
        foreach (Square square in centerSquares)
        {
            if (board.SquareIsAttackedByOpponent(square))
            {
                centerControlBlack++;
            }
            else if (board.GetPiece(square).IsWhite)
            {
                centerControlWhite++;
            }
        }

        // Evaluate control of center squares for black pieces
        foreach (Square square in centerSquares)
        {
            if (board.SquareIsAttackedByOpponent(square))
            {
                centerControlWhite++;
            }
            else if (!board.GetPiece(square).IsWhite)
            {
                centerControlBlack++;
            }
        }

        // Add the center control evaluation to the overall evaluation
        int evaluation = centerValue * (centerControlWhite - centerControlBlack);

        return evaluation;
    }











    private int CalculatePieceMobility(PieceType pieceType, Square square, Board board)
    {
        int mobilityScore = 0;

        // Calculate the potential attacks of the piece from its current square
        ulong pieceAttacks = BitboardHelper.GetPieceAttacks(pieceType, square, board, board.IsWhiteToMove);

        // Count the number of squares in the attack bitboard to get the mobility score
        mobilityScore = BitboardHelper.GetNumberOfSetBits(pieceAttacks);

        return mobilityScore;
    }




}