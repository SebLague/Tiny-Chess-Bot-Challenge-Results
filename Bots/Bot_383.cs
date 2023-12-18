namespace auto_Bot_383;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_383 : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    readonly int[] pawnSquareTable = {0,  0,  0,  0,  0,  0,  0,  0,
                                        50, 50, 50, 50, 50, 50, 50, 50,
                                        10, 10, 20, 30, 30, 20, 10, 10,
                                        5,  5, 10, 25, 25, 10,  5,  5,
                                        0,  0,  0, 20, 20,  0,  0,  0,
                                        5, -5,-10,  0,  0,-10, -5,  5,
                                        5, 10, 10,-20,-20, 10, 10,  5,
                                        0,  0,  0,  0,  0,  0,  0,  0};

    readonly int[] knightSquareTable = {-50,-40,-30,-30,-30,-30,-40,-50,
                                        -40,-20,  0,  0,  0,  0,-20,-40,
                                        -30,  0, 10, 15, 15, 10,  0,-30,
                                        -30,  5, 15, 20, 20, 15,  5,-30,
                                        -30,  0, 15, 20, 20, 15,  0,-30,
                                        -30,  5, 10, 15, 15, 10,  5,-30,
                                        -40,-20,  0,  5,  5,  0,-20,-40,
                                        -50,-40,-30,-30,-30,-30,-40,-50,};


    readonly int[] kingSquareTable = {-30,-40,-40,-50,-50,-40,-40,-30,
                                    -30,-40,-40,-50,-50,-40,-40,-30,
                                    -30,-40,-40,-50,-50,-40,-40,-30,
                                    -30,-40,-40,-50,-50,-40,-40,-30,
                                    -20,-30,-30,-40,-40,-30,-30,-20,
                                    -10,-20,-20,-20,-20,-20,-20,-10,
                                    20, 20,  0,  0,  0,  0, 20, 20,
                                    20, 30, 10,  0,  0, 10, 30, 20};
    int perspective;
    int searchedMoves = 0;

    public Move Think(Board board, Timer timer)
    {

        searchedMoves = 0;
        // Check if white or black
        bool white = board.IsWhiteToMove;
        perspective = white ? 1 : -1;

        float bestEvaluation = float.NegativeInfinity;
        Move bestMove = new();

        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float evaluation = -Search(board, 4, float.NegativeInfinity, float.PositiveInfinity);
            board.UndoMove(move);

            if (evaluation >= bestEvaluation)
            {
                bestEvaluation = evaluation;
                bestMove = move;
            }
        }
        return bestMove;
    }

    private float Search(Board board, int depth, float alpha, float beta)
    {
        searchedMoves++;

        if (depth == 0)
        {
            //return -SearchCaptureMoves(board, -beta, -alpha);
            return Evaluate(board);
        }

        Move[] moves = board.GetLegalMoves();
        moves = OrderMoves(moves.ToList());
        float bestEvaluation = float.NegativeInfinity;

        foreach (Move move in moves)
        {

            board.MakeMove(move);
            float evaluation = -Search(board, depth - 1, -beta, -alpha); // Negate beta and alpha
            board.UndoMove(move);

            if (evaluation >= beta)
            {
                return evaluation; // Beta cutoff
            }

            if (evaluation > bestEvaluation)
            {
                bestEvaluation = evaluation;
            }

            alpha = Math.Max(alpha, evaluation);

            if (alpha >= beta)
            {
                break; // Alpha cutoff
            }
        }

        return bestEvaluation;
    }

    private float SearchCaptureMoves(Board board, float alpha, float beta)
    {

        float evaluation = Evaluate(board);
        Move[] moves = board.GetLegalMoves(true);
        if (moves.Length < 1)
        {
            return evaluation;
        }
        moves = OrderMoves(moves.ToList());
        float bestEvaluation = float.NegativeInfinity;

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            evaluation = -SearchCaptureMoves(board, -beta, -alpha); // Negate beta and alpha
            board.UndoMove(move);

            if (evaluation >= beta)
            {
                return evaluation; // Beta cutoff
            }

            if (evaluation > bestEvaluation)
            {
                bestEvaluation = evaluation;
            }

            alpha = Math.Max(alpha, evaluation);

            if (alpha >= beta)
            {
                break; // Alpha cutoff
            }
        }

        return alpha;
    }


    private Move[] OrderMoves(List<Move> moves)
    {
        List<Move> sortedList = new List<Move>();

        List<Move> captureMoves = new List<Move>();
        List<Move> castles = new List<Move>();
        List<Move> otherMoves = new List<Move>();

        foreach (Move move in moves)
        {
            if (move.IsCapture)
            {
                if (pieceValues[(int)move.MovePieceType] <= pieceValues[(int)move.CapturePieceType])
                {
                    captureMoves.Add(move);
                }
                continue;
            }
            if (move.IsCastles)
            {
                castles.Add(move);
                continue;
            }
            else
            {
                otherMoves.Add(move);
            }
        }
        sortedList.AddRange(captureMoves);
        sortedList.AddRange(castles);
        sortedList.AddRange(otherMoves);

        return sortedList.ToArray<Move>();
    }

    private float Evaluate(Board board)
    {
        if (board.IsInCheckmate())
        {
            return float.PositiveInfinity * perspective;
        }
        //Piece Value
        int blackPieceValue = 0;
        int whitePieceValue = 0;
        //Piece Square Value
        float whiteSquareValue = 0;
        float blackSquareValue = 0;

        PieceList[] pieceLists = board.GetAllPieceLists();


        foreach (PieceList pieceList in pieceLists)
        {
            foreach (Piece piece in pieceList)
            {
                if (piece.IsWhite)
                {
                    whitePieceValue += pieceValues[(int)piece.PieceType];
                }
                else
                {
                    blackPieceValue += pieceValues[(int)piece.PieceType];
                }
                switch (piece.PieceType)
                {
                    case PieceType.Pawn:
                        int pawnIndex = piece.Square.Index;

                        // If the piece is black, adjust the index for the non-symmetrical table
                        if (piece.IsWhite)
                        {
                            pawnIndex = 63 - pawnIndex; // Invert the index for the non-symmetrical part
                            whiteSquareValue += pawnSquareTable[pawnIndex];
                            //DivertedConsole.Write("Found piece: " + piece.Square + "with value of " + pawnSquareTable[index]);
                        }
                        else
                        {
                            blackSquareValue += pawnSquareTable[pawnIndex];
                            //DivertedConsole.Write("Found piece: " + piece.Square + "with value of " + pawnSquareTable[index]);
                        }

                        break;
                    // Handle other piece types similarly...
                    case PieceType.Knight:
                        int knightIndex = piece.Square.Index;
                        if (piece.IsWhite)
                        {
                            knightIndex = 63 - knightIndex;
                            whiteSquareValue += knightSquareTable[knightIndex];
                        }
                        else
                        {
                            blackSquareValue += knightSquareTable[knightIndex];
                        }
                        break;


                    case PieceType.King:
                        int kingIndex = piece.Square.Index;
                        if (piece.IsWhite)
                        {
                            knightIndex = 63 - kingIndex;
                            whiteSquareValue += kingSquareTable[kingIndex];
                        }
                        else
                        {
                            blackSquareValue += kingSquareTable[kingIndex];
                        }
                        break;
                }
            }
        }

        //DivertedConsole.Write("White piece value: " + whitePieceValue);
        //DivertedConsole.Write("Black piece value: " + blackPieceValue);
        //DivertedConsole.Write("White square value: " + whiteSquareValue);
        //DivertedConsole.Write("Black square value: " + blackSquareValue);
        float squareValues = whiteSquareValue - blackSquareValue;
        return (whitePieceValue - blackPieceValue + squareValues) * perspective;
    }
}