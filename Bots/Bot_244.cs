namespace auto_Bot_244;
using ChessChallenge.API;
using System.Linq;



public class Bot_244 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 }; //do we need that first 0? could be optimised out

    public ulong[] compressedTables =
    {
    17366146244650440448, 2594113994079726336, 864705383484278272, 14555856140260597504, 576470583767985920, 16429383121569637376, 1729409615245470976, 1008822598331263232,
    75718820991656925, 506663723840031743, 17872807851308676332, 13838083924015775209, 15352160939391057905, 17294086516503810584, 649092282722677286, 646556824996277738,
    17439330781821463014, 17440174176295974908, 16932104091068206332, 15132608215315843830, 15276444110593856259, 16355914969945673987, 17437921328845953313, 16571246642289373428,
    14919809804019626981, 18377501121629193470, 16572655056907473147, 15706824830296395020, 15202414005682576401, 15346249913240130310, 16069933099467281674, 14843809387256281319,
    17231035945054959602, 17012608064794464525, 17585981431699673862, 16502849228963460373, 16356760481905845527, 16646684183458301196, 17442990008679469585, 15857697663777380073,
    17824957002454979066, 1757557169678793735, 168042790623323418, 17312945310299341087, 17021614401232524353, 447270576339451704, 1608372409069816089, 16885378433737960684,
    2125450431879886690, 18410715109724837759, 17041600078762887229, 17978362021232714847, 17906304354468370244, 18194538342471384702, 15744542636541871906, 16393070990471589877,
    13763211447769464832, 1657349952344729344, 1152939221895273984, 17366145197852577536, 14411738964386331904, 15997030157144989440, 144117572303319296, 936763210155463936,
    };

    public const byte INVALID = 0, EXACT = 1, LOWERBOUND = 2, UPPERBOUND = 3; //this can be refactored to reduce tokens at the cost of readability
    //not sure why we have INVALID tbh
    struct Transposition
    {
        /*
        public Transposition(ulong zHash, int eval, byte d) //Constructor for a transposition
        {
            zobristHash = zHash;
            evaluation = eval;
            depth = d;
            flag = INVALID;
        }*/

        public ulong zobristHash;
        public int evaluation;
        public byte depth;
        public byte flag;
        public Move bestMove;
    }

    ulong transpositionTableMask = 0x7FFFFF; //011111111111111111111111 in binary we will bitwise AND the mask and the zobrist hash to lop off all the digits except for the last 23 (in binary), 

    //COMPRESSOR
    private sbyte[][] mgPSQT;
    private sbyte[][] egPSQT;

    Transposition[] transpositions;
    public Bot_244()
    {

        mgPSQT = new sbyte[6][];//this can be changed in the future, we don't have to stick to a jagged array of 1d arrays
        egPSQT = new sbyte[6][]; //we could instead have the reading process be different, but this might be faster
        for (int pieceType = 0; pieceType < 6; pieceType++)
        {
            mgPSQT[pieceType] = new sbyte[64]; //don't like this but this is how you do jagged arrays :/
            egPSQT[pieceType] = new sbyte[64];
            for (int square = 0; square < 64; square++)
            {
                /*
                DivertedConsole.Write("Type: " + pieceType);
                DivertedConsole.Write(" |Square: " + square + " | ");
                DivertedConsole.Write(unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF)));*/

                mgPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * pieceType)) & 0xFF));

                int egTableIndex = 0;
                if (pieceType == 0)
                {
                    egTableIndex = 6;
                }
                else if (pieceType == 5)
                {
                    egTableIndex = 7;
                }
                else
                {
                    egTableIndex = pieceType;
                }

                egPSQT[pieceType][square] = unchecked((sbyte)((compressedTables[square] >> (8 * egTableIndex)) & 0xFF));

            }
        }


        transpositions = new Transposition[transpositionTableMask + 1]; // transpositionTableMask + 1 is 100000000000000000000000 in binary

    }

    bool timeout = false;
    public Move Think(Board board, Timer timer)
    {
        timeout = false;
        Move bestMove = IterativeDeepening();


        Move IterativeDeepening() //tmrw, remove the told temp best move and add move ordering to here as well to resolve the early cutoff problem
        {
            Move[] allMoves = board.GetLegalMoves();
            Move bestMove = allMoves[0];

            int searchDepth = 1; //currently with our implementation we're technically doing a 2ply search since we are evaluating the move after the next move

            while (true)
            {
                int bestMoveAdvantage = int.MinValue;
                Move tempBestMove = allMoves[0];   //temp best move so far for this search, but this is may not be the true best move as we have not finished searching yet.

                foreach (Move move in allMoves)
                {
                    if (timeout)
                    {
                        return bestMove;
                    }
                    board.MakeMove(move);
                    int moveAdvantage = -NegaMax(searchDepth, int.MinValue, int.MaxValue - 1);
                    board.UndoMove(move);
                    if (moveAdvantage > bestMoveAdvantage)
                    {
                        bestMoveAdvantage = moveAdvantage;
                        tempBestMove = move;
                    }
                }
                bestMove = tempBestMove; //only once we have finished searching a layer will we update the best move, as we can be sure it is actually better
                searchDepth++;

            }
        }

        int NegaMax(int currentDepth, int alpha, int beta)
        {

            ref Transposition transposition = ref transpositions[board.ZobristKey & transpositionTableMask];

            if (transposition.zobristHash == board.ZobristKey  //checks 2 things, that is has been hashed already (zobristHash is initally set to 0 be default) and that the entry we are getting from the table is hopefully the right transposition
                && transposition.depth >= currentDepth) //a transposition with a greater depth means it got its eval from a deeper search, so its more accurate
            {
                if (transposition.flag == EXACT) return transposition.evaluation;

                //If the value stored is a lower bound, and we have found that it is greater than beta, cut off (or at least I think this is what we are doing)
                if (transposition.flag == LOWERBOUND && transposition.evaluation >= beta) return transposition.evaluation;
                if (transposition.flag == UPPERBOUND && transposition.evaluation <= alpha) return transposition.evaluation;
            }

            int moveTime = timer.MillisecondsRemaining / 30; //arbitary value 
            if (timer.MillisecondsElapsedThisTurn > moveTime)
            {
                timeout = true;
                return alpha; //is this correct?
            }

            if (board.IsInCheckmate())
            {
                return int.MinValue + 1;
            }
            if (board.IsDraw())
            {
                return 0;
            }

            if (currentDepth == 0) //not perfect, this means a search depth of 1 leads to 2ply search
            {
                return CalculateAdvantage();
            }

            Move[] allMoves = board.GetLegalMoves();

            allMoves.OrderByDescending(move => CalculatePriorityOfMove(move)); //seen array.sort also used, need to see if that is better
            //also look into not passing board as arg, am too tired to look at the alternative

            Move bestMove = allMoves[0];
            int bestEval = int.MinValue;

            transposition.flag = UPPERBOUND;

            foreach (Move move in allMoves)
            {
                board.MakeMove(move); ;
                int eval = -NegaMax(currentDepth - 1, -beta, -alpha);
                if (eval > bestEval)
                {
                    bestEval = eval;
                    bestMove = move;
                }
                board.UndoMove(move);

                if (bestEval > alpha) //look into reworking this to use Max(), but will have to change flags
                {
                    alpha = bestEval;
                    transposition.flag = EXACT;
                }

                if (alpha >= beta)
                {
                    transposition.flag = LOWERBOUND;
                    break; //seen some return beta here, idk why though
                }
            }

            transposition.evaluation = bestEval;
            transposition.zobristHash = board.ZobristKey;
            transposition.depth = (byte)currentDepth;
            transposition.bestMove = bestMove;

            return bestEval;
        }

        int CalculatePriorityOfMove(Move move) //We want probably good moves to be checked first for better pruning
        {

            int priority = 0;

            Transposition transP = transpositions[board.ZobristKey & transpositionTableMask];

            if (transP.bestMove == move && board.ZobristKey == transP.zobristHash)
                priority = 10000; //rando big number
            if (move.IsCapture) priority += pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1]; //seen elseif used instead, not sure

            //could transposition depth also be used? mayb

            return priority;

            //optimise this in terms of tokens later
        }

        int CalculateAdvantage()
        {

            int mgWhiteAdvantage = 0, egWhiteAdvantage = 0, gamePhase = 0;
            ulong bitboard = board.AllPiecesBitboard;


            while (bitboard != 0) //learnt this trick from tyrant <3
            {

                int pieceIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref bitboard);
                Piece piece = board.GetPiece(new Square(pieceIndex));

                egWhiteAdvantage +=
                (egPSQT[(int)piece.PieceType - 1] //gets the piece square table of the current piece
                [pieceIndex ^ 56 * (piece.IsWhite ? 0 : 1)] //gets the square of that piece, flips rank if black
                + pieceValues[(int)piece.PieceType])
                * (piece.IsWhite ? 1 : -1); //negates if black


                mgWhiteAdvantage +=
                (egPSQT[(int)piece.PieceType - 1] //gets the piece square table of the current piece
                [pieceIndex ^ 56 * (piece.IsWhite ? 0 : 1)] //gets the square of that piece, flips rank if black
                + pieceValues[(int)piece.PieceType])
                * (piece.IsWhite ? 1 : -1); //negates if black;

                gamePhase += 0x00042110 >> ((int)piece.PieceType - 1) * 4 & 0x0F; //thanks bbg tyrant :*
            };

            return (mgWhiteAdvantage * gamePhase + egWhiteAdvantage * (24 - gamePhase)) / (board.IsWhiteToMove ? 24 : -24); //voodo shit from tyrant :3
        }

        return bestMove;
    }
}