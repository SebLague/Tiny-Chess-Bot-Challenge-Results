namespace auto_Bot_444;
using ChessChallenge.API;
using System;

public class Bot_444 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 325, 500, 900, 10000 };
    Random rng = new Random();
    bool odd = false;
    bool endgame = false;

    Square[] centerSquares = { new Square(2,2), new Square(2,3), new Square(2,4), new Square(2,5),
                               new Square(3,2), new Square(3,3), new Square(3,4), new Square(3,5),
                               new Square(4,2), new Square(4,3), new Square(4,4), new Square(4,5),
                               new Square(5,2), new Square(5,3), new Square(5,4), new Square(5,5), };

    public Move Think(Board board, Timer timer)
    {
        Move[] allMoves = board.GetLegalMoves();

        //Count the total amount of pieces on the board
        int count = 0;
        foreach (PieceList list in board.GetAllPieceLists())
            count += list.Count;

        if (count < 12)
            endgame = true;

        Random rng = new();

        //Call the iterative minmax funtion, taking into account the game stage and time left
        RatedMove? wantedMove = LookForward(board, (timer.MillisecondsRemaining > 5000) ? (count < 8 && !odd ? 5 : 3) : 1);

        odd = !odd;

        Move moveToPlay;
        if (wantedMove.HasValue)
            moveToPlay = wantedMove.Value.GetMove();
        else
            moveToPlay = allMoves[rng.Next(allMoves.Length)];

        return moveToPlay;
    }

    public RatedMove? LookForward(Board board, int steps)
    {
        RatedMove? bestMove = null; //keep track of the best move in this depth
        RatedMove? worstMove = null; //we tried to remove this bs unsuccessfully
        if (steps == 0)
        {
            // evaluate current board position
            float oldVal = 0;
            foreach (PieceType pt in Enum.GetValues(typeof(PieceType)))
            {
                if (pt == PieceType.King || pt == PieceType.None)
                    continue;
                if (pt == PieceType.Pawn)
                {
                    foreach (Piece piece in board.GetPieceList(pt, true))
                        oldVal += 100 * (piece.Square.Rank / (endgame ? 4 : 7) + 1);
                    foreach (Piece piece in board.GetPieceList(pt, false))
                        oldVal -= 100 * ((7 - piece.Square.Rank) / (endgame ? 4 : 7) + 1);
                    continue;
                }
                oldVal += (board.GetPieceList(pt, true).Count - board.GetPieceList(pt, false).Count) * pieceValues[(int)pt];
            }
            if (!endgame)
            {
                foreach (Square square in centerSquares)
                {
                    Piece piece = board.GetPiece(square);
                    oldVal += (piece.IsNull || piece.IsKing || piece.IsQueen) ? 0 : (piece.IsWhite ? 30 : -30);
                }
            }
            oldVal *= board.IsWhiteToMove ? -1 : 1;
            foreach (Move move in board.GetLegalMoves())
            {
                float value = oldVal + rng.NextSingle() * 40 - 20; //Adding a pinch of sult
                PieceType moved = move.MovePieceType;
                Square sq = move.TargetSquare;
                board.MakeMove(move);
                if (board.IsInCheck() && endgame)
                    value -= 200;
                else if (board.IsInCheck() && !endgame)
                    value -= 100;
                if (move.IsCapture)
                    value -= pieceValues[(int)move.CapturePieceType];
                else if (move.IsCastles)
                    value -= 100;
                else if (move.IsPromotion)
                    value -= pieceValues[(int)move.PromotionPieceType];
                else if (board.IsInCheckmate())
                    value = -float.MaxValue;
                else if (board.IsDraw())
                    value = 20;
                if (bestMove == null)
                {
                    bestMove = new RatedMove(move, value);
                    worstMove = new RatedMove(move, value);
                }
                else if (bestMove.Value.GetValue() < value)
                    bestMove = new RatedMove(move, value);
                else if (worstMove.Value.GetValue() > value)
                    worstMove = new RatedMove(move, value);
                board.UndoMove(move);
            }
        }
        else
        {
            foreach (Move move in board.GetLegalMoves())
            {
                board.MakeMove(move);
                float cur = LookForward(board, steps - 1).Value.GetValue(); //Iterate
                float change = 0;

                if (cur == float.MaxValue || cur == -float.MaxValue)
                    change = 0;
                else if (move.IsCastles)
                    change = 75;
                else if (move.MovePieceType == PieceType.King && !endgame)
                    change = -50;
                if (bestMove == null)
                {
                    bestMove = new RatedMove(move, cur + change);
                    worstMove = new RatedMove(move, cur + change);
                }
                else if (cur + change > bestMove.Value.GetValue())
                    bestMove = new RatedMove(move, cur + change);
                else if (cur + change < worstMove.Value.GetValue())
                    worstMove = new RatedMove(move, cur + change);
                board.UndoMove(move);
            }
        }
        if (bestMove != null)
        {
            bestMove.Value.flipValue();
            worstMove.Value.flipValue();
        }
        else
        {
            if (board.IsInCheckmate())
            {
                bestMove = new RatedMove(new Move(), float.MaxValue);
                worstMove = new RatedMove(new Move(), float.MaxValue);
            }
            else
            {
                bestMove = new RatedMove(new Move(), -20);
                worstMove = new RatedMove(new Move(), -20);
            }
        }

        if (steps % 2 == 0) //Again, we tried, adn failed to remove this
            return worstMove;
        return bestMove;
    }

    //A tupple of a move and how good it is
    public struct RatedMove
    {
        Move move;
        float value;

        public RatedMove(Move move, float value)
        {
            this.move = move;
            this.value = value;
        }

        public float GetValue() => value;
        public Move GetMove() => move;

        public void flipValue()
        {
            value *= -1;
        }

    }
}