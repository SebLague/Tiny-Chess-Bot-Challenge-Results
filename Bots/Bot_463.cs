namespace auto_Bot_463;
using ChessChallenge.API;
using System;
using System.Linq;

public struct ValueAndMove
{

    public int value;
    public Move move = Move.NullMove;

    public ValueAndMove(int value, Move move)
    {
        this.value = value;
        this.move = move;
    }
}

public class Bot_463 : IChessBot
{

    public int searchSteps = 0;
    public int searchStepsThisThink = 0;

    int depth = 4;
    int millisecondsElapsedLastTurn = -1;

    public static void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            n--;
            int k = Random.Shared.Next(n + 1);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    public static int Heuristic(Board board, int depth = 0)
    {

        if (board.IsDraw())
            return 0;

        if (board.IsInCheckmate())
            return (board.IsWhiteToMove ? -1 : 1) * (depth + 1000000); // add depth to incentivize checkmating faster

        var pieceLists = board.GetAllPieceLists();

        int whitePiecesValue = pieceLists[0].Count * 1000 + (pieceLists[1].Count + pieceLists[2].Count) * 3000 + pieceLists[3].Count * 5000 + pieceLists[4].Count * 10000;
        int blackPiecesValue = pieceLists[6].Count * 1000 + (pieceLists[7].Count + pieceLists[8].Count) * 3000 + pieceLists[9].Count * 5000 + pieceLists[10].Count * 10000;

        int whitePieceAdvanceBonus = 0;
        // encourage white to advance pieces (except king)
        for (int i = 0; i <= 4; i++)
        {
            foreach (var piece in pieceLists[i])
            {
                whitePieceAdvanceBonus += piece.Square.Rank;
            }
        }

        // encourage black to advance pieces (except king)
        int blackPieceAdvanceBonus = 0;
        for (int i = 6; i <= 10; i++)
        {
            foreach (var piece in pieceLists[i])
            {
                blackPieceAdvanceBonus += 7 - piece.Square.Rank;
            }
        }

        int whiteLegalMoves = 0;
        int blackLegalMoves = 0;
        // encourage having more legal moves
        if (board.IsWhiteToMove)
            whiteLegalMoves += board.GetLegalMoves().Length;
        else
            blackLegalMoves += board.GetLegalMoves().Length;
        if (board.TrySkipTurn())
        {
            if (board.IsWhiteToMove)
                whiteLegalMoves += board.GetLegalMoves().Length;
            else
                blackLegalMoves += board.GetLegalMoves().Length;
            board.UndoSkipTurn();
        }

        int whiteNotInCheckBonus = 0;
        int blackNotInCheckBonus = 0;
        // encourage not being in check
        if (board.IsWhiteToMove)
            whiteNotInCheckBonus = board.IsInCheck() ? 0 : 100;
        else
            blackNotInCheckBonus = board.IsInCheck() ? 0 : 100;
        if (board.TrySkipTurn())
        {
            if (board.IsWhiteToMove)
                whiteNotInCheckBonus = board.IsInCheck() ? 0 : 100;
            else
                blackNotInCheckBonus = board.IsInCheck() ? 0 : 100;
            board.UndoSkipTurn();
        }

        // even exchanges are beneficial if you're in the lead
        return (int)Math.Round(200000.0 * whitePiecesValue / (whitePiecesValue + blackPiecesValue) - 100000.0)
            + whitePieceAdvanceBonus - blackPieceAdvanceBonus
            + whiteLegalMoves - blackLegalMoves
            + whiteNotInCheckBonus - blackNotInCheckBonus;
    }

    /// <param name="color">1 or -1</param>
    public ValueAndMove PrincipalVariationSearch(Board board, int depth, int α, int β, int color)
    {

        searchSteps++;
        searchStepsThisThink++;

        Move[] moves;
        if (depth == 0 || (moves = board.GetLegalMoves()).Length == 0)
            return new ValueAndMove(color * Heuristic(board, depth), Move.NullMove);

        Shuffle(moves); // to stop draws by repetition

        var moveEnumerator = moves.OrderByDescending(move =>
        {
            int ret = 0;
            board.MakeMove(move);
            ret = color * Heuristic(board, depth - 1);
            board.UndoMove(move);
            return ret;
        }).ThenByDescending(move => move.IsCastles);

        ValueAndMove αAndMove = new(α, Move.NullMove);
        ValueAndMove score;
        int i = 0;
        foreach (Move move in moveEnumerator)
        {

            board.MakeMove(move);
            if (i == 0)
            {
                score = new(-PrincipalVariationSearch(board, depth - 1, -β, -αAndMove.value, -color).value, move);
            }
            else
            {
                score = new(-PrincipalVariationSearch(board, depth - 1, -αAndMove.value - 1, -αAndMove.value, -color).value, move);
                if (αAndMove.value < score.value && score.value < β)
                    score = new(-PrincipalVariationSearch(board, depth - 1, -β, -score.value, -color).value, move);
            }
            board.UndoMove(move);

            if (score.value > αAndMove.value)
                αAndMove = score;
            if (αAndMove.value >= β)
                break;
            i++;
        }

        return αAndMove;
    }

    public Move Think(Board board, Timer timer)
    {

        searchStepsThisThink = 0;

        // https://chess.stackexchange.com/questions/2506/what-is-the-average-length-of-a-game-of-chess
        int k = board.GameMoveHistory.Length;
        int estimatedPlyRemaining = (int)(59.3 + (72830 - 2330 * k) / (2644 + k * (10 + k)));
        int allottedTime = timer.MillisecondsRemaining / estimatedPlyRemaining;

        ValueAndMove valueAndMove;

        if (millisecondsElapsedLastTurn != -1)
        {
            if (millisecondsElapsedLastTurn > allottedTime)
            {
                depth--;
            }
            else if (millisecondsElapsedLastTurn < allottedTime / 2)
            {
                depth++;
            }
        }
        valueAndMove = PrincipalVariationSearch(board, depth, -10000000, 10000000, board.IsWhiteToMove ? 1 : -1);

        DivertedConsole.Write($"[MyBot]   value: {valueAndMove.value,10:N0}; depth: {depth,2:N0}; estimated ply remaining: {estimatedPlyRemaining,2:N0}; allotted time: {allottedTime,6:N0}ms; actual time used: {timer.MillisecondsElapsedThisTurn,6:N0}ms; {valueAndMove.move}");

        millisecondsElapsedLastTurn = timer.MillisecondsElapsedThisTurn;

        return valueAndMove.move;
    }
}