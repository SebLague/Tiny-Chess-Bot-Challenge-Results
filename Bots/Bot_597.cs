namespace auto_Bot_597;
using ChessChallenge.API;
using System;

public class Bot_597 : IChessBot
{

    /* OLD TO-DO [coming back after a while ima just leave this here just incase but probably just ignore it]
     * 
     * * After the main search, have a searchAllCaptures function that just will just keep looking until all captures are done,
     * then that final position can be evaluated.
     * 
     * * Don't have a fixed depth and instead just start at depth 1 ply, and keep increasing (using the previous depths as ordering
     * to help α-β pruning) until certain time condition that forces it to move, maybe once the msThisTurn * 10 < msRemaining then
     * stop. This will hopefully fix the running out of time problem, but also just drastically increase time efficiency by pruning
     * more.
     * 
     * * I'm pretty sure the defense stuff isn't fully working since those squares are blocked for sliding pieces.
     * 
     * * Run a little tournament to find weights for the stats.
     * 
     * * Reduce tokens by simplying alpha and beta bits, u can simply swap alpha and beta and negate both of them, rather than
     * using the white to move to determine which one to take and have basically double the code for that bit.
     * 
     */

    private int curDepth = 4; //fine with starting a low depth for the first move
    private int lastTime = 0;
    private bool start = true;
    private int moves = 0;
    private Random rnd;
    private Timer timer;

    public Move Think(Board board, Timer timer)
    {
        this.timer = timer;
        if (start)
        {
            rnd = new Random();
        }
        else
        {
            if (timer.MillisecondsRemaining < lastTime * Math.Max(10, 40 - moves) && curDepth > 1) curDepth--; // if under time pressure search less
            if (timer.MillisecondsRemaining > lastTime * 250) curDepth++; // if little time pressure search more
        }
        start = false;
        (Move foundMove, float eval) = search(board, board.IsWhiteToMove, curDepth, float.NegativeInfinity, float.PositiveInfinity);
        lastTime = timer.MillisecondsElapsedThisTurn;
        /*Log(board.IsWhiteToMove ? "White" : "Black", false, ConsoleColor.White);
        Log("Eval: " + eval, false, ConsoleColor.Yellow);
        Log("Depth: " + curDepth, false, ConsoleColor.Red);*/
        /*board.MakeMove(foundMove);
        Log("White Eval: " + baseEval(board, true, true), false, ConsoleColor.Magenta);
        Log("Black Eval: " + baseEval(board, false, true), false, ConsoleColor.Magenta);
        board.UndoMove(foundMove);*/
        moves++;
        return foundMove;
    }

