namespace auto_Bot_155;
using ChessChallenge.API;
using System;

public class Bot_155 : IChessBot
{
    int i;
    int j;

    private struct TranspositionEntry
    {
        public ulong ZobristKey;
        public byte Depth;
        public byte Age;
        public short Score;
        public byte Type;

        // 4 bytes (Move*) -> 4 bytes (Chess.Move*) -> 2 bytes (ushort)
        public Move BestMove;

        public TranspositionEntry(ulong zobristKey, byte depth, byte age, short score, byte type, Move bestMove)
        {
            ZobristKey = zobristKey;
            Depth = depth;
            Age = age;
            Score = score;
            Type = type;
            BestMove = bestMove;
        }
    }

    private Board ChessBoard;
    private int Age;
    private Timer Timer;

    // implement transposition table
    // Transposition table just stores the Zobrist Key
    private TranspositionEntry[] TranspositionTable = new TranspositionEntry[0x80000]; // <- change this if table is too big
    private TranspositionEntry TableEntry => TranspositionTable[ZobristIndex];
    private ulong ZobristIndex => ChessBoard.ZobristKey & 0x7FFFF; // <- change this if table is too big

    private int MoveTime = 500;

    // data for neural network
    // data consists of 16 bit weights stored in sets of 4 to be unpacked into 32 bit floats
    private ulong[] data = { 0x3c003c003c003c00, 0x3c003c003c003c00, 0x3a663c663c663c33, 0x3c333c663c663a66, 0x3c003b333b9a3c33, 0x3c333b9a3b333c00, 0x3ccd3c003c003c00, 0x3c003c003c003ccd, 0x3d003c663c333c33, 0x3c333c333c663d00, 0x3d333ccd3c663c66, 0x3c663c663ccd3d33, 0x3e003e003e003e00, 0x3e003e003e003e00, 0x3c003c003c003c00, 0x3c003c003c003c00, 0x41cd41cd419a4166, 0x4166419a41cd41cd, 0x428042664200419a, 0x419a420042664280, 0x42b3429a428041cd, 0x41cd4280429a42b3, 0x42cd42b3426641cd, 0x41cd426642b342cd, 0x42cd42b3428041cd, 0x41cd428042b342cd, 0x42b3429a426641cd, 0x41cd4266429a42b3, 0x426642664200419a, 0x419a420042664266, 0x41cd41cd419a4166, 0x4166419a41cd41cd, 0x4266426642664233, 0x4233426642664266, 0x429a429a42b34266, 0x426642b3429a429a, 0x42cd42cd42cd4266, 0x426642cd42cd42cd, 0x42cd42cd429a4266, 0x4266429a42cd42cd, 0x42cd42b342b34266, 0x426642b342b342cd, 0x42cd42b3429a4266, 0x4266429a42b342cd, 0x429a429a429a4266, 0x4266429a429a429a, 0x4266426642664233, 0x4233426642664266, 0x450d450045004500, 0x450045004500450d, 0x45004500450044f3, 0x44f3450045004500, 0x45004500450044f3, 0x44f3450045004500, 0x45004500450044f3, 0x44f3450045004500, 0x45004500450044f3, 0x44f3450045004500, 0x45004500450044f3, 0x44f3450045004500, 0x451a451a451a450d, 0x450d451a451a451a, 0x4500450045004500, 0x4500450045004500, 0x487a487348734866, 0x486648734873487a, 0x4880488048804873, 0x4873488048864880, 0x4886488648804873, 0x4873488648864886, 0x488648864880487a, 0x4880488048864886, 0x488648864880487a, 0x487a488048864886, 0x4886488648804873, 0x4873488048864886, 0x4880488048804873, 0x4873488048804880, 0x487a487348734866, 0x486648734873487a, 0x2e6638003266, 0x326638002e660000, 0xb266b26632663266, 0x32663266b266b266, 0xb266b266b266ae66, 0xae66b266b266b266, 0xb666b4cdb4cdb266, 0xb266b4cdb4cdb666, 0xb800b666b666b4cd, 0xb4cdb666b666b800, 0xb8cdb800b800b666, 0xb666b800b800b8cd, 0xb8cdb8cdb8cdb8cd, 0xb8cdb8cdb8cdb8cd, 0xb99ab99ab99aba66, 0xba66b99ab99ab99a, 0xbc00bc00bc00bc00, 0xbc00bc00bc00bc00, 0xbe00be00be00be00, 0xbe00be00be00be00, 0xbd33bccdbc66bc66, 0xbc66bc66bccdbd33, 0xbd00bc66bc33bc33, 0xbc33bc33bc66bd00, 0xbccdbc00bc00bc00, 0xbc00bc00bc00bccd, 0xbc00bb33bb9abc33, 0xbc33bb9abb33bc00, 0xba66bc66bc66bc33, 0xbc33bc66bc66ba66, 0xbc00bc00bc00bc00, 0xbc00bc00bc00bc00, 0xc1cdc1cdc19ac166, 0xc166c19ac1cdc1cd, 0xc266c266c200c19a, 0xc19ac200c266c266, 0xc2b3c29ac266c1cd, 0xc1cdc266c29ac2b3, 0xc2cdc2b3c280c1cd, 0xc1cdc280c2b3c2cd, 0xc2cdc2b3c266c1cd, 0xc1cdc266c2b3c2cd, 0xc2b3c29ac280c1cd, 0xc1cdc280c29ac2b3, 0xc280c266c200c19a, 0xc19ac200c266c280, 0xc1cdc1cdc19ac166, 0xc166c19ac1cdc1cd, 0xc266c266c266c233, 0xc233c266c266c266, 0xc29ac29ac29ac266, 0xc266c29ac29ac29a, 0xc2cdc2b3c29ac266, 0xc266c29ac2b3c2cd, 0xc2cdc2b3c2b3c266, 0xc266c2b3c2b3c2cd, 0xc2cdc2cdc29ac266, 0xc266c29ac2cdc2cd, 0xc2cdc2cdc2cdc266, 0xc266c2cdc2cdc2cd, 0xc29ac29ac2b3c266, 0xc266c2b3c29ac29a, 0xc266c266c266c233, 0xc233c266c266c266, 0xc500c500c500c500, 0xc500c500c500c500, 0xc51ac51ac51ac50d, 0xc50dc51ac51ac51a, 0xc500c500c500c4f3, 0xc4f3c500c500c500, 0xc500c500c500c4f3, 0xc4f3c500c500c500, 0xc500c500c500c4f3, 0xc4f3c500c500c500, 0xc500c500c500c4f3, 0xc4f3c500c500c500, 0xc500c500c500c4f3, 0xc4f3c500c500c500, 0xc50dc500c500c500, 0xc500c500c500c50d, 0xc87ac873c873c866, 0xc866c873c873c87a, 0xc880c880c880c873, 0xc873c880c880c880, 0xc886c886c880c873, 0xc873c880c886c886, 0xc886c886c880c87a, 0xc87ac880c886c886, 0xc886c886c880c880, 0xc87ac880c886c886, 0xc886c886c886c873, 0xc873c880c886c886, 0xc880c886c880c873, 0xc873c880c880c880, 0xc87ac873c873c866, 0xc866c873c873c87a, 0x399a399a399a3a66, 0x3a66399a399a399a, 0x38cd38cd38cd38cd, 0x38cd38cd38cd38cd, 0x38cd380038003666, 0x36663800380038cd, 0x38003666366634cd, 0x34cd366636663800, 0x366634cd34cd3266, 0x326634cd34cd3666, 0x3266326632662e66, 0x2e66326632663266, 0x32663266b266b266, 0xb266b26632663266, 0x8000ae66b800b266, 0xb266b800ae668000 };

