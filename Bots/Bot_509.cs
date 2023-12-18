namespace auto_Bot_509;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_509 : IChessBot
{
    private struct Transposition
    {
        public ulong Hash;
        public Move Move;
        public int Depth;
        public int Score;
        public int Flag;
    }

    private readonly Transposition[] _transpositionTable = new Transposition[0x800000];
    private Move[] _killerHeuristic;
    private int[] _historyHeuristic;

    private static readonly int[] PieceValues = {
            /* Middlegame */ 77,302, 310, 434, 890, 0,
            /* Endgame */ 109, 331, 335, 594, 1116, 0, },
            MoveScores = new int[218], UnpackedPestoTables = new[] {
            59445390105436474986072674560m, 70290677894333901267150682880m, 71539517137735599738519086336m, 78957476706409475571971323392m, 76477941479143404670656189696m, 78020492916263816717520067072m, 77059410983631195892660944640m, 61307098105356489251813834752m,
            77373759864583735626648317994m, 3437103645554060776222818613m, 5013542988189698109836108074m, 2865258213628105516468149820m, 5661498819074815745865228343m, 8414185094009835055136457260m, 7780689186187929908113377023m, 2486769613674807657298071274m,
            934589548775805732457284597m, 4354645360213341838043912961m, 8408178448912173986754536726m, 9647317858599793704577609753m, 9972476475626052485400971547m, 9023455558428990305557695533m, 9302688995903440861301845277m, 4030554014361651745759368192m,
            78006037809249804099646260205m, 5608292212701744542498884606m, 9021118043939758059554412800m, 11825811962956083217393723906m, 11837863313235587677091076880m, 11207998775238414808093699594m, 9337766883211775102593666830m, 4676129865778184699670239740m,
            75532551896838498151443462373m, 3131203134016898079077499641m, 8090231125077317934436125943m, 11205623443703685966919568899m, 11509049675918088175762150403m, 9025911301112313205746176509m, 6534267870125294841726636036m, 3120251651824756925472439792m,
            74280085839011331528989207781m, 324048954150360030097570806m, 4681017700776466875968718582m, 7150867317927305549636569078m, 7155688890998399537110584833m, 5600986637454890754120354040m, 1563108101768245091211217423m, 78303310575846526174794479097m,
            70256775951642154667751105509m, 76139418398446961904222530552m, 78919952506429230065925355250m, 2485617727604605227028709358m, 3105768375617668305352130555m, 1225874429600076432248013062m, 76410151742261424234463229975m, 72367527118297610444645922550m,
            64062225663112462441888793856m, 67159522168020586196575185664m, 71185268483909686702087266048m, 75814236297773358797609495296m, 69944882517184684696171572480m, 74895414840161820695659345152m, 69305332238573146615004392448m, 63422661310571918454614119936m,
        }.SelectMany(packedTable =>
        decimal.GetBits(packedTable).SelectMany(BitConverter.GetBytes)
                    // No point in only taking 12 bytes. Since we never access the last 4 anyway, we can just leave them as garbage
                    .Select((square, index) => (int)((sbyte)square * 1.461) + PieceValues[index % 12])
                .ToArray()
        ).ToArray();

    private Board _board;
    private Timer _timer;

    private int _timeLimit;

#if DEBUG
    private int _nodesSearched;
#endif

    private Move _bestMove;

    private const int KillerEvaluation = 900_000_000;

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;

        _timeLimit = timer.MillisecondsRemaining / 30;
        _bestMove = Move.NullMove;

        _historyHeuristic = new int[4096];
        _killerHeuristic = new Move[2048];

        //search 
        ref var entry = ref _transpositionTable[_board.ZobristKey & 0x7FFFFF];
        var depth = 1;
        while (_timer.MillisecondsElapsedThisTurn < _timeLimit)
        {
#if DEBUG
            _nodesSearched = 0;
#endif
            Search(depth, 0, -KillerEvaluation, KillerEvaluation);
            _bestMove = entry.Move;
#if DEBUG
            //log
            DivertedConsole.Write("depth: {0}, time: {1}, nodes: {2}, {3}, eval: {4}", entry.Depth, _timer.MillisecondsElapsedThisTurn, _nodesSearched, entry.Move, entry.Score);
#endif
            depth++;
        }

        return _bestMove;
    }

    private int Search(int depth, int ply, int alpha, int beta)
    {

        //transposition table cutoff
        ref var entry = ref _transpositionTable[_board.ZobristKey & 0x7FFFFF];

        if (entry.Depth >= depth && entry.Hash == _board.ZobristKey && entry.Score < 800_000_000)
        {
            if (entry.Flag == 1) return entry.Score;
            //If we have a lower bound better than beta, use that
            if (entry.Flag == 2 && entry.Score >= beta) return entry.Score;
            //If we have an upper bound worse than alpha, use that
            if (entry.Flag == 3 && entry.Score <= alpha) return entry.Score;
        }

        var isInCheck = _board.IsInCheck();

        //check extension
        if (isInCheck)
            depth++;


        var inQSearch = depth <= 0;

        if (inQSearch)
        {
            var eval = Evaluate();
            if (eval >= beta) return beta;
            if (alpha < eval) alpha = eval;
        }

        var bestScore = -KillerEvaluation;

        if (!inQSearch)
        {
            //null move pruning
            if (depth >= 3 && !isInCheck)
            {
                _board.TrySkipTurn();
                var score = -Search(depth - 3, ply + 1, -beta, -beta + 1);
                _board.UndoSkipTurn();

                if (score >= beta) return score;
            }

            //reverse futility pruning
            var eval = Evaluate() - 100;
            if (!isInCheck && eval >= beta)
                return eval;
        }

        var legalMoves = _board.GetLegalMoves(inQSearch);

        if (!legalMoves.Any())
            return inQSearch ? alpha : isInCheck ? ply - KillerEvaluation : 0;

        var scores = new int[legalMoves.Length];
        var moveIndex = 0;
        foreach (var move in legalMoves)
        {
            scores[moveIndex++] = -(
                move == _transpositionTable[_board.ZobristKey & 0x7FFFFF].Move ? KillerEvaluation
                : move.IsCapture ? 100_000_000 * ((int)move.CapturePieceType - (int)move.MovePieceType)
                : move == _killerHeuristic[ply] ? 80_000_000
                : _historyHeuristic[move.RawValue & 4095]);
        }

        Array.Sort(scores, legalMoves);

        var movesSearched = 0;
        foreach (var move in legalMoves)
        {
            if (!inQSearch && _timer.MillisecondsElapsedThisTurn > _timeLimit) return 100_000_000;

            _board.MakeMove(move);

            int score;

            if (movesSearched == 0)
            {
                score = -Search(depth - 1, ply + 1, -beta, -alpha);
            }
            else
            {
                if (movesSearched >= 3 && depth > 4 && !inQSearch && !isInCheck)
                {
                    score = -Search(depth - 4, ply + 1, -alpha - 1, -alpha);
                }
                else
                {
                    score = alpha + 1;
                }

                if (score > alpha)
                {
                    score = -Search(depth - 1, ply + 1, -alpha - 1, -alpha);

                    if (score > alpha && score < beta)
                        score = -Search(depth - 1, ply + 1, -beta, -alpha);
                }
            }

            _board.UndoMove(move);
            movesSearched++;

#if DEBUG
            _nodesSearched++;
#endif

            if (score > bestScore)
            {
                bestScore = score;

                if (score > alpha)
                {
                    alpha = score;

                    if (ply == 0)
                    {
                        _bestMove = move;
                    }

                    if (score >= beta)
                    {
                        if (!move.IsCapture)
                        {
                            _killerHeuristic[ply] = move;
                            _historyHeuristic[move.RawValue & 4095] += depth;
                        }

                        return score;
                    }
                }
            }
        }


        if (bestScore <= alpha) entry.Flag = 3; // UpperBound
        else if (bestScore >= beta) entry.Flag = 2; // LowerBound
        else entry.Flag = 1; // Exact

        entry.Hash = _board.ZobristKey;
        entry.Score = bestScore;
        entry.Depth = depth;
        entry.Move = _bestMove;


        return bestScore;
    }

    private int Evaluate()
    {
        //credits to tyrant for the evaluation function!

        int middlegame = 0, endgame = 0, gamephase = 0, sideToMove = 2, piece, square;
        for (; --sideToMove >= 0; middlegame = -middlegame, endgame = -endgame)
            for (piece = 6; --piece >= 0;)
                for (ulong mask = _board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                {
                    // Gamephase, middlegame -> endgame
                    // Multiply, then shift, then mask out 4 bits for value (0-16)
                    gamephase += 0x00042110 >> piece * 4 & 0x0F;

                    // Material and square evaluation
                    square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                    middlegame += UnpackedPestoTables[square * 16 + piece];
                    endgame += UnpackedPestoTables[square * 16 + piece + 6];

                    // Bishop pair bonus
                    if (piece == 2 && mask != 0)
                    {
                        middlegame += 23;
                        endgame += 62;
                    }

                    // Doubled pawns penalty (brought to my attention by Y3737)
                    if (piece == 0 && (0x101010101010101UL << (square & 7) & mask) > 0)
                    {
                        middlegame -= 15;
                        endgame -= 15;
                    }
                }

        // Tempo bonus to help with aspiration windows
        return (middlegame * gamephase + endgame * (24 - gamephase)) / (_board.IsWhiteToMove ? 24 : -24) + 16;
    }
}
