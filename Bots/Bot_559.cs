namespace auto_Bot_559;
using ChessChallenge.API;
using System;
using System.Linq;
using System.Numerics;

public class Bot_559 : IChessBot
{
    TranspositionTableResult[] transpositionTable = new TranspositionTableResult[8388608];

    public Bot_559()
    {
        mg_tables = new short[][]
        {
            DecodeDecimalArray(mg_pawn_table_encoded),
            DecodeDecimalArray(mg_knight_table_encoded),
            DecodeDecimalArray(mg_bishop_table_encoded),
            DecodeDecimalArray(mg_rook_table_encoded),
            DecodeDecimalArray(mg_queen_table_encoded),
            DecodeDecimalArray(mg_king_table_encoded)
        };

        eg_tables = new short[][]
        {
            DecodeDecimalArray(eg_pawn_table_encoded),
            DecodeDecimalArray(eg_knight_table_encoded),
            DecodeDecimalArray(eg_bishop_table_encoded),
            DecodeDecimalArray(eg_rook_table_encoded),
            DecodeDecimalArray(eg_queen_table_encoded),
            DecodeDecimalArray(eg_king_table_encoded)
        };
    }

    Move? BestNextMove;

    public Move Think(Board board, Timer timer)
    {
        BestNextMove = null; // Just in case
        MiniMax(board, 5, int.MinValue, int.MaxValue, board.IsWhiteToMove, true);

        return BestNextMove ?? board.GetLegalMoves()[0];
    }