    private int dataPointer = 0;
    private float[] weights = new float[768];

    public Bot_155()
    {
        // setup neural network

        // convert data to bytes
        byte[] dataBytes = new byte[data.Length * 8];
        Buffer.BlockCopy(data, 0, dataBytes, 0, dataBytes.Length);

        // setup neural network
        for (i = 0; i < 768; i++)
        {
            weights[i] = (float)BitConverter.ToHalf(dataBytes, dataPointer);
            dataPointer += 2;
        }
    }

    public Move Think(Board board, Timer timer)
    {

        ChessBoard = board;
        Timer = timer;

        MoveTime = getMoveTime();

        Age = ChessBoard.PlyCount;

        // iterative deepening
        int depth = 1;
        while (Timer.MillisecondsElapsedThisTurn < MoveTime)
            AlphaBetaSearch(-0x7fffffff, 0x7fffffff, depth++);

        return TableEntry.BestMove;
    }


    private int AlphaBetaSearch(int alpha, int beta, int depth)
    {
        /*
        beta is the best value the maximizer (we) can guarantee given that the minimizer (opponent) plays optimally,
        so we can never get a score higher than beta.

        alpha is the best value the minimizer (opponent) can guarantee given that the maximizer (we) plays optimally,
        so we want to improve alpha as much as possible.
        */

        if (ChessBoard.IsInCheckmate())
            return -0x7fffffff;

        if (ChessBoard.IsDraw())
            return 0;

        bool onlyCaptures = depth == 0;
        if (onlyCaptures)
        {
            depth = 1;

            int standingPat = Eval();
            if (standingPat >= beta)
                return standingPat;
            if (alpha < standingPat)
                alpha = standingPat;
        }

        // transposition table
        ulong zobristKey = ChessBoard.ZobristKey;

        bool inTable = TableEntry.ZobristKey == zobristKey;
        if (inTable && depth <= TableEntry.Depth &&
            (TableEntry.Type == 0 ||
            TableEntry.Type == 1 && TableEntry.Score >= beta ||
            TableEntry.Type == 2 && TableEntry.Score <= alpha))
            return TableEntry.Score;

        Move[] moves = ChessBoard.GetLegalMoves(onlyCaptures);
        if (moves.Length == 0)
            return Eval();

        OrderMoves(moves, inTable);

        // assume an all node
        byte nodeType = 2;
        Move bestMove = moves[0];

        foreach (Move move in moves)
        {
            ChessBoard.MakeMove(move);
            int score = -AlphaBetaSearch(-beta, -alpha, depth - 1);
            ChessBoard.UndoMove(move);

            // if we run out of time, return the current best move
            if (Timer.MillisecondsElapsedThisTurn >= MoveTime)
                return -0x7fffffff;

            // if we score higher than beta, the opponent will never allow us to get this position
            if (score >= beta)
            {
                // prune
                alpha = score;

                nodeType = 1;
                bestMove = move;
                break;
            }
            // if we score higher than alpha, we can improve our score
            if (score > alpha)
            {
                alpha = score;

                nodeType = 0;
                bestMove = move;
            }
        }

        // add to transposition table
        if (depth > TableEntry.Depth || TableEntry.Age < Age)
            TranspositionTable[ZobristIndex] = new TranspositionEntry(zobristKey, (byte)depth, (byte)ChessBoard.PlyCount, (short)alpha, nodeType, bestMove);

        return alpha;
    }

