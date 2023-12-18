namespace auto_Bot_423;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_423 : IChessBot
{
    //Main variables
    private Board _board;
    private Move _mainMove;
    private Timer _timer;
    private float _timeThisTurn;

    //Tables
    private (ulong, short, sbyte, Move, int)[] _transpositionTable = new (ulong, short, sbyte, Move, int)[0x800000];
    private int[,,] _historyTable;

    public Move Think(Board board, Timer timer)
    {
        _historyTable = new int[2, 7, 64];

        _board = board;
        _timer = timer;

        _timeThisTurn = Math.Min(timer.MillisecondsRemaining / 28, timer.IncrementMilliseconds + (0.6f + (0.04f * Math.Min(board.PlyCount, 15))) * timer.GameStartTimeMilliseconds / 72f);

        for (int depth = 0; ;)
            if (Search(-1, ++depth, -10000, 10000, true) > 10000 || _timer.MillisecondsElapsedThisTurn > _timeThisTurn)
                break;

        return _mainMove;
    }

    private int Search(int ply, int depth, int alpha, int beta, bool nullMove)
    {
        if (++ply != 0 && _board.IsRepeatedPosition())
            return 0;

        //Search extension for checks
        if (_board.IsInCheck())
            depth++;

        //Transpositions
        ref var transposition = ref _transpositionTable[_board.ZobristKey & 0x7FFFFF];

        if (transposition.Item1 == _board.ZobristKey && ply != 0 && transposition.Item3 >= depth && (transposition.Item5 == 1 || (transposition.Item5 == 0 && transposition.Item2 <= alpha) || (transposition.Item5 == 2 && transposition.Item2 >= beta)))
            return transposition.Item2;

        int currentEvaluation = Eval(), bestEvaluation = -100000000, white = _board.IsWhiteToMove ? 0 : 1, startAlpha = alpha, evaluation = 0;
        bool quiescenceSearch = depth <= 0, inCheck = _board.IsInCheck(), notFirstMove = false;

        if (quiescenceSearch)
        {
            bestEvaluation = currentEvaluation;

            //Check for cutoff
            if (currentEvaluation >= beta)
                return beta;

            alpha = Math.Max(alpha, currentEvaluation);
        }
        else if (!inCheck)
        {
            int factor = currentEvaluation - 85 * depth;

            if (depth < 4 && factor >= beta)
                return factor;

            if (nullMove && depth > 1 && ply < 60)
            {
                _board.TrySkipTurn();
                int score = -Search(ply, depth - 2 - depth / 5, -beta, -beta + 1, false);
                _board.UndoSkipTurn();

                if (score >= beta)
                    return score;
            }
        }

        //Initialize for new searches
        Move bestMove = Move.NullMove, transpositionMove = transposition.Item4;

        //Order moves
        var moves = _board.GetLegalMoves(quiescenceSearch && !inCheck).OrderByDescending(move => move == transpositionMove ? 100000 : move.IsCapture ? 500 * (_basicPieceValue[(int)move.CapturePieceType] + _basicPieceValue[(int)move.PromotionPieceType]) - _basicPieceValue[(int)move.MovePieceType] : _historyTable[white, (int)move.MovePieceType, move.TargetSquare.Index]).ToArray();

        //Loop through all available moves
        foreach (Move move in moves)
        {
            bool tactical = move.IsCapture || move.IsPromotion || !notFirstMove;

            //Futility Pruning
            if (depth <= 8 && currentEvaluation + 40 + 60 * depth <= alpha && !tactical && !quiescenceSearch && !inCheck && beta - alpha <= 1)
                continue;

            _board.MakeMove(move);

            bool pruneSearach = depth > 1 + (ply <= 1 ? 1 : 0) && !tactical;

            if (pruneSearach)
            {
                evaluation = -Search(ply, Math.Max(depth - 2, 0), -alpha - 1, -alpha, nullMove);

                if (evaluation > alpha)
                    pruneSearach = false;
            }

            if (!pruneSearach)
                evaluation = -Search(ply, depth - 1, -beta, -alpha, nullMove);

            _board.UndoMove(move);

            //Set best move
            if (evaluation > bestEvaluation)
            {
                bestMove = move;
                bestEvaluation = evaluation;

                if (ply == 0)
                    _mainMove = move;

                //Set alpha
                alpha = Math.Max(alpha, bestEvaluation);

                //Check if can cut-off
                if (beta <= alpha)
                {
                    if (!quiescenceSearch && !move.IsCapture)
                        _historyTable[white, (int)move.MovePieceType, move.TargetSquare.Index] += depth * depth;

                    break;
                }
            }

            notFirstMove = true;

            if (_timer.MillisecondsElapsedThisTurn > _timeThisTurn)
                return 200000;
        }

        if (!quiescenceSearch && moves.Length == 0)
            bestEvaluation = inCheck ? ply - 100000 : 0;

        //Set transposition
        transposition = (_board.ZobristKey, (short)bestEvaluation, (sbyte)depth, bestMove, bestEvaluation >= beta ? 2 : bestEvaluation > startAlpha ? 1 : 0);

        return bestEvaluation;
    }

    private int Eval()
    {
        int middlegame = -30, endgame = 0, gamephase = 0, sideToMove = 2;

        for (; --sideToMove >= 0;)
        {
            for (int piece = -1; ++piece < 6;)
                for (ulong mask = _board.GetPieceBitboard((PieceType)piece + 1, sideToMove > 0); mask != 0;)
                {
                    //Increase gamephase
                    gamephase += _pieceWeight[piece];

                    //Bishop pair bonus
                    if (piece == 2)
                    {
                        middlegame += 23;
                        endgame += 35;
                    }

                    //Material and square evaluation
                    int square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sideToMove;
                    middlegame += _squarePieceValues[square][piece];
                    endgame += _squarePieceValues[square][piece + 6];
                }

            middlegame = -middlegame;
            endgame = -endgame;
        }
        return (middlegame * gamephase + endgame * (24 - gamephase)) / 24 * (_board.IsWhiteToMove ? 1 : -1);
    }

    public Bot_423()
    {
        //Setup Square values for each piece
        _squarePieceValues = _rawPositionValues.Select(packedTable =>
        {
            int pieceType = 0;
            return decimal.GetBits(packedTable).Take(3).SelectMany(c => BitConverter.GetBytes(c).Select(square => (int)((sbyte)square * 2f) + _pieceValues[pieceType++])).ToArray();
        }).ToArray();
    }

    //Weights

    private readonly short[] _pieceValues = { 82, 337, 365, 497, 1025, 20000, 94, 281, 297, 512, 1000, 20000 };

    private readonly short[] _basicPieceValue = { 0, 1, 3, 3, 5, 9, 7 };

    private readonly int[] _pieceWeight = { 0, 1, 1, 2, 4, 0 };

    private readonly decimal[] _rawPositionValues =
    {
        69341637791733331988582149888m,
        74304283263169330218347253248m,
        74311550985762928484295175168m,
        78947776892082845700348508416m,
        77396701435166060466262773504m,
        78321501260234236525087220736m,
        307114381126600598230913024m,
        72441295294622804225804516864m,
        77992711032004993849258538017m,
        2500153078922453279021203760m,
        3758644912480929539623884842m,
        1915014148312645795317814835m,
        4398147725955332849454159924m,
        6232055118867755427542734890m,
        5602190673975355739581580329m,
        1864168168560650934056127009m,
        623871964619220739711762956m,
        5897206353389487955626693648m,
        6228461528753404153768975124m,
        6847422029801276644865549082m,
        7478453009757126160595495707m,
        6844961731479994533257947168m,
        6821973195819121558173655322m,
        3100890059433511351022259727m,
        78311867670589736752242361085m,
        5597392898407713720278254590m,
        6842605327094232424576586240m,
        8714013161948705130332757506m,
        8722447234699853663594419212m,
        8095000548484048293329049612m,
        6847374916884176066715255818m,
        3428518401707872421211602173m,
        76456152315182342012369959668m,
        5287907832883318488006132475m,
        5911718352820349138394024442m,
        8093824698036259725589416461m,
        8398464559917956665976032782m,
        6844990158986685295586970878m,
        4668890552680886552802626563m,
        2184543085961860879340338932m,
        75516802767218294850094166779m,
        320407954251274566007979256m,
        3433392013903642678668952825m,
        5285475814720248134905627911m,
        5287884258519157579822139910m,
        4354574598840221645349979898m,
        1247635112871657747146737676m,
        78611638624614832901423036667m,
        72421924315999524030484114178m,
        76757160621394117480960946177m,
        78919938357776242956311394812m,
        1864210929890371878615318514m,
        2173677068620754522952827121m,
        918802530676514187102519306m,
        77038793168749249673965795851m,
        74237730712878093674509432836m,
        68086711438183233653663126784m,
        70562605849513866872987250944m,
        73350431088938154802045315072m,
        76745085567188335093240754176m,
        72420743706433626765400797696m,
        76133345453449214101070280704m,
        72095523659802112378051817984m,
        67760301392812082607389333504m
    };

    private readonly int[][] _squarePieceValues = new int[64][];
}