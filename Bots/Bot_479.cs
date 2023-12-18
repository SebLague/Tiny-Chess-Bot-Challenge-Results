namespace auto_Bot_479;
using ChessChallenge.API;
using System;

//V imports for reducing tokens, they exist in the allowed above namespaces V
//using static System.Array;//removed just in case it might break allowed namespace rule
//using static ChessChallenge.API.BitboardHelper;//used mostly in alt eval1
//using static System.Convert;//used mostly in alt eval1

public class Bot_479 : IChessBot
{
    //private uint baseEvalCalls;//for debug
    //private uint treeNodes;//for debug

    private Board board;//reduce tokens by not needing to pass it into every function

    private Move[] movePreAlloc = new Move[256];
    private byte[] bytePreAlloc = new byte[256];
    //private int[] bestCap = {0,0};


    //debug tokens:
    //class: 4
    //Think: 12
    //eval1: 2
    //evaln: 4
    //total: 22
    //plus moveStats: 298
    //----
    //allowed tokens: 1322
    //current tokens (last checked): 1266
    //free tokens 56

    //var increment is 2 tokens
    //var declare + init is 4 tokens
    //empty void method is 4 tokens
    //2 tokens per argument in function declaration
    //1 token per argument in function call

    //debug method, remove when done. This method and the calls account for 276 token brain capacity
    // private void moveStats(String type, int depth, int full_depth, int eval, int startTime, Timer timer, bool isWhiteMove)
    // {
    //     DivertedConsole.Write(
    //         (isWhiteMove ? "White  |  " : "Black  |  ")
    //         + type.PadRight(7)
    //         + "|  depth: " + (depth + "(" + full_depth + ")").PadRight(9)
    //         + "|  tree nodes: " + treeNodes.ToString("N0").PadRight(12)
    //         + "|  base eval calls: " + baseEvalCalls.ToString("N0").PadRight(12)
    //         + "|  eval: " + ((eval/2048.0)<0 ? "" + (eval/2048.0) : " "+(eval/2048.0)).PadRight(22)
    //         + "|  time: " + ((startTime - timer.MillisecondsRemaining)/1000.0d) + "s"
    //         );
    // }
    public Move Think(Board boardIn, Timer timer)//I hope I'm allowed to rename the variable, saves 2 tokens
    {
        board = boardIn;
        // public Move Think(Board board, Timer timer)
        // {
        //     this.board = board;

        //baseEvalCalls = 0;//for debug
        //treeNodes = 0;//for debug

        Move[] moves = board.GetLegalMoves(),
            bestMoves;
        moves_init_sorted(moves, 0);

        Move bestPrev = moves[0];//if eval is loosing, return this and hope opponent does not see mate

        bool isWhiteToMove = board.IsWhiteToMove;
        byte depth = 0, i, count,
            movesLen = (byte)moves.Length;
        byte full_depth = 0;//depth reached in first time bracket, for debug, remove later
        int t, eval,
            eval2,
            startTime = timer.MillisecondsRemaining,
            mateVal = isMatedVal(isWhiteToMove),
        //*
            timeLeftTargetLow = startTime * 995 / 1000,
            timeLeftTargetHigh = startTime * 90 / 100;
        /*/
            timeLeftTargetLow = startTime * 9999/10000,
            timeLeftTargetHigh = startTime * 999/1000;
        //*/

        // maybe add transposition table


        int[] evals = new int[movesLen],
            evals2;


        while (true)
        {

            count = 0;
            eval = mateVal;

            foreach (Move m in moves)// code duplication with evaln, see if it's possible to reduce
            {
                if (timer.MillisecondsRemaining < timeLeftTargetHigh)//stop calculating early if ran out of alloted time
                {
                    if (count == 0)
                    {
                        return bestPrev;
                    }
                    Array.Resize(ref moves, count);
                    Array.Resize(ref evals, count);
                    movesLen = count;
                    break;
                }

                board.MakeMove(m);
                eval2 = evaln(depth, eval, false);
                board.UndoMove(m);

                if (eval2 == -mateVal)
                {
                    //moveStats("Win ",depth,full_depth,eval2,startTime,timer,isWhiteToMove);
                    return m;
                }

                evals[count] = eval2;
                count++;

                if ((eval2 - eval) * boolToSign(isWhiteToMove) > 0)
                {
                    eval = eval2;
                }
                // if (isWhiteToMove)
                // {
                //     if (eval2 > eval)
                //     {
                //         eval = eval2;
                //     }
                // }
                // else
                // {
                //     if (eval2 < eval)
                //     {
                //         eval = eval2;
                //     }
                // }
            }

            if (eval == mateVal)
            {
                //moveStats("Lose",depth,full_depth,eval,startTime,timer,isWhiteToMove);
                return bestPrev;
            }

            //filter and sort

            bestMoves = new Move[movesLen];//variable name is not accurate anymore
            evals2 = new int[movesLen];
            count = 0;
            i = 0;
            t = timer.MillisecondsRemaining;

            foreach (Move c in moves)
            {
                if (t > timeLeftTargetLow)
                {
                    if (evals[i] != mateVal)//non loosing moves
                    {
                        bestMoves[count] = c;
                        evals2[count] = evals[i];
                        count++;
                    }
                }
                else if (evals[i] == eval)//best moves
                {
                    bestMoves[count] = c;
                    count++;
                }

                i++;
            }

            //enough time to calculate at least 2 moves in next depth (estimation).
            if (timeLeftTargetHigh < t - timer.MillisecondsElapsedThisTurn * 2)//2 is tweakable parameter
            {
                if (count < movesLen)
                {
                    Array.Resize(ref bestMoves, count);
                    Array.Resize(ref evals2, count);
                    movesLen = count;
                }

                moves = bestMoves;
                if (movesLen == 1)
                {
                    //moveStats("Best",depth,full_depth,eval,startTime,timer,isWhiteToMove);
                    return moves[0];
                }

                if (t > timeLeftTargetLow)
                {
                    full_depth++;
                    Array.Sort(evals2, moves);
                    if (isWhiteToMove)
                    {
                        Array.Reverse(moves);//best moves first
                    }
                }

                bestPrev = moves[0];
            }
            else
            {
                //moveStats("Rand",depth,full_depth,eval,startTime,timer,isWhiteToMove);
                return bestMoves[new Random().Next(count)];
            }


            depth++;
        }
    }


