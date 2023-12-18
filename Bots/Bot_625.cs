namespace auto_Bot_625;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_625 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 320, 500, 900, 20000 };
    bool searchCancelled;


    Dictionary<PieceType, ulong> pieceSquareTables = new() {
        {PieceType.Pawn, 0xFECA8650102FF201},
        {PieceType.Knight, 0x51DDF0005DDEE20},
        {PieceType.Bishop, 0x5AAAA5005555550},
        {PieceType.Rook, 0x800000002488420},
        {PieceType.Queen, 0x244442002444420}
    };

    public Move Think(Board board, Timer timer)
    {
        var moves = GetLegalMovesOrdered(board);

        switch (board.PlyCount)
        {
            case 0: case 1: return moves[16];
            case 2: case 3: return moves[0];
        }

        int bestMoveIndex = 0;
        searchCancelled = false;

        for (int i = 1; i < 256; i++)
        {
            int bestEvalIteration = -9999;

            for (int j = 0; j < moves.Length; j++)
            {
                Move move = moves[j];
                board.MakeMove(move);
                int evaluation = -Search(board, timer, i, -9999, 9999);
                board.UndoMove(move);

                if (evaluation == 9999)
                    return move;

                if (searchCancelled)
                    break;

                if (evaluation > bestEvalIteration)
                {
                    bestEvalIteration = evaluation;
                    bestMoveIndex = j;
                }
            }

            if (searchCancelled)
            {
                break;
            }

            // Put best move from iteration first
            if (bestMoveIndex != 0)
            {
                Move bestMove = moves[bestMoveIndex];

                for (int j = bestMoveIndex; j > 0; j--)
                    moves[j] = moves[j - 1];

                moves[0] = bestMove;
                bestMoveIndex = 0;
            }
        }
        return moves[bestMoveIndex];
    }


    int Search(Board board, Timer timer, int depth, int alpha, int beta)
    {

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return -9999;

        if (depth == 0)
            return SearchCaptures(board, alpha, beta);

        if (timer.MillisecondsElapsedThisTurn >= 1000 || timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining * 0.05)
        {
            searchCancelled = true;
            return 0;
        }

        var moves = GetLegalMovesOrdered(board);

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int evaluation = -Search(board, timer, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta)
                return beta;

            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }

    int SearchCaptures(Board board, int alpha, int beta)
    {
        int evaluation = Evaluate(board);
        if (evaluation >= beta)
            return beta;

        alpha = Math.Max(alpha, evaluation);
        var captures = GetLegalMovesOrdered(board, true);

        foreach (var move in captures)
        {
            board.MakeMove(move);
            evaluation = -SearchCaptures(board, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta)
                return beta;

            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }


    Move[] GetLegalMovesOrdered(Board board, bool capturesOnly = false)
    {
        var moves = board.GetLegalMoves(capturesOnly);
        Dictionary<Move, int> moveScoreMap = new();

        ulong opponentPawnsBitboard = board.GetPieceBitboard(PieceType.Pawn, !board.IsWhiteToMove);
        ulong opponentPawnAttacksBitBoard = board.IsWhiteToMove
            ? (opponentPawnsBitboard >> 7 & 0xFEFEFEFEFEFEFEFE) | (opponentPawnsBitboard >> 9 & 0x7F7F7F7F7F7F7F7F)
            : (opponentPawnsBitboard << 9 & 0xFEFEFEFEFEFEFEFE) | (opponentPawnsBitboard << 7 & 0x7F7F7F7F7F7F7F7F);


        foreach (Move move in moves)
        {
            int moveScore = 0;

            PieceType movePiece = move.MovePieceType;
            PieceType capturePiece = move.CapturePieceType;

            // Prioritize capturing high value piece with low value piece
            if (move.IsCapture)
            {
                moveScore += 10 * pieceValues[(int)capturePiece] - pieceValues[(int)movePiece];
            }

            // Prioritize promoting pawn
            if (move.IsPromotion)
            {
                moveScore += pieceValues[(int)move.PromotionPieceType];
            }

            // Discourage high value piece moves to squares controlled by enemy pawns
            if (BitboardHelper.SquareIsSet(opponentPawnAttacksBitBoard, move.TargetSquare))
            {
                moveScore -= 10 * pieceValues[(int)movePiece];
            }

            moveScoreMap.Add(move, moveScore);
        }

        return moveScoreMap.OrderByDescending(KeyValuePair => KeyValuePair.Value)
            .Select(KeyValuePair => KeyValuePair.Key)
            .ToArray();
    }

    int Evaluate(Board board)
    {
        var allPieces = board.GetAllPieceLists();
        var whitePieces = allPieces.Take(6).ToArray();
        var blackPieces = allPieces.Skip(6).ToArray();

        int whiteMaterialCount = CountMaterial(whitePieces);
        int blackMaterialCount = CountMaterial(blackPieces);

        return (
            (whiteMaterialCount + EvaluatePiecePlacement(board, true) + EvaluateKingEndgame(board, whiteMaterialCount, blackMaterialCount, true))
                 -
            (blackMaterialCount + EvaluatePiecePlacement(board, false) + EvaluateKingEndgame(board, whiteMaterialCount, blackMaterialCount, false)))
                 *
            (board.IsWhiteToMove ? 1 : -1);
    }

    int EvaluatePiecePlacement(Board board, bool white)
    {
        int score = 0;

        foreach (var kv in pieceSquareTables)
        {
            ulong pieces = board.GetPieceBitboard(kv.Key, white);
            ulong squareTable = pieceSquareTables[kv.Key];

            while (pieces != 0)
            {
                int index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);
                int trueIndex = white ? index : 63 - index;
                int fileScore = (int)((0b1111UL << (trueIndex % 8 * 4) & squareTable) >> (trueIndex % 8 * 4));
                int rankScore = (int)((0b1111UL << 32 + (trueIndex / 8 * 4) & squareTable) >> 32 + (trueIndex / 8 * 4));

                score += fileScore + rankScore;
            }
        }

        return score;
    }

    int EvaluateKingEndgame(Board board, int myMaterialScore, int opponentMaterialScore, bool white)
    {
        int evaluation = 0;
        int opponentPieceCount = BitboardHelper.GetNumberOfSetBits(
            (white ? board.BlackPiecesBitboard : board.WhitePiecesBitboard)
            & ~board.GetPieceBitboard(PieceType.Pawn, !white));

        if (myMaterialScore > opponentMaterialScore + 200 && opponentPieceCount < 4)
        {
            ulong opponentKing = board.GetPieceBitboard(PieceType.King, !white);
            ulong friendlyKing = board.GetPieceBitboard(PieceType.King, white);

            // Calculate opponent kings distance to center
            evaluation += Math.Max(GetMSB(opponentKing) / 8 - 3, (GetMSB(opponentKing) % 8) - 3);

            // Find the positions (bits) of the two kings
            int position1 = GetMSB(opponentKing);
            int position2 = GetMSB(friendlyKing);

            // Calculate the distance between the kings using Manhattan distance
            evaluation += 7 - Math.Max(position2 / 8 - position1 / 8, position2 % 8 - position1 % 8);
        }

        return evaluation;
    }

    int GetMSB(ulong board)
    {
        return (int)Math.Log2(board);
    }

    int CountMaterial(PieceList[] pieces)
    {
        return pieces.Select((piece, index) => piece.Count * pieceValues[index + 1]).Sum();
    }
}
