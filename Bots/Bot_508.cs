namespace auto_Bot_508;
using ChessChallenge.API;
using System.Linq;

public class Bot_508 : IChessBot
{
    Timer timerReference;
    float millisecondLimit = 900;

    bool ShouldAbort => timerReference.MillisecondsElapsedThisTurn > millisecondLimit;

    int numPositionsSearched = 0;

    public Move Think(Board board, Timer timer)
    {
        if (timer.MillisecondsRemaining < 5000)
            millisecondLimit = timer.MillisecondsRemaining / 10;

        System.Array.Clear(table);

        bestMove = Move.NullMove;
        timerReference = timer;

        Move bestReturnedMove = Move.NullMove;

        for (int depth = 1; depth < 0xFFFFF; depth++)
        {
            numPositionsSearched = 0;

            Search(board, float.MinValue, float.MaxValue, depth, 0, 0);

            if (ShouldAbort)
            {
                if (numPositionsSearched > 0)
                    bestReturnedMove = bestMove;

                break;
            }

            bestReturnedMove = bestMove;
        }

        return bestReturnedMove;
    }

    struct Transposition { public ulong MyKey; public float Value; public int depth, bound; public Move Move; }

    Transposition[] table = new Transposition[1048576];

    static float[][] pieceSquareTables = new ulong[]
    {
        6004234345560363859, 5648800866043598468, 5287348508207109968,
        5214140734727674444, 5140953929278902854, 5576676063466049862,
        5216961999590216514, 6004234345560363859, 2183158892895282944,
        5428933629868195631, 7599987637732405564, 6799439719732960335,
        5719111249516254541, 5431442706592780104, 5353755569969510725,
        5209052108559894815, 5717106727727027525, 4349476007150508870,
        5937552341517035083, 5933323624174671441, 6149763578822548048,
        6367071013902703443, 6081089126010674005, 5278303757615780419,
        7594010649456502883, 7593198144808051553, 6589446149866806609,
        5282552348241514055, 5212441959012845121, 4850749899455285053,
        3481380726304754493, 5062423506043817290, 7667775776126817093,
        7953761885752475719, 8100690822918785869, 6076020360091485766,
        5932737498073482831, 6222378543415710796, 6076019277911641922,
        4198559145840822867, 6508900166728900403, 4990077788217758562,
        5214700291338035023, 4705214069258144075, 4198266606625116987,
        5065498710680030284, 6293863209771161428, 6511999827110880588,
        6004234345560363859, 12801083494916860588, 9042223576844764546,
        6655288161471192931, 6004792884531976282, 5716001770401978197,
        5788343068572211034, 6004234345560363859, 2464672155112914998,
        4127345989516742471, 4560544792049109319, 5356848543625532747,
        5356288879355449418, 5208784983473476168, 4415860052537002302,
        3691344518961445445, 5137287006292691276, 5499261635876311375,
        6148352827876134740, 6076860417258903634, 5715728009338180944,
        5498988991536910925, 5065510908773550668, 5427201829996351304,
        6221539650555369562, 6149477628372474457, 5931612710697916247,
        6076293043422123349, 5642817168372880981, 5426641083395298129,
        5930764961429278800, 5284214749474935887, 6726228716304621135,
        6008470888769609035, 6367356960225384009, 7309455927740948053,
        6874573734370173258, 6221824419688040011, 4846234162249877576,
        4560250113905673539, 3113464301390607659, 3836866029771374389,
        4560267758152141119, 5283669486532907849, 5283669486532907849,
        4560267758152141119, 3836866029771374389, 3113464301390607659
    }.Select(a => new float[8].Select((b, i) => (float)(((a >> i * 8) & 0b11111111) * 2) - 166))
        .SelectMany(a => a)
        .Chunk(64)
        .Select((a, i) => a.Select(b => b + new float[] { 82, 337, 365, 477, 1025, 0, 94, 281, 297, 512, 936, 0 }[i]).ToArray<float>())
        .ToArray<float[]>();

    float Evaluate(Board board, float movePerspective)
    {
        if (board.IsInCheckmate())
            return -0xFFFFF;

        if (board.IsDraw())
            return 0;

        int gamePhase = 0;

        (float wMid, float wEnd) = PESTO(board, true, ref gamePhase);
        (float bMid, float bEnd) = PESTO(board, false, ref gamePhase);

        float pesto = ((wMid - bMid) * gamePhase + (wEnd - bEnd) * (24 - gamePhase)) * movePerspective / 24;

        return pesto;
    }

