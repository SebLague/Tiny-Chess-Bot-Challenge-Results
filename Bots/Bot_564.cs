namespace auto_Bot_564;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
public class Bot_564 : IChessBot
{

    /*
        Hello! Welcome to my chess bot!
        This was made by a 14 year old who loves programming and chess, and It would be a miracle
        If I got in the video considering this bot is nothing special (he hardest bot it's beat
        on chess.com was a 1200). I'm sure people have made ones that reach 1800 or higher. This was an
        amazing challenge nonetheless. You can find my website over at https://captainexpo-1.github.io.
        Anyways, I hope I do decent in the tournament, and good luck to everyone else.
    */

    int piecesleft = int.MaxValue;
    int color = 0;
    Dictionary<ulong, (float, Move)> transpositionTable = new();
    int mv = 0;

    public Move Think(Board board, Timer timer)
    {
        //set color to play
        color = board.IsWhiteToMove ? 1 : -1;

        // Define a simple opening repertoire with a limited number of moves.
        string[] whiteOpeningMoves = { "e2e4", "d2d4", "g1f3" };
        string[] blackOpeningMoves = { "e7e5", "d7d5", "b8c6" };

        //increment move counter
        mv++;

        // Check if the current move count is within your defined opening repertoire.
        Move[] mvs = board.GetLegalMoves();
        if (mv == 1)
        {
            Random rnd = new();
            int r = rnd.Next(whiteOpeningMoves.Length);
            Move mov = new Move(color == 1 ? whiteOpeningMoves[r] : blackOpeningMoves[r], board);
            if (mvs.Contains(mov)) return mov;
            //doOpeningDepth = true;
        }

        return SearchBestMove(board, timer.MillisecondsRemaining / 1000 > 15f ? piecesleft < 6 ? 12 : 3 : 2);

    }
    //move search function
    public Move SearchBestMove(Board board, int depth)
    {

        Move[] moves = board.GetLegalMoves();

        // Order moves with move ordering (capturing moves first)
        Array.Sort(moves, CompareMoves);

        float bestEval = float.MinValue;
        Move bestMove = moves[0];

        //evaluate every move in current position
        foreach (Move move in moves)
        {
            //always find M1
            board.MakeMove(move);
            if (board.IsInCheckmate())
                return move;

            //call the negamax function to get eval for the current move
            float eval = -NegaMax(board, depth - 1, float.MinValue + 1, float.MaxValue - 1, -color, 0).Item1;
            board.UndoMove(move);

            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }
        }

