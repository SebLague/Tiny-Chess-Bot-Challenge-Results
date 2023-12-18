namespace auto_Bot_39;
using ChessChallenge.API;
using System;

public class Bot_39 : IChessBot
{
    public int[] pieceVals = { 0, 100, 300, 320, 500, 900, 10000 }; // nothing, pawn, knight, bishop, rook, queen, king
    /// <summary>

    /// Blind Bot:
    /// Probably the best chess bot that CANNOT look ahead!
    /// This was a fun challenge!
    /// This bot can only check evaluation for the current move using a very complex hand-made evaluation function
    /// This took a while!
    /// The most major advantage is that it finishes each move in ~5 ms.
    /// In the massive fight, this can only win against bots who check up to like 30 moves.
    /// However I think this would be an intresting experiment and would be fun for the grand finale video.

    /// </summary>
    public int MovesPlayed = 0;
    public int kingMoves = 0;

    public int piecesLeft(Board board)
    {
        int count = 0;
        for (int i = 0; i < 64; i++)
        {
            Square square = new Square(i);
            if (board.GetPiece(square).PieceType != PieceType.None) count++;
        }
        return count;
    }

    public bool moveIsCheckmate(Board board, Move move)
    {
        board.MakeMove(move);
        bool isMate = board.IsInCheckmate();
        board.UndoMove(move);
        return isMate;
    }

    public bool moveIsDraw(Board board, Move move)
    {
        board.MakeMove(move);
        bool isDraw = board.IsDraw();
        board.UndoMove(move);
        return isDraw;
    }

    public bool moveIsCheck(Board board, Move move)
    {
        board.MakeMove(move);
        bool isCheck = board.IsInCheck();
        board.UndoMove(move);
        return isCheck;
    }


    public int evaluateMove(Move move, Board board) // evaluates the move
    {
        PieceType capturedPiece = move.CapturePieceType;
        int eval = 0;
        eval = pieceVals[(int)capturedPiece];
        if (eval > 0) { eval += 5; }
        if (board.SquareIsAttackedByOpponent(move.TargetSquare)) // uh oh here come the piece square tables
        {
            eval -= pieceVals[(int)move.MovePieceType];
        }
        ///<summary>
        /// Piece square tables for all pieces except king.
        /// This also includes that you should push out your queen early game.
        /// This will priortise "good" moves like castling and promoting over worse moves.
        /// It will also transition into endgame tables where your queen and rook are more important.
        /// This is responsible for more than half of the tokens BTW.
        /// </summary>
        if (move.MovePieceType == PieceType.Knight)// Knight piece square table, will prefer to be in the middle.
        {
            eval += 15;
            if (move.TargetSquare.File == 7 || move.TargetSquare.File == 6 || move.TargetSquare.File == 0 || move.TargetSquare.File == 1) eval -= 60;
            if (move.TargetSquare.File == 2 || move.TargetSquare.File == 3 || move.TargetSquare.File == 4 || move.TargetSquare.File == 5)
            {
                if (move.TargetSquare.Rank == 2 || move.TargetSquare.Rank == 3 || move.TargetSquare.Rank == 4 || move.TargetSquare.Rank == 5) eval += 45;
            }
        }
        if (move.MovePieceType == PieceType.Bishop)// Bishop piece square table
        {
            if (piecesLeft(board) > 28) eval -= 30;
            eval += 15;
            if (move.TargetSquare.File == 2 || move.TargetSquare.File == 3 || move.TargetSquare.File == 4 || move.TargetSquare.File == 5)
            {
                if (move.TargetSquare.Rank == 2 || move.TargetSquare.Rank == 3 || move.TargetSquare.Rank == 4 || move.TargetSquare.Rank == 5) eval += 45;
            }
        }
        if (move.MovePieceType == PieceType.Rook)// Rook piece square table + transition to endgame
        {
            if (board.IsWhiteToMove) { if (move.TargetSquare.Rank == 7) eval += 40; }
            else if (move.TargetSquare.Rank == 2) eval += 40;
            eval += 15;
            if (move.TargetSquare.File == 3 || move.TargetSquare.File == 4) eval += 30;
            if (piecesLeft(board) > 28) eval -= 30;
            if (MovesPlayed < 9) eval -= 50;
        }
        if (move.MovePieceType == PieceType.Queen)// Queen piece square table + transition to mid/endgame
        {
            if (piecesLeft(board) < 14) eval += 25;
            else eval -= 10;
            if (move.TargetSquare.File == 2 || move.TargetSquare.File == 3 || move.TargetSquare.File == 4 || move.TargetSquare.File == 5)
            {
                if (move.TargetSquare.Rank == 2 || move.TargetSquare.Rank == 3 || move.TargetSquare.Rank == 4 || move.TargetSquare.Rank == 5) eval += 45;
            }
            if (MovesPlayed < 4) return -600;
        }

        if (move.MovePieceType == PieceType.Pawn)// Pawn "piece square table" This is mainly for early game
        {
            if (piecesLeft(board) < 14 || piecesLeft(board) > 28)
            {
                eval += 10;
                if (move.TargetSquare.File == 4 || move.TargetSquare.File == 5 && move.TargetSquare.Rank == 4 || move.TargetSquare.Rank == 5) eval += 30;
            }
            if (MovesPlayed > 8) eval -= 15;
            eval += 5;
            if (piecesLeft(board) < 8) eval += 70;
        }

        if (move.IsCastles) { eval += 50; } // castling is encouraged
        if (move.IsEnPassant) { eval += 999999999; } // en passant is forced obv

        // We're out of the piece square tables!
        // This is for the flags to buff certain moves and nerf others
        // e.g. Checkmate is the highest priority move tied with en passant
        // Drawing is discouraged massively.
        // Checks are encouraged.
        // Moving away a piece that is attack is encouraged heavily.
        // Promotions are worth sacrificing a rook

        if (moveIsCheckmate(board, move))
        {
            eval = 999999999;
        }
        if (moveIsDraw(board, move))
        {
            eval -= 1000;
        }
        if (moveIsCheck(board, move))
        {
            eval += 20;
        }
        if (board.SquareIsAttackedByOpponent(move.StartSquare))
        {
            eval += 120;
        }
        if (move.IsPromotion)
        {
            eval += 600;
        }

        return eval;
    }



    public Move Think(Board board, Timer timer)
    {

        Move[] moves = board.GetLegalMoves();

        DivertedConsole.Write(" ");

        Random rng = new Random();
        Move moveToPlay = moves[rng.Next(moves.Length)];
        int bestEvaluation = -999999;
        Move[] sameEvalMoves = { };
        foreach (Move move in moves)
        {
            DivertedConsole.Write(move.ToString() + evaluateMove(move, board));
            if (evaluateMove(move, board) > bestEvaluation)
            {
                bestEvaluation = evaluateMove(move, board);
                moveToPlay = move;
            }
        }
        DivertedConsole.Write(bestEvaluation);
        MovesPlayed++;
        if (moveToPlay.MovePieceType == PieceType.King) kingMoves++;
        return moveToPlay;
    }
}