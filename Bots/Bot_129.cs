namespace auto_Bot_129;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_129 : IChessBot
{
    private class MichaelMove
    {
        public Move _Move;
        public float _Score { get; set; }
        public MichaelMove(Move move, float score = 0)
        {
            _Move = move;
            _Score = score;
        }

    };
    private float[] _PieceVals = new float[7]
    {
        0, // None
        1, // Pawn
        3, // Knight
        3, // Bishop
        5, // Rook
        8, // Queen
        10 // King
    };
    PieceType _LastMovedPiece = PieceType.None;


    // Is the move that we are currently exploring an attack on another piece?
    private bool IsAttack(Board board, Move m)
    {
        // Skip the other player's turn
        if (!board.TrySkipTurn()) return false;
        // Look for all current moves that are captures from the start square
        bool retVal = false;
        foreach (Move secondMove in board.GetLegalMoves(true))
            if (secondMove.StartSquare == m.TargetSquare && !IsSquareDefended(board, m.TargetSquare))
            {
                retVal = true;
                break;
            }
        board.UndoSkipTurn();
        return retVal;
    }


    // Find out if starting square of move m is defended
    private bool IsSquareDefended(Board board, Square s)
    {
        if (!board.TrySkipTurn()) return false;
        bool isDefended = board.SquareIsAttackedByOpponent(s);
        board.UndoSkipTurn();
        return isDefended;
    }


    private int MoveSort(MichaelMove a, MichaelMove b)
    {
        // TODO: Optimize
        return (int)((b._Score - a._Score) * 1000);
    }

    // Does not modify board state, simply queries legal moves of current player
    // and sees if they can attack square S
    float GetLowestPieceValueAttackingSquare(Board board, Square s)
    {
        float pieceVal = 10;
        foreach (Move m in board.GetLegalMoves())
            if (m.TargetSquare == s) pieceVal = Math.Min(_PieceVals[(int)m.MovePieceType], pieceVal);
        return pieceVal == 10 ? 0 : pieceVal;
    }

    // Given a pair of ranks or files, returns the direction from b to a.
    int GetDir(int a, int b)
    {
        if (a == b) return 0;
        if (a > b) return 1;
        return -1;
    }

    // Assumes input Move m has already been made
    bool IsMovingTowardEnemyPiece(Board board, Move m)
    {
        int msr = m.StartSquare.Rank;
        int msf = m.StartSquare.File;
        int rankDir = GetDir(m.TargetSquare.Rank, msr);
        int fileDir = GetDir(m.TargetSquare.File, msf);
        var allPieces = board.GetAllPieceLists();
        foreach (var pieceList in allPieces)
            if (pieceList.IsWhitePieceList == board.IsWhiteToMove)
                foreach (var piece in pieceList)
                    if (GetDir(piece.Square.File, msf) == fileDir &&
                        GetDir(piece.Square.Rank, msr) == rankDir) return true;
        return false;
    }

    private float ScoreMove(Board board, Move m)
    {
        float eval = 0;
        float myPieceVal = _PieceVals[(int)m.MovePieceType];

        // Make the move to investigate its results
        board.MakeMove(m);
        // Return any checkmates immediately
        if (board.IsInCheckmate())
        {
            board.UndoMove(m);
            return 1000000;
        }

        // Only test this move if it's not going to cause a draw.
        if (!board.IsDraw())
        {
            // Look for checks only if move is late enough
            if (board.PlyCount > 10 && board.IsInCheck()) eval += 1;

            // Try captures and attacks as an exclusive if...else pair
            if (m.IsCapture) eval += _PieceVals[(int)m.CapturePieceType];
            else if (IsAttack(board, m)) eval += 0.1f;

            if (m.IsPromotion) eval += _PieceVals[(int)m.PromotionPieceType];

            // TODO: This pawn thing is suspicious. I think it forces lots of unguarded pawn advances in the end game
            if (m.MovePieceType == PieceType.Pawn) eval += 0.03f * board.PlyCount;
            if (m.MovePieceType == PieceType.King)
            {
                // penalize king moves that aren't castles when castling is still an option
                if (!m.IsCastles && (board.HasKingsideCastleRight(board.IsWhiteToMove) || board.HasQueensideCastleRight(board.IsWhiteToMove))) eval -= 2;
                // If we're deep enough in the end-game, have the king try to chase down enemy pieces
                if (board.PlyCount > 70) eval += IsMovingTowardEnemyPiece(board, m) ? 1 : -1;
            }

            var attackerOfTargetSquareValue = GetLowestPieceValueAttackingSquare(board, m.TargetSquare);
            if (attackerOfTargetSquareValue > 0) eval -= myPieceVal;
        }

        // Finish by undoing the move, otherwise we lose
        board.UndoMove(m);
        return eval;
    }

    bool IsGameOver(Board board, Move m)
    {
        board.MakeMove(m);
        bool isOver = board.IsDraw() || board.IsInCheckmate();
        board.UndoMove(m);
        return isOver;
    }

    void ScoreMoves(Board board, List<MichaelMove> moves, int depth)
    {
        foreach (var m in moves)
        {
            // Score each candidate move
            m._Score += ScoreMove(board, m._Move);
            //for (int i = 0; i < depth; i++) DivertedConsole.Write("\t");
            //DivertedConsole.Write(m._Move.ToString() + ", " + m._Score);
        }
        // Order moves so best is first
        moves.Sort(MoveSort);

        // Skip eval if move would cause game over
        if (IsGameOver(board, moves[0]._Move)) return;

        // Recurse
        if (depth > 0)
        {
            var scoredMoves = new List<MichaelMove>();

            for (int i = 0; i < moves.Count; i++)
            {
                if (i >= moves.Count) break;

                // Make my move
                board.MakeMove(moves[i]._Move);
                // Get list of opponent's moves
                scoredMoves.Clear();
                foreach (var m in board.GetLegalMoves())
                    scoredMoves.Add(new MichaelMove(m, 0));
                if (scoredMoves.Count == 0) { board.UndoMove(moves[i]._Move); continue; }
                // Score opponent's moves
                ScoreMoves(board, scoredMoves, depth - 1);
                // Get best opponent's move
                scoredMoves.Sort(MoveSort);
                // Subtract opponent's best response to my move's score
                moves[i]._Score -= scoredMoves[0]._Score;
                // Undo my move
                board.UndoMove(moves[i]._Move);
            }
        }
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        // Push all moves onto a new list
        var scoredMoves = new List<MichaelMove>();
        foreach (var m in moves)
            scoredMoves.Add(new MichaelMove(m, 0));

        int extraMoves = Math.Max(0, 2 - (scoredMoves.Count / 20));
        ScoreMoves(board, scoredMoves, 1 + extraMoves);

        scoredMoves.Sort(MoveSort);

        DivertedConsole.Write("MoveScore:" + scoredMoves[0]._Score.ToString());
        _LastMovedPiece = scoredMoves[0]._Move.MovePieceType;
        return scoredMoves[0]._Move;
    }
}