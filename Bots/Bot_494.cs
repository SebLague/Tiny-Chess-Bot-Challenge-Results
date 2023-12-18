namespace auto_Bot_494;
using ChessChallenge.API;
using System;
using System.Linq;

/// <summary>
/// Note I am quite confident that this bot isn't going to win but hopefully this if going to be quite a strong bot that is quite token compact,
/// as a side note, I am not really good at making chess bots as some of the features and implementation did come from another open source bot. 
/// Where I am most confident in is token optimization as I have been programming in C# for around 5 years and know the ins and outs of the language
/// as well as some of the low level functions
/// </summary>
public class Bot_494 : IChessBot
{
    Move bestrootmove;

    //Transpostion Table (ulong key, Move move, int depth, int score, int bound)
    (ulong, Move, int, int, int)[] tt;

    //decompressed is a lookup table that is condensed into a decimal array where each byte of the 768 byte array contains the piece values and square scores
    //for "midgame" and "endgame" note the true in ToByteArray to specify that our numbers are encoded as positive numbers, then the numbers are expanded and shifted
    //to a more reasonable scale
    //This lookup table I had many ideas of how to reduce the token count, most just made the bot play worse than before
    //One idea that came close and didn't have time to make was to make the "midgame" and "endgame" values into a 32 bit value where the upper 16 bits were "midgame"
    //and the lower 16 bits were "endgame" this would reduce the size of the lookup table in half and make the eval function only look at the array once per piece
    //instead of twice. Sadly didn't have enough time to test this out
    //We keep the lookup table to the class as it would be too expensive to compute this table each time we call Think
    int[] decompressed = new[] { 17078798320628004012597322274m, 9333125444692524849785552433m, 11491000994506709063692395807m,
        9643767193844008032108028710m, 10563755001543880993352327706m, 11495851031481492121365652002m, 15850506097695417493575191372m, 12427937542397017557944447025m,
        11493437920583430004736796456m, 10876881067323431980510749733m, 24859477897109925765908342053m, 27036909915508042251286037864m, 31682764487639182077742770256m,
        27659464247841063978382813795m, 27960410978821928488923912277m, 26410591744955314614102875738m, 24227233657148759196861941313m, 21444286532810235342995671116m,
        25786752725013150989425004362m, 22682231331427084577713115730m, 23299945053073219425472891461m, 29519963194250865918452386377m, 32624569738373786526510048347m,
        29829533466370946603768441960m, 31069867597095993848715502431m, 30146253011723926646968117092m, 24854670993176552123620221017m, 24238128324308118753512411216m,
        25789199040555512973780734034m, 24857121993181397119461905493m, 25165369650172923337596162383m, 24236914657745363333492396114m, 41943079200831958700934595456m,
        38849452158499688575890589835m, 37592098248375645279755138932m, 35426935883370435720997730427m, 38833674582847172418427254384m, 40392046356039515068758588285m,
        40392041615081608870935954563m, 40080129271922804546630353026m, 40078920327510921073207247490m, 39768216928834929700252909183m, 76125997110477970443776196991m,
        78911423653508744625184898303m, 74256969468557107527503114224m, 75190288645094419995357804274m, 75502186655351228764179723247m, 71777467389509964344098748148m,
        72079651250512640499100476637m, 69909643830162646583757628652m, 72385518927899158068905436383m, 69597726653308368900466403046m, 66184881655824445429035621082m,
        5899595834933605395550886110m, 3733262359571037410193772566m, 2491643504635523350337491211m, 1870269873878609464820109317m, 5592547620763608038526224646m,
        3728365007998395665681160205m, 5596231326173110746976686861m, 6834157087608497328734147602m, 4045165369473914621557872406m, 5900828501784335416611180300m,
        1870269819115403136465834259m}.SelectMany(x => new System.Numerics.BigInteger(x).ToByteArray(true)).Select(x => (int)(x * 4.524) - 74).ToArray();


