namespace auto_Bot_59;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_59 : IChessBot
{
    int[] pieceValues = { 0, 100, 350, 350, 525, 1000, 10000 };
    int depths = 5;
    bool BotIsWhite;
    public int depth4 = 40000;
    public int depth3 = 7750;

    //Half Piece-Square Tables to save tokens
    int[] PawnTable =
        {
            0,  0,  0,  0,
            50, 50, 50, 50,
            10, 10, 20, 30,
            5,  5, 10, 25,
            0,  0,  0, 20,
            5, -5,-10,  0,
            5, 10, 10,-20,
            0,  0,  0,  0,
        };

    int[] KnightTable =
    {
            -50,-40,-30,-30,
            -40,-20,  0,  0,
            -30,  0, 10, 15,
            -30,  5, 15, 20,
            -30,  0, 15, 20,
            -30,  5, 10, 15,
            -40,-20,  0,  5,
            -50,-40,-30,-30,
        };

    int[] BishopTable =
    {
            -20,-10,-10,-10,
            -10,  0,  0,  0,
            -10,  0,  5, 10,
            -10,  5,  5, 10,
            -10,  0, 10, 10,
            -10, 10, 10, 10,
            -10,  5,  0,  0,
            -20,-10,-10,-10,
        };

    int[] RookTable =
    {
              0,  0,  0,  0,
              5, 10, 10, 10,
             -5,  0,  0,  0,
             -5,  0,  0,  0,
             -5,  0,  0,  0,
             -5,  0,  0,  0,
             -5,  0,  0,  0,
              0,  0,  0,  5,
        };

    int[] QueenTable =
    {
            -20,-10,-10, -5,
            -10,  0,  0,  0,
            -10,  0,  5,  5,
             -5,  0,  5,  5,
              0,  0,  5,  5,
            -10,  5,  5,  5,
            -10,  0,  5,  0,
            -20,-10,-10, -5,
        };
    public Move Think(Board board, Timer timer)
    {
        BotIsWhite = board.IsWhiteToMove;
        //At the start of a move unfold all the tables Piece-Square Tables
        PawnTable = UnfoldTable(PawnTable, BotIsWhite);
        KnightTable = UnfoldTable(KnightTable, BotIsWhite);
        BishopTable = UnfoldTable(BishopTable, BotIsWhite);
        QueenTable = UnfoldTable(QueenTable, BotIsWhite);
        RookTable = UnfoldTable(RookTable, BotIsWhite);

        //Make sure that if time is getting low we drop the depth, with AB Pruning and a depth of 3 we shouldn't run out of time
        if (timer.MillisecondsRemaining <= depth4 && depths > 4)
        {
            depths = 4;
        }
        else if (timer.MillisecondsRemaining <= depth3 && depths > 3)
        {
            depths = 3;
        }

        // Get the best move
        Move moveToPlay = CalBestMove(BotIsWhite, board, true, depths, timer).move.Value;
        if (moveToPlay == null || moveToPlay.IsNull)
        {
            moveToPlay = board.GetLegalMoves()[0];
        }

        return moveToPlay;
    }

    int evaluateBoard(Board board, bool iswhite)
    {
        //This function evaluates the board based on material advantage
        var value = 0;

        //if the game is a draw the match is perfectly even
        if (board.IsDraw())
        {
            value += (iswhite ? -9000 : 9000);
            //DivertedConsole.Write($"Board Eval: {value}");
            return value;
        }

        //get each squars piece and add up the values
        for (int square = 0; square < 64; square++)
        {
            var piece = board.GetPiece(new Square(convertToSquare(square)));
            value += pieceValues[(int)piece.PieceType] * (piece.IsWhite == iswhite ? 1 : -1);

            if (piece.IsPawn)
            {
                value += PawnTable[square];
            }
            else if (piece.IsKnight)
            {
                value += KnightTable[square];
            }
            else if (piece.IsBishop)
            {
                value += BishopTable[square];
            }
            else if (piece.IsRook)
            {
                value += RookTable[square];
            }
            else if (piece.IsQueen)
            {
                value += QueenTable[square];
            }
        }
        return value;
    }

    //Gets A Half Square Piece Table and expands it
    private int[] UnfoldTable(int[] table, bool iswhite)
    {
        List<int> UnfoldedTable = new List<int>();
        for (int y = 0; y < 8; y++)
        {
            List<int> Last4 = new List<int>();
            for (int x = 0; x < 4; x++)
            {
                UnfoldedTable.Add(table[y * 4 + x]);
                Last4.Insert(0, table[y * 4 + x]);

            }
            foreach (int x in Last4)
            {
                UnfoldedTable.Add(x);
            }
        }

        if (!iswhite)
        {
            UnfoldedTable.Reverse();
        }

        return UnfoldedTable.ToArray();
    }

    moveScored CalBestMove(bool iswhite, Board board, bool ismaximising, int depth, Timer timer, int alpha = int.MinValue, int beta = int.MaxValue)
    {
        if (depth == 0)
        {
            return new moveScored()
            {
                score = evaluateBoard(board, iswhite),
                move = null
            };
        }

        //this function calculates the best move
        Random rnd = new Random();
        Move[] AllMoves = board.GetLegalMoves().OrderBy(x => rnd.Next()).ToArray();
        Move bestMoveSoFar = new Move();
        int bestMoveValue = (ismaximising ? int.MinValue : int.MaxValue);
        foreach (var move in AllMoves)
        {
            board.MakeMove(move);
            var value = CalBestMove(iswhite, board, !ismaximising, depth - 1, timer, alpha, beta).score.Value;

            if (ismaximising)
            {
                if (value > bestMoveValue)
                {
                    bestMoveValue = value;
                    bestMoveSoFar = move;
                }
                alpha = Math.Max(alpha, value);
            }
            else
            {
                if (value < bestMoveValue)
                {
                    bestMoveValue = value;
                    bestMoveSoFar = move;
                }
                beta = Math.Min(beta, value);
            }

            board.UndoMove(move);
            if (beta <= alpha)
            {
                break;
            }
        }

        moveScored newMove = new moveScored();
        newMove = new moveScored()
        {
            move = bestMoveSoFar,
            score = bestMoveValue,
        };

        return newMove;
    }

    string convertToSquare(int squareInt)
    {
        //This function converts a square index into chess notation
        var letter = "a";
        var number = 1;
        var counter = squareInt % 8;
        switch (counter)
        {
            case 1:
                letter = "b"; break;
            case 2:
                letter = "c"; break;
            case 3:
                letter = "d"; break;
            case 4:
                letter = "e"; break;
            case 5:
                letter = "f"; break;
            case 6:
                letter = "g"; break;
            case 7:
                letter = "h"; break;
        }

        //if counter represents the x axis position subtracting it and / by the length of the board will give us the y position
        number = ((squareInt - counter) / 8) + 1;
        return letter + number;
    }

    class moveScored
    {
        public int? score { get; set; }
        public Move? move { get; set; }
    }
}