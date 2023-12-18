namespace auto_Bot_106;
using ChessChallenge.API;
using System;
using System.Collections.Generic;

//****************************
// Miluva by Moehrchen :)
//****************************
//[v2] Estimated Chess.com rating: 2550 (1024 Tokens used)

#region ** V2 PROGRESS COMMENTS **

// { v2 Progression: }
// 1. Improved performance of Move sorting by ~86% (1 million now in ~55ms instead of ~850ms)
// 2. Improved performance of Evaluation by ~23% (1 million now in ~650ms instead of ~870ms)
// -> Also tried a bitboard version, which would save all of the square pre calculations, but would be less performant (Would be good for tokens) -> 16
// 3. Tried to optimize the positioning bitboard tables, by comparing matches against stockfish and against the previous version. The optimizations I found were rather small here afaik.
// 4. Improved the move sorting by differentiating between three different move types: Captures, Pieces which are getting attacked by an enemy piece, and others. (Since 12 deleted)
// 5. Tried to implement sliding piece bitboards, to evaluate more active pieces higher, which turned out to not end up successfully, since the current positional evaluation doesnt cooperate with it smoothly.
// -> For the bishop this technique seemed to be okayish, so I tried around with many different algebraic functions and square combinations, which all were worse than the current system.
// 6. Since the token limit got exceeded by quite a lot now, I tried to save some here and there. The code should be quite compact now.
// 7. Fixed a very bad bug with the special quioscience search extension, where all scenerious got played further. This fixes now some bad blunders and increases the depth of many searches.
// -> Additionally it looks now up to 12 moves into that specific action instead of only 1
// 8. Reworked the sorting function on a way where it stays with its performance, but saves 10 tokens. (Since the 3 parts update the sorting is at ~210ms)
// 9. After figuring out the problem with negating the max value of an int; Ive switched to the negamax algorithm, to save a lot of code and being able to put the sort function inside of the minimax function itself
// -> As well as deleting the extra Minimax call; which required to add iterative deepening even to the first move the bot plays
// 10. Lookup Table for best move of already calculated nodes (PV-Nodes)
// -> This was for some reason worse than without (also with storing the three best moves per node or by finding the doubled evaluations in a dictionary and not evaluating again) Espacially the last is wondering me. -> 15
// 11. Added actual quiescense search (Also tried out to evaluate a checked position not as quiet, which was not really good for some reason) -> solution at 13
// 12. Added history heuristic and some stuff related to that about the timing to make it work better
// 13. Added Check Extensions for the standard Minimax Function
// 14. Tried to add Countermove Heuristic, which didnt seemed to pay off (Was pretty much equal)
// 15. Successfully implemented a transposition table by storing the beta cutoffs as well
// 16. After optimizing the raw bitboard positional evaluation idea (2) as far as I could, I was able to shave off around 40ms/mil and additionally gain a lot of tokens due to the removal of almost all precalculations
// -> At this point I was interested in getting a small benchmark of how good it is compared to v1 and it played 20 matches against it and has gotten 18|1|1
// 17. Tried a lot of different extensions for the evaluation, including Mobility, Kings Safety (multiple approaches) and Passed Pawns, from which I could only realistically take one. So Ive tried them all and it seemed like passed pawns give me the best result.
// -> This made my tokens not really managable anymore, so I needed to remove the rng opening feature & optimize a lot of minor minor things in terms of code reductions (This seems now like the limit of features I personally can fit into the 1024 tokens)
// -> And then I made a 60 game test and it went 21/20/19, compared to the non passed pawn thing, so I will continue now with different stuff :/ (all of this might be due to the bug Ive fixed in 22, but I had now already a pretty clear direction in which I want to go with the bot: small evaluation, big search)
// 18. Since all my tries to make the evaluation better failed, I worked further on my search. First I tried the classic killer heuristic with 3 moves per save, which turned out to not be useful enough.
// 19. Implemented Late Move Reductions (a bit different than usually implemented, but it worked best like that for me)
// 20. Implemented Null Move Pruning for all non TT Moves & only for positions with more than 1 move option
// 21. Implemented Delta Pruning for the Quiescence Search
// 22. Fixed a really bad bug in the evaluation function (because of forgetting to set two bracket pairs)
// 23. Added Evaluation for the pawn structure since that was possible with the rest of my tokens and slightly incrasing its playing strength
// 24. Since the positional 64-bits values all were off before, I reworked them quite a lot
// -> Now by comparing against Komodo21 which is rated 2500, it wins => so the v2 will have probably a rating of 2550 or so

//Link to doc: [https://seblague.github.io/chess-coding-challenge/documentation/]

#endregion

public class Bot_106 : IChessBot
{
    #region ** MAIN **

    //Initialize Bot
    public Bot_106()
    {
        //Precalculate the early to endgame transition
        int index = 0;
        while (index < 33) earlyGameMultiplier[index] = Math.Clamp(0.03125d * index++, 0d, 1d); //1-(-0.001x^2+1) is the other function which I used before 
    }

