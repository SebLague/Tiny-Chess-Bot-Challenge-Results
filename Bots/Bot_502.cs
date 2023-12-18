namespace auto_Bot_502;

using ChessChallenge.API;
using System;

// Stickleback2
// by Brent Mardell

public class Bot_502 : IChessBot
{

    public Bot_502()
    {
        //reset killer move heuristic
        for (int i = 0; i < k_moves.Length;)
            k_moves[i++] = Move.NullMove;

        //unpack and decompress piece-square tables
        for (int i = 0, e; i < 448;)
        {
            e = ReadBits(1 << (ReadBits(2) + 1));
            ps_tables[i++] = (e & 1) != 0 ? -(e >> 1) : e >> 1;
        }
    }

    private static readonly Decimal[] data = {
25402602827074001411439067136m,   784148955231784940165884324m, 10300438289796591767343753472m, 31133647439600396175968189578m,
31288917492418648549418748306m,  3221799581430419479309204879m, 30903247881094357508178647206m, 42238229296129823208116447247m,
 1588417442158262279112395298m, 76231959794408809473571578840m, 30126837945668467954140799960m, 41279199780059009092119041413m,
10300073390647401851998720532m, 52501618109434582523175900293m,       25407519220809607435797m, 12850132235749221054896543104m,
     215826905665494141276160m,   884027005606061870049057600m, 50854770431053867109739397120m, 41279201168561298112149192704m,
 3222574067907104281349609001m, 55757654484898573336367595606m,  6653927711546606956425396310m, 11074526296967798122518501542m,
42285807321401684552748138501m, 42826615793251560588634064226m, 21762550796092974202283947581m, 31675397469895310718273152601m,
25253734774138452809177518489m, 67243257280016862855238506822m, 44380086948638445606270769507m, 44299616271361540898705899581m,
26688945987226150497825609256m, 30900551190347041245139788760m, 12862233299866933276021233039m, 51424754752319411770409615446m,
                   1704223704m
};
    //length=37

    private int bitpos = 0,
    lastdepth = 5;
    //piece-square tables
    private int[] ps_tables = new int[448],
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    piecevalues = { 0, 100, 300, 310, 500, 900, 10000 },
    //transposition table
    t_table = new int[262144],
    t_tabledepth = new int[262144];
    private ulong[] t_tablehash = new ulong[262144];
    //killer heuristic moves
    private Move[] k_moves = new Move[20];

    private int ReadBits(int bits)
    {
        int v = 0,
        i = 0,
        bitend = bitpos + bits;
        while (bitpos < bitend)
            v |= ((Decimal.GetBits(data[bitpos / 96])[bitpos / 32 % 3] >> (bitpos++ % 32)) & 1) << i++;
        return v;
    }

    private int EvaluateMove(Board board, Move move, bool endgame, int depth)
    {
        bool turn = board.IsWhiteToMove;
        board.MakeMove(move);
        if (board.IsInCheckmate())
            return 100000;
        //if(board.IsInStalemate())
        //    return -5000;

        int captured = (int)move.CapturePieceType,
        ty = move.TargetSquare.Rank,
        value;

        if (captured >= 1)
        {
            value = piecevalues[captured]; // + ps_tables[(captured - 1) * 64 + (turn ? 7 - ty : ty) * 8 + move.TargetSquare.File];
            //pawn captured, value higher closer to promotion
            if (captured == 1)
                value += (turn ? 7 - ty : ty) * 15;
            if (move.IsPromotion)
                value += piecevalues[(int)move.PromotionPieceType] - 100; //piecevalues[(int)board.GetPiece(move.TargetSquare).PieceType] - 100;
        }
        else if (move.IsCastles)
            value = 30;
        else if (move.IsPromotion)
            value = piecevalues[(int)move.PromotionPieceType] - 100; //piecevalues[(int)board.GetPiece(move.TargetSquare).PieceType] - 100;
        else
        {
            int moved = ((int)move.MovePieceType - 1) * 64,
            sy = move.StartSquare.Rank,
            tx = move.TargetSquare.File;

            if (endgame && move.MovePieceType == PieceType.King)
                moved += 64;

            if (!turn)
            {
                sy = 7 - sy;
                ty = 7 - ty;
            }

            value = ps_tables[moved + ty * 8 + tx] - ps_tables[moved + sy * 8 + move.StartSquare.File];
        }

        if (depth == 0 && board.IsInCheck())
            value += 30;

        return value;
    }