    int MiniMax(Board board, int depth, int alpha, int beta, bool maximizing, bool storeMove)
    {
        var boardKey = board.ZobristKey;

        if (depth == 0 || board.IsDraw() || board.IsInCheckmate())
            return EvalBoard(board);

        var ttIndex = boardKey % 8388608;
        var cachedResult = transpositionTable[ttIndex];

        if (!storeMove && cachedResult?.Key == boardKey && cachedResult.Depth >= depth)
            return cachedResult.Eval;

        var legalMoves = board.GetLegalMoves().OrderByDescending(move => move.IsCapture || move.IsCastles || move.IsEnPassant || move.IsPromotion);

        if (maximizing)
        {
            int maxEval = int.MinValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                var eval = MiniMax(board, depth - 1, alpha, beta, false, false);
                board.UndoMove(move);

                if (maxEval < eval)
                {
                    maxEval = eval;
                    if (storeMove)
                        BestNextMove = move;
                }

                alpha = Math.Max(alpha, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }

            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, maxEval);

            return maxEval;
        }
        else
        {
            int minEval = int.MaxValue;
            foreach (Move move in legalMoves)
            {
                board.MakeMove(move);
                int eval = MiniMax(board, depth - 1, alpha, beta, true, false);
                board.UndoMove(move);

                if (minEval > eval)
                {
                    minEval = eval;
                    if (storeMove)
                        BestNextMove = move;
                }

                beta = Math.Min(beta, eval);
                if (beta <= alpha)
                {
                    break;
                }
            }
            transpositionTable[ttIndex] = new TranspositionTableResult(boardKey, depth, minEval);
            return minEval;
        }
    }

    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 10000 };

    int EvalBoard(Board board)
    {
        /// Gets an array of all the piece lists. In order these are:
        /// Pawns(white), Knights (white), Bishops (white), Rooks (white), Queens (white), King (white),
        /// Pawns (black), Knights (black), Bishops (black), Rooks (black), Queens (black), King (black)
        var score = 0;
        foreach (var pieceList in board.GetAllPieceLists())
        {
            var pieceType = pieceList.TypeOfPieceInList;
            var pieceValue = pieceValues[(int)pieceType];
            var piecesValue = pieceValue * pieceList.Count;

            score += pieceList.IsWhitePieceList ? piecesValue : -piecesValue;
        }


        foreach (PieceType pieceType in Enum.GetValues(typeof(PieceType)))
        {
            bool[] colors = { true, false };
            foreach (var isWhiteColor in colors)
            {
                var pieceBitBoard = board.GetPieceBitboard(pieceType, isWhiteColor);
                while (pieceBitBoard > 0)
                {
                    var index = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitBoard);
                    var lookupTable = board.PlyCount < 30 ? mg_tables : eg_tables;
                    var pieceSquareTable = lookupTable[(int)pieceType - 1];
                    var index_row_number = index / 8;
                    var index_column_number = index % 8;

                    var lookup_row_number = isWhiteColor ? 7 - index_row_number : index_row_number;
                    var lookup_column_number = index_column_number;

                    var lookup_index = lookup_row_number * 8 + lookup_column_number;
                    score += (isWhiteColor ? 1 : -1) * pieceSquareTable[lookup_index];
                }
            }
        }

        return board.IsInCheckmate() ? (board.IsWhiteToMove ? -1 : 1) * 100000 - board.PlyCount * 100 : score;
    }

    // Uncomment to calculate the size in MyBotTest.cs
    //[StructLayout(LayoutKind.Sequential)]
    public class TranspositionTableResult
    {
        public ulong Key;
        public int Depth;
        public int Eval;

        public TranspositionTableResult(ulong key, int depth, int eval)
        {
            Key = key;
            Depth = depth;
            Eval = eval;
        }
    }

    short[] DecodeDecimalArray(decimal[] encodedDecimals)
    {
        var encodedValue = BigInteger.Parse(string.Concat(encodedDecimals.Select(d => d.ToString()[2..])));
        var byteArray = encodedValue.ToByteArray();
        var result = new short[byteArray.Length];

        Buffer.BlockCopy(byteArray, 0, result, 0, byteArray.Length);

        return result;
    }

    short[][] mg_tables;

    short[][] eg_tables;

    decimal[] mg_pawn_table_encoded = new decimal[] {
0.5281171908439357134103180170m,
0.8865990595315857998869949161m,
0.5440025088417148940892015302m,
0.4118787500666823943524082405m,
0.2502113748847173429566213424m,
0.0476735579862764112555634553m,
0.7858095221046448991861193141m,
0.4621180003731826435819575879m,
0.3972210208217849961371255464m,
0.643361552491085824m,
};
    decimal[] eg_pawn_table_encoded = new decimal[] {
0.5282381034379772579889694058m,
0.4335008491291735896268963686m,
0.2142802865075899115088806166m,
0.6548317393886815190688051530m,
0.0525610042223940148810034202m,
0.8838443990982551343899520130m,
0.1459736794698964146687830258m,
0.5232593050030714743448875122m,
0.6435290372340532562679412119m,
0.865596827804893184m,
};
    decimal[] mg_knight_table_encoded = new decimal[] {
0.1797089653680531265533663309m,
0.9326409285531661933326648024m,
0.0577524102248420771824476000m,
0.4551268117682873650230085009m,
0.2419018885868915555738755748m,
0.8565235109508342702112883910m,
0.5579348371858218627161621516m,
0.4544761051144845296893559426m,
0.1901901962079875478044478259m,
0.7558556709004707258856348089m,
0.3763292789387618012440559599m,
0.3m,
};
    decimal[] eg_knight_table_encoded = new decimal[] {
0.1795964985271192885610056180m,
0.9537809427636716092233734275m,
0.5480847111803724485906782695m,
0.4932349675856033737963387290m,
0.1350303415452828345803900646m,
0.7275274002814592773237866906m,
0.3455376992045513116688819713m,
0.9655142747297677044945784533m,
0.5782677127491098835627856772m,
0.9475663190442105008175524893m,
0.9717047557027132810047383136m,
0.6m,
};
    decimal[] mg_bishop_table_encoded = new decimal[] {
0.1797144506550160396191461007m,
0.6972102430580282784860470891m,
0.7880708725821908590510949048m,
0.1394404656289939315802365735m,
0.7036018167404267063444612553m,
0.6997129678253797233493474523m,
0.7790836887143531349576334938m,
0.0412737982319544568731993067m,
0.3265629258986321674267097715m,
0.4415201466094741967390798949m,
0.4892500714647445172315311305m,
0.9m,
};
    decimal[] eg_bishop_table_encoded = new decimal[] {
0.1797254243262484412791613290m,
0.8772378029139784397761746152m,
0.5096228228929802868177038675m,
0.2809871146817401178977274641m,
0.2701585550877523947519930978m,
0.3703836629529173416382190180m,
0.6922136269677335114990733605m,
0.7556246171318405550981539637m,
0.2688394443089125847972340018m,
0.7621140632130186292445421813m,
0.0902112836954406641210569521m,
0.8m,
};
    decimal[] mg_rook_table_encoded = new decimal[] {
0.1797007353867113566787087424m,
0.5555216230865807461035520222m,
0.7555382144164664550357607232m,
0.4387730766070379042717543680m,
0.8144818089989236979673144118m,
0.8646601953691853357457624234m,
0.4454180868929200360149734181m,
0.8817254929184529062352129715m,
0.8245055411504240494381732384m,
0.1562777824570254328023024007m,
0.3394653350294158044052494748m,
0.8m,
};
    decimal[] eg_rook_table_encoded = new decimal[] {
0.1797144524548150146613957777m,
0.7456939115704549984045045135m,
0.5023156099979719802794647053m,
0.2118664504590457038418197800m,
0.6877574631453896673476697504m,
0.0814680464786324717567211660m,
0.3411280022749057033125552239m,
0.2504083865394393208226518830m,
0.6539787530349016590963293714m,
0.1209326848811123657542912131m,
0.8368017923113229100800186778m,
0.9m,
};
    decimal[] mg_queen_table_encoded = new decimal[] {
0.1796349021908566681540284307m,
0.8560595033025183384762616855m,
0.2535982979508435231385557219m,
0.0755636311190669649705810115m,
0.9735206704127913619440298840m,
0.0057239422798879657521166883m,
0.1703485832629585823320791258m,
0.4826665266915909858351343058m,
0.2071045193517026472398488918m,
0.2337806197722501416820939937m,
0.0763309219252584483495562442m,
0.0m,
};
    decimal[] eg_queen_table_encoded = new decimal[] {
0.1796595902095756238283495371m,
0.0267218161874918141119181813m,
0.8915576629857105914513962261m,
0.1280889477192231032585827903m,
0.6082293609836236626433448831m,
0.3589034711596569247153144600m,
0.4140675089652527369219794842m,
0.5209339558561161282534689027m,
0.4988090799534863306410657278m,
0.1023865075580040392585432373m,
0.5914420082649215356288047512m,
0.7m,
};
    decimal[] mg_king_table_encoded = new decimal[] {
0.3840391485879738464833731646m,
0.5791856841261702807301274774m,
0.2858351627487408006902954195m,
0.6048692692139850450668619073m,
0.4456359269995736768647101752m,
0.2567240214405747777480104427m,
0.9463765929627204240300966707m,
0.8527225419803135045684985583m,
0.1991669922768688484411133436m,
0.6631247835244628732541544340m,
0.5746053332180667727347647m,
};
    decimal[] eg_king_table_encoded = new decimal[] {
0.1796541039180951060828574684m,
0.4852067124780928641588229463m,
0.2566084966161116573981696750m,
0.4186579452102552891947750213m,
0.8356420370682990558561432355m,
0.7438984680956906095473810712m,
0.0943087629617497101221608541m,
0.7927697791276282237658764937m,
0.5072855737594382798472292186m,
0.2343196522837426436484028834m,
0.5801867026644651501904763282m,
0.2m,
};

}