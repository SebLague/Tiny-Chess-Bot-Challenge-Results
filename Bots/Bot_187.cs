namespace auto_Bot_187;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_187 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();

        // Dictionary for the Pieces and there Values
        Dictionary<string, int> pieceValues = new()
        {
            { "White Pawn", 10 },
            { "Black Pawn", 10 },

            { "White Knight", 40 },
            { "Black Knight", 40 },

            { "White Bishop", 40 },
            { "Black Bishop", 40 },

            { "White Rook", 70 },
            { "Black Rook", 70 },

            { "White Queen", 110 },
            { "Black Queen", 110 },

            { "White King", 1110 },
            { "Black King", 1110 },

            { "Pawn", 10 },
            { "Knight", 40 },
            { "Bishop", 40 },
            { "Rook", 70 },
            { "Queen", 110 },
            { "King", 1110 }
        };

        // Calculate Move
        Move ReturnRandomMove()
        {
            Move randomMove = new();

            var random = new Random();
            int lenght = moves.Length;
            int rnd = random.Next(lenght);

            randomMove = moves[rnd];

            return randomMove;
        }

        Move CalculateBestMove()
        {
            Move bestMove = new();

            int bestCapture = 0;

            foreach (var move in moves)
            {
                if (move.IsCapture)
                {
                    var targetPiece = board.GetPiece(move.TargetSquare);
                    if (targetPiece.ToString() == null)
                    {
                        continue;
                    }
                    else
                    {
                        if (targetPiece.ToString() == "Null") { continue; }

                        int pieceValue = pieceValues[targetPiece.ToString()];
                        int capturingPieceValue = pieceValues[move.MovePieceType.ToString()];

                        if (pieceValue < capturingPieceValue)
                        {
                            if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                            {
                                bestCapture = pieceValue;
                                bestMove = move;
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (bestCapture < pieceValue)
                        {
                            bestCapture = pieceValue;
                            bestMove = move;
                        }
                    }
                }
            }

            if (bestMove.IsCapture)
            {
                return bestMove;
            }

            if (bestMove.IsPromotion)
            {
                return bestMove;
            }

            while (true)
            {
                bestMove = ReturnRandomMove();

                if (bestMove.IsCastles)
                {
                    break;
                }

                if (bestMove.MovePieceType == PieceType.King)
                {
                    int count = 0;
                    foreach (var move in moves)
                    {
                        if (move.MovePieceType != PieceType.King)
                        {
                            count++;
                        }
                    }
                    if (count == 0)
                    {
                        bestMove = moves[0];
                        break;
                    }
                    else
                    {
                        continue;
                    }
                }
                else
                {
                    break;
                }
            }

            return bestMove;
        }

        Move GetBestMove()
        {
            Move bestMove = new();
            int runCounter = 0;
            bool attackMode = false;

            if (board.IsInCheck())
            {
                foreach (Move move in moves)
                {
                    if (move.MovePieceType == PieceType.King)
                    {
                        bestMove = move;
                        return bestMove;
                    }
                }
            }

            bestMove = CalculateBestMove();
            var startSquare = bestMove.StartSquare;

            if (!board.GetPiece(startSquare).IsWhite)
            {
                int knightBlack = board.GetPieceList(PieceType.Knight, true).Count;
                int bishopBlack = board.GetPieceList(PieceType.Bishop, true).Count;
                int rookBlack = board.GetPieceList(PieceType.Rook, true).Count;
                int queenBlack = board.GetPieceList(PieceType.Queen, true).Count;

                if (knightBlack + bishopBlack + rookBlack + queenBlack == 0)
                {
                    attackMode = true;
                }
            }
            else
            {
                int knightWhite = board.GetPieceList(PieceType.Knight, false).Count;
                int bishopWhite = board.GetPieceList(PieceType.Bishop, false).Count;
                int rookWhite = board.GetPieceList(PieceType.Rook, false).Count;
                int queenWhite = board.GetPieceList(PieceType.Queen, false).Count;

                if (knightWhite + bishopWhite + rookWhite + queenWhite == 0)
                {
                    attackMode = true;
                }
            }

            if (attackMode)
            {
                bool pawns = false;

                foreach (Move move in moves)
                {
                    if (move.MovePieceType == PieceType.Pawn)
                    {
                        pawns = true;
                    }
                }

                Random random = new();
                int rng = random.Next(1, 4);

                if (pawns)
                {
                    if (rng == 1)
                    {
                        foreach (Move move in moves)
                        {
                            if (move.MovePieceType == PieceType.Pawn)
                            {
                                bestMove = move;
                                return bestMove;
                            }
                        }
                    }
                    else
                    {
                        int secondRng = random.Next(1, 4);
                        List<Move> possibleChecks = new();

                        foreach (Move move in moves)
                        {
                            board.MakeMove(move);

                            if (board.IsInCheck())
                            {
                                board.UndoMove(move);

                                if (board.SquareIsAttackedByOpponent(move.TargetSquare)) { continue; }

                                possibleChecks.Add(move);

                                continue;
                            }
                            board.UndoMove(move);
                        }

                        if (possibleChecks.Count != 0)
                        {
                            int helper = random.Next(possibleChecks.Count);
                            return possibleChecks[helper];
                        }
                    }
                    return bestMove;
                }
                else { return ReturnRandomMove(); }
            }


            if (board.SquareIsAttackedByOpponent(bestMove.TargetSquare))
            {
                runCounter++;
                bestMove = CalculateBestMove();

                if (runCounter == 10) { return bestMove; }
            }
            else
            {
                return bestMove;
            }

            return bestMove;
        }

        Move GetMove()
        {
            Move move = GetBestMove();

            if (move.MovePieceType == PieceType.King)
            {
                var startSquare = move.StartSquare;
                if (startSquare.Rank == 0) { move = GetBestMove(); }
                if (startSquare.Rank == 7) { move = GetBestMove(); }
            }
            return move;
        }

        return GetMove();
    }
}