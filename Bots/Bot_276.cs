namespace auto_Bot_276;
using ChessChallenge.API;
using System;

public class Bot_276 : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };
    //double numTargetBoards;
    //double numBoards;
    //int depth;
    int targetDepth = 2; //normally 6,2
    int onlyCapturesDepth = 4; //normally 8,4. might change to something like targetDepth+2. For captures only. Doing more than 8 when the board is full is very slow even if normal depth is low.
    ulong rank2 = 0b_00000000_00000000_00000000_00000000_00000000_00000000_11111111_00000000;
    ulong rank3 = 0b_00000000_00000000_00000000_00000000_00000000_11111111_00000000_00000000;
    ulong rank4 = 0b_00000000_00000000_00000000_00000000_11111111_00000000_00000000_00000000;
    ulong rank5 = 0b_00000000_00000000_00000000_11111111_00000000_00000000_00000000_00000000;
    ulong rank6 = 0b_00000000_00000000_11111111_00000000_00000000_00000000_00000000_00000000;
    ulong rank7 = 0b_00000000_11111111_00000000_00000000_00000000_00000000_00000000_00000000;
    ulong center = 0b_00000000_00000000_00011000_00111100_00111100_00011000_00000000_00000000;
    ulong edges = 0b_11111111_10000001_10000001_10000001_10000001_10000001_10000001_11111111;
    ulong kingCorners = 0b_01000110_00000000_00000000_00000000_00000000_00000000_00000000_01000110;
    ulong kingNotSafe = 0b_00101000_11111111_11111111_11111111_11111111_11111111_11111111_00101000;


    public Move Think(Board board, Timer timer)
    {
        int numPieces = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);
        //if numpieces is small manually increase onlycapturesdepth here... seems to help a bit
        if (numPieces < 10)
            onlyCapturesDepth = 50;

        //numBoards = 1;
        //depth = 0;
        //bestBranchScore = -30000;
        //numTargetBoards = 9000000;// 100 * timer.MillisecondsRemaining;

        Move[] allMoves = board.GetLegalMoves();
        ReorderMoves(allMoves); //gotta reorder here too cuz depth search returns index at the end, not move

        int bestI = DepthSearchRecursive(board, 1, -99900, 99900);


        while ((float)timer.MillisecondsElapsedThisTurn / timer.MillisecondsRemaining < 0.01)//(32.0 / numPieces / 100.0)) //(32.0/(numPieces-2))/120.0 would sometimes, thought it was slightly better than (32.0 / numPieces) / 100.0) //used to be 0.01 //this numPiece dependant calc makes it so I often have deeper searches, strengthens play a bit, does kinda waste time sometimes.
        {
            /*f (allMoves.Length >= 20 && targetDepth >= 6) //when there's a lot of pieces still on the board, this is too risky. FIX. this is also too risky after a check. need to eval how many pieces on board.
                break;*/
            //DivertedConsole.Write("zobers count:"+ZobersDict.Count);
            //ZobersDictEval.Clear();
            //ZobersDictWhiteToMove.Clear();
            targetDepth += 1;
            onlyCapturesDepth += 1;
            allMoves = board.GetLegalMoves(); //don't know why this is necessary to do again, but it is.
            ReorderMoves(allMoves); //don't know why this is necessary to do again, but it is.
            bestI = DepthSearchRecursive(board, 1, -99900, 99900);
            //DivertedConsole.Write(targetDepth);
            //float timeCalc = (float) timer.MillisecondsElapsedThisTurn / timer.MillisecondsRemaining;
            //DivertedConsole.Write(timeCalc);
        }
        //DivertedConsole.Write("targetDepth: "+targetDepth);
        targetDepth = 2;
        onlyCapturesDepth = 4;

        if (bestI >= allMoves.Length || bestI < 0)
        {
            //DivertedConsole.Write("uh oh, told to do a move index larger than move set. bestI="+bestI);
            bestI = 0;
        }
        //DivertedConsole.Write("zobers count: " + ZobersDict.Count);
        //ZobersDictEval.Clear();
        //ZobersDictWhiteToMove.Clear();
        //Move moveToPlay = allMoves[bestI]; //Before submit to competition, need to add a check that we're not out of bounds! Also there's a confirmed bug now that we can be out of bounds
        return allMoves[bestI];
    }

    //reordering to help with alpha beta pruning odds. looking at captures and promotions first
    void ReorderMoves(Move[] moveList)
    {
        int[] scores = new int[moveList.Length];
        int i = 0;
        foreach (var move in moveList)
        {
            //if (move.IsCapture) //performs better without this check
            scores[i] = pieceValues[(int)move.CapturePieceType] - pieceValues[(int)move.MovePieceType] / 16;
            //if (move.IsPromotion) //uncomment?
            scores[i] += pieceValues[(int)move.PromotionPieceType] - 100;
            //DivertedConsole.Write(pieceValues[(int)move.PromotionPieceType]);
            //if (move.IsCastles) //doesn't help any... maybe hurts a bit.
            //    scores[i] += 50;
            //scores[i] += pieceValues[(int)move.MovePieceType]; //just for debug
            //DivertedConsole.Write(scores[i]);
            i += 1;
        }
        Array.Sort(scores, moveList);
        Array.Reverse(moveList);
        //Array.Reverse(scores);
        //DivertedConsole.Write("scores in order are:");
        //foreach (var ii in scores)
        //DivertedConsole.Write(ii);
        return;// moveList;
    }

    // Return best move and the eval (int) of that move. if a Move is passed by reference, we should be able to just change bestMove and not return it's index...
    int DepthSearchRecursive(Board board, int myDepth, int alpha, int beta)
    {

        //int[] scores = new int[allMoves.Length];
        //DivertedConsole.Write(numBoards);
        //Move moveToPlay;

        Move[] allMoves = board.GetLegalMoves();
        if (allMoves.Length == 0)
        {
            //DivertedConsole.Write("no legal moves");
            //DivertedConsole.Write(myDepth);
            if (board.IsInCheck())
            {
                if (myDepth == 1)
                {
                    //DivertedConsole.Write("uhhh ohhhh, this isn't supposed to happen. checkmate at depth 1");
                    return 0;
                }
                //DivertedConsole.Write("depth search sees checkmate at depth:"); //this almost always happens at depth 6. sometimes at 5, rarely at 4. never above.. why?
                //DivertedConsole.Write(myDepth);
                return 33300 - myDepth * 200; //checkmate. With -myDepth, I think it's working now. This really doesn't seem to be working. Positive or negative, doesn't work. Computer prefers draw by repetition over checkmate. |Value| has to be less than original |alpha| or |beta|
            }
            if (myDepth == 1)
            {
                //DivertedConsole.Write("uhhh ohhhh, this isn't supposed to happen. stalemate at depth 1");
                return 0;
            }
            return 0; //stalemate
        }

        if (myDepth != 1 && board.IsDraw())
            return 0; //helps avoid draw by repetition

        ReorderMoves(allMoves); //reorder moves to help with alpha beta pruning. allMoves = 

        int score = 0;
        int bestScore = -77700; //i don't know how negative i should make this. i wouldn't think it matters
        int bestI = 0;
        int i = 0;
        if (myDepth < targetDepth)
        {
            //numBoards *= allMoves.Length;
            //DivertedConsole.Write(myDepth);
            //DivertedConsole.Write(numBoards);
            foreach (Move move in allMoves)
            {
                board.MakeMove(move);
                /*ulong zkey = board.ZobristKey;
                if (ZobersDictEval.ContainsKey(zkey))
                    score = -ZobersDictEval[zkey];
                else*/
                score = DepthSearchRecursive(board, myDepth + 1, -beta, -alpha);
                board.UndoMove(move);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestI = i;
                }
                if (score >= beta)
                    return -beta;// snip! this should prune the branch and result in exactly the same move but faster... i think it's working...
                if (alpha < score)
                    alpha = score; //alpha = maximum(alpha, score);
                i++;
            }
            //if (bestBranchScore == -30000)
            //bestBranchScore = bestScore;
            if (myDepth == 1) //if top of the depth, need to return best move instead of score because think function needs to know where best move is, not its score.
            {
                /*if (Math.Abs(bestScore) == 30000 || Math.Abs(bestScore) == 2000 || Math.Abs(bestScore) >= 2300 || Math.Abs(bestScore) <= -2300)
                {
                    DivertedConsole.Write("myDepth=1, bestScore is large");
                    DivertedConsole.Write(bestScore);
                    DivertedConsole.Write(bestI);
                }*/
                //DivertedConsole.Write(bestScore);
                return bestI;
            }
            return -alpha;
        }
        //if at maximum depth:
        //return -EvalBoard(board);
        return DepthSearchCapturesOnly(board, myDepth + 1); //make negative?. Positive seems to be best. Adding this search does make it ~twice as slow, but significantly stronger play.
    }

    int DepthSearchCapturesOnly(Board board, int myDepth)
    {

        Move[] allCaptures = board.GetLegalMoves(true);
        if (allCaptures.Length == 0 || myDepth >= onlyCapturesDepth)
            return -EvalBoard(board);


        int score;
        int bestScore = -77700; //i don't know how negative i should make this. i wouldn't think it matters
        foreach (Move move in allCaptures)
        {
            board.MakeMove(move);
            score = DepthSearchCapturesOnly(board, myDepth + 1);
            board.UndoMove(move);
            if (score > bestScore)
                bestScore = score;
        }
        return -bestScore;
    }

    //eval the board. positive is good for the player whose move it is?
    int EvalBoard(Board board)
    {
        if (board.IsDraw())
            return 0; //helps

        var allPieces = board.GetAllPieceLists();
        ulong whitePawns = board.GetPieceBitboard(PieceType.Pawn, true);
        ulong blackPawns = board.GetPieceBitboard(PieceType.Pawn, false);
        ulong whiteKing = board.GetPieceBitboard(PieceType.King, true);
        ulong blackKing = board.GetPieceBitboard(PieceType.King, false);
        int numPieces = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard);
        int materialCountW = allPieces[0].Count * pieceValues[1] + allPieces[1].Count * pieceValues[2] + allPieces[2].Count * pieceValues[3] + allPieces[3].Count * pieceValues[4] + allPieces[4].Count * pieceValues[5] + allPieces[5].Count * pieceValues[6];
        int materialCountB = allPieces[6].Count * pieceValues[1] + allPieces[7].Count * pieceValues[2] + allPieces[8].Count * pieceValues[3] + allPieces[9].Count * pieceValues[4] + allPieces[10].Count * pieceValues[5] + allPieces[11].Count * pieceValues[6];
        int eval = materialCountW - materialCountB;
        eval += 1 * BitboardHelper.GetNumberOfSetBits(whitePawns & rank5) + 2 * BitboardHelper.GetNumberOfSetBits(whitePawns & rank6) + 20 * BitboardHelper.GetNumberOfSetBits(whitePawns & rank7);
        eval -= 1 * BitboardHelper.GetNumberOfSetBits(blackPawns & rank4) + 2 * BitboardHelper.GetNumberOfSetBits(blackPawns & rank3) + 20 * BitboardHelper.GetNumberOfSetBits(blackPawns & rank2);
        eval += 3 * BitboardHelper.GetNumberOfSetBits(whiteKing & kingCorners);
        eval -= 3 * BitboardHelper.GetNumberOfSetBits(blackKing & kingCorners);
        eval += 1 * BitboardHelper.GetNumberOfSetBits(board.WhitePiecesBitboard & center);
        eval -= 1 * BitboardHelper.GetNumberOfSetBits(board.BlackPiecesBitboard & center);
        if (numPieces < 10) //in end game you want to corner their king and your king to join the battle
            eval += -3 * BitboardHelper.GetNumberOfSetBits(whiteKing & edges) + 3 * BitboardHelper.GetNumberOfSetBits(blackKing & edges);
        else if (numPieces > 20) //in opening/middle game, you want your king to be safe
            eval += -3 * BitboardHelper.GetNumberOfSetBits(whiteKing & kingNotSafe) + 3 * BitboardHelper.GetNumberOfSetBits(blackKing & kingNotSafe);
        //DivertedConsole.Write(tempKingEdgesEval);
        //eval += tempKingEdgesEval;

        if (!board.IsWhiteToMove)
            eval *= -1;

        if (board.IsInCheck())
            eval -= 50;

        //DivertedConsole.Write(numPawns4thRank);
        //DivertedConsole.Write(board.GetPieceBitboard(PieceType.Pawn, true));

        /*ulong zkey = board.ZobristKey;
        if (!ZobersDictEval.ContainsKey(zkey))
        {
            ZobersDictEval.Add(zkey, eval); //if have extra values, do need to check
            ZobersDictWhiteToMove.Add(zkey, board.IsWhiteToMove);
        }*/
        //ZobersDictEval.Add(board.ZobristKey, eval); //don't need to check if it's there already because depth search should've already done that?
        return eval;
    }

}