    private static readonly byte[] PIECE_VAL = { 0, 1, 3, 3, 5, 9, 11 };

    //public static readonly byte[] PIECE_CAP_CAP = {0,2,8,4,4,8,8};//possible capture targets
    //public static readonly sbyte[] PIECE_VAL_RANK = {0,1,2,2,3,4,5};
    private static readonly byte[] PAWN_DIST_VAL = { 0, 1, 3, 6, 10, 15 };//closer is worth more non linearly

    /*
    private int eval1()
    {
        //optimization attempt,
        //it seems to be over 2 times slower
        //pawns attacks might be the biggest slow down, maybe test this theory and make alternative
        //will probably revert or make hybrid
        
        //Pros vs last version:
        //-considers controlled open squares
        //-considers piece protection
        
        //Cons vs last version:
        //-slower
        //-does not consider promotion
        //-does not consider legality of moves/ captures
        //-uses more tokens
        
        //draw and checkmate should already be checked in parent evaln
        
        //maybe add some center control eval
        
        baseEvalCalls++;//for debug
        
        
        //bool isWhiteToMove = board.IsWhiteToMove;
        
        int eval = 0,
            pieceCount = 0,
            miniEval = 0,
            endgame = 0;
        
        //sbyte moveSign = boolToSign(board.IsWhiteToMove);
        bestCap[0] = bestCap[1] = 0;
        //ulong[] colorPieceBitboards = { board.WhitePiecesBitboard, board.BlackPiecesBitboard };
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            bool pieceColor = pieceList.IsWhitePieceList;
            sbyte pieceSign = boolToSign(pieceColor);
            byte c,
                bc,
                pieceColorByte = ToByte(pieceColor);
                //pieceType = (byte)pieceList.TypeOfPieceInList;
            pieceCount += pieceList.Count;
            //byte pieceInt = (byte)pieceList.TypeOfPieceInList;
            
            eval +=
                pieceSign
                * PIECE_VAL[(byte)pieceList.TypeOfPieceInList]
                * pieceList.Count;

            foreach (Piece piece in pieceList)//this seems to be the slow part
            {
                Square square = piece.Square;
                
                if (piece.IsPawn){//endgame pawn pushing
                    endgame += pieceSign * PAWN_DIST_VAL[pieceColor ? square.Rank - 1 : 6 - square.Rank];
                }
                
                ulong a2,
                    attacks = pieceList.TypeOfPieceInList switch
                {
                    PieceType.Pawn => GetPawnAttacks(square, pieceColor),
                    PieceType.Knight => GetKnightAttacks(square),
                    PieceType.King => GetKingAttacks(square),//maybe remove, 9 tokens
                    _ => GetSliderAttacks(pieceList.TypeOfPieceInList,square,board),
                };
                
                miniEval += pieceSign * (
                    GetNumberOfSetBits(attacks)//squares controlled/attacked/protected
                    //+ BitboardHelper.GetNumberOfSetBits(attacks & colorPieceBitboards[pieceColorByte]) * 2//protected pieces, maybe change to all pieces
                    + GetNumberOfSetBits(attacks & board.AllPiecesBitboard)//protected and attacked pieces
                    );
                
                // attacks &= colorPieceBitboards[Convert.ToByte(!pieceColor)];
                // if (attacks > 0)
                // {
                    bc = 0;

                    for (c = 1; c < 7; c++)//last checks for attacked king
                    {
                        a2 = attacks & board.GetPieceBitboard((PieceType)c, !pieceColor);
                        if (a2 > 0)
                        {
                            bc = PIECE_VAL[c];
                            miniEval += pieceSign * bc * GetNumberOfSetBits(a2) * 4;
                        }
                    }
                    
                    bestCap[pieceColorByte] = Math.Max(bc,bestCap[pieceColorByte]);
                // }
            }
        }
        
        eval *= 2048;

        bestCap[ToByte(board.IsWhiteToMove)] *= 4;

        eval += miniEval
                + (bestCap[1] - bestCap[0]) * 128//1 is white, 0 is black
                + endgame * ToByte(pieceCount < 16);

        //eval += miniEval;

        // if (pieceCount < 16)
        // {
        //     eval += endgame;
        // }
        
        return eval;
    }
    /*/
    private int eval1()
    {
        //draw and checkmate should already be checked in parent evaln

        //maybe add some center control eval

        //baseEvalCalls++;//for debug


        //bool isWhiteToMove = board.IsWhiteToMove;

        int eval = 0;
        sbyte moveSign = boolToSign(board.IsWhiteToMove);
        byte pieceCount = 0;
        foreach (PieceList pieceList in board.GetAllPieceLists())
        {
            pieceCount += (byte)pieceList.Count;
            //sbyte pieceSign = boolToSign(pieceList.IsWhitePieceList);
            //byte pieceInt = (byte)pieceList.TypeOfPieceInList;

            eval +=
                boolToSign(pieceList.IsWhitePieceList)
                * PIECE_VAL[(byte)pieceList.TypeOfPieceInList]
                * pieceList.Count;
        }

        eval *= 2048;

        //return eval;//for speed testing

        //how close pawns are to promotion
        if (pieceCount < 16)//is endgame
        {
            foreach (Piece piece in board.GetPieceList(PieceType.Pawn, true))
            {
                eval += PAWN_DIST_VAL[piece.Square.Rank - 1];
            }
            foreach (Piece piece in board.GetPieceList(PieceType.Pawn, false))
            {
                eval -= PAWN_DIST_VAL[6 - piece.Square.Rank];
            }
        }

        //maybe improve/optimize the possible moves/captures with bitboards
        //if that is done, moveCapVal won't be used here anymore and can probably be inlined in the other place it's used to save tokens

        Move[] moves = board.GetLegalMoves(true);


        int cap = 0;
        foreach (Move move in moves)//captures only
        {
            int cap2 = moveCapVal(move);

            if (cap2 > cap)
            {
                cap = cap2;
            }
            eval += cap2 * moveSign * 4;//all possible captures
        }

        eval += cap * moveSign * 512;//best capture, maybe do * 1024 or even more


        board.ForceSkipTurn();
        moves = board.GetLegalMoves(true);
        board.UndoSkipTurn();

        cap = 0;
        foreach (Move move in moves)
        {
            int cap2 = moveCapVal(move);

            if (cap2 > cap)
            {
                cap = cap2;
            }

            eval -= cap2 * moveSign * 4;//all possible captures,negative for other color
        }

        eval -= cap * moveSign * 128;//best capture, negative for other color

        return eval;
    }
    //*/

