namespace auto_Bot_586;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;


public class Bot_586 : IChessBot
{

    public Move Think(Board board, Timer timer)
    {

        currentBoard = board;
        TurnTimer = timer;

        TimeAllotted = (timer.MillisecondsRemaining - 1000) / 10;

        int depth = 0;
        do
        {
            depth++;
            Search(depth, float.NegativeInfinity, float.PositiveInfinity);
        } while (!SearchCancelled);
        return GetTableMove();
    }

    Timer TurnTimer;
    int TimeAllotted;
    Board currentBoard;
    ulong key => currentBoard.ZobristKey;
    bool SearchCancelled => TurnTimer.MillisecondsElapsedThisTurn > TimeAllotted;

    float Search(int depth, float alpha, float beta)
    {
        if (currentBoard.IsInCheckmate()) return float.NegativeInfinity;
        if (currentBoard.IsDraw()) return -20.21F;

        if (depth == 0)
        {
            ulong[] inputs = Enumerable.Range(1, 6).SelectMany(i => new[] { true, false }.Select(b => currentBoard.GetPieceBitboard((PieceType)i, b))).ToArray();
            bool[] Layer1Activations = Enumerable.Range(0, 16).SelectMany(output =>
                         ConvOutputShifts.Select(shift =>
                             inputs.Select(
                                 (input, index) => BitOperations.PopCount(input & (ConvKernelMask & ~Kernels[output * 12 + index]) << shift) - BitOperations.PopCount(input & Kernels[output * 12 + index] << shift)
                                 ).Sum() < Thresholds[output])).ToArray();

            bool[] Layer2Activations = Activations(Layer1Activations, 0).Select((a, index) => a < Thresholds[16 + index]).ToArray();
            int[] Layer3Activations = Activations(Layer2Activations, 6400).ToArray();

            return
                (currentBoard.GetAllPieceLists().Sum(pieces => pieces.Count * PieceValue[(int)pieces.TypeOfPieceInList] * (pieces.IsWhitePieceList ? 1 : -1))
                + MathF.Tan(
                        Layer3Activations.Select((value, index) => value * FloatWeights[index]).Sum() - 0.0019122446F) * 250)
                * (currentBoard.IsWhiteToMove ? 1 : -1);

        }


        Move BestMove = GetTableMove();
        IEnumerable<Move> Moves = currentBoard.GetLegalMoves().OrderBy(new MoveComparer { BestMove = BestMove }.MoveRating);


        foreach (Move move in Moves)
        {
            currentBoard.MakeMove(move);
            float value = -Search((depth - 1), -beta, -alpha);
            currentBoard.UndoMove(move);
            if (alpha < value)
            {
                BestMove = move;
                alpha = value;
            }
            if (SearchCancelled)
            {
                depth = 0; break;
            }
            if (alpha >= beta) break;
        }


        TranspositionEntry entry = new() { BestMove = BestMove, Depth = depth, Value = alpha };
        if (!Table.TryAdd(key, entry) && Table[key].Replace(entry)) Table[key] = entry;
        RemovalQueue.Enqueue(key);
        while (Table.Count > 8388608)
        {
            ulong KeyToRemove = RemovalQueue.Dequeue();
            if (!RemovalQueue.Contains(KeyToRemove)) Table.Remove(KeyToRemove);
        }

        return alpha;
    }
    static int[] PieceValue = { 0, 294, 660, 727, 1083, 1982, 0 };

    class MoveComparer
    {
        public Move BestMove;
        public int[] HeatMap;

        public int MoveRating(Move move) => (move == BestMove ? 1000 : 0) + PieceValue[(int)move.PromotionPieceType] + PieceValue[(int)move.CapturePieceType] /* - HeatMap[move.TargetSquare.Index] - HeatMap[move.StartSquare.Index] */;
    }

    Dictionary<ulong, TranspositionEntry> Table = new();
    Queue<ulong> RemovalQueue = new();

    Move GetTableMove()
    {
        if (Table.ContainsKey(key))
        {
            RemovalQueue.Enqueue(key);
            return Table[key].BestMove;
        }
        return Move.NullMove;
    }

    struct TranspositionEntry
    {
        public Move BestMove;
        public int Depth;
        public float Value;

        public bool Replace(TranspositionEntry NewEntry) => NewEntry.Depth > Depth || NewEntry.Depth == Depth && NewEntry.Value > Value;
    }