    public (Move, float) search(Board board, bool whiteToPlay, int depth, float alpha, float beta)
    {
        Move[] moves = board.GetLegalMoves(depth < 1);
        float bestEval = whiteToPlay ? float.NegativeInfinity : float.PositiveInfinity;
        int bestIndex = 0;
        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            board.MakeMove(move); // try move
            //float thisEval = score(board, !whiteToPlay, depth, -beta, -alpha); // get eval of move
            float thisEval = score(board, !whiteToPlay, depth - 1, alpha, beta); // get eval of move
            if (thisEval > 100000.0f) thisEval--;
            else if (thisEval < -100000.0f) thisEval++;
            if (whiteToPlay ? thisEval > bestEval : thisEval < bestEval) // check if new best move
            {
                bestEval = thisEval;
                bestIndex = i;
            }
            board.UndoMove(move); // reset board (importantly this happens in the right order)

            if (whiteToPlay)
            {
                alpha = Math.Max(alpha, thisEval);
                if (beta <= alpha) break;
            }
            else
            {
                beta = Math.Min(beta, thisEval);
                if (alpha >= beta) break;
            }

            //alpha = Math.Max(alpha, whiteToPlay ? thisEval : -thisEval);
            //if (alpha >= beta) break;
        }
        return (moves[bestIndex], bestEval);
    }

    public float score(Board board, bool whiteToPlay, int depth, float alpha, float beta)
    {
        //trivial evals
        if (board.IsInCheckmate()) return whiteToPlay ? -200000.0f : 200000.0f;
        if (board.IsDraw()) return 0;
        if (depth > 0) // search more
        {
            (Move foundMove, float eval) = search(board, whiteToPlay, depth, alpha, beta);
            return eval;
        }
        else if (board.GetLegalMoves(true).Length > 0 && timer.MillisecondsRemaining > timer.MillisecondsElapsedThisTurn * Math.Max(10, 40 - moves)) // search for captures if u have time
        {
            (Move foundMove, float eval) = search(board, whiteToPlay, 0, alpha, beta);
            return eval;
        }
        else
        { // eval as it is
            return baseEval(board, true) - baseEval(board, false);
            //return baseEval(board, true, false) - baseEval(board, false, false);
        }
    }

    public float baseEval(Board board, bool white)//, bool display)
    {
        /* Eval Explained
         * 
         * Pretty much everyone is going to have the same minimax searching and stuff, so in order to win I need to focus on the
         * eval. You can just do the trivial thing of using the known average material values, but that is kind of lame, and also
         * just ignores a lot of things that can make positions better or worse. You can do what you did and have like tables of
         * good squares and such. But I thought this skips the most obvious thing, why not just see how good the pieces are for
         * myself? So yeah I'm not putting on any of my values for pieces, here we will just see how valuable they are. Also
         * with some slight modifications I imagine this will let you calculate how good pieces are in a game which is cool.
         * 
         * Anyways so on to what I ended up doing. I calculated 5 stats, hp is the amount of pieces you have, honestly not really
         * important I just thought it fit the theme and its short enough to add, attack is the amount of squares you attack
         * (this includes squares you attack multiple times), defense is the amount of squares you defend, speed is the amount
         * of squares you can move without capture to (useful for seeing squares you control, or have the potential to capture on),
         * and finally dexterity is the amount of squares you could move to if there were no other pieces on the board (useful
         * to see if it's a useful piece, and if it's in a useful spot).
         * 
         * Then I just did a bit of trial and error to find the best weights for combining them. And then well combined them.
         * Also you will see when I use this function I calculate both black and white and subtract them. As a move might give you
         * a good position but your opponent an even better one. And just a general note this isn't normalised, so the eval this
         * gives is not directly comparable to a typical eval, I guess you'd have to take the average eval difference of a bunch
         * of positions with and without a random pawn and use that to scale it.
         * 
         */

        //Get all of your pieces
        PieceList pawns = board.GetPieceList(PieceType.Pawn, white);
        PieceList knights = board.GetPieceList(PieceType.Knight, white);
        PieceList bishops = board.GetPieceList(PieceType.Bishop, white);
        PieceList rooks = board.GetPieceList(PieceType.Rook, white);
        PieceList queens = board.GetPieceList(PieceType.Queen, white);
        Square king = board.GetKingSquare(white);

        //Setup
        int hp = pawns.Count + 3 * knights.Count + 3 * bishops.Count + 5 * rooks.Count + 9 * queens.Count;
        int attack = 0;
        int defense = 0;
        int speed = 0;
        int dexterity = 0;
        ulong friendlybb = white ? board.WhitePiecesBitboard : board.BlackPiecesBitboard;
        ulong enemybb = white ? board.BlackPiecesBitboard : board.WhitePiecesBitboard;
        ulong squares = 0;

        //Pawns
        for (int i = 0; i < pawns.Count; i++)
        {
            squares = BitboardHelper.GetPawnAttacks(pawns[i].Square, white);
            attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
            defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
            speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
            dexterity += BitboardHelper.GetNumberOfSetBits(squares);
        }

        //Knights
        for (int i = 0; i < knights.Count; i++)
        {
            squares = BitboardHelper.GetKnightAttacks(knights[i].Square);
            attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
            defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
            speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
            dexterity += BitboardHelper.GetNumberOfSetBits(squares);
        }

        //Bishops
        for (int i = 0; i < bishops.Count; i++)
        {
            squares = BitboardHelper.GetSliderAttacks(PieceType.Bishop, bishops[i].Square, board);
            attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
            defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
            speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
            dexterity += BitboardHelper.GetNumberOfSetBits(squares);
        }

        //Rooks
        for (int i = 0; i < rooks.Count; i++)
        {
            squares = BitboardHelper.GetSliderAttacks(PieceType.Rook, rooks[i].Square, board);
            attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
            defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
            speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
            dexterity += BitboardHelper.GetNumberOfSetBits(squares);
        }

        //Queens
        for (int i = 0; i < queens.Count; i++)
        {
            squares = BitboardHelper.GetSliderAttacks(PieceType.Queen, queens[i].Square, board);
            attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
            defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
            speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
            dexterity += BitboardHelper.GetNumberOfSetBits(squares);
        }

        //King
        squares = BitboardHelper.GetKingAttacks(king);
        attack += BitboardHelper.GetNumberOfSetBits(squares & enemybb);
        defense += BitboardHelper.GetNumberOfSetBits(squares & friendlybb);
        speed += BitboardHelper.GetNumberOfSetBits(squares & ~enemybb & ~friendlybb);
        dexterity += BitboardHelper.GetNumberOfSetBits(squares);

        /* Return overall stats with weightings 
         * I ran many hours of testing these should be fairly optimal, but games do last like 2min most of the time,
         * with me trying to run each case about 7-8 ish times to give it a fair shot thats 15min per case, meaning
         * I can only do 4 per hour, which given the amount of chances is not really that much even though hours
         * of testing sounds like a lot. So who knows if these are even optimal. Also keep in mind the weightings
         * have to keep in mind the typical range of values, like dexterity is always really high so it having the
         * lower score doesn't mean its less important, I left my display code to see which contributes the most in a position.
         */

        /*if (display)
        {
            Log("HP: " + hp + "~" + hp * 1.2f, false, ConsoleColor.DarkCyan);
            Log("ATK: " + attack + "~" + attack * 2.9f, false, ConsoleColor.DarkCyan);
            Log("DEF: " + defense + "~" + defense * 1.4f, false, ConsoleColor.DarkCyan);
            Log("SPD: " + speed + "~" + speed * 0.6f, false, ConsoleColor.DarkCyan);
            Log("DEX: " + dexterity + "~" + dexterity * 0.5f, false, ConsoleColor.DarkCyan);
        }*/
        float eval = hp * 1.2f + attack * 2.9f + defense * 1.4f + speed * 0.6f + dexterity * 0.5f;
        //if (display) Log("OVERALL: " + eval, false, ConsoleColor.DarkCyan);
        if (moves < 5)
        {
            eval *= ((float)rnd.Next(95, 105)) / 100.0f; //opening spice
        }
        //if (display) Log("FINAL: " + eval, false, ConsoleColor.DarkCyan);
        return eval;
    }
}