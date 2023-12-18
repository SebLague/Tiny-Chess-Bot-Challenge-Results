namespace auto_Bot_534;
using ChessChallenge.API;
using System;

//===============================
//Alfie Bot 4.0
//===============================

public class Bot_534 : IChessBot
{
    //Value of pieces for taking and protecting.
    //Value of pieces for taking and protecting - None, Pawn, Knight, Bishop, Rook, King, Queen.
    static public int[] pieceValues = { 0, 120, 175, 275, 275, 400, 1500 };
    static public float captureRisksPieceMultiplier = 0.8f;

    //Cost of moving a piece, to discourage moving high value pieces - None, Pawn, Knight, Bishop, Rook, King, Queen.
    static public int[] pieceMoveCost = { 0, 25, 40, 50, 50, 100, 175 };

    //Interest level of a move adds additional levels of recursion to a move
    public enum Interest
    {
        NONE = 0,
        LOW = 1,
        MEDIUM = 2,
        HIGH = 3,
    }

    //Scores awarded for various states of the game.
    const int checkValue = 50;
    const int checkMateValue = 1000000;
    const int potentialCheckmateValue = 400;
    const int drawValue = -10000000;

    //Bonuses given to special moves.
    const int promotionBonus = 100;
    const int enPassantBonus = 100;
    const int castleBonus = 75;

    //Rewards for tempting pieces into certain areas
    const int closerToCentreBonus = 25;

    //Base level of recursion to use when evaluating a move.
    const int baseMoveDepth = 3;
    const int maxMoveDepth = 6;
    const int inspectFurtherThreshold = 300;

    //Whether its the players turn or not.
    static bool isMyTurn;
    //Multiplier that sets preference to protecting pieces on the players turn.
    const float myPieceMultiplier = 1.4f;
    //Multiplier that is added to weight the enemies move higher than the players move
    const float enemyTurnMultiplier = 1f;

    static Random rng;

    //An evaluated move with a score, a level of interest and an assigned move
    public struct EvaluatedMove
    {
        //Score - How valuable this move is to the current player.
        public int score;
        //Intrest - How intresting a move is which can be used as a bonus to provide deeper levels of recursion.
        public Interest interest;
        //Move - The move that is made using the chess challenge API.
        public Move move;

        public EvaluatedMove(Move _move)
        {
            score = 0;
            interest = Interest.NONE;
            move = _move;
        }
    }

    public Move Think(Board board, Timer timer)
    {
        //Upon begining to think it should be the players turn.
        isMyTurn = true;

        //Get All Moves
        Span<Move> moveSpan = stackalloc Move[500];
        board.GetLegalMovesNonAlloc(ref moveSpan);

        Span<EvaluatedMove> evaluatedMovesSpan = stackalloc EvaluatedMove[moveSpan.Length];
        var evaluatedMoves = EvaluateMoves(moveSpan, evaluatedMovesSpan, board, timer);

        //Get the highest scoring move from all the evaluated moves, this will (hopefully) be the optimal move
        EvaluatedMove bestMove = GetBestMove(evaluatedMoves);

        DivertedConsole.Write($"Best move score: {bestMove.score}");
        return bestMove.move;
    }

    public Span<EvaluatedMove> EvaluateMoves(Span<Move> moveSpan, Span<EvaluatedMove> evaluatedMovesSpan, Board board, Timer timer, int currentDepth = 1, int recursiveDepth = baseMoveDepth)
    {
        //Consider all legal moves, with current and max depth set, by default this is base depth
        for (int i = 0; i < evaluatedMovesSpan.Length; i++)
        {
            evaluatedMovesSpan[i] = EvaluateMove(moveSpan[i], board, timer, currentDepth, recursiveDepth);
        }

        return evaluatedMovesSpan;
    }

    public static EvaluatedMove GetBestMove(Span<EvaluatedMove> evaluatedMovesSpan)
    {
        //Chooses a random move, if no move is deemed better, this is the move that is made.
        Random rng = new();
        EvaluatedMove bestMove = evaluatedMovesSpan[rng.Next(evaluatedMovesSpan.Length)];

        //Gets the best move
        for (int i = 0; i < evaluatedMovesSpan.Length; i++)
        {
            if (isMyTurn)
            {
                if (evaluatedMovesSpan[i].score > bestMove.score)
                    bestMove = evaluatedMovesSpan[i];
            }
            else
            {
                if (evaluatedMovesSpan[i].score < bestMove.score)
                    bestMove = evaluatedMovesSpan[i];
            }
        }

        return bestMove;
    }

