namespace auto_Bot_226;
// Bob

using ChessChallenge.API;

public class Bot_226 : IChessBot
{
    // Constants
    const int normalEvalDepth = 5;
    const int endgameEvalDepth = 6;
    const float positionalness = 0.1f;
    const short queenStaticMoves = 5;
    const int badPieceAvoidance = 1;
    const int badPieceThreshold = 15;
    const int evalTableSize = 16777216; // 2 ^ 24
    const int totalMaterial = 28012;
    const int minEval = 100000000;
    const int endgameCutoff = 23000; // 3000 points aside from the kings
    const int nonQueenPromotionCost = 500;
    const int panicEvalTime = 10000;
    const int panicEvalDepth = 4;

    // Other variables
    bool white;
    bool endgame;
    int evalDepth;
    int numMoves = 0;
    Move bestMove;
    int[] evalTable = new int[evalTableSize];
    bool[] evalTableValidity = new bool[evalTableSize];

    // Squares table for faster computation
    readonly int[] square = { 0, 1, 4, 9, 16, 25, 36, 49 };

    // Piece values from Hans Berliner's system
    readonly int[] pieceValues = { 0, 100, 320, 333, 510, 880, 10000 };

    // Returns taxicab distance from center of the board
    private int distanceFromCenter(Square s)
    {
        int x = s.File;
        int y = s.Rank;
        return (x < 4 ? 3 - x : x - 4) + (y < 4 ? 3 - y : y - 4);
    }

    // Piece location evaluation in general (used for final eval)
    private int locationValue(Piece p)
    {
        if (p.IsKing) // Avoid the center (or enter it during the endgame)
        {
            int d = distanceFromCenter(p.Square);

            return endgame ? 36 - 6 * d : 6 * d;
        }
        else if (p.IsPawn) // Move towards promotion
        {
            int y = p.Square.Rank;

            int startValue = 30; // 30 extra points so that they are low priority until the endgame

            // Triple pawn push bonus in endgame
            return startValue + (endgame ? 3 : 1) * (p.IsWhite ? square[y] : square[7 - y]);
        }
        else if (p.IsRook) // Move towards the center but especially the seventh/second rank
        {
            int d = distanceFromCenter(p.Square);

            return (p.IsWhite && p.Square.Rank == 6) || (!p.IsWhite && p.Square.Rank == 1) ? 96 - square[d] : 66 - square[d];
        }
        else if (!p.IsQueen) // Move towards the center
        {
            int d = distanceFromCenter(p.Square);

            return 36 - square[d];
        }

        // Encourage the queen to remain on her starting square for the first few moves (so I don't get embarrassed)
        if (numMoves <= queenStaticMoves && (p.Square.Equals(new Square(3, 0)) || p.Square.Equals(new Square(3, 7))))
            return 200;

        return 50; // Queens automatically get 50 points so that they are ignored
    }

    // Final eval is calculated based on end state and value of pieces on the board
    private int FinalEval(Board board)
    {
        uint hash = ((uint)board.ZobristKey) >> 8;
        if (evalTableValidity[hash])
            return evalTable[hash];

        float eval = 0;

        foreach (PieceList pl in board.GetAllPieceLists())
        {
            // Raw value of pieces
            float plValue = pl.Count * pieceValues[(int)pl.TypeOfPieceInList];

            // Factor in location of pieces
            foreach (Piece p in pl)
            {
                int locationEval = locationValue(p);
                if (locationEval < badPieceThreshold)
                {
                    locationEval += badPieceAvoidance * (locationEval - badPieceThreshold);
                }

                plValue += positionalness * locationEval;
            }

            // Add piece eval to total eval
            eval += board.IsWhiteToMove == pl.IsWhitePieceList ? plValue : -1 * plValue;
        }

        evalTableValidity[hash] = true;
        evalTable[hash] = (int)eval;

        return (int)eval;
    }

    // Non-final eval uses minimax and terminates with final eval
    private int Eval(Board board, int depth, int pruningCutoff)
    {
        if (depth == 0)
        {
            return FinalEval(board);
        }

        Move[] allMoves = board.GetLegalMoves();
        int moveCount = allMoves.Length;
        int bestEval = -100000000;
        int bestIndex = 0;

        // Check captures first
        int numCaptures = 0;
        for (int i = 0; i < moveCount; i++)
        {
            if (allMoves[i].IsCapture)
            {
                Move thisMove = allMoves[i];
                allMoves[i] = allMoves[numCaptures];
                allMoves[numCaptures] = thisMove;
                numCaptures++;
            }
        }

        // Evaluate moves
        for (int i = 0; i < moveCount; i++)
        {
            Move move = allMoves[i];
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                bestMove = move;

                board.UndoMove(move);
                return 10000000;
            }

            if (board.IsDraw())
            {
                bestMove = allMoves[bestIndex];

                board.UndoMove(move);
                return 0;
            }

            int eval = -1 * Eval(board, depth - 1, -1 * bestEval);

            if (move.IsPromotion && move.PromotionPieceType != PieceType.Queen)
                eval -= nonQueenPromotionCost;

            if (eval > bestEval)
            {
                bestEval = eval;
                bestIndex = i;
            }
            if (eval >= pruningCutoff)
            {
                board.UndoMove(move);
                return eval;
            }
            board.UndoMove(move);
        }

        bestMove = allMoves[bestIndex];
        return bestEval;
    }

    private int MaterialOnBoard(Board board)
    {
        int material = 0;

        foreach (PieceList pl in board.GetAllPieceLists())
            material += pl.Count * pieceValues[(int)pl.TypeOfPieceInList];

        return material;
    }

    // Sets all validity flags in the eval table to 0
    private void ResetEvalTable()
    {
        for (int i = 0; i < evalTableSize; i++)
            evalTableValidity[i] = false;
    }

    // Evaluate the position and pick the best move
    public Move Think(Board board, Timer timer)
    {
        numMoves++;
        white = board.IsWhiteToMove;

        // Always play e4 as the first move (because it is OP)
        if (white && numMoves == 1 && MaterialOnBoard(board) == totalMaterial)
        {
            Move e4 = new Move("e2e4", board);

            foreach (Move m in board.GetLegalMoves())
            {
                if (m.Equals(e4))
                    return e4;
            }
        }

        endgame = MaterialOnBoard(board) < endgameCutoff;
        evalDepth = timer.MillisecondsRemaining < panicEvalTime ? panicEvalDepth : (endgame ? endgameEvalDepth : normalEvalDepth);
        ResetEvalTable();

        Eval(board, evalDepth, minEval);

        return bestMove;
    }
}
