namespace auto_Bot_278;
using ChessChallenge.API;
using System;

public class Bot_278 : IChessBot
{
    Random random = new Random();
    //Material for air, pawn, knight, bishop, rook, queen, and king, respectively
    int[] PieceMaterial = { 0, 10, 30, 35, 50, 90, 6969 };
    int Evaluation;

    //This is the function that returns the move to be actually played
    public Move Think(Board Position, Timer timer)
    {
        /*
        int Depth = 10;

        bool IsWhite = Position.IsWhiteToMove;

        Move[] LegalMoves = Position.GetLegalMoves();
        Move MoveToPlay = DetermineMove(Position, Depth, LegalMoves, IsWhite);

        // DivertedConsole.Write("Played " + MoveToPlay.MovePieceType + " to " + MoveToPlay.TargetSquare);
        return MoveToPlay;
        */

        switch (Position.PlyCount)
        {
            case 0:
                return new Move("e2e4", Position);
            case 1:
                return new Move("e7e5", Position);
            case 2:
                return new Move("e1e2", Position);
            case 3:
                return new Move("e8e7", Position);
        }

        Move[] Moves = Position.GetLegalMoves();

        foreach (Move move in Moves)
        {
            if (move.IsCapture || move.IsPromotion) return move;

            Position.MakeMove(move);

            if (Position.IsInCheckmate()) return move;
            if (Position.IsInStalemate())
            {
                Position.UndoMove(move);
                continue;
            }
            Position.UndoMove(move);
        }

        return Moves[random.Next(Moves.Length)];
    }

    /*
    int CalculateMaterialScore(Board Position)
    {
        int Score = 0;
        PieceList[] Pieces = Position.GetAllPieceLists();
        foreach (PieceList pieceList in Pieces)
        {
            foreach (Piece piece in pieceList)
            {
                if (pieceList.IsWhitePieceList) Score += PieceMaterial[(int)piece.PieceType];
                else Score -= PieceMaterial[(int)piece.PieceType];
            }

        }
        // DivertedConsole.Write("Material score: " + Score);
        return Score;
    }

    int CalculateMobilityScore(Move[] LegalMoves, Move[] OpponentLegalMoves, bool ForWhite)
    {
        int Score;
        Score = LegalMoves.Length - OpponentLegalMoves.Length;
        // DivertedConsole.Write("Legal moves: " + LegalMoves.Length + ", opponent's legal moves: " +  OpponentLegalMoves.Length);
        // DivertedConsole.Write("Mobility score: " + Score);
        if (!ForWhite) Score *= -1;
        return Score;
    }

    int CalculateSpaceScore(Move[] LegalMoves, Move[] OpponentLegalMoves, bool IsWhite)
    {
        int Score = 0;
        foreach (Move move in LegalMoves)
        {
            if (move.TargetSquare.Index < 32)
            {
                // DivertedConsole.Write("Moves: " + move.MovePieceType + " to " + move.TargetSquare);
                Score++;
            }
        }
        foreach (Move move in OpponentLegalMoves)
        {
            if (move.TargetSquare.Index >= 32)
            {
                // DivertedConsole.Write("Opponent moves: " + move.MovePieceType + " to " + move.TargetSquare);
                Score--;
            }
        }
        DivertedConsole.Write("Space score: " + Score);
        return Score;
    }

    Move DetermineMove(
        Board Position, int Depth, Move[] LegalMoves, bool ForWhite
    ) {
        //Play e4 as the first move if white
        if (Position.PlyCount == 0) return new Move("e2e4", Position);

        Depth--;
        Move[] MoveToPlay = { LegalMoves[0] };
        foreach (Move move in LegalMoves)
        {
            string MoveText = move.MovePieceType + " to " + move.TargetSquare;
            Position.MakeMove(move);

            Move[] OpponentLegalMoves = Position.GetLegalMoves();

            Position.ForceSkipTurn();
            Move[] NewLegalMoves = Position.GetLegalMoves();

            int CurrentEvaluation =
                CalculateMaterialScore(Position) + 
                CalculateMobilityScore(NewLegalMoves, OpponentLegalMoves, ForWhite);
            DivertedConsole.Write(MoveText + ": " + CurrentEvaluation);
            DivertedConsole.Write(ForWhite);
            DivertedConsole.Write(Evaluation);

            if (CurrentEvaluation == Evaluation)
            {
                MoveToPlay.Append(move);
                foreach (Move move1 in MoveToPlay) DivertedConsole.Write(move1);
            }
            if (ForWhite)
            {
                if (CurrentEvaluation > Evaluation)
                {
                    MoveToPlay = new Move[] { move };
                }
            }
            else if (CurrentEvaluation < Evaluation)
            {
                MoveToPlay = new Move[] { move };
            }
            // DivertedConsole.Write(Position.CreateDiagram(true, false, false, MoveToPlay.TargetSquare));

            Position.UndoSkipTurn();
            Position.UndoMove(move);
        }
        return MoveToPlay[random.Next(MoveToPlay.Length)];
    }
    */
}