    public EvaluatedMove EvaluateMove(Move move, Board board, Timer timer, int currentDepth, int recursiveDepth = baseMoveDepth)
    {
        EvaluatedMove evalMove = new(move);
        float score = 0;

        //================================================
        //If the move is a capture move, how valuable would the captured piece be?
        if (move.IsCapture)
        {
            if (!board.SquareIsAttackedByOpponent(move.TargetSquare))
                score += EvaluatePieceValue(move.CapturePieceType);
            else
                score += EvaluatePieceValue(move.CapturePieceType) * captureRisksPieceMultiplier;

            evalMove.interest = GetPieceTakeInterest(move.CapturePieceType, currentDepth);
        }
        else
        {
            if (move.MovePieceType == PieceType.Knight && SquareIsCloseToCenter(move.TargetSquare))
            {
                score += closerToCentreBonus;
            }
            else
            {
                score -= closerToCentreBonus;
            }
        }


        //================================================

        //================================================
        //Each piece has a movement cost, to discourage throwing valuable pieces at the enemy
        score -= pieceMoveCost[(int)move.MovePieceType];
        //================================================


        //================================================
        //Checks next move to see if its advantagous
        board.MakeMove(move);
        isMyTurn = !isMyTurn;

        float checkingScore = 0;

        if (board.IsInCheck())
        {
            checkingScore += checkValue;

            if (RandomChanceToPass(20))
                evalMove.interest = Interest.MEDIUM;
        }

        if (board.IsInCheckmate())
        {
            if (currentDepth == 1)
                checkingScore += checkMateValue;
            else
            {
                checkingScore += potentialCheckmateValue;
            }

            evalMove.interest = Interest.MEDIUM;
        }

        score += checkingScore;

        if (board.IsDraw())
        {
            score += drawValue;
        }

        isMyTurn = !isMyTurn;
        board.UndoMove(move);
        //================================================

        //================================================
        //Bonus scores given to special moves
        if (move.IsPromotion)
            score += promotionBonus;

        if (move.IsEnPassant)
            score += enPassantBonus;

        if (move.IsCastles)
            score += castleBonus;
        //===============================================

        //Moves that are made from the other player are negative, and are retracted from a moves scoring
        evalMove.score += (int)(score * TurnMultipler());

        //Bonus depth is added depending on how interesting that move was + time must be above 5 seconds to prevent timeout
        int depthToExplore = currentDepth == 1 && timer.MillisecondsRemaining > 5000 ? recursiveDepth + (int)evalMove.interest : recursiveDepth;

        //THE SECRET SAUCE // Moves are checked recursively to decide whether a move would be good or not, the moves alternate players
        if (currentDepth < recursiveDepth)
        {
            board.MakeMove(move);
            isMyTurn = !isMyTurn;

            Span<Move> moveSpan = stackalloc Move[500];
            board.GetLegalMovesNonAlloc(ref moveSpan);

            if (moveSpan.Length > 0)
            {
                Span<EvaluatedMove> evaluatedMovesSpan = stackalloc EvaluatedMove[moveSpan.Length];

                EvaluatedMove nextBestMove = GetBestMove(EvaluateMoves(moveSpan, evaluatedMovesSpan, board, timer, currentDepth + 1, depthToExplore));
                evalMove.score += nextBestMove.score;
            }

            board.UndoMove(move);
            isMyTurn = !isMyTurn;
        }

        return evalMove;
    }

    //When its the enemies turn the scores are flipped.
    static float TurnMultipler()
    {
        if (isMyTurn)
            return 1;
        else
            return -enemyTurnMultiplier;
    }

    //Multplier that prioritises protecting high value pieces over taking pieces.
    static int EvaluatePieceValue(PieceType pieceType)
    {
        int pieceValue = pieceValues[(int)pieceType];

        if (isMyTurn)
            return pieceValue;
        else
            return (int)(myPieceMultiplier * pieceValue);
    }

    static Interest GetPieceTakeInterest(PieceType pieceType, int currentDepth)
    {
        switch (pieceType)
        {
            case PieceType.Pawn:
                if (RandomChanceToPass(1))
                    return Interest.MEDIUM;
                return Interest.NONE;

            case PieceType.Knight:
                if (RandomChanceToPass(10))
                    return Interest.MEDIUM;
                return Interest.NONE;

            case PieceType.Rook:
            case PieceType.Bishop:
                if (RandomChanceToPass(50))
                    return Interest.MEDIUM;
                return Interest.NONE;

            case PieceType.Queen:
                if (RandomChanceToPass(80))
                    return Interest.MEDIUM;
                return Interest.NONE;

            default:
                return Interest.NONE;
        }
    }

    //If square is closer to the center
    private bool SquareIsCloseToCenter(Square square)
    {
        if (square.File > 1 && square.File < 6 && square.Rank > 1 && square.Rank < 6)
        {
            return true;
        }

        return false;
    }

    private static bool RandomChanceToPass(int percentageChance)
    {
        //Seed rng
        if (rng == null)
        {
            rng = new Random(((int)DateTime.UtcNow.Ticks));
        }
        int randomValue = rng.Next(100);
        return randomValue < percentageChance;
    }

    private static int GetDepthDivision(int currentDepth)
    {
        if (currentDepth % 2 == 0)
        {
            return currentDepth / 2;
        }
        else
            return (currentDepth + 1) / 2;
    }
}