    private static sbyte boolToSign(bool b)
    {
        return (sbyte)(b ? 1 : -1);
    }

    private static int isMatedVal(bool colorIsWhite)
    {
        return -258048 * boolToSign(colorIsWhite);//-126 * 2048=-258048
    }

    private int evaln(byte n, int best_eval, bool best_eval_equal) //best_eval is for alpha beta pruning, investigate if proper alpha-beta needs a second value
    {
        //treeNodes++;//for debug

        if (board.IsDraw())
        {
            return 0;
        }

        bool isWhiteToMove = board.IsWhiteToMove;
        int eval = isMatedVal(isWhiteToMove), eval2;

        if (board.IsInCheckmate())
        {
            return eval;
        }

        if (n == 0)
        {
            //treeNodes--;//for debug

            return eval1();
            //maybe do similar thing to Sebastian's bot where it does a capture only search here before doing base eval. Not sure how to consider when it's better not to capture (e.g. only suicidal captures available).
            //maybe inline to save tokens
        }

        Move[] moves = board.GetLegalMoves();
        moves_init_sorted(moves, n);//last argument is a parameter, consider tweaking, n>0 is same as true means always check, false means never check

        //maybe do iterative deepening up to n, filter and sort each iteration, might improve alpha-beta pruning,
        //depth for loop here around the move loop
        n--;
        foreach (Move m in moves)
        {
            board.MakeMove(m);
            eval2 = evaln(n, eval, true);
            board.UndoMove(m);
            if (eval2 == isMatedVal(!isWhiteToMove))
            {
                return eval2;
            }
            if (best_eval_equal && eval2 == best_eval)
            {
                return best_eval;
            }

            if ((eval2 - eval) * boolToSign(isWhiteToMove) > 0)
            {
                eval = eval2;
                if ((eval - best_eval) * boolToSign(isWhiteToMove) > 0)
                {
                    return eval;
                }
            }

            // if (isWhiteToMove)//there might be a way to remove duplication and maybe reduce branching by refactoring to negamax, this whole block has 40 tokens
            // {
            //     if (eval2 > eval)
            //     {
            //         eval = eval2;
            //         if (eval > best_eval)
            //         {
            //             return eval;
            //         }
            //     }
            // }
            // else
            // {
            //     if (eval2 < eval)
            //     {
            //         eval = eval2;
            //         if (eval < best_eval)
            //         {
            //             return eval;
            //         }
            //     }
            // }
        }

        return eval;
    }

