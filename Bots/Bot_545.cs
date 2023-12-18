namespace auto_Bot_545;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_545 : IChessBot
{
    int checkMateScore = -10000000;
    Move pvMove;
    int Depth;
    Board board;
    Timer timer;
    int targetTime = 100;

    struct ttEntry
    {
        public int eval;
        public byte evalType;
        public int Depth;
        public ushort RawMove;
    }


    Dictionary<ulong, ttEntry> TranspositionTable = new();
    SortedSet<int> KillerMoves;



    public Move Think(Board board2, Timer timer2)
    {
        if (TranspositionTable.Count > 15777216) TranspositionTable = new();

        pieceTables = new int[8, 64];
        for (int i = 0; i < 64; i++)
            for (int j = 0; j < 32; j++)
                pieceTables[j / 4, i % 64] += (int)((((1ul << i) & preTables[j]) >> i) << j % 4);

        board = board2;
        timer = timer2;
        pvMove = board.GetLegalMoves()[0];
        targetTime = timer.MillisecondsRemaining / 60;
        Move lastPvMove = Move.NullMove;
        for (Depth = 1; Depth < 55 && timer.MillisecondsElapsedThisTurn <= targetTime; Depth++)
        {
            KillerMoves = new();
            Search_and_QuiescenceSearch(Depth, 10, -20000000, 20000000, false);
            if (!lastPvMove.Equals(pvMove)) targetTime += timer.MillisecondsRemaining / 200;
            lastPvMove = pvMove;
        }

        return pvMove;
    }


    int Search_and_QuiescenceSearch(int depth, int maxExtensionDepth, int alpha, int beta, bool onlyCaptures)
    {
        if (timer.MillisecondsElapsedThisTurn > targetTime || board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return checkMateScore;



        bool isFirstMove = depth == Depth && maxExtensionDepth == 10;
        ulong zobristKey = board.ZobristKey ^ (onlyCaptures ? 1UL : 0UL);
        if (TranspositionTable.TryGetValue(zobristKey, out ttEntry entry) && !isFirstMove)
            if (entry.Depth >= depth && (entry.evalType == 0 || (entry.evalType == 1 && entry.eval >= beta) || (entry.evalType == 2 && entry.eval <= alpha)))
                return entry.eval;

        if (depth < 1)
            return Search_and_QuiescenceSearch(200, maxExtensionDepth, alpha, beta, true); // Quiescence Search


        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves, onlyCaptures);

        Span<int> orderScores = stackalloc int[moves.Length];
        int sortIndex = 0;

        foreach (Move move in moves)
        {
            int currentOrderScore = pieceTables[7, move.TargetSquare.Index] - pieceTables[6, move.StartSquare.Index];
            if (move.RawValue == entry.RawMove) currentOrderScore += 100000;
            if (KillerMoves.Contains(board.PlyCount << 16 + move.RawValue)) currentOrderScore += 10000;
            if (move.IsPromotion || move.IsCastles) currentOrderScore += 10000;
            if (move.IsCapture)
                currentOrderScore += 100 + pieceValues[(int)move.CapturePieceType - 1] - pieceValues[(int)move.MovePieceType - 1];


            if (board.SquareIsAttackedByOpponent(move.TargetSquare)) currentOrderScore -= 100;
            orderScores[sortIndex++] = -currentOrderScore;
        }

        MemoryExtensions.Sort(orderScores, moves);


        if (onlyCaptures)
        {
            int currentEval = BoardScore();
            if (currentEval >= beta) return beta;
            alpha = Math.Max(alpha, currentEval);
            if (moves.Length == 0 || depth < 188) return alpha;
        }

        byte evalType = 2;

        foreach (Move move in moves)
        {
            int searchExtension = (board.IsInCheck() || (move.MovePieceType == PieceType.Pawn && move.TargetSquare.Rank % 5 == 1)) && maxExtensionDepth > 0 ? 1 : 0;

            board.MakeMove(move);
            int currentEval = -Search_and_QuiescenceSearch(depth - 1 + searchExtension, maxExtensionDepth - searchExtension, -beta, -alpha, onlyCaptures);
            board.UndoMove(move);

            if (timer.MillisecondsElapsedThisTurn > targetTime) return 0;

            if (currentEval >= beta)
            {
                alpha = beta;
                evalType = 1;
                entry.RawMove = move.RawValue;
                KillerMoves.Add(board.PlyCount << 16 + move.RawValue);
                break;
            }
            if (currentEval > alpha)
            {
                alpha = currentEval;
                evalType = 0;
                entry.RawMove = move.RawValue;
                if (isFirstMove)
                    pvMove = move;
            }
        }

        TranspositionTable[zobristKey] = new ttEntry
        {
            eval = alpha,
            evalType = evalType,
            Depth = depth,
            RawMove = entry.RawMove
        };


        return alpha;
    }

    // Piece values: pawn-20, knight-50, bishop-20, rook-5, queen-20, king-45
    int[] pieceValues = { 80, 275, 330, 495, 880, -45 };

    ulong[] preTables = {
        36524680437301248, 28822598428917504, 18439876420596662271, 103485865728,
        6854511819227136, 4827650281393291074, 4342241613368345148, 17027580355820544,
        18577350176538624, 9115848310565470590, 35604928818740736, 0,
        16680909151580094207, 1729382256910335744, 0, 0,
        1730577137166712856, 9115709509703139710, 35604933113708032, 0,
        18429855469913767680, 7350016974312701952, 14106399387370389504, 18446462598732840960,
        9132568582834438911, 35720492394922881, 16915432340177790, 66229406284800,
        11915017571648495293, 7404930696321025918, 1746410393481132032, 18446744073709551615
    };
    int[,] pieceTables;


    int BoardScore()
    {
        PieceList[] pieces = board.GetAllPieceLists();
        int sum = 0, wSum = 0, bSum = 0;

        for (int i = 0; i < 5; i++)
        {
            wSum += pieceValues[i] * pieces[i].Count;
            bSum += pieceValues[i] * pieces[i + 6].Count;
        }
        for (int i = 0; i < 12; i++)
        {
            if (i == 6) sum = -sum;
            float endGameModifier = i % 6 == 5 ? (8000 - wSum - bSum) / 1600f : 0;
            sum += pieces[i].Sum(piece => (int)(pieceTables[i % 6, i < 6 ? 63 - piece.Square.Index : piece.Square.Index] * (5f - endGameModifier) + pieceTables[6, i < 6 ? 63 - piece.Square.Index : piece.Square.Index] * endGameModifier));
        }
        // Chess 4.5
        bool blackIsLeading = wSum < bSum;
        int md = Math.Abs(wSum - bSum);
        int pa = pieces[blackIsLeading ? 6 : 0].Count;
        int materialAdvantage = Math.Min(3100, Math.Min(2400, md) + (md * pa * (8000 - wSum - bSum)) / (6400 * (pa + 1)));

        sum += blackIsLeading ? materialAdvantage : -materialAdvantage;


        return board.IsWhiteToMove ? -sum : sum;
    }
}
