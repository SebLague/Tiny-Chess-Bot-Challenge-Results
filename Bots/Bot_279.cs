namespace auto_Bot_279;
using ChessChallenge.API;
using System;

//The piece values, search depth, max think time etc should be set by playing against itself many times, but who has time for that?

public static class BP
{
    public static float[] pieceValues = new float[] { 0f, 1f, 3f, 3f, 4.7f, 9f, 99999999f }; //none, pawn, knight, bishop, rook, queen, king
    public static float[] passedPawnRankValues = new float[] { 0f, 0f, 0.1f, 0.15f, 0.25f, 0.5f, 0.8f, 1f }; //value of moving a pawn forward *to* this rank if that makes it a passed pawn, assuming white (stacks with promotion value!)
    public static float promotionValue = 0.5f; //value of promoting a pawn relative to the piece it will become <-- setting this to 0.75 made no discernable difference
    public static float castleValue = 0.1f; //value of castling (tie-breaker)
    public static float checkValue = 0.1f; //value of pressuring our opponent (tie-breaker) <-- bot does about 50% better with this set to 0.1 compared to 0
    public static float drawValue = -1000f; //note that the repetition check triggers too soon (2 vs 3)
    public static int defaultThinkTime = 500; //milliseconds
    public static int maxSearchDepth = 8; //maximum depth we search moves to (only if we have time)
    public static int constrainedWidth = 3; //number of top moves to explore further <-- changing this to 3 lead to a 150% improvement wrt the default 5! (probably better: decrease width with layer) (doing 4 instead of 3 does not change the number of wins but does time out more often) (6,5,4,3,2,2 does about 33% worse than 3,3,3,3,3,3, and 5,4,3,2,2,2 is even worse)
} //BP

public class ScoredMoveSet : IComparable<ScoredMoveSet>
{
    public Move move;
    public ScoredMoveSet[] nextMoves;
    public float score;
    public float likelyTotalScore;
    public bool isWhite;
    public int layer;
    public int sign;
    public int winState; //0: normal, 1: in check, 2: draw, 3: checkmate
    private ulong enemyPawnBits;

    public ScoredMoveSet(Move m) : this(m, 0) { }
    public ScoredMoveSet(Move m, int l)
    {
        move = m;
        score = 0;
        likelyTotalScore = 0;
        layer = l;
        sign = 2 * ((layer + 1) % 2) - 1; //+1 for my moves, -1 for opponent moves
        winState = 0;
    } //ScoredMoveSet

    public void AddLegalMoves(Move[] n)
    {
        nextMoves = new ScoredMoveSet[n.Length];
        for (int i = 0; i < n.Length; ++i)
        {
            nextMoves[i] = new ScoredMoveSet(n[i], layer + 1);
        } //for
    } //AddLegalMoves

    public void DetermineState(Board board)
    {
        //winState=0;
        isWhite = board.IsWhiteToMove;
        board.MakeMove(move);
        if (board.IsInCheckmate()) winState = 3;
        else if (board.IsInCheck()) winState = 1; //it would be illegal to put myself in check, so this means the other king is in check
        if (board.IsDraw()) winState = 2;
        //Also check if square we move to is attacked? Or if there are non-pawns in the way?
        enemyPawnBits = board.GetPieceBitboard(PieceType.Pawn, !isWhite);
        board.UndoMove(move);
    } //DetermineState

    public void ScoreMove()
    {
        if (winState == 3)
        {
            score = sign * BP.pieceValues[6];
            //score=sign*(BP.pieceValues[6]-10*layer); //go for earlier checkmates <-- did not lead to improvement!
        } //if
        else if (winState == 2)
        {
            score = BP.drawValue; //draw (unsigned because undesirable for me regardless)
        } //else if
        else
        {
            score = sign * (BP.pieceValues[(int)move.CapturePieceType] + BP.promotionValue * BP.pieceValues[(int)move.PromotionPieceType] + BP.checkValue * winState);
            //Castling may help
            if (move.IsCastles) score += sign * BP.castleValue;
            //Passed pawn check (note: this only checks if a moved pawn is a passed pawn, not if the move made passed pawns available)
            //Also, probably should give a captured pawn a higher value if it was a passed pawn
            if (move.MovePieceType == PieceType.Pawn)
            {
                Square targetSquare = move.TargetSquare;
                if ((enemyPawnBits & PassedPawnMask(targetSquare.File, targetSquare.Rank, isWhite)) == 0)
                {
                    //We're a passed pawn
                    score += sign * BP.passedPawnRankValues[isWhite ? targetSquare.Rank : (7 - targetSquare.Rank)];
                } //if
            } //if
            /*
            else if ((int)move.MovePieceType<6) { //adding this does about 5% WORSE
                score+=sign*0.01f; //don't just move the king back and forth (had this without a sign first, but that means every trade looks like a net positive!)
            } //else if
            */
        } //else
        likelyTotalScore = score;
    } //ScoreMove

    private ulong PassedPawnMask(int fileIndex, int rankIndex, bool isWhite)
    {
        ulong fileA = 0x0101010101010101;
        //Line below optimized to stay below brain limit
        return ((fileA << fileIndex) | (fileA << Math.Max(fileIndex - 1, 0)) | (fileA << Math.Min(fileIndex + 1, 7))) & (isWhite ? (ulong.MaxValue << (8 * (rankIndex + 1))) : (ulong.MaxValue >> (8 * (8 - rankIndex))));
    } //PassedPawnMask

