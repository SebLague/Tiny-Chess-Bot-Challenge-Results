namespace auto_Bot_490;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_490 : IChessBot
{
    public Bot_490()
    {
        string letters = "abcdefgh";
        string numbers = "12345678";
        int count = 0;
        foreach (char letter in letters)
        {
            foreach (char num in numbers)
            {
                char[] chars = { letter, num };
                squares[count] = new Square(new string(chars));
                count++;
            }
        }
    }

    Node? rootNode;

    Board _board;
    Timer _timer;

    Square[] squares = new Square[64];

    bool isWhite;

    int averageGameTurnLength = 100;
    int desiredTurnLength = 0;

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;
        isWhite = board.IsWhiteToMove;
        desiredTurnLength = calculateDesiredTurnLength();
        rootNode = new Node();

        Move? bestMove = null;
        int depth = 1;
        while (!noTimeLeftToThink())
        {
            calculateScores(depth);
            depth++;
        }
        bestMove = getBestMove();
        // DivertedConsole.Write("choosing " + bestMove.ToString());

        return bestMove != null ? (Move)bestMove : board.GetLegalMoves()[0];
    }

    void calculateScores(int maxDepth)
    {
        if (rootNode == null)
        {
            return;
        }
        int score = minimax(maxDepth, (Node)rootNode, 0, isWhite);
        // DivertedConsole.Write("Depth: " + maxDepth.ToString() + " - Current evaluation: " + score);
    }

    int minimax(int maxDepth, Node node, int currentDepth, bool wantsMax)
    {
        if (currentDepth == maxDepth || noTimeLeftToThink())
        {
            if (!node.boardEvaluated)
            {
                node.boardEvaluationAfterMove = evaluatePosition(node.move);
                node.boardEvaluated = true;
            }
            return node.boardEvaluationAfterMove;
        }

        if (node.children == null || node.children.Length == 0)
        {
            Move[] moves = _board.GetLegalMoves();
            node.children = new Node[moves.Length];
            int count = 0;
            foreach (Move move in moves)
            {
                Node childNode = new Node();
                childNode.move = move;
                node.children.SetValue(childNode, count);
                count++;
            }
        }
        int bestScore = wantsMax ? int.MinValue : int.MaxValue;
        foreach (Node childNode in node.children)
        {
            bool isAttackedByOpponent = _board.SquareIsAttackedByOpponent(childNode.move.TargetSquare);
            _board.MakeMove(childNode.move);
            childNode.score = minimax(maxDepth, childNode, currentDepth + 1, !wantsMax);
            if (childNode.score == int.MaxValue)
            {
                childNode.score -= currentDepth;
            }
            if (childNode.score == int.MinValue)
            {
                childNode.score += currentDepth;
            }
            // if (wantsMax) {
            //     childNode.score = childNode.score - currentDepth;
            // } else {
            //     childNode.score = childNode.score + currentDepth;
            // }
            _board.UndoMove(childNode.move);
            if (wantsMax)
            {
                if (isAttackedByOpponent)
                {
                    childNode.score -= 2;
                }
                if (childNode.score > bestScore)
                {
                    // if (node == rootNode) {
                    // DivertedConsole.Write("Best move so far with score " + score.ToString() + ": " + childNode.move.ToString() + " wantsMax: " + wantsMax.ToString() + ", isWhite: " + isWhite.ToString());
                    // }
                    bestScore = childNode.score;
                }
            }
            else
            {
                if (isAttackedByOpponent)
                {
                    childNode.score += 2;
                }
                if (childNode.score < bestScore)
                {
                    // if (node == rootNode) {
                    // DivertedConsole.Write("Best move so far with score " + score.ToString() + ": " + childNode.move.ToString() + " wantsMax: " + wantsMax.ToString() + ", isWhite: " + isWhite.ToString());
                    // }
                    bestScore = childNode.score;
                }
            }
        }
        return bestScore;
    }

    int evaluatePosition(Move move)
    {
        bool whiteToMove = _board.IsWhiteToMove;
        // positive = good for white
        // negative = good for black
        if (_board.IsInCheckmate())
        {
            // white in checkmate ? super neg
            // black in checkmate ? super pos
            return whiteToMove ? int.MinValue : int.MaxValue;
        }
        if (_board.IsInStalemate() || _board.IsInsufficientMaterial() || _board.IsRepeatedPosition() || _board.IsDraw())
        {
            return 0;
        }
        int score = 0;
        if (_board.IsInCheck())
        {
            if (whiteToMove)
            {
                // white is in check, good for black
                score -= 1;
            }
            else
            {
                score += 1;
            }
        }
        else
        {
            if (move.MovePieceType == PieceType.King)
            {
                // black just moved king
                if (whiteToMove)
                {
                    score += 1;
                }
                else
                {
                    // white just moved king
                    score -= 1;
                }
            }
            else
            {
                // if white just moved, black is now current turn. White is the "opponent." Therefore, white has a defender at this spot?
                if (_board.SquareIsAttackedByOpponent(move.TargetSquare))
                {
                    if (whiteToMove)
                    {
                        // black just moved, so black is defending his own piece
                        score -= 1;
                    }
                    else
                    {
                        // white just moved, so white is defending his own piece
                        score += 1;
                    }
                }
            }
        }
        if (move.IsCapture)
        {
            if (whiteToMove)
            {
                // black just moved and captured
                score -= 1;
            }
            else
            {
                score += 1;
            }
        }
        foreach (Square square in squares)
        {
            Piece piece = _board.GetPiece(square);
            if (piece.PieceType != PieceType.None)
            {
                if (piece.IsWhite)
                {
                    score += pieceSquareWorth(piece, square);
                }
                else
                {
                    score -= pieceSquareWorth(piece, square);
                }
            }
        }
        return score;
    }

    Move? getBestMove()
    {
        int bestScore = isWhite ? int.MinValue : int.MaxValue;
        if (rootNode == null || rootNode.children == null)
        {
            return null;
        }
        foreach (Node child in rootNode.children)
        {
            if ((isWhite && child.score > bestScore) || (!isWhite && child.score < bestScore))
            {
                bestScore = child.score;
            }
        }
        List<Move> moves = new List<Move>();
        foreach (Node child in rootNode.children)
        {
            if (child.score == bestScore)
            {
                moves.Add(child.move);
            }
        }
        if (moves.Count == 0)
        {
            return null;
        }
        return moves[Random.Shared.Next(moves.Count)];
    }

    int calculateDesiredTurnLength()
    {
        return _timer.MillisecondsRemaining / Math.Max(averageGameTurnLength - (_board.PlyCount / 2), 5);
    }

    bool noTimeLeftToThink()
    {
        return _timer.MillisecondsElapsedThisTurn >= desiredTurnLength;
    }

    bool isMyMove()
    {
        return (_board.IsWhiteToMove && isWhite) || (!_board.IsWhiteToMove && !isWhite);
    }

    int pieceSquareWorth(Piece piece, Square square)
    {
        switch (piece.PieceType)
        {
            case PieceType.None:
                return 0;
            case PieceType.Pawn:
                //int pawnFileWorth = piece.IsWhite ? square.Rank : 8 - square.Rank;
                return 1;
            case PieceType.Knight:
                return 3;
            case PieceType.Bishop:
                return 3;
            case PieceType.Rook:
                return 5;
            case PieceType.Queen:
                return 8;
            case PieceType.King:
                return int.MaxValue - 10000;
        }
        return 0;
    }
}

class Node
{
    public Node[] children;
    public Move move;
    public int score;
    public int boardEvaluationAfterMove = 0;
    public bool boardEvaluated = false;
}