    int[] gamePhaseIncrement = new int[] { 0, 1, 1, 2, 4, 0 };

    (float, float) PESTO(Board board, bool isWhite, ref int gamePhase)
    {
        float mid = 0, end = 0;

        for (int pieceType = 1; pieceType <= 6; pieceType++)
        {
            PieceList pieceList = board.GetPieceList((PieceType)pieceType, isWhite);

            gamePhase += gamePhaseIncrement[pieceType - 1] * pieceList.Count;

            for (int index = 0; index < pieceList.Count; index++)
            {
                int square = pieceList.GetPiece(index).Square.Index;

                if (isWhite)
                {
                    square = 63 - square;
                }

                mid += pieceSquareTables[pieceType - 1][square];
                end += pieceSquareTables[pieceType + 5][square];
            }
        }

        return (mid, end);
    }

    Move bestMove;

    float Quiesce(Board node, float alpha, float beta)
    {
        if (ShouldAbort)
            return 0;

        float evaluation = Evaluate(node, node.IsWhiteToMove ? 1 : -1);

        if (evaluation >= beta)
            return beta;
        if (evaluation > alpha)
            alpha = evaluation;

        Move[] loudMoves = node.GetLegalMoves(true).OrderByDescending(a => (int)a.CapturePieceType).ToArray<Move>();

        foreach (Move move in loudMoves)
        {
            node.MakeMove(move);

            float score = -Quiesce(node, -beta, -alpha);

            node.UndoMove(move);

            if (score >= beta)
                return beta;
            if (score > alpha)
                alpha = score;
        }

        return alpha;
    }

    public float Search(Board node, float alpha, float beta, int depth, int ply, int numExtensions)
    {
        if (ShouldAbort)
            return 0;

        ulong entryIndex = node.ZobristKey % 1048576;

        Transposition entry = table[entryIndex];

        if (entry.MyKey == node.ZobristKey && entry.depth >= depth)
        {
            float transpositionEval = entry.Value;

            if (entry.bound == 0
                || (entry.bound == 1 && transpositionEval <= alpha)
                || (entry.bound == 2 && transpositionEval >= beta))
            {
                if (ply == 0)
                    bestMove = entry.Move;

                return transpositionEval;
            }
        }

        if (depth == 0)
            return Quiesce(node, alpha, beta);

        Move primaryMove = ply == 0 ? bestMove : entry.Move,
            localBestMove = Move.NullMove;

        Move[] legalMoves = node.GetLegalMoves().OrderByDescending(a => (int)a.CapturePieceType
            + (int)a.PromotionPieceType
            + (a.RawValue == primaryMove.RawValue ? 0xFFFFF : 0)).ToArray<Move>();

        if (legalMoves.Length == 0)
            return node.IsDraw() ? 0 : -0xFFFFF;

        int evalBound = 1;

        foreach (Move move in legalMoves)
        {
            node.MakeMove(move);

            int nodalExtension = node.IsInCheck() ? 1 : 0;

            if (numExtensions > 3)
                nodalExtension = 0;

            float score = -Search(node, -beta, -alpha, depth - 1 + nodalExtension, ply + 1, numExtensions + nodalExtension);

            node.UndoMove(move);

            if (ShouldAbort)
                return 0;

            if (score >= beta)
            {
                Store(entryIndex, node.ZobristKey, beta, depth, 2, move);

                return beta;
            }

            if (score > alpha)
            {
                alpha = score;
                evalBound = 0;

                localBestMove = move;

                if (ply == 0)
                {
                    bestMove = move;

                    numPositionsSearched++;
                }
            }
        }

        Store(entryIndex, node.ZobristKey, alpha, depth, evalBound, localBestMove);

        return alpha;
    }

    void Store(ulong entryIndex, ulong zobrist, float alpha, int depth, int evalBound, Move localBest)
    {
        table[entryIndex].MyKey = zobrist;
        table[entryIndex].Value = alpha;
        table[entryIndex].depth = depth;
        table[entryIndex].bound = evalBound;
        table[entryIndex].Move = localBest;
    }
}