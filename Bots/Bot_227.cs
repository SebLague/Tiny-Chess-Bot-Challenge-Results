namespace auto_Bot_227;
//#define DEBUG
//For printing search depths

using ChessChallenge.API;
using System;
#if DEBUG
using System.Collections.Generic;
using System.IO;
#endif
using System.Linq;

public class Bot_227 : IChessBot
{
#if DEBUG
    int posCount;
#endif
    Move bestMove;
    int computeDepth,
    // indices for for-loops, putting them here initializes them to 0.
        flatFilterIndex, layer_filter, i;

    // Credits: TT implementation copied from Tyrant's bot
    // 0x400000 represents the rough number of entries it would take to fill 256mb
    // Very lowballed to make sure I don't go over
    // Hash, Move, Score, Depth, Flag
    private readonly (ulong, Move, int, int, int)[] transpositionTable = new (ulong, Move, int, int, int)[0x400000];

    Move[] killerMoves = new Move[1000];

    public Move Think(Board board, Timer timer)
    {
        // Credits:
        // Weird token-optimization with local functions and devisions by zero
        // is ErwanF's fault. Without their 200-token bot,
        // I wouldn't have known local functions exists in C#.
        #region EvaluateBoard
        int EvaluateBoard()
        {
            var activations = new double[24/*3 FILTERS*/, 4, 4];
            double value = -0.02034837; // bias of output layer

            // Generate Input for the net and multiply it with first filter layer immediately.
            // This saves time because of the many zeros in the input that we are skipping
            // Loop through each piece type and color, and set the corresponding bits in the input tensor.
            for (i = 0; i < 12/*pieces*/; i++)
                for (ulong pieceBitboard = board.GetPieceBitboard((PieceType)(i % 6 + 1), i < 6 ^ !board.IsWhiteToMove);
                    pieceBitboard != 0;
                    )
                {
                    value += flatFilters[928/*numparams+FILTERS*/ + i];
                    for (int square = BitboardHelper.ClearAndGetIndexOfLSB(ref pieceBitboard),
                            row = board.IsWhiteToMove ? 7 - square / 8 : square / 8,
                            filter = 0; filter < 8/*FILTERS*/; filter++)
                        activations[filter, row / 2, square % 8 / 2] +=
                                convolutionalFilters[filter, i, row % 2 * 2 + square % 2];
                }

            for (i = 0; i < 128 /*4*4*FILTERS*/; i++)
            {
                ref var act = ref activations[i / 16, i / 4 % 4, i % 4];
                act = Math.Max(0, act + flatFilters[896 /*num_params*/+ i / 16]);
            }
            // The above is equivalent to:
            // for(int filter = 0; filter < numFilters; filter++)
            //     for(int row = 0; row < 4; row++)
            //         for(int col = 0; col < 4; col++)
            //             activations[filter, row, col] = 
            //                     Math.Max(0,
            //                              activations[filter, row, col] + filterBiases[0][filter]);

            // Forward-pass through second and third layer
            for (layer_filter = 8 /*FILTERS*/; layer_filter < 24 /*3 FILTERS*/; layer_filter++)
                // size is first 2 and later 1.
                for (int size = 3 - layer_filter / 8 /*FILTERS*/, h = 0; h < size; h++)
                    for (int w = 0; w < size; w++)
                    {
                        // Convolve(activations, convolutionalFilters[i][d], filterBiases[d], h, w)
                        double convolutionResult = flatFilters[896/*num_params*/ + layer_filter];

                        for (i = 0; i < 32 /*4 FILTERS*/; i++)
                            convolutionResult +=
                                    activations[16 /*2 FILTERS*/ - 8 /*FILTERS*/ * size + i / 4, i / 2 % 2 + h * 2, i % 2 + w * 2]
                                    * convolutionalFilters[layer_filter, i / 4, i % 4];
                        // ReLU activation function
                        activations[layer_filter, h, w] = Math.Max(0, convolutionResult);
                    }
            // Output Layer, assuming we convoluted until we were left with a 1x1 board.
            for (i = 0; i < 8 /*FILTERS*/; i++)
                value += activations[16 /*2 FILTERS*/ + i, 0, 0] * flatFilters[920 /*num_params*/ + i];
            //DivertedConsole.Write($"output={value}");
            return (int)(10000 * value);
        }
        #endregion

        #region EvaluateWithLookahead
        int EvaluateWithLookahead(int depth, int alpha, int beta)
        {
#if DEBUG
            posCount++;
#endif
            bool qs = depth < 1;
            if (board.IsRepeatedPosition()) return 0;
            // depth < 1 means Q-Search has started
            var moves = board.GetLegalMoves(qs);
            if (moves.Length == 0)
                return qs ? EvaluateBoard() :
                        board.IsInCheck()
                            ? board.PlyCount - 100000
                            : 0;

            ulong zobristKey = board.ZobristKey;
            ref var entry = ref transpositionTable[zobristKey & 0x3FFFFF];

            // Define best eval all the way up here to generate the standing pattern for QSearch
            Move entryMove = entry.Item2,
                localBestMove = default;
            int originalAlpha = alpha,
                entryScore = entry.Item3,
                entryFlag = entry.Item5;

            // Transposition table lookup -> Found a valid entry for this position
            // Avoid retrieving mate scores from the TT since they aren't accurate to the ply
            if (entry.Item1 == zobristKey && depth != computeDepth && entry.Item4 >= depth && Math.Abs(entryScore) < 50000 && (
                    // Exact
                    entryFlag == 1 ||
                    // Upperbound
                    entryFlag == 2 && entryScore <= alpha ||
                    // Lowerbound
                    entryFlag == 3 && entryScore >= beta))
                return entryScore;

            int bestScore = qs ? EvaluateBoard() : -99999999;
            if (bestScore >= beta) return bestScore;

            // Evaluate each legal move and select the one with the highest score after lookahead.
            foreach (Move move in
                    moves
                    .Where(m => !qs || m.MovePieceType <= m.CapturePieceType
                        || !board.SquareIsAttackedByOpponent(m.TargetSquare)
                        || (board.IsInCheck() && depth > -computeDepth))
                    .OrderByDescending(
                    m => m == entryMove ? 102 : // previously best move in this position
                    // todo: killer after capture?
                            m.Equals(killerMoves[board.PlyCount]) ? 101 :
                            10 * (int)m.CapturePieceType - (int)m.MovePieceType))
            {
                //                               -1 :
                //                        m.IsPromotion ?  -2 :
                board.MakeMove(move);

                // Recursively evaluate the board after the move using lookahead for the opponent's turn.
                int score = -EvaluateWithLookahead(depth - 1, -beta, -alpha);

                if (timer.MillisecondsElapsedThisTurn * 40 > timer.MillisecondsRemaining)
                    depth /= 0;

                board.UndoMove(move);

                // Update the best evaluation score if this move leads to a higher score for white's turn.
                // For black's turn, update the best evaluation score if this move leads to a lower score (advantage for black).
                if (score > bestScore)
                {
                    bestScore = score;
                    localBestMove = move;
                    if (depth == computeDepth) bestMove = move;
                }
                alpha = Math.Max(alpha, bestScore);
                if (alpha >= beta)
                {
                    killerMoves[board.PlyCount] = move;
                    break;
                }
            }

            // Transposition table insertion
            entry = new(
                zobristKey,
                localBestMove == default ? entryMove : localBestMove,
                bestScore,
                depth,
                bestScore >= beta ? 3 : bestScore <= originalAlpha ? 2 : 1);

            return bestScore;
        }
        #endregion

#if DEBUG
        int score = 0;
#endif
        try
        {
            for (computeDepth = 0; ;)
#if DEBUG
            {
                posCount = 0;
                score =
#endif
                EvaluateWithLookahead(
                    ++computeDepth,
                    -9999999, 9999999);
#if DEBUG
                    DivertedConsole.Write($"---d:{computeDepth}\t---pos:{posCount}\t{timer.MillisecondsElapsedThisTurn}ms\t{bestMove}\tscore:{Math.Round((double)0.001*score, 2)}");
            }
#endif
        }
        catch
        {
#if DEBUG
                    DivertedConsole.Write($"!--d:{computeDepth}\t---pos:{posCount}\t{timer.MillisecondsElapsedThisTurn}ms\t{bestMove}\tscore:{Math.Round((double)0.001*score, 2)}");
#endif
        }

#if DEBUG
        DivertedConsole.Write($"Depth {computeDepth-1}: {bestMove}, score= {score}");
#endif

        return bestMove;
    }