    // Minimax search with alpha-beta pruning, move ordering, transposition table
    // and killer heuristic
    private int AlphaBeta(Board board, int alpha, int beta, int depth, int curvalue, bool endgame)
    {
        //if(depth == 0)
        //    return curvalue;

        //only look at captures at deepest node
        Move[] moves = board.GetLegalMoves(depth <= 1);

        if (moves.Length == 0)
            return curvalue;

        depth--;

        int v;

        //check transposition table
        ulong k = board.ZobristKey,
        tp = k & 0x3FFFE;
        while ((tp & 1) == 0)
        {
            if (t_tablehash[tp] == k && t_tabledepth[tp] >= depth)
            {
                v = t_table[tp];
                if (v > 99000)
                    return v - (t_tabledepth[tp] - depth);
                else if (v < -99000)
                    return v + (t_tabledepth[tp] - depth);

                return v + curvalue;
            }
            tp++;
        }

        int besteval = -1000000,
        i = 0,
        eval = -10000000;
        Move kmove = k_moves[depth];
        int[] value = new int[moves.Length];

        //evaluate moves
        foreach (Move move in moves)
        {
            v = -EvaluateMove(board, move, endgame, depth);
            if (v <= -100000)
            {
                //can't find better move than checkmate
                board.UndoMove(move);
                return 100000 + depth;
            }
            if (move == kmove)
            {
                //put killer heuristic move first
                eval = v;
                v = -10000000;
            }
            value[i++] = v;
            board.UndoMove(move);
        }

        //order moves (values lowest to highest of negated values)
        Array.Sort(value, moves);
        //replace killer move evaluation
        if (eval != -10000000)
            value[0] = eval;
        //search with ordered moves
        i = 0;
        foreach (Move move in moves)
        {
            if (depth > 0)
            {
                board.MakeMove(move);
                eval = -AlphaBeta(board, -beta, -alpha, depth, value[i] - curvalue, endgame);
                board.UndoMove(move);
            }
            else
                eval = curvalue - (value[i] / 2);

            //alpha-beta prune and get best evaluation
            if (eval >= beta)
            {
                besteval = eval;
                //set killer heuristic move
                k_moves[depth] = move;
                break;
            }
            if (eval > alpha)
                alpha = eval;
            if (eval > besteval)
                besteval = eval;
            i++;
        }

        //update transposition table
        //second entry is for deepest search
        if (depth < t_tabledepth[tp])
            tp &= 0x3FFFE;

        t_table[tp] = Math.Abs(besteval) >= 99000 ? besteval : besteval - curvalue;
        t_tabledepth[tp] = depth;
        t_tablehash[tp] = k;

        return besteval;
    }

    public Move Think(Board board, Timer timer)
    {
        Move bestmove = Move.NullMove;
        int besteval = -10000000,
        alpha = -1000000,

        timeremaining = timer.MillisecondsRemaining,
        depth = timeremaining > 15000 ? 5 : (timeremaining > 6000 ? 4 : 3);

        ulong allpieces = 0,
        bitboard = board.AllPiecesBitboard;

        for (int i = 0; i < 64; i++)
            allpieces += (bitboard >> i) & 1;

        //use deeper search with fewer pieces
        if (allpieces <= 5)
            depth += 2;
        bool endgame = allpieces <= 8;

        if (lastdepth != depth)
        {
            Array.Clear(t_tablehash);
            lastdepth = depth;
        }

        foreach (Move move in board.GetLegalMoves())
        {
            int moveeval = EvaluateMove(board, move, endgame, depth);
            int eval = 0;
            if (moveeval >= 20000)
                return move;
            else if (board.IsDraw())
            {
                //capture prevents opponent winning
                if (move.IsCapture && (!move.IsPromotion) && board.IsInsufficientMaterial())
                    return move;
                eval = -5000;
            }
            else
                eval = -AlphaBeta(board, -1000000, -alpha, depth, -moveeval, endgame);
            if (eval > alpha)
                alpha = eval;
            if (eval > besteval)
            {
                besteval = eval;
                bestmove = move;
            }
            board.UndoMove(move);
        }

        return bestmove;
    }
}
