namespace auto_Bot_281;
/*

Thank you so much for giving me this wonderful opportunity to build this mini bot.
I already made a chess engine named timecat after getting inspired into chess by an Indian comedian Samay Raina.
(My engine is named after him, Samay means time in Hindi and CAT because I was testing it against stockFISH)
My chess engine link: https://github.com/Gourab-Ghosh/timecat-rs
Your video was the first one from where I got the basic idea how a chess engine works.
As I have already made chess engine which is around 3200 (by my estimation), This project became more interesting to me, to try to code the same thing with limited number of tokens.

I have used the negamax algorithm to make this bot. I also applied the following optimizations:

- Iterative Deepening
- Alpha Beta Pruning with Move Ordering
- Transposition Table
- Mate Distance Pruning
- Null Move Pruning
- Killer Heuristic

The main challenge with project was to apply more and more optimizations and improvements within less code.
For this I had to come up with some creative approaches to shorten the code.
For example I merged the Negamax search and the Quiescence search into a single function to avoid repetition of code.
For this, I didn't have to make separate functions to sort moves and use it in both Negamax search and the Quiescence search functions.
This creative approaches saved a lot of Bot Brain Capacity. All the creative approaches are written in the comments inside the code.

But the most creative thing I have done in my code is that I could successfully encode 12*64 = 786 position scores into 24 ulongs + 16 int = 40 integers in total
(The tables were created by ChatGPT, later edited few square scores by me to make the engine more effective)
The list named positionValuesCompressedIndices contains the position values of all the pieces for opening and endgame.
It contains 14 tables in total (two extra for null pieces with all squares having value 0)

First I tried to copy paste the entire table (The opening tables only and no endgame table).
The tables itself filled 80% of the Bot Brain Capacity.
Then after looking at the rook's endgame table I realized that there are only few unique integers in the table.
I checked my assumption with python and found that there were 16 unique integers which are listed in the pieceValuesAndPositionIndices list.
I also realized that almost all the tables are Y-symmetric, which means score if File and (7-File) must be same provided rows are same.
Checking this assumption with python I found this assumption was also correct except for one pair of squares in the queen's opening table.
I changed it manually to make every table Y-symmetric.
Now for each and every table I need to save only 32 integers instead of 64.
I then took all the unique integers and sorted them and saved them in a list and then mapped all the integers to the indices of them in the list.
So -50 got mapped to 0, -40 got mapped to 1 and so on.
Each of this numbers are 4 bits (as the values are from 0 to 15). So a ulong would store 16 such integers.
So the whole table would fit into 2 such integers (as we have to save only 32 integers).
So all the 12 tables took 2*12 = 24 ulongs and 16 unique ints.

Here the compression ratio is technically 768 / 40 = 19.2 but the 16 ints are actually constants.
So if I wish to increase the number of tables to enhance my engine performance further, those 16 ints will become insignificant.
So the true compression ratio is 768 / 24 = 32 which is a huge compression

I used the following python code to automate this task:

######################################################################## Start of Python Code ########################################################################

l = np.array([
    # Null Piece Opening Phase:
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,

    # Null Piece Endgame Phase:
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,
      0,  0,  0,  0,  0,  0,  0,  0,

    # Pawn Opening Phase:
      0,  0,  0,  0,  0,  0,  0,  0,
     50, 50, 50, 50, 50, 50, 50, 50,
     10, 15, 20, 30, 30, 20, 15, 10,
      5,  5, 10, 25, 25, 10,  5,  5,
      0,  0,  0, 20, 20,  0,  0,  0,
      5, -5,-10,  0,  0,-10, -5,  5,
      5, 10, 10,-20,-20, 10, 10,  5,
      0,  0,  0,  0,  0,  0,  0,  0,
    
    # Pawn Endgame Phase:
     50, 50, 50, 50, 50, 50, 50, 50,
     40, 40, 40, 40, 40, 40, 40, 40,
     30, 30, 30, 30, 30, 30, 30, 30,
     20, 20, 20, 20, 20, 20, 20, 20,
     10, 10, 10, 10, 10, 10, 10, 10,
      5,  5,  5,  5,  5,  5,  5,  5,
     -5, -5, -5, -5, -5, -5, -5, -5,
      0,  0,  0,  0,  0,  0,  0,  0,

    # Knight Opening Phase:
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,  0,  5,  5,  0,-20,-40,
    -30,  5, 10, 15, 15, 10,  5,-30,
    -30,  0, 15, 20, 20, 15,  0,-30,
    -30,  0, 15, 20, 20, 15,  0,-30,
    -30,  5, 10, 15, 15, 10,  5,-30,
    -40,-20,  0,  5,  5,  0,-20,-40,
    -50,-40,-30,-30,-30,-30,-40,-50,

    # Knight Endgame Phase:
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,-10, -5, -5,-10,-20,-40,
    -30,-10, 10, 15, 15, 10,-10,-30,
    -30, -5, 15, 20, 20, 15, -5,-30,
    -30, -5, 15, 20, 20, 15, -5,-30,
    -30,-10, 10, 15, 15, 10,-10,-30,
    -40,-20,-10, -5, -5,-10,-20,-40,
    -50,-40,-30,-30,-30,-30,-40,-50,

    # Bishop Opening Phase:
    -20,-10,-10,-10,-10,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5, 10, 10,  5,  0,-10,
    -10,  5,  5, 10, 10,  5,  5,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10, 10, 10, 10, 10, 10, 10,-10,
    -10,  5,  0,  0,  0,  0,  5,-10,
    -20,-10,-10,-10,-10,-10,-10,-20,

    # Bishop Endgame Phase:
    -20,-10,-10,-10,-10,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10,  0, 10, 30, 30, 10,  0,-10,
    -10,  0, 10, 30, 30, 10,  0,-10,
    -10,  0, 10, 10, 10, 10,  0,-10,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -20,-10,-10,-10,-10,-10,-10,-20,

    # Rook Opening Phase:
      0,  0,  0,  0,  0,  0,  0,  0,
      5, 10, 10, 10, 10, 10, 10,  5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
     -5,  0,  0,  0,  0,  0,  0, -5,
      0,  0,  0,  5,  5,  0,  0,  0,
    
    # Rook Endgame Phase:
     40, 40, 40, 40, 40, 40, 40, 40,
     50, 50, 50, 50, 50, 50, 50, 50,
     40, 40, 40, 40, 40, 40, 40, 40,
     40, 40, 40, 40, 40, 40, 40, 40,
     40, 40, 40, 40, 40, 40, 40, 40,
     40, 40, 40, 40, 40, 40, 40, 40,
     40, 40, 40, 40, 40, 40, 40, 40,
     40, 40, 40, 40, 40, 40, 40, 40,

    # Queen Opening Phase:
    -20,-10,-10, -5, -5,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5,  5,  5,  5,  0,-10,
     -5,  0,  5, 10, 10,  5,  0, -5,
      0,  0,  5, 10, 10,  5,  0,  0,
    -10,  5,  5,  5,  5,  5,  5,-10,
    -10,  0,  5,  0,  0,  5,  0,-10,
    -20,-10,-10, -5, -5,-10,-10,-20,

    # Queen Endgame Phase:
    -20,-10,-10, -5, -5,-10,-10,-20,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -10,  0,  5,  5,  5,  5,  0,-10,
     -5,  0,  5, 10, 10,  5,  0, -5,
     -5,  0,  5, 10, 10,  5,  0, -5,
    -10,  0,  5,  5,  5,  5,  0,-10,
    -10,  0,  0,  0,  0,  0,  0,-10,
    -20,-10,-10, -5, -5,-10,-10,-20,

    # King Opening Phase:
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -30,-40,-40,-50,-50,-40,-40,-30,
    -20,-30,-30,-40,-40,-30,-30,-20,
    -10,-15,-20,-20,-20,-20,-15,-10,
     20, 20,  0,  0,  0,  0, 20, 20,
     20, 30, 10,  0,  0, 10, 30, 20,
    
    # King Endgame Phase:
    -50,-40,-30,-30,-30,-30,-40,-50,
    -40,-20,-10,  0,  0,-10,-20,-40,
    -40,-10, 20, 30, 30, 20,-10,-40,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -30,-10, 30, 40, 40, 30,-10,-30,
    -40,-10, 20, 30, 30, 20,-10,-40,
    -40,-30,  0,  0,  0,  0,-30,-40,
    -50,-40,-40,-30,-30,-40,-40,-50,
]).reshape((-1, 8, 8))

unique_values = [-50, -40, -30, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 40, 50]

print(f"Unique Values: {unique_values}")

for index, matrix in enumerate(l):
    non_equality_matrix = matrix[:, :4] != matrix[:, :3:-1]
    if (non_equality_matrix).any():
        print(f"The following matrix with index {index} is not Y-symmetric:\n\n{matrix}")
        print(non_equality_matrix)

l_new = list(l.flatten().copy())

for i in range(len(l_new)):
    l_new[i] = unique_values.index(l_new[i])

l_new = np.array(l_new).reshape((-1, 8, 8))
compressed = 0
num_bits = 0

outcomes = []

for matrix in l_new:
    matrix = matrix[:, :4]
    for val in matrix.flatten():
        val = int(val)
        compressed = (compressed << 4) | val
        num_bits += 4
        if num_bits == 64:
            outcomes.append(compressed)
            compressed = 0
            num_bits = 0

lines = []

modified_hex = lambda x: "0x" + hex(x)[2:].upper().zfill(16)

for piece in ["Null", "Pawn", "Knight", "Bishop", "Rook", "Queen", "King"]:
    s = ""
    for _ in range(4):
        s += modified_hex(outcomes.pop(0)) + ", "
    lines.append(s + f"// Compressed opening and endgame position indices for {piece} piece types")

lines[0] += " (All squares evaluates to 0)"
print("\n".join(lines))

######################################################################### End of Python Code #########################################################################

This project was awesome for me and because of this project I learnt C# for the first time in my life.
The whole experience was awesome and fun for me with few touches of expressing my creativity and skills and I enjoyed it very much.

I also tried to compress the whole code into a bunch of ulongs and then decompress them into a string of code and then run it.
But as I have started this late and also I have only one month experience in C#, so I couldn't find a way to run any code directly from string.
I believe that the above strategy will save huge space if we change the variable names to smaller names and remove unnecessary white spaces.
Then I could have implement more optimizations to make my engine ven stronger.
I don't think I can complete it and test it in 5 days. So this is the final code for now.

*/

