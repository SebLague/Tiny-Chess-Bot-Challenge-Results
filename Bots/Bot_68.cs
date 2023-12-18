namespace auto_Bot_68;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_68 : IChessBot
{
    const ulong whiteSquares = 0b0101010110101010010101011010101001010101101010100101010110101010;
    const ulong blackSquares = 0b1010101001010101101010100101010110101010010101011010101001010101;
    string[] letters = { "a", "b", "c", "d", "e", "f", "g", "h" };
    Random rd = new Random();

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> priority1Moves = new List<Move>();
        List<Move> priority2Moves = new List<Move>();
        List<Move> priority3Moves = new List<Move>();
        List<Move> priority4Moves = new List<Move>();
        List<Move> priority5Moves = new List<Move>();
        bool white = board.IsWhiteToMove;

        foreach (Move m in moves)
        {
            bool isPriority4Or5 = false;

            if (BitboardHelper.SquareIsSet(whiteSquares, m.TargetSquare) == white)
            {
                // Lands on legal square
                if (m.IsCapture)
                {
                    // Captures enemy on its wrong color (my correct color) (P1)
                    priority1Moves.Add(m);
                }
                else if (BitboardHelper.SquareIsSet(whiteSquares, m.StartSquare) == white)
                {
                    // Starts on legal square and ends on legal square (priority 5)
                    priority5Moves.Add(m);
                    isPriority4Or5 = true;
                }
                else
                {
                    // Starts on illegal square and ends on legal square (priority 4)
                    priority4Moves.Add(m);
                    isPriority4Or5 = true;
                }
            }
            else if (m.IsCapture)
            {
                // Captures enemy on its correct color (my wrong color) (P2)
                priority2Moves.Add(m);
            }

            if (isPriority4Or5 && board.TrySkipTurn())
            {
                // Check if move threats enemy pieces on illegal tiles
                int numThreatsOnIllegalTiles = 0;
                ulong enemyPiecesOnIllegalTiles;

                if (white)
                {
                    enemyPiecesOnIllegalTiles = board.BlackPiecesBitboard & whiteSquares;
                }
                else
                {
                    enemyPiecesOnIllegalTiles = board.WhitePiecesBitboard & blackSquares;
                }
                while (enemyPiecesOnIllegalTiles != 0)
                {
                    int illegalTileIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref enemyPiecesOnIllegalTiles);
                    string tileName = letters[illegalTileIndex % 8] + Convert.ToString(illegalTileIndex / 8 + 1);
                    if (board.SquareIsAttackedByOpponent(new Square(tileName)))
                    {
                        // This piece was already threatened
                        numThreatsOnIllegalTiles--;
                        //DivertedConsole.Write("Old threat on " + tileName);
                    }
                }

                board.UndoSkipTurn();
                board.MakeMove(m);
                if (white)
                {
                    enemyPiecesOnIllegalTiles = board.BlackPiecesBitboard & whiteSquares;
                }
                else
                {
                    enemyPiecesOnIllegalTiles = board.WhitePiecesBitboard & blackSquares;
                }
                while (enemyPiecesOnIllegalTiles != 0)
                {
                    int illegalTileIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref enemyPiecesOnIllegalTiles);
                    string tileName = letters[illegalTileIndex % 8] + Convert.ToString(illegalTileIndex / 8 + 1);
                    if (board.SquareIsAttackedByOpponent(new Square(tileName)))
                    {
                        // This piece is newly threatened
                        numThreatsOnIllegalTiles++;
                        //DivertedConsole.Write("New threat on " + tileName);
                    }
                }
                if (numThreatsOnIllegalTiles > 0)
                {
                    // More threats on enemy pieces are unlocked; priority 3
                    priority3Moves.Add(m);
                }
                board.UndoMove(m);
            }
        }

        if (priority1Moves.Count > 0)
        {
            //DivertedConsole.Write("Capturing illegal piece");
            return priority1Moves[rd.Next(priority1Moves.Count)];
        }
        else if (priority2Moves.Count > 0)
        {
            //DivertedConsole.Write("Capturing legal piece");
            return priority2Moves[rd.Next(priority2Moves.Count)];
        }
        else if (priority3Moves.Count > 0)
        {
            //DivertedConsole.Write("Found threat");
            return priority3Moves[rd.Next(priority3Moves.Count)];
        }
        else if (priority4Moves.Count > 0)
        {
            //DivertedConsole.Write("Moving to legal square");
            return priority4Moves[rd.Next(priority4Moves.Count)];
        }
        else if (priority5Moves.Count > 0)
        {
            //DivertedConsole.Write("Moving from legal to legal square");
            return priority5Moves[rd.Next(priority5Moves.Count)];
        }
        else
        {
            //DivertedConsole.Write("Moving to illegal square");
            return moves[rd.Next(moves.Length)];
        }
    }
}