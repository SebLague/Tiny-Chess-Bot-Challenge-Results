namespace auto_Bot_317;
using ChessChallenge.API;
using System;
using System.Numerics;

public class Bot_317 : IChessBot
{
    // Piece square tables are taken from https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // The tables have been normalized to have no negative numbers. The resulting offset has been added to the piece values.

    // Contains the borders of the tables in the compressed table array. The nth table has the lower border tableBorders[n] (including) and the upper border tableBorders[n+1] (excluding). The length of this subarray is equal to the bit depth of the corresponding table.
    private int[] tableBorders = { 0, 1, 9, 18, 26, 34, 41, 48, 49, 57, 64, 70, 76, 83, 90 };
    // Contains the compressed piece square tables.
    private ulong[] tablesCompressed = { 0, 37622403872301381, 4978761455311676507, 6574146139730819130, 4192627004385861167, 3401388891990204191, 2240864235386503202, 2454193602973732096, 0, 5507073221663024, 4336719729322750869, 6671586112861800412, 11664555371918740934, 7183649038001690347, 5051606636327085974, 7716305402536084764, 10622378756002714655, 2633946507521108112, 5348132585684897124, 7215684108951385476, 9545186232638997623, 8608447414155566956, 7814975004072894817, 7016995717371418209, 7017261713337832241, 3553133660964531005, 8242264074031172193, 7022029199949650036, 8395931465712296287, 6875377270000532294, 5066620281055489591, 3978448990741338935, 3977591367646904372, 3763399982880530989, 16674659558847281840, 18064196689745552944, 2751937087260412550, 5369992364679549243, 15076650418680831657, 2209555783725069154, 12926036042924312960, 5594721320456008171, 12820529196093301866, 7235623913978409378, 3576532448599002457, 14796913751481550113, 9593168664340023653, 5288798713943780559, 0, 52554072629939373, 12520964004627824701, 4420947311624260870, 435750859466409217, 72339116377509634, 146657271961621776, 1157444985772704000, 0, 11938927435880093352, 6244305366307282747, 11473964387651788750, 11515278958746239767, 9446069012397912894, 8304301177284474764, 4002787909109569699, 7356312654595794519, 16475952620044430986, 16874576956081670373, 4434046316641531029, 879491768245223441, 2522034625132934538, 6995164862797404500, 6150040045821040700, 10887519044978688979, 4702834658394781925, 304244858249433389, 12849101127223637504, 2642257032807928735, 6025592400584215700, 10129987883837027753, 17701771521903968723, 4743159636435151440, 3049053166944619284, 10021701763138243458, 3518904978294693371, 15995474732477996186, 6672912212326809132, 18315216548931772077, 2210700303798460158, 6845037655120591146, 13342654605973985567 };
    // Contains the piece values including the offset.
    private int[] pieceValues = { 0, 47, 170, 283, 406, 975, 0, 0, 86, 182, 270, 492, 893, 0 };
    // Contains the increment each piece type has on the game phase.
    private int[] gamePhaseInc = { 0, 0, 1, 1, 2, 4, 0 };
    // Will contain the decompressed piece square tables.
    private int[,] tables = new int[14, 64];

    // PRE:
    // POST:
    // - tables contains the decompressed piece square tables.
    public Bot_317()
    {
        // Decompress the piece square tables.
        for (int i = 0; i < 14; i++)
        {
            // Combine all ulongs to a long bit sequence.
            BigInteger bigInteger = new BigInteger(0);
            for (int j = tableBorders[i]; j < tableBorders[i + 1]; j++)
            {
                bigInteger <<= 64;
                bigInteger += tablesCompressed[j];
            }

            // Calculate the bit depth of the table.
            int bitDepth = tableBorders[i + 1] - tableBorders[i];
            // Decompress the table by assigning every bitDepth number of bits to a square and adding the piece values.
            for (int j = 63; j >= 0; j--)
            {
                BigInteger temp = bigInteger >> bitDepth;
                tables[i, j] = (int)(bigInteger - (temp << bitDepth) + pieceValues[i]);
                bigInteger = temp;
            }
        }
    }

    // Global variables for time management.
    Timer timer;
    int maxThinkingTime;

    // Global variables for move ordering.
    private Move[] orderedMovesFirstPly;
    int gamePhase;

