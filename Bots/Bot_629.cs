namespace auto_Bot_629;
using ChessChallenge.API;
using System;


// Search
// [ ] Draw = 0
// [x] iterative deepening
// [x] eval sort moves
// [x] transposition table
// [x] killer moves
// [ ] quiescence search
// [ ] history heuristic
// [ ] null move pruning

// Eval
// [x] material
// [x] attacks



public class Bot_629 : IChessBot
{
    public class SmallerTranspositionEntry
    {
        public ulong Key { get; set; }
        // TODO: store only high part
        // uint highPart = (uint)(originalValue >> 32);
        public byte Depth { get; set; }  // Use byte if depth is limited to a small range (e.g., up to 255)
        public short Score { get; set; } // Use short if the score is within a limited range
        public short Alpha { get; set; } // Use short if the alpha value is within a limited range
        public short Beta { get; set; }  // Use short if the beta value is within a limited range
        public byte Flag { get; set; }
    }

    // 8738133 = 200 * 1024 * 1024 / 24 (size of TranspositionEntry)
    // closes to 2^23 = 8388608
    // Was a bit unsure so just halved it in the end
    SmallerTranspositionEntry[] transpositionTable = new SmallerTranspositionEntry[4194304]; // 2^22
    Move[,] killerMoves = new Move[50, 2];


    public Move Think(Board board, Timer timer)
    {
        int[] BitBordPieceScore = new int[64];
        int infinity = 32000, initialAlpha = -infinity, initialBeta = infinity;
        Move bestMove = board.GetLegalMoves()[0];

        int moveTime = Math.Min(timer.GameStartTimeMilliseconds / 50, timer.MillisecondsRemaining / 10);

        int evalPosForColor(bool isWhite)
        {
            int eval = 0;
            var scores = new int[] { 0, 100, 300, 300, 500, 900, 1000 };
            for (int i = 0; ++i < 7;)
            {
                ulong pieceBoard = board.GetPieceBitboard((PieceType)i, isWhite);
                eval += BitboardHelper.GetNumberOfSetBits(pieceBoard) * scores[i];
                // loop through indvidual pieces?
                while (pieceBoard != 0)
                {
                    // Square kingSquare = board.GetKingSquare(isWhite);
                    Square square = new Square(BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBoard));
                    // imporove eval for pieces that attack more squares
                    eval += BitboardHelper.GetNumberOfSetBits(
                        BitboardHelper.GetPieceAttacks((PieceType)i, square, board.AllPiecesBitboard, isWhite)
                    ) * 10;
                }
            }
            return eval;
        }

        int evaluate(Board board, int ply)
        {
            if (board.IsInCheckmate()) return infinity - ply;
            else return evalPosForColor(true) - evalPosForColor(false);
        }

        int RankMove(Board board, Move move, int ply)
        {
            return (
                move.Equals(bestMove) ? 2000000 :
                move.IsEnPassant ? 1000000 :
                move.IsCapture ? (
                    100000
                    + (int)move.CapturePieceType * 10
                    - (board.SquareIsAttackedByOpponent(move.TargetSquare) ? (int)move.MovePieceType * 10 : (int)move.MovePieceType)
                ) :
                move.IsPromotion ? 20000 + (int)move.PromotionPieceType :
                move.Equals(killerMoves[ply, 0]) ? 10000 :
                move.Equals(killerMoves[ply, 1]) ? 9000 :
                move.IsCastles ? 900 : 0
            );
        }

        void StoreTransposition(Board board, int value, int depth, int alpha, int beta, byte flag)
        {

            int key = (int)board.ZobristKey & (4194302);
            transpositionTable[key] = new SmallerTranspositionEntry
            {
                Key = board.ZobristKey,
                Depth = (byte)depth,
                Score = (short)value,
                Alpha = (short)alpha,
                Beta = (short)beta,
                Flag = flag,
            };
        }

        SmallerTranspositionEntry? GetTransposition(Board board)
        {
            SmallerTranspositionEntry entry = transpositionTable[(int)board.ZobristKey & (4194302)];
            if (entry != null)
            {
                if (entry.Key == board.ZobristKey) return entry;
            }
            return null;
        }

        int NegaMax(Board board, int ply, int depth, int alpha, int beta)
        {
            int score, alphaOrig = alpha, bestValue = -infinity;
            SmallerTranspositionEntry storedEntry = GetTransposition(board);

            if (storedEntry != null)
            {
                if (storedEntry.Depth >= depth)
                {
                    if (storedEntry.Flag == 0)
                    {
                        return storedEntry.Score;
                    }
                    else if (storedEntry.Flag == 2)
                    {
                        alpha = Math.Max(alpha, storedEntry.Score);
                    }
                    else if (storedEntry.Flag == 1)
                    {
                        beta = Math.Min(beta, storedEntry.Score);
                    }
                    if (alpha >= beta)
                    {
                        return storedEntry.Score;
                    }
                }
            }

            if (depth == 0) return evaluate(board, ply) * (board.IsWhiteToMove ? 1 : -1);

            Move[] moves = board.GetLegalMoves();

            int[] scores = new int[moves.Length];
            for (int i = 0; i < moves.Length; i++)
            {
                scores[i] = RankMove(board, moves[i], ply);
            }

            Array.Sort(scores, moves);
            Array.Reverse(moves);
            for (int i = 0; i < moves.Length; i++)
            {
                var move = moves[i];
                board.MakeMove(move);
                score = -NegaMax(board, ply + 1, depth - 1, -beta, -alpha);
                board.UndoMove(move);

                if (score > bestValue)
                {
                    bestValue = score;
                    if (ply == 1) bestMove = move;
                }

                alpha = Math.Max(alpha, bestValue);
                if (beta <= alpha)
                {
                    if (!move.IsCapture)
                    {
                        if (move.RawValue != killerMoves[ply, 0].RawValue)
                        {
                            killerMoves[ply, 1] = killerMoves[ply, 0];
                            killerMoves[ply, 0] = move;
                        }
                    }
                    break;
                }
            }

            byte flag = 0;
            if (bestValue <= alphaOrig)
                flag = 1; // UPPERBOUND
            else if (bestValue >= beta)
                flag = 2; // LOWERBOUND

            StoreTransposition(board, bestValue, depth, alpha, beta, flag);

            if (moveTime < timer.MillisecondsElapsedThisTurn) throw new Exception();

            return bestValue;
        }

        // Think
        try
        {
            for (int depth = 1; ;) NegaMax(board, 1, depth++ - 1, initialAlpha, initialBeta);
        }
        // Out of time
        catch { }


        return bestMove;
    }
}