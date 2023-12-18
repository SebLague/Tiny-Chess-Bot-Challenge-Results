namespace auto_Bot_41;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_41 : IChessBot
{
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    List<Move> history = new();
    bool botColor;

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = new();
        int bestScore = int.MinValue;

        botColor = board.IsWhiteToMove;

        Move useMove = CalculateBestMove(board, botColor, 0, bestMove, bestScore, new MaterialAdvantage());

        history.Add(useMove);

        return useMove;
    }

    struct MaterialAdvantage
    {
        public int score;
        public int pieces;
        public int opponentPieces;
    }

    // Function to calculate the best move using recursive minimax with limited depth
    Move CalculateBestMove(Board board, bool currentPlayerColor, int depth, Move bestMove, int bestScore, MaterialAdvantage materialAdvantage)
    {
        Move[] moves = board.GetLegalMoves();

        int maxDepth = 5;

        //Change search depth depending on complexity
        if (moves.Length >= 10)
            maxDepth = 4;
        if (moves.Length >= 20)
            maxDepth = 3;
        if (moves.Length >= 30)
            maxDepth = 2;

        if (depth <= maxDepth)
        {
            if (moves != null && moves.Length > 0)
            {
                GetMaterialAdvantage(board, botColor, materialAdvantage);

                // Iterate through all legal moves and evaluate their resulting positions recursively
                foreach (Move move in moves)
                {
                    Piece capturedPiece = board.GetPiece(move.TargetSquare);
                    int capturedPieceValue = pieceValues[(int)capturedPiece.PieceType];

                    //Favour knights
                    if (board.PlyCount < 4 && move.MovePieceType == PieceType.Knight)
                    {
                        capturedPieceValue += 50;
                    }
                    //Favour bishops
                    else if (board.PlyCount < 8 && move.MovePieceType == PieceType.Bishop)
                    {
                        capturedPieceValue += 50;
                    }
                    //Favour rooks
                    else if (board.PlyCount < 12 && move.MovePieceType == PieceType.Rook)
                    {
                        capturedPieceValue += 50;
                    }
                    //Favour promotions, but only Queens
                    if (move.IsPromotion && move.PromotionPieceType == PieceType.Queen)
                        capturedPieceValue += 750;
                    //Favour castling
                    if (move.IsCastles)
                        capturedPieceValue += 100;
                    //Reduce likelihood of moving king a lot
                    if (move.MovePieceType == PieceType.King)
                        capturedPieceValue -= 25;
                    //If we have the advantage, favour trades
                    if (capturedPiece.PieceType != PieceType.None && move.MovePieceType != PieceType.King && materialAdvantage.score > 800)
                        capturedPieceValue += 25;
                    //With a big advantage, box the king
                    if (materialAdvantage.opponentPieces < 4)
                    {
                        capturedPieceValue += DistanceToKingScore(board, move, botColor);
                    }
                    //With no clear move, favour pawns
                    if (move.MovePieceType == PieceType.Pawn)
                        capturedPieceValue += 1;

                    // Make the move on a copy of the board
                    board.MakeMove(move);

                    //Favour checks
                    if (board.IsInCheck())
                        capturedPieceValue += 25;

                    //Favour checkmates
                    if (board.IsInCheckmate())
                        capturedPieceValue += 10000;

                    // Recursively calculate the opponent's best move (increment depth)
                    Move opponentBestMove = CalculateBestMove(board, !currentPlayerColor, depth + 1, bestMove, int.MinValue, materialAdvantage);
                    Piece capturedPieceOpponent = board.GetPiece(opponentBestMove.TargetSquare);
                    int capturedPieceValueOpponent = pieceValues[(int)capturedPieceOpponent.PieceType];

                    board.UndoMove(move);

                    if (capturedPieceValue - capturedPieceValueOpponent > bestScore)
                    {
                        bestScore = capturedPieceValue - capturedPieceValueOpponent;
                        bestMove = move;
                    }
                }
            }
        }

        Random rng = new();
        Move makeMove = bestMove;

        if (history.Contains(makeMove) && moves.Length > 0 && !makeMove.IsNull)
        {
            makeMove = moves[rng.Next(moves.Length)];
        }

        return makeMove;
    }

    int DistanceToKingScore(Board board, Move move, bool botColor)
    {
        Square kingSquare = board.GetKingSquare(!botColor);

        return (Math.Abs(move.StartSquare.File - kingSquare.File) + Math.Abs(move.StartSquare.Rank - kingSquare.Rank)) - (Math.Abs(move.TargetSquare.File - kingSquare.File) + Math.Abs(move.TargetSquare.Rank - kingSquare.Rank));
    }

    void GetMaterialAdvantage(Board board, bool botColor, MaterialAdvantage materialAdvantage)
    {
        int botScore = 0, opponentScore = 0;
        int totalBotPieces = 0, totalOpponentPieces = 0;

        PieceList[] allPieces = board.GetAllPieceLists();

        foreach (PieceList pieceList in allPieces)
        {
            if (pieceList.TypeOfPieceInList != PieceType.King)
            {
                if (pieceList.IsWhitePieceList == botColor)
                {
                    botScore += pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count;
                    totalBotPieces++;
                }
                else
                {
                    opponentScore += pieceValues[(int)pieceList.TypeOfPieceInList] * pieceList.Count;
                    totalOpponentPieces++;
                }
            }
        }

        materialAdvantage.pieces = totalBotPieces;
        materialAdvantage.opponentPieces = totalOpponentPieces;

        //Calculate advantage, but we don't want to favour trades if we have too few pieces
        materialAdvantage.score = totalBotPieces > 4 && totalOpponentPieces > 3 ? botScore - opponentScore : -1;
    }
}