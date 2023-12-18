namespace auto_Bot_376;
using ChessChallenge.API;
using System;

public class Bot_376 : IChessBot
{

    // ulong: 8 bytes
    // 3 * int: 12 bytes
    // Move: ~4 bytes ?
    // total: ~24 bytes
    record struct HashTableNode(
        ulong full_hash,
        int value,

        // 0 = not visited
        // 1 = principle value
        // 2 = cut node quiescent search (lower bound)
        // 3 = all node quiescent search (upper bound)
        int node_type_int_code,
        int depth_searched,
        Move move
    );

    // 8 million => ~192 mb
    static ulong hash_table_size = 8000000;
    HashTableNode[] hash_table = new HashTableNode[hash_table_size];

    // first: history heuristic table
    // second: butterfly heuristic table
    // How many times has [piece] moved to [square]?
    int[,,] heuristics_tables = new int[2, 12, 64];

    // For [piece], what is the value at [square]?
    int[,] piece_square_value_table = new int[12, 64];

    const int checkmate_value = -2147483647;

    Board board_state;
    Timer timer;

    bool TimeIsGood => timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 50;

    public Bot_376()
    {
        int Mound(int x) => x * (7 - x);

        for (int rank = 0; rank < 8; rank++)
            for (int file = 0; file < 8; file++)
            {
                // index
                int board_square_index = 8 * rank + file;

                // white pawn
                piece_square_value_table[0, board_square_index] = 100
                    + rank * rank * (rank > 4 ? 9 : 4) / 3 + Mound(file);

                // white knight
                piece_square_value_table[1, board_square_index] = 275
                    + Mound(file) * Mound(rank) / 15;

                // white bishop
                piece_square_value_table[2, board_square_index] = 170
                    + piece_square_value_table[1, board_square_index] / 2;

                // white rook
                piece_square_value_table[3, board_square_index] = 500;

                // white queen
                piece_square_value_table[4, board_square_index] = 900;

                // king
                piece_square_value_table[5, board_square_index] = -Mound(file) * 2;

                // black pieces
                for (int k = 0; k < 6; k++)
                    piece_square_value_table[k + 6, board_square_index ^ 56] = -piece_square_value_table[k, board_square_index];
            }
        Array.Fill(hash_table, new HashTableNode { node_type_int_code = 0 });
    }

    int EvaluateBoardState()
    {
        int value = 0;
        int piece_type_index = 0;
        foreach (var piece_list in board_state.GetAllPieceLists())
        {
            foreach (var piece in piece_list)
                value += piece_square_value_table[piece_type_index, piece.Square.Index];
            piece_type_index++;
        }
        return board_state.IsWhiteToMove ? value : -value;
    }

    int PlyCorrectedValue(int value) => value switch
    {
        > -checkmate_value - 99 => value - 1,
        < checkmate_value + 99 => value + 1,
        _ => value
    };