    const ulong ConvKernelMask = 0b1111000011110000111100001111;
    IEnumerable<int> ConvOutputShifts = Enumerable.Range(0, 64).Where(i => (0b1111100011111000111110001111100011111 & (ulong)1 << i) > 0);
    ulong[] Kernels = new ulong[]
    {
        3915603833621852288,
        9294735584586166516,
        3672965861995985928,
        10090886758566397699,
        1084533011153749765,
        18149506494260776719,
        1769987135158415771,
        6985347942356984037,
        14657661025129656560,
        6945880854012010495,
        17770799210302676059,
        1800594320079779056,
        1082877842209509135,
        30408159337082723,
        6318285988535797511,
        10313285960791523456,
        5197190202556022768,
        9223367626674131504,
        846748889741590791,
        5622247228782888737,
        17288157938896290567,
        12722686885012475952,
        8075219047888756976,
        17141823332647235824,
        4868152959489319152,
        12622547479260544500,
        11497081777123430159,
        17503222582726360560,
        13761942146684819647,
        3571571779382976022,
        1007945334246080512,
        13898816553270114032,
        3526584482946757129,
        703449135573728623,
        8678709372047787627,
        14082559273750003954,
        11023950935721046652,
        17361656908677705971,
        22553465724137712,
        4738000014499137519,
        16190635068230206568,
        8325261144846561520,
        4745040209105168832,
        16181698416122839270,
        12631923933146312944,
        15361231979021632640,
        10163231297877575053,
        7997835477745864463,
    }.SelectMany(input => new[] { 0, 4, 32, 36 }.Select(shift => ((ConvKernelMask << shift) & input) >> shift)).ToArray();
    bool[] Weights = new ulong[] {
            18193421370688993930,
            11527878097839882227,
            18446744073235516861,
            12682185797164632314,
            15888694586426391530,
            107024258529481727,
            17743970190754176,
            669680263601916,
            18194395031551084479,
            1898743719014399,
            18231841478714327424,
            17725744782663219755,
            17143972326299689911,
            15988553625362497332,
            11444155176739664889,
            8429189410007810064,
            8413232008506874993,
            9224520064547473010,
            13835334110510408193,
            17869629094815450636,
            1873532482255747310,
            9282354238021302759,
            9007235590978930567,
            18437736658578334142,
            13384828063624200191,
            13833932580722952000,
            2301972793781892710,
            18446743989432810489,
            18999316140105913,
            13596354025077204669,
            15235595071727337471,
            1161625113464915,
            9804336358586521852,
            17141044222376281080,
            4135994975861075967,
            18411614677198971262,
            2287525144756748247,
            142988459353572988,
            13817115812214865666,
            16686115894961864968,
            1112381961134900120,
            2361460430488456897,
            13686355567793139715,
            2303581887103352827,
            17401965052430266834,
            18419548754181411840,
            8008418285387455512,
            206038768128175092,
            5764610935168253572,
            18419789348468686921,
            5264707963989415935,
            2324178382306154880,
            2305846929664368207,
            18442495720295235363,
            73183856351511055,
            2288456713724171296,
            18380493970166773187,
            8934469499798099587,
            18584056729427985,
            9147178147706075648,
            13002809673180503860,
            288230376151713402,
            142371601942436816,
            18444738563829923840,
            288371800300285423,
            1049384892632138126,
            4091018742429908969,
            17873804232515056000,
            1657226883788970811,
            4648807781539635697,
            16131858725897518392,
            9182529577942640281,
            1147291972067201011,
            13979181207838136544,
            17867080175750512741,
            12897183570630214920,
            2304651168935704315,
            4611685946369164681,
            140491878566080,
            18345958198231039957,
            375900886303434623,
            71492471120463876,
            8572322889458524288,
            9000189790921950188,
            2467533409799372800,
            1134062106316898458,
            9225063225259712512,
            106343351735944232,
            18372716154726383616,
            2774236869081378255,
            1136809261179863182,
            1784058628865511265,
            17925595628229292418,
            13129019585459652873,
            4611844399637539266,
            14251465110697082080,
            17833390308248817552,
            296375535275682287,
            1594275978291445906,
            17861236265241391173,
            6539309981588252622,
            5170359425167987396,
            3528344577758193094,
            2966592499465534038

    }.SelectMany(token => Enumerable.Range(0, 64).Select(i => (token & (ulong)1 << i) > 0)).ToArray();
    int[] Thresholds = new ulong[] {
        9188614840057364356,
        9331882296095243905,
        10691097449148285345,
        8844129089959198077
    }.SelectMany(BitConverter.GetBytes).Select(unsigned => unsigned - 128).ToArray();
    float[] FloatWeights = {
        -0.00345635F,  0.00135299F, -0.0015705F, -0.00586785F, -0.00736081F,
        0.01026148F, -0.00613203F, -0.00369249F, -0.00794187F,  0.00625225F,
        0.00352356F,  0.00970497F, -0.00458683F,  0.00322268F,  0.0105695F,
        0.0060272F

                        };
    IEnumerable<int> Activations(IEnumerable<bool> inputs, int offset) => Enumerable.Range(0, 16).Select(output => inputs.Select((m, input) => m ^ Weights[offset + output * inputs.Count() + input] ? -1 : 1).Sum());
}