    public Move Think(Board board, Timer timer)
    {
        //It doesn't reallocate the array if the size is the same
        //(see https://source.dot.net/#System.Private.CoreLib/src/libraries/System.Private.CoreLib/src/System/Array.cs,71074deaf111c4e3,references)
        //and saves 3 tokens over just using new (ulong, Move, int, int, int)
        //Sadly using alias for tuples (see https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-12.0/using-alias-types) is a C# 12 feture
        //.Net 6 by default uses C# 10 if a C# version isn't specified in the project config
        Array.Resize(ref tt, 1_000_000);
        var killermoves = new Move[512]; //We allocate new arrays for killer moves and history as the GC penalty won't be that large (hopefully)
        var history = new int[2, 7, 64];
        //Spend no more than
        int depth = 1, timeallocated = timer.MillisecondsRemaining / 30, alpha = -30_300, beta = 30_300, eval;

        //Itterative deepening with a asperation window for time control
        for (; ; )
        {
            //Do a search
            eval = Search(alpha, beta, depth, 0, true);

            //Out of time check
            if (timer.MillisecondsElapsedThisTurn >= timeallocated) return bestrootmove;

            //If the evaluation is outside alpha or beta, do a re-search with a wider upper and lower bound
            if (eval <= alpha) alpha -= 62;
            else if (eval >= beta) beta += 62;
            else
            {
                //Best case if our narrow search result is in between alpha and beta. Then we can go to the next depth
                alpha = eval - 17;
                beta = eval + 17;
                depth++;
            }
        }

        //We make use of a lot of local function so we don't store variables in the class, we pay a small cost of putting all captured variables into a struct and passing
        //the struct by reference
        //(https://sharplab.io/#v2:D4AQTABCCMDsCwAoA3ki6oBYoFYAuAlgPYB2AFAJRoaqIb0QEl4QCGEAvBDgNzUONmEAEacIsPnQER+DJiwCSJEgFMATiHzFyFTgD42EANQjJ0mVPQBfJFaA)
        //Also note that null window search, "search", and quiescence search are all packed into 1 function to save tokens
        int Search(int alpha, int beta, int depth, int ply, bool allownull)
        {
            //Variable initialization
            ulong key = board.ZobristKey;
            bool qsearch = depth <= 0, notRoot = ply > 0, incheck = board.IsInCheck(), canEFP = false;
            Move bestMove = default;
            int best = -30_300, movelooked = 0, score = 0, origalpha = alpha, moveused = 0;

            if (notRoot && board.IsRepeatedPosition()) return 0; // Check for repetition

            var (_key, _move, _depth, _score, _bound) = tt[key % 1_000_000]; //Deconstruct the entry into diffrent variables to save tokens
            //     TT cutoffs                                 |exact score|  |    lower bound, fail high  |   |    upper bound, fail low    |
            if (notRoot && _key == key && _depth >= depth && (_bound == 3 || _bound == 2 && _score >= beta || _bound == 1 && _score <= alpha)) return _score;
            //Check extentions 
            if (incheck) depth++;

            int localsearch(int newalpha, int r = 1, bool _allownull = true) => score = -Search(-newalpha, -alpha, depth - r, ply + 1, _allownull);

            #region QSearshAndNullWindowSearch
            if (qsearch)
            {
                best = Eval();
                if (best >= beta) return best;
                alpha = Math.Max(alpha, best);
            }
            else if (!incheck && beta - alpha == 1) //Only do this block if we are in a null window search
            {
                int staticeval = Eval();
                //Reverse Futility Pruning, done before Extended Futility Pruning
                if (depth <= 10 && staticeval - 96 * depth >= beta) return staticeval;
                if (depth >= 2 && allownull)
                {
                    board.ForceSkipTurn();
                    localsearch(beta, 3 + (depth >> 2), false); //Null move pruning, don't look at moves that are worse than not doing anything
                    board.UndoSkipTurn();

                    // Failed high on the null move
                    if (score >= beta) return score;
                }
                //Extended Futility Pruning, found that 128 works as a constant to multiply with depth
                canEFP = depth <= 8 && staticeval + depth * 128 <= alpha;
            }
            #endregion
            //Note we can't use var for the type for some reason the default type for a stackalloc expression is a pointer type which is unsafe 
            Span<Move> moves = stackalloc Move[218];
            board.GetLegalMovesNonAlloc(ref moves, qsearch); // Generate moves, only captures if we are in qsearch
            Span<int> scores = stackalloc int[moves.Length];

            #region MoveSorting
            //Score the Moves                                     |   Transposition table   |            Most Valuable Victim,  Least Valuable Aggressor                        |           Killer Moves             |                     History Moves                               |
            foreach (Move move in moves) scores[movelooked++] = -(move == _move ? 9_000_000 : move.IsCapture ? 1_000_000 * (int)move.CapturePieceType - (int)move.MovePieceType : killermoves[ply] == move ? 900_000 : history[ply & 1, (int)move.MovePieceType, move.TargetSquare.Index]);
            scores.Sort(moves); //Sort to see what moves we should look at first
            #endregion

            #region SearchMoves
            foreach (Move item in moves)
            {
                if (timer.MillisecondsElapsedThisTurn >= timeallocated && depth > 1) return 30_000;
                //Where Extended Futility Pruning is used to skip looking into moves 
                if (canEFP && !(moveused == 0 || item.IsCapture || item.IsPromotion)) continue;
                board.MakeMove(item);
                #region PV&LMR
                if (qsearch || moveused++ == 0 || ((moveused < 6 || depth < 2 || localsearch(alpha + 1, 3) > alpha) && localsearch(alpha + 1) < beta)) localsearch(beta);
                #endregion
                board.UndoMove(item);
                if (score <= best) continue; //If the move isn't the best seen so far, just continue
                if (!notRoot) bestrootmove = item;
                best = score;
                bestMove = item;
                alpha = Math.Max(alpha, score); // Improve alpha
                #region KillerHistoryStoring
                if (alpha >= beta)
                {
                    if (!item.IsCapture)
                    {
                        history[ply & 1, (int)item.MovePieceType, item.TargetSquare.Index] += depth * depth;
                        killermoves[ply] = item; //If it isn't a capture, store in the killer moves 
                    }
                    break; // Fail-high
                }
                #endregion
            }
            #endregion
            // (Check/Stale)mate and draw detection
            if (!qsearch && moves.IsEmpty) return incheck ? ply - 30_000 : 0;

            // Push the best move to TT
            tt[key % 1_000_000] = (key, bestMove, depth, best, best >= beta ? 2 : best > origalpha ? 3 : 1); // Did we fail high/low or get an exact score?

            return best;
        }


        /// <summary>
        /// This is the static evaluation function
        /// </summary>
        int Eval()
        {
            //If scope is just 1 line, { and } aren't required, (still makes for some unreadable code)
            int mg = 0, eg = 0, stage = 0, sidetomove = 2, piece, index;
            //sidetomove gets set to 1 then to zero where we negate the mg and eg values
            for (; --sidetomove >= 0; mg = -mg, eg = -eg)
                for (piece = -1; ++piece < 6;) //loops piece from 0 to 5
                    for (ulong mask = board.GetPieceBitboard(piece + PieceType.Pawn, sidetomove > 0); mask != 0;) //it is faster to grab each peice bitborad than to loop over 1 bitboard and then grab the peice type
                    {
                        //Instead of using a lookuptable like for { 0, 1, 1, 2, 4, 0 }, we can use a 18 bit number (seen by 0x4448) 3 bit for each element
                        //And use bitshifts to acsess the items to save 5 tokens over creating an array (and it might be faster, IDK)
                        //Stage is used for how "endgame" our position is
                        stage += 0x4448 >> piece * 3 & 7;
                        mg += decompressed[index = piece * 128 + (BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ 56 * sidetomove)];
                        eg += decompressed[index + 64];
                    }
            return (mg * stage + eg * (24 - stage)) / 24 * (board.IsWhiteToMove ? 1 : -1);
        }
    }
}