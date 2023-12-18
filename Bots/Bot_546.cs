namespace auto_Bot_546;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_546 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    private readonly int[] _pieceValues = { 0, 10, 30, 30, 50, 90, 100 };
    private bool _isWhiteToMove;
    private int _leftPieces;
    private Board _currentBoard;
    private Move _currentMove;
    private Move? _randomMove;
    private int _pieceValue;
    private int _capturePieceValue;
    private int _checkmateDepth;

    private readonly Dictionary<PieceType, Func<int>> _piecePositionScores;

    public Bot_546()
    {
        _piecePositionScores = new()
        {
            {
                PieceType.Pawn,
                () => 10
            },
            {
                PieceType.Knight,
                KnightPositionScore
            },
            {
                PieceType.Bishop,
                BishopPositionScore
            },
            {
                PieceType.Rook,
                RookPositionScore
            },
            {
                PieceType.Queen,
                QueenPositionScore
            },
            {
                PieceType.King,
                KingPositionScore
            }
        };
    }

    public Move Think(Board board, Timer timer)
    {
        _currentBoard = board;
        _isWhiteToMove = board.IsWhiteToMove;
        _leftPieces = board.GetAllPieceLists().Sum(list => list.Count);
        _checkmateDepth = _leftPieces > 16 ? 3 : _leftPieces > 8 ? 4 : 5;

        Move[] moves = board.GetLegalMoves();
        try
        {
            _randomMove = moves
                .Where(move => move.MovePieceType == PieceType.Pawn)
                .MinBy(move => _isWhiteToMove ? move.TargetSquare.Rank : 7 - move.TargetSquare.Rank);
        }
        catch (Exception)
        {
            _randomMove = null;
        }

        Move bestMove = default;
        int bestMoveScore = int.MinValue;
        foreach (Move move in moves)
        {
            _currentMove = move;
            _pieceValue = _pieceValues[(int)_currentMove.MovePieceType];
            _capturePieceValue = _pieceValues[(int)_currentMove.CapturePieceType];

            int checkmateValue = IsCheckmate(_currentMove, _checkmateDepth);
            if (checkmateValue == 100) return move;

            int currentMoveScore = MoveScore() + checkmateValue;

            if (bestMoveScore < currentMoveScore)
            {
                bestMove = move;
                bestMoveScore = currentMoveScore;
            }
        }

        // DivertedConsole.Write($"Found {bestMoves.Count} moves with score {bestMoveScore}");
        // DivertedConsole.Write($"Selected move {selectedMove}");
        // _currentMove = selectedMove;
        // _pieceValue = _pieceValues[(int)_currentMove.MovePieceType];
        // _capturePieceValue = _pieceValues[(int)_currentMove.CapturePieceType];
        // DisplayMoveScore();

        return bestMove;
    }

    private int MoveScore()
    {
        int score = 0;

        if (_currentMove.IsCapture) score += _capturePieceValue;
        if (_currentMove.IsCastles) score += 20;
        if (_currentMove.PromotionPieceType == PieceType.Queen) score += 50;
        if (Escaped()) score += _pieceValue;

        if (WillBeCapturable()) score -= _pieceValue;
        score += KingDefenseScore();

        score += _piecePositionScores[_currentMove.MovePieceType]();

        return score;
    }

    private bool Escaped()
    {
        if (_randomMove == null) return false;

        _currentBoard.MakeMove(_randomMove.Value);
        bool isEscaped = _currentBoard.GetLegalMoves(true)
            .Any(move => move.TargetSquare == _currentMove.StartSquare);
        _currentBoard.UndoMove(_randomMove.Value);
        return isEscaped;
    }

    // private void DisplayMoveScore()
    // {
    //     if (_currentMove.IsCapture) DivertedConsole.Write($"Capture: {_capturePieceValue}");
    //     if (_currentMove.IsCastles) DivertedConsole.Write("Castle: 40");
    //     if (_currentMove.PromotionPieceType == PieceType.Queen) DivertedConsole.Write("Promotion: 50");
    //     if (Escaped()) DivertedConsole.Write($"Escape: {_pieceValue}");
    //
    //     DivertedConsole.Write($"Defense: {DefenseScore()}");
    //     DivertedConsole.Write($"King Defense: {KingDefenseScore()}");
    //     DivertedConsole.Write($"Queen Defense: {QueenDefenseScore()}");
    //
    //     DivertedConsole.Write(
    //         $"{_currentMove.MovePieceType} Position: {_piecePositionScores[_currentMove.MovePieceType]()}");
    // }

    private int IsCheckmate(Move move, int depth)
    {
        if (SimpleCheckmateTest(move)) return 1000;
        if (depth == 0) return 0;

        _currentBoard.MakeMove(move);

        IEnumerable<Move> legalMoves = _currentBoard.GetLegalMoves();
        int bestScore = int.MinValue;
        foreach (Move nextMove in legalMoves)
        {
            int opponentCheckmateScore = -IsCheckmate(nextMove, depth - 1);
            if (opponentCheckmateScore > bestScore) continue;

            bestScore = opponentCheckmateScore;
        }

        _currentBoard.UndoMove(move);

        return bestScore;
    }

    private bool SimpleCheckmateTest(Move move)
    {
        _currentBoard.MakeMove(move);
        bool checkmate = _currentBoard.IsInCheckmate();
        _currentBoard.UndoMove(move);
        return checkmate;
    }

    private bool WillBeCapturable()
    {
        _currentBoard.MakeMove(_currentMove);

        Move[] nextLegalCaptures = _currentBoard.GetLegalMoves(true);

        bool canBeCaptured = nextLegalCaptures.Any(nextMove => nextMove.TargetSquare == _currentMove.TargetSquare);

        _currentBoard.UndoMove(_currentMove);

        return canBeCaptured;
    }

    /// <summary>
    /// Score that tries to approximate how dangerous the king position is
    /// </summary>
    /// <returns></returns>
    private int KingDefenseScore()
    {
        Square kingSquare = _currentBoard.GetKingSquare(_isWhiteToMove);
        int kingRank = kingSquare.Rank, kingFile = kingSquare.File;

        int defenseScore = 0;

        for (int i = 0; i < 8; i++)
        {
            defenseScore += CheckDiagonal(kingFile + i, kingRank + i);
            defenseScore += CheckDiagonal(kingFile + i, kingRank - i);
            defenseScore += CheckDiagonal(kingFile - i, kingRank - i);
            defenseScore += CheckDiagonal(kingFile - i, kingRank + i);

            int CheckDiagonal(int file, int rank)
            {
                if (file < 0 || file >= 8 || rank < 0 || rank >= 8) return 0;

                Piece piece = _currentBoard.GetPiece(new Square(file, rank));

                if (piece.IsWhite == _isWhiteToMove) return 10;

                PieceType pieceType = piece.PieceType;

                return pieceType == PieceType.Bishop || pieceType == PieceType.Queen
                    ? -_pieceValues[(int)pieceType]
                    : 0;
            }
        }

        return defenseScore;
    }


    /// <summary>
    /// Score for the knight play style: they try to move towards the center of the board
    /// </summary>
    /// <returns>Value between -3 and 15</returns>
    private int KnightPositionScore()
    {
        int[] distanceScores = { 7, 10, 3, 0 };

        int KnightScore(Square square)
        {
            int distanceIndex = (int)Math.Max(Math.Abs(3.5f - square.Rank), Math.Abs(3.5f - square.File));

            return distanceScores[distanceIndex];
        }

        return Adjust(KnightScore(_currentMove.TargetSquare) - KnightScore(_currentMove.StartSquare));
    }

    private int BishopPositionScore()
    {
        int BishopScore(Square square)
        {
            int mainDiagonalRawDistance = Math.Abs(square.File - square.Rank);
            int otherDiagonalRawDistance = Math.Abs(square.File - 7 + square.Rank);

            int rawDiagonalSum = Math.Min(mainDiagonalRawDistance, otherDiagonalRawDistance);

            int rawScore = 3 - rawDiagonalSum;

            return rawScore * 10;
        }

        return Adjust(BishopScore(_currentMove.TargetSquare) - BishopScore(_currentMove.StartSquare));
    }

    /// <summary>
    /// Score for the knight play style: either defend on the first rank or attack on the second to last rank
    /// </summary>
    /// <returns>Value between -5 and 25</returns>
    private int RookPositionScore()
    {
        int[] rankScores = { 10, 0, 0, 0, 0, 0, 25, 15 };

        return Adjust(rankScores[RealRank(_currentMove.TargetSquare.Rank)] -
                      rankScores[RealRank(_currentMove.StartSquare.Rank)]);
    }

    /// <summary>
    /// Score for the queen play style: they try to move towards the center of the board
    /// </summary>
    /// <returns>Value between -6 and 30</returns>
    private int QueenPositionScore()
    {
        int QueenScore(Square square)
        {
            int distance = (int)(Math.Abs(3.5f - square.File) + Math.Abs(3.5f - square.Rank));

            return (7 - distance) * 10;
        }

        int score = QueenScore(_currentMove.TargetSquare) - QueenScore(_currentMove.StartSquare);

        return Adjust(score);
    }

    private int KingPositionScore()
    {
        int KingScore(Square square)
        {
            int rank = 7 - RealRank(square.Rank); // between 0 and 7

            int fileCenterDistance = (int)Math.Abs(3.5f - square.File);

            int rawScore = rank + fileCenterDistance; // between 0 and 10
            return rawScore * 3;
        }

        int score = KingScore(_currentMove.TargetSquare) - KingScore(_currentMove.StartSquare);

        return Adjust(_leftPieces < 8 ? -score : score);
    }

    private int Adjust(int score) => score < 0 ? score / 5 : score;

    private int RealRank(int rank) => _isWhiteToMove ? rank : 7 - rank;
}