    public Move Think(Board board, Timer timer)
    {
        //Clearing the transposition table for this move
        transpositionTable.Clear();

        //Iterative deepening for the time management & for the move sorting
        int searchDepth = 0;
        do if (Math.Abs(Minimax(board, -30001, 30001, ++searchDepth, 0, true, true)) == 30000) break; //Breaking out as soon as found a checkmate (positive or negative) to prevent the bot from finding new checkmates with longer sequences 
        while (timer.MillisecondsElapsedThisTurn < timer.MillisecondsRemaining / 100);

        //Returning the stored move in the transposition table
        return transpositionTable[board.ZobristKey];
    }

    #endregion

    #region ** SEARCH **

    //The dictionary is limited on 1000000 max entries;
    //If you Calculate it as 8bytes+4bytes per element the max size is 11.44 MB
    //So even if it would take 20 times the amount of that calculation (240 Bytes per Element), it would be completely fine + the table will normally never fill up completely
    private Dictionary<ulong, Move> transpositionTable = new Dictionary<ulong, Move>();
    private int[,,] historyHeuristics = new int[2, 64, 64];

    private int Minimax(Board board, int alpha, int beta, int depth, int checkExtensions, bool lmprunable, bool nullMovePossible)
    {
        //Extending the search up to 18 times by one as soon as the last move was a check
        if (board.IsInCheck() && checkExtensions++ < 18) depth++;

        //Going into the quiescence search as soon as a terminal state got detected or the depth horizon got reached
        if (depth == 0 || board.IsInCheckmate() || board.IsDraw()) return Quiescence(board, alpha, beta);

        //Getting all legal moves (Not with the stackalloc function because of easier usage for the following move sorting)
        Move[] moveList = board.GetLegalMoves();

        int moveListLength = moveList.Length, player = board.IsWhiteToMove ? 0 : 1, bestEval = -30001, iterator = moveListLength, lmrThreshold = iterator - 9;
        int[] moveSorting = new int[moveListLength];

        //Trying to get the stored PV Move from the transposition table
        transpositionTable.TryGetValue(board.ZobristKey, out Move transposMove);

        while (iterator-- > 0)
        {
            Move m = moveList[iterator];

            //PV-Node
            if (transposMove.Equals(m)) moveSorting[iterator] = 2100000000;

            //Capture Evaluation or History Heuristic
            else moveSorting[iterator] = (m.IsCapture || m.IsPromotion) ? moveSorting[iterator] = 2000000000 + evaluationFactors[(int)m.CapturePieceType]
                                                                        : historyHeuristics[player, m.StartSquare.Index, m.TargetSquare.Index];
        }

        //Sorting all moves from worst to best
        Array.Sort(moveSorting, moveList);
        iterator = moveListLength;

        //Iterating from the best move to the worst (based on the sort)
        while (iterator-- > 0)
        {
            Move curMove = moveList[iterator];
            int eval = 0;

            //Null Move Pruning
            if (iterator + 1 < moveListLength && nullMovePossible && depth > 3 && board.TrySkipTurn())
            {
                eval = -Minimax(board, -beta, -beta + 1, depth - 3, checkExtensions, lmprunable, false);
                board.UndoSkipTurn();
                if (eval >= beta) return eval;
            }

            board.MakeMove(curMove);

            //Late Move Pruning
            bool fullDepthSearch = true;
            if (checkExtensions == 0 && lmprunable && !curMove.IsCapture && !curMove.IsPromotion && iterator < lmrThreshold && depth > 2)
                fullDepthSearch = (eval = -Minimax(board, -beta, -alpha, depth - 2, checkExtensions, false, true)) > alpha;

            //Normal Full Depth Recursive Minimax
            if (fullDepthSearch) eval = -Minimax(board, -beta, -alpha, depth - 1, checkExtensions, lmprunable, true);

            board.UndoMove(curMove);

            //Replacing the best stored move and the evaluation as soon as a better move got detected
            if (eval > bestEval)
            {
                transposMove = curMove;
                bestEval = eval;
                if (eval > alpha) alpha = eval;
            }

            //Beta Cutoff
            if (eval >= beta)
            {
                //Updating the history heuristics as soon as it isnt a promotion or a capture
                if (!curMove.IsCapture && !curMove.IsPromotion)
                    historyHeuristics[player, curMove.StartSquare.Index, curMove.TargetSquare.Index] += depth * depth;

                //Not returning instantly to save the cutoff node as a PV-Node to generate more cutoffs in the future
                goto EndMinimax;
            }
        }

    //Storing the result into the transposition table
    EndMinimax: if (transpositionTable.Count < 1000000) if (!transpositionTable.TryAdd(board.ZobristKey, transposMove)) transpositionTable[board.ZobristKey] = transposMove;

        return bestEval;
    }

