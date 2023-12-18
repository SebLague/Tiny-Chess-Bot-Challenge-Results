namespace auto_Bot_219;
using ChessChallenge.API;
using System;
using System.Linq;

public class Bot_219 : IChessBot
{
    /*
     * ----- A small rundown of how the "Supertask-Time-Troubles" Bot works -----
     * 
     * -- What is a Supertask? --
     * When we talk about a "Supertask", we mean a sequence of countably infinite operations "squeezed" into a finite frame.
     * 
     * An example:
     * A marathon runner is really close to finishing her race but she's also reaaaally groggy. She is two meters away from the finish line, but due to her exhaustion every step that she takes is half as far as her last one.
     * Her first step is a strong 1 meters far. The following only 0.5 meters. The one after that 0.25 meters, etcetera etcetera.
     * After how many steps will she reach the goal? Because this is all very philosophical, we need to understand that our marathon runner is actually just a vertical line, or a point - she has no width that would trigger the finish line by proximity.
     * She should reach the goal after an infinite amount of steps, right? But damn, dat's alotta steps.
     * 
     * Vsauce has a great video about this: https://www.youtube.com/watch?v=ffUnNaQTfZE
     * 
     * 
     * -- What do the bot do? --
     * The bot has *terrible* time-management-skills.
     * It treats its time like a Supertask in that it always takes half its remaining time for its turn.
     * This might not seem like a smart strategy (it aint), but I can assure you that the bot will use the first 30 seconds to calculate a *banger* opening move.
     * 
     * I tried to make it at least a little competitive. It's very aggressive, so it can use the first moves that still search a few iterations deep to make some impact on the board.
     * It's especially entertaining to see it play against itself. If you have the patience to wait through their respective first moves, the way they ramp up their speed is quite satisfying to watch.
     * 
     * 
     * -- What are its limitations? --
     * There is an unavoidable time-loss between the time measured at the end of a turn and the time measured at the start of the next turn.
     * I measured this delta cost (_averageDeltaCostBetweenTurnsMS) to be around 16ms on my machine, which is unfortunately quite a lot.
     * To combat this, I included a buffer (_bufferMS) for 128 turns (_estimatedMaxTotalMoves) which decreases every turn and should help to remove the delta-cost from the equasion.
     * 
     * Additionally, computers are not cool enough to be infinitely fast, which is why - even without this buffer - we'd hit some time constraints eventually, to the point where even just assigning a random move would take too long.
     * But the computer should be quick enough to cover the 128 turns, even through the moves will get progressively worse. Any more than 128 turns and the buffer will be our bots detriment anyway.
     */

    const string ThanksForAllYourWorkSeb = " <3 ";

    const int PositiveInfinity = 9999999;
    const int NegativeInfinity = -PositiveInfinity;

    int[] _pieceValues = { 0, 100, 300, 320, 500, 900, 10000 };
    int[] _passedPawnBonuses = { 25, 25, 40, 65, 105, 150, 0 };

    Move _bestMoveOuterScope;
    int _bestEvalOuterScope;

    bool _searchCancelled;

    Board _board;
    Timer _timer;

    // It might be a little risky to just use the MS value that I measured on my machine, but increasing it a significant amount would make the buffer seem really big on the timer and I think that takes part of the fun out of the concept. It's a risk I'm willing to take!
    int _averageDeltaCostBetweenTurnsMS = 16;
    int _estimatedMaxTotalMoves = 128;

    // The found move needs to be better than alpha + _evalJitter. Without this, the bot would move quite deterministic. This way the moves get shuffled and the move that gets picked first has a higher chance of staying.
    int _evalJitter = 15;

    int _timeCeilingMS;
    int _bufferMS;
    int _turnCounter = 0;

    Random _random = new();

