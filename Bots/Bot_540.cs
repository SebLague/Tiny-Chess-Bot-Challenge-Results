namespace auto_Bot_540;
//Thanks to my friend James Bond 73(his bot name is MrJB73) who did this challenge along with me and helped me with some things in my bot, my search is based on his
//Also thanks to eduherminio for helping me optimize the tokens
//The transposition tables where based on Selenauts TT I believe, so thanks to him
//And finally thanks to the rest discord community for always helping me when I needed

using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;
using static ChessChallenge.API.BitboardHelper;




public class Bot_540 : IChessBot
{

    public struct Transposition
    {
        public ulong zobristHash;
        public float evaluation;
        public sbyte depth;
        public Move move;
        public byte flag;
    };

    private Transposition[] transpositionTable = new Transposition[0x800000];

    private Move chosenMove,
        moveToPlay;

    ulong[] white_pawn_attacks = new ulong[64],
        black_pawn_attacks = new ulong[64],
        knight_attacks = new ulong[64],
        bishop_attacks = new ulong[64],
        rook_attacks = new ulong[64],
        queen_attacks = new ulong[64],
        king_attacks = new ulong[64];

    public Bot_540()
    {
        for (int j = 0; ++j < 64;)
        {
            Square square = new Square(j);
            white_pawn_attacks[j] = GetPawnAttacks(square, false);
            black_pawn_attacks[j] = GetPawnAttacks(square, true);
            knight_attacks[j] = GetKnightAttacks(square);
            bishop_attacks[j] = GetSliderAttacks(PieceType.Bishop, square, 0);
            rook_attacks[j] = GetSliderAttacks(PieceType.Rook, square, 0);
            queen_attacks[j] = GetSliderAttacks(PieceType.Queen, square, 0);
            king_attacks[j] = GetKingAttacks(square);

        }

    }


    public Move Think(Board board, Timer timer)
    {
        double average_moves = 4 * Math.Pow(board.PlyCount - 200, 2) / 10000 + 20;
        int turnMaxTime = timer.MillisecondsRemaining * board.GetLegalMoves().Length / (35 * Convert.ToInt32(average_moves));
        int _depth = 1;

        float Eval()
        {

            float mg_score = 0,
                eg_score = 0,
                phase = Enumerable.Range(0, 12).Sum(i => phase_points[i % 6] * board.GetAllPieceLists()[i].Count);
            int white_attackers, black_attackers;
            for (int i = 0; ++i < 64;)
            {
                int Attackers(bool isWhite)
                    => BitOperations.PopCount(board.GetPieceBitboard(PieceType.Pawn, isWhite) & (isWhite ? white_pawn_attacks[i] : black_pawn_attacks[i]) | board.GetPieceBitboard(PieceType.King, isWhite) & king_attacks[i] | board.GetPieceBitboard(PieceType.Knight, isWhite) & knight_attacks[i] | board.GetPieceBitboard(PieceType.Bishop, isWhite) & bishop_attacks[i] | board.GetPieceBitboard(PieceType.Rook, isWhite) & rook_attacks[i] | board.GetPieceBitboard(PieceType.Queen, isWhite) & queen_attacks[i]);

                white_attackers = Attackers(true);
                black_attackers = Attackers(false);
                mg_score += white_attackers * mg_weights[i] - black_attackers * mg_weights[63 - i];
                eg_score += white_attackers * eg_weights[i] - black_attackers * eg_weights[63 - i];

            }


            return (mg_score * phase + eg_score * (24 - phase)) / 24;
        }

        float Negamax(int depth, float alpha, float beta, int color)
        {
            if (board.IsDraw()) return 0;
            if (board.IsInCheckmate()) return color * (board.IsWhiteToMove ? -100000000000000000000000000000000000000f : 100000000000000000000000000000000000000f) / (_depth - depth);

            ulong zobrist = board.ZobristKey;

            ref Transposition transposition = ref transpositionTable[zobrist & 0x7FFFFF];

            float evaluation = transposition.evaluation,
                bestEvaluation = -300000000000000000000000000000000000000f,
                startingAlpha = alpha;

            Move m = transposition.move;



            if (transposition.zobristHash == zobrist && transposition.depth >= depth && depth != _depth)
            {
                chosenMove = transposition.flag switch
                {
                    0 => m,
                    1 when evaluation >= beta => m,
                    2 when evaluation <= alpha => m,
                    _ => chosenMove
                };

                if (chosenMove == m) return evaluation;

            }




            Move[] legalMoves = board.GetLegalMoves(depth <= 0).OrderByDescending(move => (move.IsCapture ? 100 * (int)move.CapturePieceType - (int)move.MovePieceType : 0) + (move == m ? 1_000_000 : 0)).ToArray();

            if (depth <= 0)
            {
                bestEvaluation = Eval() * color;
                if (bestEvaluation >= beta)
                    return bestEvaluation;
                alpha = Math.Max(alpha, bestEvaluation);
            }

            Move bestMove = Move.NullMove;

            foreach (Move move in legalMoves)
            {
                if (timer.MillisecondsElapsedThisTurn > turnMaxTime) return 0;
                board.MakeMove(move);
                evaluation = -Negamax(depth - 1, -beta, -alpha, -color);
                board.UndoMove(move);

                if (evaluation > bestEvaluation)
                {
                    bestEvaluation = evaluation;
                    bestMove = move;
                }
                alpha = Math.Max(alpha, bestEvaluation);

                if (alpha >= beta) break;

            }
            chosenMove = bestMove;



            transposition.evaluation = bestEvaluation;
            transposition.zobristHash = zobrist;
            transposition.depth = (sbyte)depth;
            transposition.move = bestMove;

            if (bestEvaluation < startingAlpha)
                transposition.flag = 2;

            else if (bestEvaluation >= beta)
                transposition.flag = 1;

            else transposition.flag = 0;


            return bestEvaluation;
        }

        while (_depth < 100)
        {
            float eval = Negamax(_depth, -300000000000000000000000000000000000000f, 300000000000000000000000000000000000000f, board.IsWhiteToMove ? 1 : -1);
            if (timer.MillisecondsElapsedThisTurn > turnMaxTime) break;
            moveToPlay = chosenMove;
            if (eval > 10000000000000000000000000000000000f) break;
            _depth++;
        }
        return moveToPlay.IsNull ? board.GetLegalMoves()[0] : moveToPlay;
    }