    // PRE:
    // - The board for which we want to get the best move.
    // - The timer used for time control.
    // POST:
    // - Returns the move we want to play.
    public Move Think(Board board, Timer timer)
    {
        this.timer = timer;

        // Calculate in which game phase we are in. This is used by the move orderer and the time control.
        gamePhase = EvalGamePhase(board);

        // Calculate the maximum time we want to think. The estimate of how many moves are left to play are the game phase + 8. Once we have as much time left as will be added after the move we will use the whole time left (minus some safety). At this point the thinking time will.
        maxThinkingTime = timer.MillisecondsRemaining / (gamePhase + 8) + timer.IncrementMilliseconds * (gamePhase + 8) / (gamePhase + 9) - 5;
        // Just in case the maximum thinking time is longer than the time remaining. This should not happen except if the initial game duration is shorter than the increment.
        maxThinkingTime = Math.Min(maxThinkingTime, timer.MillisecondsRemaining - 5);

        // Initialize the move order for the first ply.
        orderedMovesFirstPly = OrderMoves(board);

        // Iterative deepening.
        for (int searchDepth = 2; searchDepth < Int32.MaxValue; searchDepth++)
        {
            int eval = alphaBeta(board, searchDepth, -1000000, 1000000, true);

            // If we found a mate or run out of time, exit the search. Together with iterative deepening this ensures that we always play the shortest mate sequence.
            if (eval == 1000000 | timer.MillisecondsElapsedThisTurn > maxThinkingTime)
                break;
        }

        return orderedMovesFirstPly[0];
    }

    // PRE:
    // - The board for which we want to get the evaluation.
    // - The searchDepth we want to search.
    // - alpha is equal to the minimum evaluation the maximizing player is assured of.
    // - beta is equal to the maximum evaluation the minimizing player is assured of.
    // - isFirstPly is true if we are evaluating the first ply of the game tree.
    // POST:
    // - moveOrderFirstPly contains the moves in descending order according to the evaluation if isFirstPly is true.
    // - Returns the evaluation of the board.
    private int alphaBeta(Board board, int searchDepth, int alpha, int beta, bool isFirstPly)
    {
        if (board.IsDraw())
            // If we have a draw the evaluation is zero.
            return 0;
        if (board.IsInCheckmate())
            // If we are in checkmate the evaluation is minimal.
            return -1000000;
        if (searchDepth == 0)
            // If we have exausted the search depth return the evaluation of the board according to the Eval function.
            return Eval(board);

        // The move order is given by the last iteration of the iterative deepening if we are searching the first ply. Otherwise the move order is given by the move orderer.
        Move[] orderedMoves = isFirstPly ? orderedMovesFirstPly : OrderMoves(board);
        // Generate an array to store the evaluation for each move.
        int[] evals = new int[orderedMoves.Length];

        for (int i = 0; i < orderedMoves.Length; i++)
            evals[i] = -1000000;

        // Search for the best move and save the evaluation in bestEval. For this we initialize bestEval to the minimum value.
        int bestEval = -1000000;
        for (int i = 0; i < orderedMoves.Length; i++)
        {
            // Make a move and evaluate the resulting board.
            board.MakeMove(orderedMoves[i]);
            evals[i] = -alphaBeta(board, searchDepth - 1, -beta, -alpha, false);
            board.UndoMove(orderedMoves[i]);

            // If the evaluation is better than the previously best evaluation we update the value of the best evaluation.
            if (evals[i] > bestEval)
                bestEval = evals[i];

            // Update alpha.
            alpha = Math.Max(alpha, bestEval);

            // alpha beta cutoff.
            if (alpha >= beta)
                break;

            // If we ran out of time we end the search.
            if (timer.MillisecondsElapsedThisTurn > maxThinkingTime)
                break;
        }

        // If we are evaluating the first ply we order the moves in descending order according to the evaluation and save it in orderedMovesFirstPly.
        if (isFirstPly)
        {
            Array.Sort(evals, orderedMoves);
            Array.Reverse(orderedMoves);
            orderedMovesFirstPly = orderedMoves;
        }

        return bestEval;
    }