    #region NeuralNetInitalization
    decimal[] CompressedFilters = {
        53508514737641527794351814735m,
        16516608404352703965498978413m,
        53733316527194012768291502172m,
        14303614765286218079850933224m,
        15796051583872801559945854684m,
        55664631464273921847888951900m,
        16118190710218444523615336256m,
        57568711075169829414921355767m,
        58000287594305944192094352887m,
        52780113789369434699527992844m,
        56688603707249954639581723986m,
        16169638937276723284284288146m,
        57304523834411663064406638506m,
        57218659755821479733960323076m,
        17883905987637301885048010904m,
        11098188132117211726921021887m,
        12516840796287904482020767979m,
        55564290841764647729460979885m,
        17617943192815898397900617063m,
        58377519802726545497469500460m,
        17357994792962073902485154885m,
        17678387971063824742900381919m,
        55548593335958657043569096710m,
        18999765788581769525626877421m,
        15583909083507283445832002031m,
        15161978254776642680844299098m,
        56863906150553036364668810778m,
        18322758640945802080816443669m,
        18328811977160689161867964691m,
        17175471546479417444374952622m,
        15520454073470109240489884557m,
        53503046190567359714886432626m,
        17795637615512264684325513916m,
        16902251230846288936407479701m,
        18246584176051853933471445168m,
        12757380028861795626773919784m,
        19341896665303131378643706230m,
        19396299932424638277846121832m,
        21544595683157887787006312447m,
        15106961754488048211789235658m,
        19250014465814642018661971973m,
        19241556283712812567504960949m,
        21157732023797068890769145908m,
        56169965767705625621021737920m,
        16979623800746634368576529702m,
        17150091150869136936106932253m,
        48302808943441477051314972062m,
        46880528303633051661509343981m,
        59593604416544815727641437325m,
        54334800426277161727593297975m,
        57752416009294233146758934630m,
        59321688267144023020061733272m,
        54870347001901270418903481369m,
        15102729998105121274200110745m,
        56454067088377163225793506520m,
        17164573689097035132395370391m,
        18863140300607336680646262220m,
        17175476010934317445822166939m,
        18385595233291699477033169391m,
        16681021587477347704526940205m,
        51692103683075821394386694726m,
        55719016477310835918281225622m,
        54537873640974453977957513493m,
        56156027158477858903215092882m,
        13752276468968659489771306043m,
        56603986813629508489065249561m,
        57368028134043886442384139293m,
        19229463041209381421038942167m,
        15023551250756848182667851104m,
        18224853837159305499427487168m,
        19045702424145119765717430827m,
        18866759210842140614802025316m,
        8301286994534429409765896273m,
        17666322148987510834313836459m,
        47355025664358357901632910637m,
        17095679130554468709664961179m,
        52388394762136178104419792937m,
        20259487411596165836613270266m,
        15848626630721787593264346685m,
        55040160450365455742884658071m,
        58862276504781809641297231783m,
        57258013726666164259224369111m,
        16086190460585218305061306046m,
        54331087585611397055254635194m,
        59348291469394015148741148669m,
        55842362864355157911383848852m,
        16651407387385513392585718418m,
        55104295029528156642211543097m,
        17571958614344000694492346744m,
        57070647693672125804701792055m,
        17906891663938431770533407800m,
        17927424291848405354780900775m,
        17361614670449160965514077825m,
        16331593787042786564059247714m,
        17393086154354192495425142027m,
        18626189022964918465881390670m,
        57837061054002028924593614955m,
        60627308700019454629869699150m,
        54676903744213242329399505971m,
        17286647978367487328997190058m,
        14387034181963260959674053345m,
        19190783815018983762785744916m,
        18388012153566678736157260152m,
        57069436885489027880903652482m,
        16120084065130658560197833776m,
        17839205116787326075993995635m,
        17389440329479724009400381832m,
        18987675570925933043204405028m,
        58533458421845030496106329353m,
        17468009606284983595122175646m,
        16533518848487293112604472032m,
        55343039643527811055941432127m,
        51849217407623414235677376653m,
        57920513911505619257437009483m,
        14932890759931476171896567327m,
        19862945704604550131405731444m,
        58396841144380847289532427193m,
        57496154560042884754619676315m,
        16128525554101183480650081394m,
        15291314402018642471704737039m,
        14712255969609418619359732212m,
        58612012737633911905129346159m,
        55900978380537385824404190069m,
        18568160436047773219523474167m,
        16150295320525505711614962874m,
        53927994415865587268533296782m,
        14006869622254200713951361375m,
        15565194216188466097739445749m,
        57716200393874482025563601413m,
        54813458865803076069232164062m,
        50277056084981207558967637303m,
        18863162336062382198885887593m,
        55824219808052137537258272359m,
        56290851589642325464705251520m,
        17347108732842930962324927101m,
        13361807356834259828364685959m,
        13564960777001417503870498442m,
        16706344539506054958084240080m,
        16908878536782875231679328338m,
        15370451469114894109643683205m,
        18203087501813081313360921330m,
        58650719772123084222560677081m,
        57727694132994982202133527672m,
        16346117739080898976033157208m,
        54962248182139280599462788288m,
        59135506570712373096551228699m,
        57745210793385688092747249501m,
        56973919101107631836683024582m,
        15190413921128767344695030258m,
        52689410235406185260228950949m,
        59252087040727704159654751537m,
        16947620757929419270648019223m,
        19195045798153715453503653316m,
        52481487713163858868943142314m,
        17597365221925010810431450617m,
        57204797424550505255380203868m,
        18569383944424056484587157853m};