    private void moves_init_sorted(Move[] moves, byte depth)//helps alpha beta pruning
    {
        //return moves;

        byte countStart = 0,
            countEnd = (byte)(moves.Length - 1),
            cap, i;
        bool hasCap = false;

        foreach (Move m in moves)
        {
            cap = moveCapVal(m);
            // cap = (byte) (
            //     PIECE_VAL[(byte)m.CapturePieceType]
            //     + PIECE_VAL[(byte)m.PromotionPieceType]
            //     - ToByte(m.IsPromotion)//if promotion, -1 for pawn that is replaced
            // );

            if (depth > 1)//0 is very slow, 1 is about the same as 2, subject to tweaking, also this may change if moveIsCheck is optimized
            {
                board.MakeMove(m);
                if (board.IsInCheck())//this check is slow, or maybe it's the make/undo move, or both, consider optimizing
                {
                    cap += 2;//tweakable parameter
                }
                //cap += (byte)(Convert.ToByte(board.IsInCheck()) * 2);
                board.UndoMove(m);
            }

            if (cap > 0)
            {
                hasCap = true;
                movePreAlloc[countStart] = m;
                bytePreAlloc[countStart] = (byte)(32 - cap);//inverting for backwards sort
                countStart++;
            }
            else
            {
                movePreAlloc[countEnd] = m;
                countEnd--;
            }
        }

        if (hasCap)//if no cap, nothing to sort
        {
            for (i = 0; i < moves.Length; i++)
            {
                moves[i] = movePreAlloc[i];
            }

            Array.Sort(bytePreAlloc, moves, 0, countStart);
        }
    }


    //*
    private static byte moveCapVal(Move m)//also considers promotion value
    {
        return (byte)(
            PIECE_VAL[(byte)m.CapturePieceType]
            + PIECE_VAL[(byte)m.PromotionPieceType]
            - Convert.ToByte(m.IsPromotion)//if promotion, -1 for pawn that is replaced
            );
    }
    //*/

    /*
    //function inlined until it's needed in more than one place
    private static bool moveIsCheck(Move m, Board board)//28 tokens + calls
    {
        //this method is very slow, maybe make faster version
        //maybe inline it if only used once to reduce tokens
        board.MakeMove(m);
        bool check = board.IsInCheck();
        board.UndoMove(m);
        return check;
    }
    //*/

}