    public Move Think(Board board, Timer timer)
    {
        _board = board;
        _timer = timer;

        _searchCancelled = false;
        Move[] moves = _board.GetLegalMoves();
        _bestEvalOuterScope = NegativeInfinity;
        _bestMoveOuterScope = moves[_random.Next(moves.Length)];

        _timeCeilingMS = (int)Math.Ceiling(_timer.MillisecondsRemaining * 0.5);
        _bufferMS = Math.Max((_estimatedMaxTotalMoves - _turnCounter) * _averageDeltaCostBetweenTurnsMS, 0);

        for (int searchDepth = 1; searchDepth < int.MaxValue; searchDepth++)
        {
            SearchMovesRecursive(0, searchDepth, 0, NegativeInfinity, PositiveInfinity, false);

            if (_bestEvalOuterScope > PositiveInfinity - 50000 || _searchCancelled) break;
        }

        // I need to include some waiting here to make sure no turn is ended early (this would happen when the bot finds mate and stops its search).
        while (_timer.MillisecondsRemaining - _bufferMS > _timeCeilingMS) { /* exist */ }

        _turnCounter++;
        return _bestMoveOuterScope;
    }

    int SearchMovesRecursive(int currentDepth, int iterationDepth, int numExtensions, int alpha, int beta, bool capturesOnly)
    {
        if (_timer.MillisecondsRemaining - _bufferMS <= _timeCeilingMS) _searchCancelled = true;

        if (_searchCancelled || _board.IsDraw()) return 0;

        if (_board.IsInCheckmate()) return NegativeInfinity + 1;

        if (currentDepth != 0 && _board.GameRepetitionHistory.Contains(_board.ZobristKey)) return 0;

        if (currentDepth == iterationDepth) return SearchMovesRecursive(++currentDepth, iterationDepth, numExtensions, alpha, beta, true);

        // no span because I dont think i can properly shuffle the span without first creating an array :c
        Move[] movesToSearch = _board.GetLegalMoves(capturesOnly);

        if (capturesOnly)
        {
            int captureEval = Evaluate();

            if (movesToSearch.Length == 0) return captureEval;

            if (captureEval >= beta) return beta;

            if (captureEval > alpha) alpha = captureEval;
        }

        // Shuffle so that moves with similar (see _evalJitter) eval are not deterministic.
        movesToSearch = movesToSearch.OrderBy(e => _random.Next()).ToArray();
        Array.Sort(movesToSearch, (x, y) => Math.Sign(MoveOrderCalculator(currentDepth, y) - MoveOrderCalculator(currentDepth, x)));

        for (int i = 0; i < movesToSearch.Length; i++)
        {
            Move move = movesToSearch[i];

            _board.MakeMove(move);

            int extension = (numExtensions < 16 && _board.IsInCheck()) ? 1 : 0;
            int eval = -SearchMovesRecursive(currentDepth + 1, iterationDepth + extension, numExtensions, -beta, -alpha, capturesOnly);

            _board.UndoMove(move);

            if (_searchCancelled) return 0;

            if (eval >= beta) return eval;

            if (eval >= alpha + _evalJitter)
            {
                alpha = eval;

                if (currentDepth == 0)
                {
                    _bestMoveOuterScope = move;
                    _bestEvalOuterScope = eval;
                }
            }
        }

        return alpha;
    }

    public int MoveOrderCalculator(int depth, Move move)
    {
        int moveScoreGuess = 0;

        if (depth == 0 && move == _bestMoveOuterScope) moveScoreGuess += PositiveInfinity;

        if (move.CapturePieceType != PieceType.None)
        {
            int captureMaterialDelta = 10 * _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType];
            moveScoreGuess += (captureMaterialDelta < 0 && _board.SquareIsAttackedByOpponent(move.TargetSquare)) ? 10000 - captureMaterialDelta : 50000 + captureMaterialDelta;
        }

        if (move.IsPromotion) moveScoreGuess += 30000 + _pieceValues[(int)move.PromotionPieceType];