using ChessChallenge.API;
using System;
using static System.Math;

public class Bot_281 : IChessBot
{
    Move bestMove;
    // I generated an issue that we should have a separate bar for the transposition table size but it was not officially supported.
    // So for this reason I had to set the transposition table size to 128 MB to be on safe size which takes 8388608 cells (= 2^23).
    readonly (ulong, byte, short, byte, ushort)[] transpositionTable = new (ulong, byte, short, byte, ushort)[8388608]; // (hash, depth, score, flag, bestMoveRawValue)

    public Move Think(Board board, Timer timer)
    {
        int timeDifference = Max(0, timer.OpponentMillisecondsRemaining - timer.MillisecondsRemaining);
        if (timeDifference > 3600_000)
            timeDifference = 0;
        int timePerMove = Max(
            10,
            Min(
                500 * board.PlyCount,
                (timer.MillisecondsRemaining - timeDifference) / 15 + timer.IncrementMilliseconds - 100
            )
        )
        , ply = 0
        , depth = 1;
        var killerMoves = new Move[150, 3]; // 150 is the maximum ply;
        int[] pieceValuesAndPositionIndices = {
            0, 100, 320, 330, 500, 900, 0, // Piece values (King is the most useless piece in chess so it's value is 0 :p)
            -50, -40, -30, -20, -15, -10, -5, 0, 5, 10, 15, 20, 25, 30, 40, 50, // Position values
        };
        ulong[] positionValuesCompressedIndices = {
            0x7777777777777777, 0x7777777777777777, 0x7777777777777777, 0x7777777777777777, // Compressed opening and endgame position indices for Null piece types (All squares evaluates to 0)
            0x7777FFFF9ABD889C, 0x777B865789937777, 0xFFFFEEEEDDDDBBBB, 0x9999888866667777, // Compressed opening and endgame position indices for Pawn piece types
            0x01221378289A27AB, 0x27AB289A13780122, 0x01221356259A26AB, 0x26AB259A13560122, // Compressed opening and endgame position indices for Knight piece types
            0x3555577757895889, 0x5799599958773555, 0x355557775799579D, 0x579D579957773555, // Compressed opening and endgame position indices for Bishop piece types
            0x7777899967776777, 0x6777677767777778, 0xEEEEFFFFEEEEEEEE, 0xEEEEEEEEEEEEEEEE, // Compressed opening and endgame position indices for Rook piece types
            0x3556577757886789, 0x7789588857873556, 0x3556577757886789, 0x6789578857773556, // Compressed opening and endgame position indices for Queen piece types
            0x2110211021102110, 0x32215433BB77BD97, 0x0122135715BD25DE, 0x25DE15BD12770112, // Compressed opening and endgame position indices for King piece types
        };

        ////////////////////////////////////////////////////////////////////// Functions /////////////////////////////////////////////////////////////////////////

        // I have defined all the functions locally because each static keyword was consuming brain capacity (Same for the constants).
        // Also this will help me develop my next version of the bot (if I get time to complete it).
        int DecompressData(int compressedDataOffset, int dataIndex) =>
            pieceValuesAndPositionIndices[((positionValuesCompressedIndices[compressedDataOffset] >> (60 - 4 * dataIndex)) & 0xF) + 7];

        int EvaluateBoard()
        {
            int score = 0, pieceScoreAbs = 0;
            foreach (PieceList pieceList in board.GetAllPieceLists())
                pieceScoreAbs += pieceList.Count * pieceValuesAndPositionIndices[(int)pieceList.TypeOfPieceInList];
            ulong allPiecesBB = board.AllPiecesBitboard;
            while (allPiecesBB != 0)
            {
                int squareIndex = BitboardHelper.ClearAndGetIndexOfLSB(ref allPiecesBB)
                , pieceColorSign = -1;
                Square square = new(squareIndex);
                Piece piece = board.GetPiece(square);
                if (piece.IsWhite) // Flipping the square horizontally for white pieces as the array index is mirror to square index
                {
                    squareIndex ^= 0x38; // Flip the square vertically
                    pieceColorSign = 1;
                }
                // Adding the piece value to the score
                score += pieceColorSign * pieceValuesAndPositionIndices[(int)piece.PieceType];
                // Extracting the data from the compressed data and adding to score
                int file = squareIndex % 8;
                int dataIndex = (squareIndex + Min(file, 14 - 3 * file)) / 2; // 4 * rank + Min(file, 7 - file) simplified to this formula.
                int compressedDataOffset = 4 * (int)piece.PieceType + dataIndex / 16;
                dataIndex %= 16;
                int openingScore = DecompressData(compressedDataOffset, dataIndex);
                // In the opening, the more the king is surrounded by pawns of same color, the safer it is.
                if (piece.IsKing && BitboardHelper.SquareIsSet(0xFFFF_0000_0000_FFFF, square))
                {
                    openingScore += 20 * BitboardHelper.GetNumberOfSetBits(
                        BitboardHelper.GetKingAttacks(square) &
                        board.GetPieceBitboard(PieceType.Pawn, piece.IsWhite)
                    );
                }
                // The score is interpolated between the opening position score and the endgame position score
                // with respect to the total absolute piece values on the board defined by the pieceScoreAbs variable
                score += pieceColorSign * (
                    pieceScoreAbs * openingScore +
                    (8000 - pieceScoreAbs) * DecompressData(compressedDataOffset + 2, dataIndex)
                ) / 8000; // Since max value of pieceScoreAbs is 8000
            }
            // The positional score for the king automatically forces the king to the corner in the endgame
            // which is really important for checkmate in winning positions like king rook vs king.
            // So we don't need to implement it separately.
            return board.IsWhiteToMove ? score : -score;
        }

        int ScoreMove(Move move, ushort ttBestMoveRawValue)
        {
            if (ply == 0 && move == bestMove)
                return 99_000;
            if (move.RawValue == ttBestMoveRawValue)
                return 98_000;
            int pieceTypeTimesHundred = 100 * (int)move.MovePieceType;
            if (move.IsCapture)
                return 97_000 - pieceTypeTimesHundred + (int)move.CapturePieceType;
            for (int i = 0; i < 3; i++)
                if (move == killerMoves[ply, i])
                    return 96_000 - i;
            if (move.IsPromotion)
                return 95_000 + (int)move.PromotionPieceType;
            if (move.IsCastles)
                return 94_000;
            return Min(move.TargetSquare.File, 7 - move.TargetSquare.File) - pieceTypeTimesHundred;
        }

        void WriteTranspositionTable(int depth, int score, byte flag, ushort bestMoveRawValue)
        {
            ulong index = board.ZobristKey % 8388608;
            (ulong ttHash, byte ttDepth, short ttScore, _, _) = transpositionTable[index];
            // If the value is not from quiescence search (depth != 0) and the depth <= the current depth and the score is not a mate score (<= 24000) then overwrite the entry
            if (depth != 0 && Abs(ttScore) <= 24000 && (ttHash != board.ZobristKey || ttDepth <= depth))
                transpositionTable[index] = (board.ZobristKey, (byte)depth, (short)score, flag, bestMoveRawValue);
        }

        int AlphaBetaAndQuiescence(int depth, int alpha = -30000, int beta = 30000) // Merged Quiescence and AlphaBeta into single function (depth == 0 means Quiescence search)
        {
            int mate_score = 25000 - ply, score = -mate_score;
            if (board.IsInCheck() && depth > 1)
                depth++;
            depth = Max(depth, 0);
            bool isNotQuiescenceSearch = depth != 0;
            if (board.IsInCheckmate() && isNotQuiescenceSearch)
                return score;
            if (board.IsDraw())
                return 0;
            // Mate distance pruning (copied form Weiawaga chess engine)
            alpha = Max(alpha, -mate_score);
            beta = Min(beta, mate_score - 1);
            if (alpha >= beta)
                return alpha;
            // Transposition Table Lookup
            (ulong ttHash, byte ttDepth, short ttScore, byte ttFlag, ushort ttBestMoveRawValue) = transpositionTable[board.ZobristKey % 8388608];
            if (ply != 0 && ttHash == board.ZobristKey && ttDepth >= depth && isNotQuiescenceSearch)
            {
                if (ttFlag == 0)
                    return ttScore;
                if (ttFlag == 1 && ttScore <= alpha)
                    return alpha;
                if (ttFlag == 2 && ttScore >= beta)
                    return beta;
            }
            if (ttHash != board.ZobristKey)
                ttBestMoveRawValue = Move.NullMove.RawValue;
            // Null move pruning
            if (depth > 2 && Abs(beta) <= 24000) // No need to add the condition "&& isNotQuiescenceSearch" as depth > 2 is mentioned
                if (board.TrySkipTurn())
                {
                    ply++;
                    int nullMoveReductionScore = -AlphaBetaAndQuiescence(depth - 1 - Max(2, depth / 2), -beta, 1 - beta);
                    board.UndoSkipTurn();
                    ply--;
                    if (nullMoveReductionScore >= beta)
                        return beta;
                }
            if (!isNotQuiescenceSearch)
            {
                score = EvaluateBoard();
                if (score >= beta)
                    return beta;
                alpha = Max(alpha, score);
            }
            var moves = board.GetLegalMoves(!isNotQuiescenceSearch);
            Array.Sort(moves, (a, b) => ScoreMove(b, ttBestMoveRawValue) - ScoreMove(a, ttBestMoveRawValue));
            // Make sure that the best move is not an illegal move. This is done in root node only.
            if (ply == 0)
                bestMove = moves[0];
            byte flag = 1;
            foreach (Move move in moves)
            {
                board.MakeMove(move);
                ply++;
                score = -AlphaBetaAndQuiescence(depth - 1, -beta, -alpha);
                board.UndoMove(move);
                ply--;
                if (timer.MillisecondsElapsedThisTurn > timePerMove)
                    return 0;
                if (score > alpha)
                {
                    alpha = score;
                    flag = 0;
                    if (ply == 0) // Update the best move in root node only
                        bestMove = move;
                    if (alpha >= beta)
                    {
                        if (!move.IsCapture) // no need to add "&& isNotQuiescenceSearch" here because in Quiescence search, only captures are searched
                        {
                            for (int i = 2; i > 0; i--)
                                killerMoves[ply, i] = killerMoves[ply, i - 1];
                            killerMoves[ply, 0] = move;
                        }
                        WriteTranspositionTable(depth, beta, 2, move.RawValue);
                        return beta;
                    }
                }
            }
            WriteTranspositionTable(depth, alpha, flag, ttBestMoveRawValue);
            return alpha;
        }

        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        do
            AlphaBetaAndQuiescence(depth++);
        while (timer.MillisecondsElapsedThisTurn < timePerMove && depth <= 100); // 100 is the maximum depth
        return bestMove;
    }
}