    // depth = 0 -> leaf node
    // depth < 0 -> quiescence search
    int NegaMaxSearch(int alpha, int beta, int depth)
    {
        if (board_state.IsDraw()) return 0;
        if (board_state.IsInCheckmate()) return checkmate_value;

        ulong hash = board_state.ZobristKey;
        ulong hash_table_index = hash % hash_table_size;
        HashTableNode hash_table_node = hash_table[hash_table_index];
        Move hint_move = hash_table_node.move;
        int hash_table_value = hash_table_node.value;

        // hash table look up.
        // If not a hash hit, then overwrite hint_move.
        // If in quiescence search, then keep hint_move but always search (could be check)
        //     This requires no correction. So no-op.
        // Otherwise, use the int code.
        //     If not visited (int code = 0), then overwrite hint_move.
        switch (
            hash_table_node.full_hash != hash ?
                0 :
            hash_table_node.depth_searched < depth ?
                4 :
                hash_table_node.node_type_int_code)
        {
            case 0:
                hint_move = Move.NullMove;
                break;
            case 1:
                // pv
                return PlyCorrectedValue(hash_table_value);
            case 2:
                // cut node (lower bound)
                if (hash_table_value >= beta)
                    return PlyCorrectedValue(hash_table_value);
                alpha = Math.Max(hash_table_value - 1, alpha);
                break;
            case 3:
                // all node (upper bound)
                if (hash_table_value <= alpha)
                    return PlyCorrectedValue(hash_table_value);
                beta = Math.Min(hash_table_value + 1, beta);
                break;
        }

        var moves = board_state.GetLegalMoves(depth < 0);
        var heuristic_values = new int[moves.Length];
        int best_value = checkmate_value; // return value
        Move best_move = depth < 0 ? Move.NullMove : moves[0];

        void UpdateHashTable()
        {
            hash_table[hash_table_index] = new HashTableNode
            {
                // 2: beta cutoff
                // 3: all node
                // 1: pv
                node_type_int_code =
                    best_value >= beta ?
                        2 :
                    best_value <= alpha ?
                        3 :
                        1,
                value = best_value,
                full_hash = hash,
                depth_searched = depth,
                move = best_move
            };
        }

        // Null move heuristic
        if (depth < 0)
        {
            best_value = EvaluateBoardState();
            if (best_value >= beta)
            {
                UpdateHashTable();
                return PlyCorrectedValue(best_value);
            }
        }

        // Apply some move order heursitics.
        // Gets sorted in ascending order, so negative is better.
        for (int i = 0; i < moves.Length; ++i)
        {
            Move move = moves[i];
            if (move == hint_move)
                heuristic_values[i] = -1000000;
            else
            {
                if (move.IsCapture) // "capture heuristic"
                    heuristic_values[i] = -(Math.Abs(piece_square_value_table[(int)move.CapturePieceType, move.TargetSquare.Index]) - 150) / 25;
                else
                {
                    int butterfly_heuristic_value = heuristics_tables[
                        1, (int)move.MovePieceType, move.TargetSquare.Index];
                    int history_heuristic_value = heuristics_tables[
                        0, (int)move.MovePieceType, move.TargetSquare.Index];
                    heuristic_values[i] = (butterfly_heuristic_value, history_heuristic_value) switch
                    {
                        (0, _) => 0,
                        (_, 0) => butterfly_heuristic_value,
                        _ => -history_heuristic_value / butterfly_heuristic_value
                    };
                }
            }
        }
        Array.Sort(heuristic_values, moves);

        int move_index = 0;
        foreach (var move in moves)
        {
            if (!move.IsCapture) // butterfly table
                heuristics_tables[1, (int)move.MovePieceType, move.TargetSquare.Index]++;
            board_state.MakeMove(move);
            int temp_alpha = Math.Max(best_value, alpha);
            int new_value = temp_alpha;
            // Begin pvs on third move.
            // If alpha+1 == beta, then this is a null window. (we're already in pvs)
            // If we're in check, then do a full search.
            bool principle_value_search = move_index++ > 2 && alpha + 1 < beta && !board_state.IsInCheck();
            if (principle_value_search)
                new_value = -NegaMaxSearch(-temp_alpha - 1, -temp_alpha, depth - 1); // null window search
            if (new_value > temp_alpha || !principle_value_search)
                new_value = -NegaMaxSearch(-beta, -temp_alpha, depth - 1); // full window search
            board_state.UndoMove(move);
            if (new_value > best_value) // better move/value found
            {
                best_value = new_value;
                best_move = move;
            }
            if (best_value >= beta) // beta cutoff
            {
                UpdateHashTable();
                if (!move.IsCapture)
                    heuristics_tables[0, (int)move.MovePieceType, move.TargetSquare.Index] += ++depth * depth--;
                return PlyCorrectedValue(best_value);
            }
            if (depth >= 0 && !TimeIsGood) // check time if in main search
                return PlyCorrectedValue(best_value);
        }
        UpdateHashTable();
        return PlyCorrectedValue(best_value);
    }

    public Move Think(
        Board _board_state,
        Timer _timer)
    {
        board_state = _board_state;
        timer = _timer;
        heuristics_tables.Initialize();
        var moves = board_state.GetLegalMoves();
        var values = new int[moves.Length];
        values.Initialize();

        for (int depth = -1; TimeIsGood; depth++)
        {
            for (int i = 0; i < moves.Length && TimeIsGood; i++)
            {
                board_state.MakeMove(moves[i]);
                int value = NegaMaxSearch(checkmate_value, -checkmate_value, depth);
                board_state.UndoMove(moves[i]);
                if (TimeIsGood) values[i] = value;
            }
            Array.Sort(values, moves);
        }
        return moves[0];
    }

}