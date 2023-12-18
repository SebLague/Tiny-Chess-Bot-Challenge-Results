namespace auto_Bot_306;
using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class Bot_306 : IChessBot
{
    /*
        None,   // 0
        Pawn,   // 1
        Knight, // 2
        Bishop, // 3
        Rook,   // 4
        Queen,  // 5
        King    // 6
    */
    float[] pieceValues = { 0, 82, 337, 365, 477, 1025, 0 };

    // PSQTs stolen from: https://www.chessprogramming.org/PeSTO%27s_Evaluation_Function
    // Encdoed into ulongs with a python script I made :P
    // Here is the script
    /*
        psqt = """<input goes here>"""

        psqt = psqt.replace("\n", "").replace(" ", "")

        commaSplitValues = psqt.split(",")
        output = []
        current = ''
        for val in commaSplitValues:
            x = int(val)
            if x < 0:
                x = 256 + x

            current += f'{x:08b}'

            if len(current) == 64:
                output += [f'0b{current}']
                current = ''

        print('{' + ','.join(output) + '}')
     */

    // first 6 are for middle game, last 6 are for end game
    ulong[,] psqtBytes =
    {
        {0b0000000000000000000000000000000000000000000000000000000000000000,0b0010000100101101000101010010000000010111001010100000110011111101,0b1111111000000011000010010000101100010110000100110000100111111010,0b1111110000000101000000100000011100001000000001000000011011111001,0b1111011100000000111111110000010000000110000000100000010011111000,0b1111100011111111111111111111110100000001000000010000101111111100,0b1111010100000000111110101111100111111011000010000000110111111001,0b0000000000000000000000000000000000000000000000000000000000000000},
        {0b1100100111100011111101011111000000010101111000001111101111011101,0b1110100011110011000110000000110000001000000101010000001111111011,0b1111000100010100000011010001011000011100001010110001100100001111,0b1111110100000110000001110001001000001101000101110000011000001000,0b1111110000000010000001100000010100001010000001110000011111111110,0b1111100111111101000001000000010000000111000001100000100111111011,0b1111011111101111111111001111111100000000000001101111110011111010,0b1101110111111001111011011111010111111011111101111111101011111001},
        {0b1111011100000010111001011111010011111000111100100000001111111110,0b1111100000000110111110101111110000001010000101000000011011110001,0b1111101100001101000011110000111000001100000100010000110100000000,0b1111111100000010000001110001000100001101000011010000001100000000,0b1111111000000101000011000000100100001100000011000000010000000010,0b0000000000000101000001011111101011111010000001010000011000000100,0b0000001000000101000001100000000000000011000001110000101100000001,0b1111010111111111111111001111100111111100111111001111101011111001},
        {0b0000101100001110000010110001000100010101000000110000101100001111,0b0000100100001011000101000001010100011011000101110000100100001111,0b1111111100000111000010010000110000000110000011110001010100000110,0b1111100011111101000000110000100100001000000011001111111011111010,0b1111010011111000111111000000000000000011111111100000001011111001,0b1111000111111000111110111111101100000001000000001111111111110101,0b1111001011111011111110101111110100000000000001001111111011101001,0b1111101011111100000000010000011000000110000000111111010011111000},
        {0b1111011100000000000010100000010000010100000011110000111100001111,0b1111100011110011111111110000000111111011000100110000101000010010,0b1111110011111011000000110000001100001010000100110001000000010011,0b1111011111110111111110111111101100000000000001100000000000000001,0b1111110111111000111111011111110100000000111111110000000111111111,0b1111110000000001111111010000000011111111000000010000010100000010,0b1111010111111110000001000000000100000011000001011111111100000001,0b0000000011111010111111010000010011111011111110001111011011110000},
        {0b1110101100001000000001101111101111101110111101010000000100000101,0b0000101000000000111110101111111011111110111111111111010011110111,0b1111110100001000000000011111101111111010000000100000100011111001,0b1111101111111010111111001111011111110110111110001111110011110100,0b1111000000000000111101111111001111110001111100101111010111101111,0b1111110011111100111110011111000111110010111101101111101111110111,0b0000000100000011111111101110101111110010111110110000001100000011,0b1111101100001100000001001110111000000011111101110000100000000101},
        {0b0000000000000000000000000000000000000000000000000000000000000000,0b0011110000111010001101010010110100110001001011000011011100111111,0b0010000000100010000111010001011100010011000100100001110000011100,0b0000101100001000000001010000001000000000000000100000011000000110,0b0000010100000011111111111111111011111110111111100000000100000000,0b0000001000000011111111100000000100000000111111110000000011111110,0b0000010100000011000000110000010000000101000000000000000111111110,0b0000000000000000000000000000000000000000000000000000000000000000},
        {0b1110110111110100111111001111011111110110111101111110101111011111,0b1111100011111110111110000000000011111101111110001111100011101111,0b1111100011111010000001000000001100000000111111011111101011110011,0b1111101100000001000010000000100000001000000001000000001111111010,0b1111101011111110000001100000100100000110000001100000001011111010,0b1111100111111111000000000000010100000100111111111111101011111001,0b1111001011111010111111011111111100000000111110101111100111110010,0b1111011111101111111110011111101111111001111110101111000011101011},
        {0b1111110011111001111111011111111011111110111111011111101111111000,0b1111111011111111000000111111110011111111111111001111111111111100,0b0000000111111110000000000000000000000000000000100000000000000010,0b1111111100000011000001000000001100000101000001000000000100000001,0b1111111000000001000001010000011100000011000001001111111111111101,0b1111110011111111000000110000010000000101000000011111111011111011,0b1111110011111010111111100000000000000010111111011111101111110111,0b1111100111111101111110011111111111111101111110111111111111111011},
        {0b0000010100000100000001100000010100000100000001000000001100000010,0b0000010000000101000001010000010011111111000000010000001100000001,0b0000001100000011000000110000001000000010111111111111111111111111,0b0000001000000001000001010000000100000001000000010000000000000001,0b0000000100000010000000110000001011111111111111101111111011111101,0b1111111100000000111111110000000011111110111111001111111011111011,0b1111111011111110000000000000000111111101111111011111110111111111,0b1111110100000001000000010000000011111111111111000000001011111010},
        {0b1111110100001000000010000000100100001001000001110000010000000111,0b1111101100000111000010110000111000010100000010010000101000000000,0b1111101000000010000000110001000100010000000011000000011100000011,0b0000000100001000000010000000111100010011000011100001001100001100,0b1111101000001010000001110001000000001011000011000000110100001000,0b1111101111110111000001010000001000000011000001100000010000000010,0b1111100111111001111101101111101111111011111110011111010011110110,0b1111010111110111111110011111001011111111111101101111101011110011},
        {0b1110100011110101111110101111101011111101000001010000001011111011,0b1111110000000110000001010000011000000110000011010000100000000100,0b0000010000000110000010000000010100000111000011110000111100000101,0b1111111000001000000010000000100100001001000010110000100100000001,0b1111101011111111000001110000100000001001000010000000001111111101,0b1111101011111111000001000000011100001000000001100000001111111101,0b1111011111111101000000100000010100000101000000101111111111111011,0b1110111111110101111110011111110111110111111111001111100011110010},
    };
    //0b1110100011110101111110101111101011111101000001010000001011111011
    float[,] psqts;
    Board currentPosition;
    Timer myTimer;
    Dictionary<ulong, float> storedEvaluations = new();
    int depth;

    public Bot_306()
    {
        // decoded ulongs to matrix
        psqts = new float[12, 64];
        for (int i = 0; i < 12; i++)
            for (int j = 0; j < 8; j++)
                for (int k = 0; k < 8; k++)
                    // has to calculate bytes each time but is uses less tokens
                    // values were too high to store in a single byte so were first divided by 3. 
                    psqts[i, j * 8 + 7 - k] = ((sbyte)BitConverter.GetBytes(psqtBytes[i, j])[k]) * 3;
    }

    public bool ShouldCancel()
    {
        return myTimer.MillisecondsElapsedThisTurn > 500 && depth > 1;
    }

    public Move Think(Board board, Timer timer)
    {
        Move bestMove = Move.NullMove;
        myTimer = timer;
        currentPosition = board;
        var moves = board.GetLegalMoves();
        Move bestMoveThisIter = moves[0];

        // if there is only 1 move, don't think, just return it
        if (moves.Length < 2) return bestMoveThisIter;

        depth = 1;
        while (true)
        {
            float bestMoveScore = float.NegativeInfinity;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                if (board.IsInCheckmate())
                    return move;
                float score = -NegaMax(depth + (currentPosition.IsInCheck() ? 1 : 0), float.NegativeInfinity, float.PositiveInfinity);

                if (score > bestMoveScore)
                {
                    bestMoveThisIter = move;
                    bestMoveScore = score;
                }

                board.UndoMove(move);
            }

            if (ShouldCancel())
                break;

            depth++;
            bestMove = bestMoveThisIter;
        }

        if (bestMove == Move.NullMove)
            return bestMoveThisIter;

        return bestMove;
    }

    float Quiesce(float alpha, float beta)
    {
        float eval = Evaluate() * (currentPosition.IsWhiteToMove ? 1 : -1);
        if (eval >= beta)
            return beta;
        alpha = Math.Max(eval, alpha);

        foreach (Move move in currentPosition.GetLegalMoves(true))
        {
            currentPosition.MakeMove(move);
            eval = -Quiesce(-beta, -alpha);
            currentPosition.UndoMove(move);

            if (eval >= beta)
                return beta;
            alpha = Math.Max(eval, alpha);
        }

        return alpha;
    }

    float NegaMax(int depth, float alpha, float beta)
    {
        if (currentPosition.IsDraw())
            return 0;

        if (currentPosition.IsInCheckmate())
            return (currentPosition.IsWhiteToMove ? float.NegativeInfinity : float.PositiveInfinity);

        if (depth == 0)
            return Quiesce(alpha, beta);

        foreach (Move move in GetOrderedMoves(currentPosition.GetLegalMoves()))
        {
            currentPosition.MakeMove(move);
            if (currentPosition.IsInCheckmate())
            {
                currentPosition.UndoMove(move);
                return float.PositiveInfinity;
            }
            float eval = -NegaMax(depth - 1 + (currentPosition.IsInCheck() ? 1 : 0), -beta, -alpha);
            currentPosition.UndoMove(move);

            if (ShouldCancel())
                return 0;

            alpha = Math.Max(eval, alpha);
        }

        return alpha;
    }

    Move[] GetOrderedMoves(Move[] moves)
    {
        int legalMovesLength = moves.Length;
        int[] offsets = { 0, legalMovesLength, legalMovesLength * 2, legalMovesLength * 3 };
        var orderedMoves = new Move[legalMovesLength * 4 + 1];

        // ORDER:
        //
        // higher captures
        // worse captures
        // checks
        // other
        //

        foreach (Move move in moves)
            // this line of code is so confusing, but here is the explanation:
            // it uses ternary operators to determine which index of the offsets a move belongs
            // if a move is a capture, it then checks if it is a winning capture (piece capturing has a higher value)
            // or a losing capture (piece capturing has a lower value)
            // if its winning, then it uses an index of 0 in the offsets
            // otherwise it uses 1
            // then it checks if the move checks
            // if it does it uses an index of 2
            // otherwise it is a misc. move and uses index 3
            // then the offset at that index is incremented after returning the current value.
            orderedMoves[offsets[move.IsCapture ?
                (pieceValues[(int)move.CapturePieceType] > pieceValues[(int)move.MovePieceType] ? 0 : 1)
                : MoveChecks(move) ? 2 : 3]++ + 1] = move;

        return (from move in orderedMoves where !move.IsNull select move).ToArray();
    }

    float Evaluate()
    {
        ulong key = currentPosition.ZobristKey;
        if (storedEvaluations.ContainsKey(key))
            return storedEvaluations[key];

        float endGameness = GetEndGameness();
        float score = 0;

        foreach (PieceList list in currentPosition.GetAllPieceLists())
        {
            float mult = list.IsWhitePieceList ? 1 : -1;
            foreach (Piece piece in list)
            {
                int index = piece.IsWhite ? (7 - piece.Square.Rank) * 8 + piece.Square.File : piece.Square.Index;
                score += (psqts[(int)piece.PieceType - 1, index] * (1 - endGameness) + psqts[(int)piece.PieceType + 5, index] * endGameness + pieceValues[(int)piece.PieceType]) * mult;
            }
        }

        if (endGameness == 1)
        {
            Square kingWhite = currentPosition.GetKingSquare(true);
            Square kingBlack = currentPosition.GetKingSquare(false);
            score += (float)(Math.Sign(score) * 15 * Math.ReciprocalSqrtEstimate(Math.Pow(kingWhite.File - kingBlack.File, 2) + Math.Pow(kingWhite.Rank - kingBlack.Rank, 2)));
        }

        storedEvaluations[key] = score;
        return score;
    }

    float GetEndGameness()
    {
        float endGameness = 0;

        foreach (PieceList list in currentPosition.GetAllPieceLists())
        {
            if (list.TypeOfPieceInList == PieceType.Pawn)
                continue;
            foreach (Piece piece in list)
                endGameness += pieceValues[(int)piece.PieceType];
        }

        endGameness /= 6766;
        return endGameness < 0.2 ? 1 : 1 - endGameness;
    }

    bool MoveChecks(Move move)
    {
        currentPosition.MakeMove(move);
        bool checks = currentPosition.IsInCheck();
        currentPosition.UndoMove(move);
        return checks;
    }
}