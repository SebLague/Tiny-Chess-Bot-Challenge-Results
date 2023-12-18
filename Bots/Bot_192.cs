namespace auto_Bot_192;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_192 : IChessBot
{
    public static readonly int FutureThinkTurns = 1;
    private static readonly int _maxDepth = FutureThinkTurns * 2 + 1;

    private static readonly float[] _pieceValues = { 0f, 1f, 3f, 3f, 5f, 9f, 100f };
    private const float _checkmateValue = float.MaxValue;
    private const float _drawValue = 0f;

    public Move Think(Board board, Timer timer) => GetBestMove(board);

    Move GetBestMove(Board board)
    {
        ScoredMoveList scoredMoves = EvaluateBoardMoves(board, 0, float.MinValue, float.MaxValue);

        return board.IsWhiteToMove ? scoredMoves.MaxMove : scoredMoves.MinMove;
    }

    ScoredMoveList EvaluateBoardMoves(Board board, int depth, float alpha, float beta)
    {
        ScoredMoveList scoredMoves = new();

        IEnumerable<Move> orderedLegalMoves = depth < _maxDepth ? board.GetLegalMoves().OrderByDescending(m => MoveScoreGuess(board, m)) : board.GetLegalMoves();
        foreach (Move legalMove in orderedLegalMoves)
        {
            board.MakeMove(legalMove);
            float moveScore = EvaluateBoardRecursive(board, depth, alpha, beta);
            board.UndoMove(legalMove);

            scoredMoves.Add(legalMove, moveScore);

            if (board.IsWhiteToMove && moveScore > alpha) alpha = moveScore;
            if (!board.IsWhiteToMove && moveScore < beta) beta = moveScore;

            if (!board.IsWhiteToMove && moveScore < alpha || board.IsWhiteToMove && moveScore > beta) break;
        }

        return scoredMoves;
    }

    float MoveScoreGuess(Board board, Move move)
    {
        float scoreGuess = 0f;
        float movingPieceValue = _pieceValues[(int)move.MovePieceType];
        float targetPieceValue = _pieceValues[(int)move.CapturePieceType];

        scoreGuess += targetPieceValue;
        if (board.SquareIsAttackedByOpponent(move.TargetSquare)) scoreGuess -= movingPieceValue;
        else if (board.SquareIsAttackedByOpponent(move.StartSquare)) scoreGuess += movingPieceValue;

        return scoreGuess;
    }

    float EvaluateBoardRecursive(Board board, int depth, float alpha, float beta)
    {
        if (depth == _maxDepth) return EvaluateMaterial(board);

        if (board.IsInCheckmate()) return board.IsWhiteToMove ? -_checkmateValue : _checkmateValue;
        if (board.IsDraw()) return _drawValue;

        ScoredMoveList scoredMoves = EvaluateBoardMoves(board, ++depth, alpha, beta);
        return board.IsWhiteToMove ? scoredMoves.Max : scoredMoves.Min;
    }

    float EvaluateMaterial(Board board)
    {
        float material = 0f;

        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            float piecesVal = _pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count;

            material += pieceList.IsWhitePieceList ? piecesVal : -piecesVal;
        }

        return material;
    }
}

public struct ScoredMoveList
{
    private static readonly Random _random = new();

    private readonly List<Move> _maxMoves;
    private readonly List<Move> _minMoves;
    public Move MaxMove => _maxMoves[_random.Next(_maxMoves.Count)];
    public Move MinMove => _minMoves[_random.Next(_minMoves.Count)];
    public float Min { get; private set; }
    public float Max { get; private set; }

    public ScoredMoveList()
    {
        Min = float.MaxValue;
        Max = float.MinValue;
        _maxMoves = new();
        _minMoves = new();
    }

    public void Add(Move move, float moveScore)
    {
        if (moveScore > Max)
        {
            Max = moveScore;
            _maxMoves.Clear();
            _maxMoves.Add(move);
        }
        else if (moveScore == Max) _maxMoves.Add(move);

        if (moveScore < Min)
        {
            Min = moveScore;
            _minMoves.Clear();
            _minMoves.Add(move);
        }
        else if (moveScore == Min) _minMoves.Add(move);
    }
}