        //return best move
        return bestMove;
    }

    // prioritize capturing moves first
    int CompareMoves(Move move1, Move move2)
    {
        if (move1.IsCapture && !move2.IsCapture)
            return -1;
        if (!move1.IsCapture && move2.IsCapture)
            return 1;
        return 0;
    }
    //search function
    (float, Move) NegaMax(Board board, int depth, float alpha, float beta, int color, int numExtentions)
    {
        ulong zobristKey = board.ZobristKey;

        if (transpositionTable.TryGetValue(zobristKey, out var entry) && depth > 0)
        {
            // Return stored evaluation if available and depth is not zero
            if (entry.Item1 >= beta)
                return (entry.Item1, entry.Item2);

            if (entry.Item1 <= alpha)
                return (entry.Item1, entry.Item2);
        }

        if (depth == 0)
        {
            // Evaluate the position and return the score
            float eval = EvaluateBoard(board, color);

            // Store the evaluation and best move in the transposition table
            transpositionTable[zobristKey] = (eval, new Move());

            //return the evaluation
            return (eval, new());
        }
        Move[] moves = board.GetLegalMoves();
        Move bestMove = Move.NullMove;
        float bestEval = float.MinValue;

        if (moves.Length == 0)
            return (float.MinValue, Move.NullMove);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            //depth extentions if move is capture, check, or promotion
            int extention = numExtentions < 2 ? ((board.IsInCheck() ? 1 : 0) + (move.IsPromotion ? 1 : 0) + (move.IsCapture ? 1 : 0)) : 0;
            float eval = -NegaMax(board, depth + extention - 1, -beta, -alpha, -color, numExtentions + extention).Item1;
            board.UndoMove(move);

            //set best move if eval > best eval
            if (eval > bestEval)
            {
                bestEval = eval;
                bestMove = move;
            }

            //alpha beta pruning
            alpha = Math.Max(alpha, eval);
            if (alpha >= beta)
                break;
        }

        // Store the evaluation and best move in the transposition table
        transpositionTable[zobristKey] = (bestEval, bestMove);

        return (bestEval, bestMove);
    }
    //evaluation function
    private float EvaluateBoard(Board board, int color)
    {
        //initialize and set variables
        piecesleft = 0;
        var pieces = board.GetAllPieceLists();
        float materialScore = 0;
        float positionScore = 0;
        float mobilityScore = 0;
        float kingBonus = 0;
        float pawnScore = 0;
        int[] pieceValues = { 10, 30, 30, 50, 90, 0 };

        float whitePieceDevBonus = 0;
        float blackPieceDevBonus = 0;

        float cleanUpScore = 0;
        bool wtm = board.IsWhiteToMove;

        for (int p = 0; p < pieces.Length; p++)
        {
            //count pieces
            if (pieces[p].TypeOfPieceInList != PieceType.Pawn)
                piecesleft += pieces[p].Count;

            //evaluate material
            if (pieces[p].IsWhitePieceList)
                materialScore += pieceValues[p] * pieces[p].Count;
            else
                materialScore -= pieceValues[p - 6] * pieces[p].Count;

            //evaluate mobility of pieces
            foreach (Piece piece in pieces[p])
            {
                int pieceColor = piece.IsWhite ? 1 : -1;
                int mult = (16 - piecesleft / 12) * pieceColor;

                if (piece.PieceType == PieceType.Knight)
                    mobilityScore += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKnightAttacks(piece.Square)) * mult;
                else if (!piece.IsKing && !piece.IsPawn)
                    mobilityScore += BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetSliderAttacks(piece.PieceType, piece.Square, board)) * mult;
                if (piecesleft < 6)
                {
                    int br = 7 - piece.Square.Rank;
                    int wr = piece.Square.Rank;
                    pawnScore += (pieceColor == 1 ? MathF.Pow(wr, 1.3f) : MathF.Pow(br, 1.3f)) * pieceColor * color;
                    //pawnScore += (piecesleft < 4 ? 25 / (piecesleft+1) : 0) * pieceColor;
                    //DivertedConsole.Write(pawnScore);
                }
            }
        }
        //eval if check (I still don't really understand this, like is this function doing the opposite)
        if (board.IsInCheck())
            if (wtm)
                positionScore -= 20f * color;
            else
                positionScore += 20f * color;

        //promote moving king closer to opponent's
        if (piecesleft < 4)
        {
            cleanUpScore += (1 - distBetweenSquares(board.GetKingSquare(true), board.GetKingSquare(false))) * 3;
        }

        //sum up evaluation parts
        float eval =
            materialScore * 3
            + mobilityScore
            + kingBonus
            + positionScore
            + (whitePieceDevBonus - blackPieceDevBonus) * color
            + cleanUpScore
            + pawnScore;

        //set eval if is draw P.S. I don't know if this actually works. I might need to keep a repetition table
        if (board.IsDraw())
            eval = 0; //Eval <= -1 ? -Eval * 2 : -10;

        //set eval if is checkmate
        if (board.IsInCheckmate())
            if (wtm)
                eval = float.NegativeInfinity * color;
            else
                eval = float.PositiveInfinity * color;

        return eval * color;
    }
    float distBetweenSquares(Square s1, Square s2)
    {
        //get distance between different squares
        return MathF.Sqrt(MathF.Pow(s2.File - s1.File, 2) + MathF.Pow(s2.Rank - s1.Rank, 2));
    }
}
