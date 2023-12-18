namespace auto_Bot_484;
using ChessChallenge.API;

public class Bot_484 : IChessBot
{
    // Each hex is a value, in order: King end game, King mid game,Queen, Rook, Bishop, Knight, Pawn
    uint[] piece_square_table = {
        0x04010040, 0x22212240, 0x42212440, 0x60312440, 0x60312440, 0x42212440, 0x22212240, 0x04010040,
        0x442222E0, 0x624346E0, 0x82434AE0, 0xA0434AE0, 0xA0434AE0, 0x82434AE0, 0x624346E0, 0x442222E0,
        0x44202460, 0x82414A60, 0xE2515C80, 0xF0516DA0, 0xF0516DA0, 0xE2515C80, 0x82414A60, 0x44202460,
        0x44302450, 0x82415B50, 0xF2515D60, 0xF0516E90, 0xF0516E90, 0xF2515D60, 0x82415B50, 0x44302450,
        0x46402440, 0x84414A40, 0xF4516D40, 0xF2516E80, 0xF2516E80, 0xF4516D40, 0x84414A40, 0x46402440,
        0x48202450, 0x86516B30, 0xE6516C20, 0xF6516D40, 0xF6516D40, 0xE6516C20, 0x86416B30, 0x48202450,
        0x4E202250, 0x4E415660, 0xAA514A60, 0xAA414B00, 0xAA414B00, 0xAA414A60, 0x4E415660, 0x4E202250,
        0x0E010040, 0x4F212240, 0x4C212440, 0x4A322440, 0x4A322440, 0x4C212440, 0x4F212240, 0x0E010040,
    };
    class HashNode
    {
        public ulong key;
        public int evaluation, depth, flag;

