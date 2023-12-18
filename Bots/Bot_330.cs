namespace auto_Bot_330;
using ChessChallenge.API;


public class Bot_330 : IChessBot
{
    //Constants
    //=========

    //Color (set at game start)
    int am_white;

    //Piece Values (centipawns)
    int[] piece_val_arr = { 100, 300, 300, 500, 900 };
    //=========

    //Search Depth
    int search_depth = 4;
    // \Constants
    public Move Think(Board board, Timer timer)
    {
        am_white = board.IsWhiteToMove ? 1 : -1;
        int best_score = int.MinValue;
        Move[] moves = board.GetLegalMoves();
        Move best_move = moves[0];
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int pos_eval = -negamax(board, search_depth - 1, int.MinValue, int.MaxValue, am_white * -1);
            if (pos_eval > best_score)
            {
                best_score = pos_eval;
                best_move = move;
            }
            board.UndoMove(move);
        }
        return best_move;
    }

    public int negamax(Board board, int depth, int alpha, int beta, int color) //white is 1, black is -1
    {
        if (depth == 0 || board.IsInCheckmate())
            return color * evaluate(board);
        int score = int.MinValue;
        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int search_score = -negamax(board, depth - 1, -beta, -alpha, -color);
            board.UndoMove(move);
            score = max(score, search_score); //max of current best score and new score
            alpha = max(alpha, score);
            if (alpha >= beta)  //prune the current node, we will never reach it
                break;
        }
        return score;
    }

    public int evaluate(Board board)
    {
        //local vars (ik the compiler can maybe handle the optimization but taking no chances)
        //==========

        //general
        PieceList[] piece_lists = board.GetAllPieceLists();     //getting pieces
        PieceList piece_type_list;

        //Material
        int white_points = 0;  //material counts (centipawns)
        int black_points = 0;

        //Mobility
        PieceType piece_type;
        PieceType pawn = PieceType.Pawn;
        ulong white_piece_mobility_bb;
        ulong black_piece_mobility_bb;
        ulong white_pawn_attacks_bb = 0;
        ulong black_pawn_attacks_bb = 0;
        int white_mobility = 0;
        int black_mobility = 0;

        //checkmate
        int white_to_move = board.IsWhiteToMove ? 0 : 1;
        int black_to_move = board.IsWhiteToMove ? 1 : 0;
        int is_checkmate = board.IsInCheckmate() ? 1 : 0;
        int white_checkmates = 100000 * white_to_move * is_checkmate;
        int black_checkmates = 100000 * black_to_move * is_checkmate;

        //King Safety
        int white_safety_score = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(piece_lists[5][0].Square) & board.WhitePiecesBitboard) * 3; //get squares king is attacking, AND with own pieces find # of set bits, and add a multiplier
        int black_safety_score = BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetKingAttacks(piece_lists[11][0].Square) & board.BlackPiecesBitboard) * 3;

        //==========
        //local vars



        //Mobility
        //========
        //calculate pawn attacks bb's
        foreach (Piece piece in piece_lists[0])
        {
            white_pawn_attacks_bb |= BitboardHelper.GetPieceAttacks(pawn, piece.Square, board, true);
        }
        foreach (Piece piece in piece_lists[6])
        {
            black_pawn_attacks_bb |= BitboardHelper.GetPieceAttacks(pawn, piece.Square, board, false);
        }
        //========
        //Mobility

        for (int i = 0; i < 12; i++) //iterating over all pieces
        {
            //vars set for every new piece type
            piece_type_list = piece_lists[i];
            int piece_count = piece_type_list.Count;
            piece_type = piece_lists[i].TypeOfPieceInList;

            //Material
            //========
            if ((int)piece_type != 6) //skip king values, since they are N/A
            {
                if (i < 6) //white pieces
                {
                    white_points += piece_count * piece_val_arr[i];
                }
                else //black pieces
                {
                    black_points += piece_count * piece_val_arr[i - 6];
                }
            }
            //========
            //Material

            //Mobility
            //========

            foreach (Piece piece in piece_type_list)
            {
                if ((int)piece_type != 1 && (int)piece_type != 6) //ignore pawns and kings
                {
                    if (piece.IsWhite)
                    {
                        white_piece_mobility_bb = BitboardHelper.GetPieceAttacks(piece_type, piece.Square, board, true);
                        white_mobility += (BitboardHelper.GetNumberOfSetBits(white_piece_mobility_bb) * 3) + (BitboardHelper.GetNumberOfSetBits(white_piece_mobility_bb & ~black_pawn_attacks_bb) * 2);  //bonus to move white piece where black pawns aren't attacking
                        //BitboardHelper.VisualizeBitboard(white_piece_attacks_bb);
                    }
                    else
                    {
                        black_piece_mobility_bb = BitboardHelper.GetPieceAttacks(piece_type, piece.Square, board, false);
                        black_mobility += (BitboardHelper.GetNumberOfSetBits(black_piece_mobility_bb) * 3) + (BitboardHelper.GetNumberOfSetBits(black_piece_mobility_bb & ~white_pawn_attacks_bb) * 2);  //bonus to move black piece where white pawns aren't attacking
                    }
                }
            }
            //========
            //Mobility
        }

        //calculate eval
        int eval = (white_points - black_points) + (white_checkmates - black_checkmates) + (white_mobility - black_mobility) + (white_safety_score - black_safety_score);
        return eval;
    }

    public int max(int a, int b)
    {
        return (a > b) ? a : b;
    }
}