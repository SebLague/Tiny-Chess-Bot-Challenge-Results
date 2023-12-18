namespace auto_Bot_557;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_557 : IChessBot
{

    // I can add stuff in the constructor here
    public Move bestMove;
    public bool searchCancelled;

    public Move bestMoveThisIteration;
    public int bestEvalThisIteration;

    // stores <Zobrist hash - [eval, depth]>
    public Dictionary<ulong, int[]> transpositionTable = new Dictionary<ulong, int[]>();
    public int maxTTSize = 1024 * 1024 * 128 / (sizeof(ulong) + sizeof(int));


    // How many moves do we check?
    public int randomMoves = 15;
    public Random rng = new();

    public Move Think(Board board, Timer timer)
    {
        searchCancelled = false;
        int bestEval = startSearch(board, timer);

        //DivertedConsole.Write(bestMove);
        //DivertedConsole.Write(bestEval);
        return bestMove;
    }

    // iterative deepening
    public int startSearch(Board board, Timer timer)
    {
        int bestEval = -100000;

        for (int searchDepth = 1; searchDepth < int.MaxValue; searchDepth++)
        {
            List<Move> moves = board.GetLegalMoves().ToList();
            Move bestMoveThisIteration = moves[0];
            int bestEvalThisIteration = -100000;

            // test all legal moves to see which one is best

            for (int i = 0; i < moves.Count; i++)
            {

                board.MakeMove(moves[i]);

                // need to add - sign to make sure the eval remains accurate
                // since it is not the bot's turn

                int eval = -tryMove(searchDepth, board, timer);

                if ((eval > bestEvalThisIteration) && !searchCancelled)
                {
                    bestEvalThisIteration = eval;
                    bestMoveThisIteration = moves[i];
                }
                board.UndoMove(moves[i]);
            }

            if (bestEvalThisIteration > bestEval)
            {
                bestMove = bestMoveThisIteration;
                bestEval = bestEvalThisIteration;
            }


            if (searchCancelled)
            {
                break;
            }
            DivertedConsole.Write(searchDepth);
        }

        return bestEval;
    }


    // Random Moves
    public int tryMove(int depth, Board board, Timer timer)
    {
        checkTime(timer);

        // very bad
        int negativeInfinity = -100000;

        int bestEvalThisSearch = negativeInfinity;

        // check TT first
        int[] result;
        int evaluation = 0;

        if (transpositionTable.TryGetValue(board.ZobristKey, out result))
        {
            if (result[1] >= depth)
            {
                return result[0]; // we could update the depth searched for better accuracy
            }
        }
        if (searchCancelled) return 0;
        // evaluate the board when we get to the highest considered depth
        if (depth == 0)
        {
            return evaluate(board);
        }

        // get all the possible moves at considered board state
        List<Move> moves = board.GetLegalMoves().ToList();

        if (moves.Count == 0)
        {
            if (board.IsInCheck())
            {
                // this is Checkmate
                return negativeInfinity;
            }
            // stalemate
            return 0;
        }

        for (int i = 0; i < randomMoves; i++)
        {
            Move move = moves[rng.Next(moves.Count)];
            board.MakeMove(move);
            evaluation = -tryMove(depth - 1, board, timer);
            board.UndoMove(move);

            bestEvalThisSearch = Math.Max(bestEvalThisSearch, evaluation);

        }

        // we update the TT here
        updateTT(bestEvalThisSearch, board, depth);

        return bestEvalThisSearch;
    }



    public int evaluate(Board board)
    {
        int eval = 0;


        eval += evaluateMaterial(board) + rng.Next(-1, 1);

        eval += controlCenter(board);

        if (board.PlyCount < 30) eval += kingCastled(board);
        if (board.PlyCount < 30) eval += developMinorPieces(board);
        if (board.PlyCount > 10) eval += pushPawns(board);

        return eval;
    }



    public int evaluateMaterial(Board board)
    {
        // Get the material difference
        // Probably very inefficient, bitboards may be better?

        int eval = 0;

        PieceList[] pl = board.GetAllPieceLists();

        // scores as white-black

        eval += (pl[0].Count - pl[6].Count) * 100;
        eval += (pl[1].Count - pl[7].Count) * 300;
        eval += (pl[2].Count - pl[8].Count) * 300;
        eval += (pl[3].Count - pl[9].Count) * 500;
        eval += (pl[4].Count - pl[10].Count) * 800;

        // how many moves each player has from this position
        int moves = board.GetLegalMoves().Length;
        board.ForceSkipTurn();
        int opponentMoves = board.GetLegalMoves().Length;
        board.UndoSkipTurn();
        eval += 2 * (moves - opponentMoves);


        // check who is to move to make sure the material count is
        // in the right direction
        if (!board.IsWhiteToMove) eval = -eval;

        return eval;
    }

    public int pushPawns(Board board)
    {
        int eval = 0;
        PieceList pl = board.GetPieceList(PieceType.Pawn, board.IsWhiteToMove);
        for (int i = 0; i < pl.Count; i++)
        {
            eval += pl.GetPiece(i).Square.Rank * 2;
        }

        return eval;
    }

    public int kingCastled(Board board)
    {
        int eval = 0;
        bool w = board.IsWhiteToMove;
        if (w && board.GetKingSquare(w) == new Square("g1")) { eval = 50; }
        if (!w && board.GetKingSquare(w) == new Square("g8")) { eval = 50; }

        return eval;
    }

    public int controlCenter(Board board)
    {
        int eval = 0;
        Square[] centerSquares = new Square[4];
        centerSquares[0] = new Square("d4");
        centerSquares[1] = new Square("e4");
        centerSquares[2] = new Square("d5");
        centerSquares[3] = new Square("e5");

        board.ForceSkipTurn();
        foreach (Square s in centerSquares)
        {
            if (board.SquareIsAttackedByOpponent(s)) eval += 10;
        }
        board.UndoSkipTurn();
        return eval;
    }


    public int developMinorPieces(Board board)
    {
        int eval = 0;
        int whiteToMove = 0;
        if (!board.IsWhiteToMove) whiteToMove = 56;

        Square[] minorsSquares = { new Square(whiteToMove + 1), new Square(whiteToMove + 2),
                new Square(whiteToMove + 5), new Square(whiteToMove + 6) };

        foreach (Square s in minorsSquares)
        {
            if (board.GetPiece(s).IsKnight) eval -= 50;
            if (board.GetPiece(s).IsBishop) eval -= 80;
        }
        return eval;
    }

    public void updateTT(int eval, Board board, int depth)
    {
        ulong zobrist = board.ZobristKey;
        int[] entry = { eval, depth };
        int[] result;

        if (transpositionTable.TryGetValue(board.ZobristKey, out result))
        {
            if (result[1] < depth) { transpositionTable.Remove(zobrist); }
            else { return; }
        }

        if (transpositionTable.Count == maxTTSize)
        {
            transpositionTable.Remove(transpositionTable.ElementAt(0).Key);
        }
        transpositionTable.Add(zobrist, entry);
    }

    public void checkTime(Timer timer)
    {
        int n = 3000;
        if (timer.MillisecondsRemaining < 30000)
        {
            n = 100;
        }
        if (timer.MillisecondsElapsedThisTurn > n) { searchCancelled = true; }

        return;
    }

}


