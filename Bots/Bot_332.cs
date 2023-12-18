namespace auto_Bot_332;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_332 : IChessBot
{
    // Piece values: null, pawn, knight, bishop, rook, queen, king
    int[] pieceValues = { 0, 100, 300, 350, 500, 900, 10000 };
    // Bonus to evaluation for having more legal moves for each piece type
    int[] moveBonuses = { 0, 2, 6, 10, 5, 0, 1 };
    string[] pawnBonusSquares = { "d4", "e4", "d5", "e5" };


    ulong tableSize = 4194304;
    StoredEval[] transpositionTable = new StoredEval[4194304];
    StoredEval[] capturesTable = new StoredEval[4194304];
    Move moveToPlay = Move.NullMove;

    public struct StoredEval
    {
        public ulong key;
        public int value;
        public byte depth;
        public bool isExact; // stored value can be exact or a lower bound
    }

    ulong storedIndex(Board board)
    {
        return board.ZobristKey % tableSize;
    }

    void storePosition(Board board, int value, byte depth, bool isExact, bool isCapture = false)
    {
        StoredEval newStoredEval = new StoredEval();
        if (value > 10000)
        {
            value += board.PlyCount;
        }
        if (value < -10000)
        {
            value -= board.PlyCount;
        }
        newStoredEval.key = board.ZobristKey;
        newStoredEval.value = value;
        newStoredEval.depth = depth;
        newStoredEval.isExact = isExact;
        if (isCapture)
        {
            capturesTable[storedIndex(board)] = newStoredEval;
        }
        else
        {
            transpositionTable[storedIndex(board)] = newStoredEval;
        }
    }

    int getPosition(Board board, int beta, byte depth, bool captureTable = false)
    {
        StoredEval storedEval = captureTable ? capturesTable[storedIndex(board)] : transpositionTable[storedIndex(board)];
        int value = storedEval.value;
        if (value > 10000)
        {
            value -= board.PlyCount;
        }
        if (value < -10000)
        {
            value += board.PlyCount;
        }
        if (storedEval.key == board.ZobristKey && storedEval.depth >= depth)
        {
            if (storedEval.isExact)
            {
                return value;

            }
            else if (storedEval.value >= beta)
            {
                return value;
            }

        }

        return -10000000;
    }

    public Move Think(Board board, Timer timer)
    {
        int depth = 1;
        /*for (int i = 0; i < capturesTable.Length; i++)
        {
            capturesTable[i] = new StoredEval();
        }*/
        int targetTime = timer.MillisecondsRemaining / 50 + 500;
        if (timer.MillisecondsRemaining > 45 * 1000)
        {
            targetTime *= 2;
        }
        if (timer.MillisecondsRemaining > 90 * 1000)
        {
            targetTime *= 2;
        }
        while (timer.MillisecondsElapsedThisTurn < targetTime / 20)
        {


            Minimax(board, depth, -999999, 999999, true);

            depth++;
        }

        //DivertedConsole.Write("Depth: " + (depth - 1).ToString());
        //DivertedConsole.Write("Eval: " + ((float)bestEval / 100).ToString());
        return moveToPlay;
    }


    // Minimax function
    int Minimax(Board board, int depth, int alpha, int beta, bool isRoot = false)
    {

        if (board.IsInCheckmate())
        {
            return -999999 + board.PlyCount;
        }
        if (board.IsDraw())
        {
            return 0;
        }

        int lookup;
        if (!isRoot)
        {
            lookup = getPosition(board, beta, (byte)depth, depth == 0);
            if (lookup != -10000000)
            {
                return lookup;
            }
        }


        int eval;
        int bestEval;
        Move[] moves;

        //DivertedConsole.Write(depth);
        if (depth == 0)
        {
            Move[] captures = board.GetLegalMoves(true);
            if (captures.Length == 0) // || depth >= maxDepth+8)
            {
                return Evaluate(board, beta);
            }
            // Search for captures only if max depth is reached
            moves = captures;

        }
        else
        {
            // Otherwise search all moves
            moves = board.GetLegalMoves();
        }



        int[] keys = new int[moves.Length];
        if (depth > 0)
        {
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                board.MakeMove(move);
                lookup = getPosition(board, -999999, 0);
                board.UndoMove(move);
                keys[i] = lookup == -10000000 ? 0 : lookup;
            }
            Array.Sort(keys, moves);
        }
        else
        {
            for (int i = 0; i < moves.Length; i++)
            {
                Move move = moves[i];
                keys[i] -= Math.Max(10 * (pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType]), 10);
                if (move.IsPromotion)
                {
                    keys[i] -= pieceValues[(int)move.PromotionPieceType];
                }
            }
            Array.Sort(keys, moves);
        }


        // Maximize
        // Evaluate current position in case only bad captures are available 
        bestEval = depth <= 0 ? Evaluate(board, beta) : -999999;
        alpha = Math.Max(alpha, bestEval);
        if (beta <= alpha && depth == 0)
        {
            return bestEval;
        }
        foreach (Move move in moves)
        {
            board.MakeMove(move);
            eval = -Minimax(board, Math.Max(depth - 1, 0), -beta, -alpha);
            board.UndoMove(move);
            if (eval > bestEval)
            {
                bestEval = eval;
                if (isRoot)
                {
                    moveToPlay = move;
                }
            }
            alpha = Math.Max(alpha, bestEval);
            if (beta <= alpha)
            {
                /*if(bestEval > 10000)
                {
                    bestEval--;
                }
                if (bestEval < -10000)
                {
                    bestEval++;
                }*/
                storePosition(board, bestEval, (byte)depth, false, depth == 0);
                return bestEval;
            }
        }

        /*if (bestEval > 10000)
        {
            bestEval--;
        }
        if (bestEval < -10000)
        {
            bestEval++;
        }*/
        storePosition(board, bestEval, (byte)depth, true, depth == 0);

        return bestEval;
    }


    int Evaluate(Board board, int beta)
    {
        int lookup = getPosition(board, beta, 0);
        if (lookup != -10000000)
        {
            return lookup;
        }

        int eval = 0;

        // Count material
        foreach (PieceList piecelist in board.GetAllPieceLists())
        {
            if (piecelist.IsWhitePieceList)
            {
                eval += pieceValues[(int)piecelist.TypeOfPieceInList] * piecelist.Count;

                // Bonus for pushing pawns for white
                if (piecelist.TypeOfPieceInList == PieceType.Pawn)
                {
                    foreach (Piece pawn in piecelist)
                    {
                        eval += (pawn.Square.Rank - 1) * 6;
                        if (pawnBonusSquares.Contains(pawn.Square.Name))
                        {
                            eval += 10;
                        }
                    }
                }
            }
            else
            {
                eval -= pieceValues[(int)piecelist.TypeOfPieceInList] * piecelist.Count;

                // Bonus for pushing pawns for black
                if (piecelist.TypeOfPieceInList == PieceType.Pawn)
                {
                    foreach (Piece pawn in piecelist)
                    {
                        eval -= (6 - pawn.Square.Rank) * 6;
                        if (pawnBonusSquares.Contains(pawn.Square.Name))
                        {
                            eval -= 10;
                        }
                    }
                }
            }
        }

        if (!board.IsWhiteToMove)
        {
            eval = -eval;
        }


        int movesBonus = 0;

        // Bonus for having more legal moves, hopefully this causes the bot to develop pieces
        Move[] moves = board.GetLegalMoves();
        foreach (Move move in moves)
        {
            movesBonus += moveBonuses[(int)move.MovePieceType];
        }
        board.ForceSkipTurn();

        Move[] newMoves = board.GetLegalMoves();
        foreach (Move move in newMoves)
        {
            movesBonus -= moveBonuses[(int)move.MovePieceType];
        }
        board.UndoSkipTurn();

        eval += movesBonus;


        storePosition(board, eval, 0, true);
        //DivertedConsole.Write(eval);
        return eval;
    }
}