    private int Quiescence(Board board, int alpha, int beta)
    {
        int stand_pat = PositionalEvaluation(board), iterator = -1;

        //Quiescence Beta Cutoff
        if (stand_pat >= beta) return beta;

        //Delta Pruning
        if (stand_pat < alpha - 100) return alpha;

        if (alpha < stand_pat) alpha = stand_pat;

        //Getting all capture moves
        Span<Move> moves = stackalloc Move[256];
        board.GetLegalMovesNonAlloc(ref moves, true);

        //Similar to the minimax function, but a fail hard framework without storing anything into the transposition table
        while (++iterator < moves.Length)
        {
            board.MakeMove(moves[iterator]);
            int eval = -Quiescence(board, -beta, -alpha);
            board.UndoMove(moves[iterator]);

            if (eval >= beta) return beta;
            if (eval > alpha) alpha = eval;
        }

        return alpha;
    }

    #endregion

    #region ** EVALUATION **

    //Factors which are indicating how important each piece is
    private readonly int[] evaluationFactors = { 0, 10, 30, 31, 50, 90, 0 };

    //4 ulongs for each piece to have the evaluation values of 0-15 for each square on which they can stand on
    //-> Trying to write a bitboard vertical copy version with only half of these entries would not be possible in less than 48 tokens afaik
    private readonly ulong[] squareULsEarlyGame = {
        72017853715900160, 71776377365128960, 71990781929324544, 71842089758883840,
        35578435946545152, 99343702425600, 66229404696576, 0,
        6690816, 283474549248, 2123956224, 0,
        16711704, 165, 24, 0,
        3938364, 15396, 24, 0,
        65366, 56, 0, 199,

        65082553657982720, 65051661571260160, 104098692864, 3997440,
        72724104248832, 26673262755840, 39841127202816, 0,
        6867549627088896, 25444899197288448, 168766444929024, 0,
        1729662632375353344, 11889503016258109440, 1729382256910270464, 0,
        4330277012414398464, 2610961883968045056, 1729382256910270464, 0,
        6268729206323019776, 4035225266123964416, 0, 14339461213547659264,
    }, squareULsEndGame = {
        72057589759731456, 72057589742960640, 71777218556067840, 72056494526300160,
        35604927304826880, 73110042181632, 258707030016, 65970697666560,
        66229406269440, 0, 0, 0,
        0, 18446744073709551420, 0, 0,
        707126099968, 280815279734784, 103481868288, 0,
        35604928818740736, 66125924401152, 103481868288, 0,

        65301099380211456, 4294967040, 1099494915840, 16776960,
        72724508474880, 26673865162752, 39841123270656, 3932160,
        66229406269440, 0, 0, 0,
        0, 4395513236313604095, 0, 0,
        707126099968, 280815279734784, 103481868288, 0,
        35604928818740736, 66125924401152, 103481868288, 0
    };

    private readonly double[] earlyGameMultiplier = new double[33];

    private int PositionalEvaluation(Board board)
    {
        //Returning 0 if the board position is a draw
        if (board.IsDraw()) return 0;

        //Evaluating checkmates as -30000
        if (board.IsInCheckmate()) return -30000;

        //Evaluating non terminal states
        PieceList[] pls = board.GetAllPieceLists();
        int eval = 0, totalPieceCount = BitboardHelper.GetNumberOfSetBits(board.AllPiecesBitboard), ccount = 0;
        double dPosEval = 0d;

        for (int i = 0; i < 12; i++)
        {
            int pt = i % 6 + 1, mult = i < 6 ? 1 : -1;

            //Evaluate piece values
            eval += evaluationFactors[pt] * pls[i].Count * mult;

            //Evaluate piece positioning
            bool b = mult == 1;
            ulong pieceUL = board.GetPieceBitboard((PieceType)pt, b);
            for (int iterator2 = 0; iterator2 < 4; iterator2++)
                dPosEval += mult * (1 << iterator2) * (BitboardHelper.GetNumberOfSetBits(squareULsEarlyGame[ccount] & pieceUL) * earlyGameMultiplier[totalPieceCount]
                          + BitboardHelper.GetNumberOfSetBits(squareULsEndGame[ccount++] & pieceUL) * (1d - earlyGameMultiplier[totalPieceCount]));

            //Evaluate Castling Rights & Pawn Structure
            if (pt == 1)
            {
                if (board.HasKingsideCastleRight(b) || board.HasQueensideCastleRight(b)) eval += mult * 6;

                ulong pawnUL = board.GetPieceBitboard(PieceType.Pawn, b);
                for (int j = pls[i].Count; j-- > 0;)
                    eval += mult * BitboardHelper.GetNumberOfSetBits(BitboardHelper.GetPawnAttacks(pls[i][j].Square, b) & pawnUL);
            }
        }

        //Adding the piece positioning score to the evaluation
        eval += (int)dPosEval;

        //To make the negamax minimax variation work, itll evaluate the position out of the perspective of the current player
        return board.IsWhiteToMove ? eval : -eval;
    }

    #endregion
}