        public HashNode(ulong hash_key, int eval, int type, int hash_depth)
        {
            key = hash_key;
            evaluation = eval;
            flag = type;
            depth = hash_depth;
        }
    }
    readonly int[] pieceValues = { 0, 100, 300, 300, 500, 900, 1000 };
    HashNode[] TranspositionTable = new HashNode[0x40000];

    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        OrderMove(moves);
        int index = 0;
        //Iterative deepening with limit depth of 5, time limit of 1/100 of remaining time
        for (int depth = 0; depth < 5; depth++)
        {
            if (timer.MillisecondsElapsedThisTurn > timer.MillisecondsRemaining / 100) break;
            int alpha = -10000000;
            for (int i = 0; i < moves.Length; i++)
            {
                board.MakeMove(moves[i]);
                int value = -Search(board, depth, -10000000, -alpha, false);
                board.UndoMove(moves[i]);
                if (value > alpha)
                {
                    alpha = value;
                    index = i;
                }
            }
        }
        return moves[index];
    }

    int Search(Board board, int depth, int alpha, int beta, bool quiescence)
    {

        ulong key = board.ZobristKey;
        // Check for draw
        if (board.IsInStalemate() || board.IsFiftyMoveDraw() || board.IsRepeatedPosition()) return 0;

        // Check for checkmate, increase score by depth to encourage check mate in fewer moves
        if (board.IsInCheckmate()) return -2000000 - depth;

        // Check transposition table

        int evaluation = readTT(key, alpha, beta, depth), hash_flag = 2;
        if (evaluation != -2147483648) return evaluation;

        evaluation = Evaluate(board);
        if (quiescence)
        {
            if (evaluation >= beta)
            {
                writeTT(key, beta, 3, depth);
                return beta;
            }
            if (evaluation > alpha)
            {
                hash_flag = 1;
                alpha = evaluation;
            }
        }
        quiescence = depth < 1;
        Move[] moves = board.GetLegalMoves();
        OrderMove(moves);

        for (int i = 0; i < moves.Length; i++)
        {
            board.MakeMove(moves[i]);
            if (!quiescence || (board.IsInCheck() || moves[i].IsCapture || moves[i].IsPromotion || (moves[i].MovePieceType == PieceType.Pawn && (moves[i].TargetSquare.Rank == 6 || moves[i].TargetSquare.Rank == 1)))) evaluation = -Search(board, depth - 1, -beta, -alpha, quiescence);
            board.UndoMove(moves[i]);
            if (evaluation >= beta)
            {
                writeTT(key, beta, 3, depth);
                return beta;
            }
            if (evaluation > alpha)
            {
                hash_flag = 1;
                alpha = evaluation;
            }
        }
        writeTT(key, alpha, hash_flag, depth);
        return alpha;
    }

    //Evaluate: 
    // + Material
    // + Number of legal moves
    // + Number of pieces attacking/protecting
    // + Number of squares controled
    // + Moves if board is empty
    int Evaluate(Board board)
    {
        int value = 0, isWhite = board.IsWhiteToMove ? 1 : -1;
        PieceList[] piecelists = board.GetAllPieceLists();
        for (int i = 0; i < piecelists.Length; i++)
        {
            bool white = piecelists[i].IsWhitePieceList;
            int whiteInt = white ? 1 : -1, listCount = piecelists[i].Count;
            PieceType type = piecelists[i].TypeOfPieceInList;
            for (int index = 0; index < listCount; index++)
            {
                Piece piece = piecelists[i].GetPiece(index);
                ulong controled_squared = BitboardHelper.GetPieceAttacks(type, piece.Square, board, white);
                value += whiteInt * (BitboardHelper.GetNumberOfSetBits(controled_squared) + BitboardHelper.GetNumberOfSetBits(controled_squared & board.AllPiecesBitboard) + BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPieceAttacks(type, piece.Square, 0, white)) + getPieceValue(piece, BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard) < 16));
            }
        }

        if (board.TrySkipTurn())
        {
            value += isWhite * (board.GetLegalMoves().Length + board.GetLegalMoves(true).Length);
            board.UndoSkipTurn();
            value -= isWhite * (board.GetLegalMoves().Length + board.GetLegalMoves(true).Length);
        }

        return isWhite * value;
    }

    void OrderMove(Move[] moves)
    {
        for (int i = 0; i < moves.Length; i++)
        {
            for (int j = moves.Length - 1; j > i; j--)
            {
                if (CompareMove(moves[j], moves[j - 1]))
                {
                    Move temp = moves[j];
                    moves[j] = moves[j - 1];
                    moves[j - 1] = temp;
                }
            }
        }
    }

    //Get value from piece square table
    int getPieceValue(Piece piece, bool isEndGame)
    {
        Square square = piece.Square;
        int type = (int)piece.PieceType;
        return (int)((piece_square_table[square.File + (piece.IsWhite ? square.Rank : 7 - square.Rank) * 8] & 0x0000000F << (type + (type == 6 && isEndGame ? 1 : 0))) >> type) + pieceValues[type];
    }

    //Most Valuable Victim - Least Valuable Aggressor (MVV-LVA) and promotion
    bool CompareMove(Move move1, Move move2)
    {
        int capture1 = (int)move1.CapturePieceType, capture2 = (int)move2.CapturePieceType;
        return pieceValues[capture1] - pieceValues[(int)move1.MovePieceType] > pieceValues[capture2] - pieceValues[(int)move2.MovePieceType] || capture1 > capture2 || (int)move1.PromotionPieceType > (int)move2.PromotionPieceType;
    }

    //Always overwrite transposition table entries
    void writeTT(ulong key, int evaluation, int hash_flag, int depth)
    {
        TranspositionTable[key % 0x40000] = new HashNode(key, evaluation, hash_flag, depth);
    }

    int readTT(ulong hash_key, int alpha, int beta, int depth)
    {
        HashNode hash_entry = TranspositionTable[hash_key % 0x40000];
        if (hash_entry != null)
        {
            if (hash_entry.key == hash_key && hash_entry.depth >= depth)
            {
                if (hash_entry.flag == 1) return hash_entry.evaluation;
                if (hash_entry.flag == 2 && hash_entry.evaluation <= alpha) return alpha;
                if (hash_entry.flag == 3 && hash_entry.evaluation >= beta) return beta;
            }
        }
        return -2147483648;
    }
}