        return moveScoreGuess;
    }

    int Evaluate()
    {
        bool isWhite = _board.IsWhiteToMove;

        int[] evals = new[] { 0, 0 };

        int friendlyMaterialIndex = isWhite ? 0 : 1;
        int enemyMaterialIndex = (friendlyMaterialIndex + 1) % 2;

        evals[0] += CountMaterial(true);
        evals[1] += CountMaterial(false);

        // Endgame starts very early (it still fades in) -> king comes out early and hyperaggressive.
        //0-1 -> more the fewer material the enemy has
        float enemyEndgameWeight = 1 - Math.Min(1, (evals[enemyMaterialIndex] - 10000) / 3000f);
        //0-1 -> more the more material the enemy has than me
        float disadvantageReduction = Math.Min(1, (evals[friendlyMaterialIndex] - 10000) / ((float)(evals[enemyMaterialIndex] - 10000)));

        enemyEndgameWeight *= disadvantageReduction;

        evals[0] += ForceKingToCornerEndgame(enemyEndgameWeight, true);
        evals[1] += ForceKingToCornerEndgame(enemyEndgameWeight, false);

        evals[0] += EvaluatePiecePositions(enemyEndgameWeight, true);
        evals[1] += EvaluatePiecePositions(enemyEndgameWeight, false);

        return (evals[friendlyMaterialIndex] - evals[enemyMaterialIndex]);
    }

    int EvaluatePiecePositions(float endgameWeight, bool isWhite)
    {
        int eval = 0;

        // push all pieces into the enemy!
        PieceList[] pieceLists = _board.GetAllPieceLists();
        for (int i = 0; i < 6; i++)
        {
            foreach (Piece piece in pieceLists[i + (isWhite ? 0 : 6)])
            {
                if (piece.IsKing)
                {
                    // dont move the king forward to prevent early unfortunate mates
                    continue;
                }

                Square square = piece.Square;
                int rank = isWhite ? square.Rank : 7 - square.Rank;
                int distFromMiddle = Math.Max(3 - square.File, square.File - 4);

                if (piece.IsPawn)
                {
                    eval += _passedPawnBonuses[rank];
                }

                eval += rank * (5 - distFromMiddle);
            }
        }

        // reduce the value of pushing in the endgame
        return eval * (1 - (int)endgameWeight);
    }


    int ForceKingToCornerEndgame(float enemyEndgameWeight, bool isWhite)
    {
        int eval = 0;

        if (enemyEndgameWeight > 0)
        {
            Square opponentKingSquare = _board.GetKingSquare(!isWhite);
            Square friendlyKingSquare = _board.GetKingSquare(isWhite);

            int distToCentre = DistanceInSquaresToCenter(opponentKingSquare) * 10;

            int dstBetweenKingsFile = Math.Abs(friendlyKingSquare.File - opponentKingSquare.File);
            int dstBetweenKingsRank = Math.Abs(friendlyKingSquare.Rank - opponentKingSquare.Rank);
            int dstBetweenKings = dstBetweenKingsFile + dstBetweenKingsRank;

            eval += distToCentre * 10 + (14 - dstBetweenKings) * 4;
        }

        return (int)(eval * enemyEndgameWeight);
    }

    public int DistanceInSquaresToCenter(Square square)
    {
        int squareDistToCenterFile = Math.Max(3 - square.File, square.File - 4);
        int squareDistToCenterRank = Math.Max(3 - square.Rank, square.Rank - 4);
        int squareDistToCentre = squareDistToCenterRank + squareDistToCenterFile;

        return squareDistToCentre;
    }

    int CountMaterial(bool isWhite)
    {
        int material = 0;

        PieceList[] allPieceLists = _board.GetAllPieceLists();
        for (int i = 0; i < 6; i++)
        {
            PieceList pieceList = allPieceLists[i + (isWhite ? 0 : 6)];
            material += pieceList.Count * _pieceValues[i + 1];
        }

        return material;
    }
}