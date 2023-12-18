namespace auto_Bot_160;
using ChessChallenge.API;

public class Bot_160 : IChessBot
{
    int nodes = 0;

    static int[] PawnTable =
    {
        0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0   ,
        10  ,   10  ,   0   ,   -10 ,   -10 ,   0   ,   10  ,   10  ,
        5   ,   0   ,   0   ,   5   ,   5   ,   0   ,   0   ,   5   ,
        0   ,   0   ,   10  ,   20  ,   20  ,   10  ,   0   ,   0   ,
        5   ,   5   ,   5   ,   10  ,   10  ,   5   ,   5   ,   5   ,
        10  ,   10  ,   10  ,   20  ,   20  ,   10  ,   10  ,   10  ,
        20  ,   20  ,   20  ,   30  ,   30  ,   20  ,   20  ,   20  ,
        0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0
    };
    static int[] KnightTable =
    {
        0   ,   -10 ,   0   ,   0   ,   0   ,   0   ,   -10 ,   0   ,
        0   ,   0   ,   0   ,   5   ,   5   ,   0   ,   0   ,   0   ,
        0   ,   0   ,   10  ,   10  ,   10  ,   10  ,   0   ,   0   ,
        0   ,   0   ,   10  ,   20  ,   20  ,   10  ,   5   ,   0   ,
        5   ,   10  ,   15  ,   20  ,   20  ,   15  ,   10  ,   5   ,
        5   ,   10  ,   10  ,   20  ,   20  ,   10  ,   10  ,   5   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0
    };
    static int[] BishopTable =
    {
        0   ,   0   ,   -10 ,   0   ,   0   ,   -10 ,   0   ,   0   ,
        0   ,   0   ,   0   ,   10  ,   10  ,   0   ,   0   ,   0   ,
        0   ,   0   ,   10  ,   15  ,   15  ,   10  ,   0   ,   0   ,
        0   ,   10  ,   15  ,   20  ,   20  ,   15  ,   10  ,   0   ,
        0   ,   10  ,   15  ,   20  ,   20  ,   15  ,   10  ,   0   ,
        0   ,   0   ,   10  ,   15  ,   15  ,   10  ,   0   ,   0   ,
        0   ,   0   ,   0   ,   10  ,   10  ,   0   ,   0   ,   0   ,
        0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0   ,   0
    };
    static int[] RookTable =
    {
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0   ,
        25  ,   25  ,   25  ,   25  ,   25  ,   25  ,   25  ,   25  ,
        0   ,   0   ,   5   ,   10  ,   10  ,   5   ,   0   ,   0
    };
    int[][] ScoreTable = { PawnTable, KnightTable, BishopTable, RookTable };
    int[] ScoreTableIndex = { 0, 1, 2, 3, 3, 4, 0, 1, 2, 3, 3, 4 }; //index 4 = false
    int[] PieceValue = { 0, 100, 325, 325, 550, 1000, 50000, 100, 325, 325, 550, 1000, 50000 };
    int BishopPair = 40;
    int MATE = 29000;
    Move bestMove;
    bool SearchCanceled;
    int timeThinking = 500;
    Move[] KillerMoves = new Move[1000];
    int curDepth = 1;

    int Evaluation(Board board)
    {
        int eval = 0;
        int h = 0;
        PieceList pieceList;
        PieceList[] pLists = board.GetAllPieceLists();

        if (pLists[2].Count == 2) eval += BishopPair;
        if (pLists[8].Count == 2) eval -= BishopPair;

        for (int i = 0; i < pLists.Length; i++)
        {
            pieceList = pLists[i];
            bool isWhite = pieceList.IsWhitePieceList;
            if (isWhite) h = 1;
            else h = -1;

            for (int j = 0; j < pieceList.Count; j++)
            {
                Piece pce = pieceList.GetPiece(j);
                int indexTable = ScoreTableIndex[i];
                int indexPce = pce.Square.Index;
                if (!isWhite) indexPce = 63 - indexPce;
                int scorePos;
                if (indexTable == 4) scorePos = 0;
                else scorePos = ScoreTable[indexTable][indexPce];

                eval += h * (PieceValue[i + 1] + scorePos);
            }

        }
        if (board.IsWhiteToMove) return eval;
        return -eval;
    }
    int Search(Board board, int alpha, int beta, int depth, Timer timer, bool capturesOnly, bool first)
    {
        //Search best score
        if (!capturesOnly)
        {
            if (board.IsInCheck()) depth++;
            if (depth <= 0) return Search(board, alpha, beta, depth, timer, true, false);
        }
        else
        {
            int score1 = Evaluation(board);
            if (score1 >= beta) return beta;
            if (score1 > alpha) alpha = score1;
        }
        if (board.IsDraw()) return 0;
        if (board.IsInCheckmate()) return -MATE + board.PlyCount;
        nodes++;
        int score = 0;
        if (nodes % 2047 == 0 && timer.MillisecondsElapsedThisTurn >= timeThinking && curDepth != 1) SearchCanceled = true;

        //Generate Moves
        System.Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, capturesOnly);
        if (!capturesOnly)
        {
            Move killerMove = KillerMoves[board.ZobristKey % 1000];
            //sort move, put capture move first
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                if (move.IsCapture) continue;
                for (int j = i + 1; j < moves.Length; j++)
                {

                    if (moves[j].IsCapture)
                    {
                        moves[i] = moves[j];
                        moves[j] = move;
                        break;
                    }
                }
                if (move.Equals(moves[i])) break;
            }

            // put killer move first
            if (!killerMove.IsNull)
            {
                for (int i = 0; i < moves.Length; i++)
                {
                    Move move = moves[i];
                    if (killerMove.Equals(move))
                    {
                        move = moves[0];
                        moves[0] = killerMove;
                        moves[i] = move;
                        break;
                    }
                }
            }


        }
        else
        {
            //captures Only true
            if (moves.Length == 0) return Evaluation(board);
        }
        Move bestSearch = Move.NullMove;
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            score = -Search(board, -beta, -alpha, depth - 1, timer, capturesOnly, false);
            board.UndoMove(move);
            if (SearchCanceled) return 0;
            if (score >= beta)
            {
                //killer move
                KillerMoves[board.ZobristKey % 1000] = move;
                return beta;
            }
            if (score > alpha)
            {
                alpha = score;
                bestSearch = move;
            }
        }

        if (first && !bestSearch.IsNull) bestMove = bestSearch;

        return alpha;
    }
    public Move Think(Board board, Timer timer)
    {
        //init
        timeThinking = 500;
        if (timer.MillisecondsRemaining < 20000) timeThinking = 100;
        if (timer.MillisecondsRemaining < 5000) timeThinking = 50;

        //Clear for search
        nodes = 0;
        SearchCanceled = false;

        for (curDepth = 1; curDepth <= 100; curDepth++)
        {
            Search(board, -99999, 99999, curDepth, timer, false, true);
            if (SearchCanceled) break;
        }
        //DivertedConsole.Write("Depth: " + curDepth + " BestMove: " + bestMove);
        return bestMove;
    }
}