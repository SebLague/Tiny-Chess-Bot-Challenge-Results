namespace auto_Bot_372;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class Bot_372 : IChessBot
{
    //Credit to @Selenaut for performative transposition lookup that *probably* doesn't break anything...
    public struct Transposition
    {
        public ulong zobristHash;
        public int evaluation;
        public sbyte depth;
    };

    public Transposition[] transpositionTable { get; set; }

    // Piece values: null, pawn, knight, bishop, rook, queen, king
    public int[] pieceValues = { 0, 100, 320, 330, 500, 900, 20000 };

    //credit Dorinon: https://github.com/dorinon/Chess-Pst-Quantization
    decimal[] compressedSquareTables = {
        4340076789263538792515578391m,  8368255582537961417760727831m,  11158456191182218035179964951m,
        13307917093891974474926480151m,  9625514824545891310680106519m,  12697366905970192825153707799m,
        10540700058164212799285060887m,  6500394244407116926542242327m,  9920487927444181762554093056m,
        13323614073845617228270161431m,  16412438561986469389087762954m,  18281447397919378546827489288m,
        18590894702740531601196478733m,  16418454838005141720453774888m,  14550659650403265407907424305m,
        12078429812467256652092433672m,  11472753440024384478058668550m,  14867431420219487971753225237m,
        17997326329371099189272017429m,  20156482047989786994519734545m,  20468361334222924360815050522m,
        18928175543157876813111917850m,  17065234892311914892331615022m,  13656035654420793123076466447m,
        11781053209863340821648271621m,  14603904554652582685970625814m,  20167390751457873873990679828m,
        20809316268792260891178007328m,  21414959622213990359476700451m,  20488917758988248378494713628m,
        17398898475019536371617267742m,  13362285643724625975990447110m,  13964377999423073030539406350m,
        20169789657647491978098343200m,  20790001697503450008945786652m,  21427067788613308634678072870m,
        21127258981501210608324938535m,  22660172142562572502423413024m,  21127249388549445197676445219m,
        16158573694922487778296365320m,  17658864803890118367929782803m,  19228050407286765453103438620m,
        20468408391883416066315356969m,  18644134589196265210548297261m,  19880860979860947388257577796m,
        25132411220235313898160048702m,  24809623229745987154340848681m,  18301980319386538006273232906m,
        13019021564572452270008320091m,  19240158592432847116017292915m,  18630860126053506528853074753m,
        19257078739588156431296072280m,  19271538736081412984757060166m,  23576542339272103092960664686m,
        20485338018742398357439018799m,  17985260403821339742106510864m,  27909335506732266436165655m,
        8099897515490484006734673175m,  11813746079450960178425387799m,  11817358744429988596699516951m,
        13364774348659602364205341719m,  18619974868293840544160493335m,  16446316687411893639780788247m,
        12121979510401049595651172631m };

    int[][] squareTables;

    public int maxDepth = 4;

    public Bot_372()
    {
        squareTables = UnpackToInt(compressedSquareTables);
        transpositionTable = new Transposition[0x800000];
    }

    public Move Think(Board board, Timer timer)
    {
        (Move move, int score) = MinMaxAb(board, 0, -99999999, 99999999);

        return move;
    }

    (Move, int) MinMaxAb(Board board, int depth, int alpha, int beta)
    {
        int modifier = board.IsWhiteToMove ? 1 : -1;

        var allMoves = GetOrderedMoves(board, depth);

        Move bestMove = allMoves.First();
        int bestScore = -modifier * 999999;

        foreach (Move move in allMoves)
        {
            int score;

            board.MakeMove(move);

            //again credit to Selenaut
            ref Transposition transposition = ref transpositionTable[board.ZobristKey & 0x7FFFFF];

            if (transposition.zobristHash == board.ZobristKey && transposition.depth <= depth)
            {
                score = transposition.evaluation;
            }
            else
            {
                if (board.IsInCheckmate())
                {
                    score = (99999 + maxDepth - depth) * modifier;
                }
                else if (board.IsDraw())
                {
                    score = 0;
                }
                else if (depth < maxDepth)
                {
                    (Move bestRetort, score) = MinMaxAb(board, depth + 1, alpha, beta);
                }
                else
                {
                    score = EvaluateBoard(board);
                }

                //updated during recursion
                if (transposition.zobristHash == board.ZobristKey)
                {
                    if (transposition.depth < depth)
                    {
                        throw new InvalidOperationException();
                    }
                }
                //add or replace
                else
                {
                    transposition = new Transposition
                    {
                        evaluation = score,
                        zobristHash = board.ZobristKey,
                        depth = (sbyte)depth,
                    };
                }
            }

            if (modifier == 1)
            {
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                alpha = Math.Max(alpha, score);
            }
            else
            {
                if (score < bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }

                beta = Math.Min(beta, score);
            }

            board.UndoMove(move);

            if (beta <= alpha)
                break;
        }

        return (bestMove, bestScore);
    }

    public int EvaluateBoard(Board board)
    {
        int[] gamephasePieceScores = { 0, 0, 1, 1, 2, 4, 0 };

        int mgTotal = 0, egTotal = 0, mgPhase = 0;

        for (int i = 0; i < 64; i++)
        {
            var piece = board.GetPiece(new Square(i));
            int pieceType = (int)piece.PieceType;

            if (pieceType != 0)
            {
                // saving tokens by not doing the early promotion check
                // (game phase score > 24)
                // when's that even gonna happen?
                mgPhase += gamephasePieceScores[pieceType];

                int pieceMod = piece.IsWhite ? 1 : -1, tableIndex = i;

                //reverse for black
                if (!piece.IsWhite)
                {
                    int remainder;

                    tableIndex = 8 * (7 - Math.DivRem(i, 8, out remainder));
                    tableIndex += remainder;
                }

                mgTotal += (pieceValues[pieceType] +
                    squareTables[pieceType - 1][tableIndex]) * pieceMod;

                egTotal += (pieceValues[pieceType] +
                    squareTables[pieceType + 5][tableIndex]) * pieceMod;
            }
        }

        return (mgTotal * mgPhase + egTotal * (24 - mgPhase)) / 24;
    }

    // Again credit to Dorinon, even though their documentation is awful
    public static int[][] UnpackToInt(decimal[] quantizedArray)
    {
        var unpackedArrays = new int[12][];

        int[] coefficients = { 35, 167, 82, 71, 50, 65, 8, 99, 27, 20, 43, 74 };

        for (var j = 0; j < 12; j++)
        {
            unpackedArrays[j] = new int[quantizedArray.Length];

            for (var i = 0; i < quantizedArray.Length; i++)
                unpackedArrays[j][i] = (int)((int)(((BigInteger)quantizedArray[i] >> (j * 8)) & 255) * 1.461f) - coefficients[j];

        }

        return unpackedArrays;
    }

    public Move[] GetOrderedMoves(Board board, int depth)
    {
        Move[] allMoves = board.GetLegalMoves();

        List<Move> orderedMoves = new();
        List<Move> captureMoves = new();
        List<Move> drossMoves = new();

        foreach (var move in allMoves)
        {
            board.MakeMove(move);

            ref Transposition transposition = ref transpositionTable[board.ZobristKey & 0x7FFFFF];

            if (transposition.zobristHash == board.ZobristKey && transposition.depth <= depth)
                orderedMoves.Add(move);
            else if (move.IsCapture)
                captureMoves.Add(move);
            else
                drossMoves.Add(move);

            board.UndoMove(move);
        }

        orderedMoves.AddRange(captureMoves);
        orderedMoves.AddRange(drossMoves);
        return orderedMoves.ToArray();
    }
}