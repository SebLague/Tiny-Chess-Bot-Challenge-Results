namespace auto_Bot_439;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

public class Bot_439 : IChessBot
{

    struct StateInfo
    {
        public float score;
        public Move move;
        public float examined_depth;
        public int abflag;
        public StateInfo(float s, Move m, float depth, int flag)
        {
            score = s;
            move = m;
            examined_depth = depth;
            abflag = flag;
        }
    }
    private Dictionary<ulong, StateInfo> Transpositions = new Dictionary<ulong, StateInfo>();

    bool isDoneThinking(Board board, Timer timer)
    {
        return timer.MillisecondsRemaining / 20 < (timer.MillisecondsElapsedThisTurn);
    }

    public Move Think(Board board, Timer timer)
    {
        if (board.GetLegalMoves().Length == 1)
            return board.GetLegalMoves()[0];



        float bestScore = float.NegativeInfinity;
        Move bestMove = Move.NullMove;
        int i = 0;
        while (!isDoneThinking(board, timer))
        {

            (float score, Move move) = negamax(board, i, float.NegativeInfinity, float.PositiveInfinity, timer);
            if (move != Move.NullMove && score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
            i++;
        }
        if (bestMove != Move.NullMove)
            return bestMove;

        return board.GetLegalMoves()[0];
    }

    private (float, Move) negamax(Board board, int depth, float alpha, float beta, Timer timer)
    {
        float a = alpha;
        if (board.IsInCheckmate() || board.IsDraw())
        {
            return (-90000, Move.NullMove);
        }
        Move TMove = Move.NullMove;

        if (Transpositions.ContainsKey(board.ZobristKey))
        {

            StateInfo s = Transpositions[board.ZobristKey];

            TMove = s.move;
            if (s.examined_depth >= depth)
            {

                if (s.abflag == 1)
                    beta = Math.Min(beta, s.score);
                if (s.abflag == 2)
                    alpha = Math.Max(alpha, s.score);
                if (alpha >= beta || s.abflag == 0)
                    return (s.score, s.move);
            }
        }
        if (depth <= 0)
        {
            float standing_pat = evaluate(board);
            if (standing_pat >= beta)
                return (beta, Move.NullMove);
            if (alpha < standing_pat)
                alpha = standing_pat;
        }

        if (isDoneThinking(board, timer))
            return (beta, Move.NullMove);


        Move[] possibleMoves = board.GetLegalMoves(depth <= 0);
        PriorityQueue<Move, int> pq = new PriorityQueue<Move, int>();
        foreach (Move move in possibleMoves)
        {
            if (move == TMove)
            {
                pq.Enqueue(move, -100);
            }
            else if (move.IsCapture)
                pq.Enqueue(move, -5 * (int)move.CapturePieceType - (int)move.MovePieceType);
            else
                pq.Enqueue(move, -(int)move.MovePieceType);
        }

        Move bestMove = Move.NullMove;
        while (pq.Count > 0)
        {
            if (isDoneThinking(board, timer))
                return (beta, Move.NullMove);
            Move move = pq.Dequeue();
            board.MakeMove(move);
            float score = -negamax(board, depth - 1, -beta, -alpha, timer).Item1;
            board.UndoMove(move);
            if (score > alpha)
            {
                alpha = score;
                bestMove = move;
                if (score >= beta)
                {
                    alpha = beta;
                    break;
                }
            }
        }

        int abflag = 0;
        if (alpha <= a)
            abflag = 1;
        if (alpha >= beta)
            abflag = 2;
        StateInfo info = new StateInfo(alpha, bestMove, depth, abflag);
        if (Transpositions.ContainsKey(board.ZobristKey))
            Transpositions[board.ZobristKey] = info;
        else
        {
            Transpositions.Add(board.ZobristKey, info);
        }

        return (alpha, bestMove);
    }


    // Calculated using values provided on https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // Adding 167 to each value (so they are all positive), and shifting the values by 9 bits per piece type. Then added the values together.
    // This reduces the tables to 64 ulongs per middlegame and endgame. Calculations shown in PeSTOCompressor.
    public ulong[] middlegameTable = { 3598384705831079, 6696534945864871, 6452235841374887, 5360354637442215, 3921026811021479,
        4694044952530087, 5960616593928359, 6347783732492455, 6905989889834249, 5849428617133357, 5183265490460388, 5641075182638854,
        5604725006630123, 5750477279578917, 4552210250030281, 4870658704092316, 5569735372370081, 6730548008568494, 5958142031141057,
        5324893394424006, 5185596463642344, 6102249325875423, 6664582948053184, 5117151720875667, 5287295776013465, 5181744406950068,
        5463977717494957, 4936214694443196, 4831692098476222, 5008852385913011, 5394569029579448, 4620717389085328, 4162631208613004,
        5850314184939173, 4936690620788386, 4514410916047027, 4268671411521208, 4338900563489965, 4726411437111473, 4092676528750222,
        5393739427946637, 5394841627868323, 5112474505930403, 4268667916870301, 4338833186714794, 4831895033049258, 5360484574658760,
        4937649874087579, 5920062035530884, 6133027454903462, 5606567007041171, 3635625166981264, 4374910373285016, 5325371070313151,
        6203741143380685, 6168822916655249, 5359451890023591, 7152687448663207, 6308882869967527, 3988022127824039, 6167735078300839,
        4900409280632487, 6729578399738023, 6376430489903271 };

    public ulong[] endgameTable = { 3283028480940711, 4657348891706023, 5255484293592231, 5255827489101479, 5502117691330727,
        6416361609304231, 6028714502049959, 5290529478445223, 5463909527788889, 6486799208496980, 6382070728826181, 6488242046978861,
        6489408401390906, 7226013275200811, 6698591964700492, 6274317241608034, 6237759021063941, 6485836329461515, 6697148722537212,
        6418422256197866, 6594206543203551, 7472990274272476, 7436706120542457, 6345303660494075, 5606020467469511, 6662857169130687,
        6733364695235252, 6840359309114028, 6805999706274469, 7051121944519851, 6805999300730552, 5995316036053688, 5252733502565044,
        5748476078539440, 6627467310755492, 6734944037077152, 6839396430605984, 6698864967250079, 6206627083540138, 5501839725898406,
        5217685628330155, 5779880206747822, 6275346965810337, 6626572748811432, 6697146846896807, 6451405324372130, 6134265473476262,
        5570970846831263, 4935798065789620, 5498679298696879, 6025964651887279, 6343586343371953, 6378769240378036, 6026444479735463,
        5708891507728553, 5286954991154848, 4020248072230055, 4689096219355303, 5146905503932583, 5497305583661223, 4901782060343975,
        5392506768140967, 5041489965542055, 4371540562464423 };



    float evaluate(Board board)
    {

        float phase = 0;
        if (board.IsInCheckmate())
        {
            return -900000;
        }
        int[] phases = { 0, 0, 1, 1, 2, 4, 0 };
        int[] mg_value = { 0, 82, 337, 365, 477, 1025, 0 };
        int[] eg_value = { 0, 94, 281, 297, 512, 936, 0 };


        float mg_score = 0;
        float eg_score = 0;
        for (int i = 1; i <= 6; i++)
        {
            ulong playerBboard = board.GetPieceBitboard((PieceType)i, board.IsWhiteToMove);
            ulong enemyBboard = board.GetPieceBitboard((PieceType)i, !board.IsWhiteToMove);
            phase += phases[i] * (BitboardHelper.GetNumberOfSetBits(playerBboard) + BitboardHelper.GetNumberOfSetBits(enemyBboard));
            ulong[] boards = { playerBboard, enemyBboard };
            int mul = 1;
            foreach (ulong e in boards)
            {
                ulong r = e;
                while (r != 0)
                {
                    int loc = BitboardHelper.ClearAndGetIndexOfLSB(ref r);
                    loc = board.IsWhiteToMove ? loc ^ 56 : loc;
                    mg_score += mul * (mg_value[i] + (int)((middlegameTable[loc] >> ((i - 1) * 9)) & 511) - 167);
                    eg_score += mul * (eg_value[i] + (int)((endgameTable[loc] >> ((i - 1) * 9)) & 511) - 167);
                }
                mul *= -1;
            }

        }
        phase = phase > 24 ? 24 : phase;

        return (phase * mg_score + ((24 - phase) * eg_score)) / 24;
    }
}