    // 3 layers, 8 filters per layer,
    // 12/8/8 channels per filter (some unused space), 2x2 filter shape
    // Flattening the first two and last two dimensions
    // reduces the number of for-loop tokens
    private double[,,] convolutionalFilters = new double[36/*3filters*/, 12, 4];
    private double[] flatFilters;

    // FlatFilters contains: (FILTERS=8)
    // * 12*FILTERS*4 first convolutional layer, (384)
    // * FILTERS*FILTERS*4*2 two more convolutional layers, (896)
    // * 3*FILTERS filter biases of the conv filters (920)
    // * FILTERS weights on the output layer (928)
    // * 12 piece weights (trained stand-alone and then hardcoded,
    // so they perform well by themselves) (940)
    // piece weights: pawns, knights, bishop, rook, queen, king
    // first white then black
    //        0.21969897,  0.6076166 ,  0.67003953,  0.95905274,  1.777254  ,
    //        0.43265444, -0.22002883, -0.60467845, -0.6704997 , -0.95492476,
    //       -1.7699734 , -0.40954167

    // constant inside the code:
    //    private double outputLayerBias = 0.8206829;

    public Bot_227()
    {
        // Decompression via Half to double
        // This only works if each decimal number is 12 bytes long. Don't pad with zeros!
        flatFilters = CompressedFilters
              .SelectMany(d => new System.Numerics.BigInteger(d).ToByteArray().Take(12)
              .Chunk(2)
              .Select(chunk => (double)BitConverter.ToHalf(chunk, 0)))
              .ToArray();

        // Unflatten the flatFilters into the convolutionalFilters array
        for (; layer_filter < 24/*3 FILTERS*/; layer_filter++)
            for (i = 0; i < (layer_filter < 8/*FILTERS*/ ? 48/*12*2*2*/ : 32/*4 FILTERS*/); i++)
                convolutionalFilters[layer_filter, i / 4, i % 4] =
                        flatFilters[flatFilterIndex++];
    }

    #endregion

}