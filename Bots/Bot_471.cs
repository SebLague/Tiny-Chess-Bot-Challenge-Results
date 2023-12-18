namespace auto_Bot_471;
using ChessChallenge.API;
using System;
// using System.Numerics
using System.Collections.Generic;
using System.Linq;


// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.priorityqueue-2?view=net-7.0
// https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.sortedlist-2?view=net-7.0
// https://github.com/SebLague/Chess-Challenge
// https://seblague.github.io/chess-coding-challenge/documentation/

// IDEAS:
// Improve position


public class Bot_471 : IChessBot
{

    bool myColor;

    // Piece values:    null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 10, 32, 33, 50, 90, 2000 };

    SortedList<ulong, int> savedScores = new SortedList<ulong, int>();
    int alpha = Int32.MinValue; int beta = Int32.MaxValue;


    public Move Think(Board board, Timer timer)
    {

        DivertedConsole.Write("\n\n\nMY TURN: " + board.PlyCount); // #DEBUG

        savedScores = new SortedList<ulong, int>();
        alpha = Int32.MinValue; beta = Int32.MaxValue;

        Move[] allMoves = board.GetLegalMoves();
        myColor = board.IsWhiteToMove;
        Move movetoplay;
        int currentDepth = 1;

        while (true)
        {
            int start_time = timer.MillisecondsElapsedThisTurn; // #DEBUG

            int score = MoveSearch(board, currentDepth, alpha, beta);

            movetoplay = allMoves.MaxBy(kvp => getMoveScore(kvp, board));
            DivertedConsole.Write("movetoplay " + movetoplay + " current depth " + currentDepth + // #DEBUG
                " score " + score + " time " + (timer.MillisecondsElapsedThisTurn - start_time)); // #DEBUG

            int[] timeArray = {
                timer.GameStartTimeMilliseconds / BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard),
                timer.MillisecondsRemaining / 5
            };

            if (!(timer.MillisecondsElapsedThisTurn + 20 * (timer.MillisecondsElapsedThisTurn - start_time) < timeArray.Min()))
            {
                return movetoplay;
            }
            currentDepth += 1;
        }
    }


    int BoardScore(Board board)
    {
        if (board.IsInCheckmate())
        {
            return ((myColor == board.IsWhiteToMove) ? Int32.MinValue : Int32.MaxValue);
        }

        PieceList[] allPieceLists = board.GetAllPieceLists();

        decimal myScore = allPieceLists.Where(
            pl => pl.IsWhitePieceList == myColor
        ).Aggregate(
            0,
            (s, a) => s + a.Count * pieceValues[(int)a.TypeOfPieceInList]);
        decimal oppScore = allPieceLists.Where(
            pl => pl.IsWhitePieceList != myColor
        ).Aggregate(
            0,
            (s, a) => s + a.Count * pieceValues[(int)a.TypeOfPieceInList]);

        int materialScore = (int)(1000 * (myScore / oppScore - 1));

        //if (board.IsDraw() & materialScore >= 0) { return Int32.MinValue; }
        //else if (board.IsDraw() & materialScore < 0) { return Int32.MaxValue; }

        decimal positionScore = 0;
        int kingScore = 0;
        int pawnScore = 0;
        bool endGame = (myScore + oppScore < 2 * (
            8 * pieceValues[(int)PieceType.Pawn] +
            2 * pieceValues[(int)PieceType.Knight] +
            2 * pieceValues[(int)PieceType.Bishop] +
            0 * pieceValues[(int)PieceType.Rook] +
            0 * pieceValues[(int)PieceType.Queen] +
             1 * pieceValues[(int)PieceType.King]
            ));

        foreach (PieceList pl in allPieceLists)
        {
            decimal factor = 10 / pieceValues[(int)pl.TypeOfPieceInList] * (myColor == pl.IsWhitePieceList ? 1 : -1);
            if (pl.TypeOfPieceInList == PieceType.King & !endGame)
            {
                //Protect the king
                ulong piece_attacks = BitboardHelper.GetPieceAttacks(
                    PieceType.Queen,
                    board.GetKingSquare(pl.IsWhitePieceList),
                    board,
                    pl.IsWhitePieceList
                );
                kingScore -= BitboardHelper.GetNumberOfSetBits(piece_attacks) * (myColor == pl.IsWhitePieceList ? 1 : -1);

                continue;
            }


            foreach (Piece p in pl)
            {
                if (pl.TypeOfPieceInList == PieceType.Pawn)
                {
                    //Advance Pawns Centrally
                    pawnScore += (int)(factor * (decimal)(Math.Abs((p.IsWhite ? 7 : 0) - p.Square.Rank) + Math.Abs(p.Square.File - 3.5)));
                    continue;
                }
                ulong piece_attacks = BitboardHelper.GetPieceAttacks(pl.TypeOfPieceInList, p.Square, board, p.IsWhite);
                // Mobility
                positionScore += BitboardHelper.GetNumberOfSetBits(piece_attacks) * factor;
            }
        }


        return 1000 * materialScore + (int)positionScore + kingScore + pawnScore;
    }

    int MoveSearch(Board board, int searchdepth, int alpha, int beta)
    {

        bool myturn = myColor == board.IsWhiteToMove;

        if (searchdepth == 0 | board.IsDraw() | board.IsInCheckmate())
        {
            int outscore = BoardScore(board);
            savedScores[board.ZobristKey] = outscore;
            return outscore;
        }
        Move[] allMoves = board.GetLegalMoves();
        int score = myturn ? Int32.MinValue : Int32.MaxValue;

        allMoves = (from entry in allMoves orderby getMoveScore(entry, board) descending select entry).ToArray();
        foreach (Move move in allMoves)
        {
            board.MakeMove(move);

            // DivertedConsole.Write("depth "+ searchdepth + " score " + score + " alpha " + alpha+" beta " + beta);

            if (myturn)
            {
                score = Math.Max(score, MoveSearch(board, searchdepth - 1, alpha, beta));
                if (score > beta) { board.UndoMove(move); break; }
                alpha = Math.Max(alpha, score);
            }
            else
            {
                score = Math.Min(score, MoveSearch(board, searchdepth - 1, alpha, beta));
                if (score < alpha) { board.UndoMove(move); break; }
                beta = Math.Min(beta, score);
            }

            board.UndoMove(move);
        }
        //DivertedConsole.Write(score);
        savedScores[board.ZobristKey] = score;
        return score;
    }

    int getMoveScore(Move move, Board board)
    {
        int score;
        board.MakeMove(move);
        if (savedScores.ContainsKey(board.ZobristKey))
        { score = savedScores[board.ZobristKey]; }
        else
        {
            score = BoardScore(board);
            savedScores[board.ZobristKey] = score;
        }
        board.UndoMove(move);
        return score;
    }
}