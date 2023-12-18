namespace auto_Bot_486;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_486 : IChessBot
{
    private const sbyte E = 0, L = -1, U = 1, I = -2;
    struct Transposition
    {

        public ulong zobristHash;
        public int depth;
        public int evaluation;
        public sbyte FLAG;
        public Move move;
    }

    private ulong mask = 0x7FFFFF;

    private Transposition[] transpositionTable;

    Move[] killerMoves;

    public Bot_486()
    {
        killerMoves = new Move[1024];
        transpositionTable = new Transposition[mask + 1];
    }

    // empty, pawn, horse, bishop, castle, queen, king
    int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };


    int mDepth;
    int nodes = 0;

    int infinity = 9999999;
    int fraction = 50;

    public Move Think(Board board, Timer timer)
    {
        Transposition bestRootMove = transpositionTable[board.ZobristKey & mask];
        for (int depth = 1; ; depth++)
        {
            mDepth = depth;
            Search(timer, board, depth, -infinity, infinity, 0);
            bestRootMove = transpositionTable[board.ZobristKey & mask];

            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / fraction) break;
        }
        return bestRootMove.move.IsNull ? board.GetLegalMoves()[0] : bestRootMove.move;
    }

    int Evaluate(Board board)
    {

        nodes++;
        int whiteVal = CountMaterial(board, true);
        int blackVal = CountMaterial(board, false);

        int eval = whiteVal - blackVal;

        int perspective = board.IsWhiteToMove ? 1 : -1;

        return eval * perspective;
    }

    int Search(Timer timer, Board board, int depth, int alpha, int beta, int numExtensions)
    {
        if (board.IsInCheckmate() && board.GetLegalMoves().Length == 0)
        {
            int depthFromRoot = mDepth - depth;
            return -infinity + depthFromRoot;
        }
        int bestEval = int.MinValue;
        int startingAlpha = alpha;
        if (board.IsRepeatedPosition())
        {
            return 0;
        }
        if (board.IsDraw()) return -10;

        ref Transposition transposition = ref transpositionTable[board.ZobristKey & mask];
        if (transposition.zobristHash == board.ZobristKey && transposition.depth >= depth && depth < mDepth)
        {

            if (transposition.FLAG == E) return transposition.evaluation;
            else if (transposition.FLAG == L) alpha = Math.Max(alpha, transposition.evaluation);
            else if (transposition.FLAG == U) beta = Math.Min(beta, transposition.evaluation);
            if (alpha >= beta) return transposition.evaluation;
        }


        Span<Move> moves = stackalloc Move[256];
        OrderMoves(board, ref moves, false, depth);

        if (depth == 0)
        {
            return Evaluate(board);
            //return Quiesce(board, alpha, beta);
        }

        Move moving = Move.NullMove;
        foreach (Move move in moves)
        {
            if (timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / fraction) return infinity;
            board.MakeMove(move);
            int extension = numExtensions < 16 && board.IsInCheck() ? 1 : 0;
            int value = -Search(timer, board, depth - 1 + extension, -beta, -alpha, numExtensions + extension);

            board.UndoMove(move);

            if (bestEval < value)
            {
                bestEval = value;

                moving = move;
            }

            alpha = Math.Max(alpha, value);
            if (alpha >= beta)
            {
                break;
            }
        }
        transposition.evaluation = bestEval;
        transposition.zobristHash = board.ZobristKey;
        transposition.move = moving;
        if (bestEval < startingAlpha)
        {
            transposition.FLAG = U;
        }
        else if (bestEval >= beta)
        {
            if (!moving.IsCapture && !moving.IsPromotion)
            {
                killerMoves[depth] = moving;
            }
            transposition.FLAG = L;
        }

        else
        {
            transposition.FLAG = E;
        }
        transposition.depth = depth;

        return bestEval;
    }

    Span<Move> OrderMoves(Board board, ref Span<Move> moves, bool capturesOnly, int depth = 0)
    {
        board.GetLegalMovesNonAlloc(ref moves, capturesOnly);
        Span<int> scores = stackalloc int[moves.Length];
        int i = 0;
        foreach (Move move in moves)
        {
            ref Transposition transposition = ref transpositionTable[board.ZobristKey & mask];
            int scoreGuess = 0;
            if (move == transposition.move && board.ZobristKey == transposition.zobristHash) scoreGuess -= infinity;
            if (move.IsCapture)
            {
                scoreGuess += 1000 + 10 * pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType];
            }

            if (move.IsPromotion)
            {
                scoreGuess += 1000 + 5 * pieceValues[(int)move.PromotionPieceType];
            }
            if (depth >= 0 && move.Equals(killerMoves[depth]))
            {
                scoreGuess += 1;
            }

            scores[i] = scoreGuess;
            i++;
        }

        MemoryExtensions.Sort(scores, moves);
        moves.Reverse();
        /*DivertedConsole.Write("begin : " + counter);
        foreach (Move move in moves)
        {

            DivertedConsole.Write(move);
        }

        DivertedConsole.Write("end : " + counter);*/

        return moves;
    }


    // Big table packed with data from premade piece square tables
    // Unpack using PackedEvaluationTables[set, rank] = file
    private readonly ulong[] PackedEvaluationTables = {
        0, 17876852006827220035, 17442764802556560892, 17297209133870877174, 17223739749638733806, 17876759457677835758, 17373217165325565928, 0,
        13255991644549399438, 17583506568768513230, 2175898572549597664, 1084293395314969850, 18090411128601117687, 17658908863988562672, 17579252489121225964, 17362482624594506424,
        18088114097928799212, 16144322839035775982, 18381760841018841589, 18376121450291332093, 218152002130610684, 507800692313426432, 78546933140621827, 17502669270662184681,
        2095587983952846102, 2166845185183979026, 804489620259737085, 17508614433633859824, 17295224476492426983, 16860632592644698081, 14986863555502077410, 17214733645651245043,
        2241981346783428845, 2671522937214723568, 2819295234159408375, 143848006581874414, 18303471111439576826, 218989722313687542, 143563254730914792, 16063196335921886463,
        649056947958124756, 17070610696300068628, 17370107729330376954, 16714810863637820148, 15990561411808821214, 17219209584983537398, 362247178929505537, 725340149412010486,
        0, 9255278100611888762, 4123085205260616768, 868073221978132502, 18375526489308136969, 18158510399056250115, 18086737617269097737, 0,
        13607044546246993624, 15920488544069483503, 16497805833213047536, 17583469180908143348, 17582910611854720244, 17434276413707386608, 16352837428273869539, 15338966700937764332,
        17362778423591236342, 17797653976892964347, 216178279655209729, 72628283623606014, 18085900871841415932, 17796820590280441592, 17219225120384218358, 17653536572713270000,
        217588987618658057, 145525853039167752, 18374121343630509317, 143834816923107843, 17941211704168088322, 17725034519661969661, 18372710631523548412, 17439054852385800698,
        1010791012631515130, 5929838478495476, 436031265213646066, 1812447229878734594, 1160546708477514740, 218156326927920885, 16926762663678832881, 16497506761183456745,
        17582909434562406605, 580992990974708984, 656996740801498119, 149207104036540411, 17871989841031265780, 18015818047948390131, 17653269455998023918, 16424899342964550108,
    };

    private int GetSquareBonus(int type, bool isWhite, int file, int rank)
    {
        // Mirror vertically for white pieces, since piece arrays are flipped vertically
        if (isWhite)
            rank = 7 - rank;

        // Grab the correct byte representing the value
        // And multiply it by the reduction factor to get our original value again
        return (int)Math.Round(unchecked((sbyte)((PackedEvaluationTables[(type * 8) + rank] >> file * 8) & 0xFF)) * 1.461);
    }
    int CountMaterial(Board board, bool isWhite)
    {
        int middleGame = 0;
        for (var i = PieceType.Pawn; i < PieceType.King; i++)
        {
            PieceList list = board.GetPieceList(i, isWhite);

            middleGame += list.Count * pieceValues[(int)i];

            foreach (Piece piece in list)
            {
                middleGame += GetSquareBonus((int)piece.PieceType, piece.IsWhite, piece.Square.File, piece.Square.Rank);
            }

        }
        return middleGame;
    }

}
