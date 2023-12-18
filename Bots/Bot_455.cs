namespace auto_Bot_455;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_455 : IChessBot
{
    public Move Think(Board theBoard, Timer timer)
    {
        board = theBoard;
        Move bestMove = Move.NullMove;
        Move[] allMoves = board.GetLegalMoves();
        Array.Sort(allMoves, (a, b) => MoveValue(b).CompareTo(MoveValue(a)));
        bool shouldEndSearch = false;
        for (int depth = 1; ; depth++)
        {
            if (allMoves.Length == 1) return allMoves[0];
            int bestMoveEval = -1000000001;
            System.Collections.Generic.Dictionary<Move, int> evals = new();
            foreach (Move move in allMoves)
            {
                int eval = Search(move, depth, -1000000000, 1000000000);
                evals[move] = eval;
                if (eval > bestMoveEval)
                {
                    bestMove = move;
                    bestMoveEval = eval;
                }
                shouldEndSearch = bestMoveEval == 1000000000 ||
                    timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining - Evaluate(true) >> depth;
                if (shouldEndSearch) break;
            }
            if (shouldEndSearch) break;
            allMoves = allMoves.Where(move => bestMoveEval - evals[move] < 16384 / depth).OrderByDescending(move => evals[move]).ToArray();
        }
        return bestMove;
    }

    Board board;

    int MoveValue(Move move)
    {
        return move == transpositionTable[board.ZobristKey & 65535].bestMove ? 1000000000 :
            (int)move.PromotionPieceType + (int)move.CapturePieceType
            / (board.SquareIsAttackedByOpponent(move.TargetSquare) ? (int)move.MovePieceType : 1);
    }

    int Search(Move move, int depth, int alpha, int beta)
    {
        board.MakeMove(move);
        Transposition result = AlphaBeta(board.IsInCheck() ? depth > 0 ? depth : 1 : --depth, -beta, -alpha);
        transpositionTable[board.ZobristKey & 65535] = result;
        board.UndoMove(move);
        return -result.eval;
    }

    struct Transposition
    {
        public ulong key;
        public int depth,
        eval,
        flag;// 0=unassigned, 1=exact, 2=alpha, 3=beta
        public Move bestMove;
    }

    Transposition[] transpositionTable = new Transposition[65536];

    Transposition AlphaBeta(int depth, int alpha, int beta)
    {
        Transposition newResult,
        previousResultLookup = transpositionTable[board.ZobristKey & 65535];
        newResult.key = board.ZobristKey;
        newResult.depth = depth;
        newResult.flag = 1;
        newResult.bestMove = Move.NullMove;
        if (board.IsDraw())
        {
            newResult.eval = 0;
            return newResult;
        }
        if ((previousResultLookup.depth >= depth || depth <= 0) && previousResultLookup.key == board.ZobristKey &&
            (previousResultLookup.flag == 1 ||
            previousResultLookup.flag == 2 && previousResultLookup.eval <= alpha ||
            previousResultLookup.flag == 3 && previousResultLookup.eval >= beta))
            return previousResultLookup;
        if (board.IsInCheckmate())
        {
            newResult.eval = -1000000000;
            return newResult;
        }
        int ourValue = Evaluate(board.IsWhiteToMove),
        opponentsValue = Evaluate(!board.IsWhiteToMove),
        evaluation = (ourValue - opponentsValue) * 65536 / (ourValue + opponentsValue), // Encourages trading when you're up material, but the final value no longer has a nice interpretation
        numberOfMoves = 0;
        newResult.flag = 2;
        foreach (Move move in board.GetLegalMoves().OrderByDescending(MoveValue))
        {
            // This ensures we only consider captures and promotions when depth <= 0
            if (depth <= 0 && MoveValue(move) <= 0) break;
            numberOfMoves++;
            int eval = Search(move, depth, alpha, newResult.flag == 1 ? alpha + 1 : beta);
            if (eval > alpha)
            {
                if (newResult.flag == 1 && eval < beta)
                    eval = Search(move, depth, alpha, beta);
                if (eval < alpha) continue;
                alpha = eval;
                newResult.bestMove = move;
                newResult.flag = 1;
            }
            if (eval >= beta)
            {
                newResult.bestMove = move;
                newResult.flag = 3;
                newResult.eval = beta;
                return newResult;
            }
        }
        if (depth > 0 || numberOfMoves > 0 && alpha > evaluation) newResult.eval = alpha;
        else
        {
            newResult.flag = 1;
            newResult.eval = evaluation;
        }
        return newResult;
    }

    // First 7 are "centrality" weight for each piece, next 7 are "Field of view" weights", last 7 are material weights
    int[] evalWeights = { 0, 2, 1, 0, 0, -2, 2, 7, 12, 10, 15, 11, 1, 8, 800, 2000, 2200, 3400, 6300, 3000 };

    int Evaluate(bool white)
    {
        int sum = 0,
        pieceIndex = 0;
        for (; pieceIndex < 6; pieceIndex++)
        {
            PieceType pieceType = (PieceType)(pieceIndex + 1);
            ulong bitboard = board.GetPieceBitboard(pieceType, white);
            while (bitboard != 0)
            {
                sum += evalWeights[pieceIndex + 14];
                if (pieceIndex == 6 && sum < 15000)
                    pieceIndex++;
                int index = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard),
                shiftedBitboard = (int)(bitboard >> index);
                if (pieceIndex == 0) //Pawns only
                    // Bonuses for pawn advancement, phalanx, protected pawns and doubled pawns
                    sum += (white ? index / 8 : 7 - index / 8) * 32 + (shiftedBitboard << 3 & 16) + (shiftedBitboard >> 3 & 16) + (shiftedBitboard >> 5 & 16) - (shiftedBitboard >> 3 & 32);
                ulong pieceVision = BitboardHelper.GetPieceAttacks(pieceType, new Square(index), board, white);
                while (pieceVision != 0)
                {
                    int targetSquare = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceVision);
                    sum += (targetSquare / 8 * (7 - targetSquare / 8) + targetSquare % 8 * (7 - targetSquare % 8)) * evalWeights[pieceIndex] + evalWeights[pieceIndex + 7];
                }
            }
        }
        return sum;
    }
}