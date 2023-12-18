namespace auto_Bot_406;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_406 : IChessBot
{
    private Dictionary<ulong, int[]> transpositionTable = new Dictionary<ulong, int[]>();
    private int bestMoveInLastRun;

    //ulong[] pawnPosition = { 2290649224, 8602779599652398182, 8766409058111735944, 9838263505674548839 };
    //ulong[] knightPosition = { 15819044040458685662, 14363181513831700108, 14367985280132334988, 17135295939775789245 };
    //ulong[] bishopPosition = { 12144106524623153835, 12066946028095825802, 11990383666198374026, 13450750891322280058 };
    //ulong[] rookPosition = { 8531619138958297224, 10991185015148677257, 10991185015148677257, 9837964439084107913 };
    //ulong[] queenPosition = { 12144106524623084203, 10986381249114240906, 12139302689028142984, 13450451824176301962 };
    //ulong[] kingPosition = { 14834555755207519708, 14834555755207519708, 12374690811836488907, 4785222094407895108 };//this is mid game specific

    ulong[] pieceIdealPositions = { 2290649224, 8602779599652398182, 8766409058111735944, 9838263505674548839,
    15819044040458685662, 14363181513831700108, 14367985280132334988, 17135295939775789245,
    12144106524623153835, 12066946028095825802, 11990383666198374026, 13450750891322280058,
    8531619138958297224, 10991185015148677257, 10991185015148677257, 9837964439084107913,
    12144106524623084203, 10986381249114240906, 12139302689028142984, 13450451824176301962,
    14834555755207519708, 14834555755207519708, 12374690811836488907, 4785222094407895108 };//king's is mid game specific

    int[][] pieceTables = new int[12][];
    static int[] numConversions = { 50, 40, 30, 25, 20, 15, 10, 5, 0, -5, -10, -20, -30, -40, -50 };

    PieceType[] pieceTypes = { PieceType.Pawn, PieceType.Knight, PieceType.Bishop, PieceType.Rook, PieceType.Queen, PieceType.King };
    int[] pieceValues = { 100, 300, 300, 500, 900 };

    int numEvals = 0;
    bool cancelSearch;
    Timer global_timer;
    int maxTimeThisTurn;
    //int timeUsed;
    int searchDepth;

    public Bot_406()
    {
        //Fix packed arrays
        for (int i = 0; i < 6; i++)
        {
            int[] combinedArray = new int[64];

            for (int j = i * 4; j < i * 4 + 4; j++)
            {
                int[] unpackedArray = new int[16];
                for (int x = 0; x < unpackedArray.Length; x++)
                    unpackedArray[x] = numConversions[(int)((pieceIdealPositions[j] >> (4 * x)) & 0x0F)];
                Array.Copy(unpackedArray, 0, combinedArray, (j - i * 4) * 16, 16);
            }
            pieceTables[i * 2] = combinedArray; //White pieces
            pieceTables[i * 2 + 1] = combinedArray.Reverse().ToArray(); //Black pieces
        }
    }

    public Move Think(Board board, Timer timer)
    {
        global_timer = timer;

        numEvals = 0;
        Move[] allMoves = board.GetLegalMoves();
        Move moveToPlay = allMoves[0];

        //Work out how much time to allocate
        int enemy_time_used = global_timer.GameStartTimeMilliseconds - global_timer.OpponentMillisecondsRemaining;

        int enemy_avg_turn_time = SafeDivide(2 * enemy_time_used, board.PlyCount + 1, 1000);

        //I think this was supposed to be a sophisticated calculation for how much time to use but I coded this months ago, it clearly dosen't work yet, and I need the free tokens
        //int enemy_avg_turn_time;
        //if (board.PlyCount > 0) enemy_avg_turn_time = 2 * enemy_time_used / (board.PlyCount + 1); //enemy_avg_turn_time = enemy_time_used / ((board.PlyCount+1)/2);
        //else enemy_avg_turn_time = 1000;

        //int us_avg_turn_time = SafeDivide(2 * timeUsed, board.PlyCount - 1, 1000);

        //int turn_enemy_out = SafeDivide(- global_timer.GameStartTimeMilliseconds, global_timer.IncrementMilliseconds - enemy_avg_turn_time + 1,10000);//+1 for tiny bots
        //int turn_us_out = SafeDivide(-global_timer.GameStartTimeMilliseconds, global_timer.IncrementMilliseconds - us_avg_turn_time + 1,10000);

        //int current_turn = board.PlyCount/2;

        //int turns_left = turn_us_out - current_turn;

        //maxTimeThisTurn = 1000;
        //if (turn_us_out - current_turn > 60) maxTimeThisTurn = 1000;
        maxTimeThisTurn = enemy_avg_turn_time;

        bestMoveInLastRun = 0;
        cancelSearch = false;

        for (searchDepth = 0; searchDepth < 1000; searchDepth += 2)
        {
            int bestValue = int.MinValue;
            foreach (var move in OrderMoves(board.GetLegalMoves()))
            {
                board.MakeMove(move);
                int value = Minimax(board, searchDepth, int.MinValue, int.MaxValue, false);
                board.UndoMove(move);
                if (cancelSearch) break;

                if (value > bestValue)
                {
                    bestValue = value;
                    moveToPlay = move;
                }
            }
            bestMoveInLastRun = moveToPlay.GetHashCode();

            if (cancelSearch) break;
        }

        numEvals = 0;
        board.MakeMove(moveToPlay);
        board.UndoMove(moveToPlay);
        transpositionTable.Clear();
        //timeUsed += global_timer.MillisecondsElapsedThisTurn;
        return moveToPlay;
    }
    static int SafeDivide(int numerator, int denominator, int defaultValue)
    {
        if (denominator != 0) return numerator / denominator;
        else return defaultValue;
    }
    // Minimax function with alpha-beta pruning.
    private int Minimax(Board board, int depth, int alpha, int beta, bool isMaximizingPlayer)
    {
        ulong zobristKey = board.ZobristKey;
        if (transpositionTable.TryGetValue(zobristKey, out int[] entry))
            //entry[0] is storedDepth, entry[1] is storedEval;
            if (entry[0] >= searchDepth)
                return entry[1];

        if (global_timer.MillisecondsElapsedThisTurn > maxTimeThisTurn)
        {
            cancelSearch = true;
            return 0;
        }

        if (depth == 0 || board.IsInCheckmate() || board.IsDraw())
            return EvaluateBoard(board);

        int evalValue = isMaximizingPlayer ? int.MinValue : int.MaxValue;

        foreach (var move in OrderMoves(board.GetLegalMoves()))
        {
            board.MakeMove(move);
            int eval = Minimax(board, depth - 1, alpha, beta, !isMaximizingPlayer);
            board.UndoMove(move);
            if (cancelSearch) return 0;

            if (isMaximizingPlayer)
            {
                evalValue = Math.Max(evalValue, eval);
                alpha = Math.Max(alpha, eval);
            }
            else
            {
                evalValue = Math.Min(evalValue, eval);
                beta = Math.Min(beta, eval);
            }

            if (beta <= alpha)
                break;
        }

        //StoreTranspositionEntry(zobristKey, depth, evalValue);
        transpositionTable[zobristKey] = new int[] { searchDepth, evalValue };
        return evalValue;

    }
    private int EvaluateBoard(Board board)
    {
        numEvals += 1;
        if (board.IsInCheckmate()) return int.MaxValue;
        if (board.IsDraw() || board.FiftyMoveCounter == 50) return 0;

        // Piece values: null, pawn, knight, bishop, rook, queen, king
        // int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

        //return CountMaterial(board, board.IsWhiteToMove) - CountMaterial(board, !board.IsWhiteToMove) + EvalPositions(board, board.IsWhiteToMove) - EvalPositions(board, !board.IsWhiteToMove);
        return CountMaterial(board, !board.IsWhiteToMove) - CountMaterial(board, board.IsWhiteToMove) + EvalPositions(board, !board.IsWhiteToMove) - EvalPositions(board, board.IsWhiteToMove);
    }
    int CountMaterial(Board board, bool colour)
    {
        int score = 0;
        for (int i = 0; i < pieceValues.Length; i++)
            score += board.GetPieceList(pieceTypes[i], colour).Count * pieceValues[i];
        return score;
    }
    int EvalPositions(Board board, bool colour)
    {
        int value = 0;
        int colAdd = colour ? 0 : 1;//hacky af
        for (int i = 0; i < pieceTypes.Length; i++)
            value += EvalBitBoard(board.GetPieceBitboard(pieceTypes[i], colour), pieceTables[i * 2 + colAdd]);
        if (board.IsInCheck()) value += 10000;
        return value;
    }
    int EvalBitBoard(ulong number, int[] pieceTable)
    {
        int value = 0;
        for (int bitPosition = 0; bitPosition < 64; bitPosition++)
        {
            ulong mask = (ulong)1 << bitPosition;
            int bitValue = (number & mask) != 0 ? 1 : 0;
            value += bitValue * pieceTable[bitPosition];
        }
        return value;
    }
    private IEnumerable<Move> OrderMoves(IEnumerable<Move> moves)
    {
        List<Move> orderedMoves = new(moves);
        orderedMoves.Sort((move1, move2) =>
        {
            int quality1 = MoveQuality(move1);
            int quality2 = MoveQuality(move2);
            return quality2.CompareTo(quality1);
        });

        return orderedMoves;
    }
    int MoveQuality(Move move)
    {
        int quality = 0;
        if (move.GetHashCode() == bestMoveInLastRun) return int.MaxValue;
        if (move.IsCapture) quality += pieceValues[Array.IndexOf(pieceTypes, move.CapturePieceType)]; // could throw an error if king is capture piece type?
        if (move.IsPromotion) quality += 900;
        if (move.IsCastles) quality += 600;
        return quality;
    }
}