    readonly float[] mg_weights = { 0.4591761411421006f, 0.5567281701943156f, 0.39578669329657495f, 0.795931563172893f, 0.4793746556492925f, 0.395528016150554f, 0.5406517129003732f, 0.6730949235693298f, 0.5690708399521892f, 0.7634387996359052f, 0.4048004270266824f, 0.5317854935428425f, 0.3502830746076168f, 0.47639638787171323f, 0.29858822181549277f, 0.5149205344680278f, 0.6035896426784314f, 0.507531643581848f, 0.4810217511336519f, 0.29863649895108246f, 0.22539336253809691f, 0.503130366192541f, 0.2334743458337874f, 0.6241448392481761f, 0.6629596271561256f, 0.4907597421495439f, 0.6725968005882289f, 0.30937581925876717f, 0.3483040465076157f, 0.4016490731494782f, 0.5272488187012466f, 0.46069920541867665f, 0.5773091663074397f, 0.3337943435067761f, 0.2563481403628763f, 0.31887550418895244f, 0.27244084962565646f, 0.4302664029810872f, 0.7224316948878773f, 0.5640428807076089f, 0.4309875472257737f, 0.46551065977221157f, 0.6988247688250602f, 0.8011598545840359f, 0.40558773349910116f, 0.5253572661286777f, 0.3580502437223439f, 0.30910557972996006f, 0.25737591260884163f, 0.3810285276779545f, 0.8025344746354318f, 0.5628003760738904f, 0.678466330752004f, 0.3639623932549592f, 0.4094154491581885f, 0.538924524184675f, 0.4722621902801605f, 0.27092274575463743f, 0.4110839865031839f, 0.43064675821691845f, 0.24714353650414858f, 0.4469784083215874f, 0.5748256223503513f, 0.6576370117713569f, 0.4380068807474694f },
        eg_weights = { 0.6286547084155781f, 0.6999699057686475f, 0.45288324454749673f, 0.37804640043733406f, 0.31423409242939715f, 0.5065642399221608f, 0.28595428150466057f, 0.2396441837701136f, 0.1776510855963359f, 0.4615337762915503f, 0.5676527689324087f, 0.3076556853112713f, 0.8093469729073166f, 0.5072906351839305f, 0.3734750157109442f, 0.5515938940537519f, 0.5737727193948594f, 0.47313072928310573f, 0.35429608390266937f, 0.361078721340237f, 0.5918626282410715f, 0.6447219606405502f, 0.6567360070691107f, 0.5354029286376363f, 0.2111385004255375f, 0.47432584329271854f, 0.7335166402795241f, 0.32200823249432753f, 0.24109067656456f, 0.43374980760030596f, 0.42736196880536226f, 0.6086435793689168f, 0.6879718844750707f, 0.2703836409531222f, 0.5240171502943046f, 0.3459432352665351f, 0.44827540698970025f, 0.2802080467752519f, 0.36399496810082604f, 0.6317020357903136f, 0.6285841157966374f, 0.7147691048179372f, 0.21731210872407936f, 0.47086300476737175f, 0.6955287160774285f, 0.29614207296270434f, 0.36824191937249323f, 0.3585584778678192f, 0.35988618375766757f, 0.6970707530613703f, 0.2832542728069284f, 0.3947511194575815f, 0.6198150788536749f, 0.42105567199893024f, 0.4290230793543193f, 0.25912141685952295f, 0.5738195530706408f, 0.5856646945115073f, 0.4179313326613554f, 0.8053286199161755f, 0.28392605165364215f, 0.4371028832195962f, 0.3731168134982772f, 0.35639103135278416f, 0.5140563911083295f },
        phase_points = { 0, 1, 1, 2, 4, 0 };
}