    public void ExploreDownToLayer(Board board, Timer timer, int maxThinkTime, int maxLayer)
    {
        //Branch downwards until we reach the target layer
        int constrainedWidth = nextMoves.Length;
        int exploredNextUpTo = 0;
        board.MakeMove(move);
        for (int i = 0; i < constrainedWidth && timer.MillisecondsElapsedThisTurn < maxThinkTime; ++i)
        {
            exploredNextUpTo++;
            if (nextMoves[i].winState < 2)
            {
                if (layer < maxLayer)
                {
                    nextMoves[i].ExploreDownToLayer(board, timer, maxThinkTime, maxLayer);
                } //if
                else
                {
                    nextMoves[i].ExploreCountermoves(board, timer, maxThinkTime);
                } //else
            } //if
            if (2 * timer.MillisecondsElapsedThisTurn >= maxThinkTime)
            {
                constrainedWidth = Math.Min(nextMoves.Length, BP.constrainedWidth);
            } //if
        } //for
        Array.Sort(nextMoves, 0, exploredNextUpTo);
        board.UndoMove(move);
        likelyTotalScore = score + nextMoves[0].likelyTotalScore;
    } //ExploreDownToLayer

    public void ExploreCountermoves(Board board, Timer timer, int maxThinkTime)
    { //assumes you checked that winState<2
        board.MakeMove(move);
        int exploredNextUpTo = 0;
        if (nextMoves == null)
        {
            AddLegalMoves(board.GetLegalMoves());
        } //if
        for (int i = 0; i < nextMoves.Length && timer.MillisecondsElapsedThisTurn < maxThinkTime; ++i)
        {
            nextMoves[i].DetermineState(board);
            nextMoves[i].ScoreMove();
            exploredNextUpTo++;
        } //for
        Array.Sort(nextMoves, 0, exploredNextUpTo);
        board.UndoMove(move);
        likelyTotalScore = score + nextMoves[0].likelyTotalScore;
    } //ExploreCountermoves

    public int CompareTo(ScoredMoveSet other)
    {
        return -sign * likelyTotalScore.CompareTo(other.likelyTotalScore); //sort by descending absolute score
    } //CompareTo
} //ScoredMoveSet

public class Bot_279 : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        Move[] moves = board.GetLegalMoves();
        ScoredMoveSet[] scoredMoves = new ScoredMoveSet[moves.Length];
        int constrainedWidth = BP.constrainedWidth;
        int maxThinkTime = BP.defaultThinkTime;
        int exploredUpTo;
        if (board.IsWhiteToMove && board.PlyCount == 0)
        {
            //Opening move
            return new Move("e2e4", board);
        } //if
        //The think time should really not be so rigid as this but I'm at the brain limit
        //Ideally I give more time when I'm in a tight spot (or when I don't see a good move) but that's not easy to determine
        if (timer.MillisecondsRemaining > 10000 && board.PlyCount > 5)
        {
            maxThinkTime = 2 * BP.defaultThinkTime;
            //We should get a bit more or a bit less time to think depending on the opponent's clock
            if (timer.OpponentMillisecondsRemaining < (timer.MillisecondsRemaining - maxThinkTime))
            {
                maxThinkTime += (timer.MillisecondsRemaining - maxThinkTime - timer.OpponentMillisecondsRemaining) / 2;
            } //if
            else if (timer.OpponentMillisecondsRemaining > (timer.MillisecondsRemaining + 2 * maxThinkTime))
            {
                maxThinkTime = BP.defaultThinkTime;
            } //else if
        } //if
        else if (timer.MillisecondsRemaining < (10 * BP.defaultThinkTime))
        {
            maxThinkTime = timer.MillisecondsRemaining / 10;
        } //else if
        //Score all legal moves and all their countermoves
        //Assuming the best countermove, calculate likely total scores
        for (int i = 0; i < moves.Length; ++i)
        {
            scoredMoves[i] = new ScoredMoveSet(moves[i]);
            scoredMoves[i].DetermineState(board);
            scoredMoves[i].ScoreMove();
            if (scoredMoves[i].winState < 2)
            {
                scoredMoves[i].ExploreCountermoves(board, timer, maxThinkTime);
            } //if
        } //for
        Array.Sort(scoredMoves); //sort by likelyTotalScore
        //Now explore deeper layers, doing a full breadth search until half our allowed
        //time is up. For the remainder, do a depth search, which only explores the 
        //top N moves per layer.
        constrainedWidth = scoredMoves.Length;
        for (int l = 0; l < BP.maxSearchDepth && timer.MillisecondsElapsedThisTurn < maxThinkTime; ++l)
        { //loop over layers
            exploredUpTo = 0;
            for (int i = 0; i < constrainedWidth && timer.MillisecondsElapsedThisTurn < maxThinkTime; ++i)
            {
                exploredUpTo++;
                if (scoredMoves[i].winState < 2)
                {
                    scoredMoves[i].ExploreDownToLayer(board, timer, maxThinkTime, l);
                } //if
                if (2 * timer.MillisecondsElapsedThisTurn >= maxThinkTime)
                {
                    constrainedWidth = Math.Min(scoredMoves.Length, BP.constrainedWidth);
                } //if
            } //for
            Array.Sort(scoredMoves, 0, exploredUpTo);
        } //for
        return scoredMoves[0].move;
    } //Think
} //MyBot