    // PRE:
    // - The board on which we want to play a move.
    // POST:
    // - Returns the list of all possible moves in descending order of the move evaluation according to the EvalMove function.
    private Move[] OrderMoves(Board board)
    {
        // Get all possible moves and generate an array for storing the move evaluations.
        Move[] moves = board.GetLegalMoves();
        int[] moveEvals = new int[moves.Length];

        for (int i = 0; i < moves.Length; i++)
        {
            // Calculate the move evaluation in the middlegame.
            int mgEval = EvalMove(moves[i], board, 0);
            // Calculate the move evaluation in the endgame.
            int egEval = EvalMove(moves[i], board, 7);
            // Blend between the middlegame and the endgame evaluation.
            moveEvals[i] = (gamePhase * mgEval + (24 - gamePhase) * egEval) / 24;
        }

        // Sort the moves in descending order according to the move evaluations.
        Array.Sort(moveEvals, moves);
        Array.Reverse(moves);

        return moves;
    }

    // PRE:
    // - The move we want to evaluate.
    // - The board on which the move should be played.
    // - The offset corresponding to the game phase we want to evaluate (0 if middlegame, 7 if endgame).
    // POST:
    // - Returns the effect the move will have on the evaluation of the board.
    private int EvalMove(Move move, Board board, int offset)
    {
        // The piece moves away from the start square so we should decrease the evaluation correspondingly.
        int moveValue = -tables[(int)move.MovePieceType + offset, move.StartSquare.Index];

        if (move.IsPromotion)
        {
            // If we promote we add the value of the new piece at the target position.
            moveValue += tables[(int)move.PromotionPieceType + offset, move.TargetSquare.Index];
        }
        else
        {
            // Add the value of the piece at the target position.
            moveValue += tables[(int)move.MovePieceType + offset, move.TargetSquare.Index];
        }

        if (move.IsEnPassant)
            // If we have an en passant capture, add the value of the captured pawn to the evaluation. The tables are from blacks point of view.
            moveValue += board.IsWhiteToMove ? tables[1 + offset, move.TargetSquare.Index - 8] : tables[1 + offset, move.TargetSquare.Index + 8];
        else if (move.IsCapture)
            // If we capture a piece, add the value of the captured piece to the evaluation.
            moveValue += tables[(int)move.CapturePieceType + offset, move.TargetSquare.Index];
        else if (move.IsCastles)
        {
            if (move.TargetSquare.Index == 2 | move.TargetSquare.Index == 58)
                // Left sided casteling. The rook moves from A1 to D1 (or A8 to D8).
                moveValue += tables[4 + offset, 3] - tables[4 + offset, 0];
            else
                // Right sided casteling. The rook moves from H1 to F1 (or H8 to F8).
                moveValue += tables[4 + offset, 5] - tables[4 + offset, 7];
        }

        return moveValue;
    }

    // PRE:
    // - The board for which we want to know the evaluation.
    // POST:
    // - Returns the evaluation of the board.
    private int Eval(Board board)
    {
        int mgEval = 0;
        int egEval = 0;

        for (int i = 0; i < 64; i++)
        {
            // Get the piece on the square.
            Piece piece = board.GetPiece(new Square(i));
            int pieceType = (int)piece.PieceType;

            // Depending on of the piece is black or white we need to flip the board. The tables are in blacks point of view.
            int index = piece.IsWhite ? i ^ 56 : i;

            if (board.IsWhiteToMove == piece.IsWhite)
            {
                // If the piece is our piece we add the piece value.
                mgEval += tables[pieceType, index];
                egEval += tables[pieceType + 7, index];
            }
            else
            {
                // If the piece is an opponents piece we subtract the piece value.
                mgEval -= tables[pieceType, index];
                egEval -= tables[pieceType + 7, index];
            }
        }

        // Get the game phase we are in.
        int gamePhase = EvalGamePhase(board);

        // Blend between the middlegame and endgame evaluation.
        return (gamePhase * mgEval + (24 - gamePhase) * egEval) / 24;
    }

    // PRE:
    // - The board for which we want to know the game phase it's in.
    // POST:
    // - Returns the game phase the board is in.
    private int EvalGamePhase(Board board)
    {
        // Calculate the game phase.
        int gamePhase = 0;
        for (int i = 0; i < 64; i++)
        {
            gamePhase += gamePhaseInc[(int)board.GetPiece(new Square(i)).PieceType];
        }

        // In case of early promotion the game phase value would be greater than 24.
        return Math.Min(gamePhase, 24);
    }
}