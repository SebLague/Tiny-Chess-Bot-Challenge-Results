namespace auto_Bot_185;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_185 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int EvaluateBoard(Board board)
    {
        if (board.IsInCheckmate())
        {
            return -1000000;
        }

        // Stalemate check
        if (board.GetLegalMoves().Length == 0)
        {
            return -100000;
        }

        int moveValue = 0;

        moveValue += board.GetPieceList(PieceType.King, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.King];
        moveValue += board.GetPieceList(PieceType.Queen, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Queen];
        moveValue += board.GetPieceList(PieceType.Rook, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Rook];
        moveValue += board.GetPieceList(PieceType.Bishop, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Bishop];
        moveValue += board.GetPieceList(PieceType.Knight, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Knight];
        moveValue += board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Pawn];

        moveValue -= board.GetPieceList(PieceType.King, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.King];
        moveValue -= board.GetPieceList(PieceType.Queen, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Queen];
        moveValue -= board.GetPieceList(PieceType.Rook, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Rook];
        moveValue -= board.GetPieceList(PieceType.Bishop, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Bishop];
        moveValue -= board.GetPieceList(PieceType.Knight, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Knight];
        moveValue -= board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove).Count * pieceValues[(int)PieceType.Pawn];

        if (board.IsInCheck())
        {
            moveValue -= 40;
        }

        // Count all enemy pieces
        int numEnemyPieces = 0;
        board.GetAllPieceLists()
            .Where(x => x.IsWhitePieceList == !board.IsWhiteToMove)
            .ToList().ForEach(x => { numEnemyPieces += x.Count; });

        // How far is their king from the center? Endgame only
        if (numEnemyPieces < 10)
        {
            int kingDistance = 0;
            Square kingSquare = board.GetKingSquare(!board.IsWhiteToMove);
            kingDistance += Math.Abs(kingSquare.File - 3);
            kingDistance += Math.Abs(kingSquare.Rank - 3);
            moveValue += kingDistance * 10;
        }

        // how many pieces can be captured from this position?
        int captureValue = 0;
        Move[] possibleCaptures = board.GetLegalMoves(true);
        foreach (Move move in possibleCaptures)
        {
            captureValue += (int)(0.2 * pieceValues[(int)move.CapturePieceType]);
        }
        moveValue -= captureValue;

        // How protected is our king?
        int kingProtection = 0;
        foreach (PieceList list in board.GetAllPieceLists())
        {
            if (list.IsWhitePieceList)
            {
                foreach (Piece piece in list)
                {
                    Square kingSquare = board.GetKingSquare(!board.IsWhiteToMove);
                    bool isAdjacentToKing = Math.Abs(piece.Square.File - kingSquare.File) <= 1 && Math.Abs(piece.Square.Rank - kingSquare.Rank) <= 1;
                    bool isntKing = piece.PieceType != PieceType.King;
                    if (isAdjacentToKing && isntKing)
                    {
                        kingProtection++;
                    }
                };
            }
        };

        // If it's endgame, it's bad if I have less legal moves
        if (numEnemyPieces < 10)
        {
            int numLegalMoves = board.GetLegalMoves().Where(x => x.MovePieceType == PieceType.King).Count();
            moveValue -= numLegalMoves;
        }

        moveValue += kingProtection * 10;

        return moveValue;
    }

    // how do you pronounce lague
    int MiniMaxSearch(Board board, int depth, bool myTurn, int alpha, int beta, out Move? outMove)
    {
        outMove = null;

        // Simple deep search
        if (depth == 0)
        {
            int mult = myTurn ? 1 : -1;
            return mult * EvaluateBoard(board);
        }

        int idealOpponentValue = myTurn ? int.MinValue : int.MaxValue;
        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves.OrderByDescending(x => pieceValues[(int)x.CapturePieceType]))
        {
            board.MakeMove(move);
            int enemyMoveValueThisTurn = MiniMaxSearch(board, depth - 1, !myTurn, alpha, beta, out Move? _);
            board.UndoMove(move);

            if (myTurn && enemyMoveValueThisTurn > idealOpponentValue)
            {
                idealOpponentValue = enemyMoveValueThisTurn;
                outMove = move;
                alpha = Math.Max(alpha, idealOpponentValue);
            }

            if (!myTurn && enemyMoveValueThisTurn < idealOpponentValue)
            {
                idealOpponentValue = enemyMoveValueThisTurn;
                outMove = move;
                beta = Math.Min(beta, idealOpponentValue);
            }

            if (beta <= alpha) break;
        }

        return idealOpponentValue;
    }


    public Move Think(Board board, Timer timer)
    {
        Move? outMove;
        MiniMaxSearch(board, 5, true, int.MinValue, int.MaxValue, out outMove);

        int currentBoardValue = EvaluateBoard(board);
        board.MakeMove(outMove.Value);
        int newBoardValue = -EvaluateBoard(board);
        board.UndoMove(outMove.Value);
        DivertedConsole.Write($"Board value: {currentBoardValue} -> {newBoardValue}");

        return outMove.Value;
    }
}