namespace auto_Bot_571;
using ChessChallenge.API;
using System;

public class Bot_571 : IChessBot
{
    Random rand = new Random();
    int iterator;
    int numOfPieces;

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        DivertedConsole.Write("----------");

        // Determine the number of pieces on the board, for certain move checks
        PieceList[] pieces = board.GetAllPieceLists();
        numOfPieces = 0;
        foreach (PieceList piece in pieces)
        {
            numOfPieces += piece.Count;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheckmate())
            {
                DivertedConsole.Write("Get the King!");
                return move;
            }
            board.UndoMove(move);
        }

        // Defend Check, if bot is in check
        if (board.IsInCheck())
        {
            return defend(moves);
        }
        // Siege Check, if there is below approximately 16 pieces
        if (numOfPieces <= impulsivity(16))
        {
            return siege(board);
        }
        // Advance Check, if there is above approximately 28 pieces
        if (numOfPieces > impulsivity(28))
        {
            return advance(moves);
        }
        // Attack Check, if there are any valid attacks
        if (board.GetLegalMoves(true).Length > 0)
        {
            return attack(board.GetLegalMoves(true));
        }
        // Fallback, make a random move
        return panic(moves);
    }

    // Return a slightly different number than provided
    public int impulsivity(int num)
    {
        return num + (rand.Next(11) - 5);
    }

    public Move panic(Move[] moves)
    {
        DivertedConsole.Write("PANIC!");
        return moves[rand.Next(moves.Length)];
    }

    // Phase 1 (Advance): Make Pawn moves only, until 4 pieces are taken
    public Move advance(Move[] moves)
    {
        DivertedConsole.Write("Advance!");
        iterator = 0;
        Move testMove;
        // Filter through all legal moves and verify move is pawn move
        while (true)
        {
            iterator++;
            testMove = moves[rand.Next(moves.Length)];
            if (testMove.MovePieceType == PieceType.Pawn)
            {
                return testMove;
            }
            if (iterator > 20)
            {
                return panic(moves);
            }
        }
    }

    // Phase 2 (Attack): Take any attack, otherwise, panic
    public Move attack(Move[] moves)
    {
        DivertedConsole.Write("Attack!");
        iterator = 0;

        Move testMove;
        while (true)
        {
            iterator++;
            testMove = moves[rand.Next(moves.Length)];
            if (testMove.IsCapture)
            {
                return testMove;
            }
            if (iterator > 20)
            {
                return panic(moves);
            }
        }
    }

    // While in check, make any non-king move, otherwise, attack
    public Move defend(Move[] moves)
    {
        DivertedConsole.Write("Defend!");
        iterator = 0;
        Move testMove;
        while (true)
        {
            iterator++;
            testMove = moves[rand.Next(moves.Length)];
            if (!testMove.MovePieceType.Equals(PieceType.King))
            {
                return testMove;
            }
            if (iterator > 20)
            {
                return attack(moves);
            }
        }
    }

    // When less than half of pieces remain, try to make any check move
    public Move siege(Board board)
    {
        DivertedConsole.Write("Set up a Siege!");
        Move[] moves = board.GetLegalMoves();
        iterator = 0;
        Move testMove;
        while (true)
        {
            iterator++;
            testMove = moves[rand.Next(moves.Length)];

            // Test making the move, if it results in check, return the move
            board.MakeMove(testMove);
            if (board.IsInCheck())
            {
                return testMove;
            }
            // Reset the board in order to properly check cases
            board.UndoMove(testMove);

            if (iterator > 40)
            {
                return attack(moves);
            }
        }
    }
}