namespace auto_Bot_57;
using ChessChallenge.API;
using System;

public class Bot_57 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        // Get my moves
        Move[] moves = board.GetLegalMoves();

        // What color am I? It is important when trying to promote pawns
        bool color = board.IsWhiteToMove;

        // This offset number describes what rank a pawn will try to get to for promotion
        int offset = 7;
        if (color == false)
        {
            offset = 0;
        }

        // Call the best move function
        Move move = bestMove(moves);



        // Having this function external may take up more brain space, but I makes things easier for me.
        Move bestMove(Move[] moves)
        {
            // I'll loop through each move to and assign each move a score, the record needs to start very low for this to work.
            double record = -1000000;
            int index = 0;
            for (var i = 0; i < moves.Length; i++)
            {
                double score = 0;

                // Will this move allow the opponent to checkmate me? Most rounds against itself should end in a draw if this works right

                if (nextMate(moves[i]))
                {
                    score -= 1000000;
                }

                // Can I capture a piece?
                if (moves[i].CapturePieceType != PieceType.None)
                {
                    score += (int)moves[i].CapturePieceType * 5 + 5;
                }

                // The code below helps encourage pieces to move towards an enemy king 
                Square king = board.GetKingSquare(!board.IsWhiteToMove);
                Square pieceStart = moves[i].StartSquare;
                Square pieceEnd = moves[i].TargetSquare;
                Random rnd = new Random();
                double num = rnd.NextDouble() * 3 - 1.5;

                if (moves[i].MovePieceType == PieceType.Pawn)
                {
                    // Once the board gets clear enough, this code will help pawns work towards being promoted.
                    PieceList[] pieces = board.GetAllPieceLists();
                    int pieceCount = 0;
                    foreach (PieceList list in pieces)
                    {
                        pieceCount += list.Count;
                    }
                    if (pieceCount > 16)
                    {
                        score += ((distance(pieceStart, king) - distance(pieceEnd, king)) * 4 + num);
                    }
                    else
                    {
                        Square endRank = new Square(0, offset);
                        Square startSquare = new Square(0, pieceStart.Rank);
                        Square endSquare = new Square(0, pieceEnd.Rank);
                        score += (distance(startSquare, endRank) - distance(endSquare, endRank)) * (int)moves[i].MovePieceType * 4.5;
                    }
                }
                else
                {
                    score += (distance(pieceStart, king) - distance(pieceEnd, king) + num);
                }

                // The code above helps encourage pieces to move towards an enemy king 

                // We don't want to move into enemy fire
                if (board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
                {
                    score -= (int)moves[i].MovePieceType * 5;
                }

                // If this move is trying to get us out of enemy fire then that is very good.
                if (board.SquareIsAttackedByOpponent(moves[i].StartSquare) && !board.SquareIsAttackedByOpponent(moves[i].TargetSquare))
                {
                    score += (int)moves[i].MovePieceType * 5;
                }

                int queensAttackedBefore = numberAttacked(board.GetPieceList(PieceType.Queen, board.IsWhiteToMove));


                // We'll test this move to check for...
                board.MakeMove(moves[i]);

                int queensAttackedAfter = numberAttacked(board.GetPieceList(PieceType.Queen, !board.IsWhiteToMove));

                // We want to check the enemy
                if (board.IsInCheck())
                {
                    score += 7.5;
                }

                // We want to avoid draws and stalemates
                if (board.IsDraw() || board.IsRepeatedPosition() || board.IsInsufficientMaterial())
                {
                    score -= 50;
                }

                // If the next move is checkmate then there is no doubt on what to do
                if (board.IsInCheckmate())
                {
                    return moves[i];
                }

                // Undo the move to clean everything up
                board.UndoMove(moves[i]);

                if (moves[i].IsPromotion)
                {
                    if (moves[i].PromotionPieceType == PieceType.Queen)
                    {
                        score += 10;
                    }
                }

                score += (queensAttackedBefore - queensAttackedAfter) * ((int)PieceType.Queen + 1) * ((int)PieceType.Queen + 1);

                // If this move is better then previous ones then make it the new best move
                if (score > record)
                {
                    record = score;
                    index = i;
                }
            }
            // And finally return
            return moves[index];
        }


        // We need to calculate the distance to encourage pieces to move towards the king, and where we want them
        // Thanks Pythag
        double distance(Square square1, Square square2)
        {
            return Math.Sqrt((square1.File - square2.File) * (square1.File - square2.File) + (square1.Rank - square2.Rank) * (square1.Rank - square2.Rank));
        }

        int numberAttacked(PieceList list)
        {
            int count = 0;

            foreach (Piece piece in list)
            {
                if (board.SquareIsAttackedByOpponent(piece.Square))
                {
                    count++;
                }
            }

            return count;
        }

        // This checks if a move will allow the other player to checkmate me. I'm assuming that most other bots will automatically play a move if it is checkmate, so this helps prevent that.
        bool nextMate(Move move)
        {
            bool mate = false;

            board.MakeMove(move);

            Move[] oppMoves = board.GetLegalMoves();

            foreach (Move m in oppMoves)
            {
                board.MakeMove(m);
                if (board.IsInCheckmate())
                {
                    mate = true;
                }
                board.UndoMove(m);
            }

            board.UndoMove(move);

            return mate;
        }


        // Return our move!
        return move;
    }
}