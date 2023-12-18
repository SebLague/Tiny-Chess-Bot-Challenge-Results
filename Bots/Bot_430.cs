namespace auto_Bot_430;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;


public class Bot_430 : IChessBot
{
    float[] pieceValues = { 0, 1.0f, 3.0f, 3.0f, 5.0f, 9.0f, 10000.0f };

    // if we use 4bit, we could fit 16 values into a single ulong
    // the idea being that the value would be from 0-15 and we subtract constant 7,
    // effectively giving us a range of -7 to 7 (or technically 8)
    // I have a utility script that that figures out the table values, offset and multiplier
    // automatically based on the number of bits we want to use.
    // PeSTO shows 12 tables here so with some rounding, this should still be applicable later
    // we might even use 5 bits and just use 60 out of our 64 bits!
    // we could even use the remaining bits to define asymmetry like -20 to 100 if needed
    // https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    ulong[] square_weights = {
        16408618656, 16512430854, 16515576390, 16515642854, 16515642854, 16515576390, 16512430854, 16408618656,
        18659848870, 19907722918, 19846907375, 10185326063, 10185326063, 19846905327, 19907722918, 18659848870,
        18662994310, 14580958508, 13508265269, 16731587896, 16731587896, 13508265269, 14580956460, 18662994310,
        16515513638, 16625679564, 16731587800, 23175087227, 23175087227, 16731587800, 16625679564, 16515511590,
        18662995142, 18842369132, 19919258744, 25322570779, 25322570779, 19919258744, 18842369132, 18662995142,
        19736735942, 19846904940, 23138387061, 26395264024, 26395264024, 23138387061, 19846904940, 19736735942,
        33692364998, 33799355497, 33805646956, 33805646863, 33805646863, 33805646956, 33799355497, 33692364998,
        16408618176, 16512430179, 16515575910, 16515576841, 16515576841, 16515575910, 16512430179, 16408618176
    };

    float get_square_weight(int square, int type)
    {
        // [0=king_eg, 1=king_mg, 2=queen, 3=rook, 4=knight, 5=bishop, 6=pawn]
        // (weight-offset)*multiplier, values generated using external utility script
        return (((int)(square_weights[square] >> (5 * type)) & 31) - 15) * 3.3333333333333335f;
    }

    private float eval_material(Board board)
    {
        PieceList[] pieces = board.GetAllPieceLists();

        float result = 0;
        for (int piece = 0; piece < 5; piece++)
        {
            result += pieceValues[piece + 1] * (pieces[piece].Count - pieces[piece + 6].Count);
        }
        return result;
    }

    private bool is_endgame(Board board)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        int pieces_white = pieces[1].Count + pieces[2].Count + pieces[3].Count + pieces[4].Count;
        int pieces_black = pieces[7].Count + pieces[8].Count + pieces[9].Count + pieces[10].Count;

        return pieces_white < 4 && pieces_black < 4;
    }

    private float eval_placement(Board board, bool white, bool debug = false)
    {
        float result = 0;
        //if (debug) DivertedConsole.Write("Collecting placement info for " + white + "(Endgame: " + is_endgame(board) + ")");

        int[] offsets = { is_endgame(board) ? 0 : 1, 2, 3, 4, 5, 6 };
        PieceType[] pieceTypes = { PieceType.King, PieceType.Queen, PieceType.Rook, PieceType.Knight, PieceType.Bishop, PieceType.Pawn };
        for (int i = 0; i < pieceTypes.Length; i++)
        {
            ulong pieces = board.GetPieceBitboard(pieceTypes[i], white);
            //if (debug) DivertedConsole.Write("  Number of pieces: " + BitboardHelper.GetNumberOfSetBits(pieces));
            while (BitboardHelper.GetNumberOfSetBits(pieces) > 0)
            {
                int square = BitboardHelper.ClearAndGetIndexOfLSB(ref pieces);
                if (!white) square = 63 - square;
                float wt = get_square_weight(square, offsets[i]);
                //if (debug) DivertedConsole.Write("    Square weight: " + wt + " (Square: " + square + "-" + (new Square(square)) + ", Type: " + offsets[i] + ")");
                result -= wt;
            }

        }
        return result * 0.001f;
    }

    private float eval_bishop_pair(Board board)
    {
        PieceList[] pieces = board.GetAllPieceLists();
        return ((pieces[2].Count > 1 ? 1.0f : 0.0f) - (pieces[8].Count > 1 ? 1.0f : 0.0f)) * 0.3f;
    }

    private float eval_position(Board board, Timer timer, bool debug = false)
    {
        float result = eval_material(board)
            + eval_bishop_pair(board)
            + eval_placement(board, board.IsWhiteToMove, debug)
            - eval_placement(board, !board.IsWhiteToMove, debug);

        result *= (board.IsWhiteToMove ? 1 : -1);

        //if (debug)
        //{
        //    DivertedConsole.Write("");
        //DivertedConsole.Write("    Material:   " + -eval_material(board));
        //DivertedConsole.Write("    Bishops:    " + -eval_bishop_pair(board));
        //    DivertedConsole.Write("Friend DF:  " + eval_freedom(board, board.IsWhiteToMove));
        //    DivertedConsole.Write("Foe DF:     " + eval_freedom(board, !board.IsWhiteToMove));
        //DivertedConsole.Write("    Friend Pos: " + -eval_placement(board, board.IsWhiteToMove));
        //DivertedConsole.Write("    Foe Pos:    " + eval_placement(board, !board.IsWhiteToMove));
        //DivertedConsole.Write("    Returning:  " + result);
        //}
        return result;
    }

    public void order_moves(Board board, ref Move[] moves)
    {
        float[] scores = new float[moves.Length];
        int idx = 0;
        foreach (Move move in moves)
        {
            float score_move = 0.0f;
            if (move.IsCapture)
            {
                score_move = 10 * (int)move.CapturePieceType - (int)move.MovePieceType;
            }

            if (move.IsPromotion)
            {
                score_move += (int)move.PromotionPieceType;
            }

            if (!is_endgame(board) && move.StartSquare == board.GetKingSquare(board.IsWhiteToMove))
            {
                score_move -= 10.0f / board.PlyCount;
            }

            // to check if the current square is attacked by an opponents pawn, place an
            // imaginary pawn of our color on the square and check the if there our opponent
            // has a pawn on a field attacked by our pawn (I think...)
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                ulong bitboard_pawn_squares = BitboardHelper.GetPawnAttacks(move.TargetSquare, board.IsWhiteToMove);
                ulong bitboard_opponent_pawns = board.GetPieceBitboard(PieceType.Pawn, !board.IsWhiteToMove);

                if ((bitboard_pawn_squares & bitboard_opponent_pawns) > 0)
                {
                    score_move -= (int)move.MovePieceType;
                    //DivertedConsole.Write("   Attacked by pawn: " + score_move);
                }
            }

            board.MakeMove(move);
            if (board.IsInCheck()) { score_move += 5; }
            //score_move += eval_freedom(board, board.IsWhiteToMove);
            board.UndoMove(move);

            scores[idx++] = score_move;// * (board.IsWhiteToMove ? 1.0f : -1.0f);
        }
        Array.Sort(scores, moves);
        Array.Reverse(moves);
    }

    float search(Board board, Timer timer, int depth, float alpha, float beta, bool captures_only)
    {
        if (depth == 0) return captures_only ? search(board, timer, 0, alpha, beta, true) : eval_position(board, timer);

        Move[] moves = board.GetLegalMoves(captures_only).Union(GetCheckMoves(board)).ToArray(); ;

        if (moves.Length == 0)
        {
            if (board.IsInCheckmate())
            {
                return -(65000.0f + depth); // Consider depth in checkmate scoring
            }
            else
            {
                return captures_only ? eval_position(board, timer) : 0.0f;
            }
        }
        order_moves(board, ref moves);
        float bestScore = -999999.0f;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            float eval = -search(board, timer, depth - 1, -beta, -alpha, captures_only);
            board.UndoMove(move);

            bestScore = Math.Max(bestScore, eval);
            alpha = Math.Max(alpha, eval);

            if (alpha >= beta)
                break;  // Alpha-beta cut-off
        }

        return bestScore;
    }

    private Move[] GetCheckMoves(Board board)
    {
        Move[] moves = board.GetLegalMoves();
        List<Move> result = new List<Move>();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            if (board.IsInCheck()) result.Add(move);
            board.UndoMove(move);
        }
        return result.ToArray();
    }

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves(false);
        Move move_best = moves[0];

        float alpha = -999999.0f;
        float beta = 999999.0f;

        foreach (Move move in moves)
        {
            //DivertedConsole.Write("Move: " + move);
            board.MakeMove(move);
            float new_evaluation = -search(board, timer, 3, alpha, beta, false);
            //float new_evaluation = eval_position(board, timer, true);
            board.UndoMove(move);

            //DivertedConsole.Write("    Final Eval: " + new_evaluation);

            if (new_evaluation > alpha)
            {
                alpha = new_evaluation;
                move_best = move;
            }
        }
        return move_best;
    }
}