namespace auto_Bot_604;
using ChessChallenge.API;
using System;
using System.Linq;

//
// Chess bot "Thunker Weed" for SebLague Chess Challenge
//
// Author: American Jeff
// 2023-10-01
//
// Thanks to https://github.com/Tyrant7/Chess-Challenge from which many
// token savings were gleaned.
//
public class Bot_604 : IChessBot
{
    readonly (ulong, Move, int, int, int)[] tt = new (ulong, Move, int, int, int)[0x700000];
    int timeLimit;
    Board board;
    Timer timer;
    Move bestMove;
    readonly int[] killer = new int[1300];
    int[,,] history;

    public Move Think(Board botBoard, Timer moveTimer)
    {
        board = botBoard;
        timer = moveTimer;
        history = new int[2, 64, 7];
        int depthLimit = 2, alpha = -66666, beta = 66666, score;

        //
        // Time allocation.
        //
        timeLimit = Math.Min(timer.GameStartTimeMilliseconds * 2 / 3, timer.MillisecondsRemaining) / 14;

        while (depthLimit < 66 && timer.MillisecondsElapsedThisTurn <= 0.34f * timeLimit)
        {
            score = Search(alpha, beta, 0, depthLimit, true);

            //
            // Aspiration window update.
            //
            if (alpha < score && score < beta)
            {
                alpha = score - 10;
                beta = score + 10;
                depthLimit++;
            }
            else
            {
                beta = 66666;
                alpha = -66666;
            }
        }
        return bestMove;
    }

    int Search(int alpha, int beta, int depth, int depthRemaining, bool nullMoveOk)
    {
        ulong zobrist = board.ZobristKey;
        var (entryZobrist, entryMove, score, entryDepth, entryCutoff) = tt[zobrist & 0x6fffff];
        bool interior = depth > 0, isInCheck = board.IsInCheck();
        if (isInCheck)
            depthRemaining++;
        int bestScore = -666_666, moveNum = 0;
        bool quiescence = depthRemaining <= 0, inNullWindow = beta == alpha + 1;
        int Recurse(int bayta, int depthDecrement = 1, bool isNullMoveOk = true)
            => score = -Search(-bayta, -alpha, depth + 1, depthRemaining - depthDecrement, isNullMoveOk);

        if (interior && board.IsRepeatedPosition())
            return 0;

        if (interior && entryZobrist == zobrist && entryDepth >= depthRemaining &&
            Math.Abs(score) < 30_000 &&
            (
                entryCutoff == 2 /* EXACT */ ||
                entryCutoff == 0 /* BETA */ && score >= beta ||
                entryCutoff == 1 /* ALPHA */ && score <= alpha
            ))
            return score;

        if (quiescence)
        {
            //
            // Static evaluation.
            //
            int egScore = 0,
                mgScore = 0,
                phaseWeight = 0,
                side = 2;
            for (; --side >= 0; mgScore = -mgScore, egScore = -egScore)
                for (int p = 6; --p >= 0;)
                    for (ulong mask = board.GetPieceBitboard((PieceType)p + 1, side > 0); mask != 0;)
                    {
                        phaseWeight += 0x42110 >> p * 4 & 0xf;
                        int square = BitboardHelper.ClearAndGetIndexOfLSB(ref mask),
                            index = (square ^ 0b111000 * side) * 12 + p * 2,

                            // Mobility for sliders plus support/attacks for king, pawn and knight
                            terps = BitboardHelper.GetNumberOfSetBits(
                                (p % 5 > 1 ? 0xffffffffffffffff : board.AllPiecesBitboard) &
                                BitboardHelper.GetPieceAttacks((PieceType)p + 1, new Square(square), board, side > 0)
                            );

                        mgScore += ppt[index++] + terps;
                        egScore += ppt[index] + terps * 7;
                    }
            bestScore =
                (phaseWeight * mgScore + 24 * egScore - egScore * phaseWeight) /
                (board.IsWhiteToMove ? 24 : -24) + 16;

            if (bestScore > alpha && (alpha = bestScore) >= beta)
                return alpha;
        }
        else if (!isInCheck && inNullWindow)
        {
            //
            // Razor on quiesence score.
            //
            score = Search(alpha, beta, depth, 0, true);
            if (depth > 3 && (score - 68 * depthRemaining >= beta || score + 87 * depthRemaining <= alpha))
                return score;

            //
            // Null move reduction.
            //
            if (nullMoveOk && score >= beta)
            {
                board.ForceSkipTurn();
                Recurse(beta, 4, false);
                board.UndoSkipTurn();

                if (score >= beta)
                    return score;
            }
        }

        entryCutoff = 1; // ALPHA
        Span<Move> moves = stackalloc Move[218];
        board.GetLegalMovesNonAlloc(ref moves, quiescence && !isInCheck);
        foreach (Move move in moves)
            killer[1000 + moveNum++] =
                move == entryMove ? -1_000_000_000
                : move.IsCapture ? -10_000_000 * (int)move.CapturePieceType + (int)move.MovePieceType
                : killer[depth] == move.RawValue ? -1_000_000
                : history[depth % 2, move.TargetSquare.Index, (int)move.MovePieceType]
                ;
        killer.AsSpan(1000, moves.Length).Sort(moves);

        moveNum = 0;
        foreach (var move in moves)
        {
            board.MakeMove(move);

            score = alpha + 1;

            if (moveNum++ == 0 || quiescence)
                Recurse(beta);
            else
            {
                //
                // Late move reduction.
                //
                if (depthRemaining > 1 &&
                    moveNum > 5 &&
                    // Don't sleep on DANGER PAWN!!
                    (move.MovePieceType != PieceType.Pawn || move.TargetSquare.Rank % 5 != 1)
                )
                    Recurse(score, (inNullWindow ? 2 : 1) + moveNum / 13 + depthRemaining / 9);

                if (alpha < score)
                    Recurse(alpha + 1);
                if (alpha < score)
                    Recurse(beta);
            }
            board.UndoMove(move);

            if (depthRemaining > 2 && timer.MillisecondsElapsedThisTurn >= timeLimit)
                return 55555; //TIMEOUT

            if (score > bestScore && (bestScore = score) > alpha)
            {
                entryCutoff = 2; //EXACT
                alpha = score;
                entryMove = move;
                if (!interior) bestMove = move;
            }

            if (alpha >= beta)
            {
                entryCutoff = 0; //BETA
                if (!move.IsCapture)
                {
                    killer[depth] = move.RawValue;
                    history[depth % 2, move.TargetSquare.Index, (int)move.MovePieceType] -= depthRemaining * depthRemaining;
                }
                break;
            }
        }
        if (bestScore == -666_666)
            return isInCheck ? depth - 33333 : 0;

        tt[zobrist & 0x6fffff] = (zobrist, entryMove, bestScore, depthRemaining, entryCutoff);

        return bestScore;

    }

    // Range-compressed piece position tables with piece weights added.
    // (See https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function)
    static int bi;
    int[] ppt = new[] {
            64011389261109431815273119744m,
            71819935927675683611806531584m,
            75527711785727240428128370688m,
            75811823298934744327225540608m,
            77325399015246207058715410432m,
            76415054076766120484005609472m,
            78300950002507342492240052224m,
            75525289413989155411896762368m,
            76777617637236014222454782274m,
            4022166558394757416218817883m,
            3078033696487222079489403689m,
            4017392743804176022631897920m,
            4017449208642650622976353326m,
            8352549487527753943897233749m,
            5229907893836313354943426583m,
            77347074621626337307500249081m,
            77365146269884723361665270012m,
            3733186321805683588946543621m,
            4952997509759107959633820178m,
            3391192854569898574978231573m,
            4625501670727735489924245036m,
            9598985063823378477802267686m,
            9302746181236771040552630289m,
            76734177620327003206625147378m,
            77975729200485644193926616823m,
            4934910421703457945967923209m,
            5251653838371907681492207876m,
            5858605434742890885439292174m,
            5856225546502405631273205520m,
            7097731212292275047190430472m,
            5869523748886934274706246668m,
            78889776640154751107045723376m,
            75785089723499793241670617582m,
            78608077684373462961528964863m,
            4620580485447512137361260285m,
            5229968804907575264525548296m,
            5842842378045647597824375564m,
            5225090692058955591292156676m,
            2139921537227797706920821255m,
            77329020312996645601386692591m,
            75504623618459930219892442094m,
            78908921113803679754131998205m,
            2457798026480182367984614653m,
            4604822058741166914660663801m,
            5225010411686604147318915074m,
            3689698306178672352656424194m,
            1844854041475946177151237910m,
            77658991040564475952666901496m,
            73659783651489136309177158120m,
            77068950090929389869006063103m,
            1233010017872865854483858930m,
            3044023286347775487464310768m,
            3060948339552827825654270454m,
            1225775425709592582340476944m,
            78308061332228553830301106458m,
            75521492096071442858069195761m,
            68385207668080906009952452608m,
            72140145210048004089950896128m,
            74906186485475475287419977728m,
            77327594324707689226520100864m,
            73355191271610006045598154752m,
            76730422305449627754850746368m,
            74296892318191835006387814400m,
            70265058374062339031171072000m
        }
        .SelectMany(decimal.GetBits)
        .Where((x, i) => i % 4 != 3)
        .SelectMany(BitConverter.GetBytes)
        .Select(b => new[] { // allocate int[12] 768 times to save 3 tokens :-)
                // piece weights (mg, eg)
                82,  94,   // pawn
                337, 281,  // knight
                365, 297,  // bishop
                477, 512,  // rook
                1025, 936, // queen
                0,   0     // king
            }[bi++ % 12] + 1475 * (sbyte)b / 1000)
        .ToArray();
}