    private void OrderMoves(Move[] moves, bool useTable)
    {
        // order moves based on MVV-LVA
        // most valuable victim - least valuable attacker

        int[] scores = new int[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];

            int score = 0;

            if (move.CapturePieceType != PieceType.None)
                score = 1000 * (int)move.CapturePieceType - (int)move.MovePieceType;

            if (useTable && move == TableEntry.BestMove)
                score += 1000000;

            // we sort in descending order, so we need to negate the score
            scores[i] = -score;
        }

        Array.Sort(scores, moves);
    }

    private int Eval()
    {
        // material
        float score = getNeuralNetworkScores() * 1000;

        return (int)(ChessBoard.IsWhiteToMove ? score : -score);
    }

    private float getNeuralNetworkScores()
    {
        PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };

        dataPointer = 0;

        float score = 0;
        for (i = 0; i < 12; i++)
        {
            ulong pieces = ChessBoard.GetPieceBitboard(pieceTypes[i % 6], i < 6);
            for (j = 0; j < 64; j++)
                score += (pieces >> j & 1) * weights[dataPointer++];
        }

        return score;
    }

    private int getMoveTime()
    {
        // calculate the expected number of moves remaining
        int num_moves = 20 + 70 * BitboardHelper.GetNumberOfSetBits(ChessBoard.AllPiecesBitboard) / 32;

        return Timer.IncrementMilliseconds + Timer.MillisecondsRemaining / num_moves + 1;
    }
}