namespace auto_Bot_249;
using ChessChallenge.API;
using System;

public class Bot_249 : IChessBot
{
    private static readonly int[] PieceVals = { 0, 10, 30, 35, 50, 90, 900 };

    private static readonly int[] PiecePositionalVals =
    {
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -2, -1, -2, -2, -1, -2, -4,
        -4, -2, -1,  0,  0, -1, -2, -4,
        -4, -2, -1,  0,  0, -1, -2, -4,
        -4, -2, -1, -2, -2, -1, -2, -4,
        -4, -4, -4, -3, -3, -4, -4, -4,
        -4, -4, -4, -3, -3, -4, -4, -4
    };

    private class MoveNode
    {
        public Move Move;
        public MoveNode ParentMoveNode;
        public MoveNode[] ChildMoves;
        public int HeuristicVal;
        public int Depth;

        public MoveNode() { }
        public MoveNode(MoveNode[] childMoves, MoveNode parentMoveNode, Move move, int heuristicVal, int depth)
        {
            HeuristicVal = heuristicVal;
            Move = move;
            ChildMoves = childMoves;
            ParentMoveNode = parentMoveNode;
            Depth = depth;
        }
    }

    private Board localBoard;
    private bool localIsWhite;

    private const int MaxVal = 9999999;
    private const int MinVal = -9999999;

    public Move Think(Board board, Timer timer)
    {
        localBoard = board;
        localIsWhite = localBoard.IsWhiteToMove;

        return CalculateNdepthMoves(5);
    }

    private Move CalculateNdepthMoves(int depth)
    {
        MoveNode rootMove = new MoveNode(null, new MoveNode(), new Move(), 0, 0);

        MoveNode resMoveNode = MiniMax(rootMove, depth, MinVal, MaxVal, true);

        while (resMoveNode.Depth != 1)
        {
            resMoveNode = resMoveNode.ParentMoveNode;
        }

        return resMoveNode.Move;
    }

    private void GenerateChildrenMoveNodes(MoveNode parentMoveNode)
    {
        Move[] captureMoves = localBoard.GetLegalMoves(true);
        Move[] legalMoves = localBoard.GetLegalMoves();

        parentMoveNode.ChildMoves = new MoveNode[legalMoves.Length];

        int lastIndex = 0;

        for (int i = 0; i < captureMoves.Length; i++)
        {
            MoveNode childNode = new MoveNode(null, parentMoveNode, captureMoves[i],
                parentMoveNode.ParentMoveNode.HeuristicVal, parentMoveNode.Depth + 1);

            childNode.HeuristicVal += CalculatePreHeuristicValue(childNode);

            parentMoveNode.ChildMoves[i] = childNode;
            lastIndex = i + 1;
        }

        for (int i = 0; i < legalMoves.Length; i++)
        {
            bool sameMoveIncluded = false;

            for (int j = 0; j < captureMoves.Length; j++)
            {
                if (captureMoves[j].Equals(legalMoves[i]))
                {
                    sameMoveIncluded = true;
                    break;
                }
            }

            if (!sameMoveIncluded)
            {
                MoveNode childNode = new MoveNode(null, parentMoveNode, legalMoves[i],
                    parentMoveNode.ParentMoveNode.HeuristicVal, parentMoveNode.Depth + 1);

                childNode.HeuristicVal += CalculatePreHeuristicValue(childNode);

                parentMoveNode.ChildMoves[lastIndex++] = childNode;
            }
        }
    }

    private bool IsWhitePlayingByDepth(int depth)
    {
        return localIsWhite ? depth % 2 == 1 : depth % 2 == 0;
    }

    private int CalculatePreHeuristicValue(MoveNode moveNode)
    {
        int preHeuristicVal = 0;

        if (moveNode.Move.IsPromotion)
        {
            preHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite ? 60 : -60;
        }

        if (moveNode.Move.IsCastles)
        {
            preHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite ? 15 : -15;
        }

        return preHeuristicVal;
    }

    private int CalculatePositionValue(MoveNode moveNode, out bool isCheck)
    {
        int totalHeuristicVal = moveNode.HeuristicVal;

        isCheck = false;

        if (localBoard.IsInCheckmate())
        {
            if (IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite)
            {
                return MaxVal - 10;
            }

            return MinVal + 10;
        }

        if (localBoard.IsInCheck() && (IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite))
        {
            isCheck = true;
            totalHeuristicVal += 3;
        }

        PieceList[] piecesList = localBoard.GetAllPieceLists();

        foreach (var pieceList in piecesList)
        {
            Piece firstPiece = pieceList.GetPiece(0);

            if (firstPiece.PieceType == PieceType.Pawn)
            {
                foreach (var pawn in pieceList)
                {
                    if (IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite)
                    {
                        totalHeuristicVal += pawn.IsWhite ? pawn.Square.Index / 8 : 8 - pawn.Square.Index / 8;
                    }
                }
            }

            totalHeuristicVal += firstPiece.IsWhite == localIsWhite ? PieceVals[(int)firstPiece.PieceType] * pieceList.Count :
                -PieceVals[(int)firstPiece.PieceType] * pieceList.Count;
        }

        ulong pieceBitboard = localIsWhite ? localBoard.WhitePiecesBitboard : localBoard.BlackPiecesBitboard;

        for (int i = 63; i >= 0; i--)
        {
            if (((pieceBitboard >> i) & 1) == 1)
            {
                totalHeuristicVal += IsWhitePlayingByDepth(moveNode.Depth) == localIsWhite ? PiecePositionalVals[i] : -PiecePositionalVals[i];
            }
        }

        return totalHeuristicVal;
    }

    private MoveNode MiniMax(MoveNode moveNode, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        bool isCheckMate = localBoard.IsInCheckmate();
        bool isDraw = localBoard.IsDraw();

        if (depth == 0 || isCheckMate || isDraw)
        {
            moveNode.HeuristicVal = CalculatePositionValue(moveNode, out bool isCheck);

            if (isCheckMate)
            {
                moveNode.HeuristicVal += depth;
                return moveNode;
            }

            if (isDraw)
            {
                return moveNode;
            }

            if (!isCheck)
            {
                return moveNode;
            }

            depth = 1;
        }

        GenerateChildrenMoveNodes(moveNode);

        MoveNode localMoveNode = new MoveNode();

        if (maximizingPlayer)
        {
            localMoveNode.HeuristicVal = MinVal;

            foreach (var childMove in moveNode.ChildMoves)
            {
                localBoard.MakeMove(childMove.Move);

                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, false);

                localBoard.UndoMove(childMove.Move);

                if (localMoveNode.HeuristicVal < resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }

                alpha = Math.Max(alpha, localMoveNode.HeuristicVal);

                if (localMoveNode.HeuristicVal >= beta)
                {
                    break;
                }
            }
        }
        else
        {
            localMoveNode.HeuristicVal = MaxVal;

            foreach (var childMove in moveNode.ChildMoves)
            {
                localBoard.MakeMove(childMove.Move);

                MoveNode resMoveNode = MiniMax(childMove, depth - 1, alpha, beta, true);

                localBoard.UndoMove(childMove.Move);

                if (localMoveNode.HeuristicVal > resMoveNode.HeuristicVal)
                {
                    localMoveNode = resMoveNode;
                }

                beta = Math.Min(beta, localMoveNode.HeuristicVal);

                if (localMoveNode.HeuristicVal <= alpha)
                {
                    break;
                }
            }
        }

        return